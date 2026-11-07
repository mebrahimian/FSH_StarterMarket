import { useAuth } from "@/auth/use-auth";
import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { LockKeyhole } from "lucide-react";
import {
    getPortfolioValuationSnapshots,
    getPortfolioByDisclosureId,
} from "@/api/market-intelligence";

import type {
    PortfolioValuationItem,
    PortfolioValuationResponse,
    PortfolioReport,
} from "@/api/market-intelligence";

import { PortfolioReportDialog } from
    "@/components/market-intelligence/portfolio-report-dialog";

const PAGE_SIZE = 20;
const REFRESH_INTERVAL_MS = 60_000;

export default function PortfolioValuationSnapshotsPage() {
    const { t: tMarket, i18n } =
        useTranslation("marketIntelligence");
    const { user } = useAuth();
    const isAdmin = user?.isAdmin === true;
    const isPersian = (i18n.resolvedLanguage ?? i18n.language)
        .toLowerCase()
        .startsWith("fa");

    const locale = isPersian ? "fa-IR" : "en-US";
    const direction = isPersian ? "rtl" : "ltr";

    const [searchInput, setSearchInput] = useState("");
    const [search, setSearch] = useState("");

    const [pageNumber, setPageNumber] = useState(1);
    const [sortBy, setSortBy] = useState("DISCOUNT");
    const [sortDir, setSortDir] =
        useState<"asc" | "desc">("desc");

    const [refreshVersion, setRefreshVersion] = useState(0);

    const [data, setData] = useState<PortfolioValuationResponse | null>(null);
    const previousPricesRef = useRef<Map<number, number>>(new Map());
    const [priceChanges, setPriceChanges] = useState<Record<number, "up" | "down">>({});
    const previousPortfolioValuesRef = useRef<Map<number, number>>(new Map());
    const [portfolioChanges, setPortfolioChanges] = useState<Record<number, "up" | "down">>({});
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedReport, setSelectedReport] = useState<PortfolioReport | null>(null);
    const [selectedCompanyName, setSelectedCompanyName] = useState("");
    const [detailsLoadingId, setDetailsLoadingId] = useState<string | null>(null);
    const [detailsError, setDetailsError] = useState<string | null>(null);
    const detailRequestId = useRef(0);
    const formatNumber = (
        value: number | null | undefined,
        decimals = 0,
    ): string => {
        if (value == null) return "—";

        return value.toLocaleString(locale, {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals,
        });
    };

    // Refresh automatically every 60 seconds.
    useEffect(() => {
        const timer = window.setInterval(() => {
            setRefreshVersion(value => value + 1);
        }, REFRESH_INTERVAL_MS);

        return () => window.clearInterval(timer);
    }, []);
    //
    // Read persisted PortfolioValuationSnapshots.
    useEffect(() => {
    if (!data) return;

    const changes: Record<number, "up" | "down"> = {};

    for (const item of data.items ?? []) {
        if (
            item.isLocked ||
            item.currentPortfolioValue == null
        ) {
            continue;
        }

        const previousValue =
            previousPortfolioValuesRef.current.get(item.companyId);

        const currentValue = item.currentPortfolioValue;

        if (previousValue !== undefined) {
            if (currentValue > previousValue) {
                changes[item.companyId] = "up";
            } else if (currentValue < previousValue) {
                changes[item.companyId] = "down";
            }
        }

        previousPortfolioValuesRef.current.set(
            item.companyId,
            currentValue,
        );
    }

    setPortfolioChanges(changes);

    if (Object.keys(changes).length === 0) {
        return;
    }

    const timeoutId = window.setTimeout(() => {
        setPortfolioChanges({});
    }, 1000);

    return () => window.clearTimeout(timeoutId);
}, [data]);

// Read persisted PortfolioValuationSnapshots.
useEffect(() => {
    let active = true;

    setLoading(true);
    setError(null);

    getPortfolioValuationSnapshots({
        search,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy,
        sortDir,
    })
        .then(result => {
            if (active) {
                setData(result);
            }
        })
        .catch((err: unknown) => {
            if (active) {
                setError(
                    err instanceof Error
                        ? err.message
                        : "LOAD_ERROR",
                );
            }
        })
        .finally(() => {
            if (active) {
                setLoading(false);
            }
        });

    return () => {
        active = false;
    };
}, [search, pageNumber, sortBy, sortDir, refreshVersion]);

const totalCount = data?.totalCount ?? 0;
    const totalPages = Math.max(
        1,
        Math.ceil(totalCount / PAGE_SIZE),
    );
    const items = data?.items ?? [];
    const openDetails = async (
        item: PortfolioValuationItem,
    ) => {
        const requestId = ++detailRequestId.current;

        setDetailsLoadingId(item.disclosureId);
        setDetailsError(null);
        setSelectedReport(null);

        try {
            const report = await getPortfolioByDisclosureId(
                item.disclosureId,
                true,
            );

            if (requestId !== detailRequestId.current) {
                return;
            }

            if (!report) {
                setDetailsError("NOT_FOUND");
                return;
            }

            setSelectedCompanyName(
                item.companyName ?? item.symbol ?? "",
            );

            setSelectedReport(report);
        } catch {
            if (requestId === detailRequestId.current) {
                setDetailsError("DETAILS_ERROR");
            }
        } finally {
            if (requestId === detailRequestId.current) {
                setDetailsLoadingId(null);
            }
        }
    };
    //
    useEffect(() => { if (!data) return;

    const changes: Record<number, "up" | "down"> = {};

    for (const item of data.items ?? []) {
        if (
            item.isLocked ||
            item.currentSharePrice == null
        ) {
            continue;
        }

        const previousPrice =
            previousPricesRef.current.get(item.companyId);

        const currentPrice = item.currentSharePrice;

        if (previousPrice !== undefined) {
            if (currentPrice > previousPrice) {
                changes[item.companyId] = "up";
            } else if (currentPrice < previousPrice) {
                changes[item.companyId] = "down";
            }
        }

        previousPricesRef.current.set(
            item.companyId,
            currentPrice,
        );
    }

    setPriceChanges(changes);

    if (Object.keys(changes).length === 0) {
        return;
    }

    const timeoutId = window.setTimeout(() => {
        setPriceChanges({});
    }, 1000);

    return () => window.clearTimeout(timeoutId);
    }, [data]);
    //
    //

    return (
        <div className="space-y-6 p-6" dir={direction}>
            <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold">
                        {tMarket("portfolioValuation.title")}
                    </h1>

                    <p className="mt-2 text-muted-foreground">
                        {tMarket("portfolioValuation.description")}
                    </p>
                </div>

                <div className="flex flex-wrap gap-2">
                    <button
                        type="button"
                        className="rounded-md border px-4 py-2 text-sm hover:bg-muted disabled:opacity-50"
                        onClick={() => {
                            setRefreshVersion(value => value + 1);
                        }}
                        disabled={loading}
                    >
                        {isPersian ? "به‌روزرسانی" : "Refresh"}
                    </button>                    
                </div>
            </div>

            <div className="rounded-lg border bg-card p-4">
                <form
                    className="flex flex-wrap items-end gap-3"
                    onSubmit={event => {
                        event.preventDefault();
                        setPageNumber(1);
                        setSearch(searchInput.trim());
                    }}
                >
                    <div className="min-w-56 flex-1">
                        <label className="mb-2 block text-sm">
                            {tMarket("portfolioValuation.searchLabel")}
                        </label>

                        <input
                            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                            placeholder={tMarket(
                                "portfolioValuation.searchPlaceholder",
                            )}
                            value={searchInput}
                            onChange={event =>
                                setSearchInput(event.target.value)
                            }
                        />
                    </div>

                    <button
                        type="submit"
                        className="rounded-md bg-primary px-5 py-2 text-sm text-primary-foreground"
                    >
                        {tMarket("portfolioValuation.searchButton")}
                    </button>

                    <div>
                        <label className="mb-2 block text-sm">
                            {tMarket("portfolioValuation.sortLabel")}
                        </label>

                        <select
                            className="rounded-md border bg-background px-3 py-2 text-sm"
                            value={sortBy}
                            onChange={event => {
                                setSortBy(event.target.value);
                                setPageNumber(1);
                            }}
                        >
                            <option value="DISCOUNT">
                                {tMarket("portfolioValuation.sortOptions.discount")}
                            </option>
                            <option value="SYMBOL">
                                {tMarket("portfolioValuation.sortOptions.symbol")}
                            </option>
                            <option value="CURRENTPORTFOLIOVALUE">
                                {tMarket("portfolioValuation.sortOptions.portfolioValue")}
                            </option>
                            <option value="CURRENTPORTFOLIOVALUEPERSHARE">
                                {tMarket("portfolioValuation.sortOptions.valuePerShare")}
                            </option>
                            <option value="CURRENTSHAREPRICE">
                                {tMarket("portfolioValuation.sortOptions.sharePrice")}
                            </option>
                        </select>
                    </div>

                    <div>
                        <label className="mb-2 block text-sm">
                            {tMarket("portfolioValuation.sortDirection")}
                        </label>

                        <select
                            className="rounded-md border bg-background px-3 py-2 text-sm"
                            value={sortDir}
                            onChange={event => {
                                setSortDir(
                                    event.target.value as "asc" | "desc",
                                );
                                setPageNumber(1);
                            }}
                        >
                            <option value="desc">
                                {tMarket("portfolioValuation.descending")}
                            </option>
                            <option value="asc">
                                {tMarket("portfolioValuation.ascending")}
                            </option>
                        </select>
                    </div>
                </form>
            </div>

            {error && (
                <div
                    role="alert"
                    className="rounded-md border border-destructive p-4 text-sm text-destructive"
                >
                    {tMarket("portfolioValuation.loadError")}
                    <pre className="mt-2 whitespace-pre-wrap break-all text-xs" dir="ltr">
                        {error}
                    </pre>
                </div>
            )}

            {detailsError && (
                <div
                    role="alert"
                    className="rounded-md border border-destructive p-4 text-sm text-destructive"
                >
                    {tMarket(
                        detailsError === "NOT_FOUND"
                            ? "portfolioValuation.detailsNotFound"
                            : "portfolioValuation.detailsError",
                    )}
                </div>
            )}

            <div className="overflow-hidden rounded-lg border bg-card">
                <div className="flex items-center justify-between border-b p-4">
                    <h2 className="font-semibold">
                        {tMarket("portfolioValuation.resultsTitle")}
                    </h2>

                    <span className="text-sm text-muted-foreground">
                        {tMarket("portfolioValuation.totalCount")}
                        {": "}
                        {formatNumber(totalCount)}
                    </span>
                </div>

                <div className="overflow-x-auto">
                    <table className="w-full min-w-[1100px] text-start text-sm">
                        <thead className="bg-muted/50">
                            <tr className="border-b">
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.symbol")}
                                </th>
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.period")}
                                </th>
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.portfolioValue")}
                                </th>
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.valuePerShare")}
                                </th>
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.sharePrice")}
                                </th>                                
                                                             
                                <th className="p-3">                                
                                    {tMarket("portfolioValuation.columns.scale")}
                                </th>
                                {isAdmin && (   
                                   <th className="p-3 text-start">
                                       {tMarket("portfolioValuation.columns.coverage")}
                                   </th>)
                                }
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.details")}
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {items.map(item => {
                                const priceToPortfolioValuePercent =
                                   item.discountToPortfolioValuePercent == null
                                     ? null
                                     : 100 - item.discountToPortfolioValuePercent;

                                const complete =
                                    item.pricedListedPositionCount ===
                                    item.listedPositionCount;
                                const lockedValue = (
                                     <span className="inline-flex items-center gap-1.5 text-muted-foreground">
                                         <LockKeyhole
                                             className="size-3.5"
                                             aria-hidden="true"
                                         />
                                         <span>••••</span>
                                     </span>
);
                                return (
                                    <tr key={item.companyId} className="border-b hover:bg-muted/30">
                                        <td className="p-3">
                                            <div className="flex items-center gap-1.5 font-semibold">
                                               {item.isLocked && (
                                               <LockKeyhole className="size-3.5 text-amber-500"
                                                            aria-hidden="true"
                                               />
                                               )}
                                               <span>{item.symbol ?? "—"}</span>
                                            </div>
                                            <div className="mt-1 text-xs text-muted-foreground">
                                                {item.companyName}
                                            </div>
                                        </td>
                                        <td className="p-3">
                                            {item.periodEndDate}
                                        </td>
                                       <td className={[                                       
                                          "p-3 tabular-nums",                                          
                                          "transition-colors duration-700 ease-out",                                          
                                          "motion-reduce:transition-none",
                                          !item.isLocked && portfolioChanges[item.companyId] === "up"                                          
                                             ? "bg-emerald-500/20"                                             
                                             : !item.isLocked && portfolioChanges[item.companyId] === "down"                                            
                                                 ? "bg-rose-500/20"                                                 
                                                 : "",    ].join(" ")}>                                                 
                                          {item.isLocked                                          
                                             ? lockedValue                                             
                                             : formatNumber(item.currentPortfolioValue)}                                             
                                        </td>
                                        <td className="p-3 tabular-nums">
                                            {item.isLocked                                            
                                               ? lockedValue                                               
                                               : formatNumber(item.currentPortfolioValuePerShare)}
                                        </td>
                                        <td className={["p-3 tabular-nums",                                        
                                                        "transition-colors duration-700 ease-out",
                                                        "motion-reduce:transition-none",
                                                        !item.isLocked && priceChanges[item.companyId] === "up"
                                                            ? "bg-emerald-500/20"
                                                            : !item.isLocked && priceChanges[item.companyId] === "down"
                                                              ? "bg-rose-500/20"
                                                              : "",].join(" ")}>
                                                 {item.isLocked                                                  
                                                    ? (lockedValue)                                                     
                                                    : (<>
                                                         {formatNumber(item.currentSharePrice)}
                                                         <div className="mt-1 text-xs text-muted-foreground">
                                                             {item.currentSharePriceTradeDate}
                                                         </div>
                                                         </>
                                                      )                                                      
                                                 }
                                        </td>
                                        <td className="p-3">                                        
                                            {item.isLocked                                             
                                                ? (lockedValue)                                        
                                                : priceToPortfolioValuePercent == null                                                            
                                                    ? ("—")                                                                
                                                    : ( <span className=                                                               
                                                           {priceToPortfolioValuePercent < 100                                                               
                                                               ? "font-semibold text-emerald-600"                                                                   
                                                               : priceToPortfolioValuePercent > 100                                                                   
                                                                    ? "font-semibold text-rose-600"                                                                       
                                                                    : "font-semibold"                                                                             
                                                           }                                                           
                                                        >
                                                           {formatNumber(priceToPortfolioValuePercent, 2)}%
                                                        </span>
                                                      )}
                                        </td>
                                        {isAdmin &&                                         
                                           (<td className="p-3">                                           
                                               {item.isLocked                                                
                                                  ? (lockedValue)                                                  
                                                  : (<> {formatNumber(item.pricedListedPositionCount)}
                                               {" / "}
                                               {formatNumber(item.listedPositionCount)}
                                               {!complete && (<div className="text-xs text-amber-600">
                                                                {tMarket("portfolioValuation.incompletePricing")}
                                                              </div>)}                                                              
                                               </>                                               
                                           )}
                                            </td>)}
                                        <td className="p-3">
                                            <button
                                                type="button"
                                                className="rounded-md border px-3 py-1.5 text-xs hover:bg-muted disabled:opacity-50"
                                                disabled={item.isLocked || detailsLoadingId !== null}
                                                onClick={() =>
                                                    void openDetails(item)
                                                }
                                            >
                                                {detailsLoadingId ===
                                                item.disclosureId
                                                    ? tMarket(
                                                        "portfolioValuation.loadingDetails",
                                                    )
                                                    : tMarket(
                                                        "portfolioValuation.viewPortfolio",
                                                    )}
                                            </button>
                                        </td>
                                    </tr>
                                );
                            })}

                            {items.length === 0 && (
                                <tr>
                                    <td
                                        colSpan={isAdmin ? 8 : 7}
                                        className="p-8 text-center text-muted-foreground"
                                    >
                                        {loading
                                            ? tMarket("portfolioValuation.loading")
                                            : tMarket("portfolioValuation.empty")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-3 p-4">
                   <div className="flex gap-2">
                         <button
                             type="button"
                             className="rounded-md border px-4 py-2 text-sm disabled:opacity-40"
                             disabled={pageNumber <= 1 || loading}
                             onClick={() => setPageNumber(1)}
                         >
                             {tMarket("portfolioValuation.first")}
                         </button>

                         <button
                             type="button"
                             className="rounded-md border px-4 py-2 text-sm disabled:opacity-40"
                             disabled={pageNumber <= 1 || loading}
                             onClick={() => setPageNumber(p => Math.max(1, p - 1))}
                         >
                             {tMarket("portfolioValuation.previous")}
                         </button>
                         <span className="mt-3 text-sm text-muted-foreground">
                             {tMarket("portfolioValuation.pageStatus", {
                                 page: formatNumber(pageNumber),
                                 total: formatNumber(totalPages),
                             })}
                         </span>
                         <button
                             type="button"
                             className="rounded-md border px-4 py-2 text-sm disabled:opacity-40"
                             disabled={pageNumber >= totalPages || loading}
                             onClick={() => setPageNumber(p => Math.min(totalPages, p + 1))}
                         >
                             {tMarket("portfolioValuation.next")}
                         </button>

                         <button
                             type="button"
                             className="rounded-md border px-4 py-2 text-sm disabled:opacity-40"
                             disabled={pageNumber >= totalPages || loading}
                             onClick={() => setPageNumber(totalPages)}
                         >
                             {tMarket("portfolioValuation.last")}
                         </button>
</div>
                </div>
            </div>

            <p className="text-xs text-muted-foreground">
                {tMarket("portfolioValuation.disclaimer")}
            </p>

            <PortfolioReportDialog
                open={selectedReport !== null}
                report={selectedReport}
                parentCompanyName={selectedCompanyName}
                onClose={() => {
                    detailRequestId.current += 1;
                    setSelectedReport(null);
                    setDetailsLoadingId(null);
                }}
            />
        </div>
    );
}
