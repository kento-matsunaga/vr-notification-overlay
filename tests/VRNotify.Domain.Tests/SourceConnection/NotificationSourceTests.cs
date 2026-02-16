using FluentAssertions;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.Tests.SourceConnection;

public class NotificationSourceTests
{
    private static NotificationSource CreateSource(
        SourceType type = SourceType.Discord,
        string name = "My Discord") =>
        new(Guid.NewGuid(), type, name);

    [Fact]
    public void Constructor_SetsInitialState()
    {
        var id = Guid.NewGuid();
        var source = new NotificationSource(id, SourceType.Discord, "Test");

        source.SourceId.Should().Be(id);
        source.SourceType.Should().Be(SourceType.Discord);
        source.DisplayName.Should().Be("Test");
        source.ConnectionState.Should().Be(ConnectionState.Disconnected);
        source.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void TransitionTo_Connecting_UpdatesState()
    {
        var source = CreateSource();

        source.TransitionTo(ConnectionState.Connecting);

        source.ConnectionState.Should().Be(ConnectionState.Connecting);
    }

    [Fact]
    public void TransitionTo_Connected_UpdatesState()
    {
        var source = CreateSource();

        source.TransitionTo(ConnectionState.Connected);

        source.ConnectionState.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public void TransitionTo_Reconnecting_UpdatesState()
    {
        var source = CreateSource();

        source.TransitionTo(ConnectionState.Reconnecting);

        source.ConnectionState.Should().Be(ConnectionState.Reconnecting);
    }

    [Fact]
    public void TransitionTo_Disconnected_UpdatesState()
    {
        var source = CreateSource();
        source.TransitionTo(ConnectionState.Connected);

        source.TransitionTo(ConnectionState.Disconnected);

        source.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public void Disable_SetsIsEnabledToFalse()
    {
        var source = CreateSource();

        source.Disable();

        source.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_AfterDisable_SetsIsEnabledToTrue()
    {
        var source = CreateSource();
        source.Disable();

        source.Enable();

        source.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void UpdateDisplayName_ChangesName()
    {
        var source = CreateSource(name: "Old Name");

        source.UpdateDisplayName("New Name");

        source.DisplayName.Should().Be("New Name");
    }
}
