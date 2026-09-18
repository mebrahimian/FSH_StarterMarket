using Modules.MarketIntelligence.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.MarketIntelligence.Services.Tsetmc;

public static class TsetmcYValMapper
{
    public static TsetmcAssetCategory GetCategory(string? yVal) =>
        yVal switch
        {
            // سهام
            "300" or "303" or "309" or "313"
                => TsetmcAssetCategory.Stock,

            // حق تقدم
            "400" or "401" or "403" or "404"
                => TsetmcAssetCategory.Rights,

            // اختیار
            "311" or "312" or
            "321" or "322" or "323" or
            "600" or "601" or "602"
                => TsetmcAssetCategory.Option,

            // صندوق
            "305" or "315"
                => TsetmcAssetCategory.Fund,

            // اوراق / صکوک
            "70" or
            "200" or "207" or "208" or
            "301" or "306" or "308" or
            "706"
                => TsetmcAssetCategory.Bond,

            // کالا
            "701"
                => TsetmcAssetCategory.Commodity,

            // انرژی
            "801" or "802" or "803" or "804" or
            "901" or "902"
                => TsetmcAssetCategory.Energy,

            // شاخص
            "67" or "68" or "69"
                => TsetmcAssetCategory.Index,

            _ => TsetmcAssetCategory.Unknown
        };
}
