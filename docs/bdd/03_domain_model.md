# VR通知オーバーレイシステム - ドメインモデル & 業務知識
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

## 1. ユビキタス言語（用語集）

| 用語 | 英語表記 | 定義 |
|------|----------|------|
| **通知ソース** | Notification Source | 通知を発生させるソースへの接続単位。Booth MVP では Windows 通知リスナーのみ |
| **ソースアダプター** | Source Adapter | 通知ソースのプロトコル差異を吸収し、統一的な通知イベントに変換するコンポーネント |
| **通知イベント** | Notification Event | ソースから受信した通知を正規化した内部表現（フィルタリング前） |
| **通知カード** | Notification Card | フィルタを通過し表示対象と判定された通知の最終形態。VRオーバーレイ上に描画される単位 |
| **通知キュー** | Notification Queue | 表示待ちの通知カードを保持するバッファ (`System.Threading.Channels`) |
| **オーバーレイ** | Overlay | OpenVR上に描画されるUI面（SkiaSharpでレンダリング） |
| **フィルタルール** | Filter Rule | 通知イベントを通過/遮断する条件の1単位 |
| **フィルタチェーン** | Filter Chain | 複数のフィルタルールを順序付きで評価するパイプライン |
| **通知優先度** | Notification Priority | 高(High)・中(Medium)・低(Low)の3段階。Booth MVPでは全てLow |
| **プロファイル** | Profile | フィルタルール・表示設定・DND設定の名前付き保存 |
| **表示スロット** | Display Slot | オーバーレイ上で通知カードを同時に表示できる位置 |
| **表示時間** | Display Duration | 通知カードがオーバーレイ上に表示され続ける秒数 |
| **おやすみモード** | Do Not Disturb (DND) | Off / 全抑制(SuppressAll) / 高優先度のみ(HighPriorityOnly) |
| **通知履歴** | Notification History | 過去に受信した通知カードの永続化されたログ（SQLite） |
| **Package Identity** | Package Identity | Windows通知リスナーに必要なアプリ識別子。Sparse MSIX登録で付与 |

## 2. ドメインモデル図

```
┌─────────────────────────────────────────────────────────────────────┐
│                        システム全体構成                               │
└─────────────────────────────────────────────────────────────────────┘

  [Windows通知]              [コアドメイン]              [VR表示層]
  ┌──────────────┐    ┌──────────────────────┐    ┌─────────────────┐
  │  Windows      │    │                      │    │                 │
  │  Notification │───>│   SourceAdapter      │    │   Overlay       │
  │  Listener     │    │   (Windows)          │    │   Manager       │
  └──────────────┘    │     │                │    │     │           │
                      │     v                │    │     v           │
  [将来拡張]           │   NotificationEvent  │    │   DisplaySlot   │
  ┌──────────────┐    │     │                │    │   [1] [2] [3]   │
  │  Discord Bot │···>│     v                │    │     ^           │
  │  (Phase 3)   │    │   FilterChain        │    │     │           │
  └──────────────┘    │     │                │    │   Notification  │
                      │     v                │    │   Card          │
                      │   NotificationCard   │───>│   (Rendered)    │
                      │     │                │    └─────────────────┘
                      │     v                │
                      │   NotificationQueue  │
                      │     │                │
                      │     v                │
                      │   NotificationHistory│
                      │   (SQLite)           │
                      └──────────────────────┘

                      ┌──────────────────────┐
                      │   設定管理             │
                      │                      │
                      │   Profile            │
                      │     ├─ FilterRule[]   │
                      │     ├─ DisplayConfig  │
                      │     └─ DndSettings   │
                      │                      │
                      │   UserSettings       │
                      │     ├─ ActiveProfile  │
                      │     ├─ AudioConfig   │
                      │     └─ HistoryConfig │
                      │                      │
                      │   永続化: JSON        │
                      │   %APPDATA%/VRNotify/ │
                      └──────────────────────┘
```

## 3. 主要エンティティの定義

```csharp
NotificationSource (集約ルート)
  ├── sourceId: Guid
  ├── sourceType: SourceType (Windows | Discord | Slack | ...)
  ├── displayName: string
  ├── connectionState: ConnectionState
  ├── credential: EncryptedCredential   // Windows adapter: 空
  ├── isEnabled: bool
  └── adapter: ISourceAdapter

NotificationEvent (値オブジェクト)
  ├── eventId: Guid
  ├── sourceId: Guid
  ├── sourceType: SourceType
  ├── timestamp: DateTimeOffset
  ├── sender: SenderInfo { name, avatarUrl, id }   // name = アプリ名
  ├── channel: ChannelInfo { name, id, ... }        // name = 通知タイトル
  ├── content: MessageContent { text, ... }         // text = 通知本文
  └── rawPayload: JsonElement

NotificationCard (エンティティ / 集約ルート)
  ├── cardId: Guid
  ├── originEventId: Guid
  ├── priority: Priority (High | Medium | Low)
  ├── state: NotificationState (Unread | Read | Archived)
  ├── title: string              // Channel.Name (通知タイトル)
  ├── body: string               // Content.Text (通知本文)
  ├── senderDisplay: string      // Sender.Name (アプリ名)
  ├── senderAvatarUrl: string?
  ├── createdAt: DateTimeOffset
  ├── displayedAt: DateTimeOffset?
  ├── readAt: DateTimeOffset?
  └── displayDuration: TimeSpan

FilterRule (値オブジェクト)
  ├── ruleId: Guid
  ├── ruleType: FilterRuleType   // Booth MVP: AppName のみ
  ├── condition: FilterCondition (Include | Exclude)
  ├── parameters: Dictionary<string, string>
  └── order: int (評価順序)

Profile (エンティティ)
  ├── profileId: Guid
  ├── name: string
  ├── filterRules: List<FilterRule>
  ├── displayConfig: DisplayConfig
  ├── dndSettings: DndSettings
  ├── enabledSourceIds: List<Guid>
  └── isDefault: bool
```

## 4. ビジネスルール

### 4.1 通知優先度の決定（Booth MVP）

| 条件 | 優先度 | 根拠 |
|------|--------|------|
| 全てのWindows通知 | 低 | Windows通知APIに優先度情報がないため一律Low |

> Phase 3（Discord Bot連携時）に DM=高、@メンション=高、@here=中 等の細分化を実装予定

### 4.2 表示ルール

- 同時表示スロット数: デフォルト3、最大5
- 表示時間: ユーザー設定可能（デフォルト5秒）
- キュー: スロット満杯時はキューに蓄積、空いたら順次表示
- おやすみモード: 高優先度のみ表示 or 全通知抑制を選択可能

### 4.3 フィルタリングルール（Booth MVP: AppName のみ）

- **フィルタ対象**: `Sender.Name`（= Windows通知のアプリ表示名）
- フィルタチェーンは `Order` 順に評価、最初のマッチで確定
- **大文字小文字区別なし** (case-insensitive)
- Include ルールがある場合 → デフォルトは拒否（許可リスト方式）
- Exclude ルールのみの場合 → デフォルトは許可（除外リスト方式）
- ルールなし → 全許可

### 4.4 SteamVR 接続管理

- 5秒間隔で `OpenVR.Init()` をポーリング
- 接続時: オーバーレイ初期化、通知表示開始
- 切断時: リソース解放、再ポーリング開始

### 4.5 履歴管理ルール

- 保存先: SQLite (`%APPDATA%/VRNotify/history.db`)
- 保持期間: デフォルト7日間
- 保持件数: デフォルト1000件
- 条件を超えた通知は古い順に自動パージ

### 4.6 設定永続化

- 保存先: JSON (`%APPDATA%/VRNotify/settings.json`)
- アトミック書き込み: `.tmp` に書き込み → `File.Move(overwrite: true)`
- ファイルなし/破損時: デフォルト設定で起動

## 5. 境界づけられたコンテキスト

| コンテキスト | 責務 | Booth MVP 実装状態 |
|-------------|------|-------------------|
| ソース接続 | 通知ソースとの接続管理 | ✅ WindowsNotificationAdapter |
| 通知処理 | フィルタリング、優先度判定、キューイング、履歴保存 | ✅ DefaultFilterChain, PriorityResolver, ChannelNotificationQueue, SqliteNotificationHistory |
| VR表示 | OpenVRオーバーレイ描画、スロット管理 | ✅ OpenVrOverlayManager, SkiaNotificationRenderer, DisplaySlotManager |
| 設定管理 | 設定永続化、WPF設定UI | ✅ JsonSettingsRepository, SettingsWindow (5タブ) |

コンテキスト間の連携:
- ソース接続 → 通知処理: `NotificationReceivedEvent` → `SourceConnectionService` で処理
- 通知処理 → VR表示: `NotificationCard` を `INotificationQueue` 経由で渡す
- 設定管理 → 各コンテキスト: `ISettingsRepository` 経由で設定読み書き

## 6. Windows通知フィールドマッピング

```
WindowsNotificationAdapter が Windows 通知を NotificationEvent に変換:

UserNotification (Windows API)
  ├── AppInfo.DisplayInfo.DisplayName  →  Sender.Name    (例: "Discord")
  ├── Notification.Visual (Title)      →  Channel.Name   (例: "太郎: こんにちは")
  └── Notification.Visual (Body)       →  Content.Text   (例: "今夜VRChatで遊ばない？")

NotificationEvent → NotificationCard の変換:
  ├── Channel.Name   →  card.Title          (通知タイトル)
  ├── Content.Text   →  card.Body           (通知本文)
  └── Sender.Name    →  card.SenderDisplay  (アプリ名)

フィルタ評価: Sender.Name (アプリ名) に対して FilterRule.AppName を照合
```
