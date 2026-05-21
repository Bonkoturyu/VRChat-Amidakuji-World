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
- [x] スタート/ゴール位置にマーカー Empty GameObject 配置 — `_World/WaypointMarkers/Cart0Path` に WP_0〜4 配置済(commit `b8c7103`)。当初「別 Empty 不要」想定だったが、CartController が `waypointMarkers Transform[]` を要求するため Empty で実装した
- [x] **ゴール手前バリアの仮配置**(カート用隙間付き、Z=-58.5、各バリア 4m 幅で MainFloor 全幅を密に塞ぐ) — 2026-05-16
- [x] DefaultSpawn 配置(Position (0, 0.1, +10))、Respawn Height Y=-1 — 2026-05-16
- [x] エントリーエリア仮配置(MainFloor 上の Seats 4 つ + StartButton 仮 + RulesPanel 仮 + ResultDisplay 仮) — 2026-05-16
- [x] 賞品エリア4ゾーン仮配置(Z=-64 の小部屋 4 つ、3.5m 幅で互いに 0.5m 隙間、PrizeArea_0〜3) — 2026-05-16
- [x] スケール感を VR HMD で実機確認(歩行体験フラットか、線の段差を感じないか、GoalBarrier 隙間を歩行者が通れないか) — 2026-05-17 完了、結果は commit `f5b6fc8` で spawn/cart rotation + Station 退出方針に反映
- [x] **マテリアルは初手から `VRChat/Mobile/Standard Lite` 系で組む**(Android対応のため。Phase 1 はテクスチャ無し・色のみのプレースホルダで OK。詳細は [material-set.md](./material-set.md)) — 2026-05-16 完了、11 個作成済み(M_UI_Display は Phase 5 で対応、`M_Post_Track` は `M_Line` にリネーム + 白に変更済み)

**完了基準**: 平面床(Z=+12 〜 Z=-68)を端から端まで歩ける、あみだくじの線が床面に描かれて見える、GoalBarrier の向こうに歩行者は侵入できない、床外に出ると自動リスポーンされる

## Phase 2: カート単体走行 [5/18-5/20] [3日] ★山1 ✅ 完了 (2026-05-18)

- [x] Cart Prefab 作成 (Visualモデル + Collider + VRC_Station) — Phase 1 で先行完了(2026-05-16)
- [x] VRC_Station 設定 (`disableStationExit=false`, `PlayerMobility=Immobilize (For Vehicle)`, `Seated=true`) — Phase 1 で先行完了、2026-05-17 改訂([ADR-0007](./adr/0007-vrcstation-transform-cart.md))
- [x] **Layer 設定**: カートと歩行者の衝突分離(Cart=User22 レイヤー作成、PlayerLocal と分離) — Phase 0/1 で完了
- [x] `CartController.cs` (UdonSharp) 実装 — commit `b8c7103` (2026-05-17)、サブ項目はすべて仕様通り実装済
  - [x] **Inspector フィールド**: `laneIndex / speed=2.0 / station` (Common) + `startOnEnter=true / lookAtMovingDirection=false / waypointMarkers Transform[]` (Phase 2 暫定、Phase 3 で削除/置換)
  - [x] **ローカル状態**: `_state (Idle/Running/Goaled) / _raceStartTime / _waypoints / _cumulativeDist / _totalDuration / _isLocalSeated / _isExitingByGoal`(全て private、同期不要)
  - [x] `Start()`: `waypointMarkers` から `_waypoints` / `_cumulativeDist` / `_totalDuration` を構築(Phase 3 で `ComputePath(seed, lane)` に置換)
  - [x] `Update()`: `_state==Running` のとき `Networking.CalculateServerDeltaTime` で時刻ベース Lerp、`transform.position` 更新
  - [x] `lookAtMovingDirection==true` のとき `Quaternion.LookRotation(進行方向)` を適用(デフォルト OFF、速度 2.0 m/s で視点動が大きく酔いやすいため)
  - [x] `OnStationEntered`: ローカルプレイヤーなら `_isLocalSeated = true` + `startOnEnter` 真なら `StartRace()`
  - [x] `OnStationExited`: `_isLocalSeated = false` + `HandleExit(player)`(Phase 2 では `_isExitingByGoal` 常に false なので必ずリタイア処理)。`HandleExit` の具体動作は **`_state = Idle` + `transform.position` を `_waypoints[0]` (起点) に戻す**(1 回の Build & Test で複数回乗降テスト可能にするため)。Phase 4 で `participantPlayerIds[laneIndex] = -1` + 空席走行継続に置換
  - [x] **`InputJump` イベントハンドラ**: `value && _isLocalSeated` なら `station.ExitStation(LocalPlayer)` → 結果 `OnStationExited` に流れリタイア処理([ADR-0007](./adr/0007-vrcstation-transform-cart.md) 2026-05-17 追記)
  - [x] **Phase 2 は UdonSynced 変数 0 個**(同期は Phase 3 で GameManager 実装時に導入。Phase 2 はローカル単独走行のテストに集中)
- [x] **Cart Prefab 構造再編** — commit `b8c7103`: VRC_Station を Seat 子 → Cart Root に移動(`OnStationEntered` 受信のため)、Cart Root に Box Collider(IsTrigger=true)追加、Cart Root と Seat の Layer を Default に変更、`canUseStationFromStation=false` に修正、`_World/WaypointMarkers/Cart0Path/WP_0〜4` を Cart_0 にアサイン
- [x] **Use interaction 発火問題の解消** — 2026-05-18: 真因は **VRC_Station と UdonBehaviour 同居構成では `Interact()` 未実装だと Use 表示が出ない** という VRChat 仕様([ADR-0007](./adr/0007-vrcstation-transform-cart.md) 改訂)。`CartController.Interact()` で `station.UseStation(LocalPlayer)` を呼ぶ実装に修正し解消。当初仮説の「Cart Layer (User22) 戦略起因」は誤り(Visual/Body は Collider を持たないため interaction Raycast に影響しない)
- [x] 着座すると固定経路を巡回するテスト — 2026-05-18 Build & Test で wp[0] (-6,0,2) → wp[last] (-6,0,-58.5) を totalDuration=32.6s で完走確認
- [x] **歩行者がカートをすり抜けられるか確認**(Layer 設定の検証) — 2026-05-18 2クライアント Build & Test で **停止カート** に対するすり抜けは ✅(Cart Root IsTrigger=true + Visual/Body Collider 無しの設計が機能)。**走行中の干渉**確認は Phase 2 段階では UdonSynced 未実装で他クライアントから走行が見えないため不可、Phase 3 (seed 同期実装後) で正式確認
- [x] **4 種の退出経路すべてが動作することを確認** — 2026-05-18 完了:
  - ①VR トリガー: ✅ Build & Test (HMD)
  - ②Desktop 移動入力 (WASD/スティック): ✅ Build & Test (`output_log_2026-05-18_17-20-21.txt` の OnStationExited 17:21:40 / 17:21:44)
  - ③Desktop Space キー (InputJump): ✅ Build & Test
  - ④VR ジャンプボタン (InputJump): ✅ Build & Test (HMD)
- [x] ClientSim で確認 — 2026-05-18 完了: 当初発火しなかった原因は同一(`Interact()` 未実装)、本ブロッカー解消と同時解決。着座 / 走行 / WASD 退出 / Jump 退出すべて動作確認
- [x] Build & Test で実際にHMDで着座テスト — 2026-05-18 完了(着座 → 走行 → ゴール到達まで観察、`output_log_2026-05-18_17-20-21.txt`)

**完了基準**: 1人がカートに着座し、固定経路を最後まで自動巡回、別の人が走って追いかけてもカートと干渉しない。4 種の退出経路すべてがリタイア扱いで処理される
**完了状況 (2026-05-18)**: 着座・自動巡回・退出 4 種・停止カートすり抜け = ✅。走行中干渉は Phase 3 持ち越し(Phase 2 段階では他クライアントから走行が見えない設計のため検証不能)

## 準備期間: ゴール演出 Prefab 制作 [5/19-5/20] ✅ 完了 (2026-05-19)

Phase 4 で配線するため、演出 Prefab 本体を先行制作。判断根拠は [ADR-0012](./adr/0012-goal-effect-randomized.md)、確定値は [phase4-effect-prefab-checklist.md](./phase4-effect-prefab-checklist.md)。

- [x] **マテリアル 3 個追加** — `M_FX_Explosion_Fireball` / `M_FX_Explosion_Smoke` / `M_FX_Confetti`、`VRChat/Mobile/Particles/Additive`・`Multiply` の実物理仕様(Tint Color / GPU Instancing プロパティなし)を [material-set.md §1脚注 / §2.3](./material-set.md) に反映済 — commit `5aeae24`
- [x] **ExplosionEffect.prefab** 作成 — `Assets/_Project/Prefabs/Effects/`、[§1](./phase4-effect-prefab-checklist.md#1-explosioneffectprefab) の Inspector 値どおり
- [x] **ConfettiEffect.prefab** 作成 — 同上、[§2](./phase4-effect-prefab-checklist.md#2-confettieffectprefab) の Inspector 値どおり
- [x] 既存 `PrizeArea.prefab` 4 部屋すべてにネスト Prefab として組込み — [§3](./phase4-effect-prefab-checklist.md#3-prizearea-prefab-への組み込み)
- [x] `_Managers/GameManager` 配下に `FinaleSharedAudio` (2D AudioSource) を配置 — [§4](./phase4-effect-prefab-checklist.md#4-gamemanager-直下-finalesharedaudioa-モード共通-se)
- [x] AudioClip は Phase 8 で差し替え予定、現状は空クリップ運用
- [x] ClientSim で見映え確認 — 概観確認済。粒子高さ(火球 4-6m / 煙 6-8m / 紙吹雪 10m)と観戦距離 34 m からの視認性の最終判定は [Phase 8 Quest 実機判定](#phase-8-quest-実機テスト--調整-529-1日) に統合(2026-05-19 方針確定、HMD 110° 視野角での迫力評価が PC モニタでは代替不能なため)

**完了基準**: [phase4-effect-prefab-checklist.md §7](./phase4-effect-prefab-checklist.md#7-phase-4-着手準備の完了基準) のチェックリストをすべて満たす — ✅ 達成

## Phase 3: ランダム生成 + seed同期 [5/21-5/23] [3日] ★山2 ✅ 完了 (2026-05-20、1日前倒し)

**着手前設計確定(2026-05-19、論点 ①〜④)**:

- 横線生成: 重み付き 5 パターン抽選で各 pair 出現確率 30% 均一化(詳細 [ADR-0002](./adr/0002-deterministic-rng-seed-sync.md))
- `gameState`: Phase 3 では `Idle=0 / Running=2` の 2 値(`Countdown=1` は Phase 5 で挿入する予約番号、`ResultDisplay=3` は Phase 4-5 で追加)
- seed 再現性: GameManager Inspector に `useDebugSeed (bool)` + `debugSeed (int)` 追加(Minecraft 風固定再現用)
- ComputePath waypoint 配列: `new Vector3[24]` 安全側固定確保(横線 11 段全渡り上限 = 起点1 + 横線2×11 + 終点1)

**実装中の方針変更(2026-05-20、Build & Test で V2 不具合判明)**:

- CartController を「状態 polling」設計から **「イベント駆動」** に変更。Joiner 側で `Update()` polling だと `AmidakujiGenerator.Rebuild()` と `CartController.ComputePath()` の順序が不安定になり、横線未初期化(`_initialized=false`)のまま経路計算され「直進ルート」になる回帰があった。`GameManager._ApplyState()` 内で `generator.Rebuild()` → `carts[i]._OnRaceStarted()` を同フレーム同期実行する設計に修正(commit `d316e59` の前に発見、修正コミットで対応)。CartController から `_lastGameState` フィールドと `Update()` 内の gameState 変化検知ロジックを削除し、`_OnRaceStarted()` / `_OnRaceReset()` public メソッドを追加。
- StartButton GameObject 自身に **Box Collider が必要** と判明。VRChat の Interact レイキャストは UdonBehaviour と同じ GameObject 上の Collider しか見ない仕様で、Phase 1 配置時は Visual の子に Collider があったが親 StartButton の `Interact()` が発火しなかった。`Is Trigger=ON, Size 0.6×0.6×0.3` で追加(commit `d316e59`)。

実装サブタスク:

- [x] `AmidakujiGenerator.cs` (UdonSharp) 実装
  - [x] `System.Random(seed).Next(0, 10)` を seg ごとに 1 回呼び、重み付き 5 パターンから抽選(重み比 `(2, 2, 3, 2, 1)`、[ADR-0002 §横線生成の決定論性確保](./adr/0002-deterministic-rng-seed-sync.md))
  - [x] 横線 33 個の `SetActive(true/false)` 切替
  - [x] `HasBar(seg, lanePair) → bool` および `HasBarForLane(seg, lane) → {-1, 0, +1}` API
  - [x] Inspector: `horizontalBars[]` (33 個、`lanePair * SEGMENT_COUNT + seg` 順)
  - [x] 定数: `LANE_COUNT=4`, `LANE_PAIR_COUNT=3`, `SEGMENT_COUNT=11`, `LANE_X[4]={-6,-2,+2,+6}`, `SEG_Z[11]={-3..-53}`, `TOP_Y=+2`, `BOTTOM_Y=-58.5`
- [x] `CartController.cs` 改修
  - [x] **削除**: `startOnEnter` / `lookAtMovingDirection` / `waypointMarkers` (Phase 2 暫定)
  - [x] **追加**: `gameManager` / `generator` 参照
  - [x] `ComputePath(seed, laneIndex)` 実装([ADR-0003](./adr/0003-precomputed-waypoint-lerp.md) 擬似コード準拠、安全側 `Vector3[24]` 固定確保)
  - [x] ~~`Update()` で `gameState` 変化検知~~ → **イベント駆動に変更**:`_OnRaceStarted()` / `_OnRaceReset()` public メソッドを GameManager から同フレーム呼出(上記方針変更参照)
  - [x] ~~`_lastGameState` ローカル変数~~ → 削除(イベント駆動化に伴う不要化)
- [x] `GameManager.cs` (UdonSharp) 実装
  - [x] UdonSynced: `seed (int)` / `gameState (int)` / `raceStartTime (double)`
  - [x] Inspector: `useDebugSeed (bool, default false)` / `debugSeed (int, default 12345)` / `generator` / `carts[]` 参照
  - [x] `RequestStart()` (Master 限定): `seed` 生成 → `raceStartTime = now + 3.0` (3秒バッファ) → `gameState = STATE_RUNNING` → `RequestSerialization()` → `_ApplyState()`
  - [x] `OnDeserialization()` → `_ApplyState()`(Master は OnDeserialization が呼ばれないので RequestStart 内で直接呼ぶ)
  - [x] `_ApplyState()`: gameState==Running なら `generator.Rebuild(seed)` → `carts[i]._OnRaceStarted()` を同フレーム同期実行
- [x] `StartButton.cs` (UdonSharp) 実装(仮)
  - [x] VRC_Interact + Master 二重ガード
  - [x] `gameManager.RequestStart()` 呼出のみ(視覚切替・参加者数チェックは Phase 5)
- [x] シーン配線
  - [x] `_Managers/AmidakujiGenerator` 配下に `horizontalBars[]` 33 個を `L * 11 + S` の順でドラッグ(`Bar_L0_S00..Bar_L0_S10, Bar_L1_S00..Bar_L1_S10, Bar_L2_S00..Bar_L2_S10`)
  - [x] `_Managers/GameManager` の `carts[]` に Cart_0 をドラッグ(Phase 3 は 1 台のみ、Cart_1..3 配置と参照バインドは Phase 4)、`generator` バインド、`useDebugSeed=true` / `debugSeed=12345`
  - [x] Cart_0 の CartController に `gameManager` / `generator` バインド、旧 `waypointMarkers` フィールド削除
  - [x] `EntryArea/StartButton` に `StartButton.cs` 追加、`gameManager` バインド、**Box Collider (Is Trigger=ON, Size 0.6×0.6×0.3) 追加**(上記方針変更参照)
- [x] Build & Test (2クライアント) 検証 4 単位 ✅ 全項目クリア (2026-05-20)
  - [x] **V1**: 全 33 個の横線 active 状態が両クライアントで一致(seed=12345 で 8/33 本生成、両側一致 ✅)
  - [x] **V2**: Cart_0 経路が両クライアントで一致(Lane 0→1→2、起点 (-6,0,+2) → 終点 (+2,0,-58.5)、6 waypoint ✅)
  - [x] **V3**: 走行中の位置ズレ目視で許容範囲内 ✅
  - [x] **V4**: gameState / seed / raceStartTime の同期 OK、両クライアントで raceStartTime ベースの Lerp 計算が走る ✅
- [x] `CalculateServerDeltaTime` を使った時刻計算の動作確認(V3 に内包) ✅
- [x] **走行中の歩行者すり抜け確認**(Phase 2 から引き継ぎ) ✅ — 2クライアント実機で走行中 Cart_0 に歩行者が突っ込んでも干渉なく通り抜け(Layer 設計の最終確認クリア)

**完了基準**: Master側でスタート → 別クライアントから見ても同じあみだくじ・同じ経路でカートが走る — ✅ 達成

**実機テスト結果サマリ** (seed=12345):

- 横線 8/33 本生成、稠密度 24%(理論値 30% に対し小サンプル誤差範囲内)
- Cart_0 経路: 起点 (-6,0,+2) → WP[1] (-6,0,-43) → WP[2] (-2,0,-43) → WP[3] (-2,0,-48) → WP[4] (+2,0,-48) → 終点 (+2,0,-58.5)
- 距離 68.5 m、所要 34.25 s (speed=2.0 m/s)
- 両クライアントで経路完全一致、走行中の位置ズレも目視範囲内

## Phase 4: 複数カート同時走行 + ゴール処理 [5/24-5/25] [2日]

**着手前設計確定(2026-05-21、論点 ①〜⑤)**:

- **着座と `participantPlayerIds[4]`**: GameManager 集約 + Master 一元書込。着座時は CartController から GameManager に登録要求(`SendCustomNetworkEvent` は引数を取れないため、Master 非保持時の引数渡しパターン — Owner 一時委譲 vs `NetworkEventTarget.Owner` + Master 側 Owner 判定 — の選択は実装時に詰める)。Seats[0..3] は別 GameObject を置かず、Cart_n 自身を着座対象として扱う(architecture.md の Seat (Entry) 記述は Phase 2-3 で VRC_Station が Cart 自身に移動した経緯と整合させる)
- **ゴール検知**: 経路ベース判定(`_state==Running` の Update 内で `elapsed >= _totalDuration` を検出 → `Goaled` 遷移)。trigger collider 方式は不採用(Late Joiner で「自分が見えていない瞬間に通過済」のエッジケースを避けるため)
- **空席カート**: ゴール count・演出から除外(`participantPlayerIds[lane] == -1` の Cart は `_NotifyCartGoaled` 不要)。空席カートが当たり/爆発演出を出しても体験上意味がないため
- **ゴール手前バリア**: Phase 1 で物理形状(隙間 1.5 m × 0.5 m)を VR HMD 確認済(commit `f5b6fc8`)。Phase 4 では再調整なし、4 台同時通過の Build & Test 確認のみ
- **ResultDisplay 遷移**: `STATE_RESULT_DISPLAY=3` を Phase 4 で導入(Phase 3 で番号予約済)。A モードは `finaleCountdownSeconds (Inspector, default 3.0)` 経過後発火 → 1.5 秒 → ResultDisplay、B モードは全員ゴール後 1.5 秒 → ResultDisplay、ResultDisplay 10 秒で Idle 復帰 + `participantPlayerIds[]` 全 -1 リセット + `_goaledCount=0`

実装サブタスク:

- [ ] Cart 4台に増やす(Cart_0 を複製、laneIndex=1,2,3 を設定)
- [ ] 各カートに座席番号 (0-3) を持たせる
- [ ] GameManager に `participantPlayerIds[4]` を UdonSynced 追加(初期値 -1)
- [ ] 着座イベントで参加者登録 — `CartController._OnSeated(player)` → `gameManager._RegisterParticipant(lane, pid)`(Master 一元書込、非 Master 時の引数渡しは実装時詰め)
- [ ] スタート時、全カートが同時に走行開始(Phase 3 の `_OnRaceStarted()` を 4 台分呼出)
- [ ] 経路ベースゴール検知 — `CartController.Update()` で `_state==Running && elapsed >= _totalDuration` → `_OnReachedGoal()`
- [ ] ゴール到達時、座っているプレイヤーを賞品エリアへ `TeleportTo`(`_isExitingByGoal=true` + `station.ExitStation` → `OnStationExited` で TeleportTo 分岐)
- [ ] 空席カートのゴール count・演出スキップ — `_NotifyCartGoaled` 呼出前に `participantPlayerIds[lane] != -1` をガード
- [ ] **観戦者がバリアを越えられないことを確認**(Phase 1 確認の再確認のみ)
- [ ] **ゴール演出の配線**([ADR-0012](./adr/0012-goal-effect-randomized.md))
  - [ ] `GameManager.ComputeEffectAssignment(seed, N, E, C)` 実装 — Fisher-Yates、派生 RNG (`seed ^ 0x000BEEF`)
  - [ ] Inspector フィールド追加: `explosionCount=1 / confettiCount=1 / simultaneousFinale=true / finaleCountdownSeconds=3.0 / prizeAreas[] / finaleSharedAudio`
  - [ ] `PrizeArea.PlayEffect(int kind, bool withIndividualSound)` 実装(または GameManager から直接 SetActive + Play)
  - [ ] `CartController._OnReachedGoal()` から `gameManager._NotifyCartGoaled(laneIndex)` を呼出、B モード時は即発火・A モード時は全カート集計
  - [ ] A モード: 全員ゴール → `SendCustomEventDelayedSeconds(_FireFinale, finaleCountdownSeconds)` → 一斉発火 + `finaleSharedAudio.PlayOneShot` → 1.5 秒後 `EnterResultDisplay`
  - [ ] B モード: 個別到達瞬間に該当レーンのみ `PlayEffect(kind, true)`、全員ゴール後 1.5 秒 → `EnterResultDisplay`
- [ ] `STATE_RESULT_DISPLAY=3` 追加、10 秒経過で `STATE_IDLE` 復帰 + `participantPlayerIds[]` リセット + `_goaledCount=0`
- [ ] Build & Test で4台同時走行を確認(2クライアントで演出配置が一致することを目視)
- [ ] A モード / B モード両方を切替えて動作確認

**完了基準**: 4人 (またはMaster1人+残ダミー) で同時にゴールまで走り、各自テレポートされる。非参加者は賞品エリアに入れない。爆発・紙吹雪が seed 由来でランダムに配置され、2クライアント間で同じ配置になる

## Phase 5: ゲームフロー UI [5/26] [1日]

- [ ] ステートマシン実装 (Idle/Countdown/Running/ResultDisplay)
- [ ] スタートボタン: Master判定、参加者0人時の無効化
- [ ] カウントダウン演出 (3-2-1)
- [ ] **A モード時の FinaleCountdown UI** を Countdown UI に統合(同じ 3-2-1 表示機構を Running 末尾でも再利用、独立ステートにはしない)
- [ ] **演出モード切替トグル UI** をスタートボタン付近に配置(SPEC §7.3 / [ADR-0012](./adr/0012-goal-effect-randomized.md))
  - [ ] Master 限定 + `gameState==Idle` 時のみ反応、それ以外はグレーアウト
  - [ ] 押下で `gameManager.simultaneousFinale` を反転
- [ ] **Player Persistence による永続化**(同一人物の再 Master 時に B モード設定を復元)
  - [ ] トグル操作時に `PlayerData.SetBool("amidakuji.simultaneousFinale", value)` を呼ぶ
  - [ ] `OnPlayerRestored(VRCPlayerApi player)` で `player.isLocal && player.isMaster` のとき復元 + `RequestSerialization` で他クライアントに伝播
  - [ ] **着手時に VRChat Creators Hub で `PlayerData` API の最新シグネチャを再確認**(2024 SDK 機能のため名称変動の可能性)
- [ ] 結果表示掲示(エントリーエリアの掲示UI、「席n → ゴールm」)
- [ ] 着座制御 (Idle中のみ可)
- [ ] ResultDisplay → Idle 自動遷移 (10秒)

**完了基準**: 一連の流れがUI操作だけで回せる

## Phase 6: Late Joiner / エッジケース (PC) [5/27] [1日]

- [ ] Late Joiner テスト: Idle中・Running中・ResultDisplay中それぞれで途中参加
- [ ] Master交代テスト: 走行中にMasterが退出
- [ ] **Player Persistence 動作テスト**(Phase 5 で実装した B モード永続化、[ADR-0012](./adr/0012-goal-effect-randomized.md))
  - [ ] 同じ人が Master として B モードに切替 → ワールド退出 → 再入場時に B が復元される
  - [ ] 別の人が Master として入場 → Inspector 既定値(A モード)が採用される
  - [ ] Master 交代時、新 Master の Persistence 値があれば適用される
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
  - [ ] **ゴール演出(爆発・紙吹雪)の見映えと FPS 影響を実機確認**([ADR-0012](./adr/0012-goal-effect-randomized.md))
    - [ ] 観戦者位置(MainFloor 中央)から演出が視認できる派手さか
    - [ ] **粒子高さ・観戦距離視認チェック**(準備期間 ClientSim 見映え確認 [§5](./phase4-effect-prefab-checklist.md#5-clientsim-での見映え確認) の 4 個別項目を Phase 8 に統合、2026-05-19 方針確定)
      - 火球が 4〜6 m まで上がるか
      - 煙が 6〜8 m まで立ち上がるか(Multiply で背景がやや暗くなるか)
      - 紙吹雪が 10 m 程度まで上がり横拡散 5〜6 m か
      - 観戦距離 34 m(MainFloor 中央 Z=-30 → 賞品エリア Z=-64)から壁(高さ 4 m)越しに視認できるか
    - [ ] 発火時の FPS 低下が 60 FPS を下回らないか、必要なら粒子数を削減
    - [ ] **Confetti Start Color 5 色のギラギラ感確認**(現行は原色 `#FF0000 / #FFFF00 / #00FF00 / #0088FF / #FF66CC`、HMD で過剰なら彩度を落とした中間調 `#FF3333 / #FFCC00 / #33CC33 / #3399FF` 系に差し替え。2026-05-19 確定方針)
    - [ ] **Confetti 色バリエーション拡張検討**(現行 5 色、Android 制約外なので 8〜10 色まで増やせる。Gradient Editor の Color マーカー追加のみで対応可。2026-05-19 ClientSim 確認時のユーザー所感「もう少し色バリエーションあると綺麗」を Phase 8 実機判定に持ち越し)
    - [ ] **粒子サイズ・Start Lifetime 見直し検討**(現行設計値は観戦距離 34 m 想定で計算済みだが、ClientSim では「もう少しデカい方が見える気もする」所感あり。HMD 110° 視野角での迫力次第で `Start Size` / `Start Lifetime` を 1.2〜1.5 倍に調整。2026-05-19 ClientSim 確認時のユーザー所感を Phase 8 実機判定に持ち越し)
    - [ ] A モード / B モードの体感差を比較、既定モードを最終決定
    - [ ] 個別爆発音・紙吹雪音の 3D 音量(Max Distance)を MainFloor から自然に聴こえる値に調整
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
- [ ] ワールド名・サムネイル・説明文設定([BACKLOG.md §ワールドメタデータ](../BACKLOG.md#ワールドメタデータv10-暫定確定--2026-05-21) の暫定確定値を最終調整)
- [ ] PC版 Private アップロード
- [ ] Android版 Private アップロード(同じ Blueprint ID)
- [ ] 友人にDM、Private インスタンスで動作確認
- [ ] **Community Labs 公開ボタン押下** 🎉
- [ ] v1.0 完了タグを git に打つ (`v1.0.0`)

**完了基準**: 一般の VRChat ユーザー (Community Labs オプトイン者) がワールドを訪問可能(PC + Quest 両対応)
