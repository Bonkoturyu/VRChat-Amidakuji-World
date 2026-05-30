# Audio Assets

本プロジェクトで使用するオーディオ素材の出どころ・URL・ライセンス・配置先の一覧(資産レジストリ)。
全て **CC0**(パブリックドメイン)で、クレジット表記は不要・repo 同梱可・商用可・改変可。

判断の根拠(なぜ CC0 統一か / BGM を v1.0 にした理由 / 再生方式)は [adr/0013-audio-assets-and-licensing.md](./adr/0013-audio-assets-and-licensing.md) を参照。

## 一覧

| 用途 | タイトル | 作者 | ライセンス | 配布元 | ページ URL | repo 内ファイル |
| --- | --- | --- | --- | --- | --- | --- |
| BGM | Mutant Club | HoliznaCC0 | CC0 1.0 | Free Music Archive | <https://freemusicarchive.org/music/holiznacc0/power-pop/mutant-club/> | `Audio/BGM/HoliznaCC0-Mutant-Club.wav` |
| 当たり(紙吹雪) | Balloon Pop / Confetti Cannon | Breviceps | CC0 | Freesound | <https://freesound.org/people/Breviceps/sounds/458398/> | `Audio/SE/balloon-pop.wav` |
| ハズレ(爆発) | bomb sound | mt_moon8 | CC0 | Freesound | <https://freesound.org/people/mt_moon8/sounds/592319/> | `Audio/SE/bomb-sound.wav` |

配置ルートは `Assets/_Project/Audio/` 配下。BGM 用 `Audio/BGM/` と効果音用 `Audio/SE/` のサブフォルダに分ける。

## 詳細

### BGM — Mutant Club(配置済み)

- **作者**: HoliznaCC0
- **ライセンス**: CC0 1.0 Universal
- **配布元**: Free Music Archive
- **ページ**: <https://freemusicarchive.org/music/holiznacc0/power-pop/mutant-club/>
- **尺 / 形式**: 2:10、インストゥルメンタル、Synth Pop。ソースは WAV(約 25 MB)
- **配置先**: `Assets/_Project/Audio/BGM/HoliznaCC0-Mutant-Club.wav`
- **Import 設定**: Compression Format = **Vorbis**、Quality 50〜70%(ビルドサイズの実効レバー。これで 25 MB の WAV ソースもビルドでは数 MB に圧縮される。`.ogg` への事前変換は不要)。Load Type = Streaming または Compressed In Memory(単一ループ BGM なのでどちらでも可)
- **再生方式**: AudioSource 1 本、Spatial Blend = 2D、Loop = ON、Play On Awake = ON、音量控えめ(ゴール SE が埋もれないバランス)
- **備考**: HoliznaCC0 は全作品 CC0。別曲が欲しい場合も同作者から安全に調達可 → <https://freemusicarchive.org/music/holiznacc0/>

### 当たり(紙吹雪) — balloon-pop.wav(配置済み)

- **作者**: Breviceps
- **ライセンス**: CC0
- **配布元**: Freesound(ID 458398)
- **ページ**: <https://freesound.org/people/Breviceps/sounds/458398/>
- **配置先**: `Assets/_Project/Audio/SE/balloon-pop.wav`
- **備考**: 配布元では "Balloon Pop / Christmas cracker / Confetti Cannon" と複数表記されるが ID 458398 の同一ファイル。repo では `balloon-pop.wav` 名で運用。短尺のため WAV のまま使用(3D Spatial、PrizeArea の祝砲演出に紐付け)
- **DL メモ**: Freesound はダウンロードにログインが必要

### ハズレ(爆発) — bomb-sound.wav(配置済み)

- **作者**: mt_moon8
- **ライセンス**: CC0
- **配布元**: Freesound(ID 592319)
- **ページ**: <https://freesound.org/people/mt_moon8/sounds/592319/>
- **配置先**: `Assets/_Project/Audio/SE/bomb-sound.wav`
- **備考**: 短尺のため WAV のまま使用(3D Spatial、PrizeArea の爆発演出に紐付け)
- **DL メモ**: Freesound はダウンロードにログインが必要

## ライセンスについて

3 点とも **CC0 1.0(パブリックドメイン)** のため:

- クレジット表記は法的に不要(任意。礼儀として記載してもよい)
- リポジトリへの同梱・Public 化・商用利用・改変(トリミング/ループ加工等)すべて自由
- `.gitattributes` の LFS 設定(`*.wav` `*.ogg` `*.mp3`)でそのまま管理

任意でクレジットを書く場合の例(README 等):

```text
Audio (all CC0 / public domain):
- BGM: "Mutant Club" by HoliznaCC0 (Free Music Archive)
- Win SFX: "Confetti Cannon" by Breviceps (Freesound)
- Lose SFX: "bomb sound" by mt_moon8 (Freesound)
```

## 関連

- 音源の採用判断・CC0 統一方針・再生方式: [adr/0013-audio-assets-and-licensing.md](./adr/0013-audio-assets-and-licensing.md)
- ゴール演出(割当・発火・AudioSource 構造): [adr/0012-goal-effect-randomized.md](./adr/0012-goal-effect-randomized.md)
- CC0 以外のソース(効果音ラボ/Uppbeat/Pixabay Content License 等)を避ける理由: [adr/0013](./adr/0013-audio-assets-and-licensing.md) §1 のプラットフォーム別可否表
