using FSH.Framework.Shared.Constants;

namespace FSH.Modules.MarketIntelligence.Contracts.Authorization;

public static class MarketIntelligencePermissions
{
    public static class Disclosures
    {
        public const string Resource = "Catalog.Disclosures";
        public const string View    = $"Permissions.{Resource}.View";
        public const string Create  = $"Permissions.{Resource}.Create";
        public const string Update  = $"Permissions.{Resource}.Update";
        public const string Delete  = $"Permissions.{Resource}.Delete";
        public const string Restore = $"Permissions.{Resource}.Restore";
    }

    

    

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Disclosures",    ActionConstants.View,   Disclosures.Resource, IsBasic: true),
        new("Create Disclosures",  ActionConstants.Create, Disclosures.Resource),
        new("Update Disclosures",  ActionConstants.Update, Disclosures.Resource),
        new("Delete Disclosures",  ActionConstants.Delete, Disclosures.Resource),
        new("Restore Disclosures", "Restore",              Disclosures.Resource),

        
    ];
}
