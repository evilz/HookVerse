using FluentValidation;
using HookVerse.Api.Models;
using System.Text.Json;

namespace HookVerse.Api.Validators;

/// <summary>
/// Validator for SendWebhookRequest.
/// </summary>
public class SendWebhookRequestValidator : AbstractValidator<SendWebhookRequest>
{
    public SendWebhookRequestValidator()
    {
        RuleFor(x => x.EventTypeId)
            .NotEmpty()
            .WithMessage("EventTypeId is required");

        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("Payload is required")
            .Must(BeValidJson)
            .WithMessage("Payload must be valid JSON")
            .MaximumLength(1048576) // 1MB
            .WithMessage("Payload must not exceed 1MB");

        RuleFor(x => x.ScheduledFor)
            .Must(BeInFuture)
            .When(x => x.ScheduledFor.HasValue)
            .WithMessage("ScheduledFor must be in the future");

        RuleFor(x => x.Metadata)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrWhiteSpace(x.Metadata))
            .WithMessage("Metadata must be valid JSON");
    }

    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool BeInFuture(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return true;

        return dateTime.Value > DateTime.UtcNow;
    }
}
