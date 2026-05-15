# Scene Structure & Prefab Split

Phase 1着手時の Unity シーン構成、Prefab分割、レイヤー・命名規約の指針をまとめる。

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
│   ├── AmidakujiTrack/           ← あみだくじ本体
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
│   │   └── GoalMarkers/
│   │       └── Goal_0 ... Goal_3
│   ├── Carts/
│   │   ├── Cart_0  (Prefab Instance, laneIndex=0)
│   │   ├── Cart_1  (Prefab Instance, laneIndex=1)
│   │   ├── Cart_2  (Prefab Instance, laneIndex=2)
│   │   └── Cart_3  (Prefab Instance, laneIndex=3)
│   ├── EntryArea/
│   │   ├── Floor/
│   │   ├── Seats/
│   │   │   ├── Seat_0 ... Seat_3  (Prefab Instances, seatIndex=N)
│   │   ├── StartButton            ← 一意、Prefab化しない
│   │   ├── RulesPanel             ← TextMesh Pro
│   │   └── Screen/                ← 観戦スクリーン (エントリー側)
│   │       ├── ScreenQuad
│   │       └── ScreenFrame (装飾)
│   ├── SpectatorArea/
│   │   ├── ObservationDeck/
│   │   │   ├── GlassFloor
│   │   │   ├── Railings
│   │   │   └── Stairs
│   │   └── (Screen は EntryArea と兼用、もう1枚必要ならここに配置)
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
├── _Cameras/
│   └── OverviewCamera            ← Render Texture 用カメラ
│
├── _Lighting/
│   ├── DirectionalLight
│   ├── LightProbeGroup
│   └── ReflectionProbe (1-2個)
│
├── _PostProcessing/  (任意)
│   └── PostProcessVolume
│
└── VRCWorld                      ← VRChat必須オブジェクト
```

**命名規約**:
- `_` プレフィックス: organizational root (折りたたみ用、構造上の整理)
- PascalCase + アンダースコア番号: `Lane_0`, `Seat_0`, `Cart_0`
- 番号は 0-indexed(配列インデックスと揃える)
- Prefab Instance は変更を Override せず、必要な可変項目だけ Inspector で設定

---

## 2. Prefab分割

### 2.1 Prefab化するもの (再利用あり)

| Prefab | インスタンス数 | 可変項目 (Inspector) | 内容 |
|---|---|---|---|
| `Lane.prefab` | 4 | なし(Transform位置のみ) | 縦通路の見た目(柱+床) |
| `HorizontalBar.prefab` | 最大36 | なし | 横線の見た目(連絡通路) |
| `Cart.prefab` | 4 | `laneIndex (0-3)`, `GameManager 参照` | カート本体 + VRC_Station + CartController |
| `Seat.prefab` | 4 | `seatIndex (0-3)`, `GameManager 参照` | 着座 Interact + 視覚マーカー |
| `PrizeArea.prefab` | 4 | `prizeIndex (0-3)` | ゴール後テレポート先の部屋(v1.0は同一見た目) |

### 2.2 Prefab化しないもの (一意・シーン固有)

| GameObject | 理由 |
|---|---|
| `GameManager` | シングルトン。Synced 変数のオーナー |
| `AmidakujiGenerator` | シングルトン |
| `StartButton` | 一意、Inspector で GameManager 参照を直接バインド |
| `OverviewCamera` | 一意、構図がシーン固有 |
| `RulesPanel` | テキスト内容がシーン固有 |
| `Environment/Decoration` | シーン固有装飾 |
| `ObservationDeck` | 形状が一品ものになる(階段や床配置がワールド依存) |

### 2.3 Prefab Variant の活用判断

v1.0 では Prefab Variant は使わない。理由:
- 4インスタンスしかないので Override コストが低い
- v1.1 で 20レーン化する際に Variant 化しても遅くない

---

## 3. Prefab 内部構造 (主要なもの)

### 3.1 Cart.prefab

```
Cart_X (GameObject, Transform 位置 = 起点)
├── Visual/                       ← 見た目モデル
│   ├── Body (Mesh Renderer)
│   └── Wheels (任意装飾)
├── Seat (GameObject)
│   └── VRC_Station               ← Component
└── (CartController を Root に Add)
```

- Root に `CartController.cs` (UdonBehaviour)
- VRC_Station の `Player Mobility` = Mobile
- Station 設定は Phase 2 で実機確認しながら微調整

### 3.2 Seat.prefab

```
Seat_X (GameObject)
├── Visual/                       ← 着座位置を示すマーカー(ピンスポット等)
└── InteractTrigger (Collider, IsTrigger=true)
    └── VRC_Interact + SeatInteract.cs (UdonBehaviour)
```

- 着座すると `GameManager.OnSeatClaimed(seatIndex)` を呼ぶ
- gameState != Idle なら無反応

### 3.3 HorizontalBar.prefab

```
Bar_LX_SXX (GameObject)
└── Mesh Renderer (床+手すり風の橋)
```

- Component は Mesh / Renderer のみ。Udon スクリプトは付けない
- `AmidakujiGenerator` から `SetActive(true/false)` で表示制御
- Static flag は立てない(動的 enable のため Static Batching 非対象)

### 3.4 Lane.prefab

```
Lane_X (GameObject)
└── Mesh Renderer (柱+床)
```

- Static flag を立てて Static Batching に乗せる
- Collider は床面のみ(プレイヤーが通常時に立ち入れるよう)

### 3.5 PrizeArea.prefab

```
Prize_X (GameObject)
├── Floor (Mesh)
├── Walls (Mesh)
├── TeleportTarget (Empty GameObject) ← TeleportTo の位置参照
└── DecorationMount (Empty)          ← v1.1で装飾を入れる場所
```

---

## 4. Layer / Tag 設定

VRChat World で使えるカスタムレイヤーは限られる(User Layer 22-31)。最小限の追加で運用する。

| Layer | 用途 | 設定先 |
|---|---|---|
| `Default` (0) | 通常オブジェクト | 大半 |
| `Player` (9) | VRChat予約 | (システム) |
| `PlayerLocal` (10) | VRChat予約 | (システム) |
| `MirrorReflection` (18) | VRChat予約 | (システム) |
| **User22: OverviewCameraOnly** | 俯瞰カメラ専用に映る要素(カート上マーカー等) | カート上の名前ラベル等(v1.1) |
| **User23: ExcludeFromOverview** | 俯瞰カメラから除外するもの | 観戦エリアUI、Rules Panel |

v1.0 ではほぼ `Default` で運用、`ExcludeFromOverview` を1枚足すだけで十分。OverviewCamera 側で Culling Mask から `Player`、`PlayerLocal`、`ExcludeFromOverview` を外す。

**Tags**:
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
| Cart | **None** (動く) |
| Seat | Visual部分のみ Static、InteractTrigger は None |
| StartButton | None (押下時にビジュアル変化) |
| PrizeArea | **All Static** |
| ObservationDeck | **All Static** |
| OverviewCamera | None |
| Managers | None |

Light Probe Group は ObservationDeck と PrizeAreas の周辺に密に配置。

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

### 横線

- 各セグメント境界の Y: -5, -10, ..., -55 (= -5 × (seg+1))
- 1セグメント境界に 3 ペア (Lane 0-1, 1-2, 2-3)
- 計 12 × 3 = **36個** の HorizontalBar を事前配置
- 横線の太さ・幅: 通路として違和感ない 1.5m 幅程度

### エントリーエリア

- 縦線上端より少し上(Y=+3 程度)、X方向はあみだくじ中央に合わせる
- 床サイズ: 16m × 8m
- Seat 配置: 縦線上端の真上 (X = -6, -2, +2, +6 / Y = +3 / Z = -2)
- StartButton: 中央前面 (X=0, Y=+4.5, Z=+3)
- Screen サイズ: 8m × 4.5m (16:9)、X=0, Y=+6, Z=+5

### 観戦デッキ

- ObservationDeck Floor の Y: あみだくじ中段、**Y = -20** あたり
- ガラス床は X方向であみだくじ全幅を覆う (X: -10 〜 +10)
- 奥行き: 6m 程度
- 手すり高さ: 1.2m

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
            │   ├────────┼────┼────┼────┤      │
            │   │              StartBtn         │
            │   │           [Screen]            │
            │   └─┬────┬─┬────┬─┬────┬─┬────┬─┘
            │     │    │ │    │ │    │ │    │      Y = 0 〜 -60
   -X ──────┤     │L0  │ │L1  │ │L2  │ │L3  │     (あみだくじ本体)
            │     │    │ │    │ │    │ │    │
            │     │    │ │    │ │    │ │    │
            │     │    │ │    │ │    │ │    │
            │     └────┘ └────┘ └────┘ └────┘
            │       │      │      │      │
            │     ┌────┐ ┌────┐ ┌────┐ ┌────┐
            │     │ P0 │ │ P1 │ │ P2 │ │ P3 │      Y = -64〜-60
            │     │    │ │    │ │    │ │    │     (賞品エリア)
            │     └────┘ └────┘ └────┘ └────┘
            └──────────────────────────────────┘

  + ObservationDeck はあみだくじの中段 (Y = -20) 周辺に
    横から張り出す形で配置(この図では省略)
```

---

## 9. Phase 1 着手時のチェックリスト

Phase 1 のシーン組み立て時、以下の順序を推奨:

1. `_World`、`_Managers`、`_Cameras`、`_Lighting` の organizational root を作成
2. `VRCWorld` + VRC Scene Descriptor + DefaultSpawn を最初に置く(これがないと Build できない)
3. 試しに空ワールドでアップロード疎通 (Phase 0 のおさらい)
4. Lane.prefab を1つ作成、Lane_0〜3 を配置してスケール感確認
5. HorizontalBar.prefab を作成、1セグメント分(3本)を仮配置して見栄え確認 → OKなら全36本展開
6. Cart.prefab、Seat.prefab はメッシュ未確定でもダミーキューブで Prefab 化 → Phase 2 で見た目を整える
7. EntryArea、SpectatorArea、PrizeAreas を順番に大枠だけ配置
8. ライティングは仮 (Skybox + Directional Light) のまま、Phase 9 でベイク

完了基準は SPEC.md / tasklist.md の Phase 1 セクション参照。
