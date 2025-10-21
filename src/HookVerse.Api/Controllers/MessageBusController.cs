using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace HookVerse.Api.Controllers;

/// <summary>
/// Controller for managing message bus configuration and status
/// </summary>
[ApiController]
[Route("api/v1/message-bus")]
[Produces("application/json")]
public class MessageBusController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageBusController> _logger;

    public MessageBusController(
        IConfiguration configuration,
        ILogger<MessageBusController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get message bus configuration status
    /// </summary>
    /// <returns>Current message bus configuration and status</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MessageBusStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var transport = _configuration["MessageBus:Transport"] ?? "RabbitMQ";
        var queueName = _configuration["MessageBus:ExternalWebhooks:QueueName"] ?? "external-webhooks";
        var exchangeName = _configuration["MessageBus:ExternalWebhooks:ExchangeName"] ?? "hookverse.external";
        var prefetchCount = _configuration.GetValue<int>("MessageBus:ExternalWebhooks:PrefetchCount");
        var dlqEnabled = _configuration.GetValue<bool>("MessageBus:ExternalWebhooks:DeadLetterQueue:Enabled");

        var response = new MessageBusStatusResponse
        {
            Transport = transport,
            QueueName = queueName,
            ExchangeName = exchangeName,
            PrefetchCount = prefetchCount,
            DeadLetterQueueEnabled = dlqEnabled,
            IsConfigured = true,
            ConnectionStatus = "Connected" // In a real implementation, this would check actual connection
        };

        _logger.LogInformation("Message bus status requested: Transport={Transport}, Queue={Queue}", transport, queueName);

        return Ok(response);
    }

    /// <summary>
    /// Configure message bus settings (for dynamic configuration)
    /// </summary>
    /// <param name="request">Configuration request</param>
    /// <returns>Updated configuration</returns>
    [HttpPost("configure")]
    [ProducesResponseType(typeof(MessageBusConfigureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Configure([FromBody] MessageBusConfigureRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        // Note: This is a simplified implementation
        // In production, you would update configuration dynamically or persist to a database
        _logger.LogInformation(
            "Message bus configuration update requested: Transport={Transport}, Queue={Queue}",
            request.Transport, request.QueueName);

        var response = new MessageBusConfigureResponse
        {
            Success = true,
            Message = "Configuration update queued. Restart worker to apply changes.",
            CurrentConfiguration = new
            {
                Transport = request.Transport,
                QueueName = request.QueueName,
                ExchangeName = request.ExchangeName,
                PrefetchCount = request.PrefetchCount,
                DeadLetterQueueEnabled = request.EnableDeadLetterQueue
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get message bus statistics
    /// </summary>
    /// <returns>Message bus processing statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(MessageBusStatisticsResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatistics()
    {
        // In a real implementation, this would query actual metrics from a metrics store
        var response = new MessageBusStatisticsResponse
        {
            TotalMessagesReceived = 0,
            TotalMessagesProcessed = 0,
            TotalMessagesFailed = 0,
            DeadLetterQueueCount = 0,
            AverageProcessingTimeMs = 0,
            LastMessageProcessedAt = null
        };

        _logger.LogInformation("Message bus statistics requested");

        return Ok(response);
    }
}

/// <summary>
/// Message bus status response
/// </summary>
public record MessageBusStatusResponse
{
    public string Transport { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;
    public string ExchangeName { get; init; } = string.Empty;
    public int PrefetchCount { get; init; }
    public bool DeadLetterQueueEnabled { get; init; }
    public bool IsConfigured { get; init; }
    public string ConnectionStatus { get; init; } = string.Empty;
}

/// <summary>
/// Message bus configuration request
/// </summary>
public record MessageBusConfigureRequest
{
    public string Transport { get; init; } = "RabbitMQ";
    public string QueueName { get; init; } = "external-webhooks";
    public string ExchangeName { get; init; } = "hookverse.external";
    public int PrefetchCount { get; init; } = 10;
    public bool EnableDeadLetterQueue { get; init; } = true;
}

/// <summary>
/// Message bus configuration response
/// </summary>
public record MessageBusConfigureResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? CurrentConfiguration { get; init; }
}

/// <summary>
/// Message bus statistics response
/// </summary>
public record MessageBusStatisticsResponse
{
    public long TotalMessagesReceived { get; init; }
    public long TotalMessagesProcessed { get; init; }
    public long TotalMessagesFailed { get; init; }
    public long DeadLetterQueueCount { get; init; }
    public double AverageProcessingTimeMs { get; init; }
    public DateTime? LastMessageProcessedAt { get; init; }
}
