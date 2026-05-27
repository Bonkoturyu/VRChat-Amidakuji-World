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
        if (gameManager == null || generator == null) return;
        ComputePath(gameManager.seed, laneIndex);
        if (_waypointCount > 0)
        {
            transform.position = _waypoints[0];
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

        int segIndex = 0;
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
                float zBar = generator.SegZ(seg);
                _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, zBar);
                currentLane += dir;
                _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, zBar);
            }
        }
        _waypoints[n++] = new Vector3(generator.LaneX(currentLane), 0f, generator.BOTTOM_Y);

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

        // Phase 3/4 検証用ログ(Phase 6 で削除)。
        string log = "[CartController L" + laneIndex + "] n=" + n
                     + " dist=" + total + " dur=" + _totalDuration + " goal=" + currentLane;
        for (int i = 0; i < n; i++)
        {
            log = log + " WP[" + i + "]=" + _waypoints[i];
        }
        Debug.Log(log);
    }

    // ADR-0007: VRC_Station と UdonBehaviour 同居構成では Use 表示のため Interact() 実装が必要
    public override void Interact()
    {
        if (station == null) return;
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
            // Idle 以外で着座した場合は即時退出(走行中・ResultDisplay 中の再着座を防止)
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

        // Late Joiner シナリオ予防(Phase 5 持越し): ResultDisplay 中に参加した Joiner が
        // Cart.OnDeserialization で古い PID を受信し _RegisterParticipant に流して
        // participantPlayerIds[] に誤登録するのを防ぐ。
        // _ReturnToIdle の全リセットは ResultDisplay 終了時(10 秒後)まで走らないため、
        // それより前のタイミングをカバーする(OnStationExited リタイア分岐と対称)。
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(local, gameObject);
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
        if (Networking.IsMaster && gameManager != null)
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
