# ADR-0010: Android 対応を v1.0 に含める

- **Status**: Accepted
- **Date**: 2026-05-15

## Context

当初の v1.0 は PC専用、Quest対応は v1.2 に送る方針だった(`BACKLOG.md` 旧版)。

しかし以下の条件変化があり、Android対応を v1.0 に組み込む判断:

1. **Quest実機が手元にある**(実機テスト可能、最大のリスク要因が解消)
2. **観戦システムを「追いかけ式」に変更**([ADR-0009](./0009-follow-alongside-spectator.md))により、モバイル最大の負荷源(ガラス床透明度・RenderTexture)が消えた
3. **PC専用にしてもユーザー獲得効果が限定的**(VRChatユーザーの相当数がQuest単機ユーザー)

iOS は v1.0 から外す。理由:

- iOS実機(iPhone/iPad)を所有しておらず、実機テストできない
- Android対応とは別ビルドが必要で、工数が単純に2倍化する
- VRChat の iOS フォールバック機能で「Androidビルドを iOS で読む」挙動があり、最低限の体験は保証される可能性あり

## Decision

- v1.0 で **Windows + Android** を対象プラットフォームとする
- **iOS は v1.1 へ送る**(BACKLOG.md 参照)
- Android のフォールバックで iOS ユーザーが入れる可能性は許容するが、保証はしない

### 実装方針

- Unity プロジェクトのビルドターゲットを **Windows と Android の両方** に対応させる
- 同じ Blueprint ID で PC版・Android版を両方アップロード
- 機能差は一切設けない(PC/Android 完全同一の体験を維持、ADR-0009 を前提とする)
- 開発フェーズの後半(Phase 7)に Android Platform 切替・最適化を集中

### Android 対応のための制約

- マテリアル数: 20 以下を維持(World基準)
- Tri数: 250,000 以下(World基準)
- テクスチャ: 1024×1024 以下を上限
- 透明度を使うマテリアル: ゼロ(ADR-0009により観戦デッキ廃止で達成)
- Realtime Light: ゼロ(全てBaked、PC版も同様)
- Cloth、Mirror、Video Player: 使用しない
- GPU Instancing: 全マテリアルで有効化
- カスタムシェーダー: 使う場合は `Mobile/VRChat/Lightmapped` ベースで作成

## Consequences

### Positive

- VRChatユーザー層をPCVRに限定せず、Quest単機ユーザーまでカバー
- 「巨大あみだくじ」というカジュアルなコンセプトと Quest のカジュアル層の相性が良い
- モバイル最適化を v1.0 で済ませることで、後の機能追加で「モバイル対応が動かない」リスクを最初から排除

### Negative

- 工数増(Android Platform切替・最適化・実機テストで合計 2.5〜3 日追加)
- マテリアル・テクスチャ・Tri数の上限がPC専用時より厳しくなる
- iOS非対応のため、iPhone/iPadのみのユーザーは入れない(またはフォールバック頼み)

### スケジュール影響

新スケジュール(5/15→5/31):

```
5/15  Phase 0    [0.5d]
5/16-17 Phase 1  [2d]
5/18-20 Phase 2  [3d]
5/21-23 Phase 3  [3d]
5/24-25 Phase 4  [2d]
5/26   Phase 5'  [1d]   ゲームフロー UI
5/27   Phase 6'  [1d]   Late Joiner / エッジケース (PC)
5/28   Phase 7'  [1d]   Android Platform切替 + 最適化
5/29   Phase 8'  [1d]   Quest 実機テスト + 調整
5/30   Phase 9   [1d]   ライティング(両プラットフォーム)
5/31   Phase 10        最終テスト + Community Labs 公開
```

旧 Phase 5(RenderTexture)・旧 Phase 6(観戦デッキ)が消えた分の2日を、Android 対応に充当することで全体は変わらず。

### iOS について(v1.1で検討)

- VRChat iOS は実験的に Android ビルドをフォールバックとして読み込む挙動がある
- v1.0 公開後、iOS ユーザーが Android ビルドで遊べているか観察
- 問題なく動くなら iOS 専用ビルドは不要、問題あれば v1.1 で対応

## 改訂履歴

- 2026-05-15: 制定(BACKLOG の v1.2 → v1.0 への移行を正式化)
