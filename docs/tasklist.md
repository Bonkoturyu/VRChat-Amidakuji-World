# Tasklist

Phase別タスク。各 Phase は Build & Test (動作確認) まで含めて「完了」とする。

## Phase 0: 環境構築 [5/15] [0.5日] ✅ 完了 (2026-05-16 確認)

- [x] VCC で新規 World プロジェクト作成 (`amidakuji-world`)
- [x] VRChat World SDK 3.x 最新版を導入 (3.10.3)
- [x] UdonSharp 最新版を導入 (Worlds SDK に `legacyPackages` として統合)
- [x] ClientSim 導入 (同上、Worlds SDK に統合)
- [x] **Android Build Support を Unity に追加**(Build Settings で Switch Platform 可、SDK/JDK パス設定は Phase 7 で対応)
- [x] 空シーンでビルドが通ることを確認
- [x] Private アップロード → メインアカウントで Join 確認(Windows)
- [x] VRChat Webサイトでワールド枠が表示されることを確認
- [x] `git init`、`.gitignore`、`.gitattributes` 配置
- [x] `git lfs install` で LFS 有効化 (`.gitattributes` に LFS フィルタ設定済み)
- [x] GitHub Private リポジトリ作成、初回 push
- [x] `CLAUDE.md`, `docs/`, `LICENSE` をリポジトリに配置
- [x] (追加) blueprintId 退避/復元ワークフロー (`scripts/Save-BlueprintId.ps1` / `Restore-BlueprintId.ps1` + `.blueprint-id.local`)

**完了基準**: 空ワールドでメインアカウントが Join できる + リポジトリが整っている

## Phase 1: 静的あみだくじ構造 [5/16-5/17] [2日]

レイアウト: **平面水平**([ADR-0011](./adr/0011-flat-horizontal-layout.md))。
Unity GUI 操作の確定値は [docs/phase1-prefab-checklist.md](./phase1-prefab-checklist.md)、
マテリアル定義は [docs/material-set.md](./material-set.md) を参照。
`ProjectSettings/TagManager.asset` と `DynamicsManager.asset` は更新済み(Cart=User22, GoalBarrier=User23 と衝突 Matrix)。

- [x] **MainFloor** を Primitive Cube で配置(Scale **16**×0.2×80、上面 Y=0) — 2026-05-16
- [x] **VerticalLine.prefab** を Primitive Cube で作成(細い線、Scale 0.2×0.02×60)、VLine_0〜3 配置 — 2026-05-16
- [x] **HorizontalBar.prefab** を Primitive Cube で作成(Scale 4×0.02×0.2)、全 **33** 本配置(S00〜S10 × 3 ペア、全 active 状態) — 2026-05-16
- [ ] スタート/ゴール位置にマーカー Empty GameObject 配置 — Phase 2 着手時に Cart_N と GoalBarrier_N の Transform を参照する形で対応予定(別 Empty 不要)
- [x] **ゴール手前バリアの仮配置**(カート用隙間付き、Z=-58.5、各バリア 4m 幅で MainFloor 全幅を密に塞ぐ) — 2026-05-16
- [x] DefaultSpawn 配置(Position (0, 0.1, +10))、Respawn Height Y=-1 — 2026-05-16
- [x] エントリーエリア仮配置(MainFloor 上の Seats 4 つ + StartButton 仮 + RulesPanel 仮 + ResultDisplay 仮) — 2026-05-16
- [x] 賞品エリア4ゾーン仮配置(Z=-64 の小部屋 4 つ、3.5m 幅で互いに 0.5m 隙間、PrizeArea_0〜3) — 2026-05-16
- [ ] スケール感を VR HMD で実機確認(歩行体験フラットか、線の段差を感じないか、GoalBarrier 隙間を歩行者が通れないか) — 2026-05-17 予定
- [x] **マテリアルは初手から `VRChat/Mobile/Standard Lite` 系で組む**(Android対応のため。Phase 1 はテクスチャ無し・色のみのプレースホルダで OK。詳細は [material-set.md](./material-set.md)) — 2026-05-16 完了、11 個作成済み(M_UI_Display は Phase 5 で対応、`M_Post_Track` は `M_Line` にリネーム + 白に変更済み)

**完了基準**: 平面床(Z=+12 〜 Z=-68)を端から端まで歩ける、あみだくじの線が床面に描かれて見える、GoalBarrier の向こうに歩行者は侵入できない、床外に出ると自動リスポーンされる

## Phase 2: カート単体走行 [5/18-5/20] [3日] ★山1

- [x] Cart Prefab 作成 (Visualモデル + Collider + VRC_Station) — Phase 1 で先行完了(2026-05-16)
- [x] VRC_Station 設定 (`disableStationExit=false`, `PlayerMobility=Immobilize (For Vehicle)`, `Seated=true`) — Phase 1 で先行完了、2026-05-17 改訂([ADR-0007](./adr/0007-vrcstation-transform-cart.md))
- [x] **Layer 設定**: カートと歩行者の衝突分離(Cart=User22 レイヤー作成、PlayerLocal と分離) — Phase 0/1 で完了
- [ ] `CartController.cs` (UdonSharp) 実装
  - [ ] **Inspector フィールド**: `laneIndex / speed=2.0 / station` (Common) + `startOnEnter=true / lookAtMovingDirection=false / waypointMarkers Transform[]` (Phase 2 暫定、Phase 3 で削除/置換)
  - [ ] **ローカル状態**: `_state (Idle/Running/Goaled) / _raceStartTime / _waypoints / _cumulativeDist / _totalDuration / _isLocalSeated / _isExitingByGoal`(全て private、同期不要)
  - [ ] `Start()`: `waypointMarkers` から `_waypoints` / `_cumulativeDist` / `_totalDuration` を構築(Phase 3 で `ComputePath(seed, lane)` に置換)
  - [ ] `Update()`: `_state==Running` のとき `Networking.CalculateServerDeltaTime` で時刻ベース Lerp、`transform.position` 更新
  - [ ] `lookAtMovingDirection==true` のとき `Quaternion.LookRotation(進行方向)` を適用(デフォルト OFF、速度 2.0 m/s で視点動が大きく酔いやすいため)
  - [ ] `OnStationEntered`: ローカルプレイヤーなら `_isLocalSeated = true` + `startOnEnter` 真なら `StartRace()`
  - [ ] `OnStationExited`: `_isLocalSeated = false` + `HandleExit(player)`(Phase 2 では `_isExitingByGoal` 常に false なので必ずリタイア処理)
  - [ ] **`InputJump` イベントハンドラ**: `value && _isLocalSeated` なら `station.ExitStation(LocalPlayer)` → 結果 `OnStationExited` に流れリタイア処理([ADR-0007](./adr/0007-vrcstation-transform-cart.md) 2026-05-17 追記)
  - [ ] **Phase 2 は UdonSynced 変数 0 個**(同期は Phase 3 で GameManager 実装時に導入。Phase 2 はローカル単独走行のテストに集中)
- [ ] 着座すると固定経路を巡回するテスト
- [ ] **走行中に歩行者がカートをすり抜けられるか確認**(Layer 設定の検証)
- [ ] **3 種の退出経路すべてが動作することを確認**: ①VR トリガー、②Desktop 移動入力(WASD/スティック)、③Desktop Space キー(InputJump 実装後)
- [ ] ClientSim で確認
- [ ] Build & Test で実際にHMDで着座テスト

**完了基準**: 1人がカートに着座し、固定経路を最後まで自動巡回、別の人が走って追いかけてもカートと干渉しない。3 種の退出経路すべてがリタイア扱いで処理される

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
