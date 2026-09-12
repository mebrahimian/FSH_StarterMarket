import {
    Activity,
    Database,
    ChartNoAxesCombined,
    HeartPulse,
    Link2,
    Newspaper,
    RefreshCw,
    type LucideIcon,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
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
    getCodalIncrementalSchedule,
    updateCodalIncrementalSchedule,
    type CodalIncrementalSchedule,
    type DataQualityIssue,
    type FiscalYearSales,
    type DisclosureParseStatus,
    type DisclosureSortBy,
} from "@/api/market-intelligence";

import {
    useEffect,
    useState,
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

import { FiscalYearSalesDialog } from
    "@/components/market-intelligence/fiscal-year-sales-dialog";
type ScheduleIntervalSelectProps = {
    value: number;
    onChange: (value: number) => void;
};

function ScheduleIntervalSelect({
    value,
    onChange,
}: ScheduleIntervalSelectProps) {
    const { t: tMarket } = useTranslation("marketIntelligence");
    const options = [5, 10, 15, 30, 60];

    return (
        <select
            value={value}
            onChange={(event) => onChange(Number(event.target.value))}
            className="h-9 rounded-md border border-[var(--color-border)] bg-transparent px-2"
        >
            {options.map((minutes) => (
                <option key={minutes} value={minutes}>
                    {tMarket("codalSchedule.minutes", { count: minutes })}
                </option>
            ))}
        </select>
    );
}
export function MarketHealthCenterPage() {
    const navigate = useNavigate();
    const [rtFilter, ] = useState("");
    const [letFilter, ] = useState("");
    const [sortBy, ] = useState<DisclosureSortBy>("publishDateTime");
    const [sortDir, ] = useState<"asc" | "desc">("desc");
    const [fiscalYearSales, setFiscalYearSales] =
        useState<FiscalYearSales | null>(null);
    const [isSalesDialogOpen, setIsSalesDialogOpen] = useState(false);
    const [backfillSymbol, setBackfillSymbol] = useState("");
    const [isScheduleOpen, setIsScheduleOpen] = useState(false);
    const [backfillFromDate, setBackfillFromDate] = useState<DateObject | null>(null);
    const [backfillToDate, setBackfillToDate] = useState<DateObject | null>(null);
    const [backfillMessage, setBackfillMessage] = useState("");
    const [backfillJobId, setBackfillJobId] = useState<string | null>(null);
    const [codalSchedule, setCodalSchedule] =
        useState<CodalIncrementalSchedule | null>(null);
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
    const handleSalesNavigation = async (
        yearEndDate: string | null,
    ) => {
        if (!yearEndDate || !fiscalYearSales) {
            return;
        }

        const result = await getFiscalYearSales(
            fiscalYearSales.symbol,
            "",
            yearEndDate,
        );

        setFiscalYearSales(result);
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

    const [bulkStartIndex, setBulkStartIndex] =
        useState(0);

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
    const codalScheduleQuery = useQuery<CodalIncrementalSchedule>({
        queryKey: ["market-intelligence", "codal-incremental-schedule"],
        queryFn: getCodalIncrementalSchedule,
    });
    const codalScheduleMutation = useMutation({
        mutationFn: updateCodalIncrementalSchedule,
        onSuccess: (result) => {
            setCodalSchedule(result);
        },
    });
    useEffect(() => {
        if (codalScheduleQuery.data) {
            setCodalSchedule(codalScheduleQuery.data);
        }
    }, [codalScheduleQuery.data]);

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
        const issuesToProcess =
            dataQualityIssues.slice(bulkStartIndex);
        for (const issue of issuesToProcess) {
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
    const needsReviewCount =
        new Set(
            dataQualityIssues
                .map((issue) => issue.symbol?.trim())
                .filter(
                    (symbol): symbol is string =>
                        Boolean(symbol),
                ),
        ).size;


    return (
        <div className="-mt-5">
            <PageHero   className="-mt-3 [&>div]:!py-3 sm:[&>div]:!py-3"
                title={tMarket("healthCenter.title")}
                subtitle={tMarket("healthCenter.subtitle")}
                actions={
                    <div className="flex items-center gap-2">
                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => setIsScheduleOpen((current) => !current)}
                        >
                            {isScheduleOpen
                                ? tMarket("codalSchedule.close")
                                : tMarket("codalSchedule.open")}
                        </Button>
                        
                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            className="gap-2"
                            onClick={() =>
                                navigate(
                                    "/market-intelligence/portfolio-matching",
                                )
                            }
                        >
                            <Link2 className="size-3.5" />
                            {tMarket("portfolioMatching")}
                        </Button>
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
                                    (
                                        disclosuresQuery.isFetching ||
                                        dataQualityQuery.isFetching
                                    ) &&
                                    "animate-spin",
                                )}
                            />
                            {t("actions.refresh")}
                        </Button>
                        
                    </div>
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
                    value={needsReviewCount.toLocaleString()}
                    hint={tMarket("healthCenter.gapFailedMissing")}
                />
            </section>

            {isScheduleOpen && codalSchedule && (
                <section className="mt-3 rounded-xl border border-[var(--color-border)] p-4 shadow-xs">
                    <div className="mb-4">
                        <h2 className="text-base font-semibold">
                            {tMarket("codalSchedule.title")}
                        </h2>
                        <p>
                            {tMarket("codalSchedule.description")}
                        </p>
                    </div>

                    <div className="grid gap-4 sm:grid-cols-3">
                        <label className="space-y-1.5 text-sm">
                            <span>{tMarket("codalSchedule.startHour")}</span>
                            <select
                                value={codalSchedule.startHour}
                                onChange={(event) =>
                                    setCodalSchedule({
                                        ...codalSchedule,
                                        startHour: Number(event.target.value),
                                    })
                                }
                                className="h-9 w-full rounded-md border border-[var(--color-border)] bg-transparent px-3"
                            >
                                {Array.from({ length: 24 }, (_, hour) => (
                                    <option key={hour} value={hour}>
                                        {hour.toString().padStart(2, "0")}:00
                                    </option>
                                ))}
                            </select>
                        </label>

                        <label className="space-y-1.5 text-sm">
                            <span>{tMarket("codalSchedule.morningEndHour")}  </span>
                            <select
                                value={codalSchedule.morningEndHour}
                                onChange={(event) =>
                                    setCodalSchedule({
                                        ...codalSchedule,
                                        morningEndHour: Number(event.target.value),
                                    })
                                }
                                className="h-9 w-full rounded-md border border-[var(--color-border)] bg-transparent px-3"
                            >
                                {Array.from({ length: 24 }, (_, hour) => (
                                    <option key={hour} value={hour}>
                                        {hour.toString().padStart(2, "0")}:00
                                    </option>
                                ))}
                            </select>
                        </label>

                        <label className="space-y-1.5 text-sm">
                            <span>{tMarket("codalSchedule.endHour")}</span>
                            <select
                                value={codalSchedule.endHour}
                                onChange={(event) =>
                                    setCodalSchedule({
                                        ...codalSchedule,
                                        endHour: Number(event.target.value),
                                    })
                                }
                                className="h-9 w-full rounded-md border border-[var(--color-border)] bg-transparent px-3"
                            >
                                {Array.from({ length: 24 }, (_, hour) => (
                                    <option key={hour} value={hour}>
                                        {hour.toString().padStart(2, "0")}:00
                                    </option>
                                ))}
                            </select>
                        </label>
                    </div>
                    <div className="mt-5 border-t border-[var(--color-border)] pt-4">
                        <div className="mb-4 flex items-center gap-2 text-sm">
                            <span>{tMarket("codalSchedule.busyPeriod")}</span>

                            <select
                                value={codalSchedule.busyPeriodEndDay}
                                onChange={(event) =>
                                    setCodalSchedule({
                                        ...codalSchedule,
                                        busyPeriodEndDay: Number(event.target.value),
                                    })
                                }
                                className="h-9 rounded-md border border-[var(--color-border)] bg-transparent px-3"
                            >
                                {Array.from({ length: 31 }, (_, index) => index + 1).map((day) => (
                                    <option key={day} value={day}>
                                        {day}
                                    </option>
                                ))}
                            </select>

                            <span>{tMarket("codalSchedule.firstDaysOfPersianMonth")}</span>
                        </div>

                        <div className="grid gap-4 lg:grid-cols-2">
                            <div className="rounded-lg border border-[var(--color-border)] p-3">
                                <div className="mb-3 font-medium">
                                    {tMarket("codalSchedule.weekdays")}
                                </div>

                                <div className="grid grid-cols-3 gap-2 text-sm">
                                    <span />
                                    <span className="text-center">{tMarket("codalSchedule.busyDays")}</span>
                                    <span className="text-center">{tMarket("codalSchedule.normalDays")}</span>

                                    <span>{tMarket("codalSchedule.morning")}</span>
                                    <ScheduleIntervalSelect
                                        value={codalSchedule.busyMorningMinutes}
                                        onChange={(value) =>
                                            setCodalSchedule({
                                                ...codalSchedule,
                                                busyMorningMinutes: value,
                                            })
                                        }
                                    />
                                    <ScheduleIntervalSelect
                                        value={codalSchedule.normalMorningMinutes}
                                        onChange={(value) =>
                                            setCodalSchedule({
                                                ...codalSchedule,
                                                normalMorningMinutes: value,
                                            })
                                        }
                                    />

                                    <span>{tMarket("codalSchedule.afternoon")}</span>
                                    <ScheduleIntervalSelect
                                        value={codalSchedule.busyAfternoonMinutes}
                                        onChange={(value) =>
                                            setCodalSchedule({
                                                ...codalSchedule,
                                                busyAfternoonMinutes: value,
                                            })
                                        }
                                    />
                                    <ScheduleIntervalSelect
                                        value={codalSchedule.normalAfternoonMinutes}
                                        onChange={(value) =>
                                            setCodalSchedule({
                                                ...codalSchedule,
                                                normalAfternoonMinutes: value,
                                            })
                                        }
                                    />
                                </div>
                            </div>
                        </div>
                        <div className="rounded-lg border border-[var(--color-border)] p-3">
                            <div className="mb-3 font-medium">
                                {tMarket("codalSchedule.thursday")}
                            </div>

                            <div className="grid grid-cols-3 gap-2 text-sm">
                                <span />
                                <span>{tMarket("codalSchedule.busyPeriod")}</span>
                                {tMarket("codalSchedule.weekdays")}

                                <span>{tMarket("codalSchedule.morning")}</span>
                                <ScheduleIntervalSelect
                                    value={codalSchedule.thursdayBusyMorningMinutes}
                                    onChange={(value) =>
                                        setCodalSchedule({
                                            ...codalSchedule,
                                            thursdayBusyMorningMinutes: value,
                                        })
                                    }
                                />
                                <ScheduleIntervalSelect
                                    value={codalSchedule.thursdayNormalMorningMinutes}
                                    onChange={(value) =>
                                        setCodalSchedule({
                                            ...codalSchedule,
                                            thursdayNormalMorningMinutes: value,
                                        })
                                    }
                                />

                                <span>{tMarket("codalSchedule.afternoon")}</span>
                                <ScheduleIntervalSelect
                                    value={codalSchedule.thursdayBusyAfternoonMinutes}
                                    onChange={(value) =>
                                        setCodalSchedule({
                                            ...codalSchedule,
                                            thursdayBusyAfternoonMinutes: value,
                                        })
                                    }
                                />
                                <ScheduleIntervalSelect
                                    value={codalSchedule.thursdayNormalAfternoonMinutes}
                                    onChange={(value) =>
                                        setCodalSchedule({
                                            ...codalSchedule,
                                            thursdayNormalAfternoonMinutes: value,
                                        })
                                    }
                                />
                            </div>
                        </div>
                        <div className="mt-4 rounded-lg border border-[var(--color-border)] p-3">
                            <div className="grid items-center gap-3 sm:grid-cols-[1fr_200px]">
                                <span className="font-medium">
                                    {tMarket("codalSchedule.friday")}
                                </span>

                                <ScheduleIntervalSelect
                                    value={codalSchedule.fridayMinutes}
                                    onChange={(value) =>
                                        setCodalSchedule({
                                            ...codalSchedule,
                                            fridayMinutes: value,
                                        })
                                    }
                                />
                            </div>
                        </div>
                        <div className="mt-4 flex justify-end border-t border-[var(--color-border)] pt-4">
                            <Button
                                type="button"
                                disabled={!codalSchedule || codalScheduleMutation.isPending}
                                onClick={() => {
                                    if (codalSchedule) {
                                        codalScheduleMutation.mutate(codalSchedule);
                                    }
                                }}
                            >
                                {codalScheduleMutation.isPending
                                    ? tMarket("codalSchedule.saving")
                                    : tMarket("codalSchedule.save")}
                            </Button>
                        </div>
                    </div>
                </section>
            )}
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
                            dataQualityIssues
                                .map((issue, index) => {
                                    const issueDescription =
                                        issue.issueCode === "MissingPortfolio"
                                            ? tMarket("missingPortfolio", {
                                                period: issue.periodEndDate ?? "—",
                                            })
                                            : issue.issueCode === "IncompletePortfolioMetadata"
                                                ? tMarket("incompletePortfolioMetadata", {
                                                    period: issue.periodEndDate ?? "—",
                                                })
                                                : tMarket("codalDisclosureIssue", {
                                                    period: issue.periodEndDate ?? "—",
                                                });
                                    return (
                                        <div
                                            key={`${issue.symbol}-${issue.yearEndDate}-${issue.periodEndDate}-${issue.issueCode}`}
                                            onClick={() => {
                                                setBackfillSymbol(issue.symbol);
                                                setBulkStartIndex(index);
                                            }}
                                            className={cn(
                                                "-mb-3 grid min-w-[620px] cursor-pointer grid-cols-[80px_200px_150px_150px_150px_100px] items-center gap-6 border-b py-2 transition-colors hover:bg-[var(--color-muted)] last:border-0",
                                                bulkStartIndex === index &&
                                                "bg-[var(--color-muted)]",
                                            )}
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
            <FiscalYearSalesDialog
                open={isSalesDialogOpen}
                sales={fiscalYearSales}
                onClose={() =>
                    setIsSalesDialogOpen(false)
                }
                onPrevious={() =>
                    void handleSalesNavigation(
                        fiscalYearSales?.previousYearEndDate ?? null,
                    )
                }
                onNext={() =>
                    void handleSalesNavigation(
                        fiscalYearSales?.nextYearEndDate ?? null,
                    )
                }
            />


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
