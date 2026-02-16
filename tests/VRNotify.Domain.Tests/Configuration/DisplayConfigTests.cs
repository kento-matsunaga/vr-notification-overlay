using FluentAssertions;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Domain.Tests.Configuration;

public class DisplayConfigTests
{
    [Fact]
    public void Default_Position_IsHmdTop()
    {
        var config = new DisplayConfig();

        config.Position.Should().Be(DisplayPosition.HmdTop);
    }

    [Fact]
    public void Default_SlotCount_Is3()
    {
        var config = new DisplayConfig();

        config.SlotCount.Should().Be(3);
    }

    [Fact]
    public void Default_Opacity_Is1()
    {
        var config = new DisplayConfig();

        config.Opacity.Should().Be(1.0f);
    }

    [Fact]
    public void GetHighPriorityDuration_Default_Returns10Seconds()
    {
        var config = new DisplayConfig();

        config.GetHighPriorityDuration().Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GetMediumPriorityDuration_Default_Returns7Seconds()
    {
        var config = new DisplayConfig();

        config.GetMediumPriorityDuration().Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void GetLowPriorityDuration_Default_Returns5Seconds()
    {
        var config = new DisplayConfig();

        config.GetLowPriorityDuration().Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetHighPriorityDuration_CustomValue_ReturnsCustom()
    {
        var config = new DisplayConfig(HighPriorityDuration: TimeSpan.FromSeconds(15));

        config.GetHighPriorityDuration().Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void GetMediumPriorityDuration_CustomValue_ReturnsCustom()
    {
        var config = new DisplayConfig(MediumPriorityDuration: TimeSpan.FromSeconds(12));

        config.GetMediumPriorityDuration().Should().Be(TimeSpan.FromSeconds(12));
    }

    [Fact]
    public void GetLowPriorityDuration_CustomValue_ReturnsCustom()
    {
        var config = new DisplayConfig(LowPriorityDuration: TimeSpan.FromSeconds(3));

        config.GetLowPriorityDuration().Should().Be(TimeSpan.FromSeconds(3));
    }
}
