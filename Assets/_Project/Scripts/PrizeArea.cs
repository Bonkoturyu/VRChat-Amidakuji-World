using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// PrizeArea: ゴール先の小部屋。テレポート着地点 + 演出 (爆発/紙吹雪) の発火を担う。
// 演出種別の決定は GameManager 側 (ComputeEffectAssignment) に閉じる。
// PlayEffect は SetActive(true) + ParticleSystem.Play() + 個別 SE 再生のみの受動側。
public class PrizeArea : UdonSharpBehaviour
{
    public const int KIND_NONE = 0;
    public const int KIND_EXPLOSION = 1;
    public const int KIND_CONFETTI = 2;

    [Header("Teleport")]
    public Transform teleportTarget;

    [Header("Effect Children (default inactive)")]
    public GameObject explosionEffect;
    public GameObject confettiEffect;

    [Header("Individual SE (3D Spatial)")]
    public AudioSource explosionAudio;
    public AudioSource confettiAudio;

    [Header("Wall Color Override (Phase 6 追加)")]
    [Tooltip("ゴール到達カートの色で染める壁の Renderer 群")]
    public Renderer[] colorTargetWalls;
    [Tooltip("Idle 復帰時に戻すデフォルト色(M_Wall_Generic の元の色に揃える)")]
    public Color defaultWallColor = Color.white;

    // MaterialPropertyBlock を使うと Static Batching と干渉せず色変更可能
    private MaterialPropertyBlock _propBlock;
    private const string COLOR_PROP = "_Color";

    void Start()
    {
        if (explosionEffect != null) explosionEffect.SetActive(false);
        if (confettiEffect != null) confettiEffect.SetActive(false);
        _propBlock = new MaterialPropertyBlock();
    }

    // GameManager から呼ばれる。各クライアントが独立に呼ぶ (seed 由来決定論で発火は同期)。
    // withIndividualSound=false は A モードで共通 SE と二重発音を避けるため。
    public void PlayEffect(int kind, bool withIndividualSound)
    {
        if (kind == KIND_EXPLOSION)
        {
            if (explosionEffect != null)
            {
                explosionEffect.SetActive(true);
                _PlayParticlesIn(explosionEffect);
            }
            if (withIndividualSound && explosionAudio != null) explosionAudio.Play();
        }
        else if (kind == KIND_CONFETTI)
        {
            if (confettiEffect != null)
            {
                confettiEffect.SetActive(true);
                _PlayParticlesIn(confettiEffect);
            }
            if (withIndividualSound && confettiAudio != null) confettiAudio.Play();
        }
        // KIND_NONE は無音・無演出
    }

    public void ResetEffects()
    {
        if (explosionEffect != null)
        {
            _StopParticlesIn(explosionEffect);
            explosionEffect.SetActive(false);
        }
        if (confettiEffect != null)
        {
            _StopParticlesIn(confettiEffect);
            confettiEffect.SetActive(false);
        }
        _ResetWallColor();
    }

    // GameManager から、ゴール到達カートの色で壁を染める
    public void _SetWallColor(Color color)
    {
        if (colorTargetWalls == null || _propBlock == null) return;
        _propBlock.SetColor(COLOR_PROP, color);
        for (int i = 0; i < colorTargetWalls.Length; i++)
        {
            if (colorTargetWalls[i] != null) colorTargetWalls[i].SetPropertyBlock(_propBlock);
        }
    }

    public void _ResetWallColor()
    {
        if (colorTargetWalls == null || _propBlock == null) return;
        _propBlock.SetColor(COLOR_PROP, defaultWallColor);
        for (int i = 0; i < colorTargetWalls.Length; i++)
        {
            if (colorTargetWalls[i] != null) colorTargetWalls[i].SetPropertyBlock(_propBlock);
        }
    }

    // ParticleSystem.Main.Play On Awake は OFF 設計。SetActive(true) だけだと
    // 確実に再生されない環境があるため明示的に Play() を呼ぶ。
    private void _PlayParticlesIn(GameObject root)
    {
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null) systems[i].Play();
        }
    }

    private void _StopParticlesIn(GameObject root)
    {
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null) systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
