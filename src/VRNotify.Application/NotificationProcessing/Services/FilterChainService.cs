using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Application.NotificationProcessing.Services;

public sealed class FilterChainService : IFilterChain
{
    public FilterResult Evaluate(NotificationEvent notification, IReadOnlyList<FilterRule> rules)
    {
        throw new NotImplementedException();
    }
}
