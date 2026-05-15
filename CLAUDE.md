# Amidakuji World - VRChat World Project

巨大あみだくじをテーマにしたVRChatワールド。プレイヤーはカートに乗ってランダム生成されたあみだくじを自動巡回し、ゴールの賞品エリアにテレポートする。観戦者は俯瞰スクリーンと観戦デッキから走行を見守る。

## 現在のステータス

- **目標**: 2026-05-31 までに Community Labs 公開
- **バージョン**: v1.0 (MVP) 開発中
- **担当**: 個人開発

詳細仕様は [docs/SPEC.md](./docs/SPEC.md)、進捗は [BACKLOG.md](./BACKLOG.md) / [docs/tasklist.md](./docs/tasklist.md) を参照。

## 技術スタック

- Unity 2022.3 LTS (VCC指定バージョン)
- VRChat Creator Companion (VCC) 管理プロジェクト
- VRChat World SDK 3.x
- UdonSharp (U#)
- ClientSim (ローカルテスト用)
- Visual Studio 2022 / Rider (任意)

## ディレクトリ構造

```
amidakuji-world/
├── CLAUDE.md            ← このファイル(プロジェクト規約・ナビゲーション)
├── BACKLOG.md           ← 課題・進捗・v1.1+ アイデア
├── LICENSE              ← MIT License
├── .gitignore           ← Unity/VRChat向け除外
├── docs/
│   ├── SPEC.md             ← 仕様(ロック済み)
│   ├── architecture.md     ← アーキ詳細・実装パターン
│   ├── scene-structure.md  ← Unity Hierarchy・Prefab分割案
│   ├── tasklist.md         ← Phase別タスク
│   ├── dev-workflow.md     ← Git運用・CI・テスト方針
│   └── adr/                ← Architecture Decision Records
└── Assets/              ← Unity Project (VCCが管理)
    └── _Project/
        ├── Scenes/
        ├── Prefabs/
        ├── Scripts/     ← UdonSharp スクリプト
        ├── Materials/
        └── Textures/
```

ドキュメント間の参照関係:

- 「**何を作るか**」→ `docs/SPEC.md`
- 「**どう組み立てるか**」→ `docs/architecture.md`, `docs/scene-structure.md`
- 「**いつ何をするか**」→ `docs/tasklist.md`
- 「**なぜそう決めたか**」→ `docs/adr/`
- 「**何が残っているか**」→ `BACKLOG.md`
- 「**どう運用するか(Git/CI)**」→ `docs/dev-workflow.md`

## 開発フロー

1. VCC で `amidakuji-world` プロジェクトを開く
2. ClientSim でローカル単体テスト
3. SDK の `Build & Test` で多人数同期テスト(ローカル複数クライアント)
4. SDK の `Build & Publish` で Private アップロード
5. メインアカウントで Join 動作確認
6. v1.0完成時点で問題なければ Community Labs 公開ボタンを押下

毎日 Phase 終了時に Build & Test まで通すこと。「明日まとめてテスト」はNG。Git運用詳細は `docs/dev-workflow.md` 参照。

## パフォーマンスバジェット (PC Good ランク目標)

| 指標 | 上限 |
|---|---|
| Triangle Count | 70,000 |
| Material Count | 20 |
| Draw Call | 200 |
| Skinned Mesh Renderer | 8 |
| Audio Source | 8 |
| Realtime Light | 0(全て Baked or Mixed) |

ライティング戦略: Mixed Lighting + Light Probe + Reflection Probe (Static)。

RenderTextureスクリーンはDrawCall・GPU負荷増加要因なので、観戦カメラのCulling Maskで不要レイヤーを除外、解像度 1280×720 に抑える(詳細 [ADR-0004](./docs/adr/0004-rendertexture-spectator-screen.md))。

実機目標: VR HMD でスポーン地点 **45 FPS 以上**。

## Udon# 制約のリマインダ

- `async/await` / `IEnumerator` 不可 → 遅延処理は `SendCustomEventDelayedSeconds()`
- ジェネリック (`List<T>` 等) 不可 → 固定長配列で対応
- `OnDeserialization` のタイミングに注意(Late Joiner対応の要)
- `Networking.LocalPlayer` と引数 `VRCPlayerApi` の比較でローカル/リモート分岐
- 大量の動的生成は避け、シーン配置 + enable切り替えで対応
- **時刻差の計算は `Networking.CalculateServerDeltaTime()` を使う**(`GetServerTimeInSeconds()` を直接引き算しない、[ADR-0003](./docs/adr/0003-precomputed-waypoint-lerp.md))
- **VR ユーザーは `disableStationExit = true` でもトリガーで Station 退出可能**(リタイア扱いで設計、[ADR-0007](./docs/adr/0007-vrcstation-transform-cart.md))
- `System.Random` は SDK 3.7.1 以降で利用可能(自前PRNG実装不要、[ADR-0002](./docs/adr/0002-deterministic-rng-seed-sync.md))

## 関連リソース

- VRChat Creators Hub: https://creators.vrchat.com/
- UdonSharp Documentation: https://udonsharp.docs.vrchat.com/
- VRChat Community Labs: https://docs.vrchat.com/docs/vrchat-community-labs

## 用語

- **インスタンス**: VRChatワールドの実行単位。同時にいるプレイヤーで共有される空間
- **インスタンスオーナー / Master**: インスタンス内で `Networking.IsMaster` が true のクライアント
- **Late Joiner**: 走行中などに後から参加してきたプレイヤー
- **Trust Rank**: VRChatのユーザー信頼度。Community Labs 公開には User 以上が必要
- **VCC**: VRChat Creator Companion(プロジェクト・SDK管理ツール)
