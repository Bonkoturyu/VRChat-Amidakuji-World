# UI 実装の落とし穴

Phase 5〜6 の UI 構築中(+ Phase 9 の音声配置)に遭遇した、再発しやすい落とし穴のメモ。
症状・原因・修正・検証手順をセットで残し、次に UI / 音声を追加するときの確認リストにする。

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

## 5. 2D 音源(BGM)は VRCSpatialAudioSource 自動付与で 3D 化される

### 症状(ClientSim と実機で BGM の鳴り方が違う)

Spatial Blend=0(2D)のつもりで置いた BGM が、ClientSim では均一に鳴るのに
**実 VRChat にアップロードすると場所によって音量が変わる**(GameObject の近くで大きく、
離れると減衰する)。「近いとデカい/遠いと消える」挙動になり、ClientSim の体感で合わせた
音量が実機で破綻する。

### 原因(アップロード時の自動付与 + spatialization=ON)

VRChat は**アップロード時、VRCSpatialAudioSource が付いていない AudioSource へ自動で 1 個追加し、
spatialization=ON の状態にする**。このため Spatial Blend=0 のつもりの 2D 音源が実機では
3D 点音源化する。ClientSim はこの自動付与を再現しないことが多く、ClientSim では 2D 均一のまま
鳴るため食い違いが生まれる(※実機挙動は要アップロード確認。VRChat 公式仕様)。

加えて ClientSim には実 VRChat のワールド音量スライダー/ランタイムのゲイン処理が無いため、
**ClientSim は総じて素直に・大きめに鳴る**。ClientSim の体感だけで最終音量を決めない。

### 修正(2D 音源は手動付与で spatialization OFF)

- BGM 等の 2D 音源の GameObject に **VRC Spatial Audio Source を手動で Add Component し、
  `Enable Spatialization` を OFF** にする(自動付与に任せると ON にされる)。
- 3D で鳴らしたい音(ゴールの爆発/紙吹雪 SE)は逆に spatialization ON のままで OK。
- 最終音量は ClientSim ではなく**実アップロード(Build & Test / Quest 実機)で調整**する。
  BGM Volume は 0.3〜0.4 程度の控えめからスタート(ゴール SE が埋もれないバランス)。

### 検証手順(2D 音源)

1. BGM GameObject に VRCSpatialAudioSource があり Enable Spatialization=OFF か確認
2. アップロード後の Build & Test で、ワールド内を移動して BGM 音量が**場所に依らず一定**か確認
3. ゴール SE と同時に鳴らし、BGM が SE を埋もれさせない音量バランスか確認

### 関連

- 音源の採用・再生方式: [adr/0013-audio-assets-and-licensing.md](./adr/0013-audio-assets-and-licensing.md) §4
- 資産レジストリ: [audio-assets.md](./audio-assets.md)

---

## 6. VRChat の `TextMeshPro/Distance Field` unsupported shader 警告(無害)

### 症状(消えない shader 警告)

Android(Quest)アップロード時、SDK Builder の Review Any Alerts に:

```text
World uses unsupported shader 'TextMeshPro/Distance Field'.
This could cause low performance or future compatibility issues.
```

が出る。**TMP の Examples & Extras フォルダを削除して再アップロードしても消えない**。タブ切替の
再検証でも残る。

### 原因(マテリアル不使用でもシェーダーが同梱される)

全 Assets + ProjectSettings をスキャンした結果(2026-05-30):

- desktop シェーダー `TextMeshPro/Distance Field`(`TMP_SDF.shader`、GUID `68e6db2ebdc24f95958faec2be5558d6`)を
  **参照しているマテリアル・フォントアセットは 0 件**(唯一のヒットは `TMP_SDF.shader.meta` = シェーダー自身の .meta)
- 実 UI フォント(`LiberationSans SDF` / `Fallback`)は **両方 Mobile 版**(`TMP_SDF-Mobile.shader`、GUID `fe393ace9b354375a9cb14cdbbc28be4`)
- `m_AlwaysIncludedShaders` は空、`.shadervariants` も無し

→ ワールドのどのマテリアルもこのシェーダーを使っていない。警告は **TextMeshPro パッケージの `TMP_SDF.shader`
ファイルがプロジェクトに存在しビルドに同梱される**ことを VRChat が検出して出している(マテリアル使用起因ではない)。

### 判定(v1.0 は受容)

- **ブロッカーではない**(アップロードは成功する)
- **実害ゼロ** — どのマテリアルも使わないので実レンダリングは Mobile シェーダーのみ。desktop シェーダーは
  描画に使われない死荷重(サイズ僅か)
- VRChat + TextMeshPro の既知挙動。多くの Quest ワールドが同警告のまま公開している
- 消す唯一の方法は `TMP_SDF.shader`(+ 他 desktop TMP シェーダー)ファイル自体の削除だが、**TMP の整合性を
  壊すリスク**があり公開前にやるべきでない → **v1.1 で TMP シェーダー stripping を検討**

### 検証(無害の確認)

- Quest 実機で**ワールドのパフォーマンスランクが Good** なら、この警告は無視して良い証拠

---

## 7. UdonSharp の `(int)long` キャストは実行時例外で UdonBehaviour を halt する

### 症状(v1.0 公開後に発覚)

Community Labs 公開後、**START を押下しても何も起きない**(カート発進せず、操作パネルの START が緑のまま)。
PC・Quest 両方で再現。ClientSim の Console に:

```text
GameManager.cs(114,67): Udon runtime exception detected!
An exception occurred during EXTERN to 'SystemConvert.__ToInt32__SystemInt64__SystemInt32'.
[UdonBehaviour] An exception occurred during Udon execution, this UdonBehaviour will be halted.
```

### 原因(明示縮小キャストが checked 変換にコンパイルされる)

`seed = (int)System.DateTime.Now.Ticks;` の `(int)long` キャストを、UdonSharp は通常C#の
**切り捨て(unchecked)ではなく `System.Convert.ToInt32(long)`** にコンパイルする。これは
**範囲超過で `OverflowException` を投げる checked 変換**。`DateTime.Now.Ticks` は `int.MaxValue` を
遥かに超えるため毎回例外 → `RequestStart()` がここで中断し、**以後 GameManager の UdonBehaviour が
完全停止(halt)**。これが二次的に「降りても START が緑のまま(退出時の participant クリアも動かない)」も
引き起こしていた。

`useDebugSeed = true`(=`seed = debugSeed`、キャスト無し)の間は発症せず、**本番化のため OFF にした
瞬間にこのパスへ入って発症**した。

### 対策

範囲内に収めてから変換する。下位31ビットマスクなら必ず `[0, int.MaxValue]` に収まり、下位ビットは
100ns 刻みで変化するため seed として実用上ランダム:

```csharp
seed = useDebugSeed ? debugSeed : (int)(System.DateTime.Now.Ticks & 0x7FFFFFFFL);
```

→ 修正コミット `ebe490d`。CLAUDE.md「Udon# 制約のリマインダ」にも横展開済。

### 教訓

- **`(int)long` / `(int)double` 等の明示縮小キャストは Udon では例外源**。大きな値を扱う箇所では
  マスク・剰余・`Mathf.Clamp` 等で範囲保証してからキャストする。
- **UdonBehaviour は例外で halt する**(以後その挙動が一切動かなくなる)。「一部機能が無反応 + 関連挙動も
  芋づる式に死ぬ」症状を見たら、まず ClientSim の Console で runtime exception を疑う。

---

## 8. TMP「Underline is not available in font asset」警告(無害)

ビルド時に `The character used for Underline is not available in font asset [Empty SDF for Default Font]`
が多数出る。シーン内の TextMeshPro が Underline フォントスタイルを参照しているがデフォルトフォント
(Empty SDF)に下線グリフが無いため。**黄色 Warning でゲーム動作には無影響**(下線が描画されないだけ)。
§6 と同類で **v1.0 は受容**。

---

## 関連リンク

- [docs/architecture.md](./architecture.md) — シーン全体の構成と参照関係
- [docs/scene-structure.md](./scene-structure.md) — Hierarchy 配置
- [docs/material-set.md](./material-set.md) §1 行 12 — `M_UI_Display` の TMP マテリアル定義
- 修正コミット `4216b24` — ResultDisplay 表示問題解消の差分
