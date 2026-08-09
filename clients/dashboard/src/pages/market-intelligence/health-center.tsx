import {
    Activity,
    Database,
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
    const recentDisclosures =
        disclosuresQuery.data?.items ?? [];
    return (
        <div className="-mt-5">
            <PageHero
                className="-mt-3 [&>div]:!py-3 sm:[&>div]:!py-3"
                eyebrow="Market Intelligence · Data Health"
                title="مرکز سلامت صدف بورس"
                subtitle="کنترل پوشش، کیفیت و سلامت اطلاعات بازار و ابزارهای بازیابی داده"
                actions={
                    <Button
                        type="button"
                        variant="outline"
                        size="sm"
                    >
                        <RefreshCw className="mr-1.5 size-3.5" />
                        بروزرسانی
                    </Button>
                }
            />

            <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <HealthStat
                    icon={Activity}
                    label="نمادهای فعال"
                    value="—"
                    hint="Active symbols"
                />

                <HealthStat
                    icon={Newspaper}
                    label="Disclosure"
                    value={
                        disclosuresQuery.data
                            ? disclosuresQuery.data.totalCount.toLocaleString()
                            : "—"
                    }
                    hint="کل اطلاعیه‌ها"
                />

                <HealthStat
                    icon={Database}
                    label="گزارش‌های ماهانه"
                    value="—"
                    hint="Monthly summaries"
                />

                <HealthStat
                    icon={HeartPulse}
                    label="نیازمند بررسی"
                    value="—"
                    hint="Gap / Failed / Missing"
                />
            </section>
            <section className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-5 shadow-xs sm:h-[196px] lg:h-[212.5px]">
                <div className="-mt-6 flex w-full items-end justify-between gap-3">
                    <div className="flex items-end gap-3">
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
                <h2 className="mb-0 text-center text-base font-semibold">
                    آخرین Disclosureها
                </h2>

                <div className="max-h-46 space-y-2 overflow-y-auto pr-1">
                    {recentDisclosures.map((disclosure) => (
                        <div
                            key={disclosure.id}
                            className="flex items-center justify-between border-b py-0 last:border-0"
                        >
                            <span className="font-semibold">
                                {disclosure.symbol}
                            </span>

                            <span className="text-sm text-[var(--color-muted-foreground)]">
                                {disclosure.tracingNo}
                            </span>
                        </div>
                    ))}
                </div>
            </section>
            <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-4 py-3 shadow-xs">
                <div className="mb-5">
                    <h2 className="text-base font-semibold text-[var(--color-foreground)]">
                        بازیابی اطلاعات نماد
                    </h2>

                    <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
                        اجرای Symbol Backfill برای یک نماد و بازه مشخص
                    </p>
                </div>

                <div className="-mt-5 grid gap-4 md:grid-cols-3">
                    <Field
                        label="نماد"
                        placeholder="مثلاً وصبا"
                    />

                    <Field
                        label="از تاریخ"
                        placeholder="1403/01/01"
                    />

                    <Field
                        label="تا تاریخ"
                        placeholder="1403/12/30"
                    />
                </div>

                <div className="mt-1 flex justify-end">
                    <Button type="button">
                        اجرای Symbol Backfill
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