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

    [Header("References")]
    public GameManager gameManager;
    public AmidakujiGenerator generator;

    // ローカル状態(UdonSynced 0 個、ローカル算出のみ)
    // 起点1 + (横線渡り 11 段 × 2 waypoint) + 終点1 = 24 が理論上限
    private const int MAX_WAYPOINTS = 24;
    private Vector3[] _waypoints;
    private float[] _cumulativeDist;
    private int _waypointCount;
    private float _totalDuration;
    private bool _isLocalSeated;
    private bool _isExitingByGoal;

    void Start()
    {
        _waypoints = new Vector3[MAX_WAYPOINTS];
        _cumulativeDist = new float[MAX_WAYPOINTS];
        _waypointCount = 0;
        _totalDuration = 0f;
    }

    // GameManager._ApplyState() から同フレームで呼ばれる(Rebuild 完了後)。
    // これにより Joiner 側でも横線初期化済の状態で ComputePath できる。
    public void _OnRaceStarted()
    {
        if (gameManager == null || generator == null) return;
        ComputePath(gameManager.seed, laneIndex);
        if (_waypointCount > 0)
        {
            transform.position = _waypoints[0];
        }
    }

    public void _OnRaceReset()
    {
        _waypointCount = 0;
        _totalDuration = 0f;
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

        _cumulativeDist[0] = 0f;
        for (int i = 1; i < n; i++)
        {
            _cumulativeDist[i] = _cumulativeDist[i - 1]
                + Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
        }
        float total = _cumulativeDist[n - 1];
        _totalDuration = (speed > 0.0001f) ? (total / speed) : 0f;

        // Phase 3 検証用ログ(Phase 6 で削除)。string.Concat より + 演算子のほうが UdonSharp で安定。
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
        var local = Networking.LocalPlayer;
        if (local == null) return;
        station.UseStation(local);
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (player == null || !player.isLocal) return;
        _isLocalSeated = true;
        _isExitingByGoal = false;
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player == null) return;
        if (player.isLocal) _isLocalSeated = false;
        HandleExit(player);
    }

    private void HandleExit(VRCPlayerApi player)
    {
        // Phase 4 で _isExitingByGoal=true 分岐(正常退出)追加、Phase 3 は常に false
        if (_isExitingByGoal) return;
        // Phase 3 ではリタイア時のカート位置操作はしない。
        // カートは Running 中ならそのまま終端まで走行継続(空席扱いは Phase 4 で
        // participantPlayerIds[laneIndex] = -1 として正式実装)。
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (!value || !_isLocalSeated || station == null) return;
        var local = Networking.LocalPlayer;
        if (local == null) return;
        station.ExitStation(local);
    }
}
