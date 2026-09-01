using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Data;

public sealed class MarketIntelligenceDbInitializer(
    MarketIntelligenceDbContext dbContext,
    ILogger<MarketIntelligenceDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[MarketIntelligence] applied migrations");
        }
    }

    /// <summary>
    /// MarketIntelligence has NO per-tenant auto-seed. A fresh tenant comes up with an empty
    /// MarketIntelligence and is expected to be populated by the operator via the API / UI.
    /// Demo content for the <c>acme</c> and <c>globex</c> tenants lives in the
    /// DbMigrator's <c>seed-demo</c> command, which calls
    /// </summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
