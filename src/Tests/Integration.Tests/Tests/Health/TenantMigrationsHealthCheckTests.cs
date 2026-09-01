using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Integration.Tests.Tests.Health;

/// <summary>
/// Direct unit tests for <see cref="TenantMigrationsHealthCheck"/> — the production-readiness
/// gate added when API-side auto-migration was removed. Asserts that the check returns
/// <c>Unhealthy</c> against an unmigrated database and <c>Healthy</c> after the schema is
/// brought to head (the exact transition <c>FSH.Starter.DbMigrator</c> performs in production).
/// Uses a real Postgres testcontainer + an in-memory <see cref="IMultiTenantStore{TTenantInfo}"/>
/// to keep the test independent of the shared <c>FshWebApplicationFactory</c> — avoiding
/// the global Hangfire state that would otherwise leak across test classes.
/// </summary>
public sealed class TenantMigrationsHealthCheckTests : IAsyncLifetime
{
    private readonly MsSqlContainer _mssql = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
    .WithAutoRemove(true)
    .WithCleanUp(true)
    .Build();

    public async Task InitializeAsync()
{
    await _mssql.StartAsync();

    await _mssql.ExecScriptAsync(
        "IF DB_ID(N'fsh_migrations_check') IS NULL CREATE DATABASE [fsh_migrations_check];");
}

       public Task DisposeAsync() => _mssql.DisposeAsync().AsTask();

    [Fact]
    public async Task CheckHealthAsync_Should_TransitionFromUnhealthyToHealthy_AsMigrationsApply()
    {
        await using var provider = BuildServiceProvider();
        var check = new TenantMigrationsHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("db:tenants-migrations", check, HealthStatus.Unhealthy, tags: null)
        };

        // ── Before migrations ────────────────────────────────────────────
        // Every migration is pending → check must be Unhealthy so /health/ready returns 503.
        var beforeResult = await check.CheckHealthAsync(context, CancellationToken.None);
        beforeResult.Status.ShouldBe(HealthStatus.Unhealthy);
        beforeResult.Description.ShouldNotBeNull();
        beforeResult.Description!.ShouldContain("FSH.Starter.DbMigrator");

        // ── Apply migrations (what DbMigrator does in production) ────────
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            await db.Database.MigrateAsync();
        }

        // ── After migrations ─────────────────────────────────────────────
        var afterResult = await check.CheckHealthAsync(context, CancellationToken.None);
        afterResult.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_TenantProbeThrows()
    {
        // A connection-string pointed at a port nothing listens on simulates an
        // unreachable tenant DB. The per-tenant try/catch must convert that to an
        // Unhealthy aggregate result, not propagate the exception.
        await using var provider = BuildServiceProvider(badConnectionString: true);
        var check = new TenantMigrationsHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("db:tenants-migrations", check, HealthStatus.Unhealthy, tags: null)
        };

        var result = await check.CheckHealthAsync(context, CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("error probing tenant", Case.Insensitive);
    }

    private ServiceProvider BuildServiceProvider(bool badConnectionString = false)
    {
        string connectionString;

if (badConnectionString)
{
    connectionString =
        "Server=127.0.0.1,1;" +
        "Database=does_not_exist;" +
        "User Id=sa;" +
        "Password=InvalidPassword123!;" +
        "Encrypt=False;" +
        "TrustServerCertificate=True;" +
        "Connect Timeout=2;";
}
else
{
    var builder = new SqlConnectionStringBuilder(_mssql.GetConnectionString())
    {
        InitialCatalog = "fsh_migrations_check"
    };

    connectionString = builder.ConnectionString;
}

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMultiTenantStore<AppTenantInfo>>(new SingleTenantStore());
        services.AddScoped<IMultiTenantContextSetter, FakeMultiTenantContextSetter>();
        services.AddDbContext<TenantDbContext>(opts => opts.UseSqlServer(connectionString,
                                           b => b.MigrationsAssembly("FSH.Starter.Migrations.MSSQL")));
        return services.BuildServiceProvider();
    }

    /// <summary>Minimal in-memory tenant store — returns one tenant for the test.</summary>
    private sealed class SingleTenantStore : IMultiTenantStore<AppTenantInfo>
    {
        private readonly AppTenantInfo _tenant = new(
            MultitenancyConstants.Root.Id,
            MultitenancyConstants.Root.Name,
            connectionString: string.Empty,
            MultitenancyConstants.Root.EmailAddress,
            issuer: MultitenancyConstants.Root.Issuer);

        public Task<bool> AddAsync(AppTenantInfo tenantInfo) => Task.FromResult(true);
        public Task<bool> RemoveAsync(string identifier) => Task.FromResult(true);
        public Task<bool> UpdateAsync(AppTenantInfo tenantInfo) => Task.FromResult(true);
        public Task<AppTenantInfo?> GetByIdentifierAsync(string identifier) => Task.FromResult<AppTenantInfo?>(_tenant);
        public Task<AppTenantInfo?> GetAsync(string id) => Task.FromResult<AppTenantInfo?>(_tenant);
        public Task<IEnumerable<AppTenantInfo>> GetAllAsync() => Task.FromResult<IEnumerable<AppTenantInfo>>([_tenant]);
        public Task<IEnumerable<AppTenantInfo>> GetAllAsync(int take, int skip) => Task.FromResult<IEnumerable<AppTenantInfo>>([_tenant]);
    }

    /// <summary>
    /// Fake <see cref="IMultiTenantContextSetter"/> that just records the value. The health
    /// check writes to this before resolving <see cref="TenantDbContext"/>; reads of the
    /// context are not exercised by <c>GetPendingMigrationsAsync</c>.
    /// </summary>
    private sealed class FakeMultiTenantContextSetter : IMultiTenantContextSetter
    {
        public IMultiTenantContext MultiTenantContext { get; set; } = new MultiTenantContext<AppTenantInfo>(
            new AppTenantInfo("placeholder", "placeholder", string.Empty, "x@x", null));
    }
}
