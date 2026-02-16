# VRNotify - VR通知オーバーレイシステム

## プロジェクト概要
VRChat中にDiscord・Slack等の外部アプリ通知をSteamVRオーバーレイで表示する独自アプリケーション。
OyasumiVRのようなスタンドアロンSteamVRオーバーレイアプリとして動作する。

## 重要: 開発コンテキスト
- **対象ユーザー**: VRChatユーザー（Quest PCVR環境が主）
- **配布目標**: 将来的にSteam販売
- **言語**: 日本語でのコミュニケーション。コード内コメントは英語。BDDシナリオの説明文は日本語。
- **進捗記録**: 作業を行ったら必ず `memory/` 配下のYAMLファイルに記録すること

## 技術スタック（確定）
| 要素 | 技術 |
|------|------|
| 言語/ランタイム | C# / .NET 8 |
| VRオーバーレイ | OVRSharp (OpenVR IVROverlay API) |
| Discord | Discord.Net (OAuth2認証 + Bot Gateway) |
| Slack | Socket Mode (WebSocket, 公開サーバー不要) |
| テクスチャ描画 | SkiaSharp |
| デスクトップUI | WPF |
| 設定永続化 | JSON (設定ファイル) + SQLite (通知履歴) |
| トークン保護 | Windows DPAPI (ProtectedData) |
| ログ | Serilog (推奨) |

## アーキテクチャ方針

### 境界づけられたコンテキスト（4つ）
1. **ソース接続 (Source Connection)** - 外部サービスとの接続・再接続・認証
2. **通知処理 (Notification Processing)** - フィルタリング・優先度判定・キューイング・履歴
3. **VR表示 (VR Display)** - OpenVRオーバーレイ描画・レイアウト・インタラクション
4. **設定管理 (Configuration)** - プロファイル・永続化・デスクトップUI

### コンテキスト間連携
- ソース接続 → 通知処理: `NotificationEvent` ドメインイベント
- 通知処理 → VR表示: `NotificationCard` をキュー経由
- 設定管理 → 全コンテキスト: 設定変更イベント (pub/sub)

### 拡張性の核心設計
- `INotificationSource` インターフェースでサービスを抽象化
- 統一 `NotificationEvent` モデルで全サービスの通知を正規化
- MVP時点では固定実装、Phase 3でプラグインシステムに移行予定

## MVP スコープ（標準MVP）

### 含む
- Discord通知 (OAuth2 + Bot混合方式, DM・メンション・チャンネル選択)
- Slack通知 (Socket Mode)
- 通知表示 (HMD追従上部/下部/手首 - 設定で切替)
- 通知フィルタリング (チャンネル、サーバー、キーワード、送信者)
- Do Not Disturb モード
- 通知履歴 (SQLite, 7日間/1000件)
- 通知音 (SteamVR Audio, 音量調整・ミュート)
- デスクトップ設定UI (WPF, セットアップウィザード)
- SteamVR自動起動/終了連動

### 含まない（将来Phase）
- VR内返信機能 (Phase 3)
- コントローラー振動 (Phase 2)
- 配信/プライバシーモード (Phase 2)
- プロファイル切替 (Phase 2)
- プラグインシステム (Phase 3)
- Chatwork等の追加サービス (Phase 3)
- 自動アップデート (Phase 3)

## Discord Bot 権限設計
- **OAuth2**: `identify`, `guilds`, `bot`
- **Bot Permissions**: `Read Messages/View Channels`, `Read Message History`
- **Gateway Intents**: `GUILDS`, `GUILD_MESSAGES`, `DIRECT_MESSAGES`, `MESSAGE_CONTENT` (Privileged)
- Steam販売時にBot認証申請が必要になる可能性あり（100サーバー超でPrivileged Intent要申請）

## パフォーマンス要件
- FPS影響: ベースFPSの5%以内（最大3fps低下）
- メモリ: 通常時150MB以下
- CPU: アイドル時1%以下、通知処理時5%以下
- テクスチャ更新: 通知表示中のみ最大30fps、非表示時は0

## ビジネスルール（重要）
- 通知優先度: DM/メンション=高、@here/@channel=中、キーワード=中(設定可)、通常=低
- 表示スロット: デフォルト3、最大5
- 表示時間: 高=10秒、中=7秒、低=5秒
- バンドル: 同一送信者3秒以内の連続メッセージは1枚にまとめる
- 再接続: 指数バックオフ(1秒→最大60秒、ジッタ±20%、最大10回)
- 設定保存: アトミック書き込み（.tmp→rename）

## ドキュメント配置
```
docs/
├── decisions.md                     # 全決定事項
└── bdd/
    ├── 01_ux_user_experience.feature   # UX: 表示体験
    ├── 02_ux_interaction.feature       # UX: インタラクション・セットアップ
    ├── 03_domain_model.md              # ドメインモデル・用語集
    ├── 04_domain_bdd.feature           # ドメイン: ソース管理・フィルタ・ライフサイクル
    └── 05_technical_edge_cases.feature # 技術: 障害復旧・パフォーマンス・セキュリティ
```

## 進捗管理
- `memory/` 配下にYAMLで作業記録を残す
- `memory/sessions/` に各セッションの作業ログ
- `memory/index.yaml` にプロジェクト全体の状態サマリー

## 現在の開発状態（2026-02-16時点）

### 完了済みマイルストーン
| ID | 名前 | 内容 |
|----|------|------|
| M001 | 競合調査 | XSOverlay等の競合分析、技術的実現可能性確認 |
| M002 | BDD・要件定義 | 3視点から約115シナリオ作成、ドメインモデル確立 |
| M003 | 技術・ビジネス決定 | 技術スタック、MVPスコープ等の主要決定完了 |
| M004 | アーキテクチャ設計 | 6 srcプロジェクト + 3テストプロジェクト、全ビルド成功 |
| M004.5 | ドメインテスト | 83テスト作成・全PASS（10ファイル） |

### 次のタスク: M005 OpenVRオーバーレイ プロトタイプ
**詳細な実装仕様は `memory/handoff-m005-windows.md` を参照のこと。**

ハードコードした通知カードをSteamVRオーバーレイとしてVR内に表示するプロトタイプ。

作業手順:
1. `dotnet restore && dotnet build VRNotify.sln` で全プロジェクトビルド確認
2. `dotnet test tests/VRNotify.Domain.Tests/` で83テスト全PASS確認
3. `memory/handoff-m005-windows.md` を読み、Phase C の実装を行う

### プロジェクト構造
```
VRNotify.sln
├── src/
│   ├── VRNotify.Domain/          # ドメイン層（44ファイル、完全実装済み）
│   ├── VRNotify.Application/     # アプリケーション層（スタブ）
│   ├── VRNotify.Infrastructure/  # インフラ層（スタブ）
│   ├── VRNotify.Overlay/         # OpenVR/SkiaSharp（スタブ → M005で実装）
│   ├── VRNotify.Host/            # ホストプロセス（スタブ）
│   └── VRNotify.Desktop/         # WPF設定UI（スタブ、Windows専用）
├── tests/
│   ├── VRNotify.Domain.Tests/    # ドメインテスト（83テスト、全PASS）
│   ├── VRNotify.Application.Tests/
│   └── VRNotify.Integration.Tests/
├── docs/                         # BDD/アーキテクチャドキュメント
└── memory/                       # 進捗記録（YAML）
```

## 参考プロジェクト
- [OyasumiVR](https://github.com/Raphiiko/OyasumiVR) - SteamVRオーバーレイアプリの設計参考
- [OpenVROverlayPipe](https://github.com/BOLL7708/OpenVROverlayPipe) - WebSocket経由のオーバーレイ制御
- [OVRSharp](https://github.com/OVRTools/OVRSharp) - OpenVR C#ラッパー
- [xs-notify](https://github.com/Erallie/xs-notify) - Windows通知→VR転送
- [Desktop+](https://github.com/elvissteinjr/DesktopPlus) - C++オーバーレイ実装参考
