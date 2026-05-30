# Amidakuji World - VRChat World Project

巨大あみだくじをテーマにしたVRChatワールド (PC + Android クロスプラットフォーム対応)。プレイヤーはカートに乗ってランダム生成されたあみだくじを自動巡回し、ゴールの賞品エリアにテレポートする。**ワールドは平面水平レイアウト**(地面にあみだくじが描かれた状態、段差なし、[ADR-0011](./docs/adr/0011-flat-horizontal-layout.md))。非参加者は同じ床面を自由に走り回り、カートを追いかけて間近で観戦できる。

## 現在のステータス

- **目標**: 2026-05-31 までに Community Labs 公開
- **バージョン**: v1.0 (MVP) 開発中
- **対応プラットフォーム**: Windows (PC) + Android (Quest 2/3/3S)
- **担当**: 個人開発

詳細仕様は [docs/SPEC.md](./docs/SPEC.md)、進捗は [BACKLOG.md](./BACKLOG.md) / [docs/tasklist.md](./docs/tasklist.md) を参照。

## 技術スタック

- Unity 2022.3 LTS (VCC指定バージョン)
- VRChat Creator Companion (VCC) 管理プロジェクト
- VRChat World SDK 3.x
- UdonSharp (U#)
- ClientSim (ローカルテスト用)
- Android Build Support (Quest対応のため)
- Visual Studio 2022 / Rider (任意)

## ディレクトリ構造

```text
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
        ├── Textures/
        └── Audio/       ← BGM/ (Mutant Club) + SE/ (balloon-pop, bomb-sound) — 全て CC0
```

ドキュメント間の参照関係:

- 「**何を作るか**」→ `docs/SPEC.md`
- 「**どう組み立てるか**」→ `docs/architecture.md`, `docs/scene-structure.md`
- 「**いつ何をするか**」→ `docs/tasklist.md`
- 「**なぜそう決めたか**」→ `docs/adr/`
- 「**何が残っているか**」→ `BACKLOG.md`
- 「**どう運用するか(Git/CI/プラットフォーム)**」→ `docs/dev-workflow.md`
- 「**どこで踏むか(既知の落とし穴)**」→ `docs/ui-pitfalls.md`
- 「**どの音源を使うか(出所・ライセンス)**」→ `docs/audio-assets.md`(判断は [ADR-0013](./docs/adr/0013-audio-assets-and-licensing.md))

## 開発フロー

1. VCC で `amidakuji-world` プロジェクトを開く
2. ClientSim でローカル単体テスト
3. SDK の `Build & Test` で多人数同期テスト(ローカル複数クライアント)
4. SDK の `Build & Publish` で Private アップロード(Windows)
5. メインアカウントで Join 動作確認
6. Phase 7 で Platform を Android に切替、再ビルド + 同じ Blueprint ID にアップロード
7. Quest 実機で動作確認
8. v1.0完成時点で問題なければ Community Labs 公開ボタンを押下

毎日 Phase 終了時に Build & Test まで通すこと。「明日まとめてテスト」はNG。Git運用・プラットフォーム切替詳細は `docs/dev-workflow.md` 参照。

## パフォーマンスバジェット

### 共通 (PC + Android)

| 指標 | PC (Good) | Android (Quest Good) |
| --- | --- | --- |
| Triangle Count | 70,000 | 250,000 (世界全体)、推奨は同程度に抑える |
| Material Count | 20 | 20 |
| Draw Call | 200 | 50-100 が望ましい |
| Realtime Light | 0(全て Baked or Mixed) | 0 |
| Texture | 制限なし(2048推奨) | **1024×1024 を上限** |
| ファイルサイズ | 制限緩い | **100 MB 以下** |
| 透明度マテリアル | 数枚 OK | **使用しない** |

ライティング戦略: Mixed Lighting + Light Probe + Reflection Probe (Static)。

実機目標: VR HMD でスポーン地点 **45 FPS 以上 (PC)** / **60 FPS 以上 (Quest)**。

## Android (Quest) 対応の主要制約

- **シェーダー**: World では制限なしだが、`VRChat/Mobile/Standard Lite` を基本に使う(`_Color` と `Enable GPU Instancing` を備え、Lightmap 対応 + Quest 軽量パス。詳細は [docs/material-set.md](./docs/material-set.md) §2.1)
- **透明度**: 使わない(マテリアルでアルファブレンド禁止)
- **Mirror, Cloth, Video Player**: 使わない
- **Post Processing**: 控えめ(SSR, SSAO は VR で問題)
- **GPU Instancing**: 全マテリアルで有効化必須

詳細は [ADR-0010](./docs/adr/0010-android-in-v1.0-scope.md)。

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
- Android Content Limitations: https://creators.vrchat.com/platforms/android/quest-content-limitations/
- Cross-Platform Setup: https://creators.vrchat.com/platforms/android/cross-platform-setup/

## 用語

- **インスタンス**: VRChatワールドの実行単位。同時にいるプレイヤーで共有される空間
- **インスタンスオーナー / Master**: インスタンス内で `Networking.IsMaster` が true のクライアント
- **Late Joiner**: 走行中などに後から参加してきたプレイヤー
- **Trust Rank**: VRChatのユーザー信頼度。Community Labs 公開には User 以上が必要
- **VCC**: VRChat Creator Companion(プロジェクト・SDK管理ツール)
- **Blueprint ID**: VRChatワールドの一意識別子。PC版とAndroid版は同じBlueprint IDで紐づける
