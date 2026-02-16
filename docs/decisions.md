# VR通知オーバーレイシステム - 決定事項
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

## プロジェクト名: VRNotify

## アーキテクチャピボット決定

| 項目 | 旧方式 | 新方式（確定） | 理由 |
|------|--------|--------------|------|
| 通知ソース | Discord.Net + Slack Socket Mode | Windows UserNotificationListener | あらゆるアプリ対応、ユーザー設定不要 |
| 認証 | OAuth2 + Bot Token | Package Identity (Sparse MSIX) | トークン管理不要、セットアップ簡略化 |
| 優先度判定 | DM/メンション/キーワード | 全て Low (Booth MVP) | Windows通知に優先度情報なし |
| フィルタ | チャンネル/サーバー/キーワード/送信者 | アプリ名のみ (Booth MVP) | Windows通知で利用可能な情報に限定 |

## 技術スタック

| 項目 | 決定 | 備考 |
|------|------|------|
| 言語 | C# / .NET 8 | self-contained, win-x64 |
| VRオーバーレイ | OVRSharp (OpenVR) | |
| 通知キャプチャ | Windows UserNotificationListener | Package Identity 必須 |
| テクスチャ描画 | SkiaSharp | |
| デスクトップUI | WPF + CommunityToolkit.Mvvm | |
| システムトレイ | Hardcodet.NotifyIcon.Wpf | |
| 設定永続化 | JSON (`%APPDATA%/VRNotify/settings.json`) | アトミック書き込み (.tmp → rename) |
| 通知履歴 | SQLite (`%APPDATA%/VRNotify/history.db`) | 自動マイグレーション |
| ログ | Serilog (File sink) | `%APPDATA%/VRNotify/logs/` |
| DI/ホスティング | Microsoft.Extensions.Hosting | 3 HostedServices |
| インストーラー | NSIS | 証明書 + MSIX 自動登録 |
| パッケージID | Sparse MSIX | AppxManifest.xml + 自己署名証明書 |

## Booth MVP スコープ

### 含む（実装済み）
- Windows通知キャプチャ → VRオーバーレイ表示
- アプリフィルタ（アプリ名で許可/除外）
- DND モード（Off / SuppressAll / HighPriorityOnly）
- 表示設定（位置、表示秒数、不透明度、スケール）
- 通知履歴（SQLite、7日間/1000件）
- WPF設定ウィンドウ（5タブ）
- システムトレイ常駐
- NSISインストーラー + ポータブルZIP版

### 含まない（将来実装）
- 通知音 (SteamVR Audio) → Phase 2
- 複数プロファイル切替 → Phase 2
- 配信/プライバシーモード → Phase 2
- Discord/Slack Bot 直接連携 → Phase 3
- DPAPI トークン暗号化 → Phase 3
- プラグインシステム → Phase 3
- 自動アップデート → Phase 3
- VR内返信・コントローラー振動 → Phase 3

## UX 決定事項

| 項目 | 決定 | 備考 |
|------|------|------|
| 通知ソース | Windows通知リスナー | ユーザーはトークン設定不要 |
| 初期セットアップ | インストーラーで完結 | 証明書+MSIX登録を自動化 |
| 設定変更 | デスクトップ設定ウィンドウ | VR内設定は将来対応 |
| 表示位置 | HMD追従（上部） | 設定で変更可能 |
| 通知音 | Booth MVPに含まない | Phase 2 |
| アプリ終了 | トレイメニュー→Exit | ウィンドウ閉じはHideのみ |

## ビジネス決定事項

| 項目 | 決定 | 備考 |
|------|------|------|
| 配布形態 | Booth先行（投げ銭型）→ Steam後続 | |
| ライセンス | MIT License | |
| インストーラー方式 | NSIS (.exe) | OpenVR Advanced Settings 参考 |
| 対応OS | Windows 10 (Build 19041) / Windows 11 | |
| VR要件 | SteamVR 対応ヘッドセット | Quest 2/3/Pro, Index, VIVE 等 |
| ランタイム同梱 | .NET 8 self-contained | ユーザーのインストール不要 |

## 技術的決定事項

| 決定 | 選択 | 根拠 |
|------|------|------|
| プロセスモデル | シングルプロセス | IPC不要で簡潔 |
| 非同期キュー | System.Threading.Channels | 高性能、バックプレッシャー対応 |
| 設定形式 | JSON | 人間が読める、マイグレーション容易 |
| 履歴ストレージ | SQLite | 構造化クエリ、外部サーバー不要 |
| オーバーレイレンダリング | SkiaSharp → OpenVR SetOverlayTexture | 高品質テキスト描画 |
| WPF MVVM | CommunityToolkit.Mvvm | Source generators、最小ボイラープレート |
| ログ | Serilog | 構造化ログ、ファイルローテーション |
| Entity再構築 | Internal reconstitution ctor + InternalsVisibleTo | ドメインの不変条件を維持しつつDBから復元 |
| DTO分離 | SettingsDtos.cs | JSONスキーマとドメインモデルを疎結合 |

## パフォーマンス要件

- FPS影響: ベースFPSの5%以内（最大3fps低下）
- メモリ: 通常時150MB以下
- CPU: アイドル時1%以下、通知処理時5%以下
- テクスチャ更新: 通知表示中のみ

## 段階的リリース計画

- **Phase 1 (Booth MVP)**: ✅ 完了 — Windows通知 + VR表示 + アプリフィルタ + DND + 設定UI + 履歴 + インストーラー
- **Phase 2**: 通知音 + プロファイル切替 + 配信モード
- **Phase 3**: Discord/Slack Bot連携 + プラグインシステム + 自動アップデート + VR内返信
