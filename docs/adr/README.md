# Architecture Decision Records

このディレクトリには、本プロジェクトの主要なアーキテクチャ判断を記録する。
各ADRはイミュータブルで、判断を覆す場合は新しいADRで Supersede する。

## Index

| ID | Title | Status |
|---|---|---|
| [ADR-0001](./0001-cart-based-design.md) | あみだくじ体験方式に「カート式 (B案)」を採用 | Accepted |
| [ADR-0002](./0002-deterministic-rng-seed-sync.md) | 決定論的乱数 + seed同期による横線生成 | Accepted |
| [ADR-0003](./0003-precomputed-waypoint-lerp.md) | カート移動を事前計算Waypoint + Lerp補間で実装 | Accepted |
| [ADR-0004](./0004-rendertexture-spectator-screen.md) | 観戦スクリーンをRenderTexture方式で実装 | Accepted |
| [ADR-0005](./0005-minimal-sync-variables.md) | 同期変数を最小限に絞る | Accepted |
| [ADR-0006](./0006-instance-owner-start.md) | スタート権限をインスタンスオーナーに限定 | Accepted |
| [ADR-0007](./0007-vrcstation-transform-cart.md) | カート移動を VRC_Station + Transform駆動で実装 | Accepted |
| [ADR-0008](./0008-4lane-scope-scalable-design.md) | v1.0は4レーン固定、内部設計は可変サイズ対応 | Accepted |

## フォーマット

各ADRは以下の構造を踏襲する:

- **Status** (Proposed / Accepted / Superseded by [ADR-XXXX])
- **Date** (YYYY-MM-DD)
- **Context** (なぜこの判断が必要だったか)
- **Decision** (何を決めたか)
- **Consequences** (どうなるか、トレードオフ)
