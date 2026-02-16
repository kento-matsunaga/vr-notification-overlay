using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Infrastructure.Filtering;

public sealed class DefaultFilterChain : IFilterChain
{
    public FilterResult Evaluate(NotificationEvent notification, IReadOnlyList<FilterRule> rules)
    {
        throw new NotImplementedException();
    }
}
