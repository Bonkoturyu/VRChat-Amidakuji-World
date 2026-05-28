using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

// ResultDisplay 掲示 UI(Phase 5、Rev.4 で JP/EN + Cart カラー対応)。
// STATE_RESULT_DISPLAY 進入時に GameManager から _Show() が呼ばれ、
// carts[].GoalLaneIndex / participantPlayerIds[] / carts[].colorIndex から
// 「カート N → ゴール M (プレイヤー名)」の 4 行を Cart カラーで塗って表示する。
// 表示番号は内部 0-indexed を +1 して 1-indexed 化(UI 上のみ)。
// STATE_IDLE 復帰時に _Hide() が呼ばれて非表示。
public class ResultDisplayUI : UdonSharpBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    [Tooltip("JP/EN 切替のため LocalizationManager を参照")]
    public LocalizationManager localizationManager;
    [Tooltip("Cart カラー Hex 解決用(任意、null なら全行白)")]
    public ColorPreferenceManager colorManager;

    [Header("Display")]
    [Tooltip("表示中だけ Active にするルート(Canvas など)")]
    public GameObject displayRoot;
    [Tooltip("ヘッダー(レース結果 / Race Result)を書き込む TMP")]
    public TextMeshProUGUI headerText;
    [Tooltip("カート行を書き込む TMP")]
    public TextMeshProUGUI text;

    [Header("Localized Labels")]
    public string headerJP = "レース結果";
    public string headerEN = "Race Result";
    public string separatorLine = "=====================";
    public string emptyLabelJP = "(空席)";
    public string emptyLabelEN = "(empty)";
    public string retiredLabelJP = "(退出)";
    public string retiredLabelEN = "(retired)";
    public string cartWordJP = "カート";
    public string cartWordEN = "Cart";
    public string goalWordJP = "ゴール";
    public string goalWordEN = "Goal";

    [Header("Inactive Color")]
    [Tooltip("空席・退出行の色(リッチテキスト #RRGGBB)")]
    public string inactiveHex = "808080";

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

    // LocalizationManager._ToggleLanguage() から呼ばれる(表示中のみ再描画)
    public void _RefreshLanguage()
    {
        if (displayRoot != null && displayRoot.activeSelf) _RefreshText();
    }

    private void _RefreshText()
    {
        if (text == null || gameManager == null) return;
        if (gameManager.carts == null || gameManager.participantPlayerIds == null) return;

        bool en = (localizationManager != null) && localizationManager.isEnglish;
        string cartWord = en ? cartWordEN : cartWordJP;
        string goalWord = en ? goalWordEN : goalWordJP;
        string emptyLabel = en ? emptyLabelEN : emptyLabelJP;
        string retiredLabel = en ? retiredLabelEN : retiredLabelJP;

        if (headerText != null) headerText.text = (en ? headerEN : headerJP) + "\n" + separatorLine;

        string output = "";
        for (int i = 0; i < gameManager.carts.Length; i++)
        {
            var cart = gameManager.carts[i];
            int goalLane = (cart != null) ? cart.GoalLaneIndex : -1;
            int pid = (i < gameManager.participantPlayerIds.Length)
                      ? gameManager.participantPlayerIds[i] : -1;
            int colorIdx = (cart != null) ? cart.colorIndex : -1;

            string name;
            bool isRetired = false;
            if (pid == -1)
            {
                name = emptyLabel;
            }
            else
            {
                var player = VRCPlayerApi.GetPlayerById(pid);
                if (player != null && player.IsValid())
                {
                    name = player.displayName;
                }
                else
                {
                    name = retiredLabel;
                    isRetired = true;
                }
            }

            int cartNumber = i + 1;
            string goalNumber = (goalLane >= 0) ? (goalLane + 1).ToString() : "-";

            string hex;
            bool useCartColor = (pid != -1) && !isRetired && colorIdx >= 0 && colorManager != null;
            if (useCartColor)
            {
                hex = _ColorToHex(colorManager.GetPaletteColor(colorIdx));
            }
            else
            {
                hex = inactiveHex;
            }

            output += "<color=#" + hex + ">"
                   + cartWord + " " + cartNumber + " → " + goalWord + " " + goalNumber
                   + "   " + name
                   + "</color>\n";
        }
        text.text = output;
    }

    private string _ColorToHex(Color c)
    {
        int r = Mathf.Clamp((int)(c.r * 255f), 0, 255);
        int g = Mathf.Clamp((int)(c.g * 255f), 0, 255);
        int b = Mathf.Clamp((int)(c.b * 255f), 0, 255);
        return _ToHex2(r) + _ToHex2(g) + _ToHex2(b);
    }

    private string _ToHex2(int n)
    {
        const string hex = "0123456789ABCDEF";
        int hi = (n >> 4) & 0xF;
        int lo = n & 0xF;
        return hex.Substring(hi, 1) + hex.Substring(lo, 1);
    }
}
