using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

// ResultDisplay 掲示 UI(Phase 5)。STATE_RESULT_DISPLAY 進入時に
// GameManager から _Show() が呼ばれ、carts[].GoalLaneIndex と participantPlayerIds[] から
// 「席 N → ゴール M (プレイヤー名)」の 4 行を生成して表示する。
// STATE_IDLE 復帰時に _Hide() が呼ばれて非表示。
public class ResultDisplayUI : UdonSharpBehaviour
{
    [Header("References")]
    public GameManager gameManager;

    [Header("Display")]
    [Tooltip("表示中だけ Active にするルート(Canvas など)")]
    public GameObject displayRoot;
    [Tooltip("4 行テキストを書き込む TextMeshProUGUI")]
    public TextMeshProUGUI text;

    void Start()
    {
        if (displayRoot != null) displayRoot.SetActive(false);
    }

    // GameManager._ApplyState() の STATE_RESULT_DISPLAY 分岐から呼ばれる
    public void _Show()
    {
        _RefreshText();
        if (displayRoot != null) displayRoot.SetActive(true);
    }

    // GameManager._ApplyState() の STATE_IDLE 分岐から呼ばれる
    public void _Hide()
    {
        if (displayRoot != null) displayRoot.SetActive(false);
    }

    private void _RefreshText()
    {
        if (text == null || gameManager == null) return;
        if (gameManager.carts == null || gameManager.participantPlayerIds == null) return;

        // 「席 N → ゴール M (プレイヤー名 or 空席 or 退出)」を carts.Length 行構築
        string output = "";
        for (int i = 0; i < gameManager.carts.Length; i++)
        {
            var cart = gameManager.carts[i];
            int goalLane = (cart != null) ? cart.GoalLaneIndex : -1;
            int pid = (i < gameManager.participantPlayerIds.Length)
                      ? gameManager.participantPlayerIds[i] : -1;

            string name;
            if (pid == -1)
            {
                name = "(空席)";
            }
            else
            {
                var player = VRCPlayerApi.GetPlayerById(pid);
                name = (player != null && player.IsValid()) ? player.displayName : "(退出)";
            }

            output += "席 " + i + " → ゴール " + goalLane + "   " + name + "\n";
        }
        text.text = output;
    }
}
