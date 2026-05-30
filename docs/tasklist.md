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
- [x] AudioClip 差し替え — CC0 効果音(当たり=`Audio/SE/balloon-pop.wav` / ハズレ=`Audio/SE/bomb-sound.wav`)を配置済([audio-assets.md](./audio-assets.md) / [ADR-0013](./adr/0013-audio-assets-and-licensing.md))。`FinaleSharedAudio` の A モード共通 SE と各 PrizeArea の 3D SE にアサイン
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

## Phase 4: 複数カート同時走行 + ゴール処理 [5/24-5/25] [2日] ✅ 完了 (2026-05-21、約3日前倒し)

**着手前設計確定(2026-05-21、論点 ①〜⑤)**:

- **着座と `participantPlayerIds[4]`**: GameManager 集約 + Master 一元書込。着座時は CartController から GameManager に登録要求(`SendCustomNetworkEvent` は引数を取れないため、Master 非保持時の引数渡しパターン — Owner 一時委譲 vs `NetworkEventTarget.Owner` + Master 側 Owner 判定 — の選択は実装時に詰める)。Seats[0..3] は別 GameObject を置かず、Cart_n 自身を着座対象として扱う(architecture.md の Seat (Entry) 記述は Phase 2-3 で VRC_Station が Cart 自身に移動した経緯と整合させる)
- **ゴール検知**: 経路ベース判定(`_state==Running` の Update 内で `elapsed >= _totalDuration` を検出 → `Goaled` 遷移)。trigger collider 方式は不採用(Late Joiner で「自分が見えていない瞬間に通過済」のエッジケースを避けるため)
- **空席カート**: ゴール count・演出から除外(`participantPlayerIds[lane] == -1` の Cart は `_NotifyCartGoaled` 不要)。空席カートが当たり/爆発演出を出しても体験上意味がないため
- **ゴール手前バリア**: Phase 1 で物理形状(隙間 1.5 m × 0.5 m)を VR HMD 確認済(commit `f5b6fc8`)。Phase 4 では再調整なし、4 台同時通過の Build & Test 確認のみ
- **ResultDisplay 遷移**: `STATE_RESULT_DISPLAY=3` を Phase 4 で導入(Phase 3 で番号予約済)。A モードは `finaleCountdownSeconds (Inspector, default 3.0)` 経過後発火 → 1.5 秒 → ResultDisplay、B モードは全員ゴール後 1.5 秒 → ResultDisplay、ResultDisplay 10 秒で Idle 復帰 + `participantPlayerIds[]` 全 -1 リセット + `_goaledCount=0`

**実装中の方針修正(2026-05-21、Stage A/B で判明)**:

- **演出/TeleportTo を起点 lane → 終点 lane (goalLane) ベースに変更**(Stage A、commit `7098b74` 以前): 初期実装では Cart の起点 `laneIndex` で演出種別とテレポート先を引いていたが、「Prize_n 部屋の属性=演出種別」という設計意図と乖離。CartController に `_goalLaneIndex`(ComputePath で算出)を追加し、`_NotifyCartGoaled(startLane, goalLane)` の 2 引数化 + 演出/Prize 参照は `goalLane` ベースに修正。テレポート先も `prizeAreas[_goalLaneIndex].teleportTarget` を使用。[ADR-0012 §4](./adr/0012-goal-effect-randomized.md#4-賞品エリアテレポート先は固定--spec-12-と無衝突) に明文化(2026-05-21 改訂)
- **TeleportTo の 1 フレーム遅延化**(commit `7098b74` 以前): `OnStationExited` 内で即時 `TeleportTo` すると VRC_Station の `Player Exit Location` (= Seat) への内部移動が後勝ちで上書きしてしまうため、`SendCustomEventDelayedFrames(_DelayedTeleportToPrize, 1)` で 1 フレーム遅延に変更
- **`_ApplyState()` の冪等化**(Stage B V5 検証中に判明、本コミットで修正): VRChat の `OnDeserialization` が同値で高頻度発火する仕様により、`_ApplyState()` 内で `_goaledCount=0 / _finaleArmed=false` がリセットされ、さらに各 Cart の `_OnRaceStarted()` → `_hasNotifiedGoal=false` もリセットされて、ゴール演出が 1 ラウンドで 7〜10 回再発火する不具合があった。`_appliedState` フィールドを追加して `gameState` が変化したときのみ遷移処理を実行する設計に変更。副次効果として `Idle/Running` 時の Debug.Log 出力が毎秒数回→1 回に減り、Phase 6 ログ削減 TODO も同時解消

実装サブタスク:

- [x] Cart 4台に増やす(Cart_0 を複製、laneIndex=1,2,3 を設定)
- [x] 各カートに座席番号 (0-3) を持たせる
- [x] GameManager に `participantPlayerIds[4]` を UdonSynced 追加(初期値 -1)
- [x] 着座イベントで参加者登録 — **パターン A 確定(2026-05-21)**: Cart に `[UdonSynced] int seatedPlayerId` (初期 -1) を追加、`OnStationEntered` で着座者が Cart Owner を取得 + `seatedPlayerId = player.playerId` 書込 + `RequestSerialization` → Master の `Cart.OnDeserialization` で `gameManager._RegisterParticipant(lane, pid)` 呼出 → `participantPlayerIds[lane]` 更新。Master 自身着座時は `OnStationEntered` 内で直接呼出(対称性)。`_RegisterParticipant` は同値 no-op で冪等。退出時は退出者が Cart Owner のまま `seatedPlayerId = -1` 書込。詳細は [architecture.md §着座者同期(Cart 単位)](./architecture.md#着座者同期cart-単位)
- [x] スタート時、全カートが同時に走行開始(Phase 3 の `_OnRaceStarted()` を 4 台分呼出)
- [x] 経路ベースゴール検知 — `CartController.Update()` で `_state==Running && elapsed >= _totalDuration` → `_OnReachedGoal()`(`_hasNotifiedGoal` フラグで二重発火防止)
- [x] ゴール到達時、座っているプレイヤーを賞品エリアへ `TeleportTo`(`_isExitingByGoal=true` + `station.ExitStation` → `OnStationExited` で 1 フレーム遅延後に `prizeAreas[_goalLaneIndex].teleportTarget` へテレポート)
- [x] 空席カートのゴール count・演出スキップ — `_NotifyCartGoaled` 内で `participantPlayerIds[startLane] != -1` を `laneOccupied` 判定し、B モードは `laneOccupied && !simultaneousFinale` のときのみ発火
- [x] **観戦者がバリアを越えられないことを確認**(Phase 1 で確認済、Phase 4 では再確認なし)
- [x] **ゴール演出の配線**([ADR-0012](./adr/0012-goal-effect-randomized.md))
  - [x] `GameManager.ComputeEffectAssignment(seed, N, E, C)` 実装 — Fisher-Yates、派生 RNG (`seed ^ 0x000BEEF`)
  - [x] Inspector フィールド追加: `explosionCount=1 / confettiCount=1 / simultaneousFinale=true / finaleCountdownSeconds=3.0 / prizeAreas[] / finaleSharedAudio`
  - [x] `PrizeArea.PlayEffect(int kind, bool withIndividualSound)` 実装(`SetActive(true)` + `ParticleSystem.Play()` + 個別 SE の3要素、`ResetEffects()` で `Stop + SetActive(false)`)
  - [x] `CartController._OnReachedGoal()` から `gameManager._NotifyCartGoaled(startLane, goalLane)` を呼出、B モード時は即発火・A モード時は全カート集計
  - [x] A モード: 全員ゴール → `SendCustomEventDelayedSeconds(_FireFinale, finaleCountdownSeconds)` → 一斉発火 + `finaleSharedAudio.Play()` → 1.5 秒後 `_EnterResultDisplay`(`withIndividualSound=false` で個別 SE と二重発音を回避)
  - [x] B モード: 個別到達瞬間に該当レーンのみ `PlayEffect(kind, true)`、全員ゴール後 1.5 秒 → `_EnterResultDisplay`
- [x] `STATE_RESULT_DISPLAY=3` 追加、10 秒経過で `STATE_IDLE` 復帰 + `participantPlayerIds[]` リセット + `_goaledCount=0`
- [x] Build & Test で4台同時走行を確認(2クライアントで演出配置が一致することを目視)
- [x] A モード / B モード両方を切替えて動作確認

**完了基準**: 4人 (またはMaster1人+残ダミー) で同時にゴールまで走り、各自テレポートされる。非参加者は賞品エリアに入れない。爆発・紙吹雪が seed 由来でランダムに配置され、2クライアント間で同じ配置になる — ✅ 達成

**Stage A (ClientSim 単独) 検証結果**:

- A モード(simultaneousFinale=true): 全 Cart ゴール → 3 秒カウントダウン → 一斉発火 + 共通 SE ✅
- B モード(simultaneousFinale=false): 各 Cart 到達瞬間に個別演出発火 ✅
- リタイア(Jump 退出): Cart Owner のまま seatedPlayerId=-1 書込 → 走行継続 → 該当レーンは空席扱い ✅
- 設計バグ発見: 演出割当が起点 lane ベースだとプレイヤー視点で「Prize_n 部屋の演出が毎ラウンド変わる」体験になり ADR-0012 の意図と乖離 → 終点 lane ベースに修正

**Stage B (2 クライアント Build & Test) 検証結果** (seed=12345, 観測クライアント=非 Master):

| 項目 | 結果 | 根拠ログ / 補足 |
| --- | --- | --- |
| V1 participantPlayerIds 同期 | ✅ (間接確認) | `CartGoaled ... occupied=True/False` が両クライアントで一致(着座した Cart のみ occupied=True) |
| V2 Owner 委譲 (Interact + SetOwner) | ✅ | `Transferring ownership of Cart_N to {user}` ログ確認 |
| V3 4 カート同時走行 | ✅ | seed=12345 で 4 Cart の `goal=2,3,0,1` が両クライアントで完全一致、走行中の位置ズレも目視範囲内 |
| V4 A モード同期発火 | ✅ | カウントダウン後の一斉発火と共通 SE が両クライアントで同タイミング |
| V5 B モード個別発火同期 | ✅ | Cart_3 (CONFETTI, occupied=True) → Prize_1 で即時発火、両クライアント同タイミング(2026-05-21 19:52:39) |
| V6 リタイア伝播 | ✅ | 走行中 Jump 退出 → Cart Owner のまま seatedPlayerId=-1 書込 → Master の participantPlayerIds[N]=-1 反映 |

**実機テスト結果サマリ** (seed=12345 固定):

- Effect 割当: Cart_0→goal=2 (NONE), Cart_1→goal=3 (NONE), Cart_2→goal=0 (EXPLOSION), Cart_3→goal=1 (CONFETTI) — `seed ^ 0x000BEEF` 派生 RNG + Fisher-Yates の決定論性確認
- 走行時間: Cart_0=34.25s, Cart_1=38.25s, Cart_2=42.25s, Cart_3=38.25s (speed=2.0 m/s)
- `_ApplyState()` 冪等化後、Running 中の Debug.Log 出力は 1 ラウンド 1 回のみ(修正前は毎秒 3〜6 回)
- CONFETTI 個別発火回数: 修正前=10 回 / 修正後=**1 回** ([commit pending])

## Phase 5: ゲームフロー UI [5/26] [1日] ✅ 完了 (2026-05-23、Persistence 復元 V9-V11 は Phase 6 持越し)

**着手前設計確定(2026-05-22)**:

- ステートマシン拡張は **独立ステート追加なし**(案 2 確定、[ADR-0012 §5](./adr/0012-goal-effect-randomized.md#5-ステートマシン拡張) 改訂)。`STATE_COUNTDOWN=1` は番号予約のまま温存、`gameState == STATE_RUNNING && raceStartTime - serverTime > 0` を冒頭 Countdown UI フェーズとして UI 側で扱う(`raceStartTime = now + COUNTDOWN_BUFFER (3.0s)` で Sync 遅延吸収と UI 表示時間を統合)
- `ResultDisplay → Idle 自動遷移 (10秒)` は [GameManager._ReturnToIdle()](../Assets/_Project/Scripts/GameManager.cs#L270) で実装済(Phase 4 で完了)
- 前提確認: VRChat World SDK ≥ 3.7.4(現行 3.10.3 ✅ 確認済 2026-05-22)、Player Persistence API シグネチャは [ADR-0012 §7](./adr/0012-goal-effect-randomized.md#7-b-モード切替-ui-と-player-persistence-による永続化) 確定済

実装サブタスク(`5-1 / 5-2 / 5-3 / 5-4` は並列着手可、`5-5 → 5-6` は順序依存、`5-8` は 5-1 完了後、`5-7` は独立):

- [x] **5-1 CountdownUI 実装**(新規) — 3-2-1 表示、サーバー時刻ベース、冒頭 Countdown と A モード末尾 FinaleCountdown の両用、`isFinaleOnly` で賞品エリア内 Canvas を冒頭起動からスキップ
- [x] **5-2 StartButton 強化** — 参加者≥1 / `STATE_IDLE` ガード、`sharedMaterial` 切替で Enabled / Disabled 視覚化
- [x] **5-3 Cart 着座 Idle ガード** — `Interact()` と `OnStationEntered` の両方に `gameState != STATE_IDLE` no-op ガード(片方だけだと走行中再着座が抜ける)
- [x] **5-4 `simultaneousFinale` を UdonSynced 化** — Persistence 書込結果を他クライアントへ伝播
- [x] **5-5 FinaleModeToggle UI**(新規) — Master+Idle ガード、A/B/Disabled の 3 状態 Material 切替
- [x] **5-6 FinaleModeManager (Player Persistence)**(新規) — `OnPlayerRestored`(自分の Restored + 自動 Master 時)+ `OnPlayerLeft`(Master 昇格時)の両フック、同値書込は no-op で冪等
- [x] **5-7 ResultDisplay 掲示**(新規) — 「席 N → ゴール M (名前 or 空席 or 退出)」4 行、`STATE_RESULT_DISPLAY` の間のみ Active
- [x] **5-8 FinaleCountdown を CountdownUI に統合** — `countdownUIs[]` 全要素に `_StartCountdown` を呼びつつ最初の有効要素にだけ `_FireFinale` コールバックを渡す(複数発火防止)+ 全要素 null 時の `SendCustomEventDelayedSeconds` フォールバック

**完了基準**: 一連の流れがUI操作だけで回せる(着座 → Master が StartButton 押下 → カウントダウン UI → 走行 → A/B 各モードのゴール演出 → ResultDisplay 掲示 → 自動 Idle 復帰)。演出モード切替トグルが動作し、Persistence 経由で同一人物の再 Master 時に B モード設定が復元される — ✅ UI 一連フロー達成、Persistence 復元は ClientSim/Build&Test では検証困難のため Phase 6 Private アップロード環境に移管

**Stage A (ClientSim 単独) + Stage B (2 クライアント Build & Test) 検証結果** (seed=12345 / 2026-05-23):

| 項目 | 結果 | 補足 |
| --- | --- | --- |
| V1 冒頭 3-2-1 Countdown | ✅ | StartButton 押下後、3→2→1→GO! 表示が両クライアントで見える |
| V2 StartButton 視覚切替 | ✅ | 参加者 0 / 走行中 = Disabled、参加者≥1 + Idle = Enabled |
| V3 Cart 着座 Idle ガード | ✅ | 走行中・ResultDisplay 中の Interact は即弾かれる |
| V4 A モードフィナーレ | ✅ | 全員ゴール → 3 秒 Countdown UI → 一斉発火 + 共通 SE |
| V5 B モード個別発火 | ✅ | Cart ゴール瞬間に該当 Prize で即発火、その後 1.5s で ResultDisplay |
| V6 ResultDisplay 掲示 | ✅ | 「席 N → ゴール M (名前)」4 行が 10 秒表示 → Idle 復帰 |
| V7 FinaleModeToggle 視覚切替 | ✅ | Master+Idle で押下可、A/B/Disabled の 3 状態 Material |
| V8 2 クライアント表示同期 | ✅ | Countdown / Finale UI が両クライアントで同タイミング表示 |
| V9 Persistence 復元(再入場) | Phase 6 持越し | ClientSim/Build&Test では同一アカウント再入場の再現が困難、Private アップロード + テスター必要 |
| V10 Persistence 復元(Master 昇格) | Phase 6 持越し | 同上 — 別アカウント Master 入場 + Master 退出シナリオが要 |
| V11 Persistence 既定値継続 | Phase 6 持越し | 同上 — Persistence 未保存の別アカウントが Master 入場で A モード継続を確認 |

**Phase 6 持越し事項**:

- Cart `_DelayedTeleportToPrize` 内で `seatedPlayerId=-1` 書込追加(ゴール退出時のリセット漏れ、Late Joiner シナリオでの誤登録予防)
- Persistence V9〜V11 を Private アップロード + テスター環境で実施

## Phase 6: Late Joiner / エッジケース (PC) [5/27] [1日]

- [x] **Phase 5 持越し: Cart のリセット処理見直し** — `2f3e3f3` で当初方針どおり `_DelayedTeleportToPrize` 内に `seatedPlayerId=-1` 書込追加。直後の `4216b24` で「A モードの `_FireFinale` が `participantPlayerIds[]` を参照して占有判定する」回帰を発見し方針変更:リセットを `_OnRaceReset()`(ResultDisplay → Idle 遷移時)に遅延、加えて `Cart.OnDeserialization` で ResultDisplay 中の Master 集約をスキップして Late Joiner 誤登録予防を両立
- [x] **(追加) ResultDisplay 表示問題解消** — `4216b24`。Canvas Pos Z=-0.06 と Visual Cube 厚み 0.1 の組合せで Z-Fighting 発生 → Canvas Pos Z=-0.1 でマージン 0.05 確保。separator 罫線 `─`(U+2500、Empty SDF + NotoSansJP Fallback でグリフ不在)を ASCII `=` に置換し TMP 警告も解消。落とし穴は [ui-pitfalls.md](./ui-pitfalls.md) に集約
- [x] Late Joiner テスト: Idle中・Running中・ResultDisplay中それぞれで途中参加 — **2026-05-30 全 PASS**(A=Quest Master / B=PC Desktop late joiner = クロスプラットフォーム同時検証)
  - 1a Idle 中 Join: 床・カート正常、着座可、説明文操作可、非 Master は操作パネル不可(=正しい) ✓
  - 1b 走行中 Join: カートが raceStartTime ベースで同期・直進バグなし・クラッシュなし ✓(Late Joiner Lerp 同期の本命クリア)
  - 1c ResultDisplay 中 Join: **クラッシュ・状態不整合なし**(§13#6 合格)。結果画面の中身は出ない=`GoalLaneIndex`/`_effectKinds` を RUNNING 中ローカル算出する設計上、RUNNING を経ない late joiner は表示不可。v1.1 候補
- [x] Master交代テスト: 走行中にMasterが退出 — **2026-05-30 PASS**(Build & Test 複数クライアント)。走行中に Master クライアント退出 → 他者が Master 昇格・レース破綻なし・初期状態へ正常復帰。Idle 状態の退出でも自動昇格 + 操作パネル操作権移譲を確認済(実環境 A=Quest/B=PC)
- [x] **Player Persistence 動作テスト**(Phase 5 で実装した B モード永続化、[ADR-0012](./adr/0012-goal-effect-randomized.md))— **2026-05-30 全 PASS**(Phase 5 V9-V11 持越し解消、A=Quest/B=PC 実環境)
  - [x] 同じ人が Master として B モードに切替 → ワールド退出 → 再入場時に B が復元される — **2026-05-30 PASS**(`OnPlayerRestored`)。併せて **カラー選択(Tab4)の色 Persistence 復元も確認**(Phase 8 カラーパレットの「色 Persistence 未確認」も解消)
  - [x] 別の人が Master として入場 → Inspector 既定値(A モード)が採用される — 2026-05-30 確認(Persistence 未設定の B が Master 時に A モード既定)
  - [x] Master 交代時、新 Master の Persistence 値があれば適用される — 2026-05-30 PASS。Master を A(B保持)→ B(A既定)に移すとモードが B→A に切替(新 Master の Persistence 支配を確認)
- [x] 全員退出テスト — 2026-05-30 確認。VRChat は空インスタンスを破棄するため「全員退出 → 再 Join」= 新インスタンス = UdonSynced 既定値で必ずクリーン Idle 初期化。インスタンス立て直しでクリーン Idle 開始を確認済(退出時の状態に依らず原理的に残留しない)
- [x] 着座中の人がインスタンスを抜けた場合 — **2026-05-30 PASS**(Build & Test)。走行中に着座者クライアント退出 → 席解放・`participantPlayerIds` 該当 -1・他カート無影響・初期状態へ正常復帰
- [x] VRトリガーで走行中に退出した場合のリタイア処理 — Phase 2 で 4 種退出経路の HMD Build & Test 完了済(commit `b8c7103`、[ADR-0007](./adr/0007-vrcstation-transform-cart.md))。Phase 6 リスト掲載は Phase 2 からの転記重複
- [x] ルール説明パネル設置(追いかけ式観戦の説明含む) — `2f3e3f3` で RulesPanel Rev.4 完成(4 Tab: 参加/観戦/モード/色、JP/EN 切替対応、`RulesPanelController` + `TabButton` + `LangToggleButton`)

**完了基準**: 想定エッジケースで全てクラッシュ・状態不整合が起きない

## Phase 7: Android Platform 切替 + 初期最適化 [5/28] [1日] ★山3

**事前監査結果 (2026-05-28、切替前の静的解析)**:

| 項目 | Android Good 上限 | 現状 | 判定 |
| --- | --- | --- | --- |
| Material Count | 20 | **19** (`Assets/_Project/Materials/*.mat` 実体カウント、[material-set.md §1](./material-set.md#1-マテリアル一覧-計-19)) | ⚠ ヘッドルーム +1 |
| Texture サイズ | 1024×1024 | 全 .mat で `m_Texture: {fileID: 0}`(プレースホルダ運用) | ✓ 問題なし(Phase 9 で追加時に override 要) |
| GPU Instancing | 全マテリアルで有効化 | Standard Lite 系 16 件すべて `m_EnableInstancingVariants: 1` / Particles 系 3 件は Inspector 項目なし(仕様、[material-set.md §1 脚注](./material-set.md#1-マテリアル一覧-計-19)) | ✓ |
| Transparent Materials | 使用しない | Standard Lite で alpha blend なし、Particles/Additive・Multiply は別経路、TMP も別 | ✓ |
| Tri 数 | 250,000 | **静的解析不可**(Phase 7 切替後に Stats で実測) | ⏳ Phase 7 で確認 |

事前監査により **Material Count / Texture / GPU Instancing / Transparent の 4 項目はバジェット内**。Tri 数は Platform 切替後 Stats でのみ確認可能。VCC 切替自体は粛々と進めれば良い。

- [x] VCC SDK Control Panel で Build Platform を Android に切替 — 2026-05-28
- [x] **再インポート完了まで待機**(プロジェクトサイズによっては数十分かかる) — 2026-05-28
- [x] SDK のバリデーションメッセージを確認・対応 — 警告なくアップロードまで通過(2026-05-28)
- [x] マテリアル数を Stats でカウント、20以下に絞る — 事前監査済(19/20)、Stats 値と一致確認のみ
- [x] テクスチャを 1024×1024 以下に調整 — 事前監査済(テクスチャ未使用)
- [ ] Tri 数を Stats で確認、250,000 以下に — Phase 7 では未計測、Phase 8 の実機 FPS 計測時に確認
- [x] GPU Instancing を全マテリアルで有効化 — 事前監査済(全件 ON)
- [x] Android 向け Private アップロード成功 — 2026-05-28、Quest 実機で Join + RulesPanel タップ + Cart 着座まで動作確認

**完了基準**: Android プラットフォームでビルドが通り、Quest 実機で Join できる — ✅ 達成(2026-05-28)

**Phase 7 Quest 実機での発見事項(Phase 8 持越し)**:

- **StartButton 構造的に届かない問題** — Master が Cart_0 (X=-6) / Cart_3 (X=+6) に着座すると、StartButton (X=0, Z=2) との横距離 6 m に対し `Proximity: 2` で Interact 不可。Cart_1/2 でもギリギリ。修正最短は Inspector で Proximity を 2 → 8〜10 に拡大(`StartButton.cs` 側で Master 二重ガード済のため観戦者誤押下は no-op)。Phase 8 開始時に対応([BACKLOG.md §課題・既知の制約](../BACKLOG.md#技術的不安要素phase着手時に検証する))

## Phase 8: Quest 実機テスト + 調整 [5/29] [1日]

- [x] Quest 実機で全機能を動作確認 — **2026-05-30 Phase 10 通しテスト(PC+Quest 混在)で A〜F 全 PASS**
  - [x] 着座 → カート走行 → ゴールテレポート — 通しテストで基本ループ問題なし(着座姿勢含む)
  - [x] 観戦者として走り回って追いかける(物理FPSが体験に十分か)— 2026-05-30 床走り回りで平均 70 FPS(OVR Metrics Tool)、体験十分
  - [x] ゴール手前バリア突破不可 — 通しテストで非参加者の侵入不可を確認
  - [x] **ゴール演出(爆発・紙吹雪)の見映えと FPS 影響を実機確認**([ADR-0012](./adr/0012-goal-effect-randomized.md))— 2026-05-30 Quest 実機で見映え・FPS とも問題なし
    - [x] 観戦者位置(MainFloor 中央)から演出が視認できる派手さか — 実機 OK
    - [x] **粒子高さ・観戦距離視認チェック**(準備期間 ClientSim 見映え確認 [§5](./phase4-effect-prefab-checklist.md#5-clientsim-での見映え確認) の 4 個別項目を Phase 8 に統合、2026-05-19 方針確定)— 実機で 34m からの視認問題なし
      - 火球が 4〜6 m まで上がるか
      - 煙が 6〜8 m まで立ち上がるか(Multiply で背景がやや暗くなるか)
      - 紙吹雪が 10 m 程度まで上がり横拡散 5〜6 m か
      - 観戦距離 34 m(MainFloor 中央 Z=-30 → 賞品エリア Z=-64)から壁(高さ 4 m)越しに視認できるか
    - [x] 発火時の FPS 低下が 60 FPS を下回らないか、必要なら粒子数を削減 — **2026-05-30 PASS**。ゴール演出(爆発+紙吹雪)発火時も平均 70 FPS、60 を割らず。粒子削減不要(OVR Metrics Tool 計測)
    - [x] **Confetti Start Color 5 色のギラギラ感確認** — 2026-05-30 実機で現行 5 色(原色)のまま問題なし。中間調への差し替えは不要
    - [x] **Confetti 色バリエーション拡張検討** — 現行 5 色で実機 OK。8〜10 色拡張は **v1.1 送り**(必須でない)
    - [x] **粒子サイズ・Start Lifetime 見直し検討** — 2026-05-30 実機で現行値のまま視認・迫力とも問題なし。1.2〜1.5 倍化は不要
    - [x] A モード / B モードの体感差を比較、既定モードを最終決定 — **既定 A モードで確定**(実機で演出問題なし、B は Master が操作パネルで切替可能、[ADR-0012](./adr/0012-goal-effect-randomized.md))
    - [x] 個別爆発音・紙吹雪音の 3D 音量(Max Distance)を MainFloor から自然に聴こえる値に調整 — 2026-05-30 実機で音量バランス問題なし(現行値で確定)
  - [x] Late Joiner: Quest からPC instance への参加 — 2026-05-30 確認(PC ホストに Quest 参加で同期問題なし)
  - [x] PC instance への Quest 参加 + 逆方向 — 双方向確認済(A=Quest Master+PC 参加 / PC ホスト+Quest 参加 とも同期一致)
- [x] パフォーマンス問題があれば追加最適化 — **不要**(全シーン平均 70 FPS、Quest 60 目標クリア、2026-05-30)
  - [x] FPS 60 未満ならテクスチャ・Tri 削減 — N/A(70 FPS、削減不要)
  - [x] DrawCall 多すぎなら Static Batching 確認 — Static Batching は Phase 9 で適用済、FPS 良好で追加対応不要

### Phase 8 UX 細部調整(2026-05-29 着手予定)

Phase 7 Quest 実機判定および Phase 8 機能改修コミット後に判明した UX 課題:

- [x] **TMP Empty SDF Font の Underline 警告解消** — Scene の `ResultDisplay` の `separatorLine` は既に `=====================`(`─` override は解消済)。次の ClientSim 起動時に Console から Underline 警告が消えていることを確認(残っていれば別 TMP が発生源)
- [x] **RulesPanel / ResultDisplay の高さ調整** — 2026-05-30 Quest 実機で見上げ姿勢が気にならず、**現状高さで許容**(v1.0)。当初 2026-05-28 の「見上げ」報告は再評価で問題なしと判断。今後気になれば Position.y を下げて再調整可
- [x] **(機能改修反映確認) Cart Seat 着座位置 + ResultDisplay 永続 + 賞品エリア戻り** — 2026-05-30 通しテストで確認。着座姿勢 OK / 結果 UI 掲示板は次の START まで残る / 賞品エリア滞在者は **resultHoldSeconds(10秒)後の自動 IDLE 遷移**で起点復帰(A/B 共通、START 押下トリガーではない点に注意)
- [x] **ボタン Proximity 拡大**(2026-05-29) — StartButton `2 → 10`、RulesPanel の Tab1〜4Button + LangToggleButton `2 → 4`(押しづらさ対策)。ただし StartButton は **proximity 拡大だけでは着座時に視点が真横までしか回らず正対できず不十分**と ClientSim で判明 → 下記「操作パネル方式」で根本解決
- [x] **操作パネル方式(着座者用 START/MODE)**(完了 2026-05-29、commit `674c42a`) — 着座すると視点が真横までしか回らず中央 StartButton に正対できない問題を根本解決。各 Cart 着座者の正面 + 中央に ControlPanel(START/MODE)を 5 枚配置(Prefab 化)、`GameManager.controlPanels[]` を gameState 連動で表示制御(RUNNING 非表示 / IDLE・RESULT_DISPLAY 表示)。Master のみ操作可。ボタン表面に TMP ラベル(START 押下可否 / MODE A・B)を追加。**レース後フローも Option B に修正**: 結果を `resultHoldSeconds`(既定 10 秒)表示後に自動で卓リセット(Cart 起点復帰・着座枠クリア・賞品エリア滞在者を起点へテレポート)、結果 UI とパネルは次の START まで残す。ClientSim で 1234 動作確認済、配置・配線は [phase8-control-panel-checklist.md](./phase8-control-panel-checklist.md)。**Quest 実機確認も完了(2026-05-30 通しテスト)**。※実機所感「操作パネルがやや高い位置かも」=軽微、v1.0 は放置・後日 Position.y 微調整候補
- [x] **カラーパレット UI 配置(Tab4)**(ClientSim 表示確認 2026-05-29) — Phase 6 で欠落していた色選択 UI を配置。`RulesPanelController._RefreshColorPalette` を MaterialPropertyBlock 方式に改修し、Tab4 右側に Swatch×8(2 列×4 段・縦読み)+ SelectionHighlight を配置・配線。色表示 / タブ連動 / 選択枠 / JP-EN / **着座→Cart 色反映**まで ClientSim 確認済。マテリアルは Cart の `M_LaneColor` と同じ Standard Lite(`M_Wall_Generic` 流用)で発色一致・19 維持。BodyText を Height 100 / Pos Y -5 に拡大して EN 本文のはみ出しも解消([ui-pitfalls.md §3](./ui-pitfalls.md))。**Quest 実機・色 Persistence のみ未確認**。カスタム色(9 枠目)はスペース確保のみ。確定値 [phase8-color-palette-checklist.md](./phase8-color-palette-checklist.md)

### Phase 8 調整候補値の叩き台 (2026-05-28、Phase 7 着手前事前準備)

現行値は [phase4-effect-prefab-checklist.md](./phase4-effect-prefab-checklist.md) 参照。
HMD 実機で「過剰 / 物足りない」のどちらかが出たときに即試せる試行値セット:

Confetti `Start Color` 拡張案(現行 5 色 → 8 色 / 10 色):

| バリエーション | 色セット (HEX) |
| --- | --- |
| 中間調 5 色(原色置換) | `#FF3333` / `#FFCC00` / `#33CC33` / `#3399FF` / `#CC66CC` |
| 8 色拡張(中間調 + 3 色) | 上記 + `#FF8833`(橙) / `#9933FF`(紫) / `#00CCCC`(シアン) |
| 10 色拡張(8 色 + 2 色) | 上記 + `#FFFFFF`(白フラッシュ感) / `#88FF88`(明るい緑) |

`Color over Lifetime` の Gradient Color Marker 追加で対応可、`Start Color` の Random Color from Gradient を併用する場合は Gradient マーカーを追加する。

粒子サイズ・寿命の試行値(`Start Size` / `Start Lifetime`):

| ファイル | プロパティ | 現行値 | 1.2x 試行 | 1.5x 試行 |
| --- | --- | --- | --- | --- |
| ConfettiEffect | Start Size | 0.15〜0.3 | 0.18〜0.36 | 0.225〜0.45 |
| ConfettiEffect | Start Lifetime | 3.0 | 3.6 | 4.5 |
| ExplosionEffect (Fireball) | Start Size | 0.8〜1.5 | 0.96〜1.8 | 1.2〜2.25 |
| ExplosionEffect (Fireball) | Start Lifetime | 1.2 | 1.44 | 1.8 |
| ExplosionEffect (Smoke) | Start Size | 1.0〜2.0 | 1.2〜2.4 | 1.5〜3.0 |
| ExplosionEffect (Smoke) | Start Lifetime | 2.5 | 3.0 | 3.75 |

HMD 110° 視野角 + 観戦距離 34 m の体感判定で 1.0x / 1.2x / 1.5x のいずれかを選ぶ。FPS 影響が出るなら逆に 0.8x も試行候補。

**完了基準**: Quest 実機で全体験が 60 FPS 以上、機能差なし

## Phase 9: ライティング・最終最適化 [5/30] [1日]

- [x] **BGM 配置**(単一ループ、CC0、[ADR-0013](./adr/0013-audio-assets-and-licensing.md) / [audio-assets.md](./audio-assets.md)) — 2026-05-30 完了(WAV 配置 + Import 設定 + AudioSource 配線の3点すべて済、シーンファイルで確認)
  - [x] Mutant Club (HoliznaCC0) を `Audio/BGM/HoliznaCC0-Mutant-Club.wav` に配置(WAV ソース約 25 MB)
  - [x] Import 設定: Compression Format = Vorbis / Quality 70%(`compressionFormat:1 / quality:0.7`)/ Load Type = Compressed In Memory(`loadType:1`)。`platformSettingOverrides` 空で Android も default 継承、推定ビルドサイズ約 3 MB — 2026-05-30 確認
  - [x] AudioSource 1 本(`BGM` GameObject、`Spatialize:0 / SpatialBlend=0(2D) / Loop:1 / PlayOnAwake:1 / Volume:0.3`)配置、VRCSpatialAudioSource は `EnableSpatialization:0`([ui-pitfalls.md §5](./ui-pitfalls.md))— 2026-05-30 確認
- [x] **ResultDisplay に当たり/ハズレ表記を追加**(2026-05-30 v1.0 採用)— ゴール演出 [ADR-0012](./adr/0012-goal-effect-randomized.md) の当落を結果掲示にも表示。実装 → ClientSim → PC アップロード → 実機(Desktop)確認まで完了
  - [x] `GameManager.GetEffectKind(int goalLane)` 公開 getter 追加(範囲外・未算出は 0 返し)
  - [x] [ResultDisplayUI.cs](../Assets/_Project/Scripts/ResultDisplayUI.cs) の `_RefreshText()` 各行に当落ラベル付与。`GetEffectKind(goalLane)` で 2=紙吹雪→当たり / 1=爆発→ハズレ / 0=無演出→無表記。`<b>` 強調、占有行のみ(空席・退出には出さない)
  - [x] JP/EN ラベルフィールド追加(`winLabelJP="当たり" / winLabelEN="Win"`、`loseLabelJP="ハズレ" / loseLabelEN="Lose"`、既存 cartWordJP/EN と同パターン、C# 既定値で自動投入)
  - 既知の軽微: RESULT_DISPLAY 中の Late Joiner は `_effectKinds` 未算出で当落ラベルのみ出ない(クラッシュなし、Phase 6 エッジ、v1.0 許容)
- [x] **ライティング bake 完了**(2026-05-30)— Baked GI 構成。`Assets/_Project/Amidakuji-Lightning.lighting`(Realtime GI **OFF** / Lightmap Resolution **12** / Directional Mode **Non-Directional** / Lightmapper **Progressive CPU** / Indirect 256・Direct 32・Env 256・Max Bounces 2 / Ambient Occlusion OFF)。**Directional Light Mode = Baked** → シーン唯一の光源が Baked = **Realtime Light 0** 達成。MainFloor 等に lightmap 生成確認(`Lightmap-0_comp_light.exr`)。当初 PC は Mixed Lighting 想定だったが、平面・無テクスチャ・Quest 60FPS 優先のため **完全 Baked + Light Probe** に方針変更(動的影は捨て、カート/プレイヤーは Probe で受光)
  - **bake 環境メモ**: Unity 機は i7-4790 + Intel HD 4600 で **GPU Lightmapper 不可**(VRAM < 4GB で無視)→ Progressive CPU 固定。`m_BakeBackend` を CPU に統一
  - **既知警告(許容)**: 55 オブジェクトで Lightmap UV 重複(Unity プリミティブ Cube の縦線/横線/壁)。無テクスチャ均一照明のため実害小、v1.0 は許容(プリミティブは Generate Lightmap UVs 不可)
- [x] Light Probe 配置 — `Light Probe Group`(原点)に床面グリッド約 30 probe(X: −7/0/+7、Z: +10/−8/−28/−48/−64、Y: 0.5 と 2.0 の2層)。動くカート/プレイヤーを受光。**賞品エリア内に各 1 probe 追加**(Prize_0〜3 室内 Y≈1.2、テレポート着地者の受光用)
- [x] **賞品エリア暗さ対策**(2026-05-30、Quest 実機所感「賞品エリアが暗い」を受け)— 密閉室は Baked Directional が届かず暗いため、各 Prize_0〜3 に **Baked Point Light ×4(Mode=Baked / Intensity=4 / Range=6 / 部屋中央天井寄り)** を追加 + 室内 Probe + **Ceiling を Contribute GI に追加**(天井が lightmap 未対象でフラットだった)→ 再 bake。Baked なので Realtime Light 0 維持・FPS 影響なし。明るさ解決を確認
- [x] Reflection Probe 配置 — bake 時に**環境(スカイボックス)反射プローブが自動生成**(`ReflectionProbe-0.exr`)。無テクスチャ・非金属マテリアルのため**手動 Reflection Probe は不要**(配置せず、自動環境反射で十分)
- [x] Occlusion Culling ベイク — `OcclusionCullingData.asset`(約 5.7 KB)。平面で遮蔽少なく効果は限定的だが bake 済
- [x] Static Batching 有効化 — 静的ジオメトリに Static フラグ付与(MainFloor / VerticalLines / HorizontalBars 33 / GoalBarriers / PrizeAreas の Walls・Ceiling / Seats)。カート・UI・粒子・Managers は Static 除外
- [x] PC + Android 両方で DrawCall, Triangle数を Stats で確認、バジェット内に収める — **2026-05-30 確認**。両PF とも Tris **2.7k**(Android 上限 250k)/ Batches **13**(Saved by batching 68)/ SetPass **9** / Shadow casters 0。バジェットに大幅な余裕
- [x] VRChat SDK のワールド分析で両プラットフォーム Good ランク確認 — **達成**。※World には Avatar 的な単一ランクバッジは無く、実質「バジェット内 + Realtime Light 0 + Quest 70 FPS 実測」で判定。PC Alert 0 / Android は無害な TMP 警告のみ([ui-pitfalls.md §6](./ui-pitfalls.md))
- [x] スカイボックス、Post Processing 軽く(モバイルでは Post Processing 控えめ)— **デフォルトスカイボックス + Post Processing 未導入で確定**(2026-05-30、Quest 軽量優先 + 実機見映えに不満なし)
- **新規ファイル(要 git add、未コミット)**: `Amidakuji-Lightning.lighting`(+meta)、`Assets/Scenes/VRCDefaultWorldScene/` 配下の `LightingData.asset` / `Lightmap-0_comp_light.exr` / `OcclusionCullingData.asset` / `ReflectionProbe-0.exr`(各 +meta)

**完了基準**: PC + Android 両プラットフォームで Good ランク、Quest 実機 60 FPS 以上

## Phase 10: 最終テスト & 公開 [5/31]

- [x] 多人数 (可能なら4人) で通しテスト(PC + Quest 混在)— **2026-05-30 PASS**(2アカウント A=Quest/B=PC、観点 A〜F 全 OK:基本ループ/バリア観戦/モード・カラー/クロスプラットフォーム双方向/音/ルール UI)。4人同時は未だが基本フロー網羅
- [x] ルール説明パネル最終チェック — 通しテスト F で 4タブ・JP/EN 切替の崩れなしを確認
- [x] ワールド名・サムネイル・説明文設定 — 2026-05-30 SDK 投入済。名前 `巨大あみだくじ / Ghost-Leg Express`、説明は短縮版(長文版は VRChat 説明欄の文字数上限で保存不可だったため圧縮)、タグ5(`amidakuji/ghostleg/game/party/quest`)。サムネは現状可([BACKLOG.md §ワールドメタデータ](../BACKLOG.md#ワールドメタデータv10-暫定確定--2026-05-21))
- [x] PC版 Private アップロード — 2026-05-30 賞品エリア修正込みの最新版アップ済(通しテストで修正不要のため最終版)
- [x] Android版 Private アップロード(同じ Blueprint ID)— 2026-05-30 同上、同一 Blueprint で最新版アップ済
- [x] **本番 seed ランダム化(リリース必須)** — GameManager の `useDebugSeed` を OFF に(Phase 3-4 のテスト用 `useDebugSeed=true`/`debugSeed=12345` が残っていた)。これで `seed = DateTime.Now.Ticks` で毎回ランダム生成。OFF 化後に両PF 再アップロード済(2026-05-30)。※これが ON のままだと毎回同じあみだくじになる致命的設定だった
- [x] 友人にDM、Private インスタンスで動作確認 — 2アカウント(A=Quest/B=PC)通しテストで代替確認済
- [x] **Community Labs 公開ボタン押下** 🎉 — **2026-05-30 公開**(目標 5/31 に対し1日前倒し)
- [x] v1.0 完了タグを git に打つ (`v1.0.0`)— commit `1968f07` に annotated tag、push 済

**完了基準**: 一般の VRChat ユーザー (Community Labs オプトイン者) がワールドを訪問可能(PC + Quest 両対応)
