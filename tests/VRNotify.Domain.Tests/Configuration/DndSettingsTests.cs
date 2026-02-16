using FluentAssertions;
using VRNotify.Domain.Configuration;

namespace VRNotify.Domain.Tests.Configuration;

public class DndSettingsTests
{
    [Fact]
    public void Default_Mode_IsOff()
    {
        var settings = new DndSettings();

        settings.Mode.Should().Be(DndMode.Off);
    }

    [Fact]
    public void CustomMode_SetsCorrectly()
    {
        var settings = new DndSettings(DndMode.SuppressAll);

        settings.Mode.Should().Be(DndMode.SuppressAll);
    }
}
