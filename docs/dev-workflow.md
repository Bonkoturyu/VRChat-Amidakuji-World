# Development Workflow

Git運用、GitHub Private Repository、CI、テスト戦略をまとめる。
v1.0 公開(5/31)までは最小限の運用、公開後に段階的に整備する方針。

## 1. リポジトリ設計

### 1.1 GitHub Private Repository

- リポジトリ名: `amidakuji-world` (個人の Private リポジトリ)
- ライセンス: 個人開発のため未設定(v1.0公開後にライセンス検討)
- README.md は v1.0 公開時に整備(ワールド紹介・スクショ・操作説明)

### 1.2 ディレクトリ運用

リポジトリルートは Unity プロジェクトのルートと一致させる:

```
amidakuji-world/        ← Git Root = Unity Project Root
├── .git/
├── .gitignore
├── .gitattributes      ← Git LFS設定
├── CLAUDE.md
├── BACKLOG.md
├── README.md           ← v1.0公開時に作成
├── docs/
├── Assets/
├── Packages/
└── ProjectSettings/
```

VCC は `Packages/manifest.json` でパッケージを管理するため、これはコミット対象。`Library/` 以下は Unity が自動再生成するためコミット不要。

## 2. .gitignore

Unity 標準 + VRChat 固有の除外設定。詳細は別ファイル [.gitignore](../.gitignore) を参照。

主な除外対象:

- `Library/`, `Temp/`, `Logs/`, `obj/` — Unity が自動生成、サイズ大
- `UserSettings/` — ローカル個人設定
- `*.csproj`, `*.sln`, `*.suo`, `*.user` — IDE 生成、Unity が再生成可能
- ビルド成果物: `*.apk`, `*.aab`, `*.ipa`, `*.unitypackage`
- OS固有: `.DS_Store`, `Thumbs.db`

**必ずコミットするもの**:
- `Assets/` 配下の全てと `.meta` ファイル(GUID紐づけのため絶対に除外しない)
- `Packages/manifest.json`, `Packages/packages-lock.json`
- `ProjectSettings/` 配下
- `.gitignore`, `.gitattributes`

## 3. Git LFS 設定

Unity プロジェクトは大きなバイナリアセットが含まれるため、Git LFS を有効化する。

`.gitattributes` で以下を LFS 管理:

- 画像: `*.psd`, `*.tga`, `*.png` (大きいもの), `*.tiff`
- 3Dモデル: `*.fbx`, `*.obj`, `*.blend`, `*.dae`, `*.3ds`, `*.mb`, `*.ma`
- 音声: `*.wav`, `*.mp3`, `*.ogg`, `*.aiff`
- 動画: `*.mp4`, `*.mov`, `*.avi`
- バイナリ: `*.unitypackage`, `*.asset` (大きいもの)

シーンファイル(`*.unity`)、Prefab(`*.prefab`)、マテリアル(`*.mat`)は Unity 2022 ではテキストYAMLなので LFS 対象外。

### LFS セットアップ手順 (Phase 0)

```bash
# git lfs インストール後
git lfs install
# .gitattributes 配置後にリポジトリ全体に適用
git add .gitattributes
git commit -m "chore: Setup Git LFS"
```

GitHub Private リポジトリでは LFS のストレージ・帯域に上限あり(無料枠 1GB)。個人開発では当面問題にならないが、長期的に超える可能性があれば確認する。

## 4. ブランチング・コミット運用

v1.0 期間は単独開発なので最小構成:

- **`main` ブランチで直接開発**(ブランチ運用のオーバーヘッドを避ける)
- 各 Phase 完了時に必ずコミット
- Phase 完了タグを推奨: `phase-0-done`, `phase-1-done`, ...
- v1.0 公開時に `v1.0.0` タグを打つ

v1.1 以降に複数機能を並行開発する局面が出てきたら `feature/*` ブランチ運用を導入する。

### コミットメッセージ規約

Conventional Commits ライクに、軽めの規約を採用:

- `feat: 〜` 機能追加
- `fix: 〜` バグ修正
- `chore: 〜` 雑務(Git設定、依存更新)
- `docs: 〜` ドキュメント変更
- `refactor: 〜` 内部リファクタ
- `perf: 〜` パフォーマンス改善
- `test: 〜` テスト関連

例:
```
feat(cart): カート単体走行を実装 (Phase 2)
fix(generator): seedから生成した横線の連続禁止チェックを修正
docs: ADR-0007 にVRトリガー退出の制約を追記
```

## 5. CI 戦略

### 5.1 v1.0 期間 (〜2026-05-31): CI なし

- 工数を Phase 進行に集中
- 個人プロジェクトでテストもこれから書くフェーズなので CI 投資は時期尚早
- Build & Publish は手動で SDK から実行

### 5.2 v1.1 期間 (2026-06以降): 軽量 CI を導入

GitHub Actions を使った静的チェックのみ。Unity 不要:

```yaml
# .github/workflows/docs.yml の方針 (実装は v1.1 で)
on:
  push: { paths: ["**/*.md", ".github/workflows/docs.yml"] }
  pull_request: { paths: ["**/*.md"] }

jobs:
  - markdownlint  (markdownlint-cli2)
  - ADR形式チェック (docs/adr/0XXX-*.md パターン、必須セクションの存在)
  - リンク切れチェック (lychee or markdown-link-check)
```

### 5.3 v1.2 期間: Unity ビルド・テスト CI

`game-ci/unity-builder` を使った Unity プロジェクトの compile + test 実行:

- Unity Personal ライセンスを GitHub Secrets に登録
- Edit Mode Tests を CI で実行
- Build はあくまで compile 通過確認用(VRChat 向けのアップロードは不可)

### 5.4 VRChat SDK と CI の制約

**VRChat SDK のヘッドレスアップロードは公式サポートなし**。CI で Build & Publish を自動化することはできない。Community Labs 公開ボタンも Web UI 経由のみ。

このため、CI でできるのは「コード・アセットの品質チェック」止まり。デプロイ自動化は望めない。

## 6. テスト戦略

### 6.1 UdonSharp スクリプトのテストは難しい

UdonSharp は Udon Assembly にコンパイルされ、Udon VM 上で動くため、通常の Unity Test Framework では直接テストできない。

### 6.2 ロジック分離パターン

**「Pure C# Logic クラス」と「UdonSharpBehaviour ラッパー」を分離する**ことで、ロジックを通常の C# としてテスト可能にする。

```
Assets/_Project/Scripts/
├── Logic/                        ← UnityEditor非依存、UdonSharp非依存
│   ├── AmidakujiLogic.cs        ← 横線生成アルゴリズム
│   ├── PathComputer.cs          ← Waypoint計算
│   └── StateMachine.cs          ← ステート遷移ロジック
├── Udon/                         ← UdonSharpBehaviour 群
│   ├── GameManager.cs           ← 中で Logic を呼ぶ
│   ├── AmidakujiGenerator.cs    ← AmidakujiLogic を呼ぶ
│   └── CartController.cs        ← PathComputer を呼ぶ
└── Editor/Tests/                 ← Unity Test Framework (Edit Mode)
    ├── AmidakujiLogicTests.cs
    ├── PathComputerTests.cs
    └── StateMachineTests.cs
```

ロジッククラスはUdonSharp依存を持たないため、通常の C# (.NET) として `Library/ScriptAssemblies` でコンパイルされ、Unity Test Runner からテスト可能。

### 6.3 v1.0 期間のテスト方針

- **テストは書かない**(時間優先)
- **ただしロジック分離は意識する**: Phase 3 着手時に `AmidakujiLogic` クラスを Pure C# として作る形にしておく
- これにより v1.1 で「すぐテストを書ける状態」になる

### 6.4 v1.1 期間のテスト追加

優先度順に:

1. **AmidakujiLogic**: seedを与えて、生成される横線配置が決定論的であること(同じseedで同じ結果)
2. **PathComputer**: seed + laneIndex から正しいWaypoint配列が出ること
3. **PRNG**: `System.Random` に同じseedを与えれば全環境で同じ列が出ること(回帰テスト)
4. **StateMachine**: 状態遷移の正当性(Idleからのみ着座可、など)

Edit Mode Tests で実装。Play Mode Tests は VRChat SDKシーンが必要になり手間なので避ける。

### 6.5 マニュアルテスト

VRChat固有の機能(同期、Late Joiner、VR HMD体験)はマニュアルテストに頼らざるを得ない:

- `Build & Test` で多クライアント同期テスト(VRChat SDK の機能、ローカルで複数 VRChat クライアントを起動)
- ClientSim で単体動作確認
- 友人を招いて Private インスタンスで実機テスト(Phase 10 最終確認時)

テストシナリオは `docs/tasklist.md` の各 Phase 完了基準に記述済み。

## 7. Phase 0 で実行すること(今日〜明日)

1. ローカルで `amidakuji-world` ディレクトリを Unity プロジェクトとして VCC で作成
2. GitHub で Private リポジトリ `amidakuji-world` を作成
3. ローカルで `git init` → `.gitignore`、`.gitattributes` を配置
4. `git lfs install`
5. 初回コミット: VCC生成直後の空プロジェクト + ドキュメント一式
6. GitHub にリモート追加 → push
7. Phase 0 完了タグ: `git tag phase-0-done`
8. 以降、Phase 完了ごとにコミット + タグ

## 8. 補足: VCC の Packages との付き合い方

VCC は `Packages/manifest.json` を編集することで VRChat SDK や UdonSharp のバージョンを管理する。

- このファイルは **コミット対象**(チームメンバーや別マシンで同じバージョンを再現するため)
- `Packages/packages-lock.json` も **コミット対象**(バージョン固定)
- VCC で SDK バージョンを上げたときは別コミットで明示: `chore(deps): VRChat SDK 3.x.x → 3.y.y`

`Library/PackageCache/` は自動再生成されるため除外(`.gitignore` 済み)。
