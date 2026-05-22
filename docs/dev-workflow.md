# Development Workflow

Git運用、GitHub Private Repository、CI、テスト戦略をまとめる。
v1.0 公開(5/31)までは最小限の運用、公開後に段階的に整備する方針。

## 1. リポジトリ設計

### 1.1 GitHub Private Repository

- リポジトリ名: `amidakuji-world` (個人の Private リポジトリ)
- ライセンス: **MIT License** (ルート `LICENSE` ファイル配置済み)
- README.md は v1.0 公開時に整備(ワールド紹介・スクショ・操作説明・OSS依存のクレジット)

### ライセンス採用方針

VRChat OSS界隈(UdonSharp, lilToon, ClientSim, AudioLink, VRCFury 等)はMITが事実上のデファクト。本プロジェクトも以下の理由でMITを採用:

- 商用利用OK(将来BOOTH等で派生物を出す可能性に対応)
- VRChat翻訳ツール(別プロジェクト)とのライセンス一貫性
- VRChat公式パッケージ群(UdonSharp等)との衝突がない
- 簡潔(1ファイル配置で済む)

### 依存パッケージのライセンス取り扱い

`Packages/` 配下に VCC がインストールする外部パッケージ(UdonSharp, ClientSim, 任意でlilToon等)は、それぞれ自身の LICENSE ファイルを同梱している。これらは触らずそのまま残す。README.md にクレジット記載することで MIT のAttribution要件を満たす。

### 1.2 ディレクトリ運用

リポジトリルートは Unity プロジェクトのルートと一致させる:

```text
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

```text
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

```text
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

## 9. VRChat ワールド公開フロー (Private → Labs → Public)

VRChat には「Build時にPublic/Private選択」のような単純なトグルは存在せず、3段階の状態を経て公開する仕組みになっている。アップロードしただけでは絶対に他人には公開されないので、開発中は安心して何度でも Build & Upload して良い。

### 状態遷移

```text
[SDK Build & Upload]
        ↓
   Private (自動)
   - 自分しか入れない
   - 招待 or ポータルで他人を入れることは可能
   - Build & Upload を繰り返してもPrivateのまま、バージョン更新されるだけ
        │
        ↓ (SDKまたはvrchat.com から明示的に操作)
   Community Labs
   - Labsオプトインユーザーが見つけられる
   - 1ワールド/週の公開制限
        │
        ↓ (VRChat側の自動判定、待ち時間あり)
   Public
   - 全VRChatユーザーが Worlds タブで見つけられる
```

### Build & Upload と Build & Test の使い分け

| 操作 | 動作 | 用途 |
| --- | --- | --- |
| `Build & Test` | ローカルクライアントで一時起動 | 単体ローカルテスト、毎日の動作確認 |
| `Build & Test (Multi-Client)` | ローカル複数クライアント起動 | 多人数同期テスト |
| `Build & Upload` | VRChatサーバにアップロード(Private) | 実機HMD検証、Late Joinerテスト |

Phase 1〜9 では `Build & Test` をメインに、`Build & Upload` は実機マルチクライアント検証や HMD実機での感触確認時のみ。

### Publish to Community Labs の操作

`Build & Upload` 後、以下のいずれかから:

- **SDK経由**: Builder タブの "World Visibility" セクションに表示される「Publish to Community Labs」ボタン
- **Web経由**: https://vrchat.com/home/worlds → 自分のワールド → "Danger Zone" → "Publish to Community Labs"

### 公開時の前提条件

- VRChatアカウントの Trust Rank が **User以上** であること(`New User` ではダメ)
- 1ワールド/週の Publish 制限内であること
- ワールド情報(タイトル、説明、サムネイル)が設定済みであること

### Public 昇格

Publicへの昇格は VRChat 側の自動判定。VRChat側の評価指標(訪問者数、滞在時間、Favorite数など)に基づいて自動的にPublic昇格する。具体的な閾値・期間は公開されていない。v1.0 の到達目標は Labs 公開まで、Public 昇格は時間任せ。

## 10. クロスプラットフォーム (PC + Android) ビルドワークフロー

v1.0 では Windows + Android の両プラットフォームに対応する([ADR-0010](./adr/0010-android-in-v1.0-scope.md))。
同じ Blueprint ID に対して、プラットフォームごとに別ビルドをアップロードする仕組み。

### 10.1 初回セットアップ (Phase 0)

- Unity Hub から **Android Build Support** モジュールを追加インストール
- VCC で `amidakuji-world` プロジェクトを開く
- VRChat SDK Control Panel の Builder タブで Platform が Windows になっていることを確認

### 10.2 通常開発時 (Phase 1〜6)

- Build Platform は **Windows のまま** で開発
- ClientSim と Build & Test で PC 環境を中心にイテレーション
- ただし以下は Phase 1 から守る:
  - マテリアルを `VRChat/Mobile/Standard Lite` 系で作成(詳細は [material-set.md](./material-set.md))
  - テクスチャ 1024×1024 以下
  - 透明度マテリアル使用しない
  - GPU Instancing を全マテリアルで有効化

### 10.3 Android プラットフォーム切替 (Phase 7)

1. VRChat SDK Control Panel の **Builder タブ → Build セクション → Platform ドロップダウン**
2. Windows のチェックを外し、**Android** をチェック
3. "Switch" 確認ダイアログで切替実行
4. **再インポートが走る**(プロジェクトサイズによっては 10〜30 分かかる、放置)
5. 完了後、Validation メッセージを確認
6. 問題があれば対処(マテリアル変更、テクスチャ縮小、コンポーネント除去)
7. Build & Test (Android) で Quest シミュレートを起動して確認
8. Quest 実機で Build & Test、または Build & Upload で Private アップロード後 Quest から Join

### 10.4 Blueprint ID の共有

PC 版と Android 版は **同じ Blueprint ID** で紐づける必要がある。

- 初回 PC でアップロードしたときに自動的に Blueprint ID が生成され、`VRCWorld` の Pipeline Manager コンポーネントに記録される
- Android に切替後も同じシーン・同じ `VRCWorld` を使うので、Blueprint ID は自動的に保たれる
- 別シーンや別プロジェクトを使う場合は、Pipeline Manager の "Detach" → Blueprint ID をペースト で同期する

### 10.5 開発ループ(Phase 7 以降)

```text
1. Windows 版で機能追加・修正
2. Build & Test (PC) で動作確認
3. Build & Upload (PC) で Private アップロード
4. PC実機で確認

5. Platform を Android に切替(数十分の再インポート覚悟)
6. Build & Test (Android) または Quest 実機で確認
7. Build & Upload (Android) で同じ Blueprint ID にアップロード
8. Quest 実機で確認

9. Platform を Windows に戻して次の機能へ
```

**注意**: Platform 切替時の再インポートは時間がかかるため、Phase 7 以降は「PC開発を一段落させてから Android チェック」というリズムが推奨。毎日の細かい切替は避ける。

### 10.6 マルチプラットフォーム同時ビルド (非推奨)

SDK の Builder タブで「Windows と Android を同時選択 → Build」も可能ではあるが、**SDK 3.7.6 で無限ループになるバグが報告されている**。安定するまでは 1 プラットフォームずつアップロードする方が安全。

### 10.7 iOS 非対応の方針

v1.0 では iOS ビルドは作らない([ADR-0010](./adr/0010-android-in-v1.0-scope.md))。

VRChat には実験的な「iOS で Android ビルドを読む」フォールバック機能があり、これに頼って iOS ユーザーが入れる可能性は許容するが、保証はしない。v1.1 で iOS 実機テスト体制が整ってから対応する。

## 11. Blueprint ID の保護ワークフロー

VRChat の `blueprintId` (例: `wrld_13f1b8a9-...`) は個人アカウント固有のワールド識別子で、`Build & Publish` 時にシーンファイル内の VRC Scene Descriptor へ自動書き込みされる。リポジトリにコミットすると、

- 他者がフォークしても同じ ID は使えない(別アカウントから上書きできない)
- 個人が運用するワールド ID が公開リポジトリに混入する

ため、シーンファイルに書き込まれた blueprintId はコミット前に退避し、コミット後に書き戻す運用とする。PC 版・Android 版いずれも同じ Blueprint ID を共有するため、プラットフォーム切替前後どちらでも退避・復元の対象は同じシーンファイル 1 つ。

### 退避先

- `.blueprint-id.local` (リポジトリルート配置、`.gitignore` 済み)
- 形式: `<シーンファイルの相対パス>=<blueprintId>` (1 行 1 エントリ、`#` 始まりはコメント)

### 退避・復元スクリプト

| スクリプト | 役割 |
| --- | --- |
| `scripts/Save-BlueprintId.ps1` | シーンファイル内の `blueprintId: wrld_*` を `.blueprint-id.local` に保存し、シーンファイル内の値を空にする |
| `scripts/Restore-BlueprintId.ps1` | `.blueprint-id.local` の値を対応するシーンファイルへ書き戻す (冪等) |
| `scripts/Save-BlueprintId.cmd` / `scripts/Restore-BlueprintId.cmd` | 上記 `.ps1` を `-NoProfile -ExecutionPolicy Bypass` 付きで呼ぶラッパー。ExecutionPolicy で `.ps1` 実行がブロックされる環境向け |

PowerShell の ExecutionPolicy が `Restricted` / `AllSigned` の場合、`pwsh scripts/Save-BlueprintId.ps1` は「スクリプトの実行が無効になっている」セキュリティエラーで起動できない。システム設定を変えずに回避するため、`.cmd` ラッパーを併設してある。`.cmd` 経由の呼び出しを基本にし、生 `pwsh` 呼び出しは ExecutionPolicy 設定済み環境向けの代替として併記する。

### コミット前

```cmd
:: 推奨 (.cmd ラッパー経由、ExecutionPolicy 制限を回避)
scripts\Save-BlueprintId.cmd
git add -u
git commit -m "..."
git push
```

```powershell
# 代替: ExecutionPolicy を許可している環境では直接呼び出しても良い
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Save-BlueprintId.ps1
```

### コミット後 (Unity 作業を継続する場合)

```cmd
scripts\Restore-BlueprintId.cmd
```

```powershell
# 代替
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Restore-BlueprintId.ps1
```

復元後、シーンファイルは modified 状態として残る(VRChat SDK 上でアップロードを継続するため)。これは許容し、次回コミット前に再度 `Save-BlueprintId.cmd` を実行する。

### 初回セットアップ (新しい開発環境)

1. VCC でプロジェクトを開き、VRChat SDK Control Panel から `Build & Publish` を実行 → blueprintId が新規発行されシーンファイルへ書き込まれる
2. `scripts\Save-BlueprintId.cmd` を実行して `.blueprint-id.local` を生成
3. 以後は上記「コミット前 / 後」のフローで運用

### 注意

- `Build & Publish` の度にシーンファイルへ blueprintId が再注入される。**コミット前の `Save-BlueprintId.cmd` 実行を忘れない**
- 複数シーンを扱う場合は `.blueprint-id.local` に複数行登録できる。`Save-BlueprintId.cmd -SceneFile <path>` で対象を指定する(引数は `.ps1` にそのまま渡る)
- 自動化 (git hook 化) は VRChat SDK のビルドフローと競合するリスクがあるため、当面は手動運用とする
