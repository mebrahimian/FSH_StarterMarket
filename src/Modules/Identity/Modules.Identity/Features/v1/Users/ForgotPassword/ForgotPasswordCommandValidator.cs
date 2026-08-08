using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;

namespace FSH.Modules.Identity.Features.v1.Users.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(p => p.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        RuleFor(p => p.Origin)
            .NotEmpty()
            .Must(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid password reset origin is required.");
    }
}