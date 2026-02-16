namespace VRNotify.Domain.NotificationProcessing;

public interface IFilterChain
{
    FilterResult Evaluate(NotificationEvent notification, IReadOnlyList<FilterRule> rules);
}
