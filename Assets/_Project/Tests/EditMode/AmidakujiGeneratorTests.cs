using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class AmidakujiGeneratorTests
{
    private GameObject _go;
    private AmidakujiGenerator _gen;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestAmidakujiGenerator");
        _gen = _go.AddComponent<AmidakujiGenerator>();

        // EditMode では Start() が自動で呼ばれないためリフレクションで実行する。
        // これにより _bars / _laneX / _segZ が本番と同じコードで初期化される。
        var startMethod = typeof(AmidakujiGenerator)
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(startMethod, "Start method not found on AmidakujiGenerator");
        startMethod.Invoke(_gen, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // --- 基本インバリアント ---

    [Test]
    public void Rebuild_BarsArrayLength_IsLanePairCountTimesSegmentCount()
    {
        _gen.Rebuild(0);
        Assert.AreEqual(_gen.LANE_PAIR_COUNT * _gen.SEGMENT_COUNT, _gen.GetBars().Length);
    }

    [Test]
    public void Rebuild_SameSeed_ProducesSameLayout()
    {
        _gen.Rebuild(42);
        bool[] first = (bool[])_gen.GetBars().Clone();

        _gen.Rebuild(42);
        bool[] second = _gen.GetBars();

        for (int i = 0; i < first.Length; i++)
            Assert.AreEqual(first[i], second[i], $"bars[{i}] が同一 seed で異なる");
    }

    [Test]
    public void Rebuild_DifferentSeeds_ProduceDifferentLayouts()
    {
        _gen.Rebuild(1);
        bool[] a = (bool[])_gen.GetBars().Clone();
        _gen.Rebuild(9999);
        bool[] b = _gen.GetBars();

        bool anyDiff = false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) { anyDiff = true; break; }

        Assert.IsTrue(anyDiff, "seed=1 と seed=9999 で同一レイアウト(確率的にほぼ不可能)");
    }

    // --- あみだくじ構造の不変条件 ---

    [Test]
    public void HasBarForLane_AdjacentLanesPairSymmetrically()
    {
        // lane i が +1 を返すなら lane i+1 は必ず -1 を返す
        for (int seed = 0; seed < 50; seed++)
        {
            _gen.Rebuild(seed);
            for (int seg = 0; seg < _gen.SEGMENT_COUNT; seg++)
            {
                for (int lane = 0; lane < _gen.LANE_COUNT - 1; lane++)
                {
                    if (_gen.HasBarForLane(seg, lane) == 1)
                        Assert.AreEqual(-1, _gen.HasBarForLane(seg, lane + 1),
                            $"seed={seed} seg={seg} lane={lane}: +1 だが隣が -1 でない");
                }
            }
        }
    }

    [Test]
    public void HasBarForLane_NoAdjacentLanePairsActiveInSameSegment()
    {
        // 隣接 lanePair が同時 active になるパターン (1,1,x)/(x,1,1) は禁止
        for (int seed = 0; seed < 100; seed++)
        {
            _gen.Rebuild(seed);
            for (int seg = 0; seg < _gen.SEGMENT_COUNT; seg++)
            {
                for (int pair = 0; pair < _gen.LANE_PAIR_COUNT - 1; pair++)
                {
                    Assert.IsFalse(
                        _gen.HasBar(seg, pair) && _gen.HasBar(seg, pair + 1),
                        $"seed={seed} seg={seg}: pair {pair} と {pair + 1} が同時 active");
                }
            }
        }
    }

    [Test]
    public void HasBar_MatchesGetBarsArray()
    {
        _gen.Rebuild(123);
        bool[] bars = _gen.GetBars();
        for (int pair = 0; pair < _gen.LANE_PAIR_COUNT; pair++)
            for (int seg = 0; seg < _gen.SEGMENT_COUNT; seg++)
                Assert.AreEqual(bars[pair * _gen.SEGMENT_COUNT + seg], _gen.HasBar(seg, pair),
                    $"HasBar({seg},{pair}) と GetBars() が不一致");
    }

    // --- 座標配列の健全性 ---

    [Test]
    public void LaneX_IsUniformlySpaced()
    {
        float[] laneX = _gen.GetLaneXArray();
        Assert.AreEqual(_gen.LANE_COUNT, laneX.Length);
        Assert.GreaterOrEqual(laneX.Length, 2, "laneX must have at least 2 elements to calculate interval");
        float interval = laneX[1] - laneX[0];
        Assert.Greater(interval, 0f);
        for (int i = 2; i < laneX.Length; i++)
            Assert.AreEqual(interval, laneX[i] - laneX[i - 1], 0.001f,
                $"laneX[{i}] の間隔が不均一");
    }

    [Test]
    public void SegZ_IsMonotonicallyDecreasing()
    {
        float[] segZ = _gen.GetSegZArray();
        Assert.AreEqual(_gen.SEGMENT_COUNT, segZ.Length);
        for (int i = 1; i < segZ.Length; i++)
            Assert.Less(segZ[i], segZ[i - 1], $"segZ[{i}] が単調減少でない");
    }
}
