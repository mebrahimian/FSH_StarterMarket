import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  ArrowDownUp,
  CalendarRange,
  Download,
  FileSearch,
  FilterX,
  RefreshCw,
  Search,
} from "lucide-react";
import { apiFetch } from "@/lib/api-client";

/**
 * Market Intelligence > Monthly disclosure gaps
 *
 * Preview mode is the default. Set VITE_MONTHLY_GAPS_SOURCE=api only AFTER
 * GET /api/v1/marketintelligence/monthly-disclosure-gaps is implemented.
 * API response: MonthlyDisclosureGap[] OR { items: MonthlyDisclosureGap[] }.
 * No claim is made that an internal gap proves a reporting violation.
 */
export type MonthlyDisclosureGap = {
  symbol: string;
  rt: number;
  previousPeriod: string;
  nextPeriod: string;
  missingMonths: number;
  monthsWithOtherRt: number;
  monthsWithoutDisclosure: number;
  companyId?: number | null;
};

// The 26-row user-reviewed snapshot, after removal of the false Atous gap.
// This sample is intentionally not used as fallback when live API requests fail.
const SAMPLE_GAPS: MonthlyDisclosureGap[] = [
  { symbol: "تشتاد", rt: 0, previousPeriod: "1400/04", nextPeriod: "1404/01", missingMonths: 44, monthsWithOtherRt: 0, monthsWithoutDisclosure: 44 },
  { symbol: "حشکوه", rt: 5, previousPeriod: "1400/05", nextPeriod: "1404/01", missingMonths: 43, monthsWithOtherRt: 0, monthsWithoutDisclosure: 43 },
  { symbol: "حگردش", rt: 5, previousPeriod: "1400/05", nextPeriod: "1404/01", missingMonths: 43, monthsWithOtherRt: 0, monthsWithoutDisclosure: 43 },
  { symbol: "ولقمان", rt: 2, previousPeriod: "1400/10", nextPeriod: "1404/02", missingMonths: 39, monthsWithOtherRt: 0, monthsWithoutDisclosure: 39 },
  { symbol: "وپسا", rt: 2, previousPeriod: "1400/11", nextPeriod: "1404/01", missingMonths: 37, monthsWithOtherRt: 0, monthsWithoutDisclosure: 37 },
  { symbol: "ولراز", rt: 4, previousPeriod: "1400/05", nextPeriod: "1403/01", missingMonths: 31, monthsWithOtherRt: 0, monthsWithoutDisclosure: 31 },
  { symbol: "وتوسکا", rt: 2, previousPeriod: "1399/07", nextPeriod: "1402/01", missingMonths: 29, monthsWithOtherRt: 0, monthsWithoutDisclosure: 29 },
  { symbol: "ولنوین", rt: 4, previousPeriod: "1400/07", nextPeriod: "1403/01", missingMonths: 29, monthsWithOtherRt: 0, monthsWithoutDisclosure: 29 },
  { symbol: "تمحرکه", rt: 5, previousPeriod: "1399/10", nextPeriod: "1402/01", missingMonths: 26, monthsWithOtherRt: 0, monthsWithoutDisclosure: 26 },
  { symbol: "وآفر", rt: 2, previousPeriod: "1403/06", nextPeriod: "1405/06", missingMonths: 23, monthsWithOtherRt: 0, monthsWithoutDisclosure: 23 },
  { symbol: "اروند", rt: 0, previousPeriod: "1401/07", nextPeriod: "1403/01", missingMonths: 17, monthsWithOtherRt: 0, monthsWithoutDisclosure: 17 },
  { symbol: "وارس", rt: 2, previousPeriod: "1402/09", nextPeriod: "1404/03", missingMonths: 17, monthsWithOtherRt: 0, monthsWithoutDisclosure: 17 },
  { symbol: "تادیکو", rt: 2, previousPeriod: "1400/08", nextPeriod: "1401/12", missingMonths: 15, monthsWithOtherRt: 0, monthsWithoutDisclosure: 15 },
  { symbol: "تپکو", rt: 0, previousPeriod: "1403/01", nextPeriod: "1404/01", missingMonths: 11, monthsWithOtherRt: 0, monthsWithoutDisclosure: 11 },
  { symbol: "کایتا", rt: 0, previousPeriod: "1403/07", nextPeriod: "1404/07", missingMonths: 11, monthsWithOtherRt: 0, monthsWithoutDisclosure: 11 },
  { symbol: "ودانا", rt: 2, previousPeriod: "1400/09", nextPeriod: "1401/03", missingMonths: 5, monthsWithOtherRt: 0, monthsWithoutDisclosure: 5 },
  { symbol: "قنقش", rt: 0, previousPeriod: "1403/03", nextPeriod: "1403/07", missingMonths: 3, monthsWithOtherRt: 0, monthsWithoutDisclosure: 3 },
  { symbol: "فافق", rt: 2, previousPeriod: "1400/01", nextPeriod: "1400/04", missingMonths: 2, monthsWithOtherRt: 0, monthsWithoutDisclosure: 2 },
  { symbol: "معیار", rt: 2, previousPeriod: "1405/03", nextPeriod: "1405/06", missingMonths: 2, monthsWithOtherRt: 0, monthsWithoutDisclosure: 2 },
  { symbol: "کهرام", rt: 0, previousPeriod: "1404/10", nextPeriod: "1405/01", missingMonths: 2, monthsWithOtherRt: 0, monthsWithoutDisclosure: 2 },
  { symbol: "تادیکو", rt: 2, previousPeriod: "1403/09", nextPeriod: "1403/11", missingMonths: 1, monthsWithOtherRt: 0, monthsWithoutDisclosure: 1 },
  { symbol: "نیرو", rt: 2, previousPeriod: "1400/12", nextPeriod: "1401/02", missingMonths: 1, monthsWithOtherRt: 0, monthsWithoutDisclosure: 1 },
  { symbol: "ودانا", rt: 2, previousPeriod: "1400/02", nextPeriod: "1400/04", missingMonths: 1, monthsWithOtherRt: 0, monthsWithoutDisclosure: 1 },
  { symbol: "ورسا", rt: 2, previousPeriod: "1404/03", nextPeriod: "1404/05", missingMonths: 1, monthsWithOtherRt: 0, monthsWithoutDisclosure: 1 },
  { symbol: "وهنر", rt: 2, previousPeriod: "1405/03", nextPeriod: "1405/05", missingMonths: 1, monthsWithOtherRt: 0, monthsWithoutDisclosure: 1 },
  { symbol: "ویسا", rt: 2, previousPeriod: "1404/01", nextPeriod: "1404/03", missingMonths: 1, monthsWithOtherRt: 0, monthsWithoutDisclosure: 1 },
];

const LIVE_MODE = import.meta.env.VITE_MONTHLY_GAPS_SOURCE === "api";
const API_PATH = "/api/v1/marketintelligence/monthly-disclosure-gaps";
const faNum = (n: number) => n.toLocaleString("fa-IR");
const faPeriod = (period: string) => period.replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]);
const normalize = (value: string) =>
  value.trim().replace(/[۰-۹]/g, (d) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(d)))
    .replace(/[٠-٩]/g, (d) => String("٠١٢٣٤٥٦٧٨٩".indexOf(d)))
    .replace(/ي/g, "ی").replace(/ك/g, "ک").toLowerCase();

async function fetchGaps(): Promise<MonthlyDisclosureGap[]> {
  const response = await apiFetch<MonthlyDisclosureGap[] | { items: MonthlyDisclosureGap[] }>(API_PATH, { timeoutMs: 120_000 });
  return Array.isArray(response) ? response : response.items;
}

type SortBy = "missingMonths" | "symbol" | "previousPeriod" | "nextPeriod";
type SortDirection = "asc" | "desc";

function exportCsv(items: MonthlyDisclosureGap[]) {
  const header = ["Symbol", "Rt", "PreviousPeriod", "NextPeriod", "MissingMonths", "MonthsWithOtherRt", "MonthsWithoutDisclosure"];
  const quote = (value: string | number) => `"${String(value).replace(/"/g, '""')}"`;
  const lines = [header, ...items.map((r) => [r.symbol, r.rt, r.previousPeriod, r.nextPeriod, r.missingMonths, r.monthsWithOtherRt, r.monthsWithoutDisclosure])];
  const csv = "\uFEFF" + lines.map((line) => line.map(quote).join(",")).join("\r\n");
  const objectUrl = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" }));
  const a = document.createElement("a");
  a.href = objectUrl;
  a.download = "monthly-disclosure-gaps.csv";
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
}

function StatTile({ label, value, icon: Icon, detail }: {
  label: string;
  value: number;
  icon: typeof Activity;
  detail?: string;
}) {
  return (
    <div className="rounded-xl border bg-card px-4 py-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium text-muted-foreground">{label}</p>
          <p className="mt-1 text-2xl font-bold tabular-nums">{faNum(value)}</p>
          {detail && <p className="mt-1 text-xs text-muted-foreground">{detail}</p>}
        </div>
        <div className="rounded-lg bg-muted p-2 text-muted-foreground"><Icon className="h-5 w-5" /></div>
      </div>
    </div>
  );
}

/** Full page, ready to register under /market-intelligence/monthly-gaps. */
export function MonthlyDisclosureGapsPage() {
  const [search, setSearch] = useState("");
  const [rtFilter, setRtFilter] = useState("all");
  const [minMonths, setMinMonths] = useState("0");
  const [sortBy, setSortBy] = useState<SortBy>("missingMonths");
  const [sortDir, setSortDir] = useState<SortDirection>("desc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const query = useQuery({
    queryKey: ["market-intelligence", "monthly-disclosure-gaps"],
    queryFn: fetchGaps,
    enabled: LIVE_MODE,
    staleTime: 60_000,
    retry: 1,
  });

  const data = LIVE_MODE ? (query.data ?? []) : SAMPLE_GAPS;
  const sorted = useMemo(() => {
    const term = normalize(search);
    const filtered = data.filter((gap) => {
      if (rtFilter !== "all" && gap.rt !== Number(rtFilter)) return false;
      if (gap.missingMonths < Number(minMonths)) return false;
      if (!term) return true;
      return [gap.symbol, gap.previousPeriod, gap.nextPeriod, String(gap.rt)]
        .some((value) => normalize(value).includes(term));
    });
    return filtered.sort((a, b) => {
      const compare = sortBy === "missingMonths"
        ? a.missingMonths - b.missingMonths
        : sortBy === "symbol"
          ? a.symbol.localeCompare(b.symbol, "fa")
          : a[sortBy].localeCompare(b[sortBy]);
      return (sortDir === "asc" ? 1 : -1) * compare;
    });
  }, [data, search, rtFilter, minMonths, sortBy, sortDir]);

  const summary = useMemo(() => ({
    totalGaps: data.length,
    symbols: new Set(data.map((gap) => gap.symbol.trim())).size,
    missingMonths: data.reduce((sum, gap) => sum + gap.missingMonths, 0),
    filteredMonths: sorted.reduce((sum, gap) => sum + gap.missingMonths, 0),
  }), [data, sorted]);
  const maxPage = Math.max(1, Math.ceil(sorted.length / pageSize));
  const safePage = Math.min(page, maxPage);
  const pageRows = sorted.slice((safePage - 1) * pageSize, safePage * pageSize);
  const clearFilters = () => {
    setSearch(""); setRtFilter("all"); setMinMonths("0");
    setSortBy("missingMonths"); setSortDir("desc"); setPage(1);
  };

  const inputClass = "h-10 w-full rounded-lg border bg-background px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary/15";

  return (
    <main className="space-y-5 p-4 sm:p-6" dir="rtl">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-medium text-muted-foreground">هوشمندی بازار / کیفیت داده</p>
          <h1 className="mt-1 text-xl font-bold sm:text-2xl">شکاف گزارش‌های فعالیت ماهانه</h1>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-muted-foreground">
            فاصله بین دو گزارش موجود هر نماد بر اساس دوره گزارش (نه تاریخ انتشار)، با شرط وجود قیمت معتبر در ۳۶۵ روز گذشته.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {LIVE_MODE && (
            <button type="button" onClick={() => void query.refetch()} disabled={query.isFetching}
              className="inline-flex h-10 items-center gap-2 rounded-lg border bg-card px-3 text-sm hover:bg-muted disabled:opacity-50">
              <RefreshCw className={`h-4 w-4 ${query.isFetching ? "animate-spin" : ""}`} /> به‌روزرسانی
            </button>
          )}
          <button type="button" disabled={!sorted.length} onClick={() => exportCsv(sorted)}
            className="inline-flex h-10 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50">
            <Download className="h-4 w-4" /> خروجی CSV
          </button>
        </div>
      </div>

      <div role="status" className="rounded-lg border bg-muted/40 px-4 py-3 text-xs leading-6 text-muted-foreground">
        {LIVE_MODE ? (
          <span>منبع: API سیستم — نتایج بر اساس داده‌های موجود محاسبه می‌شوند.</span>
        ) : (
          <span><strong className="text-foreground">حالت پیش‌نمایش:</strong> نمایش ۲۶ ردیف خروجی بررسی‌شده؛ هنوز به Stored Procedure / API متصل نشده است.</span>
        )}
        <span className="block">«گپ داخلی» به‌تنهایی به معنی نقص جمع‌آوری، تخلف یا عدم انتشار قطعی گزارش نیست.</span>
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile icon={FileSearch} label="کل گپ‌های داخلی" value={summary.totalGaps} />
        <StatTile icon={Activity} label="نمادهای متفاوت" value={summary.symbols} />
        <StatTile icon={CalendarRange} label="جمع ماه‌های مفقود" value={summary.missingMonths} />
        <StatTile icon={ArrowDownUp} label="ماه‌های مفقود در فیلتر فعلی" value={summary.filteredMonths} detail={`${faNum(sorted.length)} ردیف مطابق فیلتر`} />
      </div>

      <section className="rounded-xl border bg-card shadow-sm">
        <div className="grid gap-3 border-b p-4 md:grid-cols-[minmax(240px,2fr)_repeat(3,minmax(140px,1fr))_auto]">
          <label className="relative block">
            <span className="mb-1 block text-xs text-muted-foreground">جست‌وجوی نماد یا دوره</span>
            <Search className="pointer-events-none absolute right-3 top-[33px] h-4 w-4 text-muted-foreground" />
            <input aria-label="جست‌وجوی نماد یا دوره" value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1); }}
              placeholder="مثلاً ودانا یا ۱۴۰۴/۰۱" className={`${inputClass} pr-9`} />
          </label>
          <label>
            <span className="mb-1 block text-xs text-muted-foreground">نوع گزارش (Rt)</span>
            <select aria-label="نوع گزارش" value={rtFilter}
              onChange={(e) => { setRtFilter(e.target.value); setPage(1); }} className={inputClass}>
              <option value="all">همه انواع</option>
              {[0, 2, 4, 5].map((rt) => <option value={rt} key={rt}>Rt = {rt}</option>)}
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs text-muted-foreground">حداقل ماه مفقود</span>
            <select aria-label="حداقل ماه مفقود" value={minMonths}
              onChange={(e) => { setMinMonths(e.target.value); setPage(1); }} className={inputClass}>
              <option value="0">همه</option>
              {[2, 3, 6, 12, 24, 36].map((n) => <option key={n} value={n}>{faNum(n)} ماه و بیشتر</option>)}
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs text-muted-foreground">مرتب‌سازی</span>
            <select aria-label="مرتب‌سازی" value={sortBy}
              onChange={(e) => { setSortBy(e.target.value as SortBy); setPage(1); }} className={inputClass}>
              <option value="missingMonths">تعداد ماه مفقود</option>
              <option value="symbol">نام نماد</option>
              <option value="previousPeriod">دوره قبل</option>
              <option value="nextPeriod">دوره بعد</option>
            </select>
          </label>
          <div className="flex items-end gap-2">
            <button type="button" aria-label="تغییر جهت مرتب‌سازی" title="تغییر جهت مرتب‌سازی"
              onClick={() => { setSortDir((d) => d === "desc" ? "asc" : "desc"); setPage(1); }}
              className="flex h-10 w-10 items-center justify-center rounded-lg border hover:bg-muted">
              <ArrowDownUp className="h-4 w-4" />
            </button>
            <button type="button" aria-label="پاک‌کردن فیلترها" title="پاک‌کردن فیلترها" onClick={clearFilters}
              className="flex h-10 w-10 items-center justify-center rounded-lg border hover:bg-muted">
              <FilterX className="h-4 w-4" />
            </button>
          </div>
        </div>

        {LIVE_MODE && query.isError && (
          <div role="alert" className="m-4 rounded-lg border border-destructive/40 bg-destructive/5 p-4 text-sm text-destructive">
            دریافت اطلاعات از API انجام نشد. برای نمایش زنده ابتدا Endpoint را پیاده‌سازی کنید.
          </div>
        )}
        <div className="overflow-x-auto">
          <table className="w-full min-w-[900px] border-collapse text-right text-sm">
            <thead className="bg-muted/50 text-xs text-muted-foreground">
              <tr>
                <th scope="col" className="px-4 py-3 font-medium">نماد</th>
                <th scope="col" className="px-4 py-3 font-medium">Rt</th>
                <th scope="col" className="px-4 py-3 font-medium">دوره قبل</th>
                <th scope="col" className="px-4 py-3 font-medium">دوره بعد</th>
                <th scope="col" className="px-4 py-3 text-center font-medium">ماه مفقود</th>
                <th scope="col" className="px-4 py-3 text-center font-medium">ماه با Rt دیگر</th>
                <th scope="col" className="px-4 py-3 text-center font-medium">ماه بدون گزارش</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {pageRows.map((row) => (
                <tr className="transition-colors hover:bg-muted/30" key={`${row.symbol}-${row.rt}-${row.previousPeriod}-${row.nextPeriod}`}>
                  <td className="px-4 py-3 font-semibold">{row.symbol}</td>
                  <td className="px-4 py-3 tabular-nums text-muted-foreground">{faNum(row.rt)}</td>
                  <td className="px-4 py-3 tabular-nums" dir="ltr">{faPeriod(row.previousPeriod)}</td>
                  <td className="px-4 py-3 tabular-nums" dir="ltr">{faPeriod(row.nextPeriod)}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`inline-flex min-w-10 justify-center rounded-md px-2 py-1 font-semibold tabular-nums ${row.missingMonths >= 12 ? "bg-amber-500/10 text-amber-700 dark:text-amber-400" : "bg-muted text-foreground"}`}>
                      {faNum(row.missingMonths)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center tabular-nums">{faNum(row.monthsWithOtherRt)}</td>
                  <td className="px-4 py-3 text-center tabular-nums">{faNum(row.monthsWithoutDisclosure)}</td>
                </tr>
              ))}
              {!pageRows.length && (
                <tr><td colSpan={7} className="px-4 py-14 text-center text-muted-foreground">
                  {LIVE_MODE && query.isFetching ? "در حال دریافت اطلاعات…" : "موردی مطابق فیلترهای انتخاب‌شده یافت نشد."}
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3 border-t px-4 py-3 text-xs text-muted-foreground">
          <p>نمایش {sorted.length ? faNum((safePage - 1) * pageSize + 1) : faNum(0)} تا {faNum(Math.min(safePage * pageSize, sorted.length))} از {faNum(sorted.length)} ردیف</p>
          <div className="flex items-center gap-2">
            <label htmlFor="gaps-page-size">ردیف در صفحه</label>
            <select id="gaps-page-size" className="rounded-md border bg-background px-2 py-1" value={pageSize}
              onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}>
              {[10, 20, 50].map((n) => <option key={n} value={n}>{faNum(n)}</option>)}
            </select>
            <button type="button" disabled={safePage <= 1} onClick={() => setPage(Math.max(1, safePage - 1))}
              className="rounded-md border px-3 py-1.5 hover:bg-muted disabled:opacity-40">قبلی</button>
            <span className="tabular-nums">{faNum(safePage)} / {faNum(maxPage)}</span>
            <button type="button" disabled={safePage >= maxPage} onClick={() => setPage(Math.min(maxPage, safePage + 1))}
              className="rounded-md border px-3 py-1.5 hover:bg-muted disabled:opacity-40">بعدی</button>
          </div>
        </div>
      </section>
      <p className="text-xs leading-6 text-muted-foreground">
        توجه: نتیجه شامل گپ‌های داخلی میان دو دوره موجود است. دوره‌های قبل از اولین گزارش و بعد از آخرین گزارش در این خروجی بررسی نمی‌شوند.
      </p>
    </main>
  );
}

export default MonthlyDisclosureGapsPage;
