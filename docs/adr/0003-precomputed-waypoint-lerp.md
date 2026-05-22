# ADR-0003: カート移動を事前計算Waypoint + Lerp補間で実装

- **Status**: Accepted
- **Date**: 2026-05-15
- **Revised**: 2026-05-15 (時刻計算API選定を明示)

## Context

カートの移動制御方式の候補:

1. **A. リアルタイム判定**: カートが交差点に到達するたびに横線の有無を判定して進路決定
2. **B. 事前経路計算**: スタート時に全カートの経路 (Waypoint配列) を確定し、Waypoint間をLerp移動
3. **C. Animator駆動**: 経路アニメーションを Animator で再生

## Decision

**B案 (事前計算 + Lerp補間)** を採用する。

実装方針:

- スタート時、各CartControllerが seed と自分のレーン番号から `Vector3[] waypoints` を計算
- Update() 内で **`Networking.CalculateServerDeltaTime(currentServerTime, raceStartTime)`** から現在のWaypoint区間とその区間内のt値を算出
- `transform.position = Vector3.Lerp(waypoints[i], waypoints[i+1], t)`

### 時刻計算APIの注意

`Networking.GetServerTimeInSeconds()` には既知の挙動として、**約半数のクライアントで負の値が返る** ケースが存在する(VRChat内部のサーバー時刻オフセット仕様によるもので、ワールド滞在中は符号固定)。

このため、**生の値どうしを直接引き算してはならない**。代わりに VRChat が用意している以下のAPIを使う:

- `Networking.CalculateServerDeltaTime(double endTime, double startTime)`: 2つのサーバータイムスタンプの差を安全に計算

レース経過時間の取得は以下のパターン:

```csharp
double nowServer  = Networking.GetServerTimeInSeconds();
double elapsed    = Networking.CalculateServerDeltaTime(nowServer, raceStartTime);
// elapsed を使って Waypoint 間の Lerp 計算
```

`raceStartTime` は `UdonSynced` で全クライアントに配布される `GetServerTimeInSeconds()` の値。

## Consequences

### Positive

- ローカル計算のみ。同期不要
- Late Joiner: 経過時間から即座に現在位置算出可能
- 経路ロジックがテストしやすい(seed入れたら配列が決まる純関数)
- `CalculateServerDeltaTime` 採用により、サーバー時刻負値問題を回避

### Negative

- レース途中で経路を動的変更する演出が入れにくい
- Time差(クライアント間のサーバー時刻取得誤差)で数十ms単位の位置ズレは発生する。ただし観戦・体験上は無視できる範囲

### Animator不採用理由

- Animator駆動だとseed依存の動的経路にしにくい
- Udonからのフレーム精度制御が難しい
- ADR-0002で seed ベースの動的経路を選んだ時点で、Animator方式は実装複雑性が増すだけ

### Waypoint構築アルゴリズム概要

```text
ComputePath(seed, startLane):
  currentLane = startLane
  waypoints = []
  for seg in 0..SEGMENT_COUNT-1:
    y_top    = TOP_Y - seg * SEG_LENGTH
    y_bottom = TOP_Y - (seg+1) * SEG_LENGTH
    
    # 縦線を下に降りる
    waypoints.Add(LanePosition(currentLane, y_top))
    
    # この高さの横線をチェック
    bar = AmidakujiGenerator.HasBarAt(seg, currentLane)
    if bar != NONE:
      # 横線を渡る
      neighborLane = (bar == LEFT) ? currentLane - 1 : currentLane + 1
      waypoints.Add(LanePosition(currentLane, y_bottom))
      waypoints.Add(LanePosition(neighborLane, y_bottom))
      currentLane = neighborLane
  
  # ゴール位置
  waypoints.Add(LanePosition(currentLane, BOTTOM_Y))
  return waypoints
```

実コードは Phase 3 で実装。

## 改訂履歴

- 2026-05-15: 生の `GetServerTimeInSeconds()` を引き算する想定だったが、`CalculateServerDeltaTime` を使う方針に変更(サーバー時刻負値問題の回避)
