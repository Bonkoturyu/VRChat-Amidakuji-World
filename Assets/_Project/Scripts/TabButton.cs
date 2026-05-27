using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// RulesPanel の Tab 切替ボタン。GameObject 1 個につき Tab Index 0/1/2 を指定。
// Use 表示のために BoxCollider (IsTrigger=ON) が同じ GameObject に必要(Phase 3 StartButton と同流儀)。
public class TabButton : UdonSharpBehaviour
{
    [Header("References")]
    public RulesPanelController rulesPanel;

    [Tooltip("0=Join, 1=Watch, 2=Mode")]
    public int tabIndex;

    public override void Interact()
    {
        if (rulesPanel == null) return;
        if (tabIndex == 0) rulesPanel._SelectTab1();
        else if (tabIndex == 1) rulesPanel._SelectTab2();
        else if (tabIndex == 2) rulesPanel._SelectTab3();
        else if (tabIndex == 3) rulesPanel._SelectTab4();
    }
}
