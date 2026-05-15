# ADR-0006: スタート権限をインスタンスオーナー (Master) に限定

- **Status**: Accepted
- **Date**: 2026-05-15

## Context

ゲーム開始トリガーを誰が引けるかの権限モデル。候補:

1. **A. 全プレイヤー操作可**: 誰でもスタート押下可
2. **B. 参加者のみ**: 着座しているプレイヤーのみ
3. **C. インスタンスオーナーのみ**: `Networking.IsMaster` のみ
4. **D. 投票制**: 過半数で開始

## Decision

**C案 (インスタンスオーナーのみ)** を採用する。

## Consequences

### Positive

- 実装が単純 (`Networking.IsMaster` 判定のみ)
- 「友達同士で遊ぶ前提のインスタンス」と相性が良い(ホストが進行管理)
- 荒らし耐性(知らない人が勝手に始められない)

### Negative

- Master が不在/AFKだとゲーム進行不可
- パブリックインスタンスで Master が ROM な場合、参加者が遊べない

### 軽減策

- ルール説明パネルに「Instance Master only」と明記
- v1.1で「参加者全員着座 + 一定時間経過で自動スタート」フォールバック検討

### Master交代の挙動

- VRChat仕様により Master 退出時は別クライアントが自動昇格
- 走行中の Master 交代は、Synced変数のオーナーシップが移動するが gameState は維持される
- 新Masterはスタートボタンを次の Idle 状態から操作可能になる

### スタートボタンの UI 挙動

- 非Master側: グレーアウト + ツールチップ「Instance Master only」
- Master側 + 参加者0人: グレーアウト + ツールチップ「No participants」
- Master側 + 参加者>=1: アクティブ
- gameState != Idle: 全クライアント無効
