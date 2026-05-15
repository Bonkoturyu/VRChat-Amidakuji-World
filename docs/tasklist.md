# Tasklist

Phase別タスク。各 Phase は Build & Test (動作確認) まで含めて「完了」とする。

## Phase 0: 環境構築 [5/15] [0.5日]

- [ ] VCC で新規 World プロジェクト作成 (`amidakuji-world`)
- [ ] VRChat World SDK 3.x 最新版を導入
- [ ] UdonSharp 最新版を導入
- [ ] ClientSim 導入
- [ ] **Android Build Support を Unity に追加**(Quest対応のため、後のPhase 7で必要)
- [ ] 空シーンでビルドが通ることを確認
- [ ] Private アップロード → メインアカウントで Join 確認(Windows)
- [ ] VRChat Webサイトでワールド枠が表示されることを確認
- [ ] `git init`、`.gitignore`、`.gitattributes` 配置
- [ ] `git lfs install` で LFS 有効化
- [ ] GitHub Private リポジトリ作成、初回 push
- [ ] `CLAUDE.md`, `docs/`, `LICENSE` をリポジトリに配置

**完了基準**: 空ワールドでメインアカウントが Join できる + リポジトリが整っている

## Phase 1: 静的あみだくじ構造 [5/16-5/17] [2日]

- [ ] ProBuilder で縦通路ブロックを Prefab 化(歩行可能な床つき)
- [ ] 縦線4本を間隔 4m で並べる(Y方向 60m)
- [ ] 横線(連絡通路)を全パターン分配置(disable状態)
- [ ] スタート/ゴール位置にマーカーオブジェクト配置
- [ ] **ゴール手前バリアの仮配置**(カート用隙間付き)
- [ ] スポーンエリア仮配置
- [ ] エントリーエリア仮配置(座席位置のマーカーのみ、観戦デッキは無し)
- [ ] 賞品エリア4ゾーン仮配置(ただの箱でOK)
- [ ] スケール感を VR HMD で実機確認
- [ ] **マテリアルは初手から `Mobile/VRChat/Lightmapped` 系で組む**(Android対応のため)

**完了基準**: シーン内を歩き回ってあみだくじ構造全体を体感できる、観戦者として上から下まで歩行可能

## Phase 2: カート単体走行 [5/18-5/20] [3日] ★山1

- [ ] Cart Prefab 作成 (Visualモデル + Collider + VRC_Station)
- [ ] VRC_Station 設定 (`disableStationExit`, `mobilityType=Mobile`, `Seated=true` 等)
- [ ] **Layer 設定**: カートと歩行者の衝突分離(Cart レイヤー作成、PlayerLocal と分離)
- [ ] `CartController.cs` (UdonSharp) 実装
  - [ ] Waypoint配列を Inspector で設定
  - [ ] Update() で時刻ベース Lerp 補間
- [ ] 着座すると固定経路を巡回するテスト
- [ ] **走行中に歩行者がカートをすり抜けられるか確認**(Layer 設定の検証)
- [ ] ClientSim で確認
- [ ] Build & Test で実際にHMDで着座テスト

**完了基準**: 1人がカートに着座し、固定経路を最後まで自動巡回、別の人が走って追いかけてもカートと干渉しない

## Phase 3: ランダム生成 + seed同期 [5/21-5/23] [3日] ★山2

- [ ] `AmidakujiGenerator.cs` (UdonSharp) 実装
  - [ ] `System.Random(seed)` で横線配置を決定論的に算出
  - [ ] 連続横線禁止ロジック
  - [ ] HasBar(lane, segment) API
- [ ] 横線GameObject群を生成結果に応じて enable/disable
- [ ] CartController に「seed + 開始縦線番号 → Waypoint配列」算出ロジック追加
- [ ] `GameManager.cs` (UdonSharp) 実装
  - [ ] UdonSynced: `seed`, `gameState`, `raceStartTime`
  - [ ] `OnDeserialization` 処理
- [ ] スタートボタン (仮) を実装
- [ ] Build & Test (2クライアント) で同じ横線配置・同じ経路が見えるか確認
- [ ] `CalculateServerDeltaTime` を使った時刻計算の動作確認

**完了基準**: Master側でスタート → 別クライアントから見ても同じあみだくじ・同じ経路でカートが走る

## Phase 4: 複数カート同時走行 + ゴール処理 [5/24-5/25] [2日]

- [ ] Cart 4台に増やす
- [ ] 各カートに座席番号 (0-3) を持たせる
- [ ] GameManager に `participantPlayerIds[4]` を追加
- [ ] 着座イベントで参加者登録(Ownership transfer)
- [ ] スタート時、全カートが同時に走行開始
- [ ] **ゴール手前バリアの最終形状調整**(カートだけ通って人は通れない物理サイズ検証)
- [ ] ゴール到達時、座っているプレイヤーを賞品エリアへ `TeleportTo`
- [ ] **観戦者がバリアを越えられないことを確認**
- [ ] Build & Test で4台同時走行を確認

**完了基準**: 4人 (またはMaster1人+残ダミー) で同時にゴールまで走り、各自テレポートされる。非参加者は賞品エリアに入れない

## Phase 5: ゲームフロー UI [5/26] [1日]

- [ ] ステートマシン実装 (Idle/Countdown/Running/ResultDisplay)
- [ ] スタートボタン: Master判定、参加者0人時の無効化
- [ ] カウントダウン演出 (3-2-1)
- [ ] 結果表示掲示(エントリーエリアの掲示UI、「席n → ゴールm」)
- [ ] 着座制御 (Idle中のみ可)
- [ ] ResultDisplay → Idle 自動遷移 (10秒)

**完了基準**: 一連の流れがUI操作だけで回せる

## Phase 6: Late Joiner / エッジケース (PC) [5/27] [1日]

- [ ] Late Joiner テスト: Idle中・Running中・ResultDisplay中それぞれで途中参加
- [ ] Master交代テスト: 走行中にMasterが退出
- [ ] 全員退出テスト
- [ ] 着座中の人がインスタンスを抜けた場合
- [ ] VRトリガーで走行中に退出した場合のリタイア処理
- [ ] ルール説明パネル設置(追いかけ式観戦の説明含む)

**完了基準**: 想定エッジケースで全てクラッシュ・状態不整合が起きない

## Phase 7: Android Platform 切替 + 初期最適化 [5/28] [1日] ★山3

- [ ] VCC SDK Control Panel で Build Platform を Android に切替
- [ ] **再インポート完了まで待機**(プロジェクトサイズによっては数十分かかる)
- [ ] SDK のバリデーションメッセージを確認・対応
- [ ] マテリアル数を Stats でカウント、20以下に絞る
- [ ] テクスチャを 1024×1024 以下に調整
- [ ] Tri 数を Stats で確認、250,000 以下に
- [ ] GPU Instancing を全マテリアルで有効化
- [ ] Android 向け Private アップロード成功

**完了基準**: Android プラットフォームでビルドが通り、Quest 実機で Join できる

## Phase 8: Quest 実機テスト + 調整 [5/29] [1日]

- [ ] Quest 実機で全機能を動作確認
  - [ ] 着座 → カート走行 → ゴールテレポート
  - [ ] 観戦者として走り回って追いかける(物理FPSが体験に十分か)
  - [ ] ゴール手前バリア突破不可
  - [ ] Late Joiner: Quest からPC instance への参加
  - [ ] PC instance への Quest 参加 + 逆方向
- [ ] パフォーマンス問題があれば追加最適化
  - [ ] FPS 60 未満ならテクスチャ・Tri 削減
  - [ ] DrawCall 多すぎなら Static Batching 確認

**完了基準**: Quest 実機で全体験が 60 FPS 以上、機能差なし

## Phase 9: ライティング・最終最適化 [5/30] [1日]

- [ ] PC 版: Mixed Lighting ベイク
- [ ] Light Probe 配置
- [ ] Reflection Probe 配置
- [ ] Occlusion Culling ベイク
- [ ] Static Batching 有効化
- [ ] PC + Android 両方で DrawCall, Triangle数を Stats で確認、バジェット内に収める
- [ ] VRChat SDK のワールド分析で両プラットフォーム Good ランク確認
- [ ] スカイボックス、Post Processing 軽く(モバイルでは Post Processing 控えめ)

**完了基準**: PC + Android 両プラットフォームで Good ランク、Quest 実機 60 FPS 以上

## Phase 10: 最終テスト & 公開 [5/31]

- [ ] 多人数 (可能なら4人) で通しテスト(PC + Quest 混在)
- [ ] ルール説明パネル最終チェック
- [ ] ワールド名・サムネイル・説明文設定
- [ ] PC版 Private アップロード
- [ ] Android版 Private アップロード(同じ Blueprint ID)
- [ ] 友人にDM、Private インスタンスで動作確認
- [ ] **Community Labs 公開ボタン押下** 🎉
- [ ] v1.0 完了タグを git に打つ (`v1.0.0`)

**完了基準**: 一般の VRChat ユーザー (Community Labs オプトイン者) がワールドを訪問可能(PC + Quest 両対応)
