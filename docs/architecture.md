# VRNotify - Architecture Design
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

## 1. Solution Structure

```
VRNotify.sln
├── src/
│   ├── VRNotify.Domain/           # Domain layer (pure C#, no dependencies)
│   ├── VRNotify.Application/      # Application layer (use cases)
│   ├── VRNotify.Infrastructure/   # Infrastructure (Windows notifications, SQLite, JSON)
│   ├── VRNotify.Overlay/          # VR display (OVRSharp, SkiaSharp)
│   ├── VRNotify.Host/             # Composition root (DI, hosted services)
│   └── VRNotify.Desktop/          # WPF tray app + settings UI (MVVM)
├── tests/
│   ├── VRNotify.Domain.Tests/           # 83 tests
│   ├── VRNotify.Infrastructure.Tests/   # 26 tests
│   ├── VRNotify.Application.Tests/
│   ├── VRNotify.Integration.Tests/
│   ├── VRNotify.Integration.Prototype/
│   ├── VRNotify.Overlay.Prototype/
│   └── VRNotify.NotificationListener.Prototype/
├── installer/                     # NSIS script + SteamVR manifest
├── packaging/                     # AppxManifest.xml + setup scripts
└── docs/                          # BDD / architecture docs
```

## 2. Layer Dependencies (Clean Architecture)

```
          ┌──────────┐
          │  Domain   │  ← No dependencies (pure C#)
          └────▲─────┘
               │
      ┌────────┴────────┐
      │   Application   │  ← Depends on Domain only
      └────▲────────▲───┘
           │        │
    ┌──────┴──┐  ┌──┴─────────┐
    │ Infra-  │  │  Overlay   │  ← Implement Domain interfaces
    │structure│  │            │
    └────▲────┘  └─────▲──────┘
         │             │
         └──────┬──────┘
          ┌─────┴─────┐
          │   Host     │  ← Composition root, references all
          └─────▲──────┘
                │
          ┌─────┴─────┐
          │  Desktop   │  ← WPF entry point + system tray
          └───────────┘
```

**Dependency rules:**
- Domain depends on nothing (pure C#, InternalsVisibleTo for Infrastructure/Tests)
- Application depends on Domain
- Infrastructure implements Domain interfaces, depends on Domain + external packages
- Overlay implements Domain interfaces, depends on Domain + OVRSharp/SkiaSharp
- Host references Application + Infrastructure + Overlay (wires DI)
- Desktop references Host (entry point), uses WPF + CommunityToolkit.Mvvm

## 3. Domain Layer (VRNotify.Domain)

Organized by the 4 bounded contexts. Contains entities, value objects, domain events, and port interfaces.

```
VRNotify.Domain/
├── Common/
│   ├── IDomainEvent.cs
│   └── Entity.cs
│
├── SourceConnection/
│   ├── NotificationSource.cs       # Aggregate root
│   ├── ConnectionState.cs          # Value object (enum + transitions)
│   ├── SourceType.cs               # Value object (enum: Windows | Discord | Slack)
│   ├── ISourceAdapter.cs           # Port interface
│   ├── EncryptedCredential.cs      # Value object
│   ├── ReconnectPolicy.cs          # Value object
│   └── Events/
│       ├── SourceConnectedEvent.cs
│       ├── SourceDisconnectedEvent.cs
│       └── NotificationReceivedEvent.cs
│
├── NotificationProcessing/
│   ├── NotificationEvent.cs        # Value object (normalized notification)
│   ├── NotificationCard.cs         # Entity / Aggregate root (internal reconstitution ctor)
│   ├── NotificationState.cs        # Value object (enum)
│   ├── Priority.cs                 # Value object (enum)
│   ├── SenderInfo.cs               # Value object
│   ├── ChannelInfo.cs              # Value object
│   ├── MessageContent.cs           # Value object
│   ├── MentionType.cs              # Value object (enum)
│   ├── FilterRule.cs               # Value object
│   ├── FilterCondition.cs          # Value object (enum)
│   ├── FilterRuleType.cs           # Enum (AppName | Channel | Keyword | Sender | TimeRange)
│   ├── IFilterChain.cs             # Port interface
│   ├── INotificationQueue.cs       # Port interface
│   ├── INotificationHistory.cs     # Port interface
│   └── Events/
│       ├── NotificationCardCreatedEvent.cs
│       └── NotificationCardExpiredEvent.cs
│
├── VRDisplay/
│   ├── DisplaySlot.cs              # Entity
│   ├── DisplayPosition.cs          # Value object (enum)
│   ├── OverlayConfig.cs            # Value object
│   ├── IOverlayRenderer.cs         # Port interface
│   └── IOverlayManager.cs          # Port interface
│
└── Configuration/
    ├── Profile.cs                  # Entity (internal reconstitution ctor)
    ├── DisplayConfig.cs            # Value object
    ├── DndSettings.cs              # Value object
    ├── DndMode.cs                  # Value object (enum)
    ├── UserSettings.cs             # Entity (internal reconstitution ctor)
    ├── ISettingsRepository.cs      # Port interface
    ├── ICredentialStore.cs         # Port interface
    └── Events/
        ├── SettingsChangedEvent.cs
        └── DndModeChangedEvent.cs
```

### Reconstitution Pattern

Entity classes (NotificationCard, Profile, UserSettings) have `internal` reconstitution constructors
for persistence layer reconstruction. Exposed via `InternalsVisibleTo` to Infrastructure and Infrastructure.Tests.

## 4. Application Layer (VRNotify.Application)

```
VRNotify.Application/
├── Common/
│   └── DomainEventNotification.cs
│
├── SourceConnection/
│   └── Commands/
│       ├── AddSourceCommand.cs        # (stub - future)
│       └── RemoveSourceCommand.cs     # (stub - future)
│
├── NotificationProcessing/
│   ├── Services/
│   │   ├── FilterChainService.cs      # (stub - used by SourceConnectionService directly)
│   │   ├── PriorityResolver.cs        # ✅ All Low for Booth MVP
│   │   └── NotificationBundler.cs     # (stub - future)
│   └── EventHandlers/
│       └── NotificationReceivedEventHandler.cs  # ✅ Full pipeline
│
├── VRDisplay/
│   └── Services/
│       └── DisplaySlotManager.cs      # ✅ Slot allocation
│
└── Configuration/
    ├── Commands/
    │   └── ToggleDndCommand.cs        # ✅ Load → update → save
    └── Queries/
        └── GetSettingsQuery.cs        # ✅ Delegates to ISettingsRepository
```

## 5. Infrastructure Layer (VRNotify.Infrastructure)

```
VRNotify.Infrastructure/
├── Windows/
│   └── WindowsNotificationAdapter.cs    # ✅ ISourceAdapter (UserNotificationListener)
│
├── Persistence/
│   ├── JsonSettingsRepository.cs        # ✅ ISettingsRepository (atomic .tmp → rename)
│   ├── SettingsDtos.cs                  # ✅ DTO ↔ Domain mapping
│   └── SqliteNotificationHistory.cs     # ✅ INotificationHistory (auto-migration)
│
├── Filtering/
│   └── DefaultFilterChain.cs            # ✅ IFilterChain (AppName only, case-insensitive)
│
├── Queuing/
│   └── ChannelNotificationQueue.cs      # ✅ INotificationQueue (System.Threading.Channels)
│
└── Security/
    └── DpapiCredentialStore.cs          # (stub - future, for Bot tokens)
```

## 6. Overlay Layer (VRNotify.Overlay)

```
VRNotify.Overlay/
├── OpenVR/
│   └── OpenVrOverlayManager.cs          # ✅ IOverlayManager (OVRSharp)
│
└── Rendering/
    ├── SkiaNotificationRenderer.cs      # ✅ IOverlayRenderer (SkiaSharp)
    └── NotificationCardLayout.cs        # Card dimensions, font, colors
```

## 7. Host Layer (VRNotify.Host)

```
VRNotify.Host/
├── HostedServices/
│   ├── SourceConnectionService.cs       # ✅ Adapter → Filter → Priority → DND → Card → History → Queue
│   ├── NotificationDisplayService.cs    # ✅ Queue → SlotManager → OverlayManager → auto-hide
│   └── OpenVrHostedService.cs           # ✅ SteamVR polling (5s), init/shutdown
│
└── DependencyInjection/
    └── ServiceRegistration.cs           # ✅ All DI registrations
```

## 8. Desktop Layer (VRNotify.Desktop)

```
VRNotify.Desktop/
├── App.xaml / App.xaml.cs               # ✅ Entry point, system tray (Hardcodet), DI host
├── Views/
│   └── SettingsWindow.xaml / .cs        # ✅ 5-tab settings (General/Filter/Display/History/About)
├── ViewModels/
│   ├── MainViewModel.cs                 # ✅ CommunityToolkit.Mvvm, settings load/save
│   ├── FilterViewModel.cs              # ✅ App list, allow/exclude toggle
│   ├── DisplayViewModel.cs             # ✅ Position, duration, opacity, scale
│   └── HistoryViewModel.cs             # ✅ History list, clear
└── Converters/
    ├── EnumBoolConverter.cs             # ✅ DND radio buttons
    ├── InverseBoolConverter.cs          # ✅ Blocklist mode toggle
    └── AllowedTextConverter.cs          # ✅ "Allowed"/"Excluded" text
```

## 9. Notification Flow (End-to-End)

```
┌──────────────┐    ┌───────────────────────┐    ┌─────────────┐
│ Windows      │───>│ WindowsNotification   │───>│ Notification │
│ Notification │    │ Adapter               │    │ Received     │
│ Listener     │    │ (normalize)           │    │ Event        │
└──────────────┘    └───────────────────────┘    └──────┬──────┘
                                                        │
                                               ┌───────v────────┐
                                               │ SourceConnection│
                                               │ Service         │
                                               ├────────────────┤
                                               │ 1. FilterChain │
                                               │ 2. Priority    │
                                               │ 3. DND check   │
                                               │ 4. Create Card │
                                               │ 5. Save history│
                                               │ 6. Enqueue     │
                                               └───────┬────────┘
                                                       │
                                               ┌───────v────────┐
                                               │ Notification   │
                                               │ Queue          │
                                               │ (Channels<T>)  │
                                               └───────┬────────┘
                                                       │
                                               ┌───────v────────┐
                                               │ NotificationDis│
                                               │ playService    │
                                               ├────────────────┤
                                               │ 1. Dequeue     │
                                               │ 2. Slot assign │
                                               │ 3. Skia render │
                                               │ 4. SetTexture  │
                                               │ 5. Timer→hide  │
                                               └────────────────┘
```

## 10. NuGet Packages

| Project | Package | Purpose |
|---------|---------|---------|
| Domain | *(none)* | Pure C#, no external dependencies |
| Application | *(none)* | Domain interfaces only |
| Infrastructure | Microsoft.Data.Sqlite | Notification history |
| Infrastructure | System.Text.Json | Settings serialization |
| Infrastructure | Serilog | Logging |
| Overlay | OVRSharp | OpenVR overlay API |
| Overlay | SkiaSharp | 2D rendering for notification textures |
| Host | Microsoft.Extensions.Hosting | Generic host, DI, hosted services |
| Desktop | CommunityToolkit.Mvvm | WPF MVVM framework |
| Desktop | Hardcodet.NotifyIcon.Wpf | System tray icon |
| Desktop | Microsoft.Extensions.Hosting | Host builder |
| Desktop | Serilog + Serilog.Sinks.File | File logging |
| Tests | xunit + Moq + FluentAssertions | Testing |

## 11. Threading Model

```
┌─────────────────────────────────────────────────────────┐
│ WPF UI Thread (STA)                                     │
│  - Desktop UI rendering                                 │
│  - Settings window management                           │
│  - System tray icon and context menu                    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ OpenVR HostedService (ThreadPool)                        │
│  - SteamVR connection polling (5s interval)             │
│  - OpenVR initialization and shutdown                   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ SourceConnection HostedService (ThreadPool)              │
│  - Windows notification listener events                 │
│  - Filter → Priority → DND → Card → History → Queue    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ NotificationDisplay HostedService (ThreadPool)           │
│  - Reads from Channel<NotificationCard>                 │
│  - Slot assignment, Skia render, OpenVR texture update  │
│  - Auto-hide timer                                     │
└─────────────────────────────────────────────────────────┘
```

**Inter-thread communication:**
- `Channel<NotificationCard>` — SourceConnectionService → NotificationDisplayService
- `Dispatcher.Invoke()` — for HostedService → WPF UI updates

## 12. Data Storage

### Settings (JSON)
Path: `%APPDATA%/VRNotify/settings.json`

Serialized via SettingsDtos (DTO ↔ Domain mapping).
Atomic write: `.tmp` → `File.Move(overwrite: true)`.

### Notification History (SQLite)
Path: `%APPDATA%/VRNotify/history.db`

Table: `notification_history`
Auto-migration: `CREATE TABLE IF NOT EXISTS` on first access.

### Logs (Serilog)
Path: `%APPDATA%/VRNotify/logs/`

## 13. Packaging & Distribution

```
Build pipeline (build.ps1):
  dotnet publish (self-contained, single-file, win-x64)
    → copy assets (AppxManifest.xml, placeholder.png, manifest.vrmanifest)
    → remove PDBs
    → NSIS makensis → dist/VRNotify-X.Y.Z-Installer.exe
    → Compress-Archive → dist/VRNotify-X.Y.Z-Portable.zip

Installer (NSIS):
  1. File copy → %ProgramFiles%\VRNotify\
  2. Certificate → TrustedPeople store
  3. Sparse MSIX → Add-AppxPackage -Register
  4. Registry → Add/Remove Programs
  5. Start Menu shortcut
  6. Uninstaller generation
```
