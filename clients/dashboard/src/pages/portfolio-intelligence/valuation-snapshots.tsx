
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";

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

    const [data, setData] =
        useState<PortfolioValuationResponse | null>(null);

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [selectedReport, setSelectedReport] =
        useState<PortfolioReport | null>(null);

    const [selectedCompanyName, setSelectedCompanyName] =
        useState("");

    const [detailsLoadingId, setDetailsLoadingId] =
        useState<string | null>(null);

    const [detailsError, setDetailsError] =
        useState<string | null>(null);

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

    return (
        <div className="space-y-6 p-6" dir={direction}>
            <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold">
                        {isPersian
                            ? "ارزش‌گذاری پرتفوی — محاسبات آماده"
                            : "Portfolio Valuation — Snapshots"}
                    </h1>

                    <p className="mt-2 text-sm text-muted-foreground">
                        {isPersian
                            ? "ارزش‌گذاری شرکت‌ها بر اساس نتایج ذخیره‌شده CalculationWorker"
                            : "Portfolio valuations from persisted CalculationWorker results"}
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

                    <Link
                        to="/portfolio-intelligence/valuation"
                        className="rounded-md border px-4 py-2 text-sm hover:bg-muted"
                    >
                        {isPersian
                            ? "مقایسه با گزارش قدیمی"
                            : "View legacy report"}
                    </Link>
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
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.scale")}
                                </th>
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.coverage")}
                                </th>
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

                                return (
                                    <tr
                                        key={item.companyId}
                                        className="border-b hover:bg-muted/30"
                                    >
                                        <td className="p-3">
                                            <div className="font-semibold">
                                                {item.symbol ?? "—"}
                                            </div>
                                            <div className="mt-1 text-xs text-muted-foreground">
                                                {item.companyName}
                                            </div>
                                        </td>

                                        <td className="p-3">
                                            {item.periodEndDate}
                                        </td>

                                        <td className="p-3 tabular-nums">
                                            {formatNumber(
                                                item.currentPortfolioValue,
                                            )}
                                        </td>

                                        <td className="p-3 tabular-nums">
                                            {formatNumber(
                                                item.currentPortfolioValuePerShare,
                                            )}
                                        </td>

                                        <td className="p-3 tabular-nums">
                                            {formatNumber(
                                                item.currentSharePrice,
                                            )}
                                            <div className="mt-1 text-xs text-muted-foreground">
                                                {item.currentSharePriceTradeDate}
                                            </div>
                                        </td>

                                        <td className="p-3">
                                            {priceToPortfolioValuePercent == null
                                                ? "—"
                                                : (
                                                    <span className={                                                    
                                                      priceToPortfolioValuePercent < 100                                                      
                                                         ? "font-semibold text-emerald-600"                                                         
                                                         : priceToPortfolioValuePercent > 100                                                         
                                                           ? "font-semibold text-rose-600"                                                           
                                                           : "font-semibold"}                                                           
                                                    >                                                    
                                                      {formatNumber(priceToPortfolioValuePercent, 2)}%                                                      
                                                    </span>
                                                )}
                                        </td>

                                        <td className="p-3">
                                            {formatNumber(
                                                item.pricedListedPositionCount,
                                            )}
                                            {" / "}
                                            {formatNumber(
                                                item.listedPositionCount,
                                            )}
                                            {!complete && (
                                                <div className="text-xs text-amber-600">
                                                    {tMarket(
                                                        "portfolioValuation.incompletePricing",
                                                    )}
                                                </div>
                                            )}
                                        </td>

                                        <td className="p-3">
                                            <button
                                                type="button"
                                                className="rounded-md border px-3 py-1.5 text-xs hover:bg-muted disabled:opacity-50"
                                                disabled={detailsLoadingId !== null}
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
                                        colSpan={8}
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
