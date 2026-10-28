
import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";

import {
    getPortfolioValuation,
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

export default function PortfolioValuationPage() {
    const { t: tMarket, i18n } =
        useTranslation("marketIntelligence");

    const isPersian = (i18n.resolvedLanguage ?? i18n.language)
        .toLowerCase()
        .startsWith("fa");

    const locale = isPersian ? "fa-IR" : "en-US";
    const direction = isPersian ? "rtl" : "ltr";

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

    const [searchInput, setSearchInput] = useState("");
    const [search, setSearch] = useState("");

    const [pageNumber, setPageNumber] = useState(1);
    const [sortBy, setSortBy] = useState("DISCOUNT");
    const [sortDir, setSortDir] =
        useState<"asc" | "desc">("desc");

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

    useEffect(() => {
        let active = true;

        setLoading(true);
        setError(null);

        getPortfolioValuation({
            search,
            pageNumber,
            pageSize: PAGE_SIZE,
            sortBy,
            sortDir,
        })
            .then((result) => {
                if (active) {
                    setData(result);
                }
            })
            .catch((err: unknown) => {
                if (active) {
                    setData(null);
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
    }, [search, pageNumber, sortBy, sortDir]);

    const totalCount = data?.totalCount ?? 0;

    const totalPages = Math.max(
        1,
        Math.ceil(totalCount / PAGE_SIZE),
    );

    const items = loading || error
        ? []
        : data?.items ?? [];

    const openDetails = async (
        item: PortfolioValuationItem,
    ) => {
        const requestId = ++detailRequestId.current;

        setSelectedReport(null);
        setDetailsLoadingId(item.disclosureId);
        setDetailsError(null);

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
        <div
            className="space-y-6 p-6"
            dir={direction}
        >
            <div>
                <h1 className="text-2xl font-bold">
                    {tMarket("portfolioValuation.title")}
                </h1>

                <p className="mt-2 text-muted-foreground">
                    {tMarket("portfolioValuation.description")}
                </p>
            </div>

            <div className="rounded-lg border bg-card p-4">
                <form
                    className="flex flex-wrap items-end gap-3"
                    onSubmit={(event) => {
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
                            onChange={(event) =>
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
                            onChange={(event) => {
                                setSortBy(event.target.value);
                                setPageNumber(1);
                            }}
                        >
                            <option value="DISCOUNT">
                                {tMarket(
                                    "portfolioValuation.sortOptions.discount",
                                )}
                            </option>

                            <option value="SYMBOL">
                                {tMarket(
                                    "portfolioValuation.sortOptions.symbol",
                                )}
                            </option>

                            <option value="CURRENTPORTFOLIOVALUE">
                                {tMarket(
                                    "portfolioValuation.sortOptions.portfolioValue",
                                )}
                            </option>

                            <option value="CURRENTPORTFOLIOVALUEPERSHARE">
                                {tMarket(
                                    "portfolioValuation.sortOptions.valuePerShare",
                                )}
                            </option>

                            <option value="CURRENTSHAREPRICE">
                                {tMarket(
                                    "portfolioValuation.sortOptions.sharePrice",
                                )}
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
                            onChange={(event) => {
                                setSortDir(
                                    event.target.value as
                                        "asc" | "desc",
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

        <pre
            dir="ltr"
            className="mt-2 whitespace-pre-wrap break-all text-xs"
        >
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
                    <table className="w-full min-w-[1150px] text-start text-sm">
                        <thead className="bg-muted/50">
                            <tr className="border-b">
                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.symbol")}
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket("portfolioValuation.columns.period")}
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket(
                                        "portfolioValuation.columns.portfolioValue",
                                    )}
                                    <div className="text-xs font-normal">
                                        {tMarket(
                                            "portfolioValuation.units.millionIrr",
                                        )}
                                    </div>
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket(
                                        "portfolioValuation.columns.valuePerShare",
                                    )}
                                    <div className="text-xs font-normal">
                                        {tMarket("portfolioValuation.units.irr")}
                                    </div>
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket(
                                        "portfolioValuation.columns.sharePrice",
                                    )}
                                    <div className="text-xs font-normal">
                                        {tMarket("portfolioValuation.units.irr")}
                                    </div>
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket(
                                        "portfolioValuation.columns.discount",
                                    )}
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket(
                                        "portfolioValuation.columns.coverage",
                                    )}
                                </th>

                                <th className="p-3 text-start">
                                    {tMarket(
                                        "portfolioValuation.columns.details",
                                    )}
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {items.map((item) => {
                                const discount =
                                    item.discountToPortfolioValuePercent;

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

                                        <td className="p-3 font-medium tabular-nums">
                                            {formatNumber(
                                                item.currentPortfolioValuePerShare,
                                            )}
                                        </td>

                                        <td className="p-3 tabular-nums">
                                            {formatNumber(
                                                item.currentSharePrice,
                                            )}

                                            {item.currentSharePriceTradeDate && (
                                                <div className="mt-1 text-xs text-muted-foreground">
                                                    {item.currentSharePriceTradeDate}
                                                </div>
                                            )}
                                        </td>

                                        <td className="p-3">
                                            {discount == null ? (
                                                "—"
                                            ) : (
                                                <span
                                                    className={
                                                        discount >= 0
                                                            ? "font-semibold text-emerald-600"
                                                            : "font-semibold text-rose-600"
                                                    }
                                                >
                                                    {tMarket(
                                                        discount >= 0
                                                            ? "portfolioValuation.discount"
                                                            : "portfolioValuation.premium",
                                                    )}
                                                    {" "}
                                                    {formatNumber(
                                                        Math.abs(discount),
                                                        2,
                                                    )}
                                                    {"%"}
                                                </span>
                                            )}
                                        </td>

                                        <td className="p-3">
                                            <div className="tabular-nums">
                                                {formatNumber(
                                                    item.pricedListedPositionCount,
                                                )}
                                                {" / "}
                                                {formatNumber(
                                                    item.listedPositionCount,
                                                )}
                                            </div>

                                            {!complete && (
                                                <div className="mt-1 text-xs text-amber-600">
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
                                                disabled={
                                                    detailsLoadingId !== null
                                                }
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

                            {!loading && items.length === 0 && (
                                <tr>
                                    <td
                                        colSpan={8}
                                        className="p-8 text-center text-muted-foreground"
                                    >
                                        {error
                                            ? tMarket(
                                                "portfolioValuation.loadFailed",
                                            )
                                            : tMarket(
                                                "portfolioValuation.empty",
                                            )}
                                    </td>
                                </tr>
                            )}

                            {loading && (
                                <tr>
                                    <td
                                        colSpan={8}
                                        className="p-8 text-center text-muted-foreground"
                                    >
                                        {tMarket("portfolioValuation.loading")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-3 p-4">
                    <span className="text-sm text-muted-foreground">
                        {tMarket("portfolioValuation.pageStatus", {
                            page: formatNumber(pageNumber),
                            total: formatNumber(totalPages),
                        })}
                    </span>

                    <div className="flex gap-2">
                        <button
                            type="button"
                            className="rounded-md border px-4 py-2 text-sm disabled:opacity-40"
                            disabled={pageNumber <= 1 || loading}
                            onClick={() =>
                                setPageNumber((p) =>
                                    Math.max(1, p - 1),
                                )
                            }
                        >
                            {tMarket("portfolioValuation.previous")}
                        </button>

                        <button
                            type="button"
                            className="rounded-md border px-4 py-2 text-sm disabled:opacity-40"
                            disabled={
                                pageNumber >= totalPages || loading
                            }
                            onClick={() =>
                                setPageNumber((p) =>
                                    Math.min(totalPages, p + 1),
                                )
                            }
                        >
                            {tMarket("portfolioValuation.next")}
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
