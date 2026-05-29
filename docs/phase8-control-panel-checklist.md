# Phase 8: 操作パネル(着座者用 START / MODE)配置チェックリスト

着座すると視点が真横までしか回らず、中央の StartButton に正対できない問題([BACKLOG.md §課題](../BACKLOG.md))の根本解決。
**各 Cart 着座者の正面 + 中央**に START / MODE 操作パネルを配置し、Master が目の前で操作できるようにする。

## 0. 方針(確定)

- 着座者は走行方向(−Z)を向く。その**正面に操作パネル**を置けば、視点を動かさず Use できる。
- パネルは **RUNNING 中のみ非表示**、IDLE / RESULT_DISPLAY で表示(START 押下で消え、結果画面と同時に再表示 → 戻ったプレイヤーが次レースを開始できる)。
- 操作できるのは **Master だけ**(StartButton.cs / FinaleModeToggle.cs の既存 Master ガードを流用)。非 Master には見えるが押下 no-op。
- 「画面」= Collider 無しの背景板、**ボタン部分だけ IsTrigger Collider**(歩行者すり抜けと両立)。

## 1. スクリプト(✅ 完了済み)

- `GameManager` に `public GameObject[] controlPanels;` 追加。
- `_ApplyState()` で `bool show = (gameState != STATE_RUNNING)` を全 `controlPanels` に `SetActive`。
  冪等化(`_appliedState`)済みなので gameState 変化時のみ切替。
- **StartButton.cs / FinaleModeToggle.cs は状態がインスタンス毎**なので、5 個配置しても各々独立に動く(新規スクリプト不要)。
- **レース後の自動リセット(Option B)**: `_EnterResultDisplay()` が `resultHoldSeconds`(既定 10 秒)後に
  `_ReturnToIdle()` を予約。IDLE 遷移で Cart 起点復帰・着座枠クリア・賞品エリア滞在者を起点へテレポート。
  **結果 UI は IDLE で隠さず**(`_TeleportLocalToSpawnIfInPrize()` ヘルパに集約)、次の START(RUNNING)で初めて隠す。
  - ⚠ 既知の制約: 結果表示中(10 秒)に **Master が退出**すると `_ReturnToIdle` 予約が失われ RESULT_DISPLAY に留まる
    (Master 移譲先は再予約しない)。レアケースとして許容、緊急時は `_ReturnToIdle` を手動発火。

→ シーン側の配置・配線(§2〜§4)のみ未実施。

## 2. パネル構成

```text
ControlPanel(空 GameObject ← これを controlPanels に登録)
├ Visual          背景板(任意、Collider 無し)
├ StartButton     Cube + IsTrigger Collider + StartButton.cs
└ ModeButton      Cube + IsTrigger Collider + FinaleModeToggle.cs
```

既存の `EntryArea/StartButton` と `SimultaneousFinaleToggle` を中央パネルの素材に流用する。

## 3. 配置手順(Unity Editor)

### 3.1 ControlPanel を組んで Prefab 化

1. `EntryArea` に空 GameObject `ControlPanel` を作成、Position `(0, 0, 0)`
2. 既存 `StartButton` と `SimultaneousFinaleToggle` をその子に移動、見やすい相対位置に(例: START 上 `localY +0.3` / MODE 下 `localY -0.3`)
3. (任意)背景板 Visual を追加(Collider は削除 or 付けない)
4. `Assets/_Project/Prefabs/` 配下へドラッグして **Prefab 化**

> Prefab 化のメリット: ボタンのレイアウト・サイズ・材質・スクリプトを **1 箇所(Prefab 編集)で全インスタンスに反映**できる。v1.1 のカスタマイズ拡張にも有利。複製方式だと 5 箇所を個別に手直しすることになる。

### 3.2 Prefab を 5 箇所に配置

Prefab を Hierarchy へ **5 個ドラッグ**。`ControlPanel_Center` / `Cart0`〜`Cart3` にリネームし、下表の位置へ:

| パネル | Position X | Position Y | Position Z | 備考 |
| --- | --- | --- | --- | --- |
| ControlPanel_Center | 0 | 1.2 | 2 | 既存位置(立って操作 / 観戦者向け) |
| ControlPanel_Cart0 | -6 | 1.3 | 0.5 | Cart_0 着座者の正面 |
| ControlPanel_Cart1 | -2 | 1.3 | 0.5 | Cart_1 |
| ControlPanel_Cart2 | +2 | 1.3 | 0.5 | Cart_2 |
| ControlPanel_Cart3 | +6 | 1.3 | 0.5 | Cart_3 |

> 座標は `EntryArea` がローカル原点(World 0 付近)である前提の目安。**Scene ビューで各 Cart の着座位置の前に来るか・着座者(+Z 側)から正対するか**を現物確認して微調整。

### 3.3 外部参照の配線(Prefab の弱点をカバー)

**`gameManager` / `finaleModeManager` はシーン上のオブジェクト参照で、Prefab Asset には保存できない**(Prefab はシーンを参照不可)。配置後に各インスタンスで配線が要るが、**複数選択して Inspector で一度にドラッグ**すれば 1 回で済む:

1. 5 パネル配下の **StartButton を Ctrl/Shift で複数選択** → Inspector で `gameManager` に GameManager をドラッグ(全選択に適用)
2. 同様に **MODE ボタン 5 つを複数選択** → `finaleModeManager` / `gameManager` を一括配線
3. 各 Cart 前の **Proximity = 3**(複数選択で一括)。中央のみ 10 維持で立ち操作対応

> `buttonRenderer`(Prefab 内部の子参照)・material(`M_StartButton_*` / `M_FinaleToggle_*` のアセット参照)は **Prefab に保持される**ので再配線不要。

### 3.4 ボタン表面ラベル(状態 / モード可視化)

マテリアル色替えだけでは状態が分かりづらいので、表面に **3D TextMeshPro** を貼って文字でも示す。
**Prefab 編集モードで行えば 5 インスタンス全部に反映**される(`labelText` は Prefab 内部参照なので保持される)。

> ⚠ `StartButton` は Transform Scale Y が **負(-0.5)**。子に文字を付けると上下反転するので、
> **ラベルは `ControlPanel` 直下に置く**(Scale 1/1/0.02 でクリーン、平面文字なので Z 潰れは無害)。

1. Prefab を開き、`ControlPanel` 直下に `GameObject > 3D Object > Text - TextMeshPro` を 2 つ作成
   → `StartLabel` / `ModeLabel` にリネーム
2. **Font Asset** はプロジェクト既定(`Empty SDF for Default Font` + NotoSansJP フォールバック)を割当
   → 既存 Canvas TMP と**同じフォントマテリアルを共有**するので Android マテリアル枠は増えない([ui-pitfalls.md §2](./ui-pitfalls.md))。`一斉` / `個別` の JP もフォールバックで描画可
3. **Alignment** = Center / Middle、適切な Font Size、Rect 幅高さをボタン面に合わせる
4. 各ラベルを対応ボタンの**面の手前(+Z 側)**へ配置。Scene ビューでボタン正面に浮くよう微調整
   → 文字が鏡像になっていたら Rotation Y=180(パネルを回転させた Cart 用は子なので追従する)
5. 配線(Prefab 内参照なので 1 回で全インスタンス反映):
   - `StartButton.labelText` ← `StartLabel`
   - `SimultaneousFinaleToggle.labelText` ← `ModeLabel`
6. 文言は Inspector で調整可:
   - START: `Label Enabled` = `START` / `Label Disabled` = 押せない時の文言(例 `待機中`、既定は `START`)
   - MODE: `Label Mode A` = `MODE A\n一斉` / `Label Mode B` = `MODE B\n個別`(既定値)

> START ラベルは押下可否(`_IsPressable`)に連動。MODE ラベルは**押下可否と独立に現在モードを常時表示**するので、非 Master でも今どちらのモードかが分かる。

## 4. GameManager 配線

`_Managers/GameManager` の `Control Panels`(Size 5)に:
`ControlPanel_Center` / `ControlPanel_Cart0` / `Cart1` / `Cart2` / `Cart3` をドラッグ。

## 5. 検証(ClientSim)

- [ ] IDLE で 5 パネルすべて表示される(表示されない場合は **GameManager の Control Panels に 5 つ登録されているか**を真っ先に疑う)
- [ ] **Cart_0(一番遠い)に着座 → 視点を動かさず正面の START を Use できる**(これが本丸)
- [ ] START 押下 → RUNNING で 5 パネルすべて消える(消えない = Control Panels 未配線 or RUNNING 未到達。Console の `[GameManager] state=Running` で切り分け)
- [ ] ゴール → 結果(RESULT_DISPLAY)表示と同時にパネル再表示
- [ ] **結果表示から `resultHoldSeconds`(既定 10 秒)後に自動リセット**: Cart が起点へ戻る / 賞品エリアの自分が起点へテレポート / 参加枠クリア。**結果 UI とパネルは出したまま**
- [ ] リセット後に座り直し → 次の START でレース再開(START は IDLE 復帰後に押下可になる)
- [ ] MODE ボタンで A/B 切替できる(Master のみ)、非 Master は no-op
- [ ] **START ラベルが押下可否で切替わる**(参加者0で `待機中` 等)/ **MODE ラベルが A↔B で切替わる**(文字が鏡像でない)
- [ ] 走行中にパネルが残らない / 視界を邪魔しない

## 6. 完了基準

着座した Master が視点を動かさず正面の START を押してレースを開始でき、RUNNING 中はパネルが消え、結果後に再表示される。中央パネルでも同操作が可能。
