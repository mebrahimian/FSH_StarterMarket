using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Cors;
using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Features.v1.Users.ForgotPassword;

public static class ForgotPasswordEndpoint
{
    internal static RouteHandlerBuilder MapForgotPasswordEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/forgot-password", async (
            HttpRequest request,
            [FromHeader(Name = MultitenancyConstants.Identifier)] string tenant,
            [FromBody] ForgotPasswordCommand command,
            IOptions<CorsOptions> corsOptions,
            IOptions<OriginOptions> originOptions,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var requestOrigin = request.Headers.Origin.ToString();

            if (!string.IsNullOrWhiteSpace(requestOrigin))
            {
                requestOrigin = requestOrigin.TrimEnd('/');

                var cors = corsOptions.Value;

                if (!cors.AllowAll &&
                    !cors.AllowedOrigins.Any(
                        allowedOrigin =>
                            string.Equals(
                                allowedOrigin.TrimEnd('/'),
                                requestOrigin,
                                StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.BadRequest("Request origin is not allowed.");
                }

                command.Origin = requestOrigin;
            }
            else
            {
                var configuredOrigin = originOptions.Value.OriginUrl;

                if (configuredOrigin is null)
                {
                    return Results.BadRequest("Origin URL is not configured.");
                }

                command.Origin = configuredOrigin.AbsoluteUri.TrimEnd('/');
            }

            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("RequestPasswordReset")
        .WithSummary("Request password reset")
        .WithDescription("Generate a password reset token and send it via email.")
        .AllowAnonymous()
        .Produces(StatusCodes.Status200OK);
    }
}