using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FSH.Modules.Multitenancy.Data;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile(
                Path.Combine(
                    basePath,
                    "Host",
                    "FSH.Starter.DbMigrator",
                    "bin",
                    "Debug",
                    "net10.0",
                    "appsettings.json"),
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = configuration["DatabaseOptions:Provider"] ?? "MSSQL";

        var connectionString = configuration["DatabaseOptions:ConnectionString"]
            ?? throw new InvalidOperationException(
                "DatabaseOptions:ConnectionString is not configured.");

        var migrationsAssembly = configuration["DatabaseOptions:MigrationsAssembly"]
            ?? "FSH.Starter.Migrations.MSSQL";

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        switch (provider.ToUpperInvariant())
        {
            case "POSTGRESQL":
                optionsBuilder.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(migrationsAssembly));
                break;

            case "MSSQL":
                optionsBuilder.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly(migrationsAssembly));
                break;

            default:
                throw new NotSupportedException(
                    $"Database provider '{provider}' is not supported for TenantDbContext migrations.");
        }

        return new TenantDbContext(optionsBuilder.Options);
    }
}