Write-Host "Building solution..."
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Migration cancelled."
    exit 1
}

Write-Host "Build succeeded. Creating migrations..."

$contexts = @(
      @{ Name="AuditingDbContext"; Migration="InitialAuditing"; Folder="Auditing" },
    @{ Name="BillingDbContext"; Migration="InitialBilling"; Folder="Billing" },
    @{ Name="CatalogDbContext"; Migration="InitialCatalog"; Folder="Catalog" },
    @{ Name="ChatDbContext"; Migration="InitialChat"; Folder="Chat" },
    @{ Name="FilesDbContext"; Migration="InitialFiles"; Folder="Files" },
    @{ Name="IdentityDbContext"; Migration="InitialIdentity"; Folder="Identity" },
    @{ Name="MarketIntelligenceDbContext"; Migration="InitialMarketIntelligence"; Folder="MarketIntelligence" },
    @{ Name="TenantDbContext"; Migration="InitialMultitenancy"; Folder="MultiTenancy" },
    @{ Name="NotificationsDbContext"; Migration="InitialNotifications"; Folder="Notifications" },
    @{ Name="TicketsDbContext"; Migration="InitialTickets"; Folder="Tickets" },
    @{ Name="WebhookDbContext"; Migration="InitialWebhooks"; Folder="Webhooks" }
)

foreach ($ctx in $contexts) {
    Write-Host "Creating migration for $($ctx.Name)..." -ForegroundColor Cyan

    dotnet ef migrations add $ctx.Migration `
        --context $ctx.Name `
        --project .\Host\FSH.Starter.Migrations.MSSQL `
        --startup-project .\Host\FSH.Starter.DbMigrator `
        --output-dir $ctx.Folder

    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $($ctx.Name)" -ForegroundColor Red
        exit 1
    }
}

Write-Host "All MSSQL migrations created successfully." -ForegroundColor Green