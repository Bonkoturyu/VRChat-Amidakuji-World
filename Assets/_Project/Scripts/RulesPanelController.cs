using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

// Rev.4: Tab 切替(参加/観戦/演出モード)+ JP/EN 切替の RulesPanel コントローラ。
// Tab・言語は Local 状態(非 Synced)。各プレイヤーが個別に選択可能。
public class RulesPanelController : UdonSharpBehaviour
{
    [Header("References")]
    public LocalizationManager localizationManager;
    public ColorPreferenceManager colorManager;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI langToggleLabel;
    public TextMeshProUGUI[] tabButtonLabels;  // 4 個、Tab 切替で言語別に書き換え
    public Renderer[] tabButtonRenderers;       // 4 個、Active/Inactive 視覚切替

    [Header("Color Palette (Tab4 用)")]
    [Tooltip("カラーパレットボタンの Renderer 群(ColorPreferenceManager.paletteColors と同数・同順)")]
    public Renderer[] colorPaletteRenderers;
    [Tooltip("選択中の色枠を示す Renderer(任意、null なら未使用)")]
    public Renderer colorPaletteSelectionHighlight;

    [Header("Materials")]
    public Material tabActiveMaterial;
    public Material tabInactiveMaterial;
    [Tooltip("選択中のパレットボタン枠用(任意、null なら未使用)")]
    public Material colorSelectedFrameMaterial;

    [Header("Content - Title")]
    [TextArea(2, 4)] public string titleJP = "Ghost-Leg Express\n巨大あみだくじワールド";
    [TextArea(2, 4)] public string titleEN = "Ghost-Leg Express\n- Giant Ladder Lottery -";

    [Header("Content - Tab 1 (Join)")]
    [TextArea(4, 10)] public string tab1JP =
        "▼ 参加方法\n\n" +
        "1. 入口のカートに座る (Interact)\n" +
        "2. オーナーが Start ボタンを押すと開始\n" +
        "3. カートが自動でゴールへ走る\n" +
        "4. 賞品エリアにテレポート";
    [TextArea(4, 10)] public string tab1EN =
        "▼ How to Join\n\n" +
        "1. Sit on a cart at the entry (Interact)\n" +
        "2. Owner presses the Start button\n" +
        "3. Cart auto-runs through the ladder\n" +
        "4. Teleport to a prize room at the goal";

    [Header("Content - Tab 2 (Watch)")]
    [TextArea(4, 10)] public string tab2JP =
        "▼ 観戦と退出\n" +
        "床を自由に走ってカートを追いかけて OK\n" +
        "ゴール手前のバリアより先は参加者専用\n" +
        "退出 (リタイア):\n" +
        "  Desktop: Space キー\n" +
        "  VR: ジャンプ または トリガー";
    [TextArea(4, 10)] public string tab2EN =
        "▼ Spectate & Exit\n" +
        "Walk anywhere on the floor and chase carts.\n" +
        "Barrier near the goal blocks non-participants.\n" +
        "Exit / Retire:\n" +
        "  Desktop: Space key\n" +
        "  VR: Jump or Trigger";

    [Header("Content - Tab 3 (Mode)")]
    [TextArea(4, 10)] public string tab3JP =
        "▼ 演出モード (オーナー切替)\n" +
        "オーナーが Idle 中のみ切替可能\n" +
        "  A: 全員ゴール後に一斉発火 (既定)\n" +
        "  B: 個別到達時に即発火\n" +
        "設定はワールドに保存され、次回も復元";
    [TextArea(4, 10)] public string tab3EN =
        "▼ Finale Mode (Owner toggle)\n" +
        "Only the instance owner can toggle (Idle only)\n" +
        "  A: Synced finale after all goal (default)\n" +
        "  B: Each cart fires on arrival\n" +
        "The choice is saved per owner.";

    [Header("Content - Tab 4 (Color)")]
    [TextArea(4, 10)] public string tab4JP =
        "▼ カラー設定\n" +
        "下のパレットから好きな色を選択\n" +
        "選んだ色は次回入場時も保存されます\n" +
        "カートに座ると、その色になり\n" +
        "ゴール時に賞品エリアもその色に染まる";
    [TextArea(4, 10)] public string tab4EN =
        "▼ Color\n" +
        "Pick your favorite from the palette below.\n" +
        "Your choice is saved for next visits.\n" +
        "Your cart and the prize wall will be\n" +
        "tinted to your color on goal.";

    [Header("Tab Button Labels")]
    public string tab1LabelJP = "1. 参加";
    public string tab2LabelJP = "2. 観戦";
    public string tab3LabelJP = "3. 設定";
    public string tab4LabelJP = "4. 色";
    public string tab1LabelEN = "1. Join";
    public string tab2LabelEN = "2. Watch";
    public string tab3LabelEN = "3. Mode";
    public string tab4LabelEN = "4. Color";

    private int _currentTab;

    void Start()
    {
        _currentTab = 0;
        _Refresh();
    }

    // Tab ボタン GameObject から SendCustomEvent で呼ぶ
    public void _SelectTab1() { _currentTab = 0; _Refresh(); }
    public void _SelectTab2() { _currentTab = 1; _Refresh(); }
    public void _SelectTab3() { _currentTab = 2; _Refresh(); }
    public void _SelectTab4() { _currentTab = 3; _Refresh(); }

    // 言語トグル GameObject から SendCustomEvent で呼ぶ
    // LocalizationManager 経由で他 UI にも反映
    public void _ToggleLanguage()
    {
        if (localizationManager != null) localizationManager._ToggleLanguage();
    }

    // LocalizationManager._ToggleLanguage() からも呼ばれる
    public void _Refresh()
    {
        bool en = (localizationManager != null) && localizationManager.isEnglish;

        if (titleText != null) titleText.text = en ? titleEN : titleJP;

        if (bodyText != null)
        {
            string body;
            if (_currentTab == 0) body = en ? tab1EN : tab1JP;
            else if (_currentTab == 1) body = en ? tab2EN : tab2JP;
            else if (_currentTab == 2) body = en ? tab3EN : tab3JP;
            else body = en ? tab4EN : tab4JP;
            bodyText.text = body;
        }

        // 言語トグルラベル: 押下後に切り替わる先の言語を表示(JP 表示時は "EN")
        if (langToggleLabel != null) langToggleLabel.text = en ? "JP" : "EN";

        // Tab ボタンラベル
        if (tabButtonLabels != null)
        {
            if (tabButtonLabels.Length > 0 && tabButtonLabels[0] != null)
                tabButtonLabels[0].text = en ? tab1LabelEN : tab1LabelJP;
            if (tabButtonLabels.Length > 1 && tabButtonLabels[1] != null)
                tabButtonLabels[1].text = en ? tab2LabelEN : tab2LabelJP;
            if (tabButtonLabels.Length > 2 && tabButtonLabels[2] != null)
                tabButtonLabels[2].text = en ? tab3LabelEN : tab3LabelJP;
            if (tabButtonLabels.Length > 3 && tabButtonLabels[3] != null)
                tabButtonLabels[3].text = en ? tab4LabelEN : tab4LabelJP;
        }

        // Active/Inactive 視覚切替
        if (tabButtonRenderers != null)
        {
            for (int i = 0; i < tabButtonRenderers.Length; i++)
            {
                if (tabButtonRenderers[i] == null) continue;
                tabButtonRenderers[i].sharedMaterial =
                    (i == _currentTab) ? tabActiveMaterial : tabInactiveMaterial;
            }
        }

        _RefreshColorPalette();
    }

    // ColorPreferenceManager._SetColor / OnPlayerRestored からも呼ばれる
    public void _RefreshColorPalette()
    {
        if (colorManager == null || colorPaletteRenderers == null) return;
        int sel = colorManager.localColorIndex;

        // パレットボタン自身の色は MaterialPropertyBlock を介さず、シーン側で各 Renderer に
        // sharedMaterial として色マテリアル(or 同じシェーダで _Color 設定済 Material)を割り当てる前提。
        // ここでは「選択中ハイライト」のみ更新する。
        if (colorPaletteSelectionHighlight != null && sel >= 0
            && sel < colorPaletteRenderers.Length
            && colorPaletteRenderers[sel] != null)
        {
            // 選択中のボタン位置にハイライトを移動(World 座標 = 選択ボタンの World 座標)
            colorPaletteSelectionHighlight.transform.position = colorPaletteRenderers[sel].transform.position;
            colorPaletteSelectionHighlight.enabled = true;
        }
        else if (colorPaletteSelectionHighlight != null)
        {
            colorPaletteSelectionHighlight.enabled = false;
        }
    }
}
