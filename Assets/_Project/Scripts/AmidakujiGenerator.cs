using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AmidakujiGenerator : UdonSharpBehaviour
{
    [Header("Constants (Inspector exposed for transparency, do not change at runtime)")]
    public int LANE_COUNT = 4;
    public int LANE_PAIR_COUNT = 3;
    public int SEGMENT_COUNT = 11;
    public float TOP_Y = 2f;
    public float BOTTOM_Y = -58.5f;
    public float SEG_LENGTH = 5f;

    [Header("References")]
    [Tooltip("Bar GameObject を lanePair*SEGMENT_COUNT + seg の順で配置(33 個)")]
    public GameObject[] horizontalBars;

    private bool[] _bars;
    private float[] _laneX;
    private float[] _segZ;
    private bool _initialized;

    void Start()
    {
        int total = LANE_PAIR_COUNT * SEGMENT_COUNT;
        _bars = new bool[total];

        _laneX = new float[LANE_COUNT];
        _laneX[0] = -6f;
        _laneX[1] = -2f;
        _laneX[2] = 2f;
        _laneX[3] = 6f;

        _segZ = new float[SEGMENT_COUNT];
        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            _segZ[i] = -3f - i * SEG_LENGTH;
        }

        if (horizontalBars != null)
        {
            for (int i = 0; i < horizontalBars.Length; i++)
            {
                if (horizontalBars[i] != null) horizontalBars[i].SetActive(false);
            }
        }
        _initialized = false;
    }

    // ADR-0002: 重み付き 5 パターン抽選で連続禁止を内包
    // weights: (0,0,0)=2 / (1,0,0)=2 / (0,1,0)=3 / (0,0,1)=2 / (1,0,1)=1, sum=10
    public void Rebuild(int seed)
    {
        var rng = new System.Random(seed);
        int activeCount = 0;
        for (int seg = 0; seg < SEGMENT_COUNT; seg++)
        {
            int r = rng.Next(0, 10);
            bool p0, p1, p2;
            if (r < 2)       { p0 = false; p1 = false; p2 = false; }
            else if (r < 4)  { p0 = true;  p1 = false; p2 = false; }
            else if (r < 7)  { p0 = false; p1 = true;  p2 = false; }
            else if (r < 9)  { p0 = false; p1 = false; p2 = true;  }
            else             { p0 = true;  p1 = false; p2 = true;  }

            _bars[0 * SEGMENT_COUNT + seg] = p0;
            _bars[1 * SEGMENT_COUNT + seg] = p1;
            _bars[2 * SEGMENT_COUNT + seg] = p2;
            if (p0) activeCount++;
            if (p1) activeCount++;
            if (p2) activeCount++;

            ApplyActive(0, seg, p0);
            ApplyActive(1, seg, p1);
            ApplyActive(2, seg, p2);
        }
        _initialized = true;
        Debug.Log("[AmidakujiGenerator] Rebuild seed=" + seed + " bars=" + activeCount + "/" + (LANE_PAIR_COUNT * SEGMENT_COUNT));
    }

    private void ApplyActive(int lanePair, int seg, bool active)
    {
        int idx = lanePair * SEGMENT_COUNT + seg;
        if (horizontalBars == null || idx < 0 || idx >= horizontalBars.Length) return;
        if (horizontalBars[idx] == null) return;
        horizontalBars[idx].SetActive(active);
    }

    public bool HasBar(int seg, int lanePair)
    {
        if (!_initialized) return false;
        if (seg < 0 || seg >= SEGMENT_COUNT) return false;
        if (lanePair < 0 || lanePair >= LANE_PAIR_COUNT) return false;
        return _bars[lanePair * SEGMENT_COUNT + seg];
    }

    // Lane に対する横線方向 (-1: 左隣へ, 0: 直進, +1: 右隣へ)
    public int HasBarForLane(int seg, int lane)
    {
        if (!_initialized) return 0;
        bool left = (lane > 0) && HasBar(seg, lane - 1);
        bool right = (lane < LANE_COUNT - 1) && HasBar(seg, lane);
        if (left) return -1;
        if (right) return 1;
        return 0;
    }

    public float LaneX(int lane)
    {
        if (lane < 0 || lane >= LANE_COUNT) return 0f;
        return _laneX[lane];
    }

    public float SegZ(int seg)
    {
        if (seg < 0 || seg >= SEGMENT_COUNT) return 0f;
        return _segZ[seg];
    }
}
