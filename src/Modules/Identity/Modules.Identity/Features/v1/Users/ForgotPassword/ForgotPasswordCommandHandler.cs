using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, string>
{
    private readonly IUserService _userService;

    public ForgotPasswordCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<string> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Origin))
        {
            throw new InvalidOperationException("Password reset origin is not configured.");
        }

        await _userService
            .ForgotPasswordAsync(command.Email, command.Origin, cancellationToken)
            .ConfigureAwait(false);

        return "Password reset email sent.";
    }
}