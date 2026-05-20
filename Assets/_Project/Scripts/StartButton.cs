using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// Phase 3 仮実装: Master 二重ガード + RequestStart 呼出のみ。
// Phase 5 で「視覚切替(Active/Inactive Material)」「参加者数チェック」「gameState 連動グレーアウト」を追加。
public class StartButton : UdonSharpBehaviour
{
    public GameManager gameManager;

    public override void Interact()
    {
        if (!Networking.IsMaster) return;
        if (gameManager == null) return;
        gameManager.RequestStart();
    }
}
