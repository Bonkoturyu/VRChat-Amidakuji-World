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

    // 着座者同期(architecture.md §着座者同期 / tasklist Phase 4 パターン A)
    // Cart Owner が書込 → Master の OnDeserialization で gameManager._RegisterParticipant 集約
    [UdonSynced] public int seatedPlayerId = -1;

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

    void Start()
    {
        _waypoints = new Vector3[MAX_WAYPOINTS];
        _cumulativeDist = new float[MAX_WAYPOINTS];
        _waypointCount = 0;
        _totalDuration = 0f;
        _hasNotifiedGoal = false;
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
        if (gameManager != null) gameManager._NotifyCartGoaled(laneIndex);

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
            _isLocalSeated = true;
            _isExitingByGoal = false;

            // 着座者が Cart の Owner となり seatedPlayerId を書込 → Master が OnDeserialization で集約
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(player, gameObject);
            }
            seatedPlayerId = player.playerId;
            RequestSerialization();

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
                // リタイア: 参加者枠を空ける(Cart Owner のまま seatedPlayerId=-1 書込)
                if (!Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(player, gameObject);
                }
                seatedPlayerId = -1;
                RequestSerialization();

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
    }

    private void _TeleportToPrizeArea(VRCPlayerApi player)
    {
        if (gameManager == null) return;
        if (gameManager.prizeAreas == null) return;
        if (laneIndex < 0 || laneIndex >= gameManager.prizeAreas.Length) return;
        var area = gameManager.prizeAreas[laneIndex];
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
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (!value || !_isLocalSeated || station == null) return;
        var local = Networking.LocalPlayer;
        if (local == null) return;
        station.ExitStation(local);
    }
}
