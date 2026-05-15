# Backlog

## v1.0 受け入れ条件 (5/31 公開時点で満たすべき)

判定基準の定義は [docs/SPEC.md §12](./docs/SPEC.md#12-v10-完了の定義) を参照。
以下はステータストラッキング。

- [x] 仕様ロック (docs/SPEC.md)
- [x] アーキテクチャ・シーン構成案ロック (docs/architecture.md, docs/scene-structure.md)
- [ ] Phase 0: VCC環境構築 + 空ワールド Privateアップロード疎通
- [ ] Phase 1: 静的あみだくじ構造の VR HMD 実機確認
- [ ] Phase 2: カート単体走行 (1人、固定経路)
- [ ] Phase 3: ランダム生成 + seed同期 (2クライアントで一致確認)
- [ ] Phase 4: 4カート同時走行 + 賞品エリアテレポート
- [ ] Phase 5: RenderTexture俯瞰スクリーン
- [ ] Phase 6: 観戦デッキ整備
- [ ] Phase 7: ゲームフロー UI 完成
- [ ] Phase 8: Late Joiner / エッジケース対応
- [ ] Phase 9: ライティング・最適化 (PC Good ランク達成)
- [ ] Phase 10: Community Labs 公開

## v1.1 (公開後の最優先課題)

- [ ] 20人スケール対応
  - 縦線本数の可変化(プレハブ複製、シーン配置調整)
  - participantPlayerIds[] のサイズ拡大とSync制限再検証
  - 観戦カメラ画角の再調整
  - 横線生成確率の再チューニング
- [ ] 観戦カメラの複数アングル切り替え
  - サブカメラ(横からの全景)
  - フォーカスカメラ(特定カート追尾)
  - 観戦者がボタンで切り替え可能に
- [ ] BGM / SE の追加
  - エントリー中のBGM
  - カウントダウンSE
  - 横線通過時のSE
  - ゴールファンファーレ
- [ ] 賞品エリアの演出強化
  - ゴールごとに異なる装飾・ギミック
  - パーティクル演出

## v1.2 (中期)

- [ ] Quest版対応
  - Triangleバジェット、Material制約への対応
  - Shader互換性確認
- [ ] スマホ判定UI(World判定)
- [ ] ランキング機能(個人記録 or インスタンス内ベスト)
- [ ] 「歩き式モード」の追加([ADR-0001](./docs/adr/0001-cart-based-design.md) で言及)
- [ ] イベントギミック
  - ランダム加速ゾーン
  - 一時停止トラップ
  - ボーナスゴール
- [ ] CI整備(Markdownリント、ADR形式チェック、ユニットテスト追加 → `docs/dev-workflow.md` 参照)

## 課題・既知の制約

### 技術的不安要素(Phase着手時に検証する)

- **`Networking.GetServerTimeInSeconds()` のクライアント間誤差**: Phase 3 の2クライアントテストで実測。`CalculateServerDeltaTime` で吸収される想定だが、数十ms規模のズレが UX に影響するか確認([ADR-0003](./docs/adr/0003-precomputed-waypoint-lerp.md))
- **`VRC_Station` の `disableStationExit` 挙動**: VRトリガー退出はSDK仕様通り防げないので、リタイア処理のテストを Phase 2 で実機確認([ADR-0007](./docs/adr/0007-vrcstation-transform-cart.md))
- **RenderTextureのVR両眼描画コスト実測**: Phase 5 後に Stats でフレームタイムを確認、目標 FPS 切らないか
- **`UdonSynced int[4]` の Late Joiner 受信タイミング**: Phase 8 で複数クライアント実機テスト

### 検証済み(以前は不安要素だったもの)

- ~~UdonSharpでの `System.Random` 利用可否~~ → SDK 3.7.1 (2024-09) で利用可能。採用済み

### Open Questions

- ワールド名 (英語タイトル / 日本語サブタイトル) - 未決定
- サムネイル画像のコンセプト - 未決定
- ワールド説明文 - 未決定
- 賞品エリアのテーマ性 - 未決定(v1.0は無装飾でOK)

## アイデアプール (採否未定)

- カートのカスタマイズ(色・形を選べる)
- リプレイ機能(直前レースの再生)
- 観戦者からカートに「応援エモート」を送れる
- 賞品エリアにミニゲーム配置
- 季節イベント装飾 (ハロウィン版あみだくじ等)
- 多言語対応 (EN/JP切替)
- 「あみだくじ巨大化スケール変更」モード(さらに巨大に・ミニチュアに)
