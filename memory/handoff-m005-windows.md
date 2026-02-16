# M005 OpenVRオーバーレイ プロトタイプ - Windows実装ガイド

## このファイルについて
WSL2上での開発（M001〜M004.5）からWindows環境へ引き継ぐための詳細実装仕様書。
Windows上のClaude Codeがこのファイルを読んで、M005を自律的に実装できることを目指す。

---

## Phase B: Windows環境セットアップ（確認作業）

### 前提
- Windows 10/11、SteamVR インストール済み
- .NET 8 SDK インストール済み（`dotnet --version` → 8.0.x）
- Git for Windows

### ビルド確認手順
```powershell
dotnet restore VRNotify.sln
dotnet build VRNotify.sln
# 全9プロジェクトがビルド成功すること（Desktop(WPF)含む）

dotnet test tests/VRNotify.Domain.Tests/
# 83テスト全PASS
```

### SteamVR確認
- Steam → ライブラリ → ツール → SteamVR 起動
- VRヘッドセットのトラッキングが緑ステータスであること

---

## Phase C: M005 実装仕様

### ゴール
ハードコードした通知カードをSteamVRオーバーレイとしてVR内に表示する。

### 作成・変更するファイル一覧

| 操作 | ファイル | 内容 |
|------|---------|------|
| **新規** | `tests/VRNotify.Overlay.Prototype/VRNotify.Overlay.Prototype.csproj` | コンソールアプリ |
| **新規** | `tests/VRNotify.Overlay.Prototype/Program.cs` | エントリポイント |
| **編集** | `src/VRNotify.Overlay/Rendering/SkiaNotificationRenderer.cs` | スタブ→描画実装 |
| **編集** | `src/VRNotify.Overlay/OpenVR/OpenVrOverlayManager.cs` | スタブ→OVR実装 |
| **編集** | `VRNotify.sln` | プロトタイププロジェクト追加 |

---

### 1. プロトタイププロジェクト（新規）

**`tests/VRNotify.Overlay.Prototype/VRNotify.Overlay.Prototype.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\VRNotify.Overlay\VRNotify.Overlay.csproj" />
    <ProjectReference Include="..\..\src\VRNotify.Domain\VRNotify.Domain.csproj" />
  </ItemGroup>
</Project>
```
- `dotnet sln VRNotify.sln add tests/VRNotify.Overlay.Prototype/` でソリューションに追加

---

### 2. SkiaNotificationRenderer 実装

**ファイル**: `src/VRNotify.Overlay/Rendering/SkiaNotificationRenderer.cs`
**現状**: `throw new NotImplementedException();`

**実装仕様**:
- 512x128px RGBA テクスチャを生成
- 半透明ダーク背景: `#1a1a2eCC` (rgba)
- 角丸: 12px
- 左端にサービス別ボーダー（幅4px）:
  - Discord = `#5865F2` (DiscordブランドBlurple)
  - Slack = `#611F69` (Slackブランドパープル)
- テキストレイアウト:
  - 送信者名: 16px、白、太字、上部
  - 本文: 14px、`#cccccc`、下部
  - 右上に優先度インジケータ（High=赤丸、Medium=黄丸、Low=なし）
- `SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)` で生成
- `SKSurface.Create(info)` → Canvas描画 → `SKImage.FromPixels` → `SKData.ToArray()` でbyte[]返却

**SkiaSharp注意点**:
- Windows環境ではデフォルトフォントが利用可能
- `SKTypeface.FromFamilyName("Segoe UI")` を使用
- フォールバック: `SKTypeface.Default`

---

### 3. OpenVrOverlayManager 実装

**ファイル**: `src/VRNotify.Overlay/OpenVR/OpenVrOverlayManager.cs`
**現状**: 全メソッド `throw new NotImplementedException();`

**実装仕様**:

OVRSharp 1.2.0 の `OVRSharp.Overlay` クラスを使う。

```
OVRSharp名前空間: OVRSharp
主要クラス:
  - OVRSharp.Application : OpenVR初期化（コンストラクタでApplicationType指定）
  - OVRSharp.Overlay : オーバーレイ作成・テクスチャ設定・表示制御
```

**InitializeAsync**:
1. `new OVRSharp.Application(OVRSharp.ApplicationType.Overlay)` でOpenVR初期化
2. `new OVRSharp.Overlay("vrnotify.main", "VRNotify")` でオーバーレイ作成
3. 幅 0.3m に設定: `overlay.SetWidthInMeters(0.3f)`
4. `IsAvailable = true` に設定

**ShowNotificationAsync**:
1. レンダラーでテクスチャ生成（512x128）
2. byte[] をオーバーレイテクスチャとして設定
3. `overlay.Show()` で表示

**テクスチャ設定方法**:
OVRSharp の `Overlay.SetTextureRaw()` または OpenVR APIを直接使用:
```csharp
// OVRSharpの Overlay クラスは内部で IVROverlay を保持
// SetTexture に Texture_t を渡す
// RGBA byte[] → GCHandle.Alloc でピン止め → SetTextureRaw
```

**HMD相対位置設定**:
```csharp
// HMD追従型: TrackedDeviceRelative (device index 0 = HMD)
// 位置: 1.2m前方(Z=-1.2)、15°上方(Y方向オフセット)
// OpenVR座標系: 右手系、Y=上、Z=後方（HMDの前方はZ負方向）
var transform = new HmdMatrix34_t();
// 回転なし（単位行列） + 平行移動
transform.m0 = 1; transform.m5 = 1; transform.m10 = 1;
transform.m3 = 0;      // X: 中央
transform.m7 = 0.15f;  // Y: やや上
transform.m11 = -1.2f; // Z: 1.2m前方
```

**DisposeAsync**: `overlay.Destroy()` と `application.Shutdown()`

---

### 4. Program.cs（プロトタイプ エントリポイント）

**ファイル**: `tests/VRNotify.Overlay.Prototype/Program.cs`

```
処理フロー:
1. Console.WriteLine でステップ表示
2. SkiaNotificationRenderer を生成
3. OpenVrOverlayManager を生成
4. InitializeAsync() でOpenVR初期化
5. テスト用 NotificationCard を作成:
   - SourceType.Discord
   - Priority.High
   - Title: "#general"
   - Body: "Hello from VRNotify! This is a test notification."
   - SenderDisplay: "TestUser"
   - DisplayDuration: 10秒
6. DisplaySlot(0) を作成
7. ShowNotificationAsync(card, slot) で表示
8. "Press Enter to exit..." で待機
9. DisposeAsync() でクリーンアップ
```

---

### 5. 実行方法

```powershell
# SteamVRを起動してVRヘッドセットを装着してから
dotnet run --project tests/VRNotify.Overlay.Prototype/
```

### 期待する動作
1. コンソールに初期化ステップが表示される
2. VRヘッドセット内で視界上方に通知カードが出現
3. カードはHMDに追従（頭を動かすと一緒に移動）
4. Discordブルーのボーダー、送信者名「TestUser」、本文が読める
5. Enter入力でクリーンに終了

---

### トラブルシューティング

| 症状 | 原因 | 対処 |
|------|------|------|
| `OpenVR initialization timed out` | SteamVRが起動していない | SteamVRを先に起動する |
| `Could not load OpenVR DLL` | SteamVRインストール不正 | Steamからツール→SteamVR再インストール |
| オーバーレイ不可視 | Transform位置が悪い | Z=-0.8f(近く)、Y=0.1f(低く)に調整 |
| 黒いテクスチャ | フォント問題またはRGBA不整合 | テクスチャをPNGファイル保存してデバッグ |
| ビルドエラー: OVRSharp | NuGet復元失敗 | `dotnet restore` → OVRSharp 1.2.0 確認 |

---

### 検証チェックリスト
- [ ] プロトタイププロジェクトがビルド成功
- [ ] OpenVR初期化成功（コンソールに「Overlay initialized」表示）
- [ ] VR内に通知カードが表示される
- [ ] テキストが読める（512x128解像度で十分か確認）
- [ ] HMD追従動作が正常
- [ ] クリーンに終了できる

---

## NuGet パッケージ注意事項

| パッケージ | バージョン | 注意 |
|-----------|-----------|------|
| OVRSharp | 1.2.0 (最新) | `Version="1.*"` で指定済み。4.x は存在しない |
| SkiaSharp | 2.x | `Version="2.*"` で指定済み |
| SlackNet | (未使用) | Socket Modeは本体パッケージに含まれる |

---

## 既存スタブファイルの場所

実装対象のスタブ（`throw new NotImplementedException()`）:
- `src/VRNotify.Overlay/Rendering/SkiaNotificationRenderer.cs` - RenderCard メソッド
- `src/VRNotify.Overlay/OpenVR/OpenVrOverlayManager.cs` - InitializeAsync, ShowNotificationAsync, HideSlotAsync, UpdatePositionAsync

これらのスタブを上記仕様に基づいて実装する。

---

## M005完了後の次ステップ

M005完了後は以下のいずれかに進む:
- **M006**: Discord Bot プロトタイプ（Discord.Netでメッセージ受信→コンソール表示）
- **M008**: 統合プロトタイプ（Discord→VRオーバーレイ表示の一気通貫）

詳細は `memory/index.yaml` の `next_milestones` セクションを参照。
