# Copilot Instructions — VRChat Amidakuji World

## プロジェクト概要

巨大あみだくじをテーマにした VRChat ワールド。プレイヤーはカートに乗ってランダム生成されたあみだくじを自動巡回し、ゴールの賞品エリアにテレポートする。

## ビルドターゲット

**PC (Windows) / Android (Quest) / iOS** の 3 プラットフォームに対応済み・公開済み。

- `ProjectSettings.asset` に iOS 関連の設定(iOSSupport, iPhone エントリ等)が含まれているのは意図的。ADR-0010 の記述は「v1.0 初期スコープ」の経緯メモであり、現在は iOS も対応済み
- iOS Graphics API: Metal を使用、`m_Automatic: 1`(Unity 自動選択)が正しい状態

## 技術スタック

- Unity 2022.3 LTS / VRChat World SDK 3.x / UdonSharp
- ClientSim (ローカルテスト)
- Android Build Support (Quest) / iOS Build Support

## コーディング制約 (UdonSharp)

- `async/await` / `IEnumerator` 不可 → `SendCustomEventDelayedSeconds()` で代替
- ジェネリック (`List<T>` 等) 不可 → 固定長配列
- 時刻差は `Networking.CalculateServerDeltaTime()` を使用
- `int` が必要な時刻値は `Networking.GetServerTimeInMilliseconds()`
- `(int)long` の明示縮小キャストは実行時例外になる

## Android (Quest) 制約

- GPU Instancing: 全マテリアルで必須
- テクスチャ上限: 1024×1024
- 透明マテリアル・Mirror・VideoPlayer 禁止

## ドキュメント

詳細仕様は `docs/SPEC.md`、設計判断は `docs/adr/`、既知の落とし穴は `docs/ui-pitfalls.md` を参照。
