# VR通知オーバーレイシステム - ドメインモデル & 業務知識

## 1. ユビキタス言語（用語集）

| 用語 | 英語表記 | 定義 |
|------|----------|------|
| **通知ソース** | Notification Source | 通知を発生させる外部サービスへの接続単位。1つのサービスアカウントにつき1つ |
| **ソースアダプター** | Source Adapter | 各外部サービスのAPIプロトコルの差異を吸収し、統一的な通知イベントに変換するコンポーネント |
| **通知イベント** | Notification Event | 外部サービスから受信した生の通知データを正規化した内部表現（フィルタリング前） |
| **通知カード** | Notification Card | フィルタリングを通過し表示価値ありと判定された通知の最終形態。VRオーバーレイ上に描画される単位 |
| **通知キュー** | Notification Queue | 表示待ちの通知カードを優先度順に保持するバッファ |
| **オーバーレイ** | Overlay | OpenVR上に描画されるUI面 |
| **フィルタルール** | Filter Rule | 通知イベントを通過/遮断する条件の1単位 |
| **フィルタチェーン** | Filter Chain | 複数のフィルタルールを順序付きで評価するパイプライン |
| **通知優先度** | Notification Priority | 高(High)・中(Medium)・低(Low)の3段階 |
| **プロファイル** | Profile | フィルタルール・表示設定・有効ソースの組み合わせの名前付き保存 |
| **接続状態** | Connection State | 切断→接続中→接続済→再接続中→切断 のライフサイクル |
| **表示スロット** | Display Slot | オーバーレイ上で通知カードを同時に表示できる位置 |
| **表示時間** | Display Duration | 通知カードがオーバーレイ上に表示され続ける秒数 |
| **通知状態** | Notification State | 未読(Unread)→既読(Read)→アーカイブ(Archived) |
| **おやすみモード** | Do Not Disturb (DND) | すべての通知表示を抑制するモード |
| **通知履歴** | Notification History | 過去に受信した通知カードの永続化されたログ |

## 2. ドメインモデル図

```
┌─────────────────────────────────────────────────────────────────────┐
│                        システム全体構成                               │
└─────────────────────────────────────────────────────────────────────┘

  [外部サービス群]           [コアドメイン]              [VR表示層]
  ┌──────────────┐    ┌──────────────────────┐    ┌─────────────────┐
  │   Discord    │───>│                      │    │                 │
  │   (Bot/User) │    │   SourceAdapter      │    │   Overlay       │
  └──────────────┘    │     │                │    │   Manager       │
  ┌──────────────┐    │     v                │    │     │           │
  │   Slack      │───>│   NotificationEvent  │    │     v           │
  │   (App)      │    │     │                │    │   DisplaySlot   │
  └──────────────┘    │     v                │    │   [1] [2] [3]   │
  ┌──────────────┐    │   FilterChain        │    │     ^           │
  │   将来拡張    │···>│     │                │    │     │           │
  │  (Chatwork等) │    │     v                │    │   Notification  │
  └──────────────┘    │   NotificationCard   │───>│   Card          │
                      │     │                │    │   (Rendered)    │
                      │     v                │    └─────────────────┘
                      │   NotificationQueue  │
                      │     │                │
                      │     v                │
                      │   NotificationHistory│
                      └──────────────────────┘

                      ┌──────────────────────┐
                      │   設定・ルール管理     │
                      │                      │
                      │   Profile            │
                      │     ├─ FilterRule[]   │
                      │     ├─ DisplayConfig  │
                      │     └─ SourceBinding[]│
                      │                      │
                      │   UserSettings       │
                      │     ├─ ActiveProfile  │
                      │     ├─ GlobalConfig   │
                      │     └─ Credentials[]  │
                      └──────────────────────┘
```

## 3. 主要エンティティの定義

```csharp
NotificationSource (集約ルート)
  ├── sourceId: Guid
  ├── sourceType: SourceType (Discord | Slack | ...)
  ├── displayName: string
  ├── connectionState: ConnectionState
  ├── credential: EncryptedCredential
  ├── isEnabled: bool
  └── adapter: ISourceAdapter

NotificationEvent (値オブジェクト)
  ├── eventId: Guid
  ├── sourceId: Guid
  ├── sourceType: SourceType
  ├── timestamp: DateTimeOffset
  ├── sender: SenderInfo { name, avatarUrl, id }
  ├── channel: ChannelInfo { name, id, serverOrWorkspace }
  ├── content: MessageContent { text, hasAttachment, mentionType }
  └── rawPayload: JsonElement

NotificationCard (エンティティ / 集約ルート)
  ├── cardId: Guid
  ├── originEventId: Guid
  ├── priority: Priority (High | Medium | Low)
  ├── state: NotificationState (Unread | Read | Archived)
  ├── title: string
  ├── body: string (truncated)
  ├── senderDisplay: string
  ├── sourceIcon: Icon
  ├── createdAt: DateTimeOffset
  ├── displayedAt: DateTimeOffset?
  ├── readAt: DateTimeOffset?
  └── displayDuration: TimeSpan

FilterRule (値オブジェクト)
  ├── ruleId: Guid
  ├── ruleType: FilterRuleType
  ├── condition: FilterCondition (Include | Exclude)
  ├── parameters: Dictionary<string, string>
  └── priority: int (評価順序)

Profile (エンティティ)
  ├── profileId: Guid
  ├── name: string
  ├── filterRules: List<FilterRule>
  ├── displayConfig: DisplayConfig
  ├── enabledSources: List<Guid>
  ├── dndSettings: DndSettings
  └── isDefault: bool
```

## 4. ビジネスルール

### 4.1 通知優先度の決定

| 条件 | 優先度 | 根拠 |
|------|--------|------|
| ダイレクトメッセージ | 高 | 個人宛の直接通信は即時性が求められる |
| @メンション（自分宛） | 高 | 名指しでの呼びかけは応答期待がある |
| @here / @channel / @everyone | 中 | グループ宛は個人宛より緊急度が下がる |
| キーワードマッチ（ユーザー定義） | 中（設定可能） | ユーザーが関心を示した語 |
| 上記以外の通常メッセージ | 低 | 情報としては有用だが即時対応不要 |

### 4.2 表示ルール

- 同時表示スロット数: デフォルト3、最大5
- 表示時間デフォルト: 高=10秒、中=7秒、低=5秒
- キュー満杯時: 低優先度表示中なら高優先度に割り込み許可
- バンドル: 同一送信者からの3秒以内の連続投稿は1枚にまとめる
- おやすみモード: 高優先度のみ表示 or 全通知抑制を選択可能

### 4.3 フィルタリングルール

- フィルタチェーンは上から順に評価、最初のマッチで確定
- マッチなしの場合はデフォルトポリシー（デフォルト: 許可）
- 時間帯フィルタは他フィルタより先に評価
- フィルタルールはプロファイルに紐づく

### 4.4 接続管理ルール

- 自動再接続: 指数バックオフ（初回1秒、最大60秒、最大10回）
- 上限到達時: ユーザーに接続失敗通知、手動再接続を案内
- SteamVR未起動時: オーバーレイ表示スキップ、履歴にのみ記録

### 4.5 履歴管理ルール

- 保持期間: デフォルト7日間
- 保持件数: デフォルト1000件
- 両条件を満たした場合に古い通知から削除

## 5. 境界づけられたコンテキスト

| コンテキスト | 責務 | 主要な技術的関心事 |
|-------------|------|-------------------|
| ソース接続 | 外部サービスとの接続確立・維持・再接続、認証、プロトコル変換 | WebSocket, REST API, OAuth2/Bot Token, プラグイン機構 |
| 通知処理 | フィルタリング、優先度判定、キューイング、状態管理、履歴保存 | ルールエンジン、イベント駆動、永続化 |
| VR表示 | OpenVRオーバーレイ描画、レイアウト制御、ユーザーインタラクション | OVRSharp, テクスチャ描画, 入力ハンドリング |
| 設定管理 | 全コンテキスト横断の設定値管理、プロファイル切替、永続化 | ファイルI/O, 暗号化, デスクトップUI |

コンテキスト間の連携:
- ソース接続 → 通知処理: `NotificationEvent`をドメインイベントとして発行
- 通知処理 → VR表示: `NotificationCard`をキュー経由で渡す
- 設定管理 → 各コンテキスト: 設定変更イベントをpub/subで通知
