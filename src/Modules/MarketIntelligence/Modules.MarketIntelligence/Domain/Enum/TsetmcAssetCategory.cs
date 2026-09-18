using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.MarketIntelligence.Domain.Enums;

public enum TsetmcAssetCategory
{
    Unknown = 0,   // نامشخص
    Stock = 1,     // سهام
    Rights = 2,    // حق تقدم
    Option = 3,    // اختیار معامله
    Fund = 4,      // صندوق سرمایه‌گذاری
    Bond = 5,      // اوراق بدهی / صکوک / مشارکت
    Commodity = 6, // کالا / گواهی سپرده کالایی
    Energy = 7,    // ابزارهای بورس انرژی
    Index = 8      // شاخص
}
