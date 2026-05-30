using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameManager : UdonSharpBehaviour
{
    // gameState: Phase 4 で RESULT_DISPLAY=3 を有効化。
    // COUNTDOWN=1 は Phase 5 で UI 導入時に挿入する予約番号(未使用)。
    public const int STATE_IDLE = 0;
    public const int STATE_COUNTDOWN = 1;
    public const int STATE_RUNNING = 2;
    public const int STATE_RESULT_DISPLAY = 3;

    // 1 ラウンド終端の演出視認時間 → ResultDisplay 遷移
    public const float POST_FINALE_DELAY = 1.5f;

    // 賞品エリア判定の Z 閾値(GoalBarrier Z=-58.5 より奥なら賞品エリア内)。
    // STATE_RESULT_DISPLAY → STATE_RUNNING 遷移時に、LocalPlayer がこの Z より奥なら
    // defaultSpawn に強制テレポートして「次レース参加可能位置」に戻す。
    public const float PRIZE_AREA_Z_THRESHOLD = -58.0f;

    // ComputeEffectAssignment の派生 RNG mask (ADR-0012)
    private const int EFFECT_RNG_MASK = 0x000BEEF;

    [Header("References")]
    public AmidakujiGenerator generator;
    public CartController[] carts;
    public PrizeArea[] prizeAreas;
    public AudioSource finaleSharedAudio;
    [Tooltip("冒頭 3-2-1 と A モード末尾 FinaleCountdown 用 UI 群(Phase 5)。配列の最初の要素にだけ FinaleCountdown コールバックを渡す(複数発火防止)")]
    public CountdownUI[] countdownUIs;
    [Tooltip("ResultDisplay 掲示 UI(Phase 5)")]
    public ResultDisplayUI resultDisplayUI;
    [Tooltip("STATE_RUNNING 遷移時、賞品エリアにいる LocalPlayer をここへ強制テレポート(Phase 8 着座位置/戻り改修)。通常は DefaultSpawn Transform を割当")]
    public Transform defaultSpawn;
    [Tooltip("操作パネル群(各 Cart 前 + 中央の START/MODE をまとめた親)。RUNNING 中のみ非表示、IDLE/RESULT_DISPLAY で表示(Phase 8 着座者操作対応)")]
    public GameObject[] controlPanels;

    [Header("Debug / Reproducibility")]
    [Tooltip("ON のとき debugSeed をそのまま使う(再現テスト用、本番は OFF)")]
    public bool useDebugSeed = false;
    public int debugSeed = 12345;

    [Header("Finale (ADR-0012)")]
    [Tooltip("冒頭 3-2-1 カウントダウンの秒数(Sync 遅延吸収バッファ兼用)。5.0 にすると 5-4-3-2-1 表示")]
    public float startupCountdownSeconds = 3.0f;
    [Tooltip("爆発演出を割り当てるレーン数")]
    public int explosionCount = 1;
    [Tooltip("紙吹雪演出を割り当てるレーン数")]
    public int confettiCount = 1;
    [Tooltip("true=A モード(全員ゴール後一斉発火) / false=B モード(個別到達時即発火)")]
    [UdonSynced] public bool simultaneousFinale = true;
    [Tooltip("A モード時のカウントダウン秒数")]
    public float finaleCountdownSeconds = 3.0f;
    [Tooltip("結果表示してから自動で卓をリセット(Cart 起点復帰・着座枠クリア・賞品エリア滞在者を起点へテレポート)するまでの秒数。結果 UI 自体は隠さず次の START まで残す(Phase 8 Option B)")]
    public float resultHoldSeconds = 10.0f;

    [UdonSynced] public int seed;
    [UdonSynced] public int gameState;
    [UdonSynced] public double raceStartTime;
    [UdonSynced] public int[] participantPlayerIds;

    // ローカル状態(同期不要、各クライアントが seed 由来で同じ値に収束)
    private int[] _effectKinds;
    private int _goaledCount;
    private bool _finaleArmed;

    // 直前に _ApplyState() を実行した gameState。-1=未適用 で初回必ず遷移処理を走らせる。
    // OnDeserialization は同値で何度も発火しうるため、遷移時のみ初期化処理を実行して
    // _goaledCount/_finaleArmed/Cart._hasNotifiedGoal の不正リセットによる演出再発火を防ぐ。
    private int _appliedState = -1;

    void Start()
    {
        seed = 0;
        gameState = STATE_IDLE;
        raceStartTime = 0.0;

        int n = (carts != null) ? carts.Length : 0;
        participantPlayerIds = new int[n];
        for (int i = 0; i < n; i++) participantPlayerIds[i] = -1;

        _effectKinds = new int[n];
        _goaledCount = 0;
        _finaleArmed = false;
    }

    // StartButton から呼ばれる。Master 限定。
    // Phase 8 改修: STATE_RESULT_DISPLAY からも直接 Race 開始可能(STATE_IDLE 経由不要)。
    // ResultDisplay 永続表示 + 次レース予約(着座)を許容する設計に対応する。
    public void RequestStart()
    {
        if (!Networking.IsMaster) return;
        if (gameState == STATE_RUNNING) return;

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        // STATE_RESULT_DISPLAY 中は Cart.OnDeserialization の Master 集約をスキップしている
        // ため、participantPlayerIds[] が前レース時点で残っている。各 Cart の現状の
        // seatedPlayerId からスナップショットを再構築する(Cart Owner の値が正)。
        if (participantPlayerIds != null && carts != null)
        {
            for (int i = 0; i < participantPlayerIds.Length; i++)
            {
                participantPlayerIds[i] = (i < carts.Length && carts[i] != null)
                                          ? carts[i].seatedPlayerId : -1;
            }
        }

        // UdonSharp は明示キャスト (int)long を Convert.ToInt32 にコンパイルする。これは
        // チェック付き変換で int 範囲超過時に例外を投げる(通常C#の切り捨てとは異なる)。
        // DateTime.Now.Ticks は int.MaxValue を遥かに超えるため、下位31ビットをマスクして
        // [0, int.MaxValue] に収めてから変換する(下位ビットは100ns刻みで変化=実用上ランダム)。
        seed = useDebugSeed ? debugSeed : (int)(System.DateTime.Now.Ticks & 0x7FFFFFFFL);
        raceStartTime = Networking.GetServerTimeInSeconds() + startupCountdownSeconds;
        gameState = STATE_RUNNING;
        RequestSerialization();

        // Master 自身には OnDeserialization が呼ばれないため明示反映
        _ApplyState();
    }

    public override void OnDeserialization()
    {
        _ApplyState();
    }

    private void _ApplyState()
    {
        // 同一 gameState の再受信は no-op(冪等化)。
        // これを外すと OnDeserialization 高頻度発火時に _goaledCount/_finaleArmed が
        // 毎回 0/false に戻り、Cart 側の _OnReachedGoal が再発火して演出が複数回出る。
        if (_appliedState == gameState) return;
        _appliedState = gameState;

        // 操作パネル(各 Cart 前 + 中央の START / MODE)は RUNNING 中のみ隠す。
        // IDLE / RESULT_DISPLAY では表示し、着座者(Master)が目の前で操作できる(Phase 8)。
        // START 押下 → RUNNING で全消え、結果(RESULT_DISPLAY)で再表示 → 次レース受付。
        bool showControlPanels = (gameState != STATE_RUNNING);
        if (controlPanels != null)
        {
            for (int i = 0; i < controlPanels.Length; i++)
            {
                if (controlPanels[i] != null) controlPanels[i].SetActive(showControlPanels);
            }
        }

        if (gameState == STATE_RUNNING)
        {
            // Phase 8 改修: ResultDisplay 永続表示を解除
            if (resultDisplayUI != null) resultDisplayUI._Hide();

            // Phase 8 改修: 賞品エリア(GoalBarrier より奥)にいる LocalPlayer は
            // DefaultSpawn に強制テレポート。各クライアントが自分自身を判定・移動するため
            // VRC の TeleportTo Local 制約を回避できる。観戦エリア(MainFloor 上)の
            // プレイヤーには影響しない。
            _TeleportLocalToSpawnIfInPrize();

            if (generator != null) generator.Rebuild(seed);

            // 演出割当を seed 由来で算出(各クライアント独立、結果一致)
            int n = (carts != null) ? carts.Length : 0;
            _effectKinds = ComputeEffectAssignment(seed, n, explosionCount, confettiCount);
            _goaledCount = 0;
            _finaleArmed = false;

            // Rebuild 完了後に各 Cart に明示通知して同フレーム内で順序保証
            // (Joiner 側で Update polling だと Rebuild と ComputePath の順序が不安定で
            //  横線未初期化の状態で経路計算される回帰があった、Phase 3 V2 不具合)
            if (carts != null)
            {
                for (int i = 0; i < carts.Length; i++)
                {
                    if (carts[i] != null) carts[i]._OnRaceStarted();
                }
            }
            // Phase 5: 冒頭 Countdown UI を起動。Cart 側は CalculateServerDeltaTime(raceStartTime, now) が
            // 負の間は起点で待機するため、ここでは UI 表示のみで callback は不要(空文字)。
            if (countdownUIs != null)
            {
                for (int i = 0; i < countdownUIs.Length; i++)
                {
                    if (countdownUIs[i] == null) continue;
                    // 賞品エリア内 Canvas など FinaleCountdown 専用の UI は冒頭ではスキップ
                    // (冒頭時点で賞品エリアには誰もいない + ここで非 Active 化されると
                    //  UdonSharp 制約で末尾 FinaleCountdown の再起動が届かないため)
                    if (countdownUIs[i].isFinaleOnly) continue;
                    // 冒頭 Countdown はコールバック不要(Cart 側で raceStartTime 到達を独自判定して走り出すため)
                    countdownUIs[i]._StartCountdown(raceStartTime, "", false);
                }
            }
            Debug.Log("[GameManager] state=Running seed=" + seed + " raceStart=" + raceStartTime);
        }
        else if (gameState == STATE_IDLE)
        {
            if (carts != null)
            {
                for (int i = 0; i < carts.Length; i++)
                {
                    if (carts[i] != null) carts[i]._OnRaceReset();
                }
            }
            // 演出 GameObject をリセット
            if (prizeAreas != null)
            {
                for (int i = 0; i < prizeAreas.Length; i++)
                {
                    if (prizeAreas[i] != null) prizeAreas[i].ResetEffects();
                }
            }
            if (countdownUIs != null)
            {
                for (int i = 0; i < countdownUIs.Length; i++)
                {
                    if (countdownUIs[i] != null) countdownUIs[i]._CancelCountdown();
                }
            }
            // Phase 8 Option B: 結果 UI はここで隠さない(次レース開始 = RUNNING で隠す)。
            // 結果を見せたまま卓だけリセットする要望に対応。
            // 賞品エリアに残っている LocalPlayer を起点へ戻す(symptom 1: ゴール後の自動リスポーン)。
            _TeleportLocalToSpawnIfInPrize();
            _goaledCount = 0;
            _finaleArmed = false;
            Debug.Log("[GameManager] state=Idle");
        }
        else if (gameState == STATE_RESULT_DISPLAY)
        {
            if (resultDisplayUI != null) resultDisplayUI._Show();
            Debug.Log("[GameManager] state=ResultDisplay");
        }
    }

    // CartController から着座 / 退出時に呼ばれる。
    // Master のみ参加者配列を書き換える(競合回避)。Master 以外で呼ばれた場合は no-op。
    // 同値書込は no-op(冪等)、Cart.OnDeserialization の連鎖発火に対する保険。
    public void _RegisterParticipant(int lane, int playerId)
    {
        if (!Networking.IsMaster) return;
        if (participantPlayerIds == null) return;
        if (lane < 0 || lane >= participantPlayerIds.Length) return;
        if (participantPlayerIds[lane] == playerId) return;

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        participantPlayerIds[lane] = playerId;
        RequestSerialization();
        Debug.Log("[GameManager] RegisterParticipant lane=" + lane + " pid=" + playerId);
    }

    // 指定 goalLane の演出種別を返す(ResultDisplayUI の当落表記用)。
    // PrizeArea.KIND_NONE(0) / KIND_EXPLOSION(1) / KIND_CONFETTI(2)。範囲外・未算出は 0(NONE)。
    public int GetEffectKind(int goalLane)
    {
        return (_effectKinds != null && goalLane >= 0 && goalLane < _effectKinds.Length)
               ? _effectKinds[goalLane] : 0;
    }

    // CartController._OnReachedGoal() から各クライアントが呼ぶ(ネットワークイベントではない)。
    // 各クライアントが独立に Update 内で elapsed >= _totalDuration を検出 → 同タイミング発火。
    //   startLane: Cart の起点 lane(着座者識別=participantPlayerIds 参照用)
    //   goalLane:  あみだくじを辿った終点 lane(賞品エリア/演出種別の参照用、ADR-0012)
    public void _NotifyCartGoaled(int startLane, int goalLane)
    {
        if (carts == null) return;
        if (startLane < 0 || startLane >= carts.Length) return;

        _goaledCount++;

        bool laneOccupied = (participantPlayerIds != null
                             && startLane < participantPlayerIds.Length
                             && participantPlayerIds[startLane] != -1);
        int kind = (_effectKinds != null && goalLane >= 0 && goalLane < _effectKinds.Length)
                   ? _effectKinds[goalLane] : 0;

        bool prizeValid = (prizeAreas != null && goalLane >= 0 && goalLane < prizeAreas.Length
                           && prizeAreas[goalLane] != null);

        // 壁の色は到達時に即染色(A/B 両モード共通、2026-05-29)。空席カートは染めない。
        // 演出(爆発/紙吹雪)の発火タイミングはモード別(下)のまま:
        //   B=個別到達時に即発火 / A=全員ゴール後の _FireFinale で一斉発火。
        // A モードの _FireFinale も _SetWallColor を呼ぶが、到達時に染め済みのため冪等
        // (Late Joiner が到達検知を取りこぼした場合の保険として残置)。
        if (laneOccupied && prizeValid)
        {
            prizeAreas[goalLane]._SetWallColor(carts[startLane].GetCartColor());
        }

        // B モード: 個別到達時に演出も即発火(発火位置は終点 Prize)
        if (!simultaneousFinale && laneOccupied && prizeValid)
        {
            prizeAreas[goalLane].PlayEffect(kind, true);
        }

        Debug.Log("[GameManager] CartGoaled start=" + startLane + " goal=" + goalLane
                  + " count=" + _goaledCount + "/" + carts.Length
                  + " kind=" + kind + " occupied=" + laneOccupied);

        // 全カート完走でフィナーレ判定(空席含む)
        if (_goaledCount >= carts.Length && !_finaleArmed)
        {
            _finaleArmed = true;
            if (simultaneousFinale)
            {
                // UI 表示同期のため SendCustomEventDelayedSeconds から CountdownUI 経由に置換。
                // CountdownUI が targetServerTime をサーバー時刻ベースで監視し、0 到達で _FireFinale を発火する。
                if (countdownUIs != null && countdownUIs.Length > 0)
                {
                    double targetTime = Networking.GetServerTimeInSeconds() + finaleCountdownSeconds;
                    bool anyStarted = false;
                    for (int i = 0; i < countdownUIs.Length; i++)
                    {
                        if (countdownUIs[i] == null) continue;
                        // 配列の最初の有効な要素にだけ _FireFinale コールバックを渡す(複数発火防止)
                        string cb = anyStarted ? "" : nameof(_FireFinale);
                        countdownUIs[i]._StartCountdown(targetTime, cb, true);
                        anyStarted = true;
                    }
                    if (!anyStarted)
                    {
                        // 全要素が null だった場合の保険(従来の直叩きにフォールバック)
                        SendCustomEventDelayedSeconds(nameof(_FireFinale), finaleCountdownSeconds);
                    }
                }
                else
                {
                    // CountdownUI 未バインド時の保険(従来の直叩きにフォールバック)
                    SendCustomEventDelayedSeconds(nameof(_FireFinale), finaleCountdownSeconds);
                }
            }
            else
            {
                // B モード: 個別発火済 → 1.5 秒後に ResultDisplay
                SendCustomEventDelayedSeconds(nameof(_EnterResultDisplay), POST_FINALE_DELAY);
            }
        }
    }

    // A モード: 全レーン一斉発火 + 共通 SE 1 発、続けて ResultDisplay 遷移を予約。
    // 占有判定は Cart の起点 lane(participantPlayerIds[startLane]) で行い、
    // 発火位置は各 Cart の終点 lane(carts[i].GoalLaneIndex)に対応する Prize で発火する(ADR-0012)。
    public void _FireFinale()
    {
        if (carts != null && prizeAreas != null && _effectKinds != null && participantPlayerIds != null)
        {
            for (int i = 0; i < carts.Length; i++)
            {
                if (carts[i] == null) continue;
                if (i >= participantPlayerIds.Length || participantPlayerIds[i] == -1) continue;

                int goalLane = carts[i].GoalLaneIndex;
                if (goalLane < 0 || goalLane >= prizeAreas.Length) continue;
                if (prizeAreas[goalLane] == null) continue;
                int kind = (goalLane < _effectKinds.Length) ? _effectKinds[goalLane] : 0;
                // ゴール到達カートの色で壁を染める(Phase 6 追加)
                prizeAreas[goalLane]._SetWallColor(carts[i].GetCartColor());
                // withIndividualSound=false: 共通 SE と二重発音を避ける
                prizeAreas[goalLane].PlayEffect(kind, false);
            }
        }
        if (finaleSharedAudio != null) finaleSharedAudio.Play();

        SendCustomEventDelayedSeconds(nameof(_EnterResultDisplay), POST_FINALE_DELAY);
    }

    // 賞品エリア(GoalBarrier より奥 = z < PRIZE_AREA_Z_THRESHOLD)に居る LocalPlayer を
    // defaultSpawn へ戻す。各クライアントが自分自身を判定・移動(VRC TeleportTo Local 制約回避)。
    // 観戦エリア(MainFloor 上)のプレイヤーには影響しない。RUNNING 開始時 / IDLE リセット時に使用。
    private void _TeleportLocalToSpawnIfInPrize()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer != null && defaultSpawn != null
            && localPlayer.GetPosition().z < PRIZE_AREA_Z_THRESHOLD)
        {
            localPlayer.TeleportTo(defaultSpawn.position, defaultSpawn.rotation);
        }
    }

    // ResultDisplay 遷移は Master が主導(gameState 同期のため)。
    // Master 以外は OnDeserialization 経由で _ApplyState される。
    // Phase 8 Option B: 結果表示後 resultHoldSeconds 秒で _ReturnToIdle を自動予約。
    // IDLE 遷移で卓をリセット(Cart 起点復帰・着座枠クリア・賞品エリア滞在者を起点へ)するが、
    // 結果 UI は IDLE で隠さず次の START(RUNNING)まで残す。
    public void _EnterResultDisplay()
    {
        if (!Networking.IsMaster) return;
        if (gameState != STATE_RUNNING) return;

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        gameState = STATE_RESULT_DISPLAY;
        RequestSerialization();
        _ApplyState();

        // 結果を resultHoldSeconds 秒見せた後、自動で卓リセット(→ IDLE)。
        SendCustomEventDelayedSeconds(nameof(_ReturnToIdle), resultHoldSeconds);
    }

    public void _ReturnToIdle()
    {
        if (!Networking.IsMaster) return;
        if (gameState != STATE_RESULT_DISPLAY) return;

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        // 参加者配列リセット(次ラウンド向け)
        if (participantPlayerIds != null)
        {
            for (int i = 0; i < participantPlayerIds.Length; i++) participantPlayerIds[i] = -1;
        }
        gameState = STATE_IDLE;
        RequestSerialization();
        _ApplyState();
    }

    // ADR-0012: seed ^ 0xBEEF を派生 RNG として使う Fisher-Yates シャッフル
    // 0=none, 1=explosion, 2=confetti
    public int[] ComputeEffectAssignment(int rngSeed, int n, int e, int c)
    {
        var result = new int[n];
        if (n <= 0) return result;

        var idx = new int[n];
        for (int i = 0; i < n; i++) idx[i] = i;

        var rng = new System.Random(rngSeed ^ EFFECT_RNG_MASK);
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            int tmp = idx[i]; idx[i] = idx[j]; idx[j] = tmp;
        }

        int eClamped = (e < 0) ? 0 : (e > n ? n : e);
        int cClamped = (c < 0) ? 0 : (c > n - eClamped ? n - eClamped : c);

        for (int i = 0; i < eClamped; i++) result[idx[i]] = PrizeArea.KIND_EXPLOSION;
        for (int i = 0; i < cClamped; i++) result[idx[eClamped + i]] = PrizeArea.KIND_CONFETTI;
        return result;
    }
}
