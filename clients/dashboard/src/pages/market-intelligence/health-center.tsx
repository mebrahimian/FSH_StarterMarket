import {
    Activity,
    Database,
    ChartNoAxesCombined,
    ExternalLink,
    HeartPulse,
    Newspaper,
    RefreshCw,
    type LucideIcon,
} from "lucide-react";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/button";
import {
    PageHero,
    ToneIconTile,
} from "@/components/list";
import { useQuery } from "@tanstack/react-query";

import {
    getCodalDataQuality,
    searchDisclosures,
    type DisclosureParseStatus,
    type DisclosureSortBy,
} from "@/api/market-intelligence";
import { useState } from "react";

import { Combobox } from "@/components/list";
import { useTranslation } from "react-i18next";

import {
    codalReportTypeOptions,
} from "@/lib/market-intelligence/codal-report-types";
import {
    codalLetterCategoryOptions,
} from "@/lib/market-intelligence/codal-letter-categories";

const sortOptions: Array<{
    value: DisclosureSortBy;
    labelKey: string;
}> = [
        { value: "publishDateTime", labelKey: "sort.publishDate" },
        { value: "tracingNo", labelKey: "sort.tracingNo" },
        { value: "symbol", labelKey: "sort.symbol" },
        { value: "companyName", labelKey: "sort.company" },
        { value: "salesParsedAt", labelKey: "sort.parseDate" },
    ];

export function MarketHealthCenterPage() {
    const [rtFilter, setRtFilter] = useState("");
    const [letFilter, setLetFilter] = useState("");
    const [sortBy, setSortBy] = useState<DisclosureSortBy>("publishDateTime");
    const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");
    const { t } = useTranslation("disclosures");
    const { t: tMarket } = useTranslation("marketIntelligence");
    const [statusFilter, setStatusFilter] = useState<DisclosureParseStatus | null>(null);
    const selectedLetterCategory =
        codalLetterCategoryOptions.find(
            (category) =>
                category.value === letFilter,
        );

    const selectedLetCodes =
        selectedLetterCategory?.letCodes.filter(
            (code): code is number =>
                code !== null,
        ) ?? [];

    const includeNullLet =
        selectedLetterCategory?.letCodes.includes(
            null,
        ) ?? false;
    const disclosuresQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "health-center",
            "disclosures",
            rtFilter,
            letFilter,
            statusFilter,
            sortBy,
            sortDir,
        ],
        queryFn: () =>
            searchDisclosures({
                pageNumber: 1,
                pageSize: 12,
                sortBy,
                sortDir,
                rt: rtFilter
                    ? Number(rtFilter)
                    : null,
                salesParseStatus: statusFilter,
                lets:
                    selectedLetCodes.length > 0
                        ? selectedLetCodes
                        : undefined,

                includeNullLet,
            }),
    });
    const dataQualityQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "health-center",
            "data-quality",
        ],
        queryFn: () => getCodalDataQuality(5),
    });
    const recentDisclosures =
        disclosuresQuery.data?.items ?? [];
    return (
        <div className="-mt-5">
            <PageHero   className="-mt-3 [&>div]:!py-3 sm:[&>div]:!py-3"
                title={tMarket("healthCenter.title")}
                subtitle={tMarket("healthCenter.subtitle")}
                actions={
                    <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => {
                            disclosuresQuery.refetch();
                            dataQualityQuery.refetch();
                        }}
                    >
                        <RefreshCw
                            className={cn(
                                "mr-1.5 size-3.5",
                                (disclosuresQuery.isFetching || dataQualityQuery.isFetching) &&
                                "animate-spin",
                            )}
                        />
                        {t("actions.refresh")}
                    </Button>
                }
            />

            <section className="mt-2 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <HealthStat
                    icon={Activity}
                    label={tMarket("healthCenter.stats.activeSymbols")}
                    value={
                        dataQualityQuery.data
                            ? dataQualityQuery.data.historyCoverage.activeSymbols.toLocaleString()
                            : "—"
                    }
                    hint={tMarket("healthCenter.stats.activeSymbolsHint")}
                />

                <HealthStat
                    icon={Newspaper}
                    label={tMarket("healthCenter.stats.disclosures")}
                    value={
                        dataQualityQuery.data
                            ? dataQualityQuery.data.metadata.totalDisclosures.toLocaleString()
                            : "—"
                    }
                    hint={tMarket("healthCenter.stats.totalDisclosures")} 
                />

                <HealthStat
                    icon={Database}
                    label={tMarket("healthCenter.stats.monthlyReports")}
                    value={
                        dataQualityQuery.data
                            ? dataQualityQuery.data.monthlyProcessing.totalCandidates.toLocaleString()
                            : "—"
                    }
                    hint={tMarket("healthCenter.stats.monthlySummaries")}
                />

                <HealthStat
                    icon={HeartPulse}
                    label={tMarket("healthCenter.stats.needsReview")}
                    value={
                        dataQualityQuery.data
                            ? dataQualityQuery.data.historyCoverage.incompleteSymbols.toLocaleString()
                            : "—"
                    }
                    hint={tMarket("healthCenter.gapFailedMissing")}
                />
            </section>
            <section className="mt-2 pt-5 pr-5 pb-0 mb-0 overflow-hidden rounded-xl border border-[var(--color-border)]  shadow-xs sm:h-[255px]">
                <div className="-mt-4 mb-0 pb-0 flex w-full items-end justify-between gap-3 ">
                    <div className="flex items-end gap-3 -pb-5 mb-0">
                        <Combobox
                            label="RT"
                            value={rtFilter || null}
                            onChange={(value) =>
                                setRtFilter(value ?? "")
                            }
                            options={codalReportTypeOptions.map(
                                (option) => ({
                                    value: option.value,
                                    label: tMarket(option.labelKey),
                                }),
                            )}
                            variant="filter"
                            clearable
                        />
                        <Combobox
                            label={tMarket("filters.reportLet")}
                            value={letFilter || null}
                            onChange={(value) =>
                                setLetFilter(value ?? "")
                            }
                            options={codalLetterCategoryOptions.map(
                                (category) => ({
                                    value: category.value,
                                    label: tMarket(category.labelKey),
                                }),
                            )}
                            variant="filter"
                            clearable
                        />
                        <Combobox
                            label={t("filters.parseStatus")}
                            value={statusFilter}
                            onChange={(value) =>
                                setStatusFilter(
                                    value as DisclosureParseStatus | null,
                                )
                            }
                            options={[
                                { value: "Pending", label: tMarket("status.pending") },
                                { value: "Success", label: tMarket("status.success") },
                                { value: "Failed", label: tMarket("status.failed") },
                                { value: "NoData", label: tMarket("status.noData") },
                                { value: "Skipped", label: tMarket("status.skipped") },
                            ]}
                            variant="filter"
                            clearable
                        />
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                                setRtFilter("");
                                setLetFilter("");
                                setStatusFilter(null);
                                setSortBy("publishDateTime");
                                setSortDir("desc");
                            }}
                        >
                            {tMarket("filters.reset")}
                        </Button>
                    </div>

                    <div className="flex items-end gap-2 [--color-primary:#2563eb] [--color-primary-foreground:#ffffff]">
                        <Combobox
                            label={t("filters.sortBy")}
                            value={sortBy}
                            onChange={(value) => {
                                if (value) {
                                    setSortBy(
                                        value as DisclosureSortBy,
                                    );
                                }
                            }}
                            options={sortOptions.map((option) => ({
                                value: option.value,
                                label: t(option.labelKey),
                            }))}
                            variant="filter"
                        />
                        <div
                            role="group"
                            aria-label="Sort direction"
                            className="inline-flex h-8 items-center rounded-full border border-[var(--color-border)] bg-[var(--color-card)] p-0.5"
                        >
                            {(["desc", "asc"] as const).map((direction) => (
                                <button
                                    key={direction}
                                    type="button"
                                    onClick={() => setSortDir(direction)}
                                    aria-pressed={sortDir === direction}
                                    className={cn(
                                        "h-7 cursor-pointer rounded-full px-3 text-[11px] font-semibold uppercase tracking-wider transition-colors",
                                        sortDir === direction
                                            ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                                            : "text-[var(--color-muted-foreground)]"
                                    )}
                                >
                                    {t(`filters.${direction}`)}
                                </button>
                            ))}
                        </div>
                    </div>
                </div>
                <h2 className="text-center text-base font-semibold">
                    {tMarket("healthCenter.recentDisclosures")}
                </h2>
               
                <div className="max-h-50 overflow-y-auto pr-1">
                    <div className="mb-2 grid min-w-[620px] grid-cols-[80px_500px_200px_150px_100px] items-center gap-6 border-b border-[var(--color-border)] text-center text-xs font-semibold text-[var(--color-muted-foreground)]">
                        <span>{t("table.symbol")}</span>
                        <span>{t("table.disclosure")}</span>
                        <span>{t("table.published")}</span>
                        <span>{tMarket("filters.reportType")}</span>
                        <span />
                    </div>
                    {recentDisclosures.map((disclosure) => (
                        <div
                            key={disclosure.id}
                            className="grid min-w-[620px] grid-cols-[80px_500px_200px_150px_100px] items-center gap-6 border-b border-[var(--color-border)] pb-0 text-center text-xs font-semibold text-[var(--color-muted-foreground)]"
                        >
                            <span className="w-[80px] shrink-0 truncate text-right text-sm text-[var(--color-muted-foreground)] font-semibold">
                                {disclosure.symbol}
                            </span>
                            
                            <div
                                dir="rtl"
                                onWheel={(e) => {
                                    e.currentTarget.scrollLeft -= e.deltaY;
                                }}
                                className="scrollbar-hidden min-w-0 overflow-x-auto whitespace-nowrap text-right text-sm text-[var(--color-muted-foreground)]"
                            >
                                {disclosure.title}
                            </div>
                            <span dir="ltr"  className="min-w-0 truncate whitespace-nowrap text-sm text-[var(--color-muted-foreground)]">
                                {disclosure.publishDateTimeRaw ?? "—"}
                            </span>
                            <span className="text-center text-sm text-[var(--color-muted-foreground)]">
                                {disclosure.rt != null
                                    ? tMarket(
                                        codalReportTypeOptions.find(
                                            (option) =>
                                                Number(option.value) === disclosure.rt,
                                        )?.labelKey ?? "—",
                                    )
                                    : "—"}
                            </span>
                            <div className="flex items-center justify-center gap-1">
                                {toCodalUrl(disclosure.url) && (
                                    <a
                                        href={toCodalUrl(disclosure.url)!}
                                        target="_blank"
                                        rel="noreferrer"
                                        aria-label={`Open ${disclosure.symbol} on Codal`}
                                        className="grid size-8 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-primary)]"
                                    >
                                        <ExternalLink className="size-4" />
                                    </a>
                                )}

                                {disclosure.salesParseStatus === "Success" && (
                                    <button
                                        type="button"
                                        title="آخرین اطلاعات فروش"
                                        aria-label={`View sales data for ${disclosure.symbol}`}
                                        className="grid size-8 place-items-center rounded-md text-[var(--color-success)] transition-colors hover:bg-[var(--color-muted)]"
                                    >
                                        <ChartNoAxesCombined className="size-4" />
                                    </button>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            </section>
            <section className="mt-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-4 py-1 shadow-xs">
                <div className="-mt-1 mb-5">
                    <h2 className="text-base font-semibold text-[var(--color-foreground)]">
                        {tMarket("healthCenter.backfill.description")}
                    </h2>                                        
                </div>

                <div className="-mt-5 grid gap-4 md:grid-cols-3">
                    <Field
                        label={tMarket("healthCenter.backfill.symbol")}
                        placeholder={tMarket("healthCenter.backfill.symbolPlaceholder")}
                    />

                    <Field
                        label={tMarket("healthCenter.backfill.fromDate")}
                        placeholder="1403/01/01"
                    />

                    <Field
                        label={tMarket("healthCenter.backfill.toDate")}
                        placeholder="1403/12/30"
                    />
                </div>

                <div className="mt-1 flex justify-end">
                    <Button type="button">
                        {tMarket("healthCenter.backfill.run")}
                    </Button>
                </div>
            </section>
        </div>
    );
}

function HealthStat({
    icon,
    label,
    value,
    hint,
}: {
    icon: LucideIcon;
    label: string;
    value: string;
    hint: string;
}) {
    return (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-5 shadow-xs">
            <div className="flex items-start justify-between gap-4">
                <div>
                    <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                        {label}
                    </p>

                    <p className="mt-2 font-mono text-2xl font-semibold tabular-nums text-[var(--color-foreground)]">
                        {value}
                    </p>

                    <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                        {hint}
                    </p>
                </div>

                <ToneIconTile
                    icon={icon}
                    tone="muted"
                    size="md"
                />
            </div>
        </div>
    );
}
function toCodalUrl(
    url: string,
): string | null {
    if (!url.trim()) {
        return null;
    }

    try {
        return new URL(
            url,
            "https://www.codal.ir",
        ).toString();
    } catch {
        return null;
    }
}
function Field({
    label,
    placeholder,
}: {
    label: string;
    placeholder: string;
}) {
    return (
        <label className="space-y-1.5">
            <span className="text-xs font-medium text-[var(--color-muted-foreground)]">
                {label}
            </span>

            <input
                type="text"
                placeholder={placeholder}
                className="h-10 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 text-sm outline-none transition focus:border-[var(--color-ring)] focus:ring-2 focus:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.10)]"
            />
        </label>
    );


}