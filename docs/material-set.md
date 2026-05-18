# Material Set Definition

Phase 1 着手時点の v1.0 マテリアル定義。PC + Android クロスプラットフォーム対応のため、
[ADR-0010](./adr/0010-android-in-v1.0-scope.md) と [CLAUDE.md パフォーマンスバジェット](../CLAUDE.md#パフォーマンスバジェット)
の Android 制約(Material Count ≤ 20、テクスチャ 1024×1024 以下、透明度ゼロ、GPU Instancing 必須)
に従う。

---

## 1. マテリアル一覧 (計 12)

| # | 名前 | シェーダー | テクスチャ | テクスチャサイズ | GPU Inst. | 用途(主な配置先) |
|---|---|---|---|---|---|---|
| 1 | `M_Floor_Common` | `VRChat/Mobile/Standard Lite` | `T_Floor_Common_Albedo` | 512 | ✓ | MainFloor(単一の大型床) |
| 2 | `M_Line` | `VRChat/Mobile/Standard Lite` | 単色(白 `#FFFFFF`) | — | ✓ | 縦線(VLine_0〜3)・横線(Bar_LX_SXX)で共用、床に貼り付く高さ 2 cm の細い Cube |
| 3 | `M_Wall_Generic` | `VRChat/Mobile/Standard Lite` | `T_Wall_Generic_Albedo` | 512 | ✓ | PrizeArea 壁・天井 / Seat マーカー / RulesPanel 背面 |
| 4 | `M_Floor_Prize` | `VRChat/Mobile/Standard Lite` | `T_Floor_Prize_Albedo` | 512 | ✓ | (将来用) v1.0 では未使用、PrizeArea 床は MainFloor 流用 |
| 5 | `M_Barrier` | `VRChat/Mobile/Standard Lite` | `T_Barrier_Albedo`(警告ストライプ) | 256 | ✓ | GoalBarrier の WallLeft / WallRight / Ceiling 共用 |
| 6 | `M_LaneColor_0` | `VRChat/Mobile/Standard Lite` | 単色(`_Color` のみ) | — | ✓ | Cart_0 ボディ(Cart のみ色分け、[ADR-0011](./adr/0011-flat-horizontal-layout.md)) |
| 7 | `M_LaneColor_1` | `VRChat/Mobile/Standard Lite` | 単色 | — | ✓ | Cart_1 ボディ |
| 8 | `M_LaneColor_2` | `VRChat/Mobile/Standard Lite` | 単色 | — | ✓ | Cart_2 ボディ |
| 9 | `M_LaneColor_3` | `VRChat/Mobile/Standard Lite` | 単色 | — | ✓ | Cart_3 ボディ |
| 10 | `M_Button_Active` | `VRChat/Mobile/Standard Lite` | 単色(緑 + Emission) | — | ✓ | StartButton 押下可状態 |
| 11 | `M_Button_Inactive` | `VRChat/Mobile/Standard Lite` | 単色(灰) | — | ✓ | StartButton 無効状態 |
| 12 | `M_UI_Display` | TextMeshPro `Mobile/Distance Field` | TMP Font Atlas | 1024 | ✓ | ResultDisplay / RulesPanel のテキスト面 |
| 13 | `M_FX_Explosion_Fireball` | `VRChat/Mobile/Particles/Additive` | (Unity 既定 `Default-Particle`、Phase 8 で差し替え可) | 64(既定) | ✓ | ExplosionEffect.prefab の火球用 ParticleSystem |
| 14 | `M_FX_Explosion_Smoke` | `VRChat/Mobile/Particles/Multiply` | (同上) | 64 | ✓ | ExplosionEffect.prefab の煙用(灰白色、Multiply で煤感補強) |
| 15 | `M_FX_Confetti` | `VRChat/Mobile/Particles/Additive` | (同上) | 64 | ✓ | ConfettiEffect.prefab の紙片用、Color over Lifetime で多色化 |

**合計 15 マテリアル**(バジェット 20 に対し +5 のヘッドルーム。Phase 5 の UI 追加・Phase 9 の装飾に充当)。Phase 4 着手時 13〜15 を追加(`M_FX_*` 3 個、[ADR-0012](./adr/0012-goal-effect-randomized.md))。

スカイボックスは Lighting Settings の Skybox Material として別管理。v1.0 は Unity 標準
`Default-Skybox` (Procedural) を流用し、新規マテリアル定義は行わない(World パフォーマンスの
Material Count には通常含まれない)。

---

## 2. 採用シェーダーの方針

### 2.1 `VRChat/Mobile/Standard Lite`(11 個)

- VRChat 公式が Quest 向けに推奨する Mobile シェーダーの中で、以下を全て満たすため採用:
  - `_Color` (Main Color) スロットあり → テクスチャ無しの純色マテリアルが作れる(Phase 1 のプレースホルダ運用に対応)
  - Albedo / Normal Map / Smoothness / Metallic / Occlusion / Emission スロットあり(Phase 9 で順次活用)
  - Lightmap 対応(Mixed Lighting + Light Probe 運用、[scene-structure.md §5](./scene-structure.md#5-static-フラグ運用) に整合)
  - `Enable GPU Instancing` チェックボックスを Inspector で持つ([ADR-0010](./adr/0010-android-in-v1.0-scope.md) の必須要件)
- アルファブレンド/カットオフは一切使わない(全て Opaque)
- Phase 1 当初 `VRChat/Mobile/Lightmapped` を候補にしていたが、当該シェーダーは
  `_Color` も `Enable GPU Instancing` も持たない最小シェーダーであることが判明したため、
  Standard Lite に変更した(2026-05-16 改訂)

### 2.2 TextMeshPro `Mobile/Distance Field`(`M_UI_Display` のみ)

#### Distance Field の仕組み

通常のビットマップフォントは各画素が `黒/白` の RGBA 値を直接持つ。拡大時に画素境界が出て
ピクセル化し、縮小時に間引きでエッジがちらつく。

Signed Distance Field (SDF) は、各画素に「最も近い文字輪郭までの符号付き距離」を 0〜1 で
記録するテクスチャ方式。シェーダーで「距離値が閾値以下なら塗る」と判定するため、

- 拡大: 連続値ベースの判定なので画素境界が出ない → 常にシャープ
- 縮小: ハードウェアの双線形補間と SDF が相性良く、安定したアンチエイリアス
- アウトライン / グロー / 影: 閾値を 0.5 / 0.6 / 0.4 と変えるだけで生成可能(別 Pass 不要)

#### Mobile バリアントの軽量化点

`TextMeshPro/Mobile/Distance Field` は SDF 方式の Quest 向けバリアント:

- マルチサンプル SDFAA を 4 タップ → 1 タップに削減
- Bevel / Glow / Underlay 等の追加エフェクトを削除(必要なら別バリアントで)
- Outline は最低限残し、フラグメントシェーダーは数行で完結

#### 運用ノート

- TMP の Font Asset は **Static Font Asset + Character Subset** で必要文字のみ Atlas に含める
  - 数字 `0-9`、記号 `→`、想定テキスト「席 N → ゴール M」「あみだくじ」「スタート」「結果」等
  - 漢字フルセットは Atlas 肥大化を招くため避ける
- Atlas Render Mode は `SDFAA` を選択(`SDFAA_HINTED` は CPU 負荷高)
- Atlas Resolution は 1024×1024、Padding 5 を基準

### 2.3 `VRChat/Mobile/Particles/Additive` と `Multiply`(`M_FX_*` 3 個、Phase 4 で追加)

[ADR-0012](./adr/0012-goal-effect-randomized.md) のゴール演出用パーティクル。

- **`VRChat/Mobile/Particles/Additive`**: 加算合成。光・火・紙吹雪のように「明るい色を上乗せ」したい場合に使用。黒は出ない(加算では黒 = 何も足さない = 透明扱い)
- **`VRChat/Mobile/Particles/Multiply`**: 乗算合成。背景を暗くする方向(煤・影)に使用。Quest の透明度マテリアル禁止制約下で「黒煙」の代替表現として限定使用
- 両シェーダーとも `Soft Particles`(Z バッファ補間)は Quest で重いため OFF
- パーティクルシステムの `Color over Lifetime` でフェードアウトを制御する運用が標準(マテリアル側 `_Color.a` に依存しない)

---

## 3. レーン色定義

QvPen 既定の原色 8 色 (`#FF0000 / #00FF00 / #0000FF / #FFFF00 / #FF00FF / #00FFFF / #FFFFFF / #000000`)
を母集合とし、視認性を踏まえて以下を採用。

| Lane | X 座標 | 色 | HEX | `M_LaneColor_N._Color` (RGBA 0-1) | 採用理由 |
|---|---|---|---|---|---|
| 0 | -6 | 赤 | `#FF0000` | (1.0, 0.0, 0.0, 1.0) | 端の左、警戒色で「1番」と認識しやすい |
| 1 | -2 | 黄 | `#FFFF00` | (1.0, 1.0, 0.0, 1.0) | 明度高、Lane 0 と隣接でも識別容易 |
| 2 | +2 | 緑 | `#00FF00` | (0.0, 1.0, 0.0, 1.0) | 中央右、寒色寄りに切り替え |
| 3 | +6 | 青 | `#0000FF` | (0.0, 0.0, 1.0, 1.0) | 端の右、Lane 1 黄と補色関係でゴール側からも判別容易 |

除外色の根拠: 白・黒は床・壁・天井と混ざる。マゼンタ・シアンは VR HMD で彩度が浮きやすい。

> **メモ**: v1.0 ではアバター自体が「誰がどの席か」のマーカーになるため、レーン色は実質補助でしか
> ない。色を全レーン同色(例: `#DDDDDD`)に振り替えたい場合は、各 `M_LaneColor_N` の `_Color`
> 値を差し替えるだけで完結する設計とする(マテリアルの参照関係には触らない)。

### 3.1 LaneColor マテリアルが当たる箇所

**Cart のみ** に LaneColor を適用([ADR-0011](./adr/0011-flat-horizontal-layout.md))。Seat / PrizeArea / Start マーカーはグレー統一(`M_Wall_Generic`)。

| 箇所 | 紐づくマテリアル |
|---|---|
| `Cart_N` の Body Renderer | `M_LaneColor_N` |
| `Seat_N` の Visual マーカー | `M_Wall_Generic`(グレー、レーン色なし) |
| `PrizeArea_N` のアクセント | (装飾なし、`M_Wall_Generic` のみ) |
| `Start_N` マーカー | (装飾不要、Phase 1 は空 GameObject) |

理由: 観戦者にとって識別が必要なのは「走行中のカートがどれか」だけで、座席や賞品エリアは X 位置・カートの実体で自明。色を 4 箇所で繰り返すと装飾過剰になる。

---

## 4. テクスチャ仕様

| 名前 | サイズ | 形式 | 用途 |
|---|---|---|---|
| `T_Floor_Common_Albedo` | 512×512 | ETC2 (Android) / DXT1 (PC) | MainFloor の床テクスチャ(タイリング 4 m) |
| `T_Wall_Generic_Albedo` | 512×512 | 同上 | 壁面・Seat マーカー共用 |
| `T_Floor_Prize_Albedo` | 512×512 | 同上 | (v1.0 未使用、将来用) |
| `T_Barrier_Albedo` | 256×256 | 同上 | 警告ストライプ(黒黄交互) |
| TMP Font Atlas | 1024×1024 | R8(SDF 単チャンネル) | テキスト用 |
| (`M_Line` は単色のためテクスチャ無し) | — | — | 縦線・横線(Cube に直接適用) |

- 全テクスチャに **Max Size 1024 以下** を Inspector で明示
- 全テクスチャに **Generate Mipmaps を有効化**(遠景でのモアレ抑制と GPU バンド幅削減)
- sRGB は Albedo すべて ON、Font Atlas のみ OFF(SDF は線形値で扱う)
- ノーマルマップは v1.0 では使用しない(`VRChat/Mobile/Standard Lite` は Normal Map スロットを持つが、Phase 1 はプレースホルダ色のみで進める。必要になれば Phase 9 で追加検討)

---

## 5. GPU Instancing 設定

全 12 マテリアルで `Enable GPU Instancing` をオンにする(ADR-0010)。

`.mat` ファイル上の serialize では:

```yaml
m_EnableInstancingVariants: 1
```

特に効果が大きいオブジェクト:

- HorizontalBar: 最大 33 個が同じ `M_Line` を使うため Static Batching(動的 enable のため非対象)→ **GPU Instancing が事実上必須**
- VerticalLine: 4 個が `M_Line` を共有(Static フラグありで Static Batching に乗る)
- Cart: 4 個が `M_LaneColor_N` を 1 個ずつ使用(色違いなので GPU Instancing のバッチには乗らないが、命名統一のため有効化)

---

## 6. Phase 1 完了基準(マテリアル観点)

- [ ] `Assets/_Project/Materials/` 配下にマテリアル **11 個**(Phase 1 範囲) が定義済み(`M_UI_Display` は Phase 5 で対応)
- [ ] 全マテリアルのシェーダーが `VRChat/Mobile/Standard Lite`(Phase 5 で `M_UI_Display` の `TextMeshPro/Mobile/Distance Field` を追加)
- [ ] 全マテリアルで `Enable GPU Instancing` がチェック済み
- [ ] アルファブレンド系の Surface Type に切り替わったマテリアル数 = 0
- [ ] テクスチャアセットの Max Size が 1024 以下、Mipmap 生成が有効
- [ ] VRChat World パフォーマンスツール上で Material Count ≤ 20
- [ ] レーン色 4 種が `M_LaneColor_0` 〜 `M_LaneColor_3` の `_Color` プロパティに HEX 値どおり設定済み

---

## 7. Phase 1 運用方針(プレースホルダ色のみ)

Phase 1 着手時点では、テクスチャ画像は用意せず **Main Color のみ設定** したプレースホルダ
マテリアルを作る。理由:

- Phase 1 の目標はシーン構造と動線確認であり、見た目品質は対象外
- テクスチャ画像の手配を後ろに倒すことで Phase 1 を 1 日に短縮可能
- Phase 9 のライティング・最適化フェーズで「色 → テクスチャ」の差し替えはマテリアル設定のまま `Albedo` スロットに `.png` をドラッグするだけで完了するため、後戻り工数は最小

### Phase 1 プレースホルダ色一覧

| マテリアル | Albedo Color (HEX) | 備考 |
|---|---|---|
| `M_Floor_Common` | `#888888` | 中間グレー(MainFloor) |
| `M_Line` | `#FFFFFF` | 白(縦線・横線、灰色床に対し最大コントラスト) — 旧 `M_Post_Track` をリネーム + 色変更 |
| `M_Wall_Generic` | `#AAAAAA` | 明るめグレー(PrizeArea 壁・Seat マーカー) |
| `M_Floor_Prize` | `#9999BB` | (v1.0 未使用、将来用)床と区別する微青みグレー |
| `M_Barrier` | `#FFCC00` | 警告色(黄)、Phase 9 でストライプテクスチャ化 |
| `M_LaneColor_0` | `#FF0000` | 赤(Cart_0 ボディのみ) |
| `M_LaneColor_1` | `#FFFF00` | 黄(Cart_1 ボディのみ) |
| `M_LaneColor_2` | `#00FF00` | 緑(Cart_2 ボディのみ) |
| `M_LaneColor_3` | `#0000FF` | 青(Cart_3 ボディのみ) |
| `M_Button_Active` | `#00CC00` | 緑、Emission は Phase 5 で検討 |
| `M_Button_Inactive` | `#666666` | 暗めグレー |
| `M_FX_Explosion_Fireball` | `#FF6600` | オレンジ(火球用、パーティクル `Color over Lifetime` で `#FFFF00 → #FF6600 → #AA1100` グラデ運用) — Phase 4 で追加 |
| `M_FX_Explosion_Smoke` | `#DDDDDD` | 明るい灰(Multiply で背景を暗くする方向に作用、結果として煤感) — Phase 4 で追加 |
| `M_FX_Confetti` | `#FFFFFF` | 白(パーティクル `Start Color` で `#FF0000 / #FFFF00 / #00FF00 / #0088FF / #FF66CC` のランダム多色運用) — Phase 4 で追加 |

全マテリアル共通設定:

- Shader: `VRChat/Mobile/Standard Lite`
- Metallic: 0(既定)
- Smoothness: 0.5(既定)
- Normal Map / Occlusion / Detail Mask: 空のまま
- Emission: オフ(`M_Button_Active` のみ Phase 5 で再検討)
- Advanced Options > **Enable GPU Instancing: チェック**
- Advanced Options > Double Sided Global Illumination: オフ

## 8. 改訂履歴

- 2026-05-18: Phase 4 着手準備として **`M_FX_Explosion_Fireball` / `M_FX_Explosion_Smoke` / `M_FX_Confetti` の 3 マテリアルを追加**(計 15 個)。シェーダー §2.3 を Particles 系として追記、§7 にプレースホルダ色を追記([ADR-0012](./adr/0012-goal-effect-randomized.md))
- 2026-05-16: 初版作成(Phase 1 着手に合わせて 12 マテリアル構成・レーン色 4 色を確定)
- 2026-05-16: ベースシェーダーを `Mobile/VRChat/Lightmapped` → `VRChat/Mobile/Standard Lite` に変更。
  前者は `_Color` と `Enable GPU Instancing` を持たない最小シェーダーで、プレースホルダ色運用に不適と判明したため。
  Phase 1 運用方針(プレースホルダ色一覧)を §7 として追記
- 2026-05-16: 平面水平レイアウト([ADR-0011](./adr/0011-flat-horizontal-layout.md))への移行に伴い、
  `M_Post_Track`(柱用 `#555555`)を **`M_Line`(縦横線用 `#FFFFFF`)に転用**。
  Unity 側では Project ウィンドウで `M_Post_Track.mat` を F2 リネーム → `M_Line` → Albedo を白に変更で完了
  (新規マテリアルアセットを増やさず、計 11 個のまま据え置き)。
  レーン色適用箇所も Cart-only に縮小(§3.1)
