# Portfolio Analysis

## Purpose

این قابلیت برای نمایش و تحلیل پرتفوی سرمایه‌گذاری شرکت‌ها
بر اساس گزارش‌های دوره‌ای منتشرشده در کدال استفاده می‌شود.

داده اصلی از InvestmentPortfolioPositions خوانده می‌شود.

---

## Portfolio Report

هر گزارش پرتفوی به یک Disclosure مشخص متصل است.

ارتباط اصلی:

Disclosure.Id
    ↓
InvestmentPortfolioPosition.DisclosureId

TracingNo نیز برای ردیابی گزارش اصلی کدال نگهداری می‌شود.

---

## Listed / Unlisted Positions

هر Position دارای IsListed است.

- IsListed = true  → شرکت بورسی
- IsListed = false → شرکت غیربورسی

در صورت Match شدن شرکت، ChildCompanyId شناسه استاندارد شرکت را مشخص می‌کند.

---

## Reported Market Value

ارزش بازار پایان دوره، مقداری است که از گزارش ماهانه شرکت استخراج شده است.

برای بخش بورسی:

ReportedPortfolioValue
=
SUM(EndingMarketValue)

این مقدار مربوط به تاریخ PeriodEndDate گزارش است
و تا انتشار گزارش بعدی تغییر نمی‌کند.

---

## Current Estimated Market Value

برای تخمین ارزش روز پرتفوی، تعداد سهام آخرین گزارش ثابت فرض می‌شود
و قیمت روز سهم جایگزین قیمت پایان دوره می‌شود.

برای هر سهم بورسی:

CurrentPositionValue
=
EndingQuantity × LatestMarketPrice

و:

CurrentListedPortfolioValue
=
SUM(CurrentPositionValue)

---

## Important Limitation

CurrentListedPortfolioValue ارزش واقعی قطعی پرتفوی امروز نیست.

این عدد:

"ارزش روز بر مبنای آخرین ترکیب افشاشده"

است.

علت این است که بین تاریخ آخرین گزارش و امروز ممکن است شرکت:

- سهمی خریداری کرده باشد
- سهمی فروخته باشد
- تعداد Positionهای خود را تغییر داده باشد

و این تغییرات تا گزارش بعدی برای ما مشخص نیست.

---

## Portfolio Value Change

برای مشاهده جهت تغییر ارزش پرتفوی:

PortfolioValueChange
=
CurrentListedPortfolioValue
-
ReportedListedPortfolioValue

PortfolioValueChangePercent
=
PortfolioValueChange
/
ReportedListedPortfolioValue
× 100

این شاخص می‌تواند قبل از انتشار گزارش ماهانه بعدی،
روند افزایش یا کاهش ارزش آخرین پرتفوی افشاشده را نشان دهد.

---

## Unlisted Companies

شرکت‌های غیربورسی قیمت روز بازار ندارند.

بنابراین در محاسبه CurrentListedPortfolioValue وارد نمی‌شوند
و ارزش‌گذاری آنها باید در یک مدل جداگانه انجام شود.

---

## Portfolio Viewer

نمایش پرتفوی از صفحه Disclosures قابل دسترسی خواهد بود.

برای Disclosureهایی که InvestmentPortfolioPosition دارند،
آیکون Portfolio نمایش داده می‌شود.

Dialog شامل:

- دوره گزارش
- شرکت مادر
- شرکت‌های بورسی
- شرکت‌های غیربورسی
- مقادیر ابتدای دوره
- تغییرات دوره
- مقادیر پایان دوره
- ارزش بازار پایان دوره
- در آینده: ارزش روز

Navigation:

Previous → دوره قبلی همان شرکت
Next     → دوره بعدی همان شرکت

مبنای Navigation:

ParentCompanyId + PeriodEndDate