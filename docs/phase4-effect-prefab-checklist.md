# Phase 4 ゴール演出 Prefab チェックリスト

ゴール演出(爆発・紙吹雪)の Prefab を Unity GUI 上で組み立てるための確定値リスト。
判断根拠は [ADR-0012](./adr/0012-goal-effect-randomized.md)、仕様は [SPEC.md §8.3](./SPEC.md#83-ゴール演出)、
マテリアル定義は [material-set.md §2.3 / §7](./material-set.md) を参照。

**着手タイミング**: 5/19-5/20(Phase 3 着手前のバッファ日に先行制作)
**前提**: マテリアル `M_FX_Explosion_Fireball` / `M_FX_Explosion_Smoke` / `M_FX_Confetti` を [material-set.md §7](./material-set.md#7-phase-1-運用方針プレースホルダ色のみ) に従って作成済み。

---

## 0. 共通ルール

- 全 Prefab は `Assets/_Project/Prefabs/Effects/` 配下に保存(`Effects/` フォルダを新規作成)
- AudioClip(爆発音・紙吹雪音・フィナーレ共通音)は **Phase 4 着手時点では未設定**(AudioSource を空クリップでアタッチ、Phase 8 で差し替え)。差し替え手順は §6 参照
- ParticleSystem のテクスチャは **Unity 既定 `Default-Particle`(白い丸い softdot)を使用**(マテリアルの Albedo に明示的に割り当てる)。専用テクスチャ差し替えは Phase 8 で検討
- 単位: 全てメートル (m) / 秒 (s)
- Y=0 が床上面(賞品エリア内では Y=0 が床、ParticleSystem は Y=0.5〜1.0 から発火)

---

## 1. ExplosionEffect.prefab

### 1.1 Prefab 内部構造

```
ExplosionEffect (Root, Empty GameObject, Layer: Default, 既定 inactive)
├── Fireball (ParticleSystem, Material: M_FX_Explosion_Fireball)
├── Smoke (ParticleSystem, Material: M_FX_Explosion_Smoke)
└── AudioSource (Component on Root)
```

Prefab Root は **配置時 inactive** で保存(`SetActive(false)`)。`PrizeArea.PlayEffect()` で active 化 + `ParticleSystem.Play()` を呼ぶ。

### 1.2 Fireball(火球)ParticleSystem 設定

| モジュール | パラメータ | 値 |
|---|---|---|
| **Main** | Duration | 1.5 |
| | Looping | OFF |
| | Start Lifetime | 1.2 |
| | Start Speed | 6.0 |
| | Start Size | 0.8〜1.5(Random Between Two Constants) |
| | Start Color | `#FFFF00`〜`#FF6600`(Random Between Two Colors) |
| | Gravity Modifier | -0.3(上方向に浮く) |
| | Simulation Space | Local |
| | Max Particles | 40 |
| **Emission** | Rate over Time | 0 |
| | Bursts | Time=0.0, Count=35, Cycles=1 |
| **Shape** | Shape | Sphere |
| | Radius | 0.3 |
| | Position(ローカル) | (0, 0.8, 0) |
| **Color over Lifetime** | Gradient Color Keys | `#FFFFAA` (0%) → `#FF6600` (40%) → `#AA1100` (100%) |
| | Alpha Keys | 1.0 (0%) → 1.0 (60%) → 0.0 (100%) |
| **Size over Lifetime** | Curve | 0.3 (0%) → 1.0 (40%) → 1.2 (100%)(膨張) |
| **Velocity over Lifetime** | Linear Y | 1.0(上向き持続) |
| **Renderer** | Material | `M_FX_Explosion_Fireball` |
| | Render Mode | Billboard |
| | Sort Mode | None |

**期待される見た目**: 黄→オレンジ→暗赤に変化する火球が地面(Y=0.8 中心)から上方向に 4〜6 m 立ち上がり、膨張しながら 1.5 秒で消える。

### 1.3 Smoke(煙)ParticleSystem 設定

| モジュール | パラメータ | 値 |
|---|---|---|
| **Main** | Duration | 3.0 |
| | Looping | OFF |
| | Start Delay | 0.2(火球より少し遅れて噴出) |
| | Start Lifetime | 2.5 |
| | Start Speed | 2.0 |
| | Start Size | 1.0〜2.0(Random Between Two Constants) |
| | Start Color | `#DDDDDD` |
| | Gravity Modifier | -0.5(上昇を加速) |
| | Simulation Space | World(風に流される感覚を出すならローカルでも可) |
| | Max Particles | 30 |
| **Emission** | Rate over Time | 0 |
| | Bursts | Time=0.0, Count=20, Cycles=1 |
| **Shape** | Shape | Cone |
| | Angle | 25 |
| | Radius | 0.5 |
| | Position(ローカル) | (0, 0.5, 0) |
| | Rotation(ローカル) | (-90, 0, 0)(上向き) |
| **Color over Lifetime** | Alpha Keys | 0.0 (0%) → 0.8 (20%) → 0.0 (100%) |
| **Size over Lifetime** | Curve | 0.5 (0%) → 1.5 (100%)(膨張しながら消える) |
| **Renderer** | Material | `M_FX_Explosion_Smoke` |
| | Render Mode | Billboard |

**期待される見た目**: 火球の 0.2 秒後に灰白色の煙が上方向に 6〜8 m 立ち上がり、ゆっくり拡散しながら 3 秒で消える。Multiply 合成により背景がやや暗くなり、煤感を演出。

### 1.4 AudioSource 設定(Root にアタッチ)

| パラメータ | 値 |
|---|---|
| AudioClip | **未設定**(Phase 8 で爆発 SE をアサイン) |
| Play On Awake | **OFF**(`PlayEffect()` から PlayOneShot で再生) |
| Loop | OFF |
| Volume | 0.8 |
| Spatial Blend | **1.0**(完全 3D) |
| Min Distance | 3 |
| Max Distance | 40(MainFloor 観戦域 Z=-30 付近まで聴こえる距離感) |
| Rolloff Mode | Logarithmic |
| Doppler Level | 0 |

---

## 2. ConfettiEffect.prefab

### 2.1 Prefab 内部構造

```
ConfettiEffect (Root, Empty GameObject, Layer: Default, 既定 inactive)
├── Confetti (ParticleSystem, Material: M_FX_Confetti)
└── AudioSource (Component on Root)
```

### 2.2 Confetti(紙片)ParticleSystem 設定

| モジュール | パラメータ | 値 |
|---|---|---|
| **Main** | Duration | 0.5 |
| | Looping | OFF |
| | Start Lifetime | 3.0 |
| | Start Speed | 8.0(上方向に強く噴射) |
| | Start Size | 0.15〜0.3(Random Between Two Constants) |
| | Start Color | (Random Between Two Colors または Random Color from Gradient) `#FF3333 / #FFCC00 / #33CC33 / #3399FF / #FF66CC` のいずれか |
| | Start Rotation | 0〜360(Random Between Two Constants、紙片の向きをランダム化) |
| | Gravity Modifier | 1.0(自然落下) |
| | Simulation Space | World |
| | Max Particles | 150 |
| **Emission** | Rate over Time | 0 |
| | Bursts | Time=0.0, Count=120, Cycles=1 |
| **Shape** | Shape | Cone |
| | Angle | 35(横拡散を大きめに) |
| | Radius | 0.3 |
| | Position(ローカル) | (0, 0.5, 0) |
| | Rotation(ローカル) | (-90, 0, 0)(上向き噴射) |
| **Color over Lifetime** | Alpha Keys | 1.0 (0%) → 1.0 (80%) → 0.0 (100%) |
| **Rotation over Lifetime** | Angular Velocity | 90〜360 deg/s(Random Between Two Constants、紙片がくるくる回る) |
| **Velocity over Lifetime** | Linear X / Z | -1.0〜+1.0(Random Between Two Constants、横揺れ) |
| **Renderer** | Material | `M_FX_Confetti` |
| | Render Mode | Billboard(または Stretched Billboard で短冊感を出す代替案あり) |

**期待される見た目**: 5 色の紙片が 8 m/s で噴出 → 上方向 10 m 程度まで上昇 → 重力で 3 秒かけて落下しながら横にも舞う。

### 2.3 AudioSource 設定(Root にアタッチ)

| パラメータ | 値 |
|---|---|
| AudioClip | **未設定**(Phase 8 で紙吹雪 SE をアサイン) |
| Play On Awake | OFF |
| Loop | OFF |
| Volume | 0.7 |
| Spatial Blend | 1.0 |
| Min Distance | 3 |
| Max Distance | 40 |
| Rolloff Mode | Logarithmic |

---

## 3. PrizeArea Prefab への組み込み

既存の `PrizeArea.prefab`([phase1-prefab-checklist.md §7](./phase1-prefab-checklist.md#7-prizeareaprefab賞品エリア))に演出をアタッチする。

### 3.1 変更後の内部構造

```
Prize_X (Root, Rotation Y=180、既存維持)
├── Walls/             ← 既存維持
├── Ceiling            ← 既存維持
├── TeleportTarget     ← 既存維持
├── ExplosionEffect    ← Phase 4 で追加(ExplosionEffect.prefab をネスト Prefab として配置)
└── ConfettiEffect     ← Phase 4 で追加(ConfettiEffect.prefab をネスト Prefab として配置)
```

| 追加配置 | Position(ローカル) | 備考 |
|---|---|---|
| `ExplosionEffect` | (0, 0, 0) | 部屋中央床面、SetActive(false) 既定 |
| `ConfettiEffect` | (0, 0, 0) | 同上、SetActive(false) 既定 |

### 3.2 PrizeArea スクリプト(Phase 4 本実装、ここでは構造だけ確定)

`PrizeArea.cs`(UdonSharp)を Prize_X Root にアタッチ。

| Inspector フィールド | 型 | 用途 |
|---|---|---|
| `teleportTarget` | Transform | 既存 TeleportTarget の Transform |
| `explosionEffect` | GameObject | 子の ExplosionEffect |
| `confettiEffect` | GameObject | 子の ConfettiEffect |
| `explosionAudio` | AudioSource | ExplosionEffect 上の AudioSource(個別 SE 再生用) |
| `confettiAudio` | AudioSource | ConfettiEffect 上の AudioSource(個別 SE 再生用) |

API(擬似コード):

```csharp
public void PlayEffect(int kind, bool withIndividualSound) {
    if (kind == 1) {       // 爆発
        explosionEffect.SetActive(true);
        // ParticleSystem.Play は SetActive で自動再生(Main.Play On Awake は OFF だが、
        // SetActive(true) + Main 内 Start Action が Play なら発火する。
        // 確実性のため明示的に GetComponentInChildren<ParticleSystem>().Play() を呼ぶ実装でも可)
        if (withIndividualSound) explosionAudio.Play();
    } else if (kind == 2) { // 紙吹雪
        confettiEffect.SetActive(true);
        if (withIndividualSound) confettiAudio.Play();
    }
    // kind == 0 は何もしない
}
```

---

## 4. GameManager 直下: FinaleSharedAudio(A モード共通 SE)

`_Managers/GameManager` GameObject の子に AudioSource を 1 個追加。

```
_Managers/
└── GameManager
    └── FinaleSharedAudio (AudioSource, Component)
```

| パラメータ | 値 |
|---|---|
| AudioClip | **未設定**(Phase 8 で「パパーン」共通音をアサイン) |
| Play On Awake | OFF |
| Loop | OFF |
| Volume | 1.0 |
| Spatial Blend | **0.0**(2D、全員に同じ音量で聴かせる) |

GameManager の `_FireFinale()` で `finaleSharedAudio.Play()` を呼ぶ。

---

## 5. ClientSim での見映え確認

Prefab 単体テストとして、以下の手順で動作確認:

1. テスト用シーンを新規作成 or 現行シーンの空きエリアに ExplosionEffect / ConfettiEffect を配置
2. ClientSim 起動、Play Mode で該当 GameObject を `SetActive(true)` する
3. 観戦距離(賞品エリア相当の Z=-64 から MainFloor 中央 Z=-30 = 距離 34 m)に視点を置き、演出の高さ・派手さが十分か確認
4. 確認ポイント:
   - [ ] 火球が高さ 4〜6 m まで上がっているか
   - [ ] 煙が高さ 6〜8 m まで立ち上っているか(Multiply で背景が暗くなっているか)
   - [ ] 紙吹雪が高さ 10 m 程度まで上がり、横拡散 5〜6 m で舞っているか
   - [ ] 観戦距離 34 m から見て、賞品エリアの壁(高さ 4 m)を確実に超えて演出が視認できるか
   - [ ] FPS の落ち込みが目立たないか(PC 環境では問題なし、Quest は Phase 8 で本確認)

---

## 6. Phase 8 での差し替え方針(AudioClip + Texture)

### 6.1 AudioClip

調達先候補(全て商用利用可、要ライセンス確認):

- 効果音ラボ(<https://soundeffect-lab.info/>): 「爆発」「紙吹雪」「ファンファーレ」カテゴリに該当音多数。クレジット表記不要、商用可
- OtoLogic(<https://otologic.jp/>): CC BY 4.0、クレジット表記必要
- Pixabay Audio(<https://pixabay.com/sound-effects/>): Pixabay License、商用可

差し替え手順:

1. `Assets/_Project/Audio/SFX/` に `.wav` または `.mp3` を配置
2. Inspector で Force To Mono = OFF(空間音響のため Stereo 維持)、Compression = Vorbis、Quality = 70
3. ExplosionEffect / ConfettiEffect / FinaleSharedAudio の AudioSource.AudioClip スロットにドラッグ
4. Quest 実機で音量・距離減衰のバランスを確認、必要なら Max Distance を再調整

### 6.2 Texture(専用パーティクル)

差し替え手順:

1. `Assets/_Project/Textures/FX/` に `T_FX_Spark.png`(火花)、`T_FX_Confetti.png`(短冊紙片)などを配置(256×256 以下)
2. Generate Mipmaps = ON、Compression = High Quality(ETC2)
3. `M_FX_Explosion_Fireball` / `M_FX_Confetti` の `_MainTex` スロットにアサイン
4. ConfettiEffect の Renderer Render Mode を Stretched Billboard に変更すると短冊感が強調される

---

## 7. Phase 4 着手準備の完了基準

- [ ] `Assets/_Project/Materials/` に 13. 〜 15. の 3 マテリアルが作成済み([material-set.md §1 / §7](./material-set.md))
- [ ] `Assets/_Project/Prefabs/Effects/ExplosionEffect.prefab` が §1 通りの構造で作成済み
- [ ] `Assets/_Project/Prefabs/Effects/ConfettiEffect.prefab` が §2 通りの構造で作成済み
- [ ] ClientSim 上で両 Prefab が個別に `SetActive(true)` で発火する
- [ ] 既存 `PrizeArea.prefab` に Effect 2 種をネスト Prefab として組み込み済み(全 4 部屋)
- [ ] `_Managers/GameManager` 配下に `FinaleSharedAudio` (AudioSource, 2D, ClipはPhase 8) を配置済み
- [ ] §5 の見映え確認チェックを ClientSim で完了

完了したら、Phase 3 着手 → Phase 4 でロジック配線へ進む。

---

## 8. 改訂履歴

- 2026-05-18: 初版作成([ADR-0012](./adr/0012-goal-effect-randomized.md) 確定に伴い、Phase 3 着手前のバッファ日 5/19-5/20 で先行制作する Prefab 仕様を確定)
