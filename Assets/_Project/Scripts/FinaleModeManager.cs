
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

// ADR-0012 §7 の Player Persistence パターン実装。
// B モード(simultaneousFinale=false)の選好を「同一人物が Master として再入場」時に復元する。
// 復元は Master のみが行い、Inspector 既定値(A モード)を上書きする形になる。
public class FinaleModeManager : UdonSharpBehaviour
{
    [Header("References")]
    public GameManager gameManager;

    // Persistence Key(ワールド固有、ADR-0012 §7 確定)
    private const string KEY_SIMULTANEOUS_FINALE = "amidakuji.simultaneousFinale";

    // FinaleModeToggle から呼ばれる。Master かつ Idle のときのみ反転 + Persistence 書込。
    public void _ToggleFinaleMode()
    {
        if (gameManager == null) return;
        if (!Networking.IsMaster) return;
        if (gameManager.gameState != GameManager.STATE_IDLE) return;

        // GameManager の Owner を取得(UdonSynced 変数を書き換えるため)
        if (!Networking.IsOwner(gameManager.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameManager.gameObject);
        }
        gameManager.simultaneousFinale = !gameManager.simultaneousFinale;

        // Persistence 書込(LocalPlayer の領域、SDK が自動的に全クライアントへ同期)
        PlayerData.SetBool(KEY_SIMULTANEOUS_FINALE, gameManager.simultaneousFinale);

        // UdonSynced 変数の即時同期
        gameManager.RequestSerialization();
    }

    // 自分が入室した直後、自分の永続データ復元が完了したら Master 判定して復元を試みる。
    // 他人の Restored イベント発火は無視する(他人の Persistence は使わない)。
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player == null) return;
        if (player.isLocal && Networking.IsMaster)
        {
            _TryRestoreFinaleMode();
        }
    }

    // 他者退出 → 自分が Master 昇格した場合の復元フック。
    // OnPlayerRestored は新規入室時のみ発火するため、Master 昇格はこちらでカバーする。
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Networking.IsMaster)
        {
            _TryRestoreFinaleMode();
        }
    }

    private void _TryRestoreFinaleMode()
    {
        if (gameManager == null) return;
        var local = Networking.LocalPlayer;
        if (local == null) return;

        bool restored;
        if (!PlayerData.TryGetBool(local, KEY_SIMULTANEOUS_FINALE, out restored))
        {
            // 未保存(初回入場 / 別人 Master)の場合は何もしない → Inspector 既定値継続
            return;
        }

        if (gameManager.simultaneousFinale == restored)
        {
            // 既存値と同じなら no-op(Serialize 不要)
            return;
        }

        if (!Networking.IsOwner(gameManager.gameObject))
        {
            Networking.SetOwner(local, gameManager.gameObject);
        }
        gameManager.simultaneousFinale = restored;
        gameManager.RequestSerialization();
    }
}
