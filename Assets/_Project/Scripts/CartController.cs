using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRCStation = VRC.SDK3.Components.VRCStation;

public class CartController : UdonSharpBehaviour
{
    [Header("Cart Identity")]
    public int laneIndex;
    public float speed = 2.0f;
    public VRCStation station;

    [Header("Visual")]
    [Tooltip("着座者の好み色で染める対象 Renderer (Cart の Visual)")]
    public Renderer cartVisualRenderer;

    [Header("References")]
    public GameManager gameManager;
    public AmidakujiGenerator generator;
    [Tooltip("着座者の好み色を取得するため")]
    public ColorPreferenceManager colorManager;

    // 着座者同期(architecture.md §着座者同期 / tasklist Phase 4 パターン A)
    // Cart Owner が書込 → Master の OnDeserialization で gameManager._RegisterParticipant 集約
    [UdonSynced] public int seatedPlayerId = -1;

    // 着座者の好み色インデックス(ColorPreferenceManager.paletteColors のインデックス)。
    // -1 = 未着座 or 未選択(壁染色は defaultWallColor、Visual は sharedMaterial 既定色)
    [UdonSynced] public int colorIndex = -1;

    // ローカル状態
    // 起点1 + (横線渡り 11 段 × 2 waypoint) + 終点1 = 24 が理論上限
    private const int MAX_WAYPOINTS = 24;
    private Vector3[] _waypoints;
    private float[] _cumulativeDist;
    private int _waypointCount;
    private float _totalDuration;
    private bool _isLocalSeated;
    private bool _isExitingByGoal;
    private bool _hasNotifiedGoal;
    // ComputePath で算出した終点 lane(あみだくじを辿った結果のゴール先レーン)。
    // テレポート先 Prize / 演出種別の参照は起点 laneIndex ではなくこの値で行う
    // (ADR-0012: 演出種別は賞品エリア=終点ベースで割り当て)
    private int _goalLaneIndex;
    public int GoalLaneIndex { get { return _goalLaneIndex; } }

    // Visual 色更新用(MaterialPropertyBlock で Static Batching と両立)
    private MaterialPropertyBlock _propBlock;
    private int _lastColorIndex;
    private const string COLOR_PROP = "_Color";

    void Start()
    {
        _waypoints = new Vector3[MAX_WAYPOINTS];
        _cumulativeDist = new float[MAX_WAYPOINTS];
        _waypointCount = 0;
        _totalDuration = 0f;
        _hasNotifiedGoal = false;
        _goalLaneIndex = laneIndex;
        _propBlock = new MaterialPropertyBlock();
        _lastColorIndex = -2; // 初回 _RefreshVisualColor で必ず適用させる
        _RefreshVisualColor();
    }

    // GameManager (壁染色) / ColorPreferenceManager (Visual 同期) から呼ばれる
    public Color GetCartColor()
    {
        if (colorManager != null && colorIndex >= 0)
        {
            return colorManager.GetPaletteColor(colorIndex);
        }
        return Color.white;
    }

    // Cart Visual の色を colorIndex に追従させる。OnDeserialization / OnStationEntered /
    // ColorPreferenceManager._PropagateToSeatedCart から呼ばれる(同値 no-op で冪等)
    public void _RefreshVisualColor()
    {
        if (cartVisualRenderer == null || _propBlock == null) return;
        if (colorIndex < 0)
        {
            // 未選択は MaterialPropertyBlock クリアで sharedMaterial 既定色に戻す
            cartVisualRenderer.SetPropertyBlock(null);
        }
        else if (colorManager != null)
        {
            _propBlock.SetColor(COLOR_PROP, colorManager.GetPaletteColor(colorIndex));
            cartVisualRenderer.SetPropertyBlock(_propBlock);
        }
        _lastColorIndex = colorIndex;
    }

    // GameManager._ApplyState() から同フレームで呼ばれる(Rebuild 完了後)。
    public void _OnRaceStarted()
    {
        // #14: 参照欠落は「このカートが永久に未ゴール → _goaledCount が揃わず
        // インスタンス全体が RUNNING で固まるソフトロック」に直結する。黙って早期 return せず
        // 原因を可視化する(最終保険は GameManager 側の watchdog)。
        if (gameManager == null || generator == null)
        {
            Debug.LogError("[CartController L" + laneIndex + "] _OnRaceStarted: 参照未設定 "
                           + "(gameManager set=" + (gameManager != null)
                           + " generator set=" + (generator != null)
                           + ")。このカートは走行・ゴールせずレースが完了不能になります。");
            return;
        }
        ComputePath(gameManager.seed, laneIndex);
        if (_waypointCount > 0)
        {
            transform.position = _waypoints[0];
        }
        else
        {
            Debug.LogError("[CartController L" + laneIndex + "] _OnRaceStarted: 経路が空 "
                           + "(_waypointCount=0)。このカートはゴールせずレースが完了不能になります。");
        }
        _hasNotifiedGoal = false;
        _isExitingByGoal = false;
    }

    public void _OnRaceReset()
    {
        _waypointCount = 0;
        _totalDuration = 0f;
        _hasNotifiedGoal = false;
        _isExitingByGoal = false;
        // Idle 復帰時に Cart を起点位置へ戻す(Phase 6 修正:
        // 旧設計では _waypointCount=0 のみで position 据置だったため、
        // ResultDisplay → Idle 遷移後も Cart が終点に残る UX 不良があった)
        if (generator != null)
        {
            transform.position = new Vector3(generator.LaneX(laneIndex), 0f, generator.TOP_Y);
        }

        // Cart Owner が seatedPlayerId / colorIndex をリセット。
        // ゴール退出後も Cart Owner は退出者のまま保持され、ここまで来る。
        // _FireFinale が participantPlayerIds[] を参照して動くため、リセットは
        // ResultDisplay → Idle 遷移時 (本メソッド呼出時) まで遅延させる。
        if (Networking.IsOwner(gameObject))
        {
            if (seatedPlayerId != -1 || colorIndex != -1)
            {
                seatedPlayerId = -1;
                colorIndex = -1;
                RequestSerialization();
            }
        }
        _RefreshVisualColor();
    }

    void Update()
    {
        if (gameManager == null) return;
        if (gameManager.gameState != GameManager.STATE_RUNNING) return;
        if (_waypointCount < 2) return;

        // ADR-0003: 生の GetServerTimeInSeconds() を引き算してはいけない
        double now = Networking.GetServerTimeInSeconds();
        double elapsed = Networking.CalculateServerDeltaTime(now, gameManager.raceStartTime);

        if (elapsed < 0.0)
        {
            // Countdown バッファ中: 起点で待機
            transform.position = _waypoints[0];
            return;
        }

        if (elapsed >= _totalDuration)
        {
            transform.position = _waypoints[_waypointCount - 1];
            // 経路ベースゴール検知(各クライアント独立、二重発火防止フラグ)
            if (!_hasNotifiedGoal)
            {
                _hasNotifiedGoal = true;
                _OnReachedGoal();
            }
            return;
        }

        float traveled = (float)elapsed * speed;

        // #8 修正: ループが境界(浮動小数の丸めで traveled が終端を僅かに超える)で一度も
        // ヒットしないと、旧実装は segIndex=0 のまま先頭区間で誤補間した。初期値を終端区間に
        // 倒し「見つからない=終端付近」として扱う。通常区間ではループが正しく上書きする。
        int segIndex = _waypointCount - 2; // _waypointCount >= 2 は上の早期 return で保証済
        for (int i = 1; i < _waypointCount; i++)
        {
            if (_cumulativeDist[i] >= traveled)
            {
                segIndex = i - 1;
                break;
            }
        }

        float segStart = _cumulativeDist[segIndex];
        float segEnd = _cumulativeDist[segIndex + 1];
        float segLen = segEnd - segStart;
        float t = (segLen > 0.0001f) ? (traveled - segStart) / segLen : 0f;

        Vector3 a = _waypoints[segIndex];
        Vector3 b = _waypoints[segIndex + 1];
        transform.position = Vector3.Lerp(a, b, t);
    }

    private void _OnReachedGoal()
    {
        if (gameManager != null) gameManager._NotifyCartGoaled(laneIndex, _goalLaneIndex);

        // 着座者ローカルクライアントのみ: 自分をテレポート扱いで退出させる
        // OnStationExited 側で _isExitingByGoal 分岐に入り TeleportTo が走る
        if (_isLocalSeated && station != null)
        {
            _isExitingByGoal = true;
            var local = Networking.LocalPlayer;
            if (local != null) station.ExitStation(local);
        }
    }

    public void ComputePath(int rngSeed, int startLane)
    {
        if (generator == null) return;
        int currentLane = startLane;
        int n = 0;

        _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, generator.TOP_Y);

        int segCount = generator.SEGMENT_COUNT;
        for (int seg = 0; seg < segCount; seg++)
        {
            int dir = generator.HasBarForLane(seg, currentLane);
            if (dir != 0)
            {
                // #1 フェイルセーフ: SEGMENT_COUNT を Inspector で 11 超に増やすと
                // MAX_WAYPOINTS(=24)を超過しうる。1 段で 2 waypoint 積むため、
                // 「2 個 + 終点 1 個」を確保できない時点で打ち切る。これにより
                // IndexOutOfRange による UdonBehaviour halt(→ #14 のソフトロック)を防ぐ。
                // SEGMENT_COUNT=11 固定運用では一度も発火しない(挙動不変)。
                if (n + 3 > MAX_WAYPOINTS)
                {
                    Debug.LogError("[CartController L" + laneIndex + "] waypoint overflow: MAX_WAYPOINTS("
                                   + MAX_WAYPOINTS + ") 超過のため経路を打ち切り。"
                                   + "SEGMENT_COUNT を増やした場合は MAX_WAYPOINTS も更新が必要。");
                    break;
                }
                float zBar = generator.SegZ(seg);
                _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, zBar);
                currentLane += dir;
                _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, zBar);
            }
        }
        // 終点。フェイルセーフで余地を確保しているため通常 n < MAX だが、念のため範囲確認。
        if (n < MAX_WAYPOINTS)
        {
            _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, generator.BOTTOM_Y);
        }

        _waypointCount = n;
        _goalLaneIndex = currentLane;

        _cumulativeDist[0] = 0f;
        for (int i = 1; i < n; i++)
        {
            _cumulativeDist[i] = _cumulativeDist[i - 1]
                + Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
        }
        float total = _cumulativeDist[n - 1];
        _totalDuration = (speed > 0.0001f) ? (total / speed) : 0f;
        // #3: Phase 3/4 検証用の逐次ログ(本来 Phase 6 で削除予定)を撤去。
        // STATE_RUNNING 遷移毎にカート台数分呼ばれ、最大 24 個の座標を文字列連結して
        // 約 30 個の string を生成していた(Quest で GC プレッシャ)。
    }

    // ADR-0007: VRC_Station と UdonBehaviour 同居構成では Use 表示のため Interact() 実装が必要
    public override void Interact()
    {
        if (station == null) return;
        // #7/#16: 着座開始は STATE_IDLE 限定。RESULT_DISPLAY からの予約着座→直接 START という
        // 近道は仕様として採らない(sticky な seatedPlayerId によるゴースト参加者を避ける)。
        // 結果表示中の卓リセットは RESULT_DISPLAY→IDLE 遷移(_ReturnToIdle)に一本化する。
        if (gameManager != null && gameManager.gameState != GameManager.STATE_IDLE) return;
        var local = Networking.LocalPlayer;
        if (local == null) return;
        station.UseStation(local);
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (player == null) return;

        if (player.isLocal)
        {
            // #7/#16: 着座は STATE_IDLE 限定に統一。IDLE 以外(RUNNING / RESULT_DISPLAY)で
            // 着座しようとしたら即退出させる。RESULT_DISPLAY 中の卓は「結果を見る凍結状態」で、
            // リセット(→IDLE)後に着座する設計(予約着座は採らない)。Interact 側でも IDLE 限定
            // ガードしているが、ここでも弾いて「着座は IDLE のみ」を権威的に担保する。
            if (gameManager != null && gameManager.gameState != GameManager.STATE_IDLE)
            {
                if (station != null) station.ExitStation(player);
                return;
            }

            _isLocalSeated = true;
            _isExitingByGoal = false;

            // 着座者が Cart の Owner となり seatedPlayerId と colorIndex を書込
            // → Master が OnDeserialization で _RegisterParticipant 集約
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(player, gameObject);
            }
            seatedPlayerId = player.playerId;
            if (colorManager != null)
            {
                colorIndex = colorManager.localColorIndex;
            }
            RequestSerialization();

            // Local 即時 Visual 反映(OnDeserialization は Owner では発火しない)
            _RefreshVisualColor();

            // Master 自身着座時は OnDeserialization が呼ばれないため直接呼出(対称性)
            if (Networking.IsMaster && gameManager != null)
            {
                gameManager._RegisterParticipant(laneIndex, player.playerId);
            }
        }
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player == null) return;

        if (player.isLocal)
        {
            _isLocalSeated = false;

            if (_isExitingByGoal)
            {
                // 正常退出(ゴール到達): 賞品エリアへテレポート。
                // 即時 TeleportTo すると VRC_Station の Player Exit Location (= Seat) への
                // 内部移動が後勝ちで上書きしてしまうため、1 フレーム遅延で実行する。
                SendCustomEventDelayedFrames(nameof(_DelayedTeleportToPrize), 1);
            }
            else
            {
                // リタイア: 参加者枠を空ける(Cart Owner のまま seatedPlayerId=-1 / colorIndex=-1)
                if (!Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(player, gameObject);
                }
                seatedPlayerId = -1;
                colorIndex = -1;
                RequestSerialization();

                _RefreshVisualColor();

                if (Networking.IsMaster && gameManager != null)
                {
                    gameManager._RegisterParticipant(laneIndex, -1);
                }
            }
            _isExitingByGoal = false;
        }
    }

    public void _DelayedTeleportToPrize()
    {
        var local = Networking.LocalPlayer;
        if (local == null) return;
        _TeleportToPrizeArea(local);

        // 注意: seatedPlayerId / colorIndex のリセットはここでは行わない。
        // A モードの _FireFinale は participantPlayerIds[] を参照して Cart 占有を判定するため、
        // ゴール瞬間にリセットすると壁色染色・演出が走らなくなる。
        // リセットは ResultDisplay → Idle 遷移時の _OnRaceReset() で Cart Owner が行う。
        // Late Joiner 誤登録予防は OnDeserialization 側で ResultDisplay 中の Master 集約を
        // スキップすることで担保する。
    }

    private void _TeleportToPrizeArea(VRCPlayerApi player)
    {
        if (gameManager == null) return;
        if (gameManager.prizeAreas == null) return;
        // ADR-0012: テレポート先はあみだくじの結果到達した「終点 lane」の Prize
        // (Cart の起点 laneIndex ではなく、ComputePath で算出した _goalLaneIndex を使う)
        if (_goalLaneIndex < 0 || _goalLaneIndex >= gameManager.prizeAreas.Length) return;
        var area = gameManager.prizeAreas[_goalLaneIndex];
        if (area == null || area.teleportTarget == null) return;

        player.TeleportTo(area.teleportTarget.position, area.teleportTarget.rotation);
    }

    public override void OnDeserialization()
    {
        // Master 集約: Cart Owner 側で seatedPlayerId が更新された後、Master 側がここで反映
        // (Master 自身が Cart Owner の場合は OnStationEntered/Exited 内で直接呼出済 → 同値 no-op)
        // ResultDisplay 中は Late Joiner が古い seatedPlayerId を受信して participantPlayerIds[] を
        // 誤上書きするのを防ぐため、Master 集約をスキップする(結果表示の整合性確保)。
        if (Networking.IsMaster && gameManager != null
            && gameManager.gameState != GameManager.STATE_RESULT_DISPLAY)
        {
            gameManager._RegisterParticipant(laneIndex, seatedPlayerId);
        }

        // colorIndex 同期: 他クライアントは Owner の書込を受信して Visual を追従
        if (colorIndex != _lastColorIndex)
        {
            _RefreshVisualColor();
        }
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (!value || !_isLocalSeated || station == null) return;
        var local = Networking.LocalPlayer;
        if (local == null) return;
        station.ExitStation(local);
    }
}
