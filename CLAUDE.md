# VRNotify - VR通知オーバーレイシステム

## プロジェクト概要
Windows の通知を VR 空間内にリアルタイムで表示するオーバーレイアプリケーション。
SteamVR 対応の VR ヘッドセットで、Discord / LINE / Slack 等のアプリ通知を VR を外さずに確認できる。

## 重要: 開発コンテキスト
- **対象ユーザー**: VRChatユーザー（Quest PCVR環境が主）
- **配布方針**: Booth先行（投げ銭型）→ Steam後続
- **言語**: 日本語でのコミュニケーション。コード内コメント・コミットメッセージは英語。BDDシナリオの説明文は日本語。
- **PowerShell**: pwsh 未インストール。PowerShell 5.1 のみ。`?.` 構文使用不可

## アーキテクチャピボット（重要）

初期設計では Discord.Net / Slack Socket Mode による Bot 直接連携方式だったが、
**Windows 通知リスナー方式 (`UserNotificationListener`)** にピボット済み。

**理由**: Windows の通知を直接キャプチャすることで、Discord/Slack/LINE/Teams 等あらゆるアプリに対応可能。
Bot トークン管理・OAuth2 認証・サービス別アダプターが不要になり、ユーザーのセットアップも大幅に簡略化。

**技術的制約**: `UserNotificationListener` は Package Identity が必要。
Sparse MSIX パッケージ登録（自己署名証明書 + `Add-AppxPackage -Register`）で対応。
アプリは `shell:AppsFolder\{AUMID}` 経由で起動する必要がある。

## 技術スタック（確定）
| 要素 | 技術 |
|------|------|
| 言語/ランタイム | C# / .NET 8 (self-contained, win-x64) |
| VRオーバーレイ | OVRSharp (OpenVR IVROverlay API) |
| 通知キャプチャ | Windows UserNotificationListener |
| テクスチャ描画 | SkiaSharp |
| デスクトップUI | WPF + CommunityToolkit.Mvvm |
| システムトレイ | Hardcodet.NotifyIcon.Wpf |
| 設定永続化 | JSON (`%APPDATA%/VRNotify/settings.json`) |
| 通知履歴 | SQLite (`%APPDATA%/VRNotify/history.db`) |
| ログ | Serilog (File sink, `%APPDATA%/VRNotify/logs/`) |
| DI/ホスティング | Microsoft.Extensions.Hosting |
| インストーラー | NSIS (Sparse MSIX 自動登録) |
| パッケージID | Sparse MSIX (AppxManifest.xml + 自己署名証明書) |

## アーキテクチャ方針

### 境界づけられたコンテキスト（4つ）
1. **ソース接続 (Source Connection)** - 通知ソースとの接続管理
2. **通知処理 (Notification Processing)** - フィルタリング・優先度判定・キューイング・履歴
3. **VR表示 (VR Display)** - OpenVRオーバーレイ描画・レイアウト
4. **設定管理 (Configuration)** - プロファイル・永続化・デスクトップUI

### コンテキスト間連携
- ソース接続 → 通知処理: `NotificationReceivedEvent` ドメインイベント
- 通知処理 → VR表示: `NotificationCard` をキュー経由 (`INotificationQueue`)
- 設定管理 → 全コンテキスト: `ISettingsRepository` 経由

### 拡張性の核心設計
- `ISourceAdapter` インターフェースで通知ソースを抽象化
- 統一 `NotificationEvent` モデルで全ソースの通知を正規化
- 現在は `WindowsNotificationAdapter` のみ実装（将来 Discord Bot 等追加可能）

## Booth MVP スコープ

### 含む（実装済み）
- Windows通知キャプチャ → VRオーバーレイ表示
- アプリフィルタ（アプリ名で通知の表示/非表示、許可リスト/除外リスト方式）
- DND モード（全抑制 / 高優先度のみ / オフ）
- 表示設定（位置、表示秒数、不透明度、スケール）
- 通知履歴（SQLite、7日間/1000件）
- WPF 設定ウィンドウ（5タブ: 一般/フィルタ/表示/履歴/About）
- システムトレイ常駐（バックグラウンド動作）
- NSIS インストーラー（証明書+MSIX自動登録）+ ポータブルZIP版

### 含まない（将来Phase）
- 通知音 (SteamVR Audio) → Phase 2
- 複数プロファイル切替 → Phase 2
- Discord/Slack Bot 直接連携 → Phase 3
- DPAPI トークン暗号化 → Phase 3（Bot連携時）
- 自動アップデート → Phase 3
- VR内返信・コントローラー振動 → Phase 3

## ビジネスルール（重要）
- 通知優先度: Booth MVP では全て `Priority.Low`（Windows通知に優先度情報がないため）
- 表示スロット: デフォルト3、最大5
- 表示時間: ユーザー設定可能（デフォルト5秒）
- 再接続: SteamVR ポーリング（5秒間隔）
- 設定保存: アトミック書き込み（.tmp → rename）
- フィルタ: アプリ名ベース、Include/Exclude、大文字小文字区別なし、先頭マッチ優先
- 履歴パージ: 7日超 or 1000件超を削除

## 通知パイプライン

```
Windows通知 → WindowsNotificationAdapter → NotificationReceivedEvent
  → SourceConnectionService:
      1. FilterChain 評価（アプリ名フィルタ）
      2. PriorityResolver（全て Low）
      3. DND チェック
      4. NotificationCard 生成
      5. NotificationHistory 保存（SQLite）
      6. NotificationQueue へ enqueue
  → NotificationDisplayService:
      7. Queue から dequeue
      8. DisplaySlotManager でスロット確保
      9. OpenVrOverlayManager で VR 表示
     10. DisplayDuration 後に自動非表示
```

### フィールドマッピング（Windows通知 → NotificationEvent）
- `Sender.Name` = アプリ表示名（例: "Discord", "LINE"）← **フィルタ対象**
- `Channel.Name` = トースト通知のタイトル
- `Content.Text` = トースト通知の本文

## ドキュメント配置
```
docs/
├── architecture.md              # アーキテクチャ設計
├── decisions.md                 # 全決定事項
├── release-guide.md             # リリース手順書
└── bdd/
    ├── 01_ux_user_experience.feature   # UX: 表示体験
    ├── 02_ux_interaction.feature       # UX: セットアップ・日常フロー
    ├── 03_domain_model.md              # ドメインモデル・用語集
    ├── 04_domain_bdd.feature           # ドメイン: ソース管理・フィルタ・ライフサイクル
    └── 05_technical_edge_cases.feature # 技術: 障害復旧・パフォーマンス・セキュリティ
```

## 現在の開発状態（2026-02-17時点）

### 完了済みマイルストーン
| ID | 名前 | 内容 |
|----|------|------|
| M001 | 競合調査 | XSOverlay等の競合分析、技術的実現可能性確認 |
| M002 | BDD・要件定義 | 3視点から約115シナリオ作成、ドメインモデル確立 |
| M003 | 技術・ビジネス決定 | 技術スタック、MVPスコープ等の主要決定完了 |
| M004 | アーキテクチャ設計 | 6 srcプロジェクト + テストプロジェクト、全ビルド成功 |
| M004.5 | ドメインテスト | 83テスト作成・全PASS |
| M005 | OpenVRオーバーレイ | SkiaSharp描画 + OpenVR表示 プロトタイプ |
| M006 | Windows通知キャプチャ | UserNotificationListener + Sparse MSIX プロトタイプ |
| M007 | 統合プロトタイプ | Windows通知 → VR表示の E2E パイプライン動作確認 |
| M008 | 設定永続化+フィルタ | JsonSettingsRepository, SqliteNotificationHistory, DefaultFilterChain, PriorityResolver |
| M009 | HostedServices+トレイ | SourceConnectionService, NotificationDisplayService, OpenVrHostedService, App.xaml (トレイ常駐) |
| M010 | WPF設定UI | SettingsWindow (5タブ), MVVM ViewModels |
| M011 | インストーラー | NSIS + build.ps1, ポータブル版ZIP |
| M012 | リリース準備 | README.md, LICENSE, release-guide.md |

### テスト状況
- **109テスト全PASS** (Domain 83 + Infrastructure 26)
- `dotnet build VRNotify.sln -c Release` → 0 errors, 0 warnings

### プロジェクト構造
```
VRNotify.sln
├── src/
│   ├── VRNotify.Domain/          # ドメイン層（完全実装、InternalsVisibleTo設定済み）
│   ├── VRNotify.Application/     # アプリケーション層（主要ハンドラ実装済み）
│   ├── VRNotify.Infrastructure/  # インフラ層（JSON設定, SQLite履歴, フィルタ, Windows通知, キュー）
│   ├── VRNotify.Overlay/         # OpenVR/SkiaSharp（描画+オーバーレイ管理 実装済み）
│   ├── VRNotify.Host/            # HostedServices + DI（3サービス + ServiceRegistration）
│   └── VRNotify.Desktop/         # WPF設定UI + トレイアプリ（App.xaml, SettingsWindow, ViewModels）
├── tests/
│   ├── VRNotify.Domain.Tests/           # 83テスト
│   ├── VRNotify.Infrastructure.Tests/   # 26テスト（設定/履歴/フィルタ）
│   ├── VRNotify.Application.Tests/      # (空)
│   ├── VRNotify.Integration.Tests/      # (空)
│   ├── VRNotify.Integration.Prototype/  # M007 統合プロトタイプ
│   ├── VRNotify.Overlay.Prototype/      # M005 オーバーレイプロトタイプ
│   └── VRNotify.NotificationListener.Prototype/  # M006 通知リスナープロトタイプ
├── installer/                   # NSIS スクリプト + SteamVR マニフェスト
├── packaging/                   # AppxManifest.xml + 証明書/セットアップスクリプト
├── docs/                        # BDD/アーキテクチャ/リリースドキュメント
├── build.ps1                    # リリースビルドスクリプト
├── README.md                    # ユーザー向けドキュメント（日本語）
└── LICENSE                      # MIT License
```

## ビルド・リリース

```powershell
# テスト
dotnet test VRNotify.sln -c Release

# リリースビルド（インストーラー + ZIP）
.\build.ps1

# 成果物: dist/VRNotify-1.0.0-Installer.exe, dist/VRNotify-1.0.0-Portable.zip
```

詳細は `docs/release-guide.md` を参照。

## 参考プロジェクト
- [OyasumiVR](https://github.com/Raphiiko/OyasumiVR) - SteamVRオーバーレイアプリの設計参考
- [OpenVR Advanced Settings](https://github.com/OpenVR-Advanced-Settings/OpenVR-AdvancedSettings) - NSIS + Sparse MSIX インストーラー参考
- [OVRSharp](https://github.com/OVRTools/OVRSharp) - OpenVR C#ラッパー
- [xs-notify](https://github.com/Erallie/xs-notify) - Windows通知→VR転送（競合参考）
