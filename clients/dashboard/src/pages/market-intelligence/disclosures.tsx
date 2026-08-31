import { useTranslation } from "react-i18next";
import {
    useEffect,
    useState,
} from "react";
import {
    keepPreviousData,
    useQuery,
} from "@tanstack/react-query";
import {
    AlertTriangle,
    BriefcaseBusiness,
    ChartNoAxesCombined,
    ExternalLink,
    FileText,
    Newspaper,
    RefreshCw,
    Search,
} from "lucide-react";
import {
    getFiscalYearSales,
    searchDisclosures,
    getPortfolioByDisclosureId,
    type PortfolioReport,
    type DisclosureDto,
    type DisclosureParseStatus,
    type DisclosureSortBy,
    type FiscalYearSales,
} from "@/api/market-intelligence";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PortfolioReportDialog } from "@/components/market-intelligence/portfolio-report-dialog";
import { Combobox, EntityPageHeader, EntityPager,} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";
import { CodalOperationsPanel } from "./codal-operations-panel";
import { codalReportTypeOptions, } from "@/lib/market-intelligence/codal-report-types";
import { codalLetterCategoryOptions, } from "@/lib/market-intelligence/codal-letter-categories";
import { FiscalYearSalesDialog } from
    "@/components/market-intelligence/fiscal-year-sales-dialog";
const PAGE_SIZE = 25;

const statusOptions: Array<{
    value: DisclosureParseStatus;
    labelKey: string;
}> = [
        { value: "Pending", labelKey: "status.pending", },
        { value: "Success", labelKey: "status.success", },
        { value: "Failed", labelKey: "status.failed", },
        { value: "NoData", labelKey: "status.noData", },
        { value: "Skipped", labelKey: "status.skipped", },
    ];

const sortOptions: Array<{ value: DisclosureSortBy; labelKey: string; }> =
    [
        { value: "publishDateTime", labelKey: "sort.publishDate", },
        { value: "tracingNo", labelKey: "sort.tracingNo", },
        { value: "symbol", labelKey: "sort.symbol", },
        { value: "companyName", labelKey: "sort.company", },
        { value: "salesParsedAt", labelKey: "sort.parseDate", },
    ];

export function DisclosuresPage() {
    const { t } = useTranslation("disclosures");
    const [search, setSearch] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const [page, setPage] = useState(1);
    const [rtFilter, setRtFilter] = useState("");
    const [letFilter, setLetFilter] = useState("");
    const [statusFilter, setStatusFilter] = useState<DisclosureParseStatus | null>(null);
    const [sortBy, setSortBy] = useState<DisclosureSortBy>("publishDateTime");
    const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");
    const [fiscalYearSales, setFiscalYearSales] = useState<FiscalYearSales | null>(null);
    const [isSalesDialogOpen, setIsSalesDialogOpen] = useState(false);
    const [fiscalYearSalesTitle, setFiscalYearSalesTitle] = useState("");
    
    const handleSalesClick = async (
        symbol: string,
        title: string,
    ) => {
        const result = await getFiscalYearSales(
            symbol,
            title,
        );

        setFiscalYearSales(result);
        setFiscalYearSalesTitle(title);
        setIsSalesDialogOpen(true);
    };
    const handlePortfolioClick = async (
        disclosureId: string,
    ) => {
        const result =
            await getPortfolioByDisclosureId(
                disclosureId,
            );

        if (!result) {
            alert("برای این اعلامیه پرتفوی پیدا نشد.");
            return;
        }

        setPortfolioReport(result);
        setIsPortfolioDialogOpen(true);
    };
    const handlePortfolioNavigation = async (
        disclosureId: string | null,
    ) => {
        if (!disclosureId) {
            return;
        }

        const result =
            await getPortfolioByDisclosureId(
                disclosureId,
            );

        if (result) {
            setPortfolioReport(result);
        }
    };
    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(search.trim());
            setPage(1);
        }, 250);

        return () => clearTimeout(timer);
    }, [search]);

    useEffect(() => {
        setPage(1);
    }, [
        rtFilter,
        letFilter,
        statusFilter,
        sortBy,
        sortDir,
    ]);
            
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
    const query = useQuery({
        queryKey: [
            "market-intelligence",
            "disclosures",
            {
                search: debouncedSearch,
                rt: rtFilter,
                let: letFilter,
                status: statusFilter,
                sortBy,
                sortDir,
                page,
            },
        ],
        queryFn: () =>
            searchDisclosures({
                search: debouncedSearch || undefined,
                rt: toOptionalNumber(rtFilter),
                lets:
                    selectedLetCodes.length > 0
                        ? selectedLetCodes
                        : undefined,
                includeNullLet,
                salesParseStatus: statusFilter,
                pageNumber: page,
                pageSize: PAGE_SIZE,
                sortBy,
                sortDir,
            }),
        placeholderData: keepPreviousData,
    });
    const data = query.data;
    const items = data?.items ?? [];

    const totalCount = data?.totalCount ?? 0;

    const totalPages = Math.max(
        1,
        Math.ceil(totalCount / PAGE_SIZE),
    );
    const filtersApplied =
        rtFilter.trim() !== "" ||
        letFilter.trim() !== "" ||
        statusFilter !== null;

    const searchActive =
        debouncedSearch.length > 0 ||
        filtersApplied;

    const clearFilters = () => {
        setSearch("");
        setRtFilter("");
        setLetFilter("");
        setStatusFilter(null);
        setPage(1);
    };
    const handleSalesNavigation = async (
        yearEndDate: string | null,
    ) => {
        if (!yearEndDate || !fiscalYearSales) {
            return;
        }

        const result = await getFiscalYearSales(
            fiscalYearSales.symbol,
            fiscalYearSalesTitle,
            yearEndDate,
        );

        setFiscalYearSales(result);
    };
    const [portfolioReport, setPortfolioReport] =
        useState<PortfolioReport | null>(null);

    const [isPortfolioDialogOpen, setIsPortfolioDialogOpen] =
        useState(false);
    return (
        <div className="space-y-4 sm:space-y-6">
            <EntityPageHeader
                icon={Newspaper}
                title={t("page.title")}
                total={data?.totalCount ?? null}
                description={t("page.description")}
                unit={t("page.unit")}>

                <Button
                    type="button"
                    variant="outline"
                    disabled={query.isFetching}
                    onClick={() => void query.refetch()}
                    className="h-9 gap-1.5 rounded-lg px-4 text-[13px]">

                    <RefreshCw
                        className={cn(
                            "size-4",
                            query.isFetching && "animate-spin",)} />

                    {t("actions.refresh")}

                </Button>
            </EntityPageHeader>
            <CodalOperationsPanel
                onCompleted={query.refetch}
            />
            <SearchBox
                value={search}
                onChange={setSearch}
            />

            <FilterRow
                rt={rtFilter}
                onRtChange={setRtFilter}
                letValue={letFilter}
                onLetChange={setLetFilter}
                status={statusFilter}
                onStatusChange={setStatusFilter}
                sortBy={sortBy}
                onSortByChange={setSortBy}
                sortDir={sortDir}
                onSortDirChange={setSortDir}
            />

            {query.isLoading && items.length === 0 ? (
                <LoadingList />
            ) : items.length === 0 ? (
                <EmptyResults
                    searchActive={searchActive}
                    search={debouncedSearch}
                    onClear={clearFilters}
                />
            ) : (
                   <DisclosureResults
                       items={items}
                       totalCount={data?.totalCount ?? 0}
                       onSalesClick={handleSalesClick}
                       onPortfolioClick={handlePortfolioClick}
                   />

            )}

            {items.length > 0 && (
                <EntityPager
                    page={page}
                    totalPages={totalPages}
                    hasPrev={page > 1}
                    hasNext={page < totalPages}
                    onPrev={() =>
                        setPage((current) =>
                            Math.max(1, current - 1),
                        )
                    }
                    onNext={() =>
                        setPage((current) =>
                            Math.min(totalPages, current + 1),
                        )
                    }
                />
            )}
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
            <PortfolioReportDialog
                open={isPortfolioDialogOpen}
                report={portfolioReport}
                onClose={() =>
                    setIsPortfolioDialogOpen(false)
                }
                onPrevious={() =>
                    void handlePortfolioNavigation(
                        portfolioReport?.previousDisclosureId ?? null,
                    )
                }
                onNext={() =>
                    void handlePortfolioNavigation(
                        portfolioReport?.nextDisclosureId ?? null,
                    )
                }
            />
            {query.isError && (
                <div
                    role="alert"
                    className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
                >
                    <AlertTriangle className="mt-0.5 size-4 shrink-0" />
                    <span>{describe(query.error)}</span>
                </div>
            )}
        </div>
    );
}

function SearchBox({
    value,
    onChange,
}: {

    value: string;
    onChange: (value: string) => void;
}) {
    const { t } = useTranslation("disclosures");

    return (
        <div className="relative">
            <Search className="absolute left-4 top-1/2 size-[18px] -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />

            <input
                type="text"
                value={value}
                onChange={(event) =>
                    onChange(event.target.value)
                }
                placeholder={t("search.placeholder")}

                className={cn(
                    "h-[46px] w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
                    "pl-12 pr-16 text-[14px] text-[var(--color-foreground)] outline-none shadow-xs",
                    "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]",
                    "transition-all duration-200",
                    "focus:border-[oklch(from_var(--color-ring)_l_c_h_/_0.30)] focus:ring-2 focus:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.10)]",
                )}
            />

            {value && (
                <button
                    type="button"
                    onClick={() => onChange("")}
                    className="absolute right-4 top-1/2 -translate-y-1/2 cursor-pointer text-[11px] font-medium text-[var(--color-muted-foreground)]"
                >
                    Clear
                </button>
            )}
        </div>
    );
}

function FilterRow({
    rt,
    onRtChange,
    letValue,
    onLetChange,
    status,
    onStatusChange,
    sortBy,
    onSortByChange,
    sortDir,
    onSortDirChange,
}: {
    rt: string;
    onRtChange: (value: string) => void;
    letValue: string;
    onLetChange: (value: string) => void;
    status: DisclosureParseStatus | null;
    onStatusChange: (
        value: DisclosureParseStatus | null,
    ) => void;
    sortBy: DisclosureSortBy;
    onSortByChange: (
        value: DisclosureSortBy,
    ) => void;
    sortDir: "asc" | "desc";
    onSortDirChange: (
        value: "asc" | "desc",
    ) => void;
}) {
    const { t } = useTranslation("disclosures");
    const { t: tMarket } = useTranslation("marketIntelligence");
    return (
        <div className="flex flex-wrap items-center gap-2">

            <Combobox
                label={tMarket("filters.reportType")}
                value={rt || null}
                onChange={(value) =>
                    onRtChange(value ?? "")
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
                value={letValue || null}
                onChange={(value) =>
                    onLetChange(value ?? "")
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
                value={status}
                onChange={(value) =>
                    onStatusChange(
                        value as DisclosureParseStatus | null,
                    )
                }
                options={statusOptions.map((option) => ({
                    value: option.value,
                    label: tMarket(option.labelKey),
                }))}
                variant="filter"
                clearable
            />

            <Combobox
                label={t("filters.sortBy")}
                value={sortBy}
                onChange={(value) => {
                    if (value) {
                        onSortByChange(
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
                {(["desc", "asc"] as const).map(
                    (direction) => (
                        <button
                            key={direction}
                            type="button"
                            onClick={() =>
                                onSortDirChange(direction)
                            }
                            aria-pressed={sortDir === direction}
                            className={cn(
                                "h-7 cursor-pointer rounded-full px-3 text-[11px] font-semibold uppercase tracking-wider transition-colors",
                                sortDir === direction
                                    ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                                    : "text-[var(--color-muted-foreground)]",
                            )}
                        >
                            {t(`filters.${direction}`)}
                        </button>
                    ),
                )}
            </div>
        </div>
    );
}

function DisclosureResults({
    items,
    totalCount,
    onSalesClick,
    onPortfolioClick, }:
    {
    items: DisclosureDto[];
    totalCount: number;
    onSalesClick: (
        symbol: string,
        title: string,
    ) => Promise<void>;
    onPortfolioClick: (
        disclosureId: string,
    ) => Promise<void>;
    })
{
    const { t, i18n } = useTranslation("disclosures");
    const numberLocale =
        i18n.resolvedLanguage
            ?.toLowerCase()
            .startsWith("fa")
            ? "fa-IR"
            : "en-US";

    const formattedTotalCount =
        new Intl.NumberFormat(numberLocale)
            .format(totalCount);
    return (
        <div>
            <p className="mb-3 text-[12px] font-medium text-[var(--color-muted-foreground)]">
                {t("results.found", { formattedCount: formattedTotalCount })}
            </p>

            <div className="space-y-2 md:hidden">
                {items.map((disclosure) => (
                    <MobileCard
                        key={disclosure.id}
                        disclosure={disclosure}
                    />
                ))}
            </div>

            <div className="hidden overflow-x-auto rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs md:block">
                <div className="min-w-[1050px]">
                    <div className="grid grid-cols-[150px_minmax(300px,1fr)_170px_110px_120px_112px] gap-3 border-b border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.4)] px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                        <span> {t("table.symbol")} / {t("table.company")} </span>
                        <span> {t("table.disclosure")}</span>
                        <span>{t("table.published")}</span>
                        <span>Type</span>
                        <span>{t("table.parseStatus")}</span>
                        <span className="text-center">عملیات</span>

                    </div>

                    {items.map((disclosure, index) => (
                        <DesktopRow
                            key={disclosure.id}
                            disclosure={disclosure}
                            isLast={index === items.length - 1}
                            onSalesClick={onSalesClick}
                            onPortfolioClick={onPortfolioClick}
                        />
                    ))}
                </div>
            </div>
        </div>
    );
}

function DesktopRow({
    disclosure,
    isLast,
    onSalesClick,
    onPortfolioClick,
}: {
    disclosure: DisclosureDto;
    isLast: boolean;
    onSalesClick: (
        symbol: string,
        title: string,
    ) => Promise<void>;
    onPortfolioClick: (
        disclosureId: string,
    ) => Promise<void>;
}) 
{
    const codalUrl = toCodalUrl(disclosure.url);

    return (
        <div
            className={cn(
                "group grid items-center gap-3 px-5 py-3 transition-colors",
                "hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
                !isLast &&
                "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)]",
            )}
            style={{
                gridTemplateColumns:
                    "150px minmax(300px, 1fr) 170px 110px 120px 112px",
            }}
        >
            <div className="min-w-0">
                <div
                    dir="auto"
                    className="truncate text-[14px] font-semibold text-[var(--color-primary)]"
                >
                    {disclosure.symbol}
                </div>
                <div
                    dir="auto"
                    className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]"
                    title={disclosure.companyName}
                >
                    {disclosure.companyName}
                </div>
            </div>

            <div className="min-w-0">
                <div
                    dir="auto"
                    title={disclosure.title}
                    className="truncate text-[13px] font-medium text-[var(--color-foreground)]"
                >
                    {disclosure.title}
                </div>
                <div className="mt-1 flex flex-wrap items-center gap-1.5">
                    <code className="font-mono text-[10px] text-[var(--color-muted-foreground)]">
                        #{disclosure.tracingNo}
                    </code>
                    <FormatBadges disclosure={disclosure} />
                </div>
            </div>

            <div>
                <div
                    dir="ltr"
                    className="text-[12px] tabular-nums text-[var(--color-foreground)]"
                >
                    {disclosure.publishDateTimeRaw ?? "—"}
                </div>
                <div className="mt-0.5 text-[10px] text-[var(--color-muted-foreground)]">
                    {disclosure.letterCode}
                </div>
            </div>

            <div className="space-y-0.5 font-mono text-[11px] text-[var(--color-muted-foreground)]">
                <div>RT: {disclosure.rt ?? "—"}</div>
                <div>LET: {disclosure.let ?? "—"}</div>
            </div>

            <StatusChip
                status={disclosure.salesParseStatus}
            />
            <div className="flex items-center justify-end gap-1">
                <div className="grid size-8 place-items-center">
                    {hasFiscalDate(disclosure.title) && (
                        <button
                            type="button"
                            onClick={() =>
                                void onSalesClick(
                                    disclosure.symbol,
                                    disclosure.title,
                                )
                            }
                            title="مشاهده فروش"
                            className="grid size-8 place-items-center rounded-md text-[var(--color-success)] transition-colors hover:bg-[var(--color-muted)]"
                        >
                            <ChartNoAxesCombined className="size-[22px]" />
                        </button>
                    )}
                </div>
                <div className="grid size-8 place-items-center">
                    {disclosure.rt === 2 && (
                        <button
                            type="button"
                            onClick={() =>
                                void onPortfolioClick(
                                    disclosure.id,
                                )
                            }
                            title="مشاهده پرتفوی"
                            className="grid size-8 place-items-center rounded-md text-[var(--color-primary)] transition-colors hover:bg-[var(--color-muted)]"
                        >
                            <BriefcaseBusiness className="size-[20px]" />
                        </button>
                    )}
                </div>
                <div className="grid size-8 place-items-center">
                    {codalUrl && (
                        <a
                            href={codalUrl}
                            target="_blank"
                            rel="noreferrer"
                            aria-label={`Open ${disclosure.symbol} on Codal`}
                            className="grid size-8 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-primary)]"
                        >
                            <ExternalLink className="size-4" />
                        </a>
                    )}
                </div>
            </div>
            
        </div>
    );
}

function MobileCard({
    disclosure,
}: {
    disclosure: DisclosureDto;
}) {
    const codalUrl = toCodalUrl(disclosure.url);

    return (
        <article className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
            <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                    <div
                        dir="auto"
                        className="text-[15px] font-semibold text-[var(--color-primary)]"
                    >
                        {disclosure.symbol}
                    </div>
                    <div
                        dir="auto"
                        className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]"
                    >
                        {disclosure.companyName}
                    </div>
                </div>

                <StatusChip
                    status={disclosure.salesParseStatus}
                />
            </div>

            <p
                dir="auto"
                className="mt-3 line-clamp-2 text-[13px] font-medium leading-6 text-[var(--color-foreground)]"
            >
                {disclosure.title}
            </p>

            <div className="mt-3 flex flex-wrap items-center gap-2 text-[11px] text-[var(--color-muted-foreground)]">
                <span dir="ltr" className="font-mono">
                    {disclosure.publishDateTimeRaw ?? "—"}
                </span>
                <span>RT {disclosure.rt ?? "—"}</span>
                <span>LET {disclosure.let ?? "—"}</span>
                <FormatBadges disclosure={disclosure} />
            </div>

            {codalUrl && (
                <a
                    href={codalUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="mt-3 inline-flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-primary)]"
                >
                    Open on Codal
                    <ExternalLink className="size-3.5" />
                </a>
            )}
        </article>
    );
}

function FormatBadges({
    disclosure,
}: {
    disclosure: DisclosureDto;
}) {
    const formats = [
        disclosure.hasHtml ? "HTML" : null,
        disclosure.hasExcel ? "Excel" : null,
        disclosure.hasPdf ? "PDF" : null,
        disclosure.hasAttachment
            ? "Attachment"
            : null,
    ].filter(
        (value): value is string => value !== null,
    );

    if (formats.length === 0) {
        return (
            <span className="text-[10px] text-[var(--color-muted-foreground)]">
                No file
            </span>
        );
    }

    return (
        <>
            {formats.map((format) => (
                <span
                    key={format}
                    className="inline-flex h-5 items-center rounded-full bg-[var(--color-secondary)] px-2 text-[10px] font-medium text-[var(--color-secondary-foreground)]"
                >
                    {format}
                </span>
            ))}
        </>
    );
}

function StatusChip({
    status,
}: {
    status: DisclosureParseStatus;
}) {
    const tones: Record<
        DisclosureParseStatus,
        string
    > = {
        Pending:
            "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        Success:
            "bg-[oklch(from_var(--color-success)_l_c_h_/_0.14)] text-[var(--color-success)]",
        Failed:
            "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.14)] text-[var(--color-destructive)]",
        NoData:
            "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.14)] text-[var(--color-warning)]",
        Skipped:
            "bg-[var(--color-secondary)] text-[var(--color-secondary-foreground)]",
    };

    return (
        <span
            className={cn(
                "inline-flex h-6 w-fit items-center rounded-full px-2.5 text-[10px] font-semibold uppercase tracking-wider",
                tones[status],
            )}
        >
            {status}
        </span>
    );
}

function EmptyResults({
    searchActive,
    search,
    onClear,
}: {
    searchActive: boolean;
    search: string;
    onClear: () => void;
}) {
    return (
        <div className="flex flex-col items-center justify-center py-20 text-center">
            <div className="mb-4 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
                {searchActive ? (
                    <Search className="size-6 text-[var(--color-muted-foreground)]" />
                ) : (
                    <FileText className="size-6 text-[var(--color-muted-foreground)]" />
                )}
            </div>

            <h3 className="mb-1.5 font-display text-[17px] font-semibold">
                {searchActive
                    ? "No disclosures found"
                    : "No disclosures available"}
            </h3>

            <p className="mb-6 max-w-[360px] text-[13px] text-[var(--color-muted-foreground)]">
                {search
                    ? `Nothing matches "${search}".`
                    : "No disclosures match the current filters."}
            </p>

            {searchActive && (
                <Button
                    type="button"
                    variant="outline"
                    onClick={onClear}
                >
                    Clear filters
                </Button>
            )}
        </div>
    );
}

function LoadingList() {
    return (
        <div className="space-y-2">
            {Array.from({ length: 8 }).map(
                (_, index) => (
                    <div
                        key={index}
                        className="flex items-center gap-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4"
                    >
                        <Skeleton className="h-10 w-24 rounded-lg" />
                        <div className="flex-1 space-y-2">
                            <Skeleton className="h-3.5 w-3/4" />
                            <Skeleton className="h-2.5 w-1/3" />
                        </div>
                        <Skeleton className="h-6 w-20 rounded-full" />
                    </div>
                ),
            )}
        </div>
    );
}

function hasFiscalDate(
    title: string | null | undefined,
): boolean {
    return /[0-9۰-۹]{4}\/[0-9۰-۹]{2}\/[0-9۰-۹]{2}/
        .test(title ?? "");
}
function toOptionalNumber(
    value: string,
): number | null {
    const trimmed = value.trim();

    if (!trimmed) {
        return null;
    }

    const parsed = Number(trimmed);

    return Number.isFinite(parsed)
        ? parsed
        : null;
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