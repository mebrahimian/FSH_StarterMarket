
using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class UserPlanLimit : BaseEntity<Guid>, IGlobalEntity
{
    private UserPlanLimit()
    {
    }

    public string PlanKey { get; private set; } = default!;

    // -1 = Unlimited
    public int MaxSelectedSymbols { get; private set; }

    // -1 = Unlimited
    public int MaxSymbolsPerIndustry { get; private set; }

    public static UserPlanLimit Create(
        string planKey,
        int maxSelectedSymbols,
        int maxSymbolsPerIndustry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planKey);

        if (maxSelectedSymbols < -1 || maxSymbolsPerIndustry < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSelectedSymbols),
                "Limits must be -1 (unlimited) or non-negative.");
        }

        return new UserPlanLimit
        {
            Id = Guid.CreateVersion7(),
#pragma warning disable CA1308 // Plan keys match the lowercase BillingPlan keys.
            PlanKey = planKey.Trim().ToLowerInvariant(),
#pragma warning restore CA1308
            MaxSelectedSymbols = maxSelectedSymbols,
            MaxSymbolsPerIndustry = maxSymbolsPerIndustry
        };
    }

    public void UpdateLimits(
        int maxSelectedSymbols,
        int maxSymbolsPerIndustry)
    {
        if (maxSelectedSymbols < -1 || maxSymbolsPerIndustry < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSelectedSymbols),
                "Limits must be -1 (unlimited) or non-negative.");
        }

        MaxSelectedSymbols = maxSelectedSymbols;
        MaxSymbolsPerIndustry = maxSymbolsPerIndustry;
    }
}
