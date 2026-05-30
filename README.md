# 巨大あみだくじ / Ghost-Leg Express

巨大あみだくじをテーマにした VRChat ワールド (PC + Android/Quest クロスプラットフォーム対応)。プレイヤーはカートに乗ってランダム生成されたあみだくじを自動巡回し、ゴールの賞品エリアにテレポートする。非参加者はあみだくじ構造内を自由に走り回り、カートを追いかけて間近で観戦できる。

> **Status:** v1.0 公開済 (VRChat Community Labs) — PC + Quest 対応
> **Engine:** Unity 2022.3 LTS / VRChat World SDK 3.x / UdonSharp

*A giant Ghost-Leg (Amidakuji) lottery world for VRChat. Ride an auto-traveling cart through a procedurally generated path; at the goal, confetti (win) or an explosion (miss) fires at random. Spectators run freely across the board and chase the carts up close. PC + Quest, JP/EN UI.*

<!-- スクリーンショット/サムネを載せる場合は docs/images/ などに配置してここで参照 -->

## 遊び方 / How to play

- **参加者**: スタート地点のカートに座ると、ランダム生成されたあみだくじを自動で巡回し、ゴールの賞品エリアへテレポート。当たり (紙吹雪) / ハズレ (爆発) がランダムに発火する。
- **観戦者**: あみだくじの上を自由に走り、カートを間近で追いかけられる (ゴール手前バリアの先へ行けるのは参加者のみ)。
- 最大 4 人参加 + 観戦多数 / スタートはインスタンスオーナーのみ / 演出モード (全員ゴール後の一斉発火・個別到達時の即発火) 切替可 / UI 日英切替・カート色選択対応。

## 公開先 / Play it

VRChat の **Community Labs** で公開中。ワールド名 **「巨大あみだくじ / Ghost-Leg Express」** で検索。

<!-- 直リンクを載せる場合は VRChat ワールド URL をここに記載 (URL に blueprintId が含まれる点に留意) -->

## 技術スタック / Tech stack

Unity 2022.3 LTS ・ VRChat World SDK 3.x ・ UdonSharp ・ ClientSim ・ Android (Quest) Build Support

## ドキュメント

| 何を知りたいか | 参照先 |
| --- | --- |
| プロジェクト規約・ナビゲーション | [CLAUDE.md](./CLAUDE.md) |
| 仕様 (何を作るか) | [docs/SPEC.md](./docs/SPEC.md) |
| アーキテクチャ詳細 | [docs/architecture.md](./docs/architecture.md) |
| シーン構造・Prefab | [docs/scene-structure.md](./docs/scene-structure.md) |
| Phase別タスク | [docs/tasklist.md](./docs/tasklist.md) |
| Git運用・CI・テスト・プラットフォーム | [docs/dev-workflow.md](./docs/dev-workflow.md) |
| 設計判断の根拠 (ADR) | [docs/adr/](./docs/adr/) |
| 既知の落とし穴 (UI/音声) | [docs/ui-pitfalls.md](./docs/ui-pitfalls.md) |
| 音源の出所・ライセンス | [docs/audio-assets.md](./docs/audio-assets.md) |
| 課題・進捗・v1.1+ | [BACKLOG.md](./BACKLOG.md) |

## ライセンス / License

- コード・ドキュメント: [LICENSE](./LICENSE) (MIT)
- 音源 (BGM / SE): CC0 — 出所は [docs/audio-assets.md](./docs/audio-assets.md) / [ADR-0013](./docs/adr/0013-audio-assets-and-licensing.md) 参照
