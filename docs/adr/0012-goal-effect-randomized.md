# ADR-0012: ゴール演出を seed 由来決定論で配置(爆発・紙吹雪のランダム振り分け)

- **Status**: Accepted
- **Date**: 2026-05-18

## Context

[SPEC.md](../SPEC.md) §8 までの設計では、カートがゴール手前バリアを通過した瞬間にプレイヤーを賞品エリアへテレポートして終わる、というフローのみが定義されていた。実体験としては「ゴールしただけだと味気ない」という感覚が残る。

[BACKLOG.md](../../BACKLOG.md) の v1.1 項目に「賞品エリアの演出強化(ゴールごとに異なる装飾・ギミック、パーティクル演出)」が登録済みだが、最低限の演出を v1.0 公開(2026-05-31)に間に合わせたい。一方で SPEC §12 では「カスタム賞品(賞品エリアは固定)」が **非対応事項** として明記されている。

追加の制約・要望:

- 演出は **爆発** と **紙吹雪** の 2 種類のみ
- 1 ラウンドにつき爆発 1 個 + 紙吹雪 1 個(残り 2 ゴールは無演出)
- 4 ゴール同時発火は起こらない(同居しない、排他)
- 「他のレーンから演出の一部が見える派手さ」が必要(賞品エリアは Z=-64 付近の小部屋、観戦者は MainFloor 側 Z=+12〜-58 から見る)
- 爆発は **ハズレ扱い**(オレンジ・赤・黒の混じった爆発、ドリフのコント落ち感)、紙吹雪は **祝砲扱い**(マルチカラー紙片)
- v1.1 の 20 人スケール対応時に **爆発数・紙吹雪数を可変設定** できる構造にしておきたい
- 演出タイミングは 2 モードを Inspector で切替可能にする(後述)

## Decision

ゴール演出を以下の構造で実装する。

### 1. 配置決定アルゴリズム(同期コスト 0)

各クライアントが seed から決定論的に算出するため、UdonSynced 変数の追加は **不要**。

```csharp
// GameManager 側
int[] ComputeEffectAssignment(int seed, int N, int E, int C) {
    // あみだくじ生成と同じ seed を共有しつつ、派生 RNG として 0xBEEF を混ぜる
    // (経路生成と演出配置を統計的に独立させる)
    var rng = new System.Random(seed ^ 0x000BEEF);
    var idx = new int[N];
    for (int i = 0; i < N; i++) idx[i] = i;
    // Fisher-Yates シャッフル
    for (int i = N - 1; i > 0; i--) {
        int j = rng.Next(0, i + 1);
        var tmp = idx[i]; idx[i] = idx[j]; idx[j] = tmp;
    }
    var result = new int[N];           // 0=none, 1=explosion, 2=confetti
    int e = Mathf.Min(E, N);
    int c = Mathf.Min(C, N - e);
    for (int i = 0; i < e; i++)         result[idx[i]]     = 1;
    for (int i = 0; i < c; i++)         result[idx[e + i]] = 2;
    return result;
}
```

決定論性:
- `seed` 同じ → C# `System.Random` の出力列同じ → `idx` 同じ → `result` 同じ
- C# `System.Random` は実装が固定(Knuth subtractive)で、PC / Quest / Android で再現性あり
- Late Joiner も `OnDeserialization` で `seed` を受信した時点で同じ結果を計算可能

### 2. Inspector 公開項目

`GameManager` に以下を追加:

| フィールド | 型 | 既定値 | 役割 |
|---|---|---|---|
| `explosionCount` | int | 1 | 爆発演出を割り当てるレーン数 |
| `confettiCount` | int | 1 | 紙吹雪演出を割り当てるレーン数 |
| `simultaneousFinale` | bool | true | 演出タイミングモード(後述) |
| `finaleCountdownSeconds` | float | 3.0 | 一斉モード時のカウントダウン秒数 |
| `prizeAreas` | PrizeArea[] | size = N | 各レーンの賞品エリア参照 |
| `finaleSharedAudio` | AudioSource | — | 一斉モード時の共通 SE 音源 |

v1.0 では `explosionCount=1, confettiCount=1` を固定値として運用。v1.1 で 20 レーン化したら値を変えるだけで動く(UI からの可変は v1.1 で別途検討)。

### 3. 演出タイミングモード(2 モード切替)

**既定モードは A、B モードは Master が UI トグルで切替可能**(2026-05-18 ユーザー確定)。Master の選択は VRChat Player Persistence で永続化し、同一人物が次回 Master として入場した際に自動復元する(§3.x で詳述)。

#### A モード(既定、`simultaneousFinale=true`)

```
全カートがゴール → (テレポート完了)
                → [FinaleCountdown] (3 秒、UI で "3-2-1" 表示)
                → 一斉発火: 全演出 Particle.Play + 共通 SE 1 発
                → 1.5 秒待機(演出を見せる)
                → [ResultDisplay]
```

- 共通 SE は「パパーン」的な祝祭音 1 種
- 個別の爆発音は同時に鳴っても可(賞品エリアの 3D 音源、空間減衰あり)

#### B モード(`simultaneousFinale=false`)

```
各カート個別にゴール到達 → 即 PlayEffect(個別 SE 鳴らす)
全カートゴール → [ResultDisplay]
```

- ゴールごとに音が鳴る(到達順)
- 「自分の順位で先にハズレが見える」展開ができる

### 4. 賞品エリア(テレポート先)は固定 — SPEC §12 と無衝突

SPEC §12 の「カスタム賞品(賞品エリアは固定)」は維持する。

- **テレポート先**(PrizeArea[0..3] の小部屋座標)は **固定**(変動しない)
- **演出種別**(爆発・紙吹雪・無)だけが seed 由来でレーンに振り分けられる
- 「ハズレ = 爆発」だが、テレポート先は変わらず到達自体は成立する(VRChat ワールドとしての賞品体験は同質、視覚演出だけバラエティ)

この区分により SPEC §12 の文言を維持したまま機能を足せる。SPEC §12 には「演出は seed 由来でランダム」を明示する注釈を 1 行追加する。

#### 演出割当と TeleportTo は終点 lane (goalLane) ベース

**演出割当の対象は Prize_X(終点 lane)** であり、Cart の起点 lane ではない。`_effectKinds[goalLane]` で各 Prize に演出種別が固定され、Cart は `ComputePath` で算出した `_goalLaneIndex` を経由してその Prize の演出を発火する。

- TeleportTo 先も同じく終点 lane: `prizeAreas[_goalLaneIndex].teleportTarget`
- `_NotifyCartGoaled(startLane, goalLane)` に両 lane を渡し、空席判定 (`participantPlayerIds[startLane]`) と演出種別/Prize 参照 (`_effectKinds[goalLane]` / `prizeAreas[goalLane]`) で使い分ける

理由: 演出種別は「Prize_n 部屋の属性」として認識されるべきで(ハズレ部屋=爆発、当たり部屋=紙吹雪)、Cart の起点に紐付けると同じプレイヤーが毎ラウンド同じ起点に着座した場合に同じ演出を見続けることになり、賞品エリアの「部屋ごとの個性」が立たないため。Stage A 中に起点 lane ベース実装で初期化したものを、上記理由で終点 lane ベースに修正(2026-05-21 確定)。

### 5. ステートマシン拡張

A モード時のみ、`Running` と `ResultDisplay` の間に `FinaleCountdown` を挟む。ただし独立ステートにはせず、`Running` 末尾の遷移待ちフェーズとして UI フラグで扱う(同期変数の追加を回避)。

```
既存:  [Idle] → [Countdown] → [Running] → [ResultDisplay] → [Idle]
新規:  [Idle] → [Countdown] → [Running] → (FinaleCountdown_UIフェーズ) → [ResultDisplay] → [Idle]
                                            ↑ A モード時のみ、gameState は Running のまま
```

### 6. PrizeArea Prefab 構造

```
PrizeArea_n (GameObject, UdonSharp 不要)
├─ TeleportTarget (Transform、テレポート先位置)
├─ ExplosionEffect (GameObject, 既定 inactive)
│   ├─ ParticleSystem × 2 (火球 + 灰白色煙)
│   └─ AudioSource (個別爆発音、3D Spatial)
└─ ConfettiEffect (GameObject, 既定 inactive)
    ├─ ParticleSystem (紙片噴射)
    └─ AudioSource (個別紙吹雪音、3D Spatial)
```

`PlayEffect(int kind, bool withIndividualSound)` API を持つ軽量 UdonBehaviour を1 個付ける(または GameManager から直接 SetActive + Play でも可、後者のほうが軽い)。

### 7. B モード切替 UI と Player Persistence による永続化

#### UI(Phase 5 で実装)

- エントリーエリアのスタートボタン付近に **「演出モード切替トグル」** を 1 個配置(`SimultaneousFinaleToggle`)
- 操作権限は **Master のみ**(`Networking.IsMaster` で押下可否を判定、UI は非 Master にはグレーアウト表示)
- gameState が `Idle` のときのみ反応(Countdown / Running / ResultDisplay 中は無効)
- トグル押下で `gameManager.simultaneousFinale` の真偽を反転 → 即座に表示更新

#### Player Persistence(VRChat SDK 3.7.4 以降、2024 年導入)

VRChat の Player Persistence(`VRC.SDK3.Persistence.PlayerData`)を使い、Master の B モード選好を **同一人物が再度 Master として入場した際に復元** する。

##### 確定 API シグネチャ(2026-05-18 公式ドキュメント確認時点)

- 名前空間: `VRC.SDK3.Persistence`(`using VRC.SDK3.Persistence;`)
- クラス: `PlayerData`(静的メソッド群)
- **保存(ローカルプレイヤーのみに書き込み、自動同期で全クライアントに伝播)**:
  - `PlayerData.SetBool(string key, bool value)` → void
  - 引数に VRCPlayerApi は取らない(暗黙に `Networking.LocalPlayer` の領域に書く)
- **取得(任意のプレイヤーのデータを読める)**:
  - `PlayerData.GetBool(VRCPlayerApi player, string key)` → bool(キー未設定時は既定値 false)
  - `PlayerData.TryGetBool(VRCPlayerApi player, string key, out bool result)` → bool(成功=true / 未設定=false の判別が必要なときに使用)
- **イベント(UdonSharpBehaviour で override)**:
  - `OnPlayerRestored(VRCPlayerApi player)`: そのプレイヤーの永続データ読み込み完了時に発火
    - **自分が入室した直後**: インスタンス内の全プレイヤー分が順次発火(自分自身含む)
    - **他人が入室した直後**: その入室者の分のみ発火
  - `OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)`: 任意プレイヤーの PlayerData 値が変更されたとき発火
- 容量制限: 1 ワールドあたり **PlayerData 100 KB + PlayerObject 100 KB / プレイヤー**

##### 実装パターン

```csharp
using UdonSharp;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon.Common;

public class FinaleModeManager : UdonSharpBehaviour {
    public GameManager gameManager;
    private const string KEY_SIMULTANEOUS_FINALE = "amidakuji.simultaneousFinale";

    // 保存: トグル UI から呼ばれる(Master のみ操作可)
    public void ToggleFinaleMode() {
        if (!Networking.IsMaster) return;
        gameManager.simultaneousFinale = !gameManager.simultaneousFinale;
        PlayerData.SetBool(KEY_SIMULTANEOUS_FINALE, gameManager.simultaneousFinale);
        // SetBool は自動で全クライアントに同期される。
        // gameManager.simultaneousFinale 自体も UdonSynced なら別途 RequestSerialization。
        gameManager.RequestSerialization();
    }

    // 復元 (1): 自分が入室時、自分の Restored が来たら Master 判定して復元
    public override void OnPlayerRestored(VRCPlayerApi player) {
        if (player.isLocal && Networking.IsMaster) {
            TryRestoreFinaleMode();
        }
    }

    // 復元 (2): 他者退出で自分が Master 昇格した場合
    // (OnPlayerRestored は新規入室時のみ発火のため、Master 昇格はここでフック)
    public override void OnPlayerLeft(VRCPlayerApi player) {
        if (Networking.IsMaster) {
            TryRestoreFinaleMode();
        }
    }

    private void TryRestoreFinaleMode() {
        bool restored;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, KEY_SIMULTANEOUS_FINALE, out restored)) {
            if (gameManager.simultaneousFinale != restored) {
                gameManager.simultaneousFinale = restored;
                gameManager.RequestSerialization();
            }
        }
        // TryGetBool == false の場合は何もしない → Inspector 既定値(A モード)継続
    }
}
```

##### Master 昇格時の追加フック(重要)

`OnPlayerRestored` は **新規入室時にのみ発火** する。インスタンス参加中に Master が交代しても再発火しない。そのため、Master 昇格時の復元は `OnPlayerLeft` で `Networking.IsMaster` を再判定し、新 Master になっていれば `TryGetBool` で自分のキャッシュ済 PlayerData から復元する(自分の Restored は入室時に既に完了済 → ローカルキャッシュに保持されている)。

##### 永続化の動作仕様

| シナリオ | 挙動 |
|---|---|
| 同じ人が再度 Master として入場 | 自分の OnPlayerRestored で復元 → 前回値が反映される |
| 違う人が Master として入場(Persistence 履歴なし) | TryGetBool == false → Inspector 既定値(A モード)継続 |
| 違う人が Master として入場(別ワールドで履歴あり別ワールド由来) | キー名がワールド固有のため復元されない |
| Master が交代(走行中・Idle 中問わず) | 新 Master の OnPlayerLeft 経由復元、Persistence 値があれば適用 |
| 初回入場(誰もキー未保存) | Inspector 既定値(A モード) |

##### v1.0 スコープでの実装範囲

- Phase 5 (5/26): UI トグル追加、`PlayerData.SetBool` / `OnPlayerRestored` / `OnPlayerLeft` 実装
- Phase 6 (5/27): Master 交代テストと同居して動作確認(同じ人再入場 / 別人 Master / 走行中 Master 交代の 3 ケース)
- 工数増 +0.5 日(Phase 5 内で吸収可)

##### Phase 5 着手時の最終確認事項

- VRChat SDK のバージョンが **3.7.4 以降**(現行は 3.10.3、Persistence 利用可能)
- `TryGetBool` の `out` 引数の正式形式を Unity Inspector のメソッド補完で再確認(本 ADR の擬似コードは [VRChat 公式ドキュメント](https://creators.vrchat.com/worlds/udon/persistence/player-data/) 2026-05-18 確認時点の JumpCounter サンプル + Try-pattern 推定)
- 公式 JumpCounter サンプルでは `OnPlayerDataUpdated` で値変更を受信しているが、本設計では Master のみが書く + Master のみが読む方針のため `OnPlayerRestored` + `OnPlayerLeft` の 2 フックで十分(他クライアントは UdonSynced 経由で `gameManager.simultaneousFinale` を受け取る)

### 8. 演出発火位置(プレイヤー目線)

爆発・紙吹雪のエフェクト Prefab は **`Prize_X` の中央(部屋内)に配置** する。プレイヤーは `TeleportTarget` で部屋内にテレポート → そこで演出が発火するため、**プレイヤーは演出の中に立つ** 体験になる(2026-05-18 ユーザー確定)。

- **爆発(ハズレ)**: プレイヤーが部屋にテレポート → 爆発の中に巻き込まれる → ドリフのコント落ちと同質の体験
- **紙吹雪(祝砲)**: プレイヤーが部屋にテレポート → 頭上から紙片が降りかかる → 祝祭感

観戦者は MainFloor 中央(Z=-30 付近)から見るため、演出の上部(高さ 4 m を超える部分)が部屋の壁を越えて視認できる(§3 で粒子の上昇高さを 6〜10 m に設計している根拠)。

## Consequences

### Positive

- **同期コスト 0**: UdonSynced 変数追加なし、既存の `seed` を派生利用するのみ
- **Late Joiner 整合**: seed さえ届けば全クライアントが同じ演出配置を計算
- **SPEC §12 と衝突しない**: テレポート先固定の原則を維持
- **v1.1 スケール対応容易**: `explosionCount` / `confettiCount` を Inspector で変えるだけ。20 レーン化時も配置アルゴリズム不変
- **2 モード対応**: 体験の好みに応じて運用切替可能。コードパスは分岐 2 箇所のみ
- **「味気なさ」の解消**: ハズレ/アタリのコントラストで、ゴール後にもう一段のサプライズが乗る

### Negative

- **黒煙が Quest 制約と相性悪い**: VRChat Mobile Particle 系は Additive 中心で、加算合成では黒が出ない。
  - 対策: 爆発はオレンジ→赤→暗赤グラデーション火球 + 灰白色(明るい灰)の煙 で代替。完全な「煤」感は出ないが、ドリフ的なコント感は維持可能
  - Mobile Particles/Multiply で煤の暗さを限定的に出す案もあり、Phase 4〜8 で実機検証
- **粒子数の Quest 実機調整が必要**: 同時最大 1 ゴールあたり粒子 < 80 を目安に、Phase 8 で FPS 影響を測定
- **Phase 5 ステートマシンに UI フェーズを追加**: `FinaleCountdown_UIフェーズ` を `Running` 内部状態として実装。ステート列挙には追加しない(同期影響なし)
- **演出 Prefab 制作工数 +0.5〜1 日**: パーティクル + SE を 2 種制作。5/19-20 のバッファ日に前倒し実施

### スケジュール影響

| 期間 | 作業 |
|---|---|
| 5/19-20 | パーティクル Prefab(爆発・紙吹雪)+ 共通 SE 制作 |
| Phase 4 (5/24-25) | `ComputeEffectAssignment` + `PrizeArea.PlayEffect` 配線 + A/B モード分岐 |
| Phase 5 (5/26) | `FinaleCountdown` UI を Countdown UI と統合(既存 3-2-1 表示の再利用) |
| Phase 8 (5/29) | Quest 実機で粒子数・音量・見映え調整 |

工数増は **+1 日(主にパーティクル制作)**、バッファ内に収まる。

## 関連 ADR

- [ADR-0002](./0002-deterministic-rng-seed-sync.md): seed 同期によるあみだくじ生成 — 本 ADR の派生 RNG (`seed ^ 0xBEEF`) が同一 seed から独立した分布を生成する根拠
- [ADR-0005](./0005-minimal-sync-variables.md): 同期変数の最小化 — 本 ADR は UdonSynced を追加しないことで方針継続
- [ADR-0008](./0008-4lane-scope-scalable-design.md): 4 レーン固定 + 可変サイズ対応 — `explosionCount` / `confettiCount` の Inspector 化はこの方針と整合
- [ADR-0010](./0010-android-in-v1.0-scope.md): Android 対応 — 透明度マテリアル制約と粒子バジェット制約を本 ADR で踏襲

## 改訂履歴

- 2026-05-18: 制定(v1.0 ゴール演出の追加スコープを確定)
- 2026-05-18: §3 に「A モード既定 + B モード切替 UI」確定を追記。§7 を新設し B モード切替 UI と Player Persistence による永続化仕様を定義。§8 に演出発火位置(プレイヤーは演出の中に立つ)を確定として追記
- 2026-05-18: §7 の Player Persistence セクションを VRChat 公式ドキュメント確認結果で実 API に揃えた。`PlayerData.SetBool / GetBool / TryGetBool` のシグネチャ、`OnPlayerRestored` の発火タイミング(新規入室時のみ)、Master 昇格時の追加フック(`OnPlayerLeft` で再判定)を明示。SDK 3.7.4 以降が前提
- 2026-05-21: §4 に「演出割当と TeleportTo は Cart 起点 lane ではなく終点 lane (goalLane) ベースで行う」を明記。Phase 4 Stage A で起点 lane ベース実装→終点 lane ベースに修正済み
