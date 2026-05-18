using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRCStation = VRC.SDK3.Components.VRCStation;

public class CartController : UdonSharpBehaviour
{
    // --- Inspector: Common (Phase 3 以降も維持) ---
    public int laneIndex;
    public float speed = 2.0f;
    public VRCStation station;

    // --- Inspector: Phase 2 暫定 (Phase 3 で ComputePath(seed, lane) に置換) ---
    public bool startOnEnter = true;
    public bool lookAtMovingDirection = false;
    public Transform[] waypointMarkers;

    // --- Local state (UdonSynced 0 個、Phase 2 はローカル単独走行) ---
    private int _state;
    private double _raceStartTime;
    private Vector3[] _waypoints;
    private float[] _cumulativeDist;
    private float _totalDuration;
    private bool _isLocalSeated;
    private bool _isExitingByGoal;

    private const int STATE_IDLE = 0;
    private const int STATE_RUNNING = 1;
    private const int STATE_GOALED = 2;

    void Start()
    {
        if (waypointMarkers == null || waypointMarkers.Length < 2)
        {
            Debug.LogError("[CartController] waypointMarkers requires >= 2 entries (laneIndex=" + laneIndex + ")");
            _state = STATE_IDLE;
            return;
        }

        int n = waypointMarkers.Length;
        _waypoints = new Vector3[n];
        _cumulativeDist = new float[n];

        for (int i = 0; i < n; i++)
        {
            _waypoints[i] = waypointMarkers[i].position;
        }

        _cumulativeDist[0] = 0f;
        for (int i = 1; i < n; i++)
        {
            _cumulativeDist[i] = _cumulativeDist[i - 1] + Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
        }

        float total = _cumulativeDist[n - 1];
        _totalDuration = (speed > 0.0001f) ? (total / speed) : 0f;

        transform.position = _waypoints[0];
        _state = STATE_IDLE;
    }

    void Update()
    {
        if (_state != STATE_RUNNING) return;
        if (_waypoints == null || _waypoints.Length < 2) return;

        // ADR-0003: 生の GetServerTimeInSeconds() を引き算してはいけない
        double now = Networking.GetServerTimeInSeconds();
        double elapsed = Networking.CalculateServerDeltaTime(now, _raceStartTime);

        if (elapsed >= _totalDuration)
        {
            transform.position = _waypoints[_waypoints.Length - 1];
            _state = STATE_GOALED;
            return;
        }

        float traveled = (float)elapsed * speed;

        int segIndex = 0;
        int last = _waypoints.Length - 1;
        for (int i = 1; i <= last; i++)
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

        if (lookAtMovingDirection)
        {
            Vector3 dir = b - a;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    // ADR-0007: VRC_Station と UdonBehaviour が同じ GameObject に同居する構成では、
    // VRC_Station 自前の Use 表示は出ない(VRChat 仕様で UdonBehaviour 側の Interactable が優先される)。
    // UdonBehaviour に Interact() を実装し、内部で UseStation() を明示的に呼ぶ必要がある。
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
        if (startOnEnter) StartRace();
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player == null) return;
        if (player.isLocal) _isLocalSeated = false;
        HandleExit(player);
    }

    private void HandleExit(VRCPlayerApi player)
    {
        // Phase 4 で _isExitingByGoal=true 分岐(ゴール到達による正常退出)を追加する。
        // Phase 2 は常に false なので必ずリタイア処理に流れる。
        if (_isExitingByGoal) return;

        _state = STATE_IDLE;
        if (_waypoints != null && _waypoints.Length > 0)
        {
            transform.position = _waypoints[0];
        }
        if (lookAtMovingDirection && _waypoints != null && _waypoints.Length >= 2)
        {
            Vector3 dir = _waypoints[1] - _waypoints[0];
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    private void StartRace()
    {
        _raceStartTime = Networking.GetServerTimeInSeconds();
        _state = STATE_RUNNING;
    }

    // ADR-0007 L60-71 の Phase 2 例外規定: participantPlayerIds[] 参照を _isLocalSeated に置換
    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (!value || !_isLocalSeated || station == null) return;
        var local = Networking.LocalPlayer;
        if (local == null) return;
        station.ExitStation(local);
    }
}
