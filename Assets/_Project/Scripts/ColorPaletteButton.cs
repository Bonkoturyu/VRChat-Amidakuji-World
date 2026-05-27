
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// RulesPanel Tab4 のカラーパレットボタン。Use 表示のため同 GameObject に BoxCollider (IsTrigger=ON) が必要。
// colorIndex は 0..N-1(ColorPreferenceManager.paletteColors 配列のインデックス)。
public class ColorPaletteButton : UdonSharpBehaviour
{
    [Header("References")]
    public ColorPreferenceManager colorManager;

    [Tooltip("0..N-1、ColorPreferenceManager.paletteColors のインデックス")]
    public int colorIndex;

    public override void Interact()
    {
        if (colorManager == null) return;
        colorManager._SetColor(colorIndex);
    }
}
