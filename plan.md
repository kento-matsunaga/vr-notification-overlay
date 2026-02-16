# アーキテクチャ設計 実装プラン

## 概要
VRNotifyのアーキテクチャ設計を行い、`docs/architecture.md` と `.sln` スキャフォールドを生成する。

## 設計方針（確定済み）
- **単一プロセス**（スレッド分離で各関心を分離）
- **MediatR** でドメインイベント/コマンドのディスパッチ
- **Clean Architecture** + 境界づけられたコンテキスト4つ

## 成果物

### 1. docs/architecture.md
アーキテクチャ設計ドキュメント。以下を含む：
- ソリューション構造図
- レイヤー依存関係（Clean Architecture）
- 各レイヤーの責務とプロジェクト構成
- ドメイン層: 4つの境界づけられたコンテキスト別のエンティティ・値オブジェクト・インターフェース一覧
- アプリケーション層: コマンド/クエリ/イベントハンドラー一覧
- インフラ層: Discord.Net, Slack, SQLite, DPAPI 実装
- オーバーレイ層: OVRSharp, SkiaSharp, トラッキング実装
- ホスト層: DI構成, HostedService一覧
- デスクトップ層: WPF MVVM構成
- スレッディングモデル
- 通知フロー（受信→フィルタ→表示の全ステップ）
- NuGetパッケージ一覧

### 2. .sln + .csproj スキャフォールド
以下のプロジェクト構造を生成：

```
VRNotify.sln
├── src/
│   ├── VRNotify.Domain/           # ドメイン層（純粋C#、依存なし）
│   ├── VRNotify.Application/      # アプリケーション層（MediatR）
│   ├── VRNotify.Infrastructure/   # インフラ層（Discord.Net, Slack, SQLite, DPAPI）
│   ├── VRNotify.Overlay/          # VR表示層（OVRSharp, SkiaSharp）
│   ├── VRNotify.Desktop/          # WPF UI（CommunityToolkit.Mvvm）
│   └── VRNotify.Host/             # 合成ルート（Microsoft.Extensions.Hosting）
└── tests/
    ├── VRNotify.Domain.Tests/
    ├── VRNotify.Application.Tests/
    └── VRNotify.Integration.Tests/
```

各プロジェクトに含める内容：
- `.csproj` ファイル（NuGetパッケージ参照、プロジェクト参照）
- ディレクトリ構造（名前空間に対応するフォルダ）
- **キーインターフェース**: `ISourceAdapter`, `IOverlayRenderer`, `IOverlayManager`, `IFilterChain`, `INotificationQueue`, `INotificationHistory`, `ISettingsRepository`, `ICredentialStore`
- **コアドメインモデル**: `NotificationEvent`, `NotificationCard`, `NotificationSource`, `FilterRule`, `Profile`, 各種Value Object
- **MediatR イベント/コマンド定義**: `NotificationReceivedEvent`, `NotificationCardCreatedEvent`, `SettingsChangedEvent` 等

### 3. memory/ 更新
- `memory/index.yaml` の M004 を完了に更新
- `memory/sessions/` にセッションログ追加

## 実装ステップ

### Step 1: docs/architecture.md 作成
設計ドキュメントを作成（上記の全セクション）

### Step 2: .sln + .csproj 生成
`dotnet new` コマンドで各プロジェクトを生成し、プロジェクト参照を設定

### Step 3: Domain層 実装
- 4コンテキストのフォルダ構造作成
- コアエンティティ・値オブジェクト・インターフェースを実装
- MediatR イベント/コマンド定義

### Step 4: Application層 スケルトン
- コマンド/クエリ/イベントハンドラーの空実装

### Step 5: 他レイヤーのフォルダ構造
- Infrastructure, Overlay, Host, Desktop の基本構造

### Step 6: ビルド確認
`dotnet build` で全プロジェクトのビルドが通ることを確認

### Step 7: memory/ 更新
進捗記録を更新
