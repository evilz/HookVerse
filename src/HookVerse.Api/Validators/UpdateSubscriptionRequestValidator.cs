using FluentValidation;
using HookVerse.Api.Models;

namespace HookVerse.Api.Validators;

/// <summary>
/// Validator for UpdateSubscriptionRequest
/// </summary>
public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.EndpointUrl)
            .Must(BeValidHttpsUrl)
            .When(x => !string.IsNullOrEmpty(x.EndpointUrl))
            .WithMessage("Endpoint URL must be a valid HTTPS URL");

        RuleFor(x => x.Secret)
            .MinimumLength(32)
            .When(x => !string.IsNullOrEmpty(x.Secret))
            .WithMessage("Secret must be at least 32 characters long");

        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(1, 300)
            .When(x => x.TimeoutSeconds.HasValue)
            .WithMessage("Timeout must be between 1 and 300 seconds");

        RuleFor(x => x.MaxRetries)
            .InclusiveBetween(0, 10)
            .When(x => x.MaxRetries.HasValue)
            .WithMessage("Max retries must be between 0 and 10");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 500 characters");
    }

    private static bool BeValidHttpsUrl(string? url)
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
