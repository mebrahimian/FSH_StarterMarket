Add-Migration InitialMultitenancy  -Context TenantDbContext        -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir MultiTenancy
Remove-Migration                   -Context TenantDbContext        -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api



Add-Migration InitialIdentity      -Context IdentityDbContext      -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Identity
Remove-Migration                   -Context IdentityDbContext      -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api



Add-Migration InitialAuditing      -Context AuditDbContext         -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Auditing



Add-Migration InitialBilling       -Context BillingDbContext       -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Billing



Add-Migration InitialCatalog       -Context CatalogDbContext       -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Catalog



Add-Migration InitialChat          -Context ChatDbContext          -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Chat



Add-Migration InitialFiles         -Context FilesDbContext         -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Files



Add-Migration InitialNotifications -Context NotificationsDbContext -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Notifications



Add-Migration InitialTickets       -Context TicketsDbContext       -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Tickets



Add-Migration InitialWebhooks      -Context WebhookDbContext       -Project FSH.Starter.Migrations.MSSQL -StartupProject FSH.Starter.Api -OutputDir Webhooks





TenantDbContextو AuditDbContextوBillingDbContextوCatalogDbContextوChatDbContextوNotificationsDbContextوTicketsDbContextوWebhookDbContext

