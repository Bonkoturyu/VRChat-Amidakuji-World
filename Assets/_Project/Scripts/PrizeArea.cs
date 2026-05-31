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

    // #4: GetComponentsInChildren は呼ぶたびに ParticleSystem[] を new する。
    // 演出発火・リセットのたびのアロケーション(Quest で GC プレッシャ)を避けるため
    // Start で一度だけ取得してキャッシュし、以降は使い回す。
    private ParticleSystem[] _explosionSystems;
    private ParticleSystem[] _confettiSystems;

    void Start()
    {
        if (explosionEffect != null) explosionEffect.SetActive(false);
        if (confettiEffect != null) confettiEffect.SetActive(false);
        _propBlock = new MaterialPropertyBlock();

        // true = 非アクティブな子も含めて取得(上で SetActive(false) 済のため必須)
        if (explosionEffect != null)
            _explosionSystems = explosionEffect.GetComponentsInChildren<ParticleSystem>(true);
        if (confettiEffect != null)
            _confettiSystems = confettiEffect.GetComponentsInChildren<ParticleSystem>(true);
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
                _PlayParticles(_explosionSystems);
            }
            if (withIndividualSound && explosionAudio != null) explosionAudio.Play();
        }
        else if (kind == KIND_CONFETTI)
        {
            if (confettiEffect != null)
            {
                confettiEffect.SetActive(true);
                _PlayParticles(_confettiSystems);
            }
            if (withIndividualSound && confettiAudio != null) confettiAudio.Play();
        }
        // KIND_NONE は無音・無演出
    }

    public void ResetEffects()
    {
        if (explosionEffect != null)
        {
            _StopParticles(_explosionSystems);
            explosionEffect.SetActive(false);
        }
        if (confettiEffect != null)
        {
            _StopParticles(_confettiSystems);
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
    // #4: 引数は Start でキャッシュした配列(都度 GetComponentsInChildren しない)。
    private void _PlayParticles(ParticleSystem[] systems)
    {
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null) systems[i].Play();
        }
    }

    private void _StopParticles(ParticleSystem[] systems)
    {
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null) systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
