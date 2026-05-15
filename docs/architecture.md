# Architecture

実装パターン・データフロー・モジュール責務をまとめる。
仕様の定義(ステートマシン、Synced変数一覧)は [SPEC.md](./SPEC.md) を、設計判断の根拠は [ADRs](./adr/) を参照。

## システム構成

```
World Scene
├── GameManager (UdonBehaviour)                ← ステート・同期の中枢
├── AmidakujiGenerator (UdonBehaviour)         ← seedから配置算出
├── Tracks
│   ├── VerticalLanes[0..3] (Prefab)           ← 縦通路(歩行可能な床+柱)
│   └── HorizontalBars[lane][seg] (Prefab)     ← 横線全パターン事前配置(歩行可能)
├── Carts[0..3] (Prefab)
│   ├── VRC_Station
│   └── CartController (UdonBehaviour)
├── EntryArea
│   ├── Seats[0..3]                            ← 着座Interact
│   ├── StartButton (UdonBehaviour)            ← Master限定
│   └── ResultDisplay (Worldspace UI)          ← 結果掲示
├── GoalBarriers[0..3]                         ← 各レーン下端の物理メッシュバリア
└── PrizeAreas[0..3]                           ← ゴール後テレポート先
```

シーンのHierarchy詳細とPrefab分割は [scene-structure.md](./scene-structure.md) を参照。

## Layer 設計 (重要)

カートと歩行者が同じ空間を使用するため、衝突分離が必須。

| Layer | 用途 |
|---|---|
| `Default` (0) | 通常オブジェクト、世界の床・壁 |
| `Player` (9) | VRChat予約、リモートプレイヤー |
| `PlayerLocal` (10) | VRChat予約、ローカルプレイヤー |
| **User22: Cart** | カートの Visual/Collider 用。Player と衝突しない設定 |
| **User23: GoalBarrier** | ゴール手前バリア。Player とは衝突、Cart とは衝突しない |

Edit > Project Settings > Physics で:
- `Cart × Player` の衝突を **Off**
- `Cart × PlayerLocal` の衝突を **Off**
- `GoalBarrier × Cart` の衝突を **Off**
- `GoalBarrier × Player` の衝突を **On**(歩行者は通れない)
- `GoalBarrier × PlayerLocal` の衝突を **On**

## データフロー(レーススタート時シーケンス)

```
[Master] StartButton.Interact()
   │
   ▼
GameManager.RequestStart()
   │  Ownership確認 → 自分がMasterか
   ▼
GameManager:
   seed         = (int)System.DateTime.Now.Ticks
   raceStartTime = Networking.GetServerTimeInSeconds() + 3.0
   gameState    = Countdown
   RequestSerialization()
   │
   ▼   (UdonSync → 全クライアントへ)
[All Clients] OnDeserialization()
   │
   ▼
AmidakujiGenerator.Rebuild(seed)
   ├─ System.Random(seed) で横線配置を決定
   └─ HorizontalBars[i][j].SetActive(hasBar)
   │
   ▼
CartController[n].ComputePath(seed, n)
   └─ Waypoint[] を算出して保持
   │
   ▼  (raceStartTime 到達まで待機)
   ▼
[各クライアント] Update()
   double now     = Networking.GetServerTimeInSeconds();
   double elapsed = Networking.CalculateServerDeltaTime(now, raceStartTime);
   transform.position = LerpAlongWaypoints(elapsed);
```

## キーモジュール責務

### GameManager

- ステートマシン保持(状態定義は [SPEC.md §6](./SPEC.md#6-ゲームフロー-ステートマシン))
- Synced変数の更新権(Master限定。変数定義は [SPEC.md §9](./SPEC.md#9-同期モデル))
- スタートボタンからのコール受付
- カート群への状態通知
- Late Joiner時の状態復元 (`OnDeserialization`)

### AmidakujiGenerator

- seedから横線配置を生成(`System.Random` 使用、詳細は [ADR-0002](./adr/0002-deterministic-rng-seed-sync.md))
- `HorizontalBars[][]` の enable/disable
- 連続横線禁止ロジック
- CartControllerに対する経路問い合わせAPI (`HasBarAt(seg, lane)` 等)

### CartController

- 自カートの開始レーン番号を保持
- gameState変化時にWaypoint配列を再計算
- Running中、`CalculateServerDeltaTime` ベースで `transform.position` を補間
- ゴール到達時、座っているプレイヤーを賞品エリアに `TeleportTo`
- 走行中の `OnStationExited` 検出でリタイア処理([ADR-0007](./adr/0007-vrcstation-transform-cart.md))

### StartButton

- VRC_Interact 受け取り
- Master判定 (`Networking.IsMaster`)
- 参加者数チェック (>= 1)
- `GameManager.RequestStart()` を呼ぶ
- ビジュアル状態 (有効/無効) を更新

### Seat (Entry)

- VRC_Interact で着座リクエスト
- gameState == Idle のときのみ反応
- 自分のレーン番号を `GameManager.participantPlayerIds[]` に登録 (Ownership transfer)

## 観戦システム(追いかけ式)

[ADR-0009](./adr/0009-follow-alongside-spectator.md) で決定。

### 物理構造

- あみだくじ本体は **全プレイヤーが歩行可能**(コライダー付き床メッシュ)
- カートは Cart レイヤー、歩行者と衝突しない
- 各レーン下端の手前 1.5m に **ゴール手前バリア** を設置
  - バリアは GoalBarrier レイヤー
  - 物理メッシュ製の壁、カート幅(約1.5m × 0.5m高さ)の隙間あり
  - 隙間の高さ 0.5m により、歩行者は立ったまま通れない
  - カートは隙間を通り抜けてゴール位置に到達 → 座っているプレイヤーをテレポート

### 動線

```
[Spawn] → [EntryArea (最上部)] → 縦線・横線を歩いて下降 → [Goal Barriers]
                                                            ↑ ここで止まる
                                                          (カートだけ通過)
[Cart で座った参加者] → 自動巡回 → Goal を通過 → [PrizeArea] にテレポート
```

### 移動速度バランス

- カート速度: 2.0 m/s
- プレイヤー走行速度: 約 4-5 m/s(VRChatデフォルト)
- → 歩行者は十分カートに追いつける速度設定

## Late Joiner対応詳細

仕様レベルの方針は [SPEC.md §9](./SPEC.md#9-同期モデル)。ここでは実装手順を記す。

### Idle中に参加した場合

1. `OnDeserialization` で全Synced変数を受信
2. `participantPlayerIds[]` を読んで、どの席が埋まっているか UI に反映
3. 自分は空席に着座可能

### Running中に参加した場合

1. `OnDeserialization` で seed/state/startTime を受信
2. `AmidakujiGenerator.Rebuild(seed)` で横線配置を復元
3. 各 `CartController.ComputePath(seed, lane)` で経路復元
4. `Update()` 内で:
   ```
   double now     = Networking.GetServerTimeInSeconds();
   double elapsed = Networking.CalculateServerDeltaTime(now, raceStartTime);
   ```
   から現在位置を補間して即座にカート位置を表示
5. 既にゴール済みのカート(elapsed > totalPathTime)は終端位置で停止
6. 自分はあみだくじ最上部にスポーン誘導(参加不可状態、観戦に参加)

### ResultDisplay中に参加した場合

- 結果表示UIを Synced 情報から再構築
- 自由移動可

## 時刻計算の落とし穴

`Networking.GetServerTimeInSeconds()` は内部実装の都合で **クライアントによって負の値を返すケースがある**。生の値を直接引き算すると一部クライアントで意図しない結果になる。

正しいパターン:

```
double now     = Networking.GetServerTimeInSeconds();
double elapsed = Networking.CalculateServerDeltaTime(now, raceStartTime);
```

`CalculateServerDeltaTime` は内部で符号差を吸収する。詳細は [ADR-0003](./adr/0003-precomputed-waypoint-lerp.md)。

## パフォーマンス考察

### 共通

- カート位置補間は GameObject Transform 直接更新。Animator不使用で軽量
- 横線オブジェクトは事前配置 + enable切り替えで、Instantiate不要
- Static Batchingに乗せやすいよう、地形・装飾は Static フラグを立てる(詳細 [scene-structure.md §5](./scene-structure.md))

### Android (Quest) 固有

- 透明度マテリアルゼロ(観戦デッキ廃止により達成)
- マテリアル数 20 以下を意識
- テクスチャ 1024×1024 を上限
- GPU Instancing 全マテリアルで有効化
- Realtime Light なし、全 Baked
- 詳細制約は [ADR-0010](./adr/0010-android-in-v1.0-scope.md) と CLAUDE.md パフォーマンスバジェット参照
