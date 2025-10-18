using FluentValidation;
using HookVerse.Api.Models;

namespace HookVerse.Api.Validators;

/// <summary>
/// Validator for CreateSubscriptionRequest
/// </summary>
public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.EventTypeId)
            .NotEmpty()
            .WithMessage("Event type ID is required");

        RuleFor(x => x.EndpointUrl)
            .NotEmpty()
            .WithMessage("Endpoint URL is required")
            .Must(BeValidHttpsUrl)
            .WithMessage("Endpoint URL must be a valid HTTPS URL");

        RuleFor(x => x.Secret)
            .NotEmpty()
            .WithMessage("Secret is required")
            .MinimumLength(32)
            .WithMessage("Secret must be at least 32 characters long");

        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(1, 300)
            .WithMessage("Timeout must be between 1 and 300 seconds");

        RuleFor(x => x.MaxRetries)
            .InclusiveBetween(0, 10)
            .WithMessage("Max retries must be between 0 and 10");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 500 characters");
    }

    private static bool BeValidHttpsUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps;
    }
}
