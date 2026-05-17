# Phase 1 Prefab 構築チェックリスト

Phase 1 で Unity GUI 上で組み立てる Prefab・シーンオブジェクトの確定値リスト
(平面水平レイアウト = [ADR-0011](./adr/0011-flat-horizontal-layout.md) 準拠)。
配置寸法は [scene-structure.md §7](./scene-structure.md#7-配置数値の指針-phase-1-で参照)、
マテリアルは [material-set.md](./material-set.md)、レイヤー設計は
[scene-structure.md §4](./scene-structure.md#4-layer--tag-設定) を参照しつつ、ここでは
**「ユーザーが Unity 操作する手順」と「Inspector に入れる値」**を一箇所に集めている。

前提として `ProjectSettings/TagManager.asset` と `ProjectSettings/DynamicsManager.asset`
は Opus が編集済み(User22=`Cart`, User23=`GoalBarrier` + 衝突 Matrix)。Unity を起動
したら `Edit > Project Settings > Tags and Layers` と `... > Physics` の Layer Collision
Matrix を**目視確認**してから本チェックリストに進むこと。

---

## 0. 共通ルール

- 全 Prefab は `Assets/_Project/Prefabs/` 配下に保存
- メッシュは **Unity 標準 Primitive Cube を Scale で伸ばす方式**(ProBuilder 不要、[ADR-0011](./adr/0011-flat-horizontal-layout.md))
- 単位: 全てメートル (m)、Unity 既定スケール
- 座標系: **Y=0 が床上面**、X が左右、**Z が EntryArea(正)〜 PrizeArea(負)方向**
- Static フラグ運用は [scene-structure.md §5](./scene-structure.md#5-static-フラグ運用)

## 1. MainFloor(シーン直配置、Prefab 化なし)

全プレイヤーが歩く 1 枚の大型床。

| 項目 | 値 |
|---|---|
| Mesh | Primitive Cube |
| Scale | **(16, 0.2, 80)** |
| Position | (0, -0.1, -28) → 上面 Y=0、X 範囲 -8〜+8、Z 範囲 -68〜+12 |
| Layer | `Default` |
| Material | `M_Floor_Common`(灰色 `#888888`) |
| Static フラグ | All Static |
| Collider | Box Collider(Primitive Cube が自動付与、IsTrigger=false) |

シーン Hierarchy: `_World > Ground > MainFloor`

---

## 2. VerticalLine.prefab(縦線)

### 2.1 Prefab 仕様

```
VLine_X (Root, Layer: Default)
└── (Primitive Cube が Root 自身を兼ねる)
```

| 項目 | 値 |
|---|---|
| Mesh | Primitive Cube |
| Scale | (0.2, 0.02, 60.0) |
| Material | `M_Line`(白 `#FFFFFF`) |
| Static フラグ | All Static |
| Collider | 残す(2 cm の段差は Step Climb 内で歩行体験ゼロ) |

### 2.2 シーン配置(VLine_0〜3)

シーン Hierarchy: `_World > AmidakujiLines > VerticalLines/`

| Instance | Position (X, Y, Z) |
|---|---|
| `VLine_0` | (-6, 0.01, -28) |
| `VLine_1` | (-2, 0.01, -28) |
| `VLine_2` | ( 2, 0.01, -28) |
| `VLine_3` | ( 6, 0.01, -28) |

Y=0.01 は線の中心高さ(床上面 Y=0 から 1 cm 浮かせ、線の上面が Y=0.02)。

---

## 3. HorizontalBar.prefab(横線)

### 3.1 Prefab 仕様

```
Bar_LX_SXX (Root, Layer: Default)
└── (Primitive Cube が Root 自身を兼ねる)
```

| 項目 | 値 |
|---|---|
| Mesh | Primitive Cube |
| Scale | (4.0, 0.02, 0.2) |
| Material | `M_Line`(白) |
| Static フラグ | **None**(動的 enable のため) |
| Collider | 残す(同上、Step Climb 内) |

### 3.2 シーン配置(33 個、Bar_L{L}_S{SS})

シーン Hierarchy: `_World > AmidakujiLines > HorizontalBars/`

レーン ペア L = 0, 1, 2(L=0 は VLine_0-VLine_1 間、L=1 は 1-2 間、L=2 は 2-3 間)、横線位置 S = 0..**10**(**11 段**)。

- Position X: `-4 + L*4`(L=0: -4, L=1: 0, L=2: +4)
- Position Y: **0.01**
- Position Z: `-3 - 5*S`(S=0: -3, S=1: -8, ..., S=10: -53)
- 命名: `Bar_L{L}_S{S:00}`(例: `Bar_L0_S00` = Lane0-Lane1 間, 位置 0 = Z=-3、`Bar_L2_S10` = Lane2-Lane3 間, 位置 10 = Z=-53)

> Phase 1 では全パターン配置 → 全部 `SetActive(true)` のままで OK。動的 enable/disable は Phase 3 で `AmidakujiGenerator` から制御。
>
> 最終 S10(Z=-53)から GoalBarrier(Z=-58.5)までの 5.5m は **run-out zone**(横線無し、あみだくじの確定演出スペース)として確保。

---

## 4. GoalBarrier.prefab(ゴール手前壁)

カート用隙間: 幅 1.5 m × 高さ 0.5 m。壁本体の総幅は X 方向 3.0 m(両側 0.75 m の壁 + 中央 1.5 m の隙間)。

### 4.1 Prefab 内部構造

Prefab ローカル原点を **「隙間の床面中心」** に取る(配置 Y=0 でワールド Y=0 = 床上面と一致)。

```
Barrier_X (Root, Layer: GoalBarrier)
├── WallLeft (Primitive Cube)
├── WallRight (Primitive Cube)
└── Ceiling (Primitive Cube)
```

| パート | Scale (X, Y, Z) | Position(ローカル) |
|---|---|---|
| `WallLeft` | **(1.25, 2.0, 0.2)** | **(-1.375, +1.0, 0)** |
| `WallRight` | **(1.25, 2.0, 0.2)** | **(+1.375, +1.0, 0)** |
| `Ceiling` | (1.5, 1.5, 0.2) | (0, +1.25, 0) |

- Barrier 1 個の総幅: **4 m**(レーン間隔と同じ、隣のバリアと隙間なく接続して MainFloor 全幅 16 m を 4 連で密に塞ぐ)
- Ceiling の下端 Y=+0.5 → 隙間の高さ 0.5 m を担保
- 全パーツ Collider = Box Collider、IsTrigger=false
- Material: 全パーツ `M_Barrier`(警告色 `#FFCC00`)
- Static フラグ: All Static

### 4.2 コンポーネント

| GameObject | Layer | 追加コンポーネント |
|---|---|---|
| `Barrier_X` (Root) | `GoalBarrier` (User23) | (Inspector) `public int laneIndex` 0..3 — Phase 1 はダミー値で OK、Phase 4 で本実装 |
| 子3パーツ | `GoalBarrier`(親から継承、Apply Children で Yes) | なし |

### 4.3 シーン配置(Barrier_0〜3)

シーン Hierarchy: `_World > GoalBarriers/`

| Instance | Position (X, Y, Z) | laneIndex |
|---|---|---|
| `Barrier_0` | (-6, 0, -58.5) | 0 |
| `Barrier_1` | (-2, 0, -58.5) | 1 |
| `Barrier_2` | ( 2, 0, -58.5) | 2 |
| `Barrier_3` | ( 6, 0, -58.5) | 3 |

> Phase 1 完了時に **VR HMD + 小柄アバター + 匍匐姿勢で通れないこと** を実機確認(本仕様の唯一の物理テスト)。

---

## 5. Cart.prefab(カート本体、ダミー版)

Phase 1 ではメッシュ未確定のため Primitive Cube で Prefab 化。実走行ロジックは Phase 2、ビジュアル整形は Phase 2 後半。

### 5.1 Prefab 内部構造

```
Cart_X (Root, Layer: Cart)
├── Visual/
│   └── Body (Primitive Cube)                              ← Material: M_LaneColor_N
└── Seat (Empty GameObject, Layer: Cart)
    └── VRC_Station (Component)
```

| 項目 | 値 |
|---|---|
| Body Mesh | Primitive Cube |
| Body Scale | (0.9, 0.9, 1.4)(幅 × 高さ × 奥行) |
| Body Position(ローカル) | (0, 0.45, 0)(床上面 Y=0 → Body 下面 Y=0、上面 Y=0.9) |
| Body Collider | **削除**(物理駆動ではなく Transform 駆動、Layer 分離で歩行者衝突を制御) |
| Seat Position(ローカル) | (0, 0.9, 0) |
| VRC_Station 設定 | **`Disable Station Exit=false`** ([ADR-0007](./adr/0007-vrcstation-transform-cart.md) 2026-05-17 改訂、Desktop は移動入力(WASD/スティック)・VR はトリガー(Use)で退出可=リタイア扱い), **`Player Mobility=Immobilize (For Vehicle)`**(または `Immobilize All`、`Mobile` は不可), `Player Enter Location=Seat Transform`, `Player Exit Location=Seat Transform` |

### 5.2 CartController.cs(Phase 2 で本実装、Phase 1 は空アタッチ)

Phase 2 暫定と Phase 3 以降で Inspector フィールドが切り替わる(設計詳細は [scene-structure.md §6](./scene-structure.md))。スタート位置・ゴール位置は Cart_N / GoalBarrier_N の Transform を直接参照する設計のため、`startMarker / goalMarker / prizeTeleport` フィールドは作らない(commit `00511e8` で StartMarkers/GoalMarkers Empty 配置自体を廃止済み)。

| Inspector フィールド | 型 | Phase | Phase 1 時点 |
|---|---|---|---|
| `laneIndex` | int | Common | 0..3(Cart_N の N) |
| `speed` | float | Common | 2.0 |
| `station` | VRC_Station | Common | 自身の子 Seat の Station |
| `startOnEnter` | bool | Phase 2 暫定 | true |
| `lookAtMovingDirection` | bool | Phase 2 暫定 | false |
| `waypointMarkers` | Transform[] | Phase 2 暫定 | (Phase 2 着手時に Empty Marker を 4-5 点配置) |
| `gameManager` | GameManager | Phase 3 以降 | (Phase 3 で追加) |
| `generator` | AmidakujiGenerator | Phase 3 以降 | (Phase 3 で追加) |

### 5.3 シーン配置(Cart_0〜3)

シーン Hierarchy: `_World > Carts/`

| Instance | Position (X, Y, Z) | Body Material |
|---|---|---|
| `Cart_0` | (-6, 0, +2) | `M_LaneColor_0`(赤) |
| `Cart_1` | (-2, 0, +2) | `M_LaneColor_1`(黄) |
| `Cart_2` | ( 2, 0, +2) | `M_LaneColor_2`(緑) |
| `Cart_3` | ( 6, 0, +2) | `M_LaneColor_3`(青) |

Position Y=0 は床面、Body 下面が床に接する状態(Body Position(ローカル) Y=0.45 で Body 中心が床から 0.45 m 浮く)。
Z=+2 は縦線の上端(Z=+2 が縦線スタート位置)。

---

## 6. Seat.prefab(着座 Interact、ダミー版)

エントリーエリアの座席。Phase 1 はマーカー + Interact を用意、Phase 2 で着座 → カート転送ロジックを実装。

### 6.1 Prefab 内部構造

```
Seat_X (Root, Layer: Default)
├── Visual (Primitive Cube)                                ← Material: M_Wall_Generic(灰)
└── InteractTrigger (Empty GameObject)
    └── Box Collider (IsTrigger=true, Size 0.5×0.5×0.3, Center (0,0,0))
```

- Visual: Cube Scale (0.4, 0.1, 0.4)、Position(ローカル) (0, 0.05, 0)
- Material はレーン色をやめてグレー統一([ADR-0011](./adr/0011-flat-horizontal-layout.md) Cart-only color 方針)
- **VRC_Interact コンポーネントは SDK 3.x には存在しない**(SDK 2 時代の遺物)。SDK 3.x では Interact() を override した UdonBehaviour に Collider を併設するだけでインタラクト可能になる
- `SeatInteract.cs`(UdonSharp)は Phase 5 で実装し、その時 InteractTrigger にアタッチする

### 6.2 シーン配置(Seat_0〜3)

シーン Hierarchy: `_World > EntryArea > Seats/`

| Instance | Position (X, Y, Z) | seatIndex |
|---|---|---|
| `Seat_0` | (-6, 0, +5) | 0 |
| `Seat_1` | (-2, 0, +5) | 1 |
| `Seat_2` | ( 2, 0, +5) | 2 |
| `Seat_3` | ( 6, 0, +5) | 3 |

(MainFloor 上、Cart の X と一致、Z=+5 で EntryArea 中央)

---

## 7. PrizeArea.prefab(賞品エリア)

GoalBarrier の先(Z=-60 〜 Z=-68)に 4 部屋。床は MainFloor が下まで届いているため、各 PrizeArea は **壁 4 枚 + 天井** のみで構築。

### 7.1 Prefab 内部構造

Prefab ローカル原点 = 部屋中心の床上面(Y=0)。部屋サイズ **X=3.5 m × Y=4 m × Z=8 m**(壁の内側寸法、4m レーン間隔で互いに 0.5 m の隙間を残す配置)。

**Prefab Root の Rotation Y = 180** に設定する。これにより、Wall_S_Left/Right の隙間が +Z 方向(GoalBarrier 側)を向く。

```
Prize_X (Root, Layer: Default, Rotation Y=180)
├── Walls/
│   ├── Wall_N (Primitive Cube, Scale 3.5 × 4 × 0.2, Position (0, 2, +4))   ← Root から見て +Z 側
│   ├── Wall_E (Scale 0.2 × 4 × 8, Position (+1.75, 2, 0))                   ← 右側
│   ├── Wall_W (Scale 0.2 × 4 × 8, Position (-1.75, 2, 0))                   ← 左側
│   ├── Wall_S_Left (Scale 1 × 4 × 0.2, Position (-1.25, 2, -4))             ← 隙間の左側
│   └── Wall_S_Right (Scale 1 × 4 × 0.2, Position (+1.25, 2, -4))            ← 隙間の右側
├── Ceiling (Primitive Cube, Scale 3.5 × 0.2 × 8, Position (0, 4.1, 0), Collider 削除可)
└── TeleportTarget (Empty GameObject, Position (0, 0.1, 0))                  ← TeleportTo 参照点
```

- 全 Wall: Material `M_Wall_Generic`、Static All Static
- Ceiling: Material `M_Wall_Generic`、Static All Static
- 手前側(Root ローカル -Z 側)の Wall_S は **2 ピース構成**(Wall_S_Left + Wall_S_Right)で中央に 1.5 m 幅のカート通過用隙間を空ける
- 隙間中心 X=0、X 範囲 -0.75〜+0.75。世界座標では Root Rotation Y=180 を経由するため、実際の隙間は **+Z 方向(GoalBarrier 側)** に開口する
- 隣接する PrizeArea とは 0.5 m の物理ギャップ(センター 4m 間隔 - 部屋幅 3.5m = 0.5m)

### 7.2 シーン配置(Prize_0〜3)

シーン Hierarchy: `_World > PrizeAreas/`

| Instance | Position (X, Y, Z) | prizeIndex |
|---|---|---|
| `Prize_0` | (-6, 0, -64) | 0 |
| `Prize_1` | (-2, 0, -64) | 1 |
| `Prize_2` | ( 2, 0, -64) | 2 |
| `Prize_3` | ( 6, 0, -64) | 3 |

(部屋中心 Z=-64、GoalBarrier(Z=-58.5)から壁手前 Z=-60 まで 1.5 m のバッファ、部屋自体は Z=-60〜-68)

---

## 8. EntryArea / DefaultSpawn / StartButton(Prefab 化しない、シーン直配置)

平面レイアウトでは EntryArea も MainFloor 上に直接配置する。スポーンデッキ + 接続橋は不要([ADR-0011](./adr/0011-flat-horizontal-layout.md))。

### 8.1 EntryArea(物理床なし、論理エリア)

```
EntryArea (Empty GameObject, Position (0, 0, +5))
├── Seats (上記 Seat_0〜3 の親)
├── StartButton (後述)
├── RulesPanel (Phase 5 で本実装、Phase 1 はダミー Cube)
└── ResultDisplay (Phase 5 で本実装、Phase 1 はダミー Cube)
```

### 8.2 DefaultSpawn

```
DefaultSpawn (Empty GameObject, Position (0, 0.1, +10))
```

VRC Scene Descriptor の `Spawns[0]` にこの Transform を指定。プレイヤーは Y=0.1 にスポーンし、視線正面に EntryArea / Seat 群が見える配置。

### 8.3 RulesPanel(Phase 1 はダミー)

```
RulesPanel (Empty GameObject, Position (0, 2, +12))
└── Visual (Primitive Cube, Scale 4 × 2 × 0.1, Material: M_Wall_Generic)
```

Phase 5 で TextMeshPro パネルに置換予定。Phase 1 は灰色立て看板。

### 8.4 StartButton(Phase 1 はダミー)

```
StartButton (Empty GameObject, World Position (0, 1.2, +7) = EntryArea ローカル (0, 1.2, +2))
├── Visual (Primitive Cube, Scale 0.5 × 0.5 × 0.2, Material: M_Button_Inactive [初期])
└── InteractTrigger (Empty + Box Collider IsTrigger=true, Size (0.5, 0.5, 0.3), Center (0,0,0))
```

- Phase 1 は Collider と Visual だけ。`StartButton.cs` (UdonBehaviour) は Phase 5 で実装、その時 InteractTrigger にアタッチ
- VRC_Interact は SDK 3.x に存在しない(§6.1 と同様、UdonBehaviour + Collider のみで OK)

---

## 9. _Managers と VRCWorld(シーン直配置)

```
_Managers (Empty GameObject, Position (0, 0, 0))
├── GameManager (Empty + GameManager UdonBehaviour 空アタッチ)
└── AmidakujiGenerator (Empty + AmidakujiGenerator UdonBehaviour 空アタッチ)

_Lighting (Empty)
├── DirectionalLight (Mode: Mixed、Phase 9 でベイク調整)
└── (LightProbeGroup, ReflectionProbe は Phase 9)

VRCWorld (Empty + VRCSceneDescriptor)
└── VRCSceneDescriptor 設定:
    - Spawns[0] = DefaultSpawn の Transform
    - Respawn Height Y = **-1**(床上面 Y=0 の 1 m 下、床外に落ちたら自動 Respawn)
    - Object Behaviour at Respawn = Respawn
```

---

## 10. Phase 1 完了基準(Prefab・シーン観点)

- [ ] `Assets/_Project/Prefabs/` に 6 個の Prefab(VerticalLine / HorizontalBar / GoalBarrier / Cart / Seat / PrizeArea)が存在
- [ ] `Cart.prefab` Root の Layer = `Cart`(User22)、子の Seat も同じ
- [ ] `GoalBarrier.prefab` Root の Layer = `GoalBarrier`(User23)、子の壁・天井も同じ
- [ ] `MainFloor` が §1 の値で配置済み
- [ ] VLine_0〜3、HorizontalBar 33 個(S00〜S10 × 3 ペア)、GoalBarrier_0〜3、Cart_0〜3、Seat_0〜3、PrizeArea_0〜3 が §2〜§7 の座標で配置済み
- [ ] EntryArea(Seats 4 つの親)+ DefaultSpawn + RulesPanel(仮)+ StartButton(仮)+ ResultDisplay(仮)が配置済み
- [ ] `_Managers/GameManager`、`_Managers/AmidakujiGenerator` が空 UdonBehaviour アタッチ済み
- [ ] `VRCSceneDescriptor.Spawns[0]` が DefaultSpawn を指している、`Respawn Height Y = -1`
- [ ] **VR HMD で実機確認**:
  - DefaultSpawn からの初見で EntryArea / 縦線 / GoalBarrier の方向感が掴める
  - MainFloor 上を自由歩行(Z=+12 ~ Z=-68 まで)
  - 縦線・横線の 2 cm 段差が歩行時に感じられないこと
  - GoalBarrier の隙間(W=1.5 m, H=0.5 m)を **歩行者がしゃがんでも匍匐でも通れない**
  - 賞品エリアに歩行者は侵入不可(GoalBarrier で阻まれる)
  - 床外に出るとリスポーンする
- [ ] [material-set.md §6](./material-set.md) の完了基準 を満たす

---

## 11. Opus 側で生成済みの設定(参考)

- `ProjectSettings/TagManager.asset`: User22=`Cart`, User23=`GoalBarrier` 追加済み
- `ProjectSettings/DynamicsManager.asset`: Layer Collision Matrix を [scene-structure.md §4.2](./scene-structure.md#42-物理コリジョン-matrix-project-settings--physics) 通りに更新済み(Layer 9, 10, 22, 23 の 4 箇所)
- Unity 起動後、`Edit > Project Settings > Physics` の Layer Collision Matrix を **目視確認** すること
- マテリアル 11 個(`M_UI_Display` を除く)は作成済み([material-set.md §7](./material-set.md))

## 12. 改訂履歴

- 2026-05-16: 初版作成(Phase 1 着手用、縦置きレイアウト)
- 2026-05-16: 平面水平レイアウトに全面改訂([ADR-0011](./adr/0011-flat-horizontal-layout.md))。Lane.prefab → VerticalLine.prefab に名称変更、SpawnDeck + Bridge を削除、座標を Y→Z 転置
- 2026-05-16: 横線位置を 12 段 → **11 段** に修正(S=0..10、最大 33 本)。最終 S10 Z=-53 から GoalBarrier Z=-58.5 までの 5.5 m を run-out zone として確保。当初の S11(Z=-58)は GoalBarrier 直前 0.5 m に位置し、不自然な配置だったため削除
- 2026-05-16: PrizeArea 寸法 8×8m → **3.5×8m** に修正(4m レーン間隔で 4 部屋並べた際に重ならないため)。GoalBarrier 1 個の幅 3m → **4m** に拡張(隣のバリアと隙間なく接続、歩行者迂回路を排除)。MainFloor 18×80 → **16×80** に縮小(GoalBarrier 連の全幅 16m と整合)
- 2026-05-16: PrizeArea Prefab Root の Rotation Y を **180°** に設定する旨を §7.1 に明記(Wall_S_Left/Right の隙間が +Z = GoalBarrier 側を向くようにする)
- 2026-05-16: VRC_Station の Player Mobility を `Mobile` → **`Immobilize (For Vehicle)`** に修正(乗り物用、Mobile だと着座中もプレイヤーが WASD で動けてカート移動と競合する)
- 2026-05-16: SDK 3.x には `VRC_Interact` コンポーネントが存在しないことを §6.1 / §8.4 に明記(Interact は UdonBehaviour 側の `Interact()` メソッドで実装)
- 2026-05-17: §5.1 の VRC_Station 設定で `Disable Station Exit` を `true` → **`false`** に変更([ADR-0007](./adr/0007-vrcstation-transform-cart.md) 2026-05-17 改訂、Phase 1 実機確認で「Desktop でカートから降りられない」UX を改善するため、両プラットフォームで退出可 = リタイア扱いに統合)
- 2026-05-17: Phase 1 実機確認で DefaultSpawn と Cart_0〜3 の rotation が +Z 向き(本来は -Z = ゴール方向を向くべき)になっていたため、シーンファイル直接編集で Y軸 180° に修正(checklist §5.3 / §8.2 の意図に整合)
