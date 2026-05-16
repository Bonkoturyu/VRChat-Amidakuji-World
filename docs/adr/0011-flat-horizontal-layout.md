# ADR-0011: あみだくじ本体を「平面水平レイアウト」に変更(縦置き 60m 構造を廃止)

- **Status**: Accepted
- **Date**: 2026-05-16

## Context

[SPEC.md](../SPEC.md) §3.2 と [scene-structure.md](../scene-structure.md) §7 では、
あみだくじ本体を「**縦置き 60 m**」(Y 軸方向に高さ 60 m の歩行可能な多層構造)として定義していた。
縦線・横線はすべて空中に浮かぶ歩行可能な床として実装し、観戦者は階段やランプを介して上下を移動する想定だった。

その後、ユーザーと設計意図のすり合わせを行ったところ、本来のコンセプトは:

> 「平たい地面に、あみだくじの線が書かれて(段差もなし)、その上を Primitive Cube に乗った参加者が動く。
>  あみだくじがランダムで書かれても、Primitive Cube の動きが変わるだけで済む」

であることが判明した。これは私(Opus)が SPEC 策定時に「縦置き」という言葉を立体構造として
過剰解釈したことに起因する、設計意図とドキュメントの乖離である。

加えて、ユーザーから「シンプルにした方が 5/31 公開に間に合う品質に届く」との指摘もあった。

## Decision

**あみだくじ本体を、Y=0 の単一平面の床上にレイアウトする「平面水平レイアウト」に変更する。**
従来の「縦置き 60 m」表現は廃止する。

### 新レイアウトの骨子

- 床: 単一の大型 Mesh(Cube Scale 例: 18 × 0.2 × 80 m)、上面 Y=0
- 縦線(4 本): 細い Cube(0.2 m 幅 × 0.02 m 高 × 60 m 長)を床上面に貼り付け、X=-6/-2/+2/+6
- 横線(最大 36 本): 細い Cube(4.0 m 幅 × 0.02 m 高 × 0.2 m 長)、隣接縦線ペアの境界 Z に配置、enable/disable でランダム表示
- 縦軸は **Z 方向(水平)** に転置(従来の Y=0 → -60 を、Z=+2 → -58 に置き換え)
- EntryArea / Seat / StartButton は床と同じ Y=0 平面上に配置
- PrizeArea は GoalBarrier の Z=-58.5 から先に小部屋として接続
- 観戦者は同じ Y=0 床面を自由歩行でカートを追いかける(階段・ランプ・段差はゼロ)
- 床外に出たプレイヤーは VRCSceneDescriptor の `Respawn Height = -1` で自動リスポーン

### あみだくじ線の表現

- 床上面 Y=0 に対し、Y=0.01(中心)に細い Cube を置き、線の上面が Y=0.02
- 2 cm の段差は VRChat の自動 Step Climb 範囲内のため、歩行体験では段差として認識されない
- 物理的に「足元の線が見える + 何の障害物にもならない」状態を担保
- 全ての線は単一マテリアル `M_Line`(白 `#FFFFFF`)で描画。Mobile Tri 数への影響は微小
  (最大 40 本 × Cube 12 tris = 480 tris、バジェット 250,000 の 0.2%)

### ゲームロジックは不変

以下の判断は維持される:

- [ADR-0001](./0001-cart-based-design.md): カート式体験
- [ADR-0002](./0002-deterministic-rng-seed-sync.md): seed 同期によるあみだくじ生成
- [ADR-0003](./0003-precomputed-waypoint-lerp.md): カートの事前計算 Waypoint + Lerp 補間
- [ADR-0005](./0005-minimal-sync-variables.md): 同期変数の最小化
- [ADR-0007](./0007-vrcstation-transform-cart.md): VRC_Station + Transform 駆動
- [ADR-0008](./0008-4lane-scope-scalable-design.md): 4 レーン固定 + 可変サイズ対応
- [ADR-0009](./0009-follow-alongside-spectator.md): 追いかけ式観戦
- [ADR-0010](./0010-android-in-v1.0-scope.md): Android 対応

特に ADR-0003 は「カートは物理経路を辿るのではなく、Udon が計算した Waypoint を Lerp で繋ぐ」
ことを定義しており、本変更とは独立に成立する。線はあくまで視覚マーカーであり、カートの動きは
従来通り Udon コードが決定する。

ADR-0009「追いかけ式観戦」は、平面レイアウトでさらに自然に成立する(立体構造の階段が消えるため、
観戦者は床面でカートを追走するだけになる)。

## Consequences

### Positive

- **シーン構造が劇的にシンプル化**: Lane.prefab / HorizontalBar.prefab の「歩行可能床 + 柱」が不要。
  代わりに「細い線 Cube」だけになる
- **モバイル負荷がさらに軽くなる**: 縦に長い床が不要、Tri 数・DrawCall とも余裕度向上
- **VR 体験の安全性向上**: 60 m の高所からの墜落リスクが消える(Quest ユーザーで VR 酔いを起こす要因の除去)
- **観戦 UX 改善**: 階段・ランプを通らず、すぐカートに追いつける
- **実装工数削減**: ProBuilder で複雑な歩行可能床を組む必要が無く、Primitive Cube + Scale で足りる
- **Phase 1 着手が即可能**: ユーザーが ProBuilder を未習得でも、Unity 標準 Primitive のみで Phase 1 を消化可能

### Negative

- **既存ドキュメントの大幅改訂**: SPEC.md / scene-structure.md / phase1-prefab-checklist.md / material-set.md /
  tasklist.md / CLAUDE.md / BACKLOG.md の座標系・寸法を全更新する必要がある
- **「巨大あみだくじ」感の演出が変わる**: 縦置きで「見上げる」迫力が無くなる代わり、
  「足元に広がる巨大な書き物」という視点に変わる。これはコンセプト上、むしろ忠実だが、
  既に作っていたメンタルモデルとは別物
- **PrizeArea の上下関係が消える**: 縦置きでは「ゴール後に下に落下する」演出ができたが、
  平面では「壁の向こうに進む」だけになる。v1.1 で何らかの演出を足す余地

### マテリアルへの影響

- `M_Post_Track`(柱用、灰色 #555555)が不要になる
- `M_Line`(線用、白 #FFFFFF)が新規必要
- 既に作成済みの `M_Post_Track.mat` は **リネーム + Albedo 色変更で `M_Line` に転用** する
  (マテリアル数は 11 のまま据え置き)

### スケジュール影響

- 影響なし。むしろ Phase 1 の Unity 作業量が減るため、5/16〜5/17 の 2 日枠で余裕が生まれる
- Phase 9 のライティング・最適化は単一平面のため、Light Probe / Reflection Probe の配置が容易

## 改訂履歴

- 2026-05-16: 制定(SPEC 策定時の縦置き解釈がユーザー意図と乖離していたことの是正)
