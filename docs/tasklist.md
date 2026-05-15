# Tasklist

Phase別タスク。各 Phase は Build & Test (動作確認) まで含めて「完了」とする。

## Phase 0: 環境構築 [5/15] [0.5日]

- [ ] VCC で新規 World プロジェクト作成 (`amidakuji-world`)
- [ ] VRChat World SDK 3.x 最新版を導入
- [ ] UdonSharp 最新版を導入
- [ ] ClientSim 導入
- [ ] 空シーンでビルドが通ることを確認
- [ ] Private アップロード → メインアカウントで Join 確認
- [ ] VRChat Webサイトでワールド枠が表示されることを確認
- [ ] `git init`、`.gitignore` 配置 (`Library/`, `Temp/`, `obj/`, `*.csproj` 等を除外)
- [ ] `CLAUDE.md`, `docs/` をリポジトリに配置

**完了基準**: 空ワールドでメインアカウントが Join できる + リポジトリが整っている

## Phase 1: 静的あみだくじ構造 [5/16-5/17] [2日]

- [ ] ProBuilder で縦通路ブロックを Prefab 化
- [ ] 縦線4本を間隔 4m で並べる(Y方向 60m)
- [ ] 横線(連絡通路)を全パターン分配置(disable状態)
- [ ] スタート/ゴール位置にマーカーオブジェクト配置
- [ ] スポーンエリア仮配置
- [ ] エントリーエリア仮配置(座席位置のマーカーのみ)
- [ ] 観戦デッキ仮配置(ガラス床)
- [ ] 賞品エリア4ゾーン仮配置(ただの箱でOK)
- [ ] スケール感を VR HMD で実機確認

**完了基準**: シーン内を歩き回ってあみだくじ構造全体を体感できる

## Phase 2: カート単体走行 [5/18-5/20] [3日] ★山1

- [ ] Cart Prefab 作成 (Visualモデル + Collider + VRC_Station)
- [ ] VRC_Station 設定 (`disableStationExit`, `mobilityType` 等)
- [ ] `CartController.cs` (UdonSharp) 実装
  - [ ] Waypoint配列を Inspector で設定
  - [ ] Update() で時刻ベース Lerp 補間
- [ ] 着座すると固定経路を巡回するテスト
- [ ] ClientSim で確認
- [ ] Build & Test で実際にHMDで着座テスト

**完了基準**: 1人がカートに着座し、固定経路を最後まで自動巡回できる

## Phase 3: ランダム生成 + seed同期 [5/21-5/23] [3日] ★山2

- [ ] xorshift32 PRNG ヘルパー実装(System.Random 利用可否確認 → 不可なら自前)
- [ ] `AmidakujiGenerator.cs` (UdonSharp) 実装
  - [ ] seedから横線配置を決定論的に算出
  - [ ] 連続横線禁止ロジック
  - [ ] HasBar(lane, segment) API
- [ ] 横線GameObject群を生成結果に応じて enable/disable
- [ ] CartController に「seed + 開始縦線番号 → Waypoint配列」算出ロジック追加
- [ ] `GameManager.cs` (UdonSharp) 実装
  - [ ] UdonSynced: `seed`, `gameState`, `raceStartTime`
  - [ ] `OnDeserialization` 処理
- [ ] スタートボタン (仮) を実装
- [ ] Build & Test (2クライアント) で同じ横線配置・同じ経路が見えるか確認

**完了基準**: Master側でスタート → 別クライアントから見ても同じあみだくじ・同じ経路でカートが走る

## Phase 4: 複数カート同時走行 [5/24-5/25] [2日]

- [ ] Cart 4台に増やす
- [ ] 各カートに座席番号 (0-3) を持たせる
- [ ] GameManager に `participantPlayerIds[4]` を追加
- [ ] 着座イベントで参加者登録(Ownership transfer)
- [ ] スタート時、全カートが同時に走行開始
- [ ] ゴール到達時、賞品エリアへ `TeleportTo`
- [ ] Build & Test で4台同時走行を確認

**完了基準**: 4人 (またはMaster1人+残ダミー) で同時にゴールまで走り、各自テレポートされる

## Phase 5: RenderTextureスクリーン [5/26] [1日]

- [ ] 俯瞰カメラ GameObject 配置 (Orthographic, あみだくじ真上)
- [ ] RenderTexture アセット作成 (1280×720)
- [ ] スクリーン Quad 配置、Material (Unlit/Texture) に RT 割当
- [ ] Culling Mask で観戦者・UIレイヤー除外
- [ ] Clear Flags = Solid Color に設定
- [ ] エントリーエリアからの視認性確認

**完了基準**: スクリーンに俯瞰映像が表示され、カートの動きが追える

## Phase 6: 観戦デッキ整備 [5/27] [1日]

- [ ] ガラス床マテリアル調整(透明度・反射)
- [ ] 観戦デッキの手すり・装飾
- [ ] スポーンエリアからの動線確認
- [ ] エントリーエリアとの境界(走行中は入れない誘導)

**完了基準**: 観戦者が直接俯瞰でカートを見られる、危なくない、迷わない

## Phase 7: ゲームフロー UI [5/28] [1日]

- [ ] ステートマシン実装 (Idle/Countdown/Running/ResultDisplay)
- [ ] スタートボタン: Master判定、参加者0人時の無効化
- [ ] カウントダウン演出 (3-2-1)
- [ ] 結果表示 (スクリーンに「席n → ゴールm」)
- [ ] 着座制御 (Idle中のみ可)
- [ ] ResultDisplay → Idle 自動遷移 (10秒)

**完了基準**: 一連の流れがUI操作だけで回せる

## Phase 8: Late Joiner / エッジケース [5/29] [1日]

- [ ] Late Joiner テスト: Idle中・Running中・ResultDisplay中それぞれで途中参加
- [ ] Master交代テスト: 走行中にMasterが退出
- [ ] 全員退出テスト
- [ ] 着座中の人がインスタンスを抜けた場合
- [ ] ルール説明パネル設置

**完了基準**: 想定エッジケースで全てクラッシュ・状態不整合が起きない

## Phase 9: ライティング・最適化 [5/30] [1日]

- [ ] Mixed Lighting ベイク
- [ ] Light Probe 配置
- [ ] Reflection Probe 配置
- [ ] Occlusion Culling ベイク
- [ ] Static Batching 有効化
- [ ] DrawCall, Triangle数を Stats で確認、バジェット内に収める
- [ ] VRChat SDK のワールド分析でランクGood確認
- [ ] スカイボックス、Post Processing 軽く

**完了基準**: PC Good ランク、VR で 45 FPS 以上

## Phase 10: 最終テスト & 公開 [5/31]

- [ ] 多人数 (可能なら4人) で通しテスト
- [ ] ルール説明パネル最終チェック
- [ ] ワールド名・サムネイル・説明文設定
- [ ] Private で最終アップロード
- [ ] 友人にDM、Private インスタンスで動作確認
- [ ] **Community Labs 公開ボタン押下** 🎉
- [ ] v1.0 完了タグを git に打つ

**完了基準**: 一般の VRChat ユーザー (Community Labs オプトイン者) がワールドを訪問可能
