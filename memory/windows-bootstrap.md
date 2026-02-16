# Windows Claude Code 起動手順書

このファイルはWindows環境のClaude Codeが最初に読むべきブートストラップガイドです。

---

## Step 0: このファイルを読んだら

以下の順番で作業を進めてください。各ステップで「完了」をユーザーに報告してから次に進むこと。

---

## Step 1: リポジトリの状態確認

### 読むべきファイル（この順番で）
1. **`CLAUDE.md`**（ルート） — プロジェクト全体の仕様・ルール・現在の開発状態
2. **`memory/index.yaml`** — マイルストーン進捗。M004.5まで完了済み、次はM005
3. **`memory/handoff-m005-windows.md`** — M005の詳細実装仕様（最重要）

### ビルド確認コマンド
```powershell
dotnet restore VRNotify.sln
dotnet build VRNotify.sln
```
- 全9プロジェクトがビルド成功すること（Linux環境ではDesktopがスキップされていたが、Windowsでは全て通る）

### テスト確認コマンド
```powershell
dotnet test tests\VRNotify.Domain.Tests\
```
- 83テスト全PASSすること

---

## Step 2: 既存スタブの確認

M005で実装するスタブファイルを読んで現状を把握する:

1. **`src/VRNotify.Overlay/Rendering/SkiaNotificationRenderer.cs`**
   - 現状: `RenderCard` メソッドが `throw new NotImplementedException()`
   - → SkiaSharpで512x128 RGBAテクスチャ描画を実装する

2. **`src/VRNotify.Overlay/OpenVR/OpenVrOverlayManager.cs`**
   - 現状: 全メソッドが `throw new NotImplementedException()`
   - → OVRSharp 1.2.0 を使ってOpenVRオーバーレイの初期化・テクスチャ表示を実装する

3. **`src/VRNotify.Overlay/VRNotify.Overlay.csproj`**
   - OVRSharp `Version="1.*"` と SkiaSharp `Version="2.*"` が既に参照済み

4. **ドメインモデル（テスト作成に使った型）**
   - `src/VRNotify.Domain/NotificationProcessing/NotificationCard.cs`
   - `src/VRNotify.Domain/NotificationProcessing/Priority.cs`
   - `src/VRNotify.Domain/SourceConnection/SourceType.cs`
   - `src/VRNotify.Domain/VRDisplay/DisplaySlot.cs`
   - `src/VRNotify.Domain/VRDisplay/IOverlayRenderer.cs`
   - `src/VRNotify.Domain/VRDisplay/IOverlayManager.cs`

---

## Step 3: M005 実装（5ファイル）

### 3-1. プロトタイププロジェクト新規作成

**ディレクトリ作成**: `tests\VRNotify.Overlay.Prototype\`

**ファイル作成**: `tests\VRNotify.Overlay.Prototype\VRNotify.Overlay.Prototype.csproj`
- OutputType: Exe
- TargetFramework: net8.0-windows
- RuntimeIdentifier: win-x64
- ProjectReference: `..\..\src\VRNotify.Overlay\VRNotify.Overlay.csproj` と `..\..\src\VRNotify.Domain\VRNotify.Domain.csproj`

**ソリューションに追加**:
```powershell
dotnet sln VRNotify.sln add tests\VRNotify.Overlay.Prototype\VRNotify.Overlay.Prototype.csproj
```

### 3-2. SkiaNotificationRenderer 実装

**編集**: `src\VRNotify.Overlay\Rendering\SkiaNotificationRenderer.cs`

仕様は `memory/handoff-m005-windows.md` のセクション2を参照。要点:
- 512x128 RGBA、半透明ダーク背景 `#1a1a2eCC`
- Discord=`#5865F2` / Slack=`#611F69` の左ボーダー
- 送信者名16px白太字 + 本文14px灰色
- 優先度インジケータ（High=赤丸、Medium=黄丸）

### 3-3. OpenVrOverlayManager 実装

**編集**: `src\VRNotify.Overlay\OpenVR\OpenVrOverlayManager.cs`

仕様は `memory/handoff-m005-windows.md` のセクション3を参照。要点:
- `OVRSharp.Application(ApplicationType.Overlay)` で初期化
- `OVRSharp.Overlay("vrnotify.main", "VRNotify")` でオーバーレイ作成
- 幅0.3m、HMD相対位置（Z=-1.2, Y=0.15）
- RGBA byte[] をテクスチャとして設定

### 3-4. Program.cs 作成

**ファイル作成**: `tests\VRNotify.Overlay.Prototype\Program.cs`

仕様は `memory/handoff-m005-windows.md` のセクション4を参照。要点:
- コンソールにステップ表示
- テスト用NotificationCard作成（Discord, High, "TestUser", "#general"）
- オーバーレイ初期化→表示→Enter待ち→終了

### 3-5. ビルド確認
```powershell
dotnet build VRNotify.sln
```

---

## Step 4: 実行テスト

### 前提条件
- SteamVRが起動していること
- VRヘッドセットが接続されていること

### 実行
```powershell
dotnet run --project tests\VRNotify.Overlay.Prototype\
```

### 確認事項
1. コンソールに初期化ステップが表示される
2. VRヘッドセット内で視界上方に通知カードが出現
3. テキストが読める
4. HMD追従が正常
5. Enter入力でクリーン終了

---

## Step 5: 完了処理

### 進捗記録
`memory/index.yaml` のM005ステータスを `completed` に更新。
`memory/sessions/` に作業ログYAMLを作成。

### コミット＆プッシュ
```powershell
git add -A
git commit -m "feat: M005 OpenVR overlay prototype - render notification card in VR"
git push origin main
```

---

## 重要な注意事項

### NuGetパッケージ
- **OVRSharp**: 最新は `1.2.0`。`4.*` は存在しない。
- **SkiaSharp**: `2.*` で指定済み。

### パス区切り文字
- Windows環境なのでパスは `\`（バックスラッシュ）
- ただし .csproj 内のProjectReferenceは `/` でも `\` でも動く

### SteamVR未起動時
- OpenVR初期化がタイムアウトする。必ずSteamVRを先に起動する。
- ユーザーに「SteamVRを起動してVRヘッドセットを接続してから実行してください」と伝える。

### OVRSharp API の調査
- OVRSharp 1.2.0 のAPIが不明な場合、NuGet復元後に以下を確認:
  - `~/.nuget/packages/ovrsharp/` 配下のDLLをILSpyやdotnet-decompileで確認
  - または GitHub: https://github.com/OVRTools/OVRSharp のソースを参照
- `OVRSharp.Application` と `OVRSharp.Overlay` が主要クラス
