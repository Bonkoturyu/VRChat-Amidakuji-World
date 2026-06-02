using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    private GameObject _go;
    private GameManager _gm;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestGameManager");
        _gm = _go.AddComponent<GameManager>();
        var startMethod = typeof(GameManager)
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(startMethod, "Start method not found on GameManager");
        startMethod.Invoke(_gm, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // --- ComputeEffectAssignment ---

    [Test]
    public void ComputeEffectAssignment_N0_ReturnsEmpty()
    {
        int[] result = _gm.ComputeEffectAssignment(0, 0, 1, 1);
        Assert.AreEqual(0, result.Length);
    }

    [Test]
    public void ComputeEffectAssignment_SameSeed_SameResult()
    {
        int[] a = _gm.ComputeEffectAssignment(42, 4, 1, 2);
        int[] b = _gm.ComputeEffectAssignment(42, 4, 1, 2);
        for (int i = 0; i < a.Length; i++)
            Assert.AreEqual(a[i], b[i], $"index {i} が同一 seed で異なる");
    }

    [Test]
    public void ComputeEffectAssignment_ExplosionCount_IsCorrect()
    {
        int[] result = _gm.ComputeEffectAssignment(7, 4, 1, 2);
        int count = 0;
        foreach (int v in result) if (v == PrizeArea.KIND_EXPLOSION) count++;
        Assert.AreEqual(1, count);
    }

    [Test]
    public void ComputeEffectAssignment_ConfettiCount_IsCorrect()
    {
        int[] result = _gm.ComputeEffectAssignment(7, 4, 1, 2);
        int count = 0;
        foreach (int v in result) if (v == PrizeArea.KIND_CONFETTI) count++;
        Assert.AreEqual(2, count);
    }

    [Test]
    public void ComputeEffectAssignment_AllSlotsFilledWhenEplusC_EqualsN()
    {
        int[] result = _gm.ComputeEffectAssignment(99, 4, 2, 2);
        foreach (int v in result)
            Assert.AreNotEqual(0, v, "全スロットが割り当てられるはず");
    }

    [Test]
    public void ComputeEffectAssignment_NoneSlots_WhenEplusC_LessThanN()
    {
        int[] result = _gm.ComputeEffectAssignment(5, 4, 1, 1);
        int noneCount = 0;
        foreach (int v in result) if (v == 0) noneCount++;
        Assert.AreEqual(2, noneCount, "残り 2 スロットは none(0) のはず");
    }

    [Test]
    public void ComputeEffectAssignment_Clamping_E_GreaterThanN()
    {
        // e > n のとき n 個すべてが爆発
        int[] result = _gm.ComputeEffectAssignment(1, 3, 10, 0);
        int count = 0;
        foreach (int v in result) if (v == PrizeArea.KIND_EXPLOSION) count++;
        Assert.AreEqual(3, count, "e > n のとき爆発が n 個になるはず");
    }

    [Test]
    public void ComputeEffectAssignment_Clamping_EplusC_GreaterThanN()
    {
        // e=2, c=5, n=4 → c は (n - e) = 2 にクランプ
        int[] result = _gm.ComputeEffectAssignment(1, 4, 2, 5);
        int eCount = 0, cCount = 0;
        foreach (int v in result)
        {
            if (v == PrizeArea.KIND_EXPLOSION) eCount++;
            if (v == PrizeArea.KIND_CONFETTI) cCount++;
        }
        Assert.AreEqual(2, eCount);
        Assert.AreEqual(2, cCount, "c は (n - e) = 2 にクランプされるはず");
    }
}
