using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Files.Contracts;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.MarketIntelligence.MarketIntelligenceModule), 600)]

namespace FSH.Modules.MarketIntelligence
{
#pragma warning disable S2094 // Empty classes should not be used
    public class MarketIntelligenceModule :IModule
    {
        public void ConfigureServices(IHostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            PermissionConstants.Register(MarketIntelligencePermissions.All);

            builder.Services.AddHeroDbContext<MarketIntelligenceDbContext>();
            builder.Services.AddScoped<IDbInitializer, MarketIntelligenceDbInitializer>();

            

            builder.Services.AddHealthChecks()
                .AddDbContextCheck<MarketIntelligenceDbContext>(
                    name: "db:catalog",
                    failureStatus: HealthStatus.Unhealthy);
        }

    }
#pragma warning restore S2094
}
