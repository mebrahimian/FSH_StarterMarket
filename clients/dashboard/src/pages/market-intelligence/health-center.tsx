import {
    Activity,
    Database,
    ChartNoAxesCombined,
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
import {
    useMutation,
    useQuery,
} from "@tanstack/react-query";
import {
    getCodalDataQuality,
    searchDisclosures,
    getFiscalYearSales,
    getCodalJobStatus,
    getDataQualityIssues,
    queueCodalSymbolBackfill,
    type DataQualityIssue,
    type FiscalYearSales,
    type DisclosureParseStatus,
    type DisclosureSortBy,
} from "@/api/market-intelligence";

import {
    useEffect,
    useRef,
    useState,
    type PointerEvent as ReactPointerEvent,
} from "react";


import { useTranslation } from "react-i18next";
import DateObject from "react-date-object";
import DatePicker from "react-multi-date-picker";

import persian from "react-date-object/calendars/persian";
import persianFa from "react-date-object/locales/persian_fa";
import type { ComponentType } from "react";
import type { Calendar, Locale } from "react-date-object";
type PersianDatePickerProps = {
    value: DateObject | null;
    onChange: (value: DateObject | null) => void;
    calendar: Calendar | Omit<Calendar, "leapsLength">;
    locale: Locale;
    format?: string;
    calendarPosition?: string;
    inputClass?: string;
    fixRelativePosition?: boolean
    containerClassName?: string;
    disabled?: boolean;
    placeholder?: string;
};

const PersianDatePicker =
    DatePicker as unknown as ComponentType<PersianDatePickerProps>;
import {
    codalLetterCategoryOptions,
} from "@/lib/market-intelligence/codal-letter-categories";

export function MarketHealthCenterPage() {
    const [rtFilter, ] = useState("");
    const [letFilter, ] = useState("");
    const [sortBy, ] = useState<DisclosureSortBy>("publishDateTime");
    const [sortDir, ] = useState<"asc" | "desc">("desc");
    const [fiscalYearSales, setFiscalYearSales] =
        useState<FiscalYearSales | null>(null);
    const [isSalesDialogOpen, setIsSalesDialogOpen] = useState(false);
    const [backfillSymbol, setBackfillSymbol] = useState("");

    const [backfillFromDate, setBackfillFromDate] = useState<DateObject | null>(null);
    const [backfillToDate, setBackfillToDate] = useState<DateObject | null>(null);
    const [backfillMessage, setBackfillMessage] = useState("");
    const [backfillJobId, setBackfillJobId] = useState<string | null>(null);

    const {
        data: dataQualityIssues = [],
        isLoading: isDataQualityIssuesLoading,
        refetch: refetchDataQualityIssues,
    } = useQuery<DataQualityIssue[]>({
        queryKey: ["market-intelligence", "data-quality-issues"],
        queryFn: getDataQualityIssues,
    });
    
    const handleSalesClick = async (
        symbol: string,
        title: string,
    ) => {
        const result = await getFiscalYearSales(
            symbol,
            title,
        );

        setFiscalYearSales(result);
        setIsSalesDialogOpen(true);

        console.log("open sales dialog");
    };
    const backfillMutation = useMutation({
        mutationFn: queueCodalSymbolBackfill,

        onSuccess: (result) => {
            setBackfillJobId(result.jobId);
            setBackfillMessage(
                tMarket("healthCenter.backfill.queued", {
                    jobId: result.jobId,
                }),
            );
        },

        onError: () => {
            setBackfillMessage(
                tMarket("healthCenter.backfill.queueError"),
            );
        },
    });
    const backfillStatusQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "backfill-job",
            backfillJobId,
        ],

        queryFn: () => {
            if (!backfillJobId) {
                throw new Error("JobId is required.");
            }

            return getCodalJobStatus(backfillJobId);
        },

        enabled: backfillJobId !== null,

        refetchInterval: (query) => {
            const status =
                query.state.data?.status;

            if (
                status === "Succeeded" ||
                status === "Failed" ||
                status === "Deleted"
            ) {
                return false;
            }

            return 15_000;
        },
    });
    useEffect(() => {
        const status =
            backfillStatusQuery.data?.status;

        if (status === "Succeeded") {
            window.location.reload();
            return;
        }

        if (
            status === "Failed" ||
            status === "Deleted"
        ) {
            setBackfillMessage(
                tMarket("healthCenter.backfill.finishedWithStatus", {
                    status,
                }),
            );

            setBackfillJobId(null);
        }
    }, [backfillStatusQuery.data?.status]);
    const normalizeDateDigits = (value: string) =>
        value
            .replace(/[۰-۹]/g, (digit) =>
                String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)),
            )
            .replace(/[٠-٩]/g, (digit) =>
                String("٠١٢٣٤٥٦٧٨٩".indexOf(digit)),
            );

    const handleRunBackfill = () => {
        const symbol = backfillSymbol.trim();

        const fromDate = backfillFromDate
            ? normalizeDateDigits(
                backfillFromDate.format("YYYY/MM/DD"),
            )
            : "";

        const toDate = backfillToDate
            ? normalizeDateDigits(
                backfillToDate.format("YYYY/MM/DD"),
            )
            : "";

        if (!symbol || !fromDate || !toDate) {
            setBackfillMessage(
                tMarket("healthCenter.backfill.validationRequired"),
            );

            return;
        }

        const normalizedFromDate =
            fromDate <= toDate ? fromDate : toDate;

        const normalizedToDate =
            fromDate <= toDate ? toDate : fromDate;

        setBackfillMessage("");

        backfillMutation.mutate({
            symbol,
            fromDate: normalizedFromDate,
            toDate: normalizedToDate,
        });
    };
    const { t } = useTranslation("disclosures");
    const { t: tMarket } = useTranslation("marketIntelligence");
    const [statusFilter, ] = useState<DisclosureParseStatus | null>(null);
    const [bulkBackfillRunning, setBulkBackfillRunning] =
        useState(false);

    const [bulkBackfillMessage, setBulkBackfillMessage] =
        useState("");
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
    const rule0Issues: DataQualityIssue[] =
        dataQualityQuery.data?.historyCoverage.gaps.flatMap(
            (gap) =>
                gap.missingPeriodDetails.map(
                    (missingPeriod) => ({
                        symbol: gap.symbol,
                        yearEndDate: null,
                        periodEndDate:
                            missingPeriod.periodEndDate,
                        publishDate:
                            missingPeriod.publishDate,
                        issueCode: "MissingPeriod",
                        previousValue: null,
                        currentValue: null,
                    }),
                ),
        ) ?? [];
    console.log(
    "Rule 0 Issues:",
    rule0Issues.length,
    rule0Issues,
);
    const [salesWindowOffset, setSalesWindowOffset] =
        useState({ x: 0, y: 0 });

    const salesWindowDragRef = useRef<{
        startX: number;
        startY: number;
        offsetX: number;
        offsetY: number;
    } | null>(null);

    const handleSalesWindowPointerDown = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        if (event.button !== 0) {
            return;
        }

        event.currentTarget.setPointerCapture(event.pointerId);

        salesWindowDragRef.current = {
            startX: event.clientX,
            startY: event.clientY,
            offsetX: salesWindowOffset.x,
            offsetY: salesWindowOffset.y,
        };
    };

    const handleSalesWindowPointerMove = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        const drag = salesWindowDragRef.current;

        if (!drag) {
            return;
        }

        setSalesWindowOffset({
            x: drag.offsetX + event.clientX - drag.startX,
            y: drag.offsetY + event.clientY - drag.startY,
        });
    };

    const handleSalesWindowPointerUp = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        if (event.currentTarget.hasPointerCapture(event.pointerId)) {
            event.currentTarget.releasePointerCapture(event.pointerId);
        }

        salesWindowDragRef.current = null;
    };
    const waitForBackfillJob = async (
        jobId: string,
    ) => {
        while (true) {
            const job =
                await getCodalJobStatus(jobId);

            if (job.status === "Succeeded") {
                return true;
            }

            if (
                job.status === "Failed" ||
                job.status === "Deleted"
            ) {
                return false;
            }

            await new Promise<void>(
                (resolve) =>
                    window.setTimeout(
                        resolve,
                        2_000,
                    ),
            );
        }
    };
    const handleQueueAllBackfills = async () => {
        const issueRanges = new Map<
            string,
            {
                fromDate: string;
                toDate: string;
            }
        >();

        for (const issue of [
            ...rule0Issues,
            ...dataQualityIssues,
        ]) {
            const symbol = issue.symbol?.trim();

            const publishDate =
                issue.publishDate?.trim();

            if (!symbol || !publishDate) {
                continue;
            }

            const date = publishDate;

            const current =
                issueRanges.get(symbol);

            if (!current) {
                issueRanges.set(symbol, {
                    fromDate: date,
                    toDate: date,
                });

                continue;
            }

            if (date < current.fromDate) {
                current.fromDate = date;
            }

            if (date > current.toDate) {
                current.toDate = date;
            }
        }

        const backfillItems =
            Array.from(issueRanges.entries())
                .map(
                    ([
                        symbol,
                        range,
                    ]) => ({
                        symbol,
                        ...range,
                    }),
                );

        if (backfillItems.length === 0) {
            setBulkBackfillMessage(
                tMarket(
                    "healthCenter.backfill.noIssues",
                ),
            );

            return;
        }

        setBulkBackfillRunning(true);

        let queued = 0;
        let failed = 0;

        try {
            for (const item of backfillItems) {
                try {
                    setBulkBackfillMessage(
                        tMarket(
                            "healthCenter.backfill.bulkCurrent",
                            {
                                symbol: item.symbol,
                                completed:
                                    queued +
                                    failed,
                                total:
                                    backfillItems.length,
                            },
                        ),
                    );

                    const result =
                        await queueCodalSymbolBackfill({
                            symbol: item.symbol,
                            fromDate:
                                item.fromDate,
                            toDate:
                                item.toDate,
                        });

                    const succeeded =
                        await waitForBackfillJob(
                            result.jobId,
                        );

                    if (succeeded) {
                        queued++;
                    } else {
                        failed++;
                    }
                } catch {
                    failed++;
                }
            }

            setBulkBackfillMessage(
                tMarket(
                    "healthCenter.backfill.bulkFinished",
                    {
                        queued,
                        failed,
                    },
                ),
            );
        } finally {
            setBulkBackfillRunning(false);
        }
    };
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
                            void refetchDataQualityIssues();
                            void dataQualityQuery.refetch();
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
            <section className="mt-2 pt-5 pr-5 pb-0 mb-0 overflow-hidden rounded-xl border border-[var(--color-border)]  shadow-xs sm:h-[280px]">
                
                <h2 className="-mt-5 mb-2 text-center text-base font-semibold">
                    {tMarket("healthCenter.dataQualityIssues")}
                </h2>
               
                <div className="max-h-60 overflow-y-auto pr-1">
                    <div className="-mb-2 grid min-w-[620px] grid-cols-[80px_200px_150px_150px_150px_100px] items-center gap-6 border-b border-[var(--color-border)] text-center text-xs font-semibold text-[var(--color-muted-foreground)]">
                        <span>{t("table.symbol")}</span>
                        <span>{t("table.disclosure")}</span>
                        <span>{t("table.published")}</span>
                        <span>{t("table.endYearDate")}</span>
                        <span>{t("table.discrepancy")}</span>
                        <span />
                    </div>
                    {isDataQualityIssuesLoading || dataQualityQuery.isLoading ? (
                        <div className="py-6 text-center text-sm text-[var(--color-muted-foreground)]">
                            {tMarket("healthCenter.dataQualityChecking")}
                        </div>
                    ) : (
                            [...rule0Issues, ...dataQualityIssues]
                                .map((issue) => {
                            const issueDescription =
                                `اشکال در اعلامیه کدال ${issue.periodEndDate ?? "—"}`;

                            return (
                                <div
                                    key={`${issue.symbol}-${issue.yearEndDate}-${issue.periodEndDate}-${issue.issueCode}`}
                                    onClick={() =>
                                        setBackfillSymbol(issue.symbol)
                                    }
                                    className="-mb-3 grid min-w-[620px] cursor-pointer grid-cols-[80px_200px_150px_150px_150px_100px] items-center gap-6 border-b py-2 transition-colors hover:bg-[var(--color-muted)] last:border-0"
                                >
                                    <div className="font-semibold">
                                        {issue.symbol}
                                    </div>

                                    <div className="min-w-0 truncate text-sm text-[var(--color-muted-foreground)]">
                                        {issueDescription}
                                    </div>
                                    <div
                                        dir="ltr"
                                        className="text-center text-sm tabular-nums"
                                    >
                                        {issue.publishDate ?? "—"}
                                    </div>
                                    <div
                                        dir="ltr"
                                        className="text-center text-sm tabular-nums"
                                    >
                                        {issue.yearEndDate}
                                    </div>

                                    <div
                                        dir="ltr"
                                        className="text-center text-sm tabular-nums"
                                    >
                                        {issue.previousValue?.toLocaleString() ?? "—"}
                                        {"  →  "}
                                        {issue.currentValue?.toLocaleString() ?? "—"}
                                    </div>

                                    <div className="flex justify-center">
                                        <button
                                            type="button"
                                            onClick={() =>
                                                void handleSalesClick(
                                                    issue.symbol,
                                                    issueDescription,
                                                )
                                            }
                                            title={tMarket("healthCenter.viewSales")}
                                            className="grid size-8 place-items-center rounded-md text-[var(--color-success)] transition-colors hover:bg-[var(--color-muted)]"
                                        >
                                            <ChartNoAxesCombined className="size-[22px]" />
                                        </button>
                                    </div>
                                </div>
                            );
                        })
                    )}
                </div>

            </section>
            <section className="relative mt-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-4 py-7.5 shadow-xs">
            
                <div className="mb-4">
                    <h2 className="-mt-5 text-base font-semibold text-[var(--color-foreground)]">
                        {tMarket("healthCenter.backfill.description")}
                    </h2>
                </div>

                <div className="-mt-3 grid gap-4 md:grid-cols-[1fr_1fr_1fr_auto] md:items-end ">
                    <label>
                        

                        <input
                            type="text"
                            value={backfillSymbol}
                            onChange={(event) =>
                                setBackfillSymbol(
                                    event.target.value,
                                )
                            }
                            placeholder={tMarket(
                                "healthCenter.backfill.symbolPlaceholder",
                            )}
                            className="h-8 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 text-sm outline-none transition focus:border-[var(--color-ring)]"
                        />
                    </label>

                    <label>
                        <PersianDatePicker
                            value={backfillFromDate}
                            onChange={(value) =>
                                setBackfillFromDate(
                                    value ?? null,
                                )
                            }
                            calendar={persian}
                            locale={persianFa}
                            format="YYYY/MM/DD"
                            calendarPosition="bottom-right"
                            containerClassName="w-full"
                            fixRelativePosition
                            inputClass="h-8 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 text-sm outline-none"
                            placeholder={tMarket("healthCenter.backfill.fromDate")}
                        />
                    </label>

                    <label>
                        <PersianDatePicker
                            value={backfillToDate}
                            onChange={(value) =>
                                setBackfillToDate(
                                    value ?? null,
                                )
                            }
                            calendar={persian}
                            locale={persianFa}
                            format="YYYY/MM/DD"
                            calendarPosition="bottom-right"
                            containerClassName="w-full"
                            fixRelativePosition
                            inputClass="h-8 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 text-sm outline-none"
                            placeholder={tMarket("healthCenter.backfill.toDate")}
                        />
                    </label>

                    <Button
                        type="button"
                        onClick={handleRunBackfill}
                        disabled={backfillMutation.isPending}
                    >
                        {backfillMutation.isPending
                            ? tMarket("healthCenter.backfill.send")
                            : tMarket("healthCenter.backfill.run")}
                    </Button>
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() =>
                            void handleQueueAllBackfills()
                        }
                        disabled={bulkBackfillRunning}
                    >
                        {bulkBackfillRunning
                            ? tMarket(
                                "healthCenter.backfill.bulkRunning",
                            )
                            : tMarket(
                                "healthCenter.backfill.runAll",
                            )}
                    </Button>
                </div>
                {bulkBackfillMessage && (
                    <div className="text-sm text-[var(--color-muted-foreground)]">
                        {bulkBackfillMessage}
                    </div>
                )}
                {backfillMessage && (
                    <div className="-mt-0 text-sm text-[var(--color-muted-foreground)]">
                        {backfillMessage}
                    </div>
                )}
            </section>
            {isSalesDialogOpen && fiscalYearSales && (
                <div
                    
                    className="text-center fixed left-1/2 top-20 z-50 w-[min(600px,calc(100vw-2rem))] overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-2xl"
                    style={{
                        transform: `translate(calc(-50% + ${salesWindowOffset.x}px), ${salesWindowOffset.y}px)`,
                    }}
                >
                    <div
                        onPointerDown={handleSalesWindowPointerDown}
                        onPointerMove={handleSalesWindowPointerMove}
                        onPointerUp={handleSalesWindowPointerUp}
                        onPointerCancel={handleSalesWindowPointerUp}
                        className="relative flex touch-none select-none items-center justify-center border-b border-[var(--color-border)] px-4 py-3 cursor-grab active:cursor-grabbing"
                    >
                        <div className="text-center font-semibold">
                            {tMarket("healthCenter.fiscalYearSalesTitle", {
                                symbol: fiscalYearSales.symbol,
                            })}{" "}
                            <span dir="ltr">
                                {fiscalYearSales.yearEndDate}
                            </span>
                        </div>

                        <button
                            type="button"
                            onPointerDown={(event) =>
                                event.stopPropagation()
                            }
                            onClick={() =>
                                setIsSalesDialogOpen(false)
                            }
                            className="absolute left-3 grid size-8 place-items-center rounded-md text-xl leading-none text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]"
                            aria-label={tMarket("actions.close")}
                            title={tMarket("actions.close")}
                        >
                            ×
                        </button>
                    </div>

                    <div className="max-h-[70vh] overflow-auto p-4">
                        <table className="w-full border-collapse text-center text-sm">
                            <thead>
                                <tr className="border-b text-xs font-semibold text-[var(--color-muted-foreground)]">
                                    <th className="px-3 py-2">
                                        {tMarket("salesTable.period")}
                                    </th>

                                    <th className="px-3 py-2">
                                        {tMarket("salesTable.monthSales")}
                                    </th>

                                    <th className="px-3 py-2">
                                        {tMarket("salesTable.yearToDate")}
                                    </th>

                                    <th className="px-3 py-2">
                                        {tMarket("salesTable.previousYearToDate")}
                                    </th>
                                </tr>
                            </thead>

                            <tbody>
                                {fiscalYearSales.rows.map((row) => (
                                    <tr
                                        key={row.periodEndDate}
                                        className="border-b last:border-0"
                                    >
                                        <td
                                            dir="ltr"
                                            className="px-3 py-2 tabular-nums"
                                        >
                                            {row.periodEndDate}
                                        </td>

                                        <td className="px-3 py-2 tabular-nums">
                                            {row.periodAmount?.toLocaleString() ??
                                                "—"}
                                        </td>

                                        <td className="px-3 py-2 tabular-nums">
                                            {row.yearToDateAmount?.toLocaleString() ??
                                                "—"}
                                        </td>

                                        <td className="px-3 py-2 tabular-nums">
                                            {row.previousYearToDateAmount?.toLocaleString() ??
                                                "—"}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
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
