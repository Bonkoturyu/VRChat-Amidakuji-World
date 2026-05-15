# Architecture

実装パターン・データフロー・モジュール責務をまとめる。
仕様の定義(ステートマシン、Synced変数一覧)は [SPEC.md](./SPEC.md) を、設計判断の根拠は [ADRs](./adr/) を参照。

## システム構成

```
World Scene
├── GameManager (UdonBehaviour)                ← ステート・同期の中枢
├── AmidakujiGenerator (UdonBehaviour)         ← seedから配置算出
├── Tracks
│   ├── VerticalLines[0..3] (Prefab)           ← 縦通路ビジュアル
│   └── HorizontalBars[lane][seg] (Prefab)     ← 横線全パターン事前配置
├── Carts[0..3] (Prefab)
│   ├── VRC_Station
│   └── CartController (UdonBehaviour)
├── EntryArea
│   ├── Seats[0..3]                            ← 着座Interact
│   └── StartButton (UdonBehaviour)            ← Master限定
├── SpectatorArea
│   ├── ObservationDeck                        ← ガラス床バルコニー
│   └── ScreenSystem
│       ├── OverviewCamera                     ← Orthographic, top-down
│       ├── RenderTexture (Asset)
│       └── ScreenQuad (Material: RT)
├── PrizeAreas[0..3]                           ← ゴール後テレポート先
└── UI
    ├── RulesPanel
    └── ResultDisplay                          ← Worldspace Canvas
```

シーンのHierarchy詳細とPrefab分割は [scene-structure.md](./scene-structure.md) を参照。

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
6. 自分は観戦エリアにスポーン誘導(着座不可状態)

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

- カート位置補間は GameObject Transform 直接更新。Animator不使用で軽量
- 横線オブジェクトは事前配置 + enable切り替えで、Instantiate不要
- RenderTextureカメラ1台で +1パス。Culling Maskで描画対象を絞る
- Reflection Probe は静的Baked。ガラス床の反射ベイク済み
- Static Batchingに乗せやすいよう、地形・装飾は Static フラグを立てる(詳細 [scene-structure.md §5](./scene-structure.md))
