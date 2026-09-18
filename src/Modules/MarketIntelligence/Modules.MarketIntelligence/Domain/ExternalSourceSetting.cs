using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class ExternalSourceSetting
    : BaseEntity<int>, IGlobalEntity
{
    public string Source { get; private set; } = default!;

    public string Key { get; private set; } = default!;

    public string Value { get; private set; } = default!;

    public bool IsActive { get; private set; } = true;
}