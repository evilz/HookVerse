using System.Diagnostics.Metrics;

namespace HookVerse.Infrastructure.Metrics;

/// <summary>
/// Metrics for subscription management
/// </summary>
public class SubscriptionMetrics
{
    private readonly Counter<long> _subscriptionCreatedCounter;
    private readonly Counter<long> _subscriptionUpdatedCounter;
    private readonly Counter<long> _subscriptionDeletedCounter;
    private readonly Counter<long> _subscriptionPausedCounter;
    private readonly Counter<long> _subscriptionResumedCounter;
    private readonly ObservableGauge<int> _activeSubscriptionsGauge;
    private readonly Func<int> _getActiveSubscriptionCount;

    public SubscriptionMetrics(IMeterFactory meterFactory, Func<int> getActiveSubscriptionCount)
    {
        var meter = meterFactory.Create("HookVerse.Subscriptions");
        
        _subscriptionCreatedCounter = meter.CreateCounter<long>(
            "subscriptions.created",
            description: "Number of subscriptions created");

        _subscriptionUpdatedCounter = meter.CreateCounter<long>(
            "subscriptions.updated",
            description: "Number of subscriptions updated");

        _subscriptionDeletedCounter = meter.CreateCounter<long>(
            "subscriptions.deleted",
            description: "Number of subscriptions deleted");

        _subscriptionPausedCounter = meter.CreateCounter<long>(
            "subscriptions.paused",
            description: "Number of subscriptions paused");

        _subscriptionResumedCounter = meter.CreateCounter<long>(
            "subscriptions.resumed",
            description: "Number of subscriptions resumed");

        _getActiveSubscriptionCount = getActiveSubscriptionCount;
        
        _activeSubscriptionsGauge = meter.CreateObservableGauge<int>(
            "subscriptions.active",
            observeValue: () => _getActiveSubscriptionCount(),
            description: "Current number of active subscriptions");
    }

    public void RecordSubscriptionCreated(Guid subscriberId, Guid eventTypeId)
    {
        _subscriptionCreatedCounter.Add(1, 
            new KeyValuePair<string, object?>("subscriber_id", subscriberId.ToString()),
            new KeyValuePair<string, object?>("event_type_id", eventTypeId.ToString()));
    }

    public void RecordSubscriptionUpdated(Guid subscriptionId)
    {
        _subscriptionUpdatedCounter.Add(1,
            new KeyValuePair<string, object?>("subscription_id", subscriptionId.ToString()));
    }

    public void RecordSubscriptionDeleted(Guid subscriptionId)
    {
        _subscriptionDeletedCounter.Add(1,
            new KeyValuePair<string, object?>("subscription_id", subscriptionId.ToString()));
    }

    public void RecordSubscriptionPaused(Guid subscriptionId)
    {
        _subscriptionPausedCounter.Add(1,
            new KeyValuePair<string, object?>("subscription_id", subscriptionId.ToString()));
    }

    public void RecordSubscriptionResumed(Guid subscriptionId)
    {
        _subscriptionResumedCounter.Add(1,
            new KeyValuePair<string, object?>("subscription_id", subscriptionId.ToString()));
    }
}
