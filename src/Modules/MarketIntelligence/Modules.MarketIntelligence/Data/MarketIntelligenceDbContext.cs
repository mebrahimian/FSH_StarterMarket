using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Data.Views;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Insights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data;

public sealed class MarketIntelligenceDbContext : BaseDbContext
{
    public const string Schema = "marketintelligence";

    public MarketIntelligenceDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<MarketIntelligenceDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Disclosure> Disclosures => Set<Disclosure>();
    public DbSet<MonthlyActivitySummary> MonthlyActivitySummaries =>  Set<MonthlyActivitySummary>();
    public DbSet<PortfolioCompanyAlias> PortfolioCompanyAliases => Set<PortfolioCompanyAlias>();
    public DbSet<InvestmentPortfolioPosition> InvestmentPortfolioPositions => Set<InvestmentPortfolioPosition>();
    public DbSet<InvestmentPortfolioReportMetadata> InvestmentPortfolioReportMetadata => Set<InvestmentPortfolioReportMetadata>();
    public DbSet<CodalIncrementalScheduleSetting> CodalIncrementalScheduleSettings => Set<CodalIncrementalScheduleSetting>();
    public DbSet<Insight> Insights => Set<Insight>();
    public DbSet<SalesPerformanceSnapshot> SalesPerformanceSnapshots => Set<SalesPerformanceSnapshot>();
    public DbSet<CompanyMasterView> CompanyMaster => Set<CompanyMasterView>();
    public DbSet<CodalCompanyImport> CodalCompanyImports => Set<CodalCompanyImport>();
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<PortfolioHoldingAsset> PortfolioHoldingAssets => Set<PortfolioHoldingAsset>();
    public DbSet<InvestmentPortfolioHoldingPeriod> InvestmentPortfolioHoldingPeriods => Set<InvestmentPortfolioHoldingPeriod>();
    public DbSet<ExternalSourceSetting> ExternalSourceSettings => Set<ExternalSourceSetting>();
    public DbSet<TsetmcInstrument> TsetmcInstruments
       => Set<TsetmcInstrument>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketIntelligenceDbContext).Assembly);
        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply sees
        // fully-configured entities (including HasMany child types like ProductImage).
        base.OnModelCreating(modelBuilder);
    }
}
