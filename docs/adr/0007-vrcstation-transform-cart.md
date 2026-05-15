# ADR-0007: カート移動を VRC_Station + Transform駆動で実装

- **Status**: Accepted
- **Date**: 2026-05-15
- **Revised**: 2026-05-15 (VRトリガー退出の制約・Station 設定値を明示)

## Context

プレイヤーを乗せて動かす実装の候補:

1. **A. VRC_Station + 親Transform駆動**: Stationを Cart GameObject の子にし、Cart の Transform をUdonで動かす
2. **B. VRC_Station + Animator駆動**: Cart の Animator を再生してアニメーション移動
3. **C. プレイヤー側のTeleportTo連打**: 一定間隔でプレイヤー位置を更新

## Decision

**A案 (Station親 + Transform駆動)** を採用する。

### Station 設定値

| プロパティ | 値 | 理由 |
|---|---|---|
| `Player Mobility` | **Mobile** | カートTransformの動きにプレイヤーが追従する |
| `Seated` | **true** | 着座IK適用、`pinned-down bug` 回避 |
| `Can Use Station From Station` | false | 他席への席替え不可 |
| `Disable Station Exit` | true | スティック移動による退出を防止 |

**重要な制約**: `Disable Station Exit = true` でも、**VR ユーザーは VR コントローラのトリガー(Use)で常に Station を退出できる**。これはユーザー安全のためのVRChat仕様で、ワールド側で無効化できない。

### 走行中の VR トリガー退出への対応

退出されたら以下の挙動とする:

- そのカートは **空席のまま走行を継続**(seed由来の経路は変更しない)
- ゴール到達してもテレポート発火対象がいないため、何も起こらない
- 退出したプレイヤーは観戦エリア相当の位置に立つ(通常の Station 退出位置)
- `participantPlayerIds[laneIndex]` は `-1` に戻す(`OnStationExited` で検出)

= リタイア扱い。ペナルティはなし、勝手に再エントリーは Idle 状態に戻るまで不可。

## Consequences

### Positive

- VRChat標準パターンで安定動作実績多数
- Animator不要で軽量
- 経路をデータドリブンに変更可能(ADR-0003 の事前計算Waypointを直接消化)

### Negative

- VR ユーザーの強制着座は不可能(VRトリガー退出は仕様)
- Desktop ユーザーには WASD/スティック移動を試みても退出しない動きが返るが、これは意図通り

### C案不採用理由

- TeleportTo連打は VR で酔いが酷い、Quest互換性も微妙

### B案不採用理由

- Animatorだとseed依存の動的経路を組みづらい
- ADR-0003で「事前計算+Lerp」を選択した時点でA案が自然な帰結

### 落とし穴の事前回避

- `Immobilize For Vehicle` + `Seated: false` の組み合わせは「退出後にプレイヤーが移動できなくなる pinned-down bug」を引き起こす既知のバグがある → **採用しない**
- Cart Prefab の Station 設定は上記表の通り `Mobile` + `Seated: true` で固定する

### 安全策

- gameState が ResultDisplay に遷移したら `Disable Station Exit = false` に戻す(念のため)
- ゴール時のテレポートは: ① Station から `ExitStation()` で降車 → ② プレイヤーに `TeleportTo` を発火、の順序

## 改訂履歴

- 2026-05-15: `disableStationExit` でVRトリガー退出も防げる想定だったが、VR仕様により不可と判明。リタイア扱いとして許容する設計に変更
