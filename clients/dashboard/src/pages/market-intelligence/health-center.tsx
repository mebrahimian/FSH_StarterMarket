import {
    Activity,
    Clock3,    
    ChartNoAxesCombined,    
    Link2,
    CalendarClock,    
    RefreshCw,    
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { CodalOperationsPanel } from "./codal-operations-panel";
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
    getDisclosureStats,
    getFiscalYearSales,
    getCodalJobStatus,
    getDataQualityIssues,
    queueCodalSymbolBackfill,
    getCodalIncrementalSchedule,
    rebuildMissingSalesPerformanceSnapshots,
    getSalesPerformanceRebuildStatus,
    updateCodalIncrementalSchedule,
    backfillTsetmcPrices,
    getCompanyProfiles,
    getBackgroundJobStatuses,
    type CompanyProfile,
    type CodalIncrementalSchedule,
    type DataQualityIssue,
    type FiscalYearSales,
    type DisclosureParseStatus,
    type DisclosureSortBy,
    type BackgroundJobStatus,
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

const PersianDatePicker = DatePicker as unknown as ComponentType<PersianDatePickerProps>;
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
    const [isJobStatusOpen, setIsJobStatusOpen] = useState(false);
    const navigate = useNavigate();
    const [rtFilter, ] = useState("");
    const [letFilter, ] = useState("");
    const [sortBy, ] = useState<DisclosureSortBy>("publishDateTime");
    const [sortDir, ] = useState<"asc" | "desc">("desc");
    const [fiscalYearSales, setFiscalYearSales] = useState<FiscalYearSales | null>(null);
    const [isSalesDialogOpen, setIsSalesDialogOpen] = useState(false);
    const [backfillSymbol, setBackfillSymbol] = useState("");
    const [debouncedBackfillSymbol, setDebouncedBackfillSymbol,] = useState("");
    const [symbolSuggestionsOpen, setSymbolSuggestionsOpen,] = useState(false);
    const [backfillInstrumentId, setBackfillInstrumentId] = useState<number | null>(null);
    const [isScheduleOpen, setIsScheduleOpen] = useState(false);
    const [backfillFromDate, setBackfillFromDate] = useState<DateObject | null>(null);
    const [backfillToDate, setBackfillToDate] = useState<DateObject | null>(null);
    const [backfillMessage, setBackfillMessage] = useState("");
    const [backfillJobId, setBackfillJobId] = useState<string | null>(null);
    const [codalSchedule, setCodalSchedule] = useState<CodalIncrementalSchedule | null>(null);
   
    useEffect(() => {
    const timer = window.setTimeout(() => {
        setDebouncedBackfillSymbol(
            backfillSymbol.trim(),
        );
    }, 300);

    return () => window.clearTimeout(timer);
}, [backfillSymbol]);
    
    
    const salesInsightRebuildMutation = useMutation({mutationFn: rebuildMissingSalesPerformanceSnapshots, });

    const salesPerformanceRebuildStatusQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "sales-performance-rebuild-status",
        ],
        queryFn: getSalesPerformanceRebuildStatus,

        enabled: salesInsightRebuildMutation.isSuccess,

        refetchInterval: (query) => {
            if (query.state.data?.isComplete) {
                return false;
            }

            return 5_000;
        },
    });

    const {
        data: dataQualityIssues = [],
        isLoading: isDataQualityIssuesLoading,
        refetch: refetchDataQualityIssues,
    } = useQuery<DataQualityIssue[]>({
        queryKey: ["market-intelligence", "data-quality-issues"],
        queryFn: getDataQualityIssues,
    });

     const handleSalesClick = async (symbol: string) => {
    const result = await getFiscalYearSales(symbol);

    setFiscalYearSales(result);
    setIsSalesDialogOpen(true);
};

    const handleSalesNavigation = async (
    yearEndDate: string | null,
) => {
    if (!yearEndDate || !fiscalYearSales) {
        return;
    }

    const result = await getFiscalYearSales(
        fiscalYearSales.symbol,
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
    const tsetmcBackfillMutation = useMutation({
        mutationFn: backfillTsetmcPrices,

        onSuccess: (result) => {
            setBackfillMessage(
                `تاریخچه قیمت TSETMC بازخوانی شد — ${result.inserted.toLocaleString()} رکورد درج شد.`,
            );
        },

        onError: () => {
            setBackfillMessage(
                "خطا در بازخوانی تاریخچه قیمت TSETMC.",
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
                
    const handleRunTsetmcBackfill = async () => {
    if (backfillInstrumentId === null) {
        return;
    }

    setBackfillMessage("");

    tsetmcBackfillMutation.mutate(
        backfillInstrumentId, );
    };
        
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

    const dataQualityQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "health-center",
            "data-quality",
        ],
        queryFn: () => getCodalDataQuality(5),
       
    });
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
       
    const disclosureStatsQuery = useQuery({
       queryKey: [
          "market-intelligence",
          "health-center",
          "disclosure-stats",
       ],
       queryFn: getDisclosureStats,
    });

    //card 1
    const { data: companyProfiles = [],} = useQuery<CompanyProfile[]>({
           queryKey: ["market-intelligence", "company-profiles"],
           queryFn: getCompanyProfiles,
        });

    const normalizeSymbolForSearch = (value: string,) =>
    value
        .trim()
        .replace(/ي/g, "ی")
        .replace(/ك/g, "ک")
        .replace(/\u200c/g, "")
        .replace(/\s+/g, "");

    const searchText = normalizeSymbolForSearch(debouncedBackfillSymbol, );

    const matchingCompanyProfiles =  searchText.length >= 2 ? companyProfiles
            .filter((profile) => {
                const symbol =
                    profile.normalizedSymbol ??
                    normalizeSymbolForSearch(
                        profile.symbol,
                    );

                const companyName =
                    profile.normalizedName ??
                    normalizeSymbolForSearch(
                        profile.companyName ?? "",
                    );

                return (
                    symbol.includes(searchText) ||
                    companyName.includes(searchText)
                );
            })
            .sort((a, b) => {
                const aSymbol =
                    a.normalizedSymbol ??
                    normalizeSymbolForSearch(
                        a.symbol,
                    );

                const bSymbol =
                    b.normalizedSymbol ??
                    normalizeSymbolForSearch(
                        b.symbol,
                    );

                const aStarts =
                    aSymbol.startsWith(searchText);

                const bStarts =
                    bSymbol.startsWith(searchText);

                if (aStarts !== bStarts) {
                    return aStarts ? -1 : 1;
                }

                return a.symbol.localeCompare(
                    b.symbol,
                    "fa",
                );
            })
            .slice(0, 20)
        : [];

    const latestDailyPriceRunAt = companyProfiles.reduce<string | null>((latest, profile) =>
    
    {
        const current = profile.monthlyPeriodEndDate;

        if (!current) {
            return latest;
        }

        return !latest || current > latest
            ? current
            : latest;
    }, null);
    console.log("DailyPriceRunAt:",
        companyProfiles.find((x) => x.lastDailyPriceRunAt)?.lastDailyPriceRunAt,
         latestDailyPriceRunAt,);
    const activeSinceDate = new Date();
    activeSinceDate.setMonth(activeSinceDate.getMonth() - 12);

    const activeCompanySymbolSet = new Set(companyProfiles
        .filter((profile) => {
            if (!profile.monthlyPublishDateTime) {
                return false;
            }

            return (
                new Date(profile.monthlyPublishDateTime) >=
                activeSinceDate
            );
        })
        .map((profile) => profile.symbol),
);

    const latestTradeDate =
        companyProfiles.reduce<string | null>((latest, profile) => {
            if (!profile.tradeDate) {
                return latest;
            }

            if (!latest || profile.tradeDate > latest) {
                return profile.tradeDate;
            }

            return latest;
        }, null);

    const activeFromDate = (() => {
        if (!latestTradeDate) {
            return null;
        }

        const normalizedTradeDate =
            latestTradeDate.length >= 10
                ? latestTradeDate.slice(0, 10)
                : latestTradeDate;

        const [year, month, day] =
            normalizedTradeDate.split("-").map(Number);

        if (!year || !month || !day) {
            return null;
        }

        return `${year - 1}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    })();

    const activeCompanyCount = companyProfiles.filter(
    (profile) => {
        const hasRecentTrade =
            activeFromDate !== null &&
            !!profile.tradeDate &&
            profile.tradeDate >= activeFromDate;

        const hasRecentCodal =
            !!profile.monthlyPublishDateTime &&
            new Date(profile.monthlyPublishDateTime) >=
                activeSinceDate;

        return hasRecentTrade || hasRecentCodal;
    },
).length;

const inactiveCompanyCount = Math.max(
    companyProfiles.length - activeCompanyCount,
    0,
);

const openCompanyCount =
    latestTradeDate === null
        ? 0
        : companyProfiles.filter(
              (profile) =>
    dataQualityQuery.data?.historyCoverage.windowEndPeriod
        ? profile.monthlyPeriodEndDate?.startsWith(
              dataQualityQuery.data.historyCoverage.windowEndPeriod,
          ) === true
        : false,
          ).length;

const closedCompanyCount = Math.max(
    activeCompanyCount - openCompanyCount,
    0,
);
    //

    // Card 2 - Disclosure statistics
    const disclosureStats = disclosureStatsQuery.data;
    const latestDisclosureDate = disclosureStats?.latestDisclosure ?? null;    
    const disclosureToday = disclosureStats?.dailyCount ?? 0;
    const disclosureMonth = disclosureStats?.monthlyCount ?? 0;
    //const disclosureYear = disclosureStats?.yearlyCount ?? 0;
    const disclosureTotal = disclosureStats?.totalCount ?? 0;
 //   const currentPersianDay = Number(disclosureStats?.persianDate?.slice(8, 10) ?? 0);
    const disclosureDayMonthPercent = disclosureMonth > 0
        ? (disclosureToday / disclosureMonth) * 100
        : 0;
    const currentPersianMonth = Number(disclosureStats?.persianDate.slice(5, 7) ?? 0);
    const currentPersianDay = Number(disclosureStats?.persianDate.slice(8, 10) ?? 0);
    const daysInCurrentPersianMonth =  currentPersianMonth >= 1 &&
                                       currentPersianMonth <= 6
                                          ? 31
                                          : currentPersianMonth >= 7 &&
                                            currentPersianMonth <= 11
                                                ? 30
                                                : 29;

    const disclosureProgress = daysInCurrentPersianMonth > 0
        ? Math.min(
              (currentPersianDay /
                  daysInCurrentPersianMonth) *
                  100,
              100,
          )
        : 0; 
    //
    // Card 3 - calculations only
// Card 3 - Monthly reports

// Card 3 - Monthly reports

const monthlyReportProfiles = companyProfiles.filter(
    (profile) =>
        profile.isic != null &&
        profile.monthlyPeriodEndDate != null,
);

// شرکت‌هایی که اصولاً گزارش ماهانه دارند
const monthlyReportTotal =
    monthlyReportProfiles.length;
const expectedMonthlyPeriod =
    new DateObject({
        calendar: persian,
    })
        .subtract(1, "month")
        .format("YYYY/MM");
// شرکت‌هایی که گزارش آخرین ماه را دارند
const companiesWithMonthlyReports =
    expectedMonthlyPeriod === null
        ? 0
        : monthlyReportProfiles.filter(
              (profile) =>
                  profile.monthlyPeriodEndDate?.startsWith(
                      expectedMonthlyPeriod,
                  ) === true,
          ).length;

// شرکت‌هایی که گزارش دارند ولی آخرین ماه را هنوز ندارند
const companiesWithoutMonthlyReports =
    monthlyReportTotal -
    companiesWithMonthlyReports;

const monthlyReportCoverage =
    monthlyReportTotal > 0
        ? (companiesWithMonthlyReports /
              monthlyReportTotal) *
          100
        : 0;

const latestMonthlyReportDate =
    monthlyReportProfiles.reduce<string | null>(
        (latest, profile) => {
            const current =
                profile.monthlyPublishDateTime;

            if (!current) {
                return latest;
            }

            if (
                !latest ||
                new Date(current).getTime() >
                    new Date(latest).getTime()
            ) {
                return current;
            }

            return latest;
        },
        null,
    );
    //
    
    
    const backgroundJobStatusesQuery = useQuery<BackgroundJobStatus[]>({
        queryKey: [
            "market-intelligence",
            "health-center",
            "background-jobs",
        ],
        queryFn: getBackgroundJobStatuses,
        enabled: false,
    });
    
      
    
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
    
    const rule0Issues: DataQualityIssue[] =
    dataQualityQuery.data?.historyCoverage.gaps
        .filter((gap) =>
            activeCompanySymbolSet.has(gap.symbol),
        )
        .flatMap((gap) =>
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


    return (
        <div className="-mt-6">
            <PageHero   className="-mt-5 [&>div]:!py-3 sm:[&>div]:!py-3"
                title={tMarket("healthCenter.title")}
                subtitle={tMarket("healthCenter.subtitle")}
                actions={

                    
                    <div className="-mt-5 flex w-[600px] flex-col items-start gap-2 self-start">

                        <CodalOperationsPanel />
                        <div className="mt-2 grid w-full grid-cols-4 gap-1">
                            <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                onClick={() => {
                                    setIsJobStatusOpen((current) => {
                                        const next = !current;

                                        if (next) {
                                            void backgroundJobStatusesQuery.refetch();
                                        }

                                        return next;
                                    });
                                }}
                                disabled={backgroundJobStatusesQuery.isFetching}
                            >
                                <Clock3 className="h-4 w-4" />
                                {isJobStatusOpen
                                    ? "Close Job Status"
                                    : backgroundJobStatusesQuery.isFetching
                                        ? "Recieving Jobs ..."
                                        : tMarket("jobStatus")}
                            </Button>
                            <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                onClick={() => setIsScheduleOpen((current) => !current)}
                            >    
                            <CalendarClock className="h-4 w-4" />
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
                            Refresh
                        </Button>
                        </div>
                    </div>
                }
            />
            {isJobStatusOpen &&
                backgroundJobStatusesQuery.data &&
                backgroundJobStatusesQuery.data.length > 0 && (
                <div className="mt-4 overflow-x-auto rounded-lg border">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b">
                                <th className="px-3 py-2 text-right">جاب</th>
                                <th className="px-3 py-2 text-right">وضعیت</th>
                                <th className="px-3 py-2 text-right">شروع</th>
                                <th className="px-3 py-2 text-right">پایان</th>
                                <th className="px-3 py-2 text-right">مدت</th>
                            </tr>
                        </thead>

                        <tbody>
                            {backgroundJobStatusesQuery.data.map((job) => (
                                <tr
                                    key={job.jobCode}
                                    className="border-b last:border-b-0"
                                >
                                    <td className="px-3 py-2">
                                        {job.jobName}
                                    </td>

                                    <td className="px-3 py-2">
                                        {job.lastStatus}
                                    </td>

                                    <td className="px-3 py-2">
                                        {job.lastStartAt
                                            ? new Date(job.lastStartAt).toLocaleString()
                                            : "-"}
                                    </td>

                                    <td className="px-3 py-2">
                                        {job.lastEndAt
                                            ? new Date(job.lastEndAt).toLocaleString()
                                            : "-"}
                                    </td>

                                    <td className="px-3 py-2">
                                        {job.lastDurationMs !== null
                                            ? `${Math.round(job.lastDurationMs / 1000)} s`
                                            : "-"}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            <section className="mt-2 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <div className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-1 shadow-xs">
    {/* کل شرکت‌ها */}
    <div className="-mt-1 flex items-center justify-between gap-3 px-2 py-1">
        <div className="flex items-center gap-2 text-sm font-semibold text-[var(--color-muted-foreground)]">
            <Activity className="h-4 w-4" />
            <span>کل شرکت‌ها</span>
        </div>

        <div className="text-2xl font-semibold tracking-tight tabular-nums">
            {companyProfiles.length > 0
                ? companyProfiles.length.toLocaleString()
                : "—"}
        </div>
    </div>

    {/* فعال / غیرفعال */}
    <div className="-mt-2 grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-emerald-500/10 px-3 py-1.5 h-7">
            <div className="flex items-center justify-between">
                <span className="font-black text-xs leading-none text-[var(--color-muted-foreground)]">
                     فعال                 
                </span>

                <span className="font-semibold tabular-nums text-emerald-600">
                    {activeCompanyCount.toLocaleString()}
                </span>
            </div>
        </div>

        <div className="rounded-lg bg-slate-500/10 px-3 py-1.5 h-7">
            <div className="flex items-center justify-between">
                <span className="font-black text-xs text-[var(--color-muted-foreground)]">
                    غیرفعال
                </span>

                <span className="font-semibold tabular-nums text-slate-600">
                    {inactiveCompanyCount.toLocaleString()}
                </span>
            </div>
        </div>
    </div>

    {/* باز / بسته */}
    <div className="mt-1 grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-emerald-500/10 px-3 py-1.5 h-7">
            <div className="flex items-center justify-between">
                <span className="font-black text-xs text-[var(--color-muted-foreground)]">
                    باز
                </span>

                <span className="font-semibold tabular-nums text-emerald-600">
                    {openCompanyCount.toLocaleString()}
                </span>
            </div>
        </div>

        <div className="rounded-lg bg-amber-500/10 px-3 py-1.5 h-7">
            <div className="flex items-center justify-between">
                <span className="font-black text-xs text-[var(--color-muted-foreground)]">
                    بسته
                </span>

                <span className="font-semibold tabular-nums text-amber-600">
                    {closedCompanyCount.toLocaleString()}
                </span>
            </div>
        </div>
    </div>
    <div className="mt-2 flex items-center justify-between font-semibold text-xs text-[var(--color-muted-foreground)]">
                            <span>آخرین‌ تاریخ بازار</span>

                            <span dir="ltr" className="font-mono tabular-nums">
    {latestDailyPriceRunAt
    ? `${new DateObject({
          date: `${latestDailyPriceRunAt.slice(0, 4)}/${latestDailyPriceRunAt.slice(4, 6)}/${latestDailyPriceRunAt.slice(6, 8)}:00`,
          format: "YYYY/MM/DD",
      })
          .convert(persian)
          .format("YYYY/MM/DD")} ${latestDailyPriceRunAt.slice(9)}:00`
    : "—"}
</span>
                        </div>

</div>
                <div className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-1 shadow-xs">
                    <div className="flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2">
                            <div className="flex items-center gap-2 text-sm font-semibold text-[var(--color-muted-foreground)]">
                                <Activity className="h-4 w-4" />
                                <span>اطلاعیه‌های‌کدال</span>
                            </div>

                            <div className="text-2xl font-semibold tracking-tight tabular-nums">
                                {disclosureTotal > 0
                                    ? disclosureTotal.toLocaleString()
                                    : "—"}
                            </div>
                        </div>

                        <div className="shrink-0 rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-600">
                            {disclosureDayMonthPercent.toFixed(1)}٪ روز/ماه
                        </div>
                    </div>

                    <div className="-mt-1 grid grid-cols-2 gap-2">
                        <div className="rounded-lg bg-emerald-500/10 px-3 py-2">
                            <div className="flex items-center justify-between">
                                <div className="font-black text-xs text-[var(--color-muted-foreground)]">
                                    روز
                                </div>

                                <div className="font-semibold tabular-nums text-emerald-600">
                                    {disclosureToday.toLocaleString()}
                                </div>
                            </div>
                        </div>

                        <div className="rounded-lg bg-amber-500/10 px-3 py-2">
                            <div className="flex items-center justify-between">
                                <span className="font-black text-xs text-[var(--color-muted-foreground)]">
                                    ماه
                                </span>

                                <span className="font-semibold tabular-nums text-amber-600">
                                    {disclosureMonth.toLocaleString()}
                                </span>
                            </div>
                        </div>
                    </div>

                    <div className="mt-3">
                        <div className="h-1.5 overflow-hidden rounded-full bg-[var(--color-muted)]">
                            <div
                                className="h-full rounded-full bg-emerald-500 transition-all duration-500"
                                style={{
                                    width: `${Math.min(disclosureProgress, 100)}%`,
                                }}
                            />
                        </div>

                        <div className="mt-2 flex items-center justify-between font-semibold text-xs text-[var(--color-muted-foreground)]">
                            <span>آخرین‌اطلاعیه‌کدال</span>

                            <span dir="ltr" className="font-mono tabular-nums">
                               {latestDisclosureDate ?? "—"}
                            </span>
                        </div>
                    </div>
                </div>
                <div className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-1 shadow-xs">
    <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
            <div className="flex items-center gap-2 text-sm font-semibold text-[var(--color-muted-foreground)]">
                <Activity className="h-4 w-4" />
                <span>گزارش‌های ماهانه</span>
            </div>

            <div className="text-2xl font-semibold tracking-tight tabular-nums">
                {companyProfiles.length > 0
                    ? monthlyReportTotal.toLocaleString()
                    : "—"}
            </div>
        </div>

        <div className="shrink-0 rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-600">
            {monthlyReportCoverage.toFixed(1)}٪ پوشش
        </div>
    </div>

    <div className="-mt-1 grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-emerald-500/10 px-3 py-2">
            <div className="flex items-center justify-between">
                <div className="font-black text-xs text-[var(--color-muted-foreground)]">
                      گزارش شده
                </div>

                <div className="font-semibold tabular-nums text-emerald-600">
                    {companiesWithMonthlyReports.toLocaleString()}
                </div>
            </div>
        </div>

        <div className="rounded-lg bg-amber-500/10 px-3 py-2">
            <div className="flex items-center justify-between">
                <span className="font-black text-xs text-[var(--color-muted-foreground)]">
                     گزارش نشده
                </span>

                <span className="font-semibold tabular-nums text-amber-600">
                    {companiesWithoutMonthlyReports.toLocaleString()}
                </span>
            </div>
        </div>
    </div>

    <div className="mt-3">
        <div className="h-1.5 overflow-hidden rounded-full bg-[var(--color-muted)]">
            <div
                className="h-full rounded-full bg-emerald-500 transition-all duration-500"
                style={{
                    width: `${Math.min(monthlyReportCoverage, 100)}%`,
                }}
            />
        </div>

        <div className="mt-2 flex items-center justify-between font-semibold text-xs text-[var(--color-muted-foreground)]">
            <span>آخرین گزارش ماهانه</span>

            <span dir="ltr" className="font-mono tabular-nums">
               {latestMonthlyReportDate
    ? `${new DateObject({
          date: latestMonthlyReportDate.slice(0, 10),
          format: "YYYY-MM-DD",
      })
          .convert(persian)
          .format("YYYY/MM/DD")} ${latestMonthlyReportDate.slice(11, 20)}`
    : "—"}
            </span>
        </div>
    </div>
</div>
                <div className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-1 shadow-xs">
                    <div className="flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2">
                            <div className="flex items-center gap-2 text-sm font-semibold text-[var(--color-muted-foreground)]">
                                <Activity className="h-4 w-4" />
                                <span>شرکت‌های فعال</span>
                            </div>

                            <div className="text-2xl font-semibold tracking-tight tabular-nums">
                                {companyProfiles.length > 0
                                    ? activeCompanyCount.toLocaleString()
                                    : "—"}
                            </div>
                        </div>

                        <div className="shrink-0 rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-600">
                            ٪ باز
                        </div>
                    </div>

                    <div className="-mt-1 grid grid-cols-2 gap-2">
                        <div className="rounded-lg bg-emerald-500/10 px-3 py-2">
                            <div className="flex items-center justify-between">
                                <div className="font-black text-xs text-[var(--color-muted-foreground)]">
                                    باز
                                </div>

                                <div className="font-semibold tabular-nums text-emerald-600">
                                    {openCompanyCount.toLocaleString()}
                                </div>
                            </div>
                        </div>

                        <div className="rounded-lg bg-amber-500/10 px-3 py-2">
                            <div className="flex items-center justify-between">
                                <span className="font-black text-xs text-[var(--color-muted-foreground)]">
                                    بسته
                                </span>

                                <span className="font-semibold tabular-nums text-amber-600">
                                    {closedCompanyCount.toLocaleString()}
                                </span>
                            </div>
                        </div>
                    </div>

                    <div className="mt-3">
                        <div className="h-1.5 overflow-hidden rounded-full bg-[var(--color-muted)]">
                            <div
                                
                            />
                        </div>

                        <div className="mt-2 flex items-center justify-between font-semibold text-xs text-[var(--color-muted-foreground)]">
                            <span>آخرین‌اطلاعیه‌کدال</span>

                            <span className="font-mono tabular-nums">
                                {latestDisclosureDate
                                    ? new DateObject({
                                        date: latestDisclosureDate.slice(0, 10),
                                        format: "YYYY-MM-DD",
                                    })
                                        .convert(persian)
                                        .format("YYYY/MM/DD")
                                    : "—"}
                            </span>
                        </div>
                    </div>
                </div>


                
            </section>
            <section className="mt-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-start gap-3">
                        <ToneIconTile
                            icon={ChartNoAxesCombined}
                            tone="muted"
                            size="md"
                        />

                        <div>
                            <h2 className="text-sm font-semibold text-[var(--color-foreground)]">
                                {tMarket(
                                    "healthCenter.analyticsMaintenance.title",
                                )}
                            </h2>

                            <p className="mt-1 max-w-2xl text-xs leading-5 text-[var(--color-muted-foreground)]">
                                {tMarket(
                                    "healthCenter.analyticsMaintenance.description",
                                )}
                            </p>
                        </div>
                    </div>

                    <div className="flex flex-col items-start gap-2 sm:items-end">
                        <Button
                            type="button"
                            variant="outline"
                            className="gap-2"
                            disabled={salesInsightRebuildMutation.isPending}
                            onClick={() =>
                                salesInsightRebuildMutation.mutate()
                            }
                        >
                            <RefreshCw
                                className={cn(
                                    "size-3.5",
                                    salesInsightRebuildMutation.isPending &&
                                    "animate-spin",
                                )}
                            />

                            {salesInsightRebuildMutation.isPending
                                ? tMarket(
                                    "healthCenter.analyticsMaintenance.queuing",
                                )
                                : tMarket(
                                    "healthCenter.analyticsMaintenance.rebuildMissing",
                                )}
                        </Button>

                        {salesInsightRebuildMutation.isSuccess && (
                            <p className="text-xs text-[var(--color-success)]">
                                {tMarket(
                                    "healthCenter.analyticsMaintenance.queued",
                                    {
                                        jobId:
                                            salesInsightRebuildMutation.data
                                                .jobId,
                                    },
                                )}
                            </p>
                        )}

                        {salesInsightRebuildMutation.isError && (
                            <p className="text-xs text-[var(--color-destructive)]">
                                {tMarket(
                                    "healthCenter.analyticsMaintenance.queueError",
                                )}
                            </p>
                        )}
                        {salesPerformanceRebuildStatusQuery.data && (
                            <div className="w-full min-w-0 sm:w-80">
                                <div className="mb-1.5 flex items-center justify-between text-xs">
                                    <span className="text-[var(--color-muted-foreground)]">
                                        {salesPerformanceRebuildStatusQuery.data.completed.toLocaleString()}
                                        {" / "}
                                        {salesPerformanceRebuildStatusQuery.data.total.toLocaleString()}
                                    </span>

                                    <span className="font-mono tabular-nums">
                                        {salesPerformanceRebuildStatusQuery.data.progressPercent.toFixed(2)}%
                                    </span>
                                </div>

                                <div className="h-2 overflow-hidden rounded-full bg-[var(--color-muted)]">
                                    <div
                                        className="h-full rounded-full bg-[var(--color-foreground)] transition-all duration-500"
                                        style={{
                                            width: `${Math.min(
                                                salesPerformanceRebuildStatusQuery.data.progressPercent,
                                                100,
                                            )}%`,
                                        }}
                                    />
                                </div>

                                <p className="mt-1.5 text-xs text-[var(--color-muted-foreground)]">
                                    {tMarket(
                                        "healthCenter.analyticsMaintenance.remaining",
                                        {
                                            count:
                                                salesPerformanceRebuildStatusQuery.data
                                                    .remaining
                                                    .toLocaleString(),
                                        },
                                    )}
                                </p>
                            </div>
                        )}
                    </div>
                </div>
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
                                                : issue.issueCode === "SalesHistoryGap"
                                                    ? `گپ سابقه فروش ماهانه از ${issue.periodEndDate ?? "—"} تا ${issue.publishDate ?? "—"}`
                                                    : `شرح تعریف نشده برای نوع اشکال: ${issue.issueCode}`;
                                    return (
                                        <div
                                            key={`${issue.symbol}-${issue.yearEndDate}-${issue.periodEndDate}-${issue.issueCode}-${index}`}
                                            onClick={() => {
                                                setBackfillSymbol(issue.symbol);
                                                setBackfillInstrumentId(issue.asset?.tsetmcInstrumentId ?? null);
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
                                                void handleSalesClick(issue.symbol)
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
                        <div className="relative">
                            <input
                                type="text"
                                value={backfillSymbol}
                                onFocus={() =>
                                    setSymbolSuggestionsOpen(true)
                                }
                                onChange={(event) => {
                                    setBackfillSymbol(event.target.value);
                                    setBackfillInstrumentId(null);
                                    setSymbolSuggestionsOpen(true);
                                }}
                                placeholder={tMarket(
                                    "healthCenter.backfill.symbolPlaceholder",
                                )}
                                className="h-8 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 text-sm outline-none transition focus:border-[var(--color-ring)]"
                            />
                            {symbolSuggestionsOpen &&
                             searchText.length >= 2 &&
                             matchingCompanyProfiles.length > 0 && (
                            <div className="absolute bottom-full z-50 mb-1 max-h-72 w-full overflow-y-auto rounded-lg border border-blue-900 bg-blue-100 text-red-950 shadow-xl">
                                {matchingCompanyProfiles.map(
                                     (profile) => (
                                       <button
                                          key={profile.instrumentId}
                                          type="button"
                                          className="flex w-full items-center justify-between gap-3 border-b px-3 py-2 text-start text-sm hover:bg-muted"
                                          onMouseDown={(event) =>
                                          event.preventDefault()                               }
                                          onClick={() => {
                                             setBackfillSymbol(profile.symbol, );
                                             setBackfillInstrumentId(profile.instrumentId,);
                                             setSymbolSuggestionsOpen(false,);
                                }}
                                       >
                                <span className="font-semibold">
                                    {profile.symbol}
                                </span>

                                <span className="truncate text-xs">
                                    {profile.companyName ?? ""}
                                </span>
                    </button>
                ),
            )}
        </div>
    )}
                            
                        </div>
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
                    <div className="flex items-center gap-2 whitespace-nowrap">
                        <Button
                            type="button"
                            onClick={handleRunBackfill}
                            disabled={
                                !backfillSymbol.trim() ||
                                backfillFromDate === null ||
                                backfillToDate === null ||
                                backfillMutation.isPending
                            }
                            className="whitespace-nowrap"
                        >
                            {backfillMutation.isPending
                                ? tMarket("healthCenter.backfill.send")
                                : tMarket("healthCenter.backfill.runcodal")}
                        </Button>

                        <Button
                            type="button"
                            onClick={handleRunTsetmcBackfill}
                            disabled={
                                !backfillSymbol.trim() ||
                                backfillInstrumentId === null ||
                                tsetmcBackfillMutation.isPending
                            }
                            className="whitespace-nowrap"
                        >
                            {tsetmcBackfillMutation.isPending
                                ? tMarket("healthCenter.backfill.receive")
                                : tMarket("healthCenter.backfill.runtsetmc")}
                        </Button>
                    </div>
                                        
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


