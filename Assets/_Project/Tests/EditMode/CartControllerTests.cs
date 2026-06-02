using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CartControllerTests
{
    private GameObject _genGo;
    private GameObject _cartGo;
    private AmidakujiGenerator _gen;
    private CartController _cart;

    [SetUp]
    public void SetUp()
    {
        _genGo = new GameObject("TestGenerator");
        _gen = _genGo.AddComponent<AmidakujiGenerator>();
        typeof(AmidakujiGenerator)
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_gen, null);

        _cartGo = new GameObject("TestCart");
        _cart = _cartGo.AddComponent<CartController>();
        _cart.generator = _gen;
        _cart.laneIndex = 0;
        typeof(CartController)
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_cart, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_cartGo);
        Object.DestroyImmediate(_genGo);
    }

    // HasBarForLane を手動で辿って期待 GoalLane を返す
    private int TraverseAmidakuji(int seed, int startLane)
    {
        _gen.Rebuild(seed);
        int lane = startLane;
        for (int seg = 0; seg < _gen.SEGMENT_COUNT; seg++)
            lane += _gen.HasBarForLane(seg, lane);
        return lane;
    }

    // --- ComputePath ---

    [Test]
    public void ComputePath_GoalLaneIndex_IsInValidRange()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            _gen.Rebuild(seed);
            for (int startLane = 0; startLane < _gen.LANE_COUNT; startLane++)
            {
                _cart.ComputePath(seed, startLane);
                Assert.GreaterOrEqual(_cart.GoalLaneIndex, 0,
                    $"seed={seed} startLane={startLane}: GoalLaneIndex < 0");
                Assert.Less(_cart.GoalLaneIndex, _gen.LANE_COUNT,
                    $"seed={seed} startLane={startLane}: GoalLaneIndex >= LANE_COUNT");
            }
        }
    }

    [Test]
    public void ComputePath_GoalLaneIndex_MatchesManualTraversal()
    {
        // ComputePath の経路追跡が HasBarForLane の手動トラバースと一致することを確認
        for (int seed = 0; seed < 30; seed++)
        {
            for (int startLane = 0; startLane < _gen.LANE_COUNT; startLane++)
            {
                int expected = TraverseAmidakuji(seed, startLane);
                _gen.Rebuild(seed);
                _cart.ComputePath(seed, startLane);
                Assert.AreEqual(expected, _cart.GoalLaneIndex,
                    $"seed={seed} startLane={startLane}: GoalLane が手動トラバースと不一致");
            }
        }
    }

    [Test]
    public void ComputePath_Determinism_SameSeedSameLane_SameGoal()
    {
        _gen.Rebuild(42);
        _cart.ComputePath(42, 1);
        int first = _cart.GoalLaneIndex;

        _gen.Rebuild(42);
        _cart.ComputePath(42, 1);
        Assert.AreEqual(first, _cart.GoalLaneIndex, "同一 seed+startLane で GoalLaneIndex が異なる");
    }
}
