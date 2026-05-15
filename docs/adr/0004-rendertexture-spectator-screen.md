# ADR-0004: 観戦スクリーンをRenderTexture方式で実装

- **Status**: **Superseded by [ADR-0009](./0009-follow-alongside-spectator.md)**
- **Date**: 2026-05-15
- **Superseded**: 2026-05-15

> **このADRは廃止されました。**
> v1.0 で Android 対応([ADR-0010](./0010-android-in-v1.0-scope.md))を含めることが決まり、
> 観戦システムは「追いかけ式」へ変更されました。
> 詳細は [ADR-0009](./0009-follow-alongside-spectator.md) を参照。

---

## (廃止) 当初の Context

非参加者がカート走行を観戦する手段が必要。候補:

1. A. 物理観戦デッキのみ
2. B. RenderTextureスクリーン: Unity Camera → RenderTexture → Quad
3. C. VRC Mirror流用

## (廃止) 当初の Decision

A + B の併用を採用予定だった:

- 物理観戦デッキはガラス床バルコニーとして設置
- エントリーエリアに RenderTexture スクリーンを1枚配置(俯瞰カメラ)

## 廃止の理由

- v1.0 で Android 対応を含めるため、透明度(ガラス床)と RenderTexture が
  モバイルGPUのパフォーマンス要件と衝突
- 観戦UXを「追いかけ式」に変更することで、より能動的な体験+モバイル制約
  クリアが同時に達成された

## 改訂履歴

- 2026-05-15: ADR-0009 により Superseded
