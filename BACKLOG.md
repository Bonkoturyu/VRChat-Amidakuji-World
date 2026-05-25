# Backlog

## v1.0 受け入れ条件 (5/31 公開時点で満たすべき)

判定基準の定義は [docs/SPEC.md §13](./docs/SPEC.md#13-v10-完了の定義) を参照。
以下はステータストラッキング。

- [x] 仕様ロック (docs/SPEC.md)
- [x] ゴール演出仕様決定: 爆発(ハズレ扱い)+ 紙吹雪(祝砲扱い)、seed 由来決定論配置 ([ADR-0012](./docs/adr/0012-goal-effect-randomized.md))
- [x] アーキテクチャ・シーン構成案ロック (docs/architecture.md, docs/scene-structure.md)
- [x] Android 対応スコープ決定 (ADR-0010)
- [x] 観戦システム方針決定: 追いかけ式 (ADR-0009)
- [x] 平面水平レイアウト決定 (ADR-0011)
- [x] Phase 0: VCC環境構築 + GitHub Private Repo + 空ワールド Privateアップロード疎通
- [x] Phase 1: 平面水平あみだくじ床(MainFloor + 縦線・横線、Mobile考慮)の VR HMD 実機確認
- [x] Phase 2: カート単体走行 + 歩行者非衝突レイヤー設定(走行中の干渉確認のみ Phase 3 に持ち越し、停止カートで Layer 設計は検証済み)
- [ ] Phase 3: ランダム生成 + seed同期 (2クライアントで一致確認)
- [x] Phase 4: 4カート同時走行 + 賞品エリアテレポート + ゴール演出(2026-05-21 完了、約3日前倒し。`_ApplyState()` 冪等化で OnDeserialization 高頻度発火問題を解消、演出割当は終点 lane ベースに修正 [ADR-0012 §4](./docs/adr/0012-goal-effect-randomized.md))
- [x] Phase 5: ゲームフロー UI 完成(2026-05-23、Persistence 復元 V9-V11 は Phase 6 で Private アップロード環境にて検証予定)
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
- [ ] **賞品エリアの演出拡張**(v1.0 で爆発・紙吹雪のみ実装済、[ADR-0012](./docs/adr/0012-goal-effect-randomized.md))
  - 演出種別の追加(花火、光柱、紙吹雪の派生など)
  - ゴールごとに異なる固定装飾・ギミック
  - エントリーエリア UI からの「爆発数・紙吹雪数」可変設定(現状は Inspector のみ)
  - 20 レーンスケール時の粒子バジェット再評価

## v1.2 (中期)

- [ ] スマホ判定UI(World判定)
- [ ] ランキング機能(個人記録 or インスタンス内ベスト)
- [ ] 「歩き式モード」の追加([ADR-0001](./docs/adr/0001-cart-based-design.md) で言及)
- [ ] イベントギミック
  - ランダム加速ゾーン
  - 一時停止トラップ
  - ボーナスゴール
- [ ] CI整備(Markdownリント、ADR形式チェック、ユニットテスト追加 → `docs/dev-workflow.md` 参照)
  - 含むもの: `.markdownlint.json` 追加で将来の違反を機械的に防止(MD060 / MD040 / MD031 のリポジトリ全体統一は 2026-05-22 に手作業で解消済み)、ADR フロントマター形式チェック、ファイル末尾改行など
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

### ワールドメタデータ(v1.0 暫定確定 — 2026-05-21)

Phase 10 の「ワールド名・サムネイル・説明文設定」で使用する確定値。Phase 10 着手時に最終調整。

- **World 名**: `Ghost-Leg Express — 巨大あみだくじ`(英語タイトル + 日本語サブ併記)
- **サムネイル**: 斜め上空俯瞰構図(Z=+20, Y=15, X=0、X 軸 -45° あたりから 4 台のカートが縦線を走行中の画)。現状サムネで暫定 OK、Phase 10 で再撮影判断
- **ワールド説明文(暫定)**:

```text
自動巡回カートで体験する、巨大あみだくじワールド。

参加者: スタート位置のカートに座ると、ランダム生成されたあみだくじを自動で巡回。
ゴール先の賞品エリアでは爆発(ハズレ)か紙吹雪(祝砲)がランダム発火します。

観戦者: あみだくじ構造内を自由に走り回り、カートを追いかけて間近で観戦できます。
ゴール手前のバリアより先に行けるのは参加者のみ。

最大 4 人参加 + 観戦者多数 OK / Quest 対応 / PC 対応。

スタートボタンはインスタンスオーナーのみ押下可能。
演出モード(全員ゴール後一斉発火 / 個別到達時即発火)も切り替え可能です。
```

文字数・改行は Phase 10 で VRChat 説明文 UI の実カット位置を見て最終調整。

### Open Questions

- 賞品エリアのテーマ性 - 未決定(v1.0は固定装飾なし、ただし爆発・紙吹雪のゴール演出は実装、[ADR-0012](./docs/adr/0012-goal-effect-randomized.md))
- ゴール演出のタイミングモード - **確定: A 既定 + Master が UI で B 切替可能**、選択は VRChat Player Persistence で永続化(同一人物が再 Master 時に復元、Phase 5 で UI 実装、[ADR-0012](./docs/adr/0012-goal-effect-randomized.md))

## アイデアプール (採否未定)

- カートのカスタマイズ(色・形を選べる)
- 観戦者からカートに「応援エモート」を送れる
- 賞品エリアにミニゲーム配置
- 季節イベント装飾 (ハロウィン版あみだくじ等)
- 多言語対応 (EN/JP切替)
- 「あみだくじ巨大化スケール変更」モード(さらに巨大に・ミニチュアに)
