# VRNotify - Architecture Design

## 1. Solution Structure

```
VRNotify.sln
├── src/
│   ├── VRNotify.Domain/           # Domain layer (pure C#, no dependencies)
│   ├── VRNotify.Application/      # Application layer (use cases, MediatR)
│   ├── VRNotify.Infrastructure/   # Infrastructure (Discord.Net, Slack, SQLite, DPAPI)
│   ├── VRNotify.Overlay/          # VR display (OVRSharp, SkiaSharp)
│   ├── VRNotify.Desktop/          # WPF settings UI (MVVM)
│   └── VRNotify.Host/             # Composition root (DI, hosted services)
└── tests/
    ├── VRNotify.Domain.Tests/
    ├── VRNotify.Application.Tests/
    └── VRNotify.Integration.Tests/
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
          │  Desktop   │  ← WPF entry point
          └───────────┘
```

**Dependency rules:**
- Domain depends on nothing (pure C# with no NuGet references)
- Application depends on Domain + MediatR
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
│   ├── SourceType.cs               # Value object (enum)
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
│   ├── NotificationCard.cs         # Entity / Aggregate root
│   ├── NotificationState.cs        # Value object (enum)
│   ├── Priority.cs                 # Value object (enum)
│   ├── SenderInfo.cs               # Value object
│   ├── ChannelInfo.cs              # Value object
│   ├── MessageContent.cs           # Value object
│   ├── MentionType.cs              # Value object (enum)
│   ├── FilterRule.cs               # Value object
│   ├── FilterCondition.cs          # Value object (enum)
│   ├── FilterRuleType.cs           # Enum
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
    ├── Profile.cs                  # Entity
    ├── DisplayConfig.cs            # Value object
    ├── DndSettings.cs              # Value object
    ├── DndMode.cs                  # Value object (enum)
    ├── UserSettings.cs             # Entity
    ├── ISettingsRepository.cs      # Port interface
    ├── ICredentialStore.cs         # Port interface
    └── Events/
        ├── SettingsChangedEvent.cs
        └── DndModeChangedEvent.cs
```

### Key Domain Interfaces (Ports)

```csharp
// Adapter for external notification services (Discord, Slack, etc.)
public interface ISourceAdapter : IAsyncDisposable
{
    SourceType SourceType { get; }
    ConnectionState State { get; }
    event Func<NotificationEvent, Task> NotificationReceived;
    event Func<ConnectionState, Task> ConnectionStateChanged;
    Task ConnectAsync(EncryptedCredential credential, CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
}

// Filter chain evaluation
public interface IFilterChain
{
    FilterResult Evaluate(NotificationEvent notification, IReadOnlyList<FilterRule> rules);
}

// Notification queue (producer-consumer)
public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationCard card, CancellationToken ct);
    IAsyncEnumerable<NotificationCard> DequeueAllAsync(CancellationToken ct);
    int Count { get; }
}

// Notification history persistence
public interface INotificationHistory
{
    Task SaveAsync(NotificationCard card, CancellationToken ct);
    Task<IReadOnlyList<NotificationCard>> GetRecentAsync(int count, CancellationToken ct);
    Task PurgeOldEntriesAsync(TimeSpan maxAge, int maxCount, CancellationToken ct);
}

// OpenVR overlay management
public interface IOverlayManager : IAsyncDisposable
{
    bool IsAvailable { get; }
    Task InitializeAsync(CancellationToken ct);
    Task ShowNotificationAsync(NotificationCard card, DisplaySlot slot, CancellationToken ct);
    Task HideSlotAsync(DisplaySlot slot, CancellationToken ct);
    Task UpdatePositionAsync(DisplayPosition position, CancellationToken ct);
}

// Overlay texture rendering
public interface IOverlayRenderer
{
    byte[] RenderCard(NotificationCard card, int width, int height);
}

// Settings persistence
public interface ISettingsRepository
{
    Task<UserSettings> LoadAsync(CancellationToken ct);
    Task SaveAsync(UserSettings settings, CancellationToken ct);
}

// Credential encryption
public interface ICredentialStore
{
    Task<EncryptedCredential> EncryptAsync(string plainText, CancellationToken ct);
    Task<string> DecryptAsync(EncryptedCredential credential, CancellationToken ct);
}
```

## 4. Application Layer (VRNotify.Application)

Contains use cases implemented as MediatR commands/queries/event handlers. No direct infrastructure dependencies.

```
VRNotify.Application/
├── SourceConnection/
│   ├── Commands/
│   │   ├── AddSourceCommand.cs + AddSourceCommandHandler.cs
│   │   ├── RemoveSourceCommand.cs + RemoveSourceCommandHandler.cs
│   │   ├── EnableSourceCommand.cs + EnableSourceCommandHandler.cs
│   │   └── DisableSourceCommand.cs + DisableSourceCommandHandler.cs
│   └── EventHandlers/
│       └── NotificationReceivedEventHandler.cs
│
├── NotificationProcessing/
│   ├── Services/
│   │   ├── FilterChainService.cs
│   │   ├── PriorityResolver.cs
│   │   └── NotificationBundler.cs
│   └── EventHandlers/
│       └── NotificationCardCreatedEventHandler.cs
│
├── VRDisplay/
│   ├── Services/
│   │   └── DisplaySlotManager.cs
│   └── EventHandlers/
│       └── DisplayNotificationEventHandler.cs
│
└── Configuration/
    ├── Commands/
    │   ├── UpdateSettingsCommand.cs + Handler
    │   ├── SwitchProfileCommand.cs + Handler
    │   └── ToggleDndCommand.cs + Handler
    └── Queries/
        ├── GetSettingsQuery.cs + Handler
        └── GetNotificationHistoryQuery.cs + Handler
```

### Notification Processing Pipeline

```
NotificationReceivedEventHandler (central orchestrator):
  1. Receive NotificationEvent from source adapter
  2. Check DND mode → if full-suppress, skip to step 6
  3. Evaluate FilterChain → pass/block
  4. Resolve Priority (DM/mention/keyword rules)
  5. Check bundling (same sender within 3 seconds)
  6. Create NotificationCard
  7. Save to NotificationHistory
  8. If display allowed → publish NotificationCardCreatedEvent
```

## 5. Infrastructure Layer (VRNotify.Infrastructure)

Implements domain ports with concrete technologies.

```
VRNotify.Infrastructure/
├── Discord/
│   ├── DiscordSourceAdapter.cs          # ISourceAdapter implementation
│   ├── DiscordEventMapper.cs            # SocketMessage → NotificationEvent
│   ├── DiscordOAuth2Handler.cs          # OAuth2 flow for desktop setup
│   └── DiscordReconnectPolicy.cs        # Exponential backoff with jitter
│
├── Slack/
│   ├── SlackSourceAdapter.cs            # ISourceAdapter implementation
│   ├── SlackEventMapper.cs              # Slack event → NotificationEvent
│   └── SlackReconnectPolicy.cs          # Exponential backoff with jitter
│
├── Persistence/
│   ├── JsonSettingsRepository.cs        # ISettingsRepository (atomic .tmp → rename)
│   ├── SqliteNotificationHistory.cs     # INotificationHistory
│   ├── SettingsMigrator.cs              # Schema version migration
│   └── Migrations/
│       └── V1_InitialSchema.cs
│
├── Security/
│   └── DpapiCredentialStore.cs          # ICredentialStore (Windows DPAPI)
│
├── Queuing/
│   └── ChannelNotificationQueue.cs      # INotificationQueue (System.Threading.Channels)
│
├── Filtering/
│   └── DefaultFilterChain.cs            # IFilterChain implementation
│
└── Audio/
    └── SteamVrAudioPlayer.cs            # Notification sound via SteamVR audio
```

## 6. Overlay Layer (VRNotify.Overlay)

OpenVR overlay rendering and tracking.

```
VRNotify.Overlay/
├── OpenVR/
│   ├── OpenVrOverlayManager.cs          # IOverlayManager implementation
│   ├── OpenVrLifecycleService.cs        # Init, shutdown, VR event polling
│   └── SteamVrMonitor.cs               # Detect SteamVR start/stop
│
├── Rendering/
│   ├── SkiaNotificationRenderer.cs      # IOverlayRenderer implementation
│   ├── TexturePool.cs                   # Reusable texture allocation (max 20)
│   └── NotificationCardLayout.cs        # Card dimensions, font, colors
│
└── Tracking/
    ├── IPositionTracker.cs              # Strategy interface
    ├── HmdFollowTracker.cs              # HMD-follow with SmoothDamp
    ├── WristAttachTracker.cs            # Left wrist attachment
    └── WorldFixedTracker.cs             # World-space fixed position
```

## 7. Host Layer (VRNotify.Host)

Composition root. Wires all dependencies and manages application lifecycle.

```
VRNotify.Host/
├── HostedServices/
│   ├── OpenVrHostedService.cs           # OpenVR lifecycle (init, poll, shutdown)
│   ├── SourceConnectionService.cs       # Manages source adapter connections
│   ├── NotificationDisplayService.cs    # Dequeue → render → display loop
│   └── SteamVrWatcherService.cs         # Monitors SteamVR start/stop
│
├── DependencyInjection/
│   └── ServiceRegistration.cs           # All DI registrations
│
└── Logging/
    └── SerilogConfiguration.cs          # Serilog setup (file sink, rotation)
```

## 8. Desktop Layer (VRNotify.Desktop)

WPF application with MVVM pattern using CommunityToolkit.Mvvm.

```
VRNotify.Desktop/
├── App.xaml / App.xaml.cs               # Entry point, creates Host
├── Views/
│   ├── MainWindow.xaml
│   ├── SetupWizard/
│   │   ├── WizardWindow.xaml
│   │   ├── ServiceConnectionPage.xaml
│   │   ├── ChannelSelectionPage.xaml
│   │   └── DisplaySettingsPage.xaml
│   ├── SettingsView.xaml
│   └── HistoryView.xaml
│
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── SetupWizardViewModel.cs
│   ├── SettingsViewModel.cs
│   └── HistoryViewModel.cs
│
└── SystemTray/
    └── TrayIconManager.cs               # System tray icon and context menu
```

## 9. Threading Model

```
┌─────────────────────────────────────────────────────────┐
│ WPF UI Thread (STA)                                     │
│  - Desktop UI rendering                                 │
│  - Settings changes → MediatR → repository              │
│  - System tray management                               │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ OpenVR Thread (dedicated, via HostedService)             │
│  - OpenVR event polling                                 │
│  - Pose tracking (WaitGetPoses / HMD proximity sensor)  │
│  - Texture updates (SetOverlayTexture)                  │
│  - 30fps when displaying notifications, idle otherwise  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Discord Gateway Thread (async, managed by Discord.Net)  │
│  - WebSocket message receive                            │
│  - Event → Channel<NotificationEvent> (non-blocking)    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Slack Socket Mode Thread (async)                        │
│  - WebSocket message receive                            │
│  - Event → Channel<NotificationEvent> (non-blocking)    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Notification Processing (ThreadPool via HostedService)  │
│  - Reads from Channel<NotificationEvent>                │
│  - Filter evaluation, priority resolution               │
│  - Card creation, history save                          │
│  - Writes to Channel<NotificationCard> for display      │
└─────────────────────────────────────────────────────────┘
```

**Inter-thread communication:**
- `Channel<NotificationEvent>` — source adapters → notification processor
- `Channel<NotificationCard>` — notification processor → overlay display
- MediatR — for in-process pub/sub within same thread context
- `IProgress<T>` / Dispatcher — for thread pool → WPF UI updates

## 10. Notification Flow (End-to-End)

```
┌──────────────┐    ┌───────────────┐    ┌─────────────┐
│ Discord/Slack │───>│ SourceAdapter │───>│ Channel<NE> │
│   WebSocket   │    │  (normalize)  │    │  (buffer)   │
└──────────────┘    └───────────────┘    └──────┬──────┘
                                                │
                                                v
                                     ┌──────────────────┐
                                     │ NotificationProc  │
                                     │  HostedService    │
                                     ├──────────────────┤
                                     │ 1. DND check     │
                                     │ 2. FilterChain   │
                                     │ 3. Priority      │
                                     │ 4. Bundling      │
                                     │ 5. Create Card   │
                                     │ 6. Save history  │
                                     └────────┬─────────┘
                                              │
                                              v
                                     ┌──────────────────┐
                                     │ Channel<NC>      │
                                     │  (display queue) │
                                     └────────┬─────────┘
                                              │
                                              v
                                     ┌──────────────────┐
                                     │ DisplayService   │
                                     │  (OpenVR thread) │
                                     ├──────────────────┤
                                     │ 1. Slot assign   │
                                     │ 2. Preemption    │
                                     │ 3. Skia render   │
                                     │ 4. SetTexture    │
                                     │ 5. Timer→hide    │
                                     └──────────────────┘
```

**NE = NotificationEvent, NC = NotificationCard**

### Detailed Steps

1. **Receive**: Discord.Net/Slack client receives WebSocket message
2. **Normalize**: SourceAdapter maps service-specific payload → `NotificationEvent` (value object)
3. **Buffer**: Write to `Channel<NotificationEvent>` (non-blocking, bounded capacity 100)
4. **Process** (NotificationProcessingService reads from channel):
   a. **DND check**: If DND=full-suppress → save to history only, skip display
   b. **Filter**: `FilterChainService.Evaluate()` — sequential rule matching, first match wins
   c. **Priority**: `PriorityResolver.Resolve()` — DM/mention=High, @here=Medium, keyword=configurable, default=Low
   d. **Bundle**: `NotificationBundler.TryBundle()` — same sender within 3s → update existing card
   e. **Create**: New `NotificationCard` with calculated priority and display duration
   f. **Persist**: `INotificationHistory.SaveAsync()` — SQLite write
   g. **Enqueue**: Write to `Channel<NotificationCard>` for display
5. **Display** (NotificationDisplayService on OpenVR thread):
   a. **Slot assignment**: `DisplaySlotManager` finds empty slot or preempts low-priority
   b. **Render**: `SkiaNotificationRenderer.RenderCard()` — produces pixel buffer
   c. **Show**: `OpenVrOverlayManager.ShowNotificationAsync()` — `SetOverlayTexture()`
   d. **Timer**: After display duration → `HideSlotAsync()` → texture back to pool
   e. **Publish**: `NotificationCardExpiredEvent` → mark as Read

## 11. NuGet Packages

| Project | Package | Purpose |
|---------|---------|---------|
| Domain | *(none)* | Pure C#, no external dependencies |
| Application | MediatR | Command/query/event dispatch |
| Infrastructure | Discord.Net | Discord Gateway + REST |
| Infrastructure | SlackNet | Slack Socket Mode WebSocket |
| Infrastructure | Microsoft.Data.Sqlite | Notification history |
| Infrastructure | System.Security.Cryptography.ProtectedData | DPAPI token encryption |
| Infrastructure | Serilog + Serilog.Sinks.File | Structured logging |
| Overlay | OVRSharp | OpenVR overlay API |
| Overlay | SkiaSharp | 2D rendering for notification textures |
| Host | Microsoft.Extensions.Hosting | Generic host, DI, hosted services |
| Host | Serilog.Extensions.Hosting | Serilog integration with host |
| Desktop | CommunityToolkit.Mvvm | WPF MVVM framework |
| Tests | xunit + Moq + FluentAssertions | Testing |

## 12. Configuration File Structure

```json
{
  "schemaVersion": "1.0",
  "activeProfileId": "00000000-0000-0000-0000-000000000000",
  "sources": [
    {
      "sourceId": "...",
      "sourceType": "Discord",
      "displayName": "Personal Discord",
      "isEnabled": true,
      "credential": "<DPAPI encrypted base64>"
    }
  ],
  "profiles": [
    {
      "profileId": "...",
      "name": "Default",
      "isDefault": true,
      "display": {
        "position": "HmdTop",
        "slotCount": 3,
        "opacity": 1.0,
        "scale": 1.0,
        "highPriorityDuration": 10,
        "mediumPriorityDuration": 7,
        "lowPriorityDuration": 5
      },
      "dnd": {
        "isEnabled": false,
        "mode": "SuppressAll"
      },
      "filterRules": [],
      "enabledSourceIds": []
    }
  ],
  "audio": {
    "isEnabled": true,
    "volume": 0.3
  },
  "history": {
    "retentionDays": 7,
    "maxEntries": 1000
  }
}
```

## 13. Error Handling Strategy

| Layer | Strategy |
|-------|----------|
| Domain | Throw domain exceptions (`DomainException` hierarchy) |
| Application | Catch domain exceptions, return Result<T> or re-throw |
| Infrastructure | Wrap external exceptions into domain-meaningful exceptions |
| Host | Global exception handler, log and continue for non-fatal |
| Overlay | OpenVR errors → graceful degradation (retry or disable overlay) |
| Desktop | Display user-friendly error messages via ViewModel |

### Reconnection Policy
- Exponential backoff: 1s → 2s → 4s → 8s → 16s → 32s → 60s (cap)
- Jitter: ±20% random variation on each interval
- Max attempts: 10
- After max: enter periodic retry mode (5min interval)
- Auth errors (401/403): stop retrying, notify user

## 14. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Process model | Single process | Simpler than sidecar; IPC not needed for MVP |
| Event bus | MediatR | Standard .NET choice; clean pub/sub |
| Async queues | System.Threading.Channels | High-performance, bounded, backpressure |
| Settings format | JSON | Human-readable, easy migration |
| History storage | SQLite | Structured queries, no external server |
| Token security | Windows DPAPI | OS-level encryption, no key management |
| Overlay rendering | SkiaSharp → DXGI | High-quality text, efficient texture sharing |
| Tracking | Strategy pattern | Easy to switch HMD/wrist/world-fixed |
| WPF MVVM | CommunityToolkit.Mvvm | Source generators, minimal boilerplate |
| Logging | Serilog | Structured logging, file rotation built-in |
