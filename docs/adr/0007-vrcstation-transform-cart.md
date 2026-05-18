# ADR-0007: カート移動を VRC_Station + Transform駆動で実装

- **Status**: Accepted
- **Date**: 2026-05-15
- **Revised**: 2026-05-15 (VRトリガー退出の制約・Station 設定値を明示)
- **Revised**: 2026-05-17 (`disableStationExit` を **false** に変更。Desktop も移動入力(WASD/スティック)で退出可能とし、退出 = リタイアの扱いを VR/Desktop 共通で統一)
- **Revised**: 2026-05-18 (UdonBehaviour 同居構成での `Interact()` / `UseStation()` 実装義務を明文化。Phase 2 アクティブブロッカーの真因に対応)

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
| `Player Mobility` | **Immobilize (For Vehicle)** | カートTransform駆動中、プレイヤー入力で動かない(WASD/スティックでカート位置と競合させない) |
| `Seated` | **true** | 着座IK適用、`pinned-down bug` 回避 |
| `Can Use Station From Station` | false | 他席への席替え不可 |
| `Disable Station Exit` | **false** | Desktop は移動入力(WASD/スティック)、VR はトリガー(Use)で退出可。退出 = リタイアとして両プラットフォーム共通で扱う。なお VR のトリガー退出は VRChat 仕様で本設定の値に関わらず常時有効 |

**設計の意図**: VR コントローラのトリガー(Use)による退出は VRChat 仕様で常に有効であり、`Disable Station Exit = true` でも防げない。当初は Desktop のみ完走を強制する設計だったが、

- Desktop ユーザーから見ると「カートから降りられない」UX に不安感がある(Phase 1 実機確認で判明、2026-05-17)
- VR/Desktop で挙動が一致せず、ルール説明が複雑化する

ため、両プラットフォームで自由退出を許容し、退出 = リタイア扱いに統合する。

### Interaction(Use 表示・着座)経路

VRC_Station と UdonBehaviour が **同じ GameObject に同居** する構成(Cart Root に両方配置)では、**VRC_Station 自前の Use 表示は出ない**。VRChat の interaction 仕様で UdonBehaviour 側の Interactable が優先されるためであり、UdonBehaviour に `Interact()` メソッドが定義されていないと Use テキスト自体が表示されない。

この場合は UdonSharp 側で `Interact()` を実装し、内部で `VRCStation.UseStation(VRCPlayerApi)` を明示的に呼ぶ必要がある。

```csharp
public override void Interact()
{
    if (station == null) return;
    var local = Networking.LocalPlayer;
    if (local == null) return;
    station.UseStation(local);
}
```

実装上の要点:

- UdonBehaviour の `interactText` フィールド(デフォルト `"Use"`)が表示テキストになる
- `proximity` は UdonBehaviour 側の値が使われる(VRC_Station 側の proximity ではない)
- VRC_Station を別 GameObject(例: Seat 子)に分離する代替案もあるが、その場合は `OnStationEntered` / `OnStationExited` を UdonBehaviour で受けるためのフォワード経路を別途用意する必要があるため、本プロジェクトでは **同居 + `Interact()` 実装** を採用する

### 走行中の退出への対応(VR/Desktop 共通)

退出されたら以下の挙動とする:

- そのカートは **空席のまま走行を継続**(seed由来の経路は変更しない)
- ゴール到達してもテレポート発火対象がいないため、何も起こらない
- 退出したプレイヤーはカート Seat 位置に立つ(`stationExitPlayerLocation = Seat Transform`)。走行中カートから降りると物理的に MainFloor 上の進行中位置に下車する
- `participantPlayerIds[laneIndex]` は `-1` に戻す(`OnStationExited` で検出)

= リタイア扱い。ペナルティはなし、再エントリーは Idle 状態に戻るまで不可。

### 退出入力経路の一覧(Phase 2 実装)

| プラットフォーム | 入力 | 実現方法 |
|---|---|---|
| VR | コントローラ Trigger (Use) | VRChat 標準仕様(常時有効、`disableStationExit` の値に無関係) |
| Desktop | 移動入力 (WASD / 左スティック) | VRC_Station の `disableStationExit=false` 設定 |
| Desktop | **Space キー** (ジャンプ) | UdonSharp で `InputJump(bool value, UdonInputEventArgs args)` をフックし、着座中であれば `station.ExitStation(localPlayer)` を呼ぶ |
| VR | ジャンプボタン (A/B 等の Input mapping) | 同上(`InputJump` イベントは VR/Desktop 共通で発火) |

`InputJump` を採用する理由は、VRChat Station デフォルトの Desktop 退出が「移動入力」のみで、初見ユーザーには発見しにくいため。Space キー(ジャンプ)はゲーム標準の「離脱」「キャンセル」相当の直感的入力として追加する。

実装擬似コード(`CartController.cs`):

> **Phase 2 (CartController 単独実装)** では GameManager が未実装のため、下記の `participantPlayerIds[]` 参照の代わりにローカル `_isLocalSeated` フラグで判定する(`if (!value || !_isLocalSeated) return;`)。`participantPlayerIds[]` 参照は GameManager 実装後の **Phase 4** で導入する。

```csharp
public override void InputJump(bool value, UdonInputEventArgs args)
{
    if (!value) return; // 押下のみ反応、離した時は無視
    var local = Networking.LocalPlayer;
    if (local == null) return;
    if (gameManager.participantPlayerIds[laneIndex] != local.playerId) return; // 自分が着座中の Cart のみ
    station.ExitStation(local);
}
```

注意: `InputJump` は地上歩行中も発火するが、`participantPlayerIds[laneIndex] != local.playerId` で短絡するため副作用なし。VRChat 標準のジャンプ動作は別系統で処理されるため、本ハンドラがイベントを受け取っても歩行中のジャンプは阻害しない。

## Consequences

### Positive

- VRChat標準パターンで安定動作実績多数
- Animator不要で軽量
- 経路をデータドリブンに変更可能(ADR-0003 の事前計算Waypointを直接消化)

### Negative

- 走行中の自由退出を許容するため、参加者が途中で降りるとレースの体感は減る(設計トレードオフ。「降りられない閉塞感」よりは許容)
- Desktop ユーザーは着座中に WASD/スティックを倒した時点で退出してしまう(=移動入力をした時点で「リタイアの意思表示」と解釈する設計)。誤操作リスクは中程度

### C案不採用理由

- TeleportTo連打は VR で酔いが酷い、Quest互換性も微妙

### B案不採用理由

- Animatorだとseed依存の動的経路を組みづらい
- ADR-0003で「事前計算+Lerp」を選択した時点でA案が自然な帰結

### 落とし穴の事前回避

- `Immobilize For Vehicle` + `Seated: false` の組み合わせは「退出後にプレイヤーが移動できなくなる pinned-down bug」を引き起こす既知のバグがある → **採用しない**
- Cart Prefab の Station 設定は上記表の通り `Immobilize (For Vehicle)` + `Seated: true` で固定する

### 安全策

- ゴール時のテレポートは: ① Station から `ExitStation()` で降車 → ② プレイヤーに `TeleportTo` を発火、の順序
- `OnStationExited` で「ゴール到達による正常退出」と「ユーザー意思によるリタイア退出」を区別する判定が必要(Phase 4 実装時、`gameState == Running` かつ `cart 進行率 < 1.0` ならリタイア扱い)

## 改訂履歴

- 2026-05-15: `disableStationExit` でVRトリガー退出も防げる想定だったが、VR仕様により不可と判明。リタイア扱いとして許容する設計に変更
- 2026-05-17: Phase 1 実機確認で Desktop ユーザーがカートから降りられない UX が不安感を生むことを確認。`disableStationExit` を **false** に変更し、Desktop も移動入力(WASD/スティック)で退出可能に(VRChat 仕様で Station Exit のトリガーは移動入力。ジャンプキーではない点に注意)。VR/Desktop 共通の「退出 = リタイア」扱いに統合。`Player Mobility` の値も初版の `Mobile` から `Immobilize (For Vehicle)` に修正(Mobile だと着座中も WASD で動けてカート移動と競合する、phase1-prefab-checklist.md §5.1 で既に修正済みの値に整合)
- 2026-05-17 追記: 「移動入力での退出」は初見ユーザーに発見しにくいため、Phase 2 で `CartController.cs` に `InputJump` イベントハンドラを実装し、**Desktop の Space キー / VR のジャンプボタン**でもリタイア退出可能にする方針を追加(§退出入力経路の一覧 参照)
- 2026-05-17 追記2: §「退出入力経路の一覧」の擬似コードは Phase 4 以降前提(GameManager 連携時)。Phase 2 (CartController 単独実装) では `participantPlayerIds[]` 参照を `_isLocalSeated` フラグ判定に置き換える旨を擬似コード直前に明記
- 2026-05-18: Phase 2 Build & Test で Cart に対し Use テキスト自体が表示されない問題が発生。原因は **VRC_Station と UdonBehaviour が同じ GameObject に同居する構成では UdonBehaviour 側の Interactable が優先され、`Interact()` 未実装だと Use 表示が出ない** という VRChat 仕様。`CartController.Interact()` で `station.UseStation(LocalPlayer)` を呼ぶ実装に修正し解消。§「Interaction(Use 表示・着座)経路」を新設し、設計を明文化(Phase 1 時点では Station が Seat 子で UdonBehaviour と別 GameObject だったため発生していなかった。`b8c7103` で Station を Cart Root に移したことで顕在化)
- 2026-05-18 追記: 当初別問題と考えていた **ClientSim 上での Use 発火不能** も、本修正で同時に解消されたことを確認。原因が物理 Collider/Layer ではなく UdonBehaviour API レベル(`Interact()` の有無)であったため、ClientSim と実 VRChat ビルドの双方で同一の解決となった
