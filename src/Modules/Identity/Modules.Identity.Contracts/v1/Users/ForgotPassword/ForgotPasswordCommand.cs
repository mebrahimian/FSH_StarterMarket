using System.Text.Json.Serialization;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;

public class ForgotPasswordCommand : ICommand<string>
{
    public string Email { get; set; } = default!;

    [JsonIgnore]
    public string Origin { get; set; } = default!;
}