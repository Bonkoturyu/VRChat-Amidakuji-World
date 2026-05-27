using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// RulesPanel の JP/EN 切替ボタン。BoxCollider (IsTrigger=ON) が同じ GameObject に必要。
// Interact で RulesPanelController._ToggleLanguage() を呼び、内部で LocalizationManager 経由で
// 全 subscriber(RulesPanel / ResultDisplay 等)に Refresh が伝播する。
public class LangToggleButton : UdonSharpBehaviour
{
    [Header("References")]
    public RulesPanelController rulesPanel;

    public override void Interact()
    {
        if (rulesPanel == null) return;
        rulesPanel._ToggleLanguage();
    }
}
