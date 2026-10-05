using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Data.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class DisclosureStatsConfiguration : IEntityTypeConfiguration<DisclosureStats>
{
    public void Configure(
        EntityTypeBuilder<DisclosureStats> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToView(
            "vw_DisclosureStats",
            "marketintelligence");

        builder.HasKey(x => x.PersianDate);

        builder.Property(x => x.PersianDate)
            .HasMaxLength(10);
    }
}