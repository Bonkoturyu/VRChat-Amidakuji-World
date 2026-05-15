# ADR-0005: 同期変数を最小限に絞る

- **Status**: Accepted
- **Date**: 2026-05-15

## Context

VRChatのUdon Syncは帯域制限が厳しく、頻繁な同期は接続不安定の原因になる。同期する状態の選定が重要。

## Decision

同期する変数を以下の4種に限定する:

| 変数 | 型 | 説明 |
|---|---|---|
| `seed` | int | あみだくじ生成シード |
| `gameState` | int (enum) | ステート (Idle/Countdown/Running/ResultDisplay) |
| `raceStartTime` | double | レース開始サーバー時刻 |
| `participantPlayerIds` | int[4] | 各座席に座っているPlayerID (-1=空) |

それ以外(カート位置、横線状態、UI状態、ローカルカメラ等)はローカル算出のみ。

## Consequences

### Positive

- 同期トラフィック極小、ネットワーク負荷低い
- Late Joiner: `OnDeserialization` で4変数を受信するだけで全状態を復元できる
- バグ発生時のデバッグが容易(状態が少ない)

### Negative

- 「Master側でだけ起きるロジック」と「全クライアントで決定論的に再現する必要があるロジック」を厳密に分離する必要がある
- アルゴリズム変更時は注意深く全クライアントの整合性を確認する必要がある

### 同期トリガ

- スタート時: gameState/seed/raceStartTime 全部 → `RequestSerialization()`
- 着座/離席時: participantPlayerIds → Ownership 取得 → `RequestSerialization()`
- ゴール完了時: gameState → `RequestSerialization()`

### 採用しないパターン

- カート位置を毎フレーム同期する: 帯域爆発、Time-based 補間で代替可能なため不要
- 横線state配列を同期する: seed1個で再現できるため冗長
- 個別プレイヤーのカート所有権同期: PlayerID配列に統合可能

### v1.1 拡張時の懸念

- 20レーン化で `participantPlayerIds[20]` になると Synced 1パケットあたりの上限を確認する必要あり (80 bytes は問題ないと思われるが、Sync機構の挙動を実機検証)
