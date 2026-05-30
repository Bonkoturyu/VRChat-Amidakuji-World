# ADR-0013: オーディオ(SFX + BGM)の採用と CC0 統一ライセンス方針

- **Status**: Accepted
- **Date**: 2026-05-15(制定) / 2026-05-30(現行実装に整合化)

## Context

v1.0 に音を入れるにあたり、以下を一括で決める必要があった。

1. ゴール演出の効果音(SFX)に何を使うか
2. BGM を v1.0 に含めるか(当初は v1.1 送りだった)
3. 音源のライセンス方針(本リポジトリは MIT + 将来 Public 化の可能性あり)
4. 各音源の再生方式・Quest 対応の Import 設定

なお **ゴール演出そのものの設計**(当落の seed 由来ランダム割当、A/B 発火モード、AudioSource の配置構造)は [ADR-0012](./0012-goal-effect-randomized.md) で確定済みであり、本 ADR はそこに**乗せる音源アセットとライセンス**の判断に限定する。当落をどう割り当てるか(固定 or ランダム)は ADR-0012 が正(= **seed 由来ランダム**、本 ADR では再決定しない)。

## Decision

### 1. CC0 統一(repo 同梱・Public 化の前提)

採用する音源は **CC0(パブリックドメイン)表記のものに限定**する。

- 理由: MIT + Public リポジトリ化の可能性があるため、再配布制約のある素材は repo に同梱できない。CC0 なら再配布・改変・商用・repo 同梱すべて可、クレジット表記も法的に不要。
- 生ファイルの LFS 管理は `.gitattributes` の `*.wav` `*.ogg` `*.mp3` 設定でカバー済み。

**プラットフォーム別の repo 同梱(Public)可否** — 「ワールドに埋め込む」のはどのソースでもほぼ可だが、「生ファイルを Public repo で再配布」できるのは CC0 のものだけ:

| ソース | repo 同梱(Public)可否 | 備考 |
| --- | --- | --- |
| Freesound で **CC0 フィルタ** | ✓ | 最も確実。per-file で CC0 を選べる |
| Pixabay で **"CC0" 表記**(2019-01-09 以前) | ✓ | 要・個別確認 |
| Pixabay の **Content License**(現行デフォルト) | ✗ | スタンドアロン再配布禁止。ワールド埋め込みのみ可 |
| Uppbeat | ✗ | User Agreement 7.1.2 で再配布禁止 |
| 効果音ラボ / 効果音辞典 | ✗ | 再配布原則禁止 |
| Myinstants | ✗✗ | 個人非商用のみ + 出所不明。採用不可 |

> **Pixabay は「全部 CC0」ではない**。2019-01-09 より前のアップロードのみ CC0、それ以降は Content License(スタンドアロン再配布禁止)。取得時にライセンス表記が "CC0" か "Content License" かを必ず確認する。

### 2. 採用音源(確定 / いずれも CC0)

具体的な出所・URL・配置先・Import 設定の一覧は **[docs/audio-assets.md](../audio-assets.md)** を正とする(本 ADR は判断、audio-assets.md は資産レジストリ)。確定内容の要約:

| 用途 | タイトル | 作者 | 配布元 / ID | repo 内ファイル |
| --- | --- | --- | --- | --- |
| BGM | Mutant Club | HoliznaCC0 | Free Music Archive | `Assets/_Project/Audio/BGM/HoliznaCC0-Mutant-Club.wav` |
| 当たり(紙吹雪/祝砲) | Balloon Pop / Confetti Cannon | Breviceps | Freesound 458398 | `Assets/_Project/Audio/SE/balloon-pop.wav` |
| ハズレ(爆発) | bomb sound | mt_moon8 | Freesound 592319 | `Assets/_Project/Audio/SE/bomb-sound.wav` |

- SFX 2 種は配置済み。Breviceps の音源は配布元で "Balloon Pop / Christmas cracker / Confetti Cannon" と複数表記されるが Freesound ID 458398 の同一ファイルで、repo では `balloon-pop.wav` として運用する。
- BGM(Mutant Club)はファイル配置済み(WAV ソース約 25 MB)。**残作業は Import 設定(Vorbis 圧縮)とシーンへの AudioSource 配置**(§4)。HoliznaCC0 は全作品 CC0 のため、将来の差し替え・追加も同作者から安全に調達できる。

### 3. BGM を v1.0 に含める(当初 v1.1 → v1.0 へ昇格)

当初 BGM は工数優先で v1.1 送りだったが、「CC0 ループ 1 本を 2D・低音量で流すだけ」なら工数は軽微(Phase 9 で 30 分程度)で、カジュアルなパーティーゲームとしての体験価値が上がるため v1.0 に昇格する。

- **単一ループ BGM のみ** v1.0。動的 BGM 切替(待機中/レース中で曲を変える等)・カウントダウン専用ジングル・ゴールファンファーレは **v1.1 以降**。
- Phase 9(ライティング・最終最適化)に BGM 配置タスクを組み込む。

### 4. 再生方式と Quest 対応 Import 設定

| 音 | Spatial | Loop | 再生主体 | Import |
| --- | --- | --- | --- | --- |
| BGM | **2D**(均一に流す) | ON | AudioSource 1 本 | Compression Format = **Vorbis** / Quality 50〜70% / Load Type = Streaming or Compressed In Memory |
| ゴール個別 SE(爆発/紙吹雪) | 3D(空間減衰) | OFF | 各 PrizeArea の AudioSource(ADR-0012 §6) | 短尺のため WAV 可 |
| A モード共通 SE | 2D | OFF | `GameManager` 直下 `finaleSharedAudio`(ADR-0012) | 短尺のため WAV 可 |

- BGM 音量はゴールの紙吹雪/爆発が埋もれない控えめなバランスにする。
- **Quest 100 MB 制限に効くのは Import の Compression Format(Vorbis)+ Quality** であり、ソースファイルの形式ではない。Unity はソース WAV をそのままビルドに含めず再エンコードするため、25 MB の WAV ソースもビルドでは数 MB に収まる(`.ogg` への事前変換は不要)。Load Type は単一ループ BGM なので Streaming / Compressed In Memory どちらでも可。SFX は短尺(< 4 s)のため WAV のままで許容。
- AudioSource はワールド全体で **8 本以内**(BGM 1 + 共通 SE 1 + PrizeArea 4 × 各 SE で収まる)。
- **2D 音源(BGM)は VRCSpatialAudioSource を手動付与して `Enable Spatialization` を OFF にする**。VRChat はアップロード時に未付与の AudioSource へ spatialization=ON で自動付与するため、放置すると 2D のつもりの BGM が実機で 3D 点音源化する。ClientSim はこれを再現しないので食い違う。最終音量は ClientSim ではなく実アップロードで調整(落とし穴の詳細は [ui-pitfalls.md §5](../ui-pitfalls.md))。

## Consequences

### Positive

- あみだくじの「当落」体験 + カジュアルな BGM で v1.0 の体験価値が完成する。
- **CC0 統一**により Public 化時のライセンス問題を回避、クレジット表記コストゼロ。
- 追加同期ゼロ(SFX は ADR-0012 の seed 決定論で発火、BGM はローカル 2D 再生)。

### Negative

- BGM は単一ループのため長時間プレイで単調になりうる(動的切替は v1.1)。
- Quest 向けに粒子数・音量・見映えの実機調整が必要(Phase 8〜9)。

### v1.1 拡張余地

- 待機中/レース中/結果表示中で BGM を切り替え、カウントダウン専用ジングル、ゴールファンファーレ。
- HoliznaCC0 の別曲で雰囲気バリエーション。

## 関連

- [ADR-0012](./0012-goal-effect-randomized.md): ゴール演出の割当・発火・AudioSource 構造(本 ADR の音源が乗る土台)
- [ADR-0010](./0010-android-in-v1.0-scope.md): Android 制約(透明度・ファイルサイズ・粒子バジェット)
- [docs/audio-assets.md](../audio-assets.md): 採用音源の資産レジストリ(出所・URL・配置先の正)

## 改訂履歴

- 2026-05-15: 制定(旧 timeline では SFX=ADR-0011 / BGM=ADR-0012 として起草。CC0 統一方針、採用音源、BGM の v1.0 昇格を確定)
- 2026-05-30: 現行 repo に統合。ADR 番号衝突(0011=flat-layout / 0012=goal-effect が使用済み)を解消し SFX+BGM を **ADR-0013** に集約。当落割当は ADR-0012(seed 由来ランダム)を正とし本 ADR からは再決定を削除。実ファイル名(`balloon-pop.wav` / `bomb-sound.wav`)と `Audio/BGM/`・`Audio/SE/` のサブフォルダ構造に整合化
