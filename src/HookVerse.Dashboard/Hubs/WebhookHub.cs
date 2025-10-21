using Microsoft.AspNetCore.SignalR;

namespace HookVerse.Dashboard.Hubs;

/// <summary>
/// SignalR hub for real-time webhook delivery notifications.
/// </summary>
public class WebhookHub : Hub
{
    /// <summary>
    /// Notify clients about a webhook delivery completion.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="webhookId">The webhook event ID.</param>
    /// <param name="status">The delivery status.</param>
    /// <param name="message">Optional status message.</param>
    public async Task NotifyWebhookDelivered(Guid subscriberId, Guid webhookId, string status, string? message = null)
    {
        await Clients.Group(subscriberId.ToString()).SendAsync("WebhookDelivered", new
        {
            WebhookId = webhookId,
            Status = status,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify clients about a delivery attempt.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="webhookId">The webhook event ID.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <param name="status">The attempt status.</param>
    public async Task NotifyDeliveryAttempt(Guid subscriberId, Guid webhookId, int attemptNumber, string status)
    {
        await Clients.Group(subscriberId.ToString()).SendAsync("DeliveryAttempt", new
        {
            WebhookId = webhookId,
            AttemptNumber = attemptNumber,
            Status = status,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Join subscriber group for targeted notifications.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID to join.</param>
    public async Task JoinSubscriberGroup(string subscriberId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, subscriberId);
    }

    /// <summary>
    /// Leave subscriber group.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID to leave.</param>
    public async Task LeaveSubscriberGroup(string subscriberId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, subscriberId);
    }
}
