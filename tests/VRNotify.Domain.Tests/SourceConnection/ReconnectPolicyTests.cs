using FluentAssertions;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.Tests.SourceConnection;

public class ReconnectPolicyTests
{
    [Fact]
    public void Default_InitialDelay_Is1Second()
    {
        ReconnectPolicy.Default.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Default_MaxDelay_Is60Seconds()
    {
        ReconnectPolicy.Default.MaxDelay.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Default_MaxAttempts_Is10()
    {
        ReconnectPolicy.Default.MaxAttempts.Should().Be(10);
    }

    [Fact]
    public void Default_JitterFactor_Is0Point2()
    {
        ReconnectPolicy.Default.JitterFactor.Should().Be(0.2);
    }

    [Fact]
    public void CustomPolicy_SetsAllValues()
    {
        var policy = new ReconnectPolicy(
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(120),
            MaxAttempts: 5,
            JitterFactor: 0.1);

        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(2));
        policy.MaxDelay.Should().Be(TimeSpan.FromSeconds(120));
        policy.MaxAttempts.Should().Be(5);
        policy.JitterFactor.Should().Be(0.1);
    }
}
