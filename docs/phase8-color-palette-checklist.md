# Phase 8: カラーパレット UI 配置チェックリスト

Tab4(色タブ)に、プレイヤーが好み色を選ぶ色見本 **Swatch × 8** を配置する手順。
背景は [BACKLOG.md §追加実装](../BACKLOG.md) / [ADR-0012](./adr/0012-goal-effect-randomized.md)。

## 0. 経緯(なぜこの作業が要るか)

`ColorPreferenceManager`(自動色割当・Persistence・Cart 伝播・壁色染色)と `ColorPaletteButton.cs`(色選択 Interact)は
Phase 6 で **スクリプトのみ実装**され、**シーンへの UI 配置が欠落**していた。
そのためプレイヤーは `playerId % 8` の既定色に固定で、**任意の色選択ができない**。
本手順で Tab4 に色見本を配置し、機能を完成させる。

## 1. 方式(確定)

- 色見本の着色は **MaterialPropertyBlock + `_Color`**(Cart / PrizeArea と同方式)。**マテリアル追加ゼロ**、Android バジェット 19/20 維持。
- 8 個の色見本は **共通マテリアル `M_Wall_Generic` 流用**(色は実行時に PropertyBlock で個別上書き、ベース色は無関係)。
- 色見本は **UI(Image)ではなく 3D Cube + MeshRenderer + BoxCollider**(Image は MaterialPropertyBlock 不可)。既存 Tab*Button と同じ作り。
- パレットは **Tab4 選択時のみ表示**。`RulesPanelController` が自動で出し入れする(下記スクリプトで対応済、手作業不要)。

## 2. スクリプト改修(✅ 完了済み)

[RulesPanelController.cs](../Assets/_Project/Scripts/RulesPanelController.cs):
`colorPaletteContainer` 追加 + `_RefreshColorPalette()` を「Tab4 時のみ表示 + 8 Renderer を `paletteColors[i]` で塗る」方式に変更済。
→ **シーン側の配置・配線(§4〜§5)のみ未実施。**

> **2026-05-29 実装メモ**: レイアウトは試行の結果 **RulesPanel 右側に 2 列 ×4 段(縦読み)+ 下にカスタム枠** に確定。
> マテリアルは Unlit ではなく **`M_Wall_Generic`(Cart の `M_LaneColor` と同じ `VRChat/Mobile/Standard Lite`)** を流用 —
> 色見本と Cart を同シェーダ・同 `_Color` にすることで発色を一致させる狙い(Unlit にすると Cart とズレるため不採用)。マテリアル 19 維持。
> 確定座標は §4 の表。本文(BodyText)は左に寄せ、Tab4 文言は「右のパレットから…」に変更済(Inspector override)。

## 3. Hierarchy 構造

### 現状(RulesPanel 配下)

```text
RulesPanel
├ Visual                  (背景の板、3D)
├ Canvas                  (World Space Canvas, scale 0.01)
│  ├ TitleText
│  ├ BodyText             ← タブ本文。Tab 切替でここの文章が変わる
│  ├ Tab1Label 〜 Tab4Label
│  └ LangToggleLabel
├ Tab1Button              ← 押す本体(3D Cube + Collider + U#)。Canvas の外
├ Tab2Button
├ Tab3Button
├ Tab4Button
└ LangToggleButton
```

### 完成形(★ = 新規作成)

```text
RulesPanel
├ Visual
├ Canvas
│  ├ TitleText / BodyText / Tab1-4Label / LangToggleLabel   (変更なし)
├ Tab1Button 〜 Tab4Button / LangToggleButton                (変更なし)
└ ColorPaletteContainer        ★ 空 GameObject(パレットの親。Tab4 時だけ表示される)
   ├ Swatch_0                  ★ 3D Cube(色見本。Interact で色決定)
   ├ Swatch_1                  ★
   ├ Swatch_2 〜 Swatch_6      ★
   ├ Swatch_7                  ★
   └ SelectionHighlight        ★ 3D Cube(選択中の色を囲む白枠)
```

### 正面から見たレイアウト(RulesPanel をプレイヤー視点で)

```text
┌──────────────────────────────┐
│            TitleText             │
│   ┌────────────────────────┐  │
│   │        BodyText           │  │   ← Tab4 では「色の説明文」
│   └────────────────────────┘  │
│     ■0  ■1  ■2  ■3             │   ← Swatch 上段 (y = -0.48)
│     ■4  ■5  ■6  ■7             │   ← Swatch 下段 (y = -0.64)
│  [Tab1][Tab2][Tab3][Tab4]  [JP] │   ← 既存ボタン (y = -0.85)
└──────────────────────────────┘
```

## 4. 配置手順(Unity Editor)

座標は **RulesPanel ローカル = ほぼ World メートル**(RulesPanel は scale 1)。
既存 Tab*Button が `y=-0.85, z=-0.02, scale 0.7×0.25×0.02` に並んでいるのが寸法の目安。

### 4.1 ColorPaletteContainer(親)

1. Hierarchy で `RulesPanel` を右クリック → **Create Empty**
2. 名前を `ColorPaletteContainer` に変更
3. Transform: Position `(0, 0, 0)` / Scale `(1, 1, 1)`

### 4.2 Swatch_0 〜 Swatch_7(色見本)

`ColorPaletteContainer` を右クリック → **3D Object > Cube** を 8 個作成(`Swatch_0`..`Swatch_7`)。
各 Cube に以下を設定:

- Transform: Scale `(0.13, 0.13, 0.02)` / Position は下表(z は全て `-0.05` = 背景 Visual(z=0)より手前)
- **Mesh Renderer** の Material(Element 0)= `M_Wall_Generic`
- **Box Collider**: `Is Trigger` を **ON**(Cube に自動で付く Collider をそのまま ON にするだけ)
- **Add Component → ColorPaletteButton**(U# スクリプト)を付与し、Inspector で:
  - `Color Manager` = `_Managers/ColorPreferenceManager` をドラッグ
  - `Color Index` = 下表の値
- 同じ Inspector の UdonBehaviour 欄: `Interaction Text` = `色を選ぶ` / `Proximity` = **4**

| GameObject | Color Index | Position x | Position y | 表示される色 |
| --- | --- | --- | --- | --- |
| Swatch_0 | 0 | +1.45 | +0.30 | 赤 |
| Swatch_1 | 1 | +1.45 | +0.10 | 橙 |
| Swatch_2 | 2 | +1.45 | -0.10 | 黄 |
| Swatch_3 | 3 | +1.45 | -0.30 | 緑 |
| Swatch_4 | 4 | +1.85 | +0.30 | シアン |
| Swatch_5 | 5 | +1.85 | +0.10 | 青 |
| Swatch_6 | 6 | +1.85 | -0.10 | 紫 |
| Swatch_7 | 7 | +1.85 | -0.30 | ピンク |

> 左列 x=+1.45 に 0/1/2/3、右列 x=+1.85 に 4/5/6/7 の縦読み。Color Index は `paletteColors` のインデックス。色はスクリプトが塗るので Cube 自体の色設定は不要。
> **カスタム色の空き枠**: `(+1.65, -0.52)`(2 列中央下・EN ボタンの真上)。v1.0 はスペース確保のみで未配置。

### 4.3 SelectionHighlight(選択枠)

1. `ColorPaletteContainer` を右クリック → **3D Object > Cube**、名前 `SelectionHighlight`
2. Transform: Scale `(0.18, 0.18, 0.01)` / Position は任意(z = `-0.035`、Swatch のわずか奥)
3. Mesh Renderer の Material = `M_Line`(白)など目立つもの
4. **Box Collider は削除**(枠は押さない)
5. 位置と表示はスクリプトが制御するので初期位置は適当で可

## 5. 配線(RulesPanel の RulesPanelController)

`RulesPanel` を選択し、Inspector の RulesPanelController で:

- `Color Palette Container` = `ColorPaletteContainer`
- `Color Palette Renderers`(Size を 8 に)= `Swatch_0`..`Swatch_7` の **Mesh Renderer** を index 0〜7 順にドラッグ(**Color Index と順序を一致させる**)
- `Color Palette Selection Highlight` = `SelectionHighlight` の Mesh Renderer

※ `colorManager` は配線済み。

## 6. 検証

### 6.1 ClientSim 単独

- [ ] Tab1〜3 ではパレット非表示、Tab4 でのみ 8 色が表示される
- [ ] 8 色が赤〜ピンクで正しく表示される
- [ ] Swatch を Interact → 白枠が移動し、着座中なら Cart Visual の色が変わる

### 6.2 Quest 実機(Build & Upload 後)

- [ ] proximity 4 で Swatch を無理なく押せる(RulesPanel 高さ調整と併せて)
- [ ] 色選択 → 着座 → ゴールで賞品エリア壁が選択色に染まる
- [ ] 色選択 → 退出 → 再入場で選択色が復元される

## 7. 完了基準

Tab4 で 8 色から選べ、選択色が Cart と賞品エリア壁に反映され、再入場で復元される。マテリアル数 19 維持。
