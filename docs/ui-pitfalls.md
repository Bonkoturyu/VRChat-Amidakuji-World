# UI 実装の落とし穴

Phase 5〜6 の UI 構築中に遭遇した、再発しやすい落とし穴のメモ。
症状・原因・修正・検証手順をセットで残し、次に UI 追加するときの確認リストにする。

関連: [docs/architecture.md](./architecture.md) / [docs/scene-structure.md](./scene-structure.md) /
今回の修正コミット `4216b24`。

---

## 1. World Space Canvas と背景 Mesh の Z-Fighting

### 症状(背景だけ見えテキストが消える)

ResultDisplay の `Canvas` を `_Show()` で `SetActive(true)` しても、Game ビューで
背景パネル(`Visual` = Cube 厚み 0.1 の Mesh)だけ見えてテキストが描画されない。
Editor の Scene ビューではテキスト("New Text")が見えるため、TMP の生成や
スクリプトの呼び出し系統では原因が掴めない。Console にもエラーは出ない。

### 原因(深度バッファの Z-Fighting)

- `ResultDisplay` Position `(3, 2.5, 7)`、Rotation Y=180、`Visual` Scale `(4, 2, 0.1)`
  → Visual の World Z 範囲は `6.95〜7.05`
- `Canvas` Local Position Z=`-0.06`、Rotation Y=180 適用後の World Z は `7.06`
- プレイヤー(spawn Z=10)から見たカメラ距離差: `Canvas 2.94 m` vs `Visual front 2.95 m`
- **わずか 0.01 m のマージン**は GPU の depth precision で Z-Fighting レンジ内。
  TMP 側が負けて背景 Mesh に上書きされる。

`World Space Canvas` は薄い一枚平面で、TMP は通常 ZWrite Off / ZTest LEqual。
理論上は手前に来るはずだが、距離 ~3 m / Far plane 1000 m 程度の near/far 比では
深度バッファの精度がこの距離差を分離できないことがある。

### 修正(Canvas を手前にオフセット)

Canvas Local Position Z を背景 Mesh から **0.05 m 以上のマージン**を持って手前に
オフセットする。今回は `-0.06` → `-0.1`(Rotation Y=180 配下では Local Z をマイナス
方向に振ると World Z が大きくなる = プレイヤー側に近づく)。

Rotation Y=0 に統一する場合は符号反転 = Canvas Local Z=`+0.1` で同じ効果。
**Rotation Y と Canvas Local Z の符号関係**で頭がこんがらがるので、設計時に
「Canvas はプレイヤー側に出す」という意図をコード/Prefab レベルで明文化するのが安全。

### 検証手順(Z-Fighting)

1. Canvas Pos Z を変更して ClientSim 再生
2. `STATE_RESULT_DISPLAY` の 10 秒間に Game ビューで文字が見えることを確認
3. 見えなければ `Visual` を一時 `SetActive(false)` にして単独描画を確認
   → 見える: Z-Fighting / 隠蔽が原因(別の値で再調整)
   → 見えない: 別の問題(TMP 参照外れ、`_Show()` 未到達など)

### 同種ケースが潜むパネル

- [x] `ResultDisplay/Canvas` (修正済、Pos Z=-0.1)
- [x] `RulesPanel/Canvas` (予防修正済、Pos Z=-0.1)
- [ ] 今後 Visual 背景つき UI を追加するときは **必ず 0.05 m マージン**を取る

---

## 2. TMP Font Asset `Empty SDF + Fallback` のグリフ不在

### 症状(罫線文字で警告大量)

ResultDisplayUI の `separatorLine = "─────────────────────"` を表示すると、
Console に次の警告が大量に出る:

```text
The character used for Underline is not available in font asset [Empty SDF for Default Font].
```

文字自体は罫線が抜けるだけで、ヘッダー(レース結果)は正常表示される。

### 原因(Fallback にグリフ不在)

本プロジェクトの TMP は以下の構成:

```text
Font Asset (メイン):  Empty SDF for Default Font   ← グリフ 0 個
  ↓ Fallback
Font Asset (補完):    NotoSansJP-Medium SDF        ← JP + ASCII を含む
```

メインを空 SDF にして全文字を Fallback の `NotoSansJP-Medium SDF` で描画する設計。
NotoSansJP-Medium に含まれない文字(罫線文字 `─` = U+2500、その他装飾文字)は
グリフを見つけられず警告 + 描画スキップになる。

Underline という単語は紛らわしいが、TMP 内部で「下線処理」と「不在グリフの代替経路」を
共通の警告メッセージで出す実装になっており、罫線文字 `─` でもこのメッセージが出る。

### 修正(ASCII 置換 or グリフ追加)

選択肢は 2 つ:

| 方針 | 手間 | 副作用 |
| --- | --- | --- |
| A. テキストを ASCII に置換(`─` → `=` / `-`) | 1 行変更 | 見た目が罫線→等号になる |
| B. Fallback Font Asset に罫線文字グリフを追加 | TMP Font Asset Creator で再生成、容量増 | 他の装飾文字も拾える |

今回は A を採用(`separatorLine = "====================="`)。装飾性を重視する場合は B。

### 検証手順(グリフ不在)

1. ClientSim 再生 → Console に `not available in font asset` 警告が出ないことを確認
2. ResultDisplay 表示中、ヘッダー下の区切り線が想定どおり描画される

### 注意点(Inspector override)

- Scene の Inspector で `Separator Line` フィールドを上書き設定していると、コード側
  デフォルト値の変更が **反映されない**(UdonSharpBehaviour は Inspector 値を serialize)。
  既存 Scene の値を更新する場合は Inspector で右クリック → `Revert to Prefab default`、
  または手動で書き換える。
- 将来、絵文字や特殊記号を UI に入れたくなった場合は同じ落とし穴に当たる。
  装飾文字を増やすなら Fallback Font Asset の glyph table を最初に確認する。

---

## 3. タブ切替の単一 BodyText は「最長言語」に高さを合わせる

### 症状(EN で本文がはみ出す)

Tab 切替で 1 つの `BodyText` に文字列を差し替える方式(RulesPanel)。JP 基準で高さを
決めると、行数が増える EN(特に Tab2 観戦 = 7〜8 行)が領域を超え、Vertical Align Top の
まま下のタブボタンに重なってはみ出す。JP では問題なく見えるため気づきにくい。

### 原因(高さが JP 基準)

`BodyText` Height 75(JP 5 行想定)に対し EN は 7〜8 行。TMP の Overflow=Overflow で
領域外にそのまま描画され、タブボタン(y=-0.85)に被さる。

### 修正(最長言語に合わせる)

- Height を**最長言語(EN)基準**に拡大(75 → 100)+ Pos Y を下げて、上端をタイトル下・
  下端をタブ手前に収める(確定値 Pos Y=-5 / Height 100 → 上端 World +0.45・下端 -0.55、タブ -0.85 と非干渉)。
- 確実を期すなら TMP の **Auto Size ON**(Min/Max 指定)で、長いタブだけ自動縮小させる。

### 注意点(全言語で確認)

多言語 UI は必ず**全言語で最長ケースを Game ビュー確認**する。JP だけ見て OK としない。

---

## 4. 3D Mesh の UI 部品は背景 Visual と同じ z に置くと埋もれる

### 症状(色見本が見えない)

カラーパレットの色見本(3D Cube + MeshRenderer)を配置しても Game ビューで見えない。

### 原因(背景と同じ z)

色見本の z=0 が背景 `Visual`(z=0)と同一面で Z-Fighting・埋没。RulesPanel の**手前方向は
−z**(本文 Canvas z=-0.1、タブ z=-0.02 が手前で見えている)。

### 修正(手前へオフセット)

- 色見本を背景より手前(z=-0.05)へオフセット。
- 選択枠(SelectionHighlight)は Swatch のわずか奥かつ薄く(z=-0.035 / 厚み 0.01)にして、
  選択色を覆い隠さず縁取りに見せる。

### 補足(Mesh と発色一致)

- Image(UI)でなく **3D Mesh** にするのは、色を MaterialPropertyBlock + `_Color` で
  動的に塗るため(Image は CanvasRenderer で MPB 不可)。
- 色見本は Cart の `M_LaneColor` と**同じ `VRChat/Mobile/Standard Lite`** を使うと、
  選んだ色とカートの発色が一致する(Unlit にすると Cart とズレるので不可)。

---

## 関連リンク

- [docs/architecture.md](./architecture.md) — シーン全体の構成と参照関係
- [docs/scene-structure.md](./scene-structure.md) — Hierarchy 配置
- [docs/material-set.md](./material-set.md) §1 行 12 — `M_UI_Display` の TMP マテリアル定義
- 修正コミット `4216b24` — ResultDisplay 表示問題解消の差分
