# VR通知オーバーレイシステム - 決定事項

## プロジェクト名（仮）: VRNotify

## 技術スタック

| 項目 | 決定 | 備考 |
|------|------|------|
| 言語 | C# / .NET 8 | |
| VRオーバーレイ | OVRSharp (OpenVR) | |
| Discord連携 | OAuth2 + Bot混合方式 | 認証はOAuth2で簡単に、メッセージ取得はBot (Gateway) で |
| Slack連携 | Socket Mode (WebSocket) | 公開サーバー不要 |
| テクスチャ描画 | SkiaSharp | クロスプラットフォーム・高品質フォント |
| デスクトップUI | WPF | Windows専用で問題なし |
| 設定永続化 | JSON (設定) + SQLite (通知履歴) | |
| トークン保護 | Windows DPAPI | |

## MVP スコープ（標準MVP）

### 含む
- Discord 通知（DM、メンション、チャンネル選択）
- Slack 通知（Socket Mode）
- HMD追従表示（上部/下部/手首を設定で切替可能）
- 通知フィルタリング（チャンネル、サーバー、キーワード、送信者）
- Do Not Disturb モード
- 通知履歴
- 通知音（SteamVR Audio）
- デスクトップ設定UI (WPF)
- SteamVR自動起動/終了連動

### 含まない（将来実装）
- VR内からの返信機能
- コントローラー振動フィードバック
- 配信モード（プライバシーモード）
- スケジュールDND
- VRChatワールド移動検知
- Chatwork等のプラグイン拡張
- プロファイル切替
- 自動アップデート

## UX 決定事項

| 項目 | 決定 | 備考 |
|------|------|------|
| 通知表示位置 | 全方式実装、設定で切替可能 | デフォルトはテスト後に決定 |
| 返信機能 | MVPに含めない | 将来の拡張ポイントのみ設計 |
| 通知音 | MVPに含む（音のみ、振動なし） | SteamVRオーディオ経由 |
| 初期セットアップ | デスクトップで完結 | VR内では基本設定のみ変更可能 |

## ビジネス決定事項

| 項目 | 決定 | 備考 |
|------|------|------|
| 配布形態 | 将来的にSteam販売 | 初期はテスト配布 |
| Discord権限 | Privileged Intent使用 | Steam販売時は100サーバー未満制限に注意。Bot認証申請が必要になる可能性 |
| ライセンス | 未定 | Steam販売前提なのでプロプライエタリ |

## Discord Bot 権限スコープ設計

### OAuth2 スコープ
- `identify` - ユーザー情報取得
- `guilds` - サーバー一覧取得
- `bot` - Bot機能

### Bot Permissions
- `Read Messages/View Channels` - チャンネルメッセージ読み取り
- `Read Message History` - メッセージ履歴

### Gateway Intents
- `GUILDS` - サーバー情報
- `GUILD_MESSAGES` - サーバーメッセージ
- `DIRECT_MESSAGES` - DM
- `MESSAGE_CONTENT` (Privileged) - メッセージ本文

## アーキテクチャ方針

### 拡張性
- `INotificationSource` インターフェースでサービス抽象化
- 将来のプラグインシステムを見据えた設計（MVP時点では固定実装）
- 統一 `NotificationEvent` モデルで全サービスの通知を正規化

### パフォーマンス要件
- FPS影響: ベースFPSの5%以内（最大3fps低下）
- メモリ: 通常時150MB以下
- CPU: アイドル時1%以下、通知処理時5%以下
- テクスチャ更新: 通知表示中のみ30fps

### 段階的リリース計画
- **Phase 1 (MVP)**: Discord + Slack + 基本UI + DND + 通知履歴 + 通知音
- **Phase 2**: プロファイル + 配信モード + コントローラー振動 + スケジュールDND
- **Phase 3**: プラグインシステム + Chatwork + 返信機能 + 自動アップデート
