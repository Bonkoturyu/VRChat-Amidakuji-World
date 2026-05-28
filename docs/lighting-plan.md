# Lighting Plan (Phase 9 着手前ドラフト)

Phase 9「ライティング・最終最適化」の事前設計。Phase 7-8 着手前に確定しておく
ことで、Material/Mesh 追加修正時に Static フラグ・UV2 まわりを取りこぼさない。

関連: [CLAUDE.md パフォーマンスバジェット](../CLAUDE.md#パフォーマンスバジェット) /
[ADR-0010](./adr/0010-android-in-v1.0-scope.md) /
[ADR-0011](./adr/0011-flat-horizontal-layout.md) /
[material-set.md](./material-set.md) /
[scene-structure.md](./scene-structure.md)

---

## 1. 基本方針

- **PC + Android 共通**: Mixed Lighting + Light Probe + Reflection Probe (Static)
- **Realtime Light は 0**(CLAUDE.md バジェット準拠)。`Directional Light` も Baked か Mixed に
- **Lightmap 対応必須**: 全 Static Mesh で UV2 (Lightmap UV) が生成されていること
- **GPU Instancing**: Phase 7 監査で 16/16 件 ON 済([material-set.md §1](./material-set.md))
- **Post Processing**: 控えめ(SSR / SSAO は VR 酔いの原因、使わない)

---

## 2. シーン空間の整理(Lightmap / Probe 配置の前提)

| 領域 | World 範囲 | 用途 | 配置密度 |
| --- | --- | --- | --- |
| MainFloor | X[-8, +8] / Z[+12, -68] / Y=0 | あみだくじ走行面 + 観戦面 | 中(平面、均一光) |
| EntryArea | X[-6, +6] / Z[+2, +12] / Y=0〜3 | Seats / StartButton / RulesPanel / ResultDisplay | 高(UI 多、視認重要) |
| GoalBarrier | X[-8, +8] / Z=-58.5 / Y=0〜4 | カート用隙間付きバリア(高さ 4 m) | 低 |
| PrizeArea_0〜3 | 各 X[3.5m 幅] / Z[-64〜] / Y=0〜4 | 賞品エリア 4 部屋 | 中(内部光と外部の差) |
| Sky / Ambient | — | 屋外想定、Skybox 由来 Ambient | — |

走行範囲は全体的に **屋外フラット**。劇的なライティング差は不要、均一に明るく
読みやすい仕上げを優先する(VR の体験ストレス回避)。

---

## 3. Lightmap 設定

### 3.1 Static フラグ

Lightmap に焼くオブジェクトに `Static Editor Flags > Contribute GI` を立てる。
**現状の Prefab で Static フラグが混在しているケース**:

- `PrizeArea.prefab` の壁が `StaticEditorFlags: 123` (commit `2f3e3f3` で調整) — Contribute GI 等含む(値の bit 内訳は Phase 9 で再確認)
- カート (`Cart_n`) は走行移動するので **Static にしない**(Light Probe で補完)
- 横線 (`Bar_LX_SXX`) / 縦線 (`VLine_n`) は SetActive 切替するが位置固定 → Static 化して可

### 3.2 Lightmap サイズ

| プラットフォーム | Lightmap Resolution | Max Size |
| --- | --- | --- |
| PC | 40 (Default) | 1024 |
| Android | 20-30 (削減) | 1024(VRChat 推奨上限) |

`MainFloor` は面積大きいが、テクスチャ無しの単色運用なら **大胆に Lightmap
Scale を下げる**(Mesh Renderer の `Scale In Lightmap` を 0.1〜0.5)。

---

## 4. Light Probe Group 配置案

平面なので Y=0.5〜2.0 m を 2 段、X/Z は走行レーン中心 + Prize 部屋内に配置。

### 4.1 走行面 (MainFloor) 上の Probe

```
Y=0.5: X={-6, -2, +2, +6} × Z={+10, 0, -10, -20, -30, -40, -50, -58} = 32 点
Y=2.0: 同 X / 同 Z                                                   = 32 点
小計: 64 点
```

走行ライン上(レーン X={-6,-2,+2,+6})は Cart が必ず通るので最低 8 段。

### 4.2 EntryArea 内の Probe

```
Y=1.0: X={-4, 0, +4} × Z={+4, +8, +11} = 9 点
Y=2.5: X={0} × Z={+7}                  = 1 点(ResultDisplay 中央近傍)
小計: 10 点
```

### 4.3 PrizeArea_n 内の Probe(各部屋)

```
Y=0.5: X={-1, +1} × Z={-62, -64, -66} = 6 点
Y=2.0: X={0} × Z={-64}                = 1 点
小計: 7 点 × 4 部屋 = 28 点
```

**Probe 総数: 約 100 点**(VRChat World としては中規模)。

---

## 5. Reflection Probe 配置案

Realtime Reflection なし。すべて Baked。

| # | 位置 (X, Y, Z) | Box Size | 用途 |
| --- | --- | --- | --- |
| 1 | (0, 2, -28) | (16, 4, 80) | MainFloor 全体カバー |
| 2 | (X_n, 2, -64) × 4 | (3.5, 4, 6) | PrizeArea_0〜3 内部 |
| 3 | (0, 2, +7) | (12, 4, 12) | EntryArea(Seats + UI) |

**合計 6 個**(VRChat ヘビーユース帯、上限内)。

---

## 6. Occlusion Culling

- 全 Static Mesh で **Occluder Static** + **Occludee Static** を立てる
- PrizeArea の壁・GoalBarrier が観戦者視点で奥を隠す → ベイク効果あり
- MainFloor / 線オブジェクトは平面で遮蔽効果薄、Occludee のみで可

ベイクは Phase 9 後半(Lightmap 確定後)。Window > Rendering > Occlusion。

---

## 7. Static Batching

- 横線 33 本(`Bar_LX_SXX`) / 縦線 4 本(`VLine_n`) / MainFloor → Static 化で 1 DrawCall に
- PrizeArea 壁(各 5 面 × 4 部屋) → 部屋ごとにバッチング
- Cart は除外(走行移動)、Effect Prefab も除外(ParticleSystem)

`Player Settings > Other Settings > Static Batching` ON 確認。

---

## 8. Skybox / Ambient / Post Processing

- **Skybox**: Unity 標準 `Default-Skybox` (Procedural) を流用([BACKLOG ワールドメタデータ §](../BACKLOG.md))。Phase 10 で雰囲気に合わせて差し替え検討
- **Ambient Source**: Skybox(Spherical Harmonics)。Ambient Intensity 1.0
- **Post Processing**: 使わない(VR 酔い + Quest GPU 負荷)

---

## 9. Phase 9 着手時チェックリスト

1. 全 Static Mesh に Lightmap UV があるか(`Generate Lightmap UVs` ON)
2. Static フラグの確認(MainFloor / 線 / 壁 = Static、Cart / Effect = 非 Static)
3. PC: Lightmap ベイク(Bake)
4. Android Platform 切替後: Lightmap 再ベイク or PC ベイクを Android で確認
5. Light Probe Group を §4 案で配置 → ベイクで `SH` データ生成
6. Reflection Probe を §5 案で配置 → 各 Probe で Bake 実行
7. Occlusion Culling ベイク
8. PC / Android 両方で VRChat World Analyzer → Good ランク確認
9. DrawCall / Triangle 数を Stats で実測、バジェット内

---

## 10. 想定リスク

- **Quest 実機で Lightmap が大きすぎて FPS 低下**: Lightmap Resolution を下げる、または PC 専用 Lightmap で Android は Light Probe 中心に
- **PrizeArea 内が暗くなる**: 部屋内 Probe 密度を上げる、または Realtime Skybox Ambient のみで補う
- **Probe 配置漏れで Cart が暗くなる**: 走行ライン上の Probe 密度を §4.1 より上げる(Y=1.0 段を追加)

これらは Phase 9 実機判定で再調整、本ドラフトは出発点として使う。
