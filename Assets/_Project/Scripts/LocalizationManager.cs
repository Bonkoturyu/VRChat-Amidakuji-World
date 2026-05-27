using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// 言語選択は Local 状態(非 UdonSynced)。各プレイヤーが個別に JP/EN を選べる。
// 表示言語の違いは「同じ Synced データの見せ方の違い」だけで整合性に影響しない。
public class LocalizationManager : UdonSharpBehaviour
{
    [Header("Subscribers")]
    [Tooltip("言語切替時に _Refresh() を呼ぶ UI 群")]
    public RulesPanelController rulesPanel;
    public ResultDisplayUI resultDisplay;

    // Local 状態。public だが UdonSynced 属性を付けないことで非同期化。
    [HideInInspector] public bool isEnglish;

    void Start()
    {
        isEnglish = false;
    }

    // RulesPanel の JP|EN トグルボタンから呼ばれる
    public void _ToggleLanguage()
    {
        isEnglish = !isEnglish;
        _BroadcastRefresh();
    }

    private void _BroadcastRefresh()
    {
        if (rulesPanel != null) rulesPanel._Refresh();
        if (resultDisplay != null) resultDisplay._RefreshLanguage();
    }
}
