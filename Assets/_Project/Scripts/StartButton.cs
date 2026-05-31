using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

// Phase 5 実装: 参加者数チェック・gameState 連動・Material 切替による視覚フィードバックを追加。
public class StartButton : UdonSharpBehaviour
{
    public GameManager gameManager;

    [Header("Visual State (Material 切替)")]
    [Tooltip("Material を差し替える対象 Renderer")]
    public Renderer buttonRenderer;
    [Tooltip("押下可能時の Material")]
    public Material materialEnabled;
    [Tooltip("押下不可時の Material(走行中・参加者0)")]
    public Material materialDisabled;

    [Header("Label (任意)")]
    [Tooltip("ボタン表面の状態表示テキスト(3D TextMeshPro、未設定可)")]
    public TextMeshPro labelText;
    [Tooltip("押下可能時の表示")]
    public string labelEnabled = "START";
    [Tooltip("押下不可時の表示(参加者0・走行中など)")]
    public string labelDisabled = "START";

    // Material 差し替えを状態変化時のみ実行するためのキャッシュ
    private bool _lastPressable = false;
    private bool _hasAppliedOnce = false;

    // 初回 Material 反映(_lastPressable は false 初期化なので必ず disabled が当たる)
    void Start()
    {
        _ApplyVisual(false);
        _hasAppliedOnce = true;
    }

    // 毎フレーム押下可否を評価し、状態変化時のみ Material を差し替える
    void Update()
    {
        bool now = _IsPressable();
        if (now != _lastPressable || !_hasAppliedOnce)
        {
            _ApplyVisual(now);
            _lastPressable = now;
            _hasAppliedOnce = true;
        }
    }

    // Master かつ押下可能なときのみ RequestStart を委譲する
    public override void Interact()
    {
        if (!Networking.IsMaster) return;
        if (gameManager == null) return;
        if (!_IsPressable()) return;
        gameManager.RequestStart();
    }

    // 押下可否判定: STATE_IDLE かつ参加者が 1 人以上のときのみ true
    // (#7/#16: レース開始は IDLE 限定に統一。RESULT_DISPLAY からの直接開始は採らない)
    private bool _IsPressable()
    {
        if (gameManager == null) return false;
        if (gameManager.gameState != GameManager.STATE_IDLE) return false;
        if (gameManager.participantPlayerIds == null) return false;
        int count = 0;
        for (int i = 0; i < gameManager.participantPlayerIds.Length; i++)
        {
            if (gameManager.participantPlayerIds[i] != -1) count++;
        }
        return count >= 1;
    }

    // buttonRenderer の sharedMaterial を切り替える(material プロパティはインスタンス生成リークのため使用禁止)
    private void _ApplyVisual(bool pressable)
    {
        if (buttonRenderer != null)
        {
            var mat = pressable ? materialEnabled : materialDisabled;
            if (mat != null) buttonRenderer.sharedMaterial = mat;
        }
        if (labelText != null) labelText.text = pressable ? labelEnabled : labelDisabled;
    }
}
