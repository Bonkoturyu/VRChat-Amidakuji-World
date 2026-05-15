# Scene Structure & Prefab Split

Phase 1着手時の Unity シーン構成、Prefab分割、レイヤー・命名規約の指針をまとめる。
v1.0 では PC + Android 両プラットフォーム対応のため、モバイル制約を意識した構成にしている。

---

## 1. ルート Hierarchy

```
[Scene Root]
├── _World/                       ← ワールド要素の organizational root
│   ├── Environment/              ← 装飾・地形・空・床(Static)
│   │   ├── Skybox_Reference (任意)
│   │   ├── Ground/
│   │   └── Decoration/
│   ├── SpawnPoints/
│   │   └── DefaultSpawn          ← VRC Scene Descriptor 参照
│   ├── AmidakujiTrack/           ← あみだくじ本体(歩行可能構造)
│   │   ├── VerticalLanes/
│   │   │   ├── Lane_0  (Prefab Instance)
│   │   │   ├── Lane_1  (Prefab Instance)
│   │   │   ├── Lane_2  (Prefab Instance)
│   │   │   └── Lane_3  (Prefab Instance)
│   │   ├── HorizontalBars/
│   │   │   ├── Bar_L0_S00  (Prefab Instance, lane 0-1, seg 0)
│   │   │   ├── Bar_L0_S01
│   │   │   ├── ... (全パターン事前配置、最大36個)
│   │   │   └── Bar_L2_S11
│   │   ├── StartMarkers/
│   │   │   ├── Start_0 ... Start_3
│   │   │   └── (Empty GameObject, Transformのみ使用)
│   │   ├── GoalMarkers/
│   │   │   └── Goal_0 ... Goal_3
│   │   └── GoalBarriers/         ← カートだけ通れる物理壁
│   │       ├── Barrier_0  (Prefab Instance, laneIndex=0)
│   │       ├── Barrier_1
│   │       ├── Barrier_2
│   │       └── Barrier_3
│   ├── Carts/
│   │   ├── Cart_0  (Prefab Instance, laneIndex=0)
│   │   ├── Cart_1  (Prefab Instance, laneIndex=1)
│   │   ├── Cart_2  (Prefab Instance, laneIndex=2)
│   │   └── Cart_3  (Prefab Instance, laneIndex=3)
│   ├── EntryArea/                ← あみだくじ最上部
│   │   ├── Floor/
│   │   ├── Seats/
│   │   │   ├── Seat_0 ... Seat_3  (Prefab Instances, seatIndex=N)
│   │   ├── StartButton            ← 一意、Prefab化しない
│   │   ├── RulesPanel             ← 追いかけ式観戦の説明含む
│   │   └── ResultDisplay          ← レース結果掲示UI
│   └── PrizeAreas/
│       ├── Prize_0  (Prefab Instance, prizeIndex=0)
│       ├── Prize_1
│       ├── Prize_2
│       └── Prize_3
│
├── _Managers/                    ← Udon Behaviour 群(可視不要、Position 0)
│   ├── GameManager               ← UdonBehaviour: GameManager.cs
│   └── AmidakujiGenerator        ← UdonBehaviour: AmidakujiGenerator.cs
│
├── _Lighting/
│   ├── DirectionalLight
│   ├── LightProbeGroup
│   └── ReflectionProbe (1-2個)
│
└── VRCWorld                      ← VRChat必須オブジェクト
```

注: 観戦デッキ(`SpectatorArea`)、観戦スクリーン(`ScreenSystem`)、俯瞰カメラ(`OverviewCamera`)は廃止。
非参加者はあみだくじ構造そのものを歩いて観戦する([ADR-0009](./adr/0009-follow-alongside-spectator.md))。

**命名規約**:
- `_` プレフィックス: organizational root (折りたたみ用、構造上の整理)
- PascalCase + アンダースコア番号: `Lane_0`, `Seat_0`, `Cart_0`, `Barrier_0`
- 番号は 0-indexed(配列インデックスと揃える)
- Prefab Instance は変更を Override せず、必要な可変項目だけ Inspector で設定

---

## 2. Prefab分割

### 2.1 Prefab化するもの (再利用あり)

| Prefab | インスタンス数 | 可変項目 (Inspector) | 内容 |
|---|---|---|---|
| `Lane.prefab` | 4 | なし(Transform位置のみ) | 縦通路(歩行可能な床+柱) |
| `HorizontalBar.prefab` | 最大36 | なし | 横線(歩行可能な連絡通路) |
| `GoalBarrier.prefab` | 4 | `laneIndex (0-3)` | ゴール手前壁(カート用隙間あり) |
| `Cart.prefab` | 4 | `laneIndex (0-3)`, `GameManager 参照` | カート本体 + VRC_Station + CartController |
| `Seat.prefab` | 4 | `seatIndex (0-3)`, `GameManager 参照` | 着座 Interact + 視覚マーカー |
| `PrizeArea.prefab` | 4 | `prizeIndex (0-3)` | ゴール後テレポート先の部屋(v1.0は同一見た目) |

### 2.2 Prefab化しないもの (一意・シーン固有)

| GameObject | 理由 |
|---|---|
| `GameManager` | シングルトン。Synced 変数のオーナー |
| `AmidakujiGenerator` | シングルトン |
| `StartButton` | 一意、Inspector で GameManager 参照を直接バインド |
| `RulesPanel` | テキスト内容がシーン固有 |
| `ResultDisplay` | 一意 |
| `Environment/Decoration` | シーン固有装飾 |

---

## 3. Prefab 内部構造 (主要なもの)

### 3.1 Cart.prefab

```
Cart_X (GameObject, Transform 位置 = 起点)  [Layer: Cart]
├── Visual/                       ← 見た目モデル
│   ├── Body (Mesh Renderer)
│   └── Wheels (任意装飾)
├── Seat (GameObject)
│   └── VRC_Station               ← Component
└── (CartController を Root に Add)
```

- Root に `CartController.cs` (UdonBehaviour)
- VRC_Station の `Player Mobility` = Mobile、`Seated` = true
- **Cart レイヤー設定により、歩行者プレイヤーと衝突しない**

### 3.2 GoalBarrier.prefab

```
Barrier_X (GameObject)                       [Layer: GoalBarrier]
├── WallLeft (Mesh + Collider)               ← 隙間の左側の壁
├── WallRight (Mesh + Collider)              ← 隙間の右側の壁
└── Ceiling (Mesh + Collider)                ← 隙間の上部(歩行者立位を阻止)
```

- 隙間サイズ: 幅 1.5m × 高さ 0.5m(カート幅・高さの設計値に合わせる)
- カートは Cart レイヤーなので Ceiling とは衝突しない
- 歩行者は Player レイヤーなので Ceiling にぶつかり、しゃがんでも通れない設計
- 物理レイヤー設定は §4 参照

### 3.3 Seat.prefab

```
Seat_X (GameObject)
├── Visual/                       ← 着座位置を示すマーカー(ピンスポット等)
└── InteractTrigger (Collider, IsTrigger=true)
    └── VRC_Interact + SeatInteract.cs (UdonBehaviour)
```

- 着座すると `GameManager.OnSeatClaimed(seatIndex)` を呼ぶ
- gameState != Idle なら無反応

### 3.4 HorizontalBar.prefab

```
Bar_LX_SXX (GameObject)
├── FloorMesh (歩行可能な床、Mesh + Collider)
└── Railing (任意装飾)
```

- 歩行可能な床として機能
- `AmidakujiGenerator` から `SetActive(true/false)` で表示制御
- Static flag は立てない(動的 enable のため Static Batching 非対象)

### 3.5 Lane.prefab

```
Lane_X (GameObject)
├── FloorMesh (歩行可能な床、Mesh + Collider)
└── PostMesh (柱、視覚装飾)
```

- Static flag を立てて Static Batching に乗せる
- 歩行可能な床面コライダー(プレイヤーが上を歩ける)

### 3.6 PrizeArea.prefab

```
Prize_X (GameObject)
├── Floor (Mesh)
├── Walls (Mesh)
├── TeleportTarget (Empty GameObject) ← TeleportTo の位置参照
└── DecorationMount (Empty)          ← v1.1で装飾を入れる場所
```

---

## 4. Layer / Tag 設定

カートと歩行者の衝突分離が重要。Project Settings > Tags and Layers と Physics で設定。

### 4.1 Layer 定義

| Layer | 用途 | 設定先 |
|---|---|---|
| `Default` (0) | 通常オブジェクト | 大半 |
| `Player` (9) | VRChat予約 | (システム、リモートプレイヤー) |
| `PlayerLocal` (10) | VRChat予約 | (システム、ローカルプレイヤー) |
| `MirrorReflection` (18) | VRChat予約 | (システム) |
| **User22: Cart** | カート本体・カート用コライダー | Cart Prefab Root |
| **User23: GoalBarrier** | ゴール手前バリア | GoalBarrier Prefab Root |

### 4.2 物理コリジョン Matrix (Project Settings > Physics)

| | Default | Player | PlayerLocal | Cart | GoalBarrier |
|---|---|---|---|---|---|
| Default | ✓ | ✓ | ✓ | ✓ | ✓ |
| Player | ✓ | ✓ | ✓ | **✗** | ✓ |
| PlayerLocal | ✓ | ✓ | ✓ | **✗** | ✓ |
| Cart | ✓ | **✗** | **✗** | ✓ | **✗** |
| GoalBarrier | ✓ | ✓ | ✓ | **✗** | ✓ |

ポイント:
- Cart × Player / PlayerLocal: **Off**(カートと歩行者は衝突しない)
- Cart × GoalBarrier: **Off**(カートはバリアの隙間を通過できる)
- GoalBarrier × Player / PlayerLocal: **On**(歩行者は壁にぶつかって賞品エリアに入れない)

### 4.3 Tags

- 既定の `Untagged`, `Player` などをそのまま使用
- 独自タグ追加は v1.0 では不要

---

## 5. Static フラグ運用

Static Batching・Light Baking のため、以下の方針:

| 対象 | Static フラグ |
|---|---|
| Environment 装飾、床、壁 | **All Static** |
| Lane Prefab | **All Static** |
| HorizontalBar | **None** (動的 enable のため) |
| GoalBarrier | **All Static**(配置は固定) |
| Cart | **None** (動く) |
| Seat | Visual部分のみ Static、InteractTrigger は None |
| StartButton | None (押下時にビジュアル変化) |
| PrizeArea | **All Static** |
| Managers | None |

Light Probe Group はあみだくじ構造内とプレイヤーが歩く範囲に配置。

---

## 6. Inspector フィールド設計 (主要 Prefab)

### CartController (Cart.prefab Root)

```
[Serializable Fields]
- public int laneIndex            // 0-3
- public Transform startMarker    // Start_N をドラッグ
- public Transform goalMarker     // Goal_N をドラッグ
- public Transform prizeTeleport  // PrizeArea_N の TeleportTarget をドラッグ
- public GameManager gameManager  // _Managers/GameManager をドラッグ
- public AmidakujiGenerator generator
- public float speed = 2.0f
- public VRC_Station station      // 自分の子の Station 参照
```

### SeatInteract (Seat.prefab Root)

```
- public int seatIndex
- public GameManager gameManager
- public Cart targetCart          // 紐づくカートへの参照(着座すると Cart の Station へ転送)
```

### StartButton (シーン専用)

```
- public GameManager gameManager
- public Renderer buttonRenderer  // 色変化用
- public Material activeMaterial
- public Material inactiveMaterial
```

### GameManager (シーン専用)

```
- public AmidakujiGenerator generator
- public CartController[] carts       // 4要素配列(シーンで埋める)
- public SeatInteract[] seats         // 4要素
- public Transform[] prizeTeleports   // 4要素
- public StartButton startButton
- public Text resultText              // 結果表示UI
```

### AmidakujiGenerator (シーン専用)

```
- public GameObject[] horizontalBars  // 全パターンを1次元配列で保持
                                       // [lane * SEGMENT_COUNT + segment] でアクセス
- public int LANE_COUNT = 4
- public int SEGMENT_COUNT = 12
```

---

## 7. 配置数値の指針 (Phase 1 で参照)

### あみだくじ本体

- 縦線間隔: **4.0 m** (X方向)
- 縦線長さ: **60.0 m** (Y方向、上から下へ)
- 縦線の上端 Y: **0** (基準)
- 縦線の下端 Y: **-60**
- セグメント長さ: 60 / 12 = **5.0 m** (1セグメントあたり)
- 縦線の中央 X: 4本それぞれ -6, -2, +2, +6 (中心 0)

### 縦線(歩行可能)の床

- 床幅: 約 1.5m(カート幅 + 歩行余裕)
- カートは中央を走り、両側 0.3m 程度のスペースがある状態

### 横線

- 各セグメント境界の Y: -5, -10, ..., -55 (= -5 × (seg+1))
- 1セグメント境界に 3 ペア (Lane 0-1, 1-2, 2-3)
- 計 12 × 3 = **36個** の HorizontalBar を事前配置
- 横線の幅: 1.5m(縦線床と同じ、歩行可能)

### ゴール手前バリア

- 配置 Y: **-58.5**(縦線下端 -60 の 1.5m 手前)
- 隙間幅: **1.5 m**(カート幅 + 余裕 0.1m)
- 隙間高さ: **0.5 m**(歩行者がしゃがんでも通れない高さ)
- 隙間中心位置: 各レーンの中央(X = -6, -2, +2, +6 / Y = -58.0)

### エントリーエリア

- 縦線上端より少し上(Y=+3 程度)、X方向はあみだくじ中央に合わせる
- 床サイズ: 16m × 8m
- Seat 配置: 縦線上端の真上 (X = -6, -2, +2, +6 / Y = +3 / Z = -2)
- StartButton: 中央前面 (X=0, Y=+4.5, Z=+3)

### 賞品エリア

- 縦線下端 (Y=-60) の直下にエリアを並べる
- 各 PrizeArea: 8m × 8m × 4m (高さ) の部屋
- TeleportTarget は床上 0.1m

### スポーン位置

- DefaultSpawn: エントリーエリアの後方 (X=0, Y=+3.1, Z=-6)
- スポーン直後にルール説明パネルとあみだくじ全景が見える位置取り

---

## 8. ワールド俯瞰イメージ (簡易)

```
                  TOP VIEW (X-Z plane, Y方向は省略)

                              +Z
                               ↑
            ┌──────────────────────────────────┐
            │           SpawnPoint              │
            │        (背後にRulesPanel)         │
            │                                   │
            │   ┌────────┬────┬────┬────┐      │
            │   │EntryArea│ S1 │ S2 │ S3 │      │  Y = +3
            │   │  S0    │    │    │    │      │
            │   │        │    │ ▼  │    │      │
            │   ├────────┴────┴────┴────┤      │
            │   │       StartButton      │      │
            │   │       ResultDisplay    │      │
            │   └─┬────┬─┬────┬─┬────┬─┬─┘
            │     │    │ │    │ │    │ │    │      Y = 0 〜 -58
   -X ──────┤     │L0  │ │L1  │ │L2  │ │L3  │     (あみだくじ本体、歩行可能)
            │     │    │ │    │ │    │ │    │     観戦者はここを走り回る
            │     │    │ │    │ │    │ │    │
            │     │    │ │    │ │    │ │    │
            │     │═══ │ │═══ │ │═══ │ │═══ │     Y = -58.5 GoalBarrier
            │     │ ↓  │ │ ↓  │ │ ↓  │ │ ↓  │     (隙間からカートのみ通過)
            │     └────┘ └────┘ └────┘ └────┘
            │       │      │      │      │
            │     ┌────┐ ┌────┐ ┌────┐ ┌────┐
            │     │ P0 │ │ P1 │ │ P2 │ │ P3 │      Y = -64〜-60
            │     │    │ │    │ │    │ │    │     (賞品エリア)
            │     └────┘ └────┘ └────┘ └────┘
            └──────────────────────────────────┘
```

---

## 9. Phase 1 着手時のチェックリスト

Phase 1 のシーン組み立て時、以下の順序を推奨:

1. `_World`、`_Managers`、`_Lighting` の organizational root を作成
2. `VRCWorld` + VRC Scene Descriptor + DefaultSpawn を最初に置く(これがないと Build できない)
3. **Tags and Layers で User22 (Cart)、User23 (GoalBarrier) を追加**
4. **Physics 設定でコリジョンMatrix(§4.2)を設定**
5. 試しに空ワールドでアップロード疎通 (Phase 0 のおさらい)
6. Lane.prefab を1つ作成(歩行可能な床つき)、Lane_0〜3 を配置してスケール感確認
7. HorizontalBar.prefab を作成(歩行可能な床つき)、1セグメント分(3本)を仮配置 → OKなら全36本展開
8. GoalBarrier.prefab を作成、4個配置して隙間サイズを VR HMD で実機確認(しゃがんで通れないか、カート想定サイズで通れるか)
9. Cart.prefab、Seat.prefab はメッシュ未確定でもダミーキューブで Prefab 化 → Phase 2 で見た目を整える
10. EntryArea、PrizeAreas を順番に大枠だけ配置
11. ライティングは仮 (Skybox + Directional Light) のまま、Phase 9 でベイク

**重要**: 全マテリアルを Mobile/VRChat/Lightmapped 系で作成し、テクスチャは 1024×1024 以下に。Android対応のため、Phase 1 から制約を守ったほうが Phase 7 での手戻りが少ない。

完了基準は SPEC.md / tasklist.md の Phase 1 セクション参照。
