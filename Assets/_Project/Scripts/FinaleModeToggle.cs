
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

// A モード ⇔ B モード切替トグル(ADR-0012 §7、Phase 5 実装)。
// Master かつ Idle のときのみ反応。Material 3 個切替(A/B/Disabled)で状態可視化。
public class FinaleModeToggle : UdonSharpBehaviour
{
    [Header("References")]
    public FinaleModeManager finaleModeManager;
    public GameManager gameManager;

    [Header("Visual State (Material 切替)")]
    [Tooltip("Material を差し替える対象 Renderer")]
    public Renderer buttonRenderer;
    [Tooltip("押下可能 + 現在 A モード時")]
    public Material materialModeA;
    [Tooltip("押下可能 + 現在 B モード時")]
    public Material materialModeB;
    [Tooltip("押下不可時(非 Master or 走行中)")]
    public Material materialDisabled;

    [Header("Label (任意)")]
    [Tooltip("ボタン表面のモード表示テキスト(3D TextMeshPro、未設定可)")]
    public TextMeshPro labelText;
    [Tooltip("A モード(一斉)時の表示")]
    public string labelModeA = "MODE A\n一斉";
    [Tooltip("B モード(個別)時の表示")]
    public string labelModeB = "MODE B\n個別";

    // 直前の表示モード(0=Disabled / 1=A / 2=B)。状態変化時のみ Material 差し替え
    private int _lastShownMode = -1;
    // 直前にラベルへ表示したモード(1=A / 2=B)。押下可否と独立に現在モードを表示
    private int _lastLabelMode = -1;

    void Start()
    {
        _UpdateVisual();
    }

    void Update()
    {
        _UpdateVisual();
    }

    public override void Interact()
    {
        if (!_IsPressable()) return;
        if (finaleModeManager == null) return;
        finaleModeManager._ToggleFinaleMode();
    }

    // 押下可否判定: Idle かつ Master のときのみ true
    private bool _IsPressable()
    {
        if (gameManager == null) return false;
        if (gameManager.gameState != GameManager.STATE_IDLE) return false;
        if (!Networking.IsMaster) return false;
        return true;
    }

    private void _UpdateVisual()
    {
        // --- Material(押下可否を反映: Disabled / A / B)---
        if (buttonRenderer != null)
        {
            int mode;
            if (!_IsPressable())
            {
                mode = 0;
            }
            else if (gameManager.simultaneousFinale)
            {
                mode = 1;
            }
            else
            {
                mode = 2;
            }

            if (mode != _lastShownMode)
            {
                Material mat;
                if (mode == 0) mat = materialDisabled;
                else if (mode == 1) mat = materialModeA;
                else mat = materialModeB;

                if (mat != null) buttonRenderer.sharedMaterial = mat;
                _lastShownMode = mode;
            }
        }

        // --- Label(押下可否に関係なく現在モードを常に表示。非 Master でも視認可)---
        if (labelText != null && gameManager != null)
        {
            int labelMode = gameManager.simultaneousFinale ? 1 : 2;
            if (labelMode != _lastLabelMode)
            {
                labelText.text = labelMode == 1 ? labelModeA : labelModeB;
                _lastLabelMode = labelMode;
            }
        }
    }
}
