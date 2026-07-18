Add-Migration InitialMultitenancy  -Context TenantDbContext   -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir MultiTenancy
Remove-Migration                   -Context TenantDbContext   -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api


Add-Migration InitialIdentity      -Context IdentityDbContext -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Identity
Remove-Migration                             -Context IdentityDbContext -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api


Add-Migration InitialAuditing       -Context AuditDbContext    -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api  -OutputDir Auditing


Add-Migration InitialBilling       -Context BillingDbContext  -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api  -OutputDir Billing