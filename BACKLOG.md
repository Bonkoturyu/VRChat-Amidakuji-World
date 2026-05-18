# Backlog

## v1.0 受け入れ条件 (5/31 公開時点で満たすべき)

判定基準の定義は [docs/SPEC.md §13](./docs/SPEC.md#13-v10-完了の定義) を参照。
以下はステータストラッキング。

- [x] 仕様ロック (docs/SPEC.md)
- [x] アーキテクチャ・シーン構成案ロック (docs/architecture.md, docs/scene-structure.md)
- [x] Android 対応スコープ決定 (ADR-0010)
- [x] 観戦システム方針決定: 追いかけ式 (ADR-0009)
- [x] 平面水平レイアウト決定 (ADR-0011)
- [x] Phase 0: VCC環境構築 + GitHub Private Repo + 空ワールド Privateアップロード疎通
- [x] Phase 1: 平面水平あみだくじ床(MainFloor + 縦線・横線、Mobile考慮)の VR HMD 実機確認
- [x] Phase 2: カート単体走行 + 歩行者非衝突レイヤー設定(走行中の干渉確認のみ Phase 3 に持ち越し、停止カートで Layer 設計は検証済み)
- [ ] Phase 3: ランダム生成 + seed同期 (2クライアントで一致確認)
- [ ] Phase 4: 4カート同時走行 + 賞品エリアテレポート + ゴール手前バリア
- [ ] Phase 5: ゲームフロー UI 完成
- [ ] Phase 6: Late Joiner / エッジケース対応 (PC)
- [ ] Phase 7: Android Platform 切替 + 初期最適化
- [ ] Phase 8: Quest 実機テスト + 調整
- [ ] Phase 9: ライティング・最終最適化(PC + Android)
- [ ] Phase 10: Community Labs 公開(PC + Android 両ビルド)

## v1.1 (公開後の最優先課題)

- [ ] **iOS 対応**
  - VRChat の Android フォールバックでの実体験を確認
  - 必要なら iOS 専用ビルドを追加
  - iOS 実機(or Mac + iPhone借りるなど)が必要
- [ ] **20人スケール対応**
  - 縦線本数の可変化(プレハブ複製、シーン配置調整)
  - participantPlayerIds[] のサイズ拡大とSync制限再検証
  - 横線生成確率の再チューニング
- [ ] **観戦補助機能**
  - 観戦者向け加速ゾーン(誰でも速く走れる場所)
  - 観戦モード: 上空からフリーカメラできるUDONギミック
- [ ] **BGM / SE の追加**
  - エントリー中のBGM
  - カウントダウンSE
  - 横線通過時のSE
  - ゴールファンファーレ
- [ ] **賞品エリアの演出強化**
  - ゴールごとに異なる装飾・ギミック
  - パーティクル演出

## v1.2 (中期)

- [ ] スマホ判定UI(World判定)
- [ ] ランキング機能(個人記録 or インスタンス内ベスト)
- [ ] 「歩き式モード」の追加([ADR-0001](./docs/adr/0001-cart-based-design.md) で言及)
- [ ] イベントギミック
  - ランダム加速ゾーン
  - 一時停止トラップ
  - ボーナスゴール
- [ ] CI整備(Markdownリント、ADR形式チェック、ユニットテスト追加 → `docs/dev-workflow.md` 参照)
  - 含むもの: リポジトリ全体の Markdown テーブルスタイル統一(現状 ADR-0007 等で MD060 警告。データ行は spaced・セパレータ行は compact の混在)、ADR フロントマター形式チェック、ファイル末尾改行など
- [ ] リプレイ機能(直前レースの再生)

## 課題・既知の制約

### 技術的不安要素(Phase着手時に検証する)

- **`Networking.GetServerTimeInSeconds()` のクライアント間誤差**: Phase 3 の2クライアントテストで実測。`CalculateServerDeltaTime` で吸収される想定だが、数十ms規模のズレが UX に影響するか確認([ADR-0003](./docs/adr/0003-precomputed-waypoint-lerp.md))
- **`VRC_Station` の `disableStationExit` 挙動**: VRトリガー退出はSDK仕様通り防げないので、リタイア処理のテストを Phase 2 で実機確認([ADR-0007](./docs/adr/0007-vrcstation-transform-cart.md))
- **`UdonSynced int[4]` の Late Joiner 受信タイミング**: Phase 6 で複数クライアント実機テスト
- **ゴール手前バリアの隙間設計**: カートだけが通れて歩行者は通れない物理形状、Phase 4 で実機調整必要
- **Quest 実機でのパフォーマンス**: Phase 8 で実機 FPS 測定、必要に応じて Tri数・マテリアル数を絞り込み

### 検証済み(以前は不安要素だったもの)

- ~~UdonSharpでの `System.Random` 利用可否~~ → SDK 3.7.1 (2024-09) で利用可能。採用済み

### Open Questions

- ワールド名 (英語タイトル / 日本語サブタイトル) - 未決定
- サムネイル画像のコンセプト - 未決定
- ワールド説明文 - 未決定
- 賞品エリアのテーマ性 - 未決定(v1.0は無装飾でOK)

## アイデアプール (採否未定)

- カートのカスタマイズ(色・形を選べる)
- 観戦者からカートに「応援エモート」を送れる
- 賞品エリアにミニゲーム配置
- 季節イベント装飾 (ハロウィン版あみだくじ等)
- 多言語対応 (EN/JP切替)
- 「あみだくじ巨大化スケール変更」モード(さらに巨大に・ミニチュアに)
