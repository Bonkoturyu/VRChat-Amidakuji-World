
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

// 各プレイヤーの好みカラー(0..N-1)を Player Persistence で永続化する Local 状態マネージャ。
// 着座中の Cart に colorIndex を伝播し、Cart Visual と賞品エリア壁色の両方をその色に統一する。
// paletteColors[] は全クライアントで同一(Inspector 設定)なので、index だけ同期すれば色が一致する。
public class ColorPreferenceManager : UdonSharpBehaviour
{
    [Header("Palette (Inspector で MD500 系 8 色を設定)")]
    public Color[] paletteColors;

    [Header("References")]
    [Tooltip("色変更時に着座中の Cart に伝播する対象")]
    public CartController[] carts;
    [Tooltip("色変更時にパレットボタン視覚を更新するため")]
    public RulesPanelController rulesPanel;

    // Persistence Key(ADR-0012 §7 命名規則に合わせる)
    private const string KEY_COLOR_INDEX = "amidakuji.colorIndex";

    // Local 状態(非 Synced、各プレイヤー個別)。-1 = 未復元
    [HideInInspector] public int localColorIndex;

    void Start()
    {
        localColorIndex = -1;
    }

    // ColorPaletteButton.Interact() から呼ばれる
    public void _SetColor(int index)
    {
        if (paletteColors == null) return;
        if (index < 0 || index >= paletteColors.Length) return;
        if (localColorIndex == index) return;

        localColorIndex = index;
        PlayerData.SetInt(KEY_COLOR_INDEX, index);

        _PropagateToSeatedCart();

        if (rulesPanel != null)
        {
            rulesPanel._RefreshColorPalette();
        }
    }

    public Color GetPaletteColor(int index)
    {
        if (paletteColors == null) return Color.white;
        if (index < 0 || index >= paletteColors.Length) return Color.white;
        return paletteColors[index];
    }

    // 自分の Persistence 復元完了時。他者の Restored は無視。
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player == null || !player.isLocal) return;

        var local = Networking.LocalPlayer;
        if (local == null) return;

        int restored;
        bool hasSaved = PlayerData.TryGetInt(local, KEY_COLOR_INDEX, out restored);

        if (hasSaved && paletteColors != null && restored >= 0 && restored < paletteColors.Length)
        {
            localColorIndex = restored;
        }
        else if (paletteColors != null && paletteColors.Length > 0)
        {
            // 初回入場: playerId ベースの決定論既定色を割り当て、Persistence にも書込
            int pid = local.playerId;
            if (pid < 0) pid = -pid;
            localColorIndex = pid % paletteColors.Length;
            PlayerData.SetInt(KEY_COLOR_INDEX, localColorIndex);
        }

        // 復元前に着座済の場合に備えて Cart にも反映
        _PropagateToSeatedCart();

        if (rulesPanel != null)
        {
            rulesPanel._RefreshColorPalette();
        }
    }

    private void _PropagateToSeatedCart()
    {
        var local = Networking.LocalPlayer;
        if (local == null || carts == null) return;
        int myPid = local.playerId;

        for (int i = 0; i < carts.Length; i++)
        {
            if (carts[i] == null) continue;
            if (carts[i].seatedPlayerId != myPid) continue;

            if (!Networking.IsOwner(carts[i].gameObject))
            {
                Networking.SetOwner(local, carts[i].gameObject);
            }
            carts[i].colorIndex = localColorIndex;
            carts[i].RequestSerialization();
            carts[i]._RefreshVisualColor();
            break;
        }
    }
}
