using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CodalIncrementalScheduleSettingConfiguration
    : IEntityTypeConfiguration<CodalIncrementalScheduleSetting>
{
    public void Configure(
        EntityTypeBuilder<CodalIncrementalScheduleSetting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
    }
}