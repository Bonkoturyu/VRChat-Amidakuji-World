# Scene Structure & Prefab Split

Phase 1着手時の Unity シーン構成、Prefab分割、レイヤー・命名規約の指針をまとめる。
v1.0 では PC + Android 両プラットフォーム対応のため、モバイル制約を意識した構成にしている。

レイアウト方針(2026-05-16 改訂): 全てを Y=0 平面の床上に配置する **平面水平レイアウト**
([ADR-0011](./adr/0011-flat-horizontal-layout.md))。縦線・横線は床上面に貼り付く細い Cube(高さ 2 cm)で表現。

---

## 1. ルート Hierarchy

```
[Scene Root]
├── _World/                       ← ワールド要素の organizational root
│   ├── Environment/              ← 装飾・空(Static)
│   │   ├── Skybox_Reference (任意)
│   │   └── Decoration/
│   ├── SpawnPoints/
│   │   └── DefaultSpawn          ← VRC Scene Descriptor 参照
│   ├── Ground/                   ← 床(単一の大型 Cube)
│   │   └── MainFloor             ← Scale (16, 0.2, 80), Position (0, -0.1, -28)
│   ├── AmidakujiLines/           ← あみだくじの線(床上面に貼り付く細い Cube)
│   │   ├── VerticalLines/
│   │   │   ├── VLine_0           ← X=-6, Cube Scale (0.2, 0.02, 60)
│   │   │   ├── VLine_1           ← X=-2
│   │   │   ├── VLine_2           ← X=+2
│   │   │   └── VLine_3           ← X=+6
│   │   └── HorizontalBars/
│   │       ├── Bar_L0_S00  (lane pair 0-1, position 0, Z=-3)
│   │       ├── Bar_L0_S01  (Z=-8)
│   │       ├── ... (全パターン事前配置、最大33個 = 11 段 × 3 ペア)
│   │       └── Bar_L2_S10  (lane pair 2-3, position 10, Z=-53)
│   ├── GoalBarriers/             ← カートだけ通れる物理壁
│   │   ├── Barrier_0  (Prefab Instance, laneIndex=0)
│   │   ├── Barrier_1
│   │   ├── Barrier_2
│   │   └── Barrier_3
│   ├── Carts/
│   │   ├── Cart_0  (Prefab Instance, laneIndex=0)
│   │   ├── Cart_1  (Prefab Instance, laneIndex=1)
│   │   ├── Cart_2  (Prefab Instance, laneIndex=2)
│   │   └── Cart_3  (Prefab Instance, laneIndex=3)
│   ├── EntryArea/                ← あみだくじ手前(Z=+3〜+7)
│   │   ├── Seats/
│   │   │   ├── Seat_0 ... Seat_3  (Prefab Instances, seatIndex=N)
│   │   ├── StartButton            ← 一意、Prefab化しない
│   │   ├── RulesPanel             ← 追いかけ式観戦の説明含む
│   │   └── ResultDisplay          ← レース結果掲示UI
│   └── PrizeAreas/                ← GoalBarrier の向こう(Z=-60〜-68)
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
非参加者はあみだくじ床面を歩いて観戦する([ADR-0009](./adr/0009-follow-alongside-spectator.md))。
歩行可能な床は単一の `MainFloor` のみで、縦線・横線は床上面に貼り付く視覚マーカー(高さ 2 cm の細い Cube)
([ADR-0011](./adr/0011-flat-horizontal-layout.md))。

**命名規約**:
- `_` プレフィックス: organizational root (折りたたみ用、構造上の整理)
- PascalCase + アンダースコア番号: `VLine_0`, `Seat_0`, `Cart_0`, `Barrier_0`
- 番号は 0-indexed(配列インデックスと揃える)
- Prefab Instance は変更を Override せず、必要な可変項目だけ Inspector で設定

---

## 2. Prefab分割

### 2.1 Prefab化するもの (再利用あり)

| Prefab | インスタンス数 | 可変項目 (Inspector) | 内容 |
|---|---|---|---|
| `VerticalLine.prefab` | 4 | なし(Transform位置のみ) | 縦線(細い Cube、高さ 2 cm × 幅 0.2 m × 長さ 60 m) |
| `HorizontalBar.prefab` | 最大33 | なし | 横線(細い Cube、高さ 2 cm × 幅 4.0 m × 長さ 0.2 m)、11 段 × 3 ペア |
| `GoalBarrier.prefab` | 4 | `laneIndex (0-3)` | ゴール手前壁(カート用隙間あり) |
| `Cart.prefab` | 4 | `laneIndex (0-3)`, `GameManager 参照` | カート本体 + VRC_Station + CartController |
| `Seat.prefab` | 4 | `seatIndex (0-3)`, `GameManager 参照` | 着座 Interact + 視覚マーカー |
| `PrizeArea.prefab` | 4 | `prizeIndex (0-3)` | ゴール後テレポート先の部屋(v1.0は同一見た目) |

### 2.2 Prefab化しないもの (一意・シーン固有)

| GameObject | 理由 |
|---|---|
| `MainFloor` | 一意。シーン全体で 1 枚の大型 Cube(18 × 0.2 × 80 m) |
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
- VRC_Station の `Player Mobility` = **Immobilize (For Vehicle)**(乗り物用、Mobile は着座中の WASD と競合するため不可)
- VRC_Station の `Disable Station Exit` = **false**([ADR-0007](./adr/0007-vrcstation-transform-cart.md) 2026-05-17 改訂。VR/Desktop 両プラットフォームで退出可 = リタイア扱いに統合)
- Desktop の Space キー / VR ジャンプボタンでの退出は `CartController.InputJump` イベントハンドラで `station.ExitStation()` を呼ぶ実装(Phase 2)
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
├── Visual/                       ← 着座位置を示すマーカー(灰色、ADR-0011 で Cart-only color 方針)
└── InteractTrigger (Collider, IsTrigger=true)
    └── SeatInteract.cs (UdonBehaviour、Interact() override)
```

- VRC_Interact コンポーネントは SDK 3.x には存在しない。UdonBehaviour + Collider のみでインタラクト可能
- 着座すると `GameManager.OnSeatClaimed(seatIndex)` を呼ぶ
- gameState != Idle なら無反応

### 3.4 HorizontalBar.prefab

```
Bar_LX_SXX (GameObject)
└── LineMesh (Primitive Cube, Mesh + Box Collider)
```

- Cube Scale: (4.0, 0.02, 0.2)、Position は配置時にシーンで設定
- Material: `M_Line`(白 `#FFFFFF`)
- Collider は残す(2 cm の段差として残る、ただし VRChat の Step Climb 内で歩行体験ゼロ)
- `AmidakujiGenerator` から `SetActive(true/false)` で表示制御
- Static flag は立てない(動的 enable のため Static Batching 非対象)

### 3.5 VerticalLine.prefab

```
VLine_X (GameObject)
└── LineMesh (Primitive Cube, Mesh + Box Collider)
```

- Cube Scale: (0.2, 0.02, 60.0)、Position は配置時にシーンで設定
- Material: `M_Line`(白 `#FFFFFF`)
- Static flag を立てて Static Batching に乗せる(常時表示のため)
- 縦線は AmidakujiGenerator の制御対象外(常時 enable)

### 3.6 PrizeArea.prefab

```
Prize_X (GameObject, Root Rotation Y=180)
├── Walls/
│   ├── Wall_N (奥側、+Z 側)
│   ├── Wall_E, Wall_W (左右壁)
│   ├── Wall_S_Left, Wall_S_Right (手前側、隙間 1.5m 空ける)
├── Ceiling (天井)
├── TeleportTarget (Empty GameObject) ← TeleportTo の位置参照
└── (v1.1 で DecorationMount を追加可能)
```

- 部屋サイズ: 3.5 × 4 × 8 m(4m レーン間隔で隣と 0.5m 隙間を残す)
- 床は MainFloor が下まで続いているので Prefab 内に床なし
- Root Rotation Y=180 にすることで Wall_S_Left/Right の隙間が +Z(GoalBarrier 側)を向く
- 詳細は [phase1-prefab-checklist.md §7](./phase1-prefab-checklist.md) 参照

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
| Environment 装飾 | **All Static** |
| MainFloor(単一の床 Cube) | **All Static** |
| VerticalLine Prefab(縦線) | **All Static**(常時表示) |
| HorizontalBar Prefab(横線) | **None** (動的 enable のため) |
| GoalBarrier | **All Static**(配置は固定) |
| Cart | **None** (動く) |
| Seat | Visual部分のみ Static、InteractTrigger は None |
| StartButton | None (押下時にビジュアル変化) |
| PrizeArea | **All Static** |
| Managers | None |

Light Probe Group はプレイヤーが歩く床面範囲(X=-9〜+9, Z=-68〜+12)に格子状に配置。

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
- public CartController targetCart // 紐づくカートへの参照(着座すると Cart の Station へ転送)
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
                                       // [lanePair * SEGMENT_COUNT + segment] でアクセス
                                       // (lanePair = 0..2、3 ペア × 11 段 = 33 個)
- public int LANE_COUNT = 4           // 縦線本数
- public int LANE_PAIR_COUNT = 3      // 横線が架けられる隣接ペア数 (= LANE_COUNT - 1)
- public int SEGMENT_COUNT = 11       // 横線位置の段数 (Z=-3..-53, 5m 間隔)
```

---

## 7. 配置数値の指針 (Phase 1 で参照)

座標系: Unity 標準。Y=0 が床上面、Y 軸が高さ、X が左右、**Z が EntryArea(正)〜 PrizeArea(負)方向**。

### MainFloor(全プレイヤーが歩く床)

- 単一の Primitive Cube
- Scale: **(16, 0.2, 80)** (X 幅 16 m、厚み 0.2 m、Z 奥行 80 m。GoalBarrier 4 連の全幅 16 m と整合)
- Position: **(0, -0.1, -28)** → 上面 Y=0、X 範囲 **-8〜+8**、Z 範囲 -68〜+12
- Material: `M_Floor_Common`、Static フラグ All Static

### あみだくじ本体(縦線・横線)

- 縦線本数: **4** ([ADR-0008](./adr/0008-4lane-scope-scalable-design.md))
- 縦線間隔: **4.0 m** (X方向)、X = **-6, -2, +2, +6**
- 縦線長さ: **60.0 m** (Z方向、EntryArea 側 Z=+2 → GoalBarrier 側 Z=-58)
- 横線間隔: **5.0 m**(縦線 60 m を 12 区間に分割。内部境界の 11 箇所が横線位置 = `SEGMENT_COUNT`)
- 縦線中心の Z 座標: **-28**(線の Z 範囲 +2 〜 -58 の中点)

### 縦線(VerticalLine.prefab)

- 全 4 本、Cube Scale **(0.2, 0.02, 60.0)**
- Position: 各 (X=-6/-2/+2/+6, **Y=0.01**, Z=-28)
- Material: `M_Line`(白 `#FFFFFF`)
- 常時表示、Static All Static

### 横線(HorizontalBar.prefab)

- Cube Scale **(4.0, 0.02, 0.2)**
- 横線位置の Z 座標: **-3, -8, -13, -18, -23, -28, -33, -38, -43, -48, -53** (= -3 - 5×S, S=0..10、計 **11 段**)
- 1 段に 3 本(Lane 0-1, 1-2, 2-3 を結ぶ)、計 11 × 3 = **33 本** 事前配置
- Lane ペア L=0,1,2 の中心 X: **-4, 0, +4**
- 命名: `Bar_L{L}_S{S:00}`(例: `Bar_L0_S00` は Lane0-Lane1 間、Z=-3、`Bar_L2_S10` は Lane2-Lane3 間、Z=-53)
- Material: `M_Line`、動的 enable/disable のため Static フラグなし
- ゴール手前の run-out zone: 最終横線 Z=-53 から GoalBarrier Z=-58.5 まで 5.5 m。あみだくじの確定演出スペース

### ゴール手前バリア(GoalBarrier.prefab)

- 配置 Z: **-58.5**(縦線下端 Z=-58 の 0.5 m 先)
- 配置 Y: **0**(壁の下端 Y=0、上端 Y=+2.0)
- 配置 X: 各 Lane 中央(X = -6, -2, +2, +6)
- 隙間幅: **1.5 m**(X 方向、カート幅 0.9 m + 両側 0.3 m)
- 隙間高さ: **0.5 m**(Y=0 〜 Y=+0.5、歩行者がしゃがんでも通れない)
- 壁の厚み(Z 方向): 0.2 m
- 隙間中心位置: 各レーンの中央(X = -6, -2, +2, +6 / Y=+0.25 が隙間の中心)
- Phase 1 終了時に小柄アバター + 匍匐姿勢で侵入できないことを VR HMD で確認

### エントリーエリア(床上の領域)

- MainFloor の Z=+3〜+7 範囲を EntryArea として利用(物理的な追加床なし、MainFloor 上)
- Seat 配置: (X = -6, -2, +2, +6 / **Y=0** / Z=+5) — Seat Root の床貼り付き(子 Visual が Y=0.05 で着座マーカー高 0.1 m)
- StartButton: (X=0, Y=1.2, Z=+7) — 床に立つボタン
- ResultDisplay: 詳細は Phase 5 で確定、StartButton 付近を想定

### スポーン位置

平面水平レイアウトのため、スポーンデッキ + 接続橋は不要(段差ゼロのため、別レベルの床を作る必要がない、[ADR-0011](./adr/0011-flat-horizontal-layout.md))。

- DefaultSpawn: **(0, 0.1, +10)**、Rotation **Y=180°**(2026-05-17 修正)— MainFloor の EntryArea(Z=+5)より +Z 側に配置し、180° 回転で -Z 方向(EntryArea / Seat / Goal 側)を向く。プレイヤーの視線正面に Seat 群と縦線群が入る
- RulesPanel: (0, 2, +12) に高さ 2 m × 幅 4 m パネル(DefaultSpawn の背後)。Phase 5 で TextMeshPro 化、Phase 1 は灰色立て看板

### 賞品エリア(PrizeArea.prefab)

- GoalBarrier(Z=-58.5)の先 1.5 m バッファ後に 4 部屋並べる(部屋手前壁 Z=-60、奥壁 Z=-68)
- 各 PrizeArea 中心: (X = -6, -2, +2, +6 / **Y=0** / **Z=-64**)
- 部屋サイズ: **3.5 m × 4 m × 8 m**(X 幅 × Y 高さ × Z 奥行、4 m レーン間隔で隣と 0.5 m 隙間)
- Prefab Root の Rotation Y=180°(Wall_S_Left/Right の隙間が +Z = GoalBarrier 側を向くため)
- 床は MainFloor が下まで届いているのでそのまま流用、壁・天井のみを各部屋で構築
- 各 PrizeArea の TeleportTarget: 部屋中心の床上 0.1 m(Y=0.1)
- 隣接する PrizeArea 同士は 0.5 m の物理ギャップと壁で仕切る(他のレーンの賞品エリアが見えないように)

---

## 8. ワールド俯瞰イメージ (TOP-DOWN VIEW、Y=0 平面)

```
                       +Z(EntryArea 側、奥)
                        ↑
       ┌────────────────────────────────────────────────┐
       │                                                 │
       │                  ● DefaultSpawn (0, 0.1, +10)   │ Z=+10
       │              ┌─ RulesPanel ─┐                   │ Z=+12 付近
       │                                                 │
       │              ┌─ StartButton ─┐                  │ Z=+7
       │                                                 │
       │              S0    S1    S2    S3               │ Z=+5 (Seat 列)
       │              X=-6  X=-2  X=+2  X=+6             │
       │              │     │     │     │                │
       │              ▼     ▼     ▼     ▼                │
       │  ●Cart_0  ●Cart_1 ●Cart_2 ●Cart_3              │ Z=+2 (Start)
       │              │     │     │     │                │
       │            VLine VLine VLine VLine               │
       │              │     │     │     │                │
       │              │     ├ Bar ┤     │  (例)           │ Z=-3 (Seg 0)
       │              │     │     │     │                │
       │              ├ Bar ┤     │     │  (例)           │ Z=-8 (Seg 1)
       │              │     │     │     │                │
       │              │     │     │     │                │ ... (Seg 2-10)
       │              │     │     │     │                │
       │              │     │     ├ Bar ┤  (例)           │ Z=-53 (S10, 最終)
       │              │     │     │     │                │ Z=-53〜-58.5: run-out zone 5.5m
  -X ──┤            ═══════════════════════              │ Z=-58.5 GoalBarrier
       │              ║     ║     ║     ║                │ (隙間 1.5×0.5 m)
       │              ║     ║     ║     ║                │
       │            ┌─P0─┐┌─P1─┐┌─P2─┐┌─P3─┐             │ Z=-60 〜 -68
       │            │    ││    ││    ││    │             │ (賞品エリア 4 部屋)
       │            └────┘└────┘└────┘└────┘             │
       │                                                 │
       └────────────────────────────────────────────────┘
                       ↓
                       -Z(PrizeArea 側、手前)

  全プレイヤーは Y=0 床面を歩行。縦線・横線は床上面 Y=0.01 の高さで貼り付き、
  歩行体験はフラット。カートはこの平面を Z+ から Z- に向かって走る。
  観戦者は床上を自由に走り回ってカートを追いかける。
```

---

## 9. マテリアル定義

マテリアル一覧・採用シェーダー(`VRChat/Mobile/Standard Lite` および TMP `Mobile/Distance Field`)・
レーン色定義・テクスチャ仕様・GPU Instancing 設定は別ファイルに分離した。

→ [docs/material-set.md](./material-set.md) を参照。

Phase 1 着手時点で **計 12 マテリアル**(バジェット 20 に対し +8 のヘッドルーム)で組み上げる方針。

---

## 10. Phase 1 着手時のチェックリスト

各 Prefab の **Inspector 値・配置座標の確定リスト** は別ファイルに分離:
→ [docs/phase1-prefab-checklist.md](./phase1-prefab-checklist.md)。

Phase 1 のシーン組み立て時、以下の順序を推奨:

1. `_World`、`_Managers`、`_Lighting` の organizational root を作成
2. `VRCWorld` + VRC Scene Descriptor + DefaultSpawn (Z=+10) を最初に置く(これがないと Build できない)、Respawn Height Y=-1
3. **Tags and Layers で User22 (Cart)、User23 (GoalBarrier) を追加**(Opus が ProjectSettings 編集済み、Unity GUI で目視確認)
4. **Physics 設定でコリジョンMatrix(§4.2)を確認**(同上)
5. 試しに空ワールドでアップロード疎通 (Phase 0 のおさらい)
6. `MainFloor` を Primitive Cube で 1 枚配置(Scale 18×0.2×80、Position 0,-0.1,-28)
7. `VerticalLine.prefab` を Primitive Cube で作成、VLine_0〜3 を配置してスケール感確認
8. `HorizontalBar.prefab` を Primitive Cube で作成、1段分(3本)を仮配置 → OKなら全33本(11 段 × 3 ペア)展開
9. `GoalBarrier.prefab` を作成、4個配置して隙間サイズ(1.5×0.5 m)を VR HMD で実機確認(しゃがんで通れないか、カート想定 0.9 m で通れるか)
10. `Cart.prefab`、`Seat.prefab` はダミーキューブで Prefab 化 → Phase 2 で見た目を整える
11. EntryArea(Seat 4 つ + StartButton 仮)、PrizeAreas 4 部屋を順番に大枠だけ配置
12. ライティングは仮 (Skybox + Directional Light) のまま、Phase 9 でベイク

**重要**: 全マテリアルを `VRChat/Mobile/Standard Lite` 系で作成し、テクスチャは 1024×1024 以下に。Android対応のため、Phase 1 から制約を守ったほうが Phase 7 での手戻りが少ない(Phase 1 はテクスチャ無し・色のみのプレースホルダで構わない。詳細は [material-set.md](./material-set.md))。

完了基準は SPEC.md / tasklist.md の Phase 1 セクション参照。
