using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameManager : UdonSharpBehaviour
{
    // gameState: Phase 3 では IDLE / RUNNING のみ使用。
    // COUNTDOWN は Phase 5 で UI 導入時に挿入する予約番号。
    // RESULT_DISPLAY は Phase 4-5 で追加。
    public const int STATE_IDLE = 0;
    public const int STATE_COUNTDOWN = 1;
    public const int STATE_RUNNING = 2;
    public const int STATE_RESULT_DISPLAY = 3;

    // 3 秒のクロック同期バッファ(VRChat Sync 遅延吸収用)
    public const float COUNTDOWN_BUFFER = 3.0f;

    [Header("References")]
    public AmidakujiGenerator generator;
    public CartController[] carts;

    [Header("Debug / Reproducibility")]
    [Tooltip("ON のとき debugSeed をそのまま使う(再現テスト用、本番は OFF)")]
    public bool useDebugSeed = false;
    public int debugSeed = 12345;

    [UdonSynced] public int seed;
    [UdonSynced] public int gameState;
    [UdonSynced] public double raceStartTime;

    void Start()
    {
        seed = 0;
        gameState = STATE_IDLE;
        raceStartTime = 0.0;
    }

    // StartButton から呼ばれる。Master 限定。
    public void RequestStart()
    {
        if (!Networking.IsMaster) return;
        if (gameState != STATE_IDLE) return;

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        seed = useDebugSeed ? debugSeed : (int)System.DateTime.Now.Ticks;
        raceStartTime = Networking.GetServerTimeInSeconds() + COUNTDOWN_BUFFER;
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
        if (gameState == STATE_RUNNING)
        {
            if (generator != null) generator.Rebuild(seed);
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
            Debug.Log("[GameManager] state=Idle");
        }
    }
}
