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
- [x] Phase 3: ランダム生成 + seed同期 (2クライアントで一致確認、2026-05-20 完了、1日前倒し)
- [x] Phase 4: 4カート同時走行 + 賞品エリアテレポート + ゴール演出(2026-05-21 完了、約3日前倒し。`_ApplyState()` 冪等化で OnDeserialization 高頻度発火問題を解消、演出割当は終点 lane ベースに修正 [ADR-0012 §4](./docs/adr/0012-goal-effect-randomized.md))
- [x] Phase 5: ゲームフロー UI 完成(2026-05-23、Persistence 復元 V9-V11 は Phase 6 で Private アップロード環境にて検証予定)
- [x] Phase 6: Late Joiner / エッジケース対応 (PC)(2026-05-30 完了。Late Joiner 3状態 / Master 交代(走行中+Idle)/ 全員退出 / 着座中退出 / Player Persistence V9-V11 全 PASS、A=Quest・B=PC 実環境 + Build & Test 併用)
- [x] Phase 7: Android Platform 切替 + 初期最適化(2026-05-28、Quest 実機 Join + RulesPanel + Cart 着座まで動作確認、Tri 数 Stats と StartButton Proximity は Phase 8 持越し)
- [x] Phase 8: Quest 実機テスト + 調整(2026-05-30 完了。全シーン 70 FPS / 演出・色・音量・パネル高さ実機 OK / 賞品エリア暗さ対策(Baked Point Light)/ クロスプラットフォーム双方向 / Phase 10 通しテスト A〜F 全 PASS)
- [ ] Phase 9: ライティング・最終最適化(PC + Android)
- [x] Phase 10: Community Labs 公開(PC + Android 両ビルド)— **2026-05-30 公開達成**(目標 5/31 に対し1日前倒し、tag `v1.0.0`)

### v1.0 範囲内で追加実装(当初スコープ外、開発中に合流)

- **多言語対応 (EN/JP 切替)** — Phase 6 (`2f3e3f3`) で `LocalizationManager` + `LangToggleButton` を実装。RulesPanel と ResultDisplay の両 UI で JP/EN を動的切替、現状は Local 状態(永続化なし)。当初アイデアプール掲載だったが、RulesPanel Rev.4 制作タイミングで実装コストが小さく合流。
- **カート個人カラー機能** — Phase 6 (`2f3e3f3`) で `ColorPreferenceManager` + `ColorPaletteButton` を実装。MD500 系 8 色パレットを Player Persistence で永続化、`OnPlayerRestored` 初回は `playerId % 8` の決定論既定色。着座中 Cart に `colorIndex` を同期伝播、ゴール時は `PrizeArea._SetWallColor` で壁色も染色([ADR-0012](./docs/adr/0012-goal-effect-randomized.md) の Cart カラーバリエーション拡張枠)。
  - **⚠ 色選択 UI(`ColorPaletteButton`)のシーン配置が欠落していた**(2026-05-29 発見)。Phase 6 ではスクリプトのみ実装され、Tab4 にパレットが配置されておらず、プレイヤーは `playerId % 8` の既定色に固定で**任意選択ができなかった**。Phase 8 で Tab4 に Swatch×8 を配置して機能を完成させる(手順 [docs/phase8-color-palette-checklist.md](./docs/phase8-color-palette-checklist.md)、`RulesPanelController._RefreshColorPalette` は MaterialPropertyBlock 方式に改修済)。

## GitHub リポジトリ Public 化前チェックリスト

リポジトリを Public にする前の確認事項(2026-05-30 監査)。Save→commit→Restore で blueprintId を退避し続けてきた前提での残点検。

- [x] **シーンの blueprintId 退避** — 全コミットがクリーン。シーンの実 blueprintId は履歴に一度も入っていない(`git log --all -S wrld_` で検証、ヒットは下記 dev-workflow.md の例とスクリプト正規表現のみ)
- [x] **dev-workflow.md §11 の blueprintId 例を redact** — 実 ID prefix `wrld_13f1b8a9-...` をプレースホルダ `wrld_xxxx...` に変更(2026-05-30)
- [ ] **(任意・低優先)履歴に残る blueprintId 例の扱い** — 旧版 dev-workflow.md の partial prefix は履歴 `11ea836` に残存。world は Community Labs 公開済で blueprintId は実質非秘匿(公開ワールドの URL/API に出る)・partial のみのため **低リスク。履歴 rewrite はコスト過大で非推奨、受容で可**
- [ ] **README.md を公開向けに拡充**(任意)— 現状は開発ドキュメント索引として機能十分だが、訪問者向けに ①ワールドのスクショ/サムネ ②Community Labs リンク ③リリース状態(v1.0)④技術スタック を足すと親切
- [ ] **.gitignore の Unity 標準除外を最終確認** — Public 化直前に `git ls-files` で Library/Temp/Logs/obj 等の生成物が混入していないか確認(現状混入なし)
- [x] **第三者素材のライセンス表記** — CC0 音源の出所・ライセンスは [audio-assets.md](./docs/audio-assets.md) + [ADR-0013](./docs/adr/0013-audio-assets-and-licensing.md) に記載済、LICENSE(MIT)あり
- [x] **個人ファイル(.local 等)の非追跡** — `opus_startup_prompt.local.md` / `.blueprint-id.local` は gitignore 済・未追跡
- 備考: 全コミットの author email は GitHub 公開アカウントと同一の通常公開情報、対処不要

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
- [ ] **BGM / SE の拡張**(v1.0 で単一ループ BGM + ゴール効果音(当たり/ハズレ)を実装済、[ADR-0013](./docs/adr/0013-audio-assets-and-licensing.md) / [audio-assets.md](./docs/audio-assets.md))
  - 動的BGM切替(待機中 / レース中 / 結果表示中で曲を変える)
  - カウントダウン専用ジングル
  - 横線通過時のSE
  - ゴールファンファーレ(当たり時の特別演出)
  - HoliznaCC0 の別曲で雰囲気バリエーション
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
- ~~**StartButton Proximity が Cart 着座距離に対し狭すぎる**~~(解決方針確定 2026-05-29): proximity を 2→10 に拡大したが、**着座すると視点が真横までしか回らず StartButton に正対できない**(向きの制約で距離問題ではない)と ClientSim で判明。**操作パネル方式**で根本解決 — 各 Cart 着座者の正面 + 中央に START/MODE パネルを配置(計 5)、`GameManager.controlPanels[]` を gameState 連動表示(RUNNING 非表示 / IDLE・RESULT_DISPLAY 表示)。Master のみ操作可。GameManager 改修済、配置手順 [docs/phase8-control-panel-checklist.md](./docs/phase8-control-panel-checklist.md)。中央 StartButton の proximity 10 は立ち操作用に維持
- ~~**Phase 8 で RESULT_DISPLAY 永続化に伴い卓リセットが消えた**~~(解決 2026-05-29): 自動 Idle 復帰廃止で `_OnRaceReset`(Cart 起点復帰・着座枠クリア)が走らず、ゴール後にカートが終点に残り・プレイヤーがリスポーンされない回帰。**Option B** で再導入 — `_EnterResultDisplay` が `resultHoldSeconds`(既定 10 秒)後に `_ReturnToIdle` を予約、IDLE 遷移で卓リセット + 賞品エリア滞在者を起点へテレポート(`_TeleportLocalToSpawnIfInPrize`)。**結果 UI とパネルは次の START まで出したまま**。
  - 残課題(レア): 結果表示中(10 秒)に **Master が退出**すると `_ReturnToIdle` 予約が失われ RESULT_DISPLAY に留まる。Master 移譲先での再予約は未実装。v1.1 で OnOwnershipTransferred 監視を検討

### 検証済み(以前は不安要素だったもの)

- ~~UdonSharpでの `System.Random` 利用可否~~ → SDK 3.7.1 (2024-09) で利用可能。採用済み

### ワールドメタデータ(v1.0 暫定確定 — 2026-05-21)

Phase 10 の「ワールド名・サムネイル・説明文設定」で使用する確定値。Phase 10 着手時に最終調整。

- **World 名**: `巨大あみだくじ / Ghost-Leg Express`(日本語主 + 英語併記。VRChat の日本語ユーザー比率を踏まえ JP 先頭、2026-05-30 確定)
- **タグ(5、SDK 上限)**: `amidakuji` `ghostleg` `game` `party` `quest`(`race` は不採用 — 速さを競うゲームではなく運ゲー/パーティ寄りのため誤解回避、2026-05-30 確定)
- **サムネイル**: 斜め上空俯瞰構図(Z=+20, Y=15, X=0、X 軸 -45° あたりから 4 台のカートが縦線を走行中の画)。現状サムネで暫定 OK、Phase 10 で再撮影判断
- **ワールド説明文(確定版、JP/EN 並記)** — 2026-05-30 採用。**当初の長文版は VRChat 説明欄の文字数上限で `Saving World Changes` が保存できず**、下記の短縮版で保存成功(VRChat の World 説明欄には文字数上限あり、JP+EN 併記は要圧縮):

```text
[ 日本語 ]
自動巡回カートで遊ぶ巨大あみだくじ。カートに座るとランダム生成の道を進み、
ゴールで紙吹雪(当たり)か爆発(ハズレ)が発火。観戦者は自由に走ってカートを追えます。
最大4人 + 観戦多数 / PC・Quest 対応 / 日英UI。スタートはインスタンスオーナーのみ。

[ English ]
A giant Ghost-Leg (Amidakuji) world with auto-riding carts. Sit to ride a random
path; confetti (win) or explosion (miss) fires at the goal. Spectators chase freely.
Up to 4 players + spectators / PC & Quest / JP-EN UI. Owner-only start.
```

VRChat の説明欄は単一テキスト UI なので JP/EN を 1 枠に `[ 日本語 ]` / `[ English ]` ラベルで並記。長文版(演出モード切替の説明等を含む詳細版)は文字数超過で不可だったため、要点に圧縮した上記が最終版。

### Open Questions

- 賞品エリアのテーマ性 - 未決定(v1.0は固定装飾なし、ただし爆発・紙吹雪のゴール演出は実装、[ADR-0012](./docs/adr/0012-goal-effect-randomized.md))
- ゴール演出のタイミングモード - **確定: A 既定 + Master が UI で B 切替可能**、選択は VRChat Player Persistence で永続化(同一人物が再 Master 時に復元、Phase 5 で UI 実装、[ADR-0012](./docs/adr/0012-goal-effect-randomized.md))

## アイデアプール (採否未定)

- **操作パネルの高さ微調整**(2026-05-30 Phase 10 通しテスト所感「操作パネルがやや高い位置かも」。軽微で v1.0 は放置、後日 ControlPanel の Position.y を少し下げる候補)
- **カート名称をカラー名に変更**(現状「カート1〜4」/ "Cart 1-4"、将来「カート赤 / Cart Red」等にしたい、Rev.4 UI 検討時メモ 2026-05-25)
- カートのカスタマイズ(色・形を選べる)
- 観戦者からカートに「応援エモート」を送れる
- 賞品エリアにミニゲーム配置
- 季節イベント装飾 (ハロウィン版あみだくじ等)
- 「あみだくじ巨大化スケール変更」モード(さらに巨大に・ミニチュアに)
