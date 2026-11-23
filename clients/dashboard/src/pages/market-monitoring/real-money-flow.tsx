
import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";

import {
    getRealMoneyFlowReport,
    getMySubscriptionSelections,
    type RealMoneyFlowReportParams,
    type RealMoneyFlowReportResponse,
} from "@/api/market-intelligence";

const PAGE_SIZE = 20;

type FlowFilters = Required<
    Pick<
        RealMoneyFlowReportParams,
        | "lookbackDays"
        | "minZScore"
        | "minFlowMultiple"
        | "minTradeValueMultiple"
    >
>;

const DEFAULT_FILTERS: FlowFilters = {
    lookbackDays: 20,
    minZScore: 1.5,
    minFlowMultiple: 2,
    minTradeValueMultiple: 1.5,
};

const FILTER_KEYS = [
    "lookbackDays",
    "minZScore",
    "minFlowMultiple",
    "minTradeValueMultiple",
] as const;

const PLAN_LIMITS: Record<string, number> = {
    free: 0,
    economic: 1,
    professional: 2,
    premium: 4,
};

const TABLE_COLUMNS = [
    "symbol",
    "tradeDate",
    "netFlow",
    "zScore",
    "flowMultiple",
    "tradeValueMultiple",
    "priceChange",
] as const;

type TableColumn = (typeof TABLE_COLUMNS)[number];

const INFO_COLUMNS = new Set<TableColumn>([
    "netFlow",
    "zScore",
    "flowMultiple",
    "tradeValueMultiple",
    "priceChange",
]);

// Parameter badges reuse their matching table-column explanations.
const PARAMETER_GUIDE_KEYS: Record<
    keyof FlowFilters,
    TableColumn | "lookbackDays"
> = {
    lookbackDays: "lookbackDays",
    minZScore: "zScore",
    minFlowMultiple: "flowMultiple",
    minTradeValueMultiple: "tradeValueMultiple",
};

type InfoPopupState = {
    id: string;
    titleKey: string;
    guideKey: string;
    days: number;
    top: number;
    left: number;
};

export default function RealMoneyFlowPage() {
    const { t: tNav, i18n } = useTranslation("navigation");
    const { t } = useTranslation("marketIntelligence");

    const isPersian = (
        i18n.resolvedLanguage ?? i18n.language
    ).toLowerCase().startsWith("fa");

    const locale = isPersian ? "fa-IR" : "en-US";

    const [pageNumber, setPageNumber] = useState(1);
    const [direction, setDirection] = useState<"Inflow" | "Outflow">("Inflow");
    const [data, setData] =
        useState<RealMoneyFlowReportResponse | null>(null);

    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    const [draftFilters, setDraftFilters] =
        useState<FlowFilters>({ ...DEFAULT_FILTERS });

    const [appliedFilters, setAppliedFilters] =
        useState<FlowFilters>({ ...DEFAULT_FILTERS });

    const [planKey, setPlanKey] = useState<string | null>(null);
    const [planError, setPlanError] = useState(false);
    const [filterError, setFilterError] = useState(false);

    const [infoPopup, setInfoPopup] =
        useState<InfoPopupState | null>(null);

    // Subscription plan
    useEffect(() => {
        let active = true;

        getMySubscriptionSelections()
            .then(result => {
                if (active) {
                    setPlanKey(result.planKey);
                    setPlanError(false);
                }
            })
            .catch(() => {
                if (active) {
                    setPlanError(true);
                }
            });

        return () => {
            active = false;
        };
    }, []);

    const allowedChanges = planKey === null
        ? 0
        : (PLAN_LIMITS[planKey] ?? 0);

    const changedCount = FILTER_KEYS.filter(
        key => draftFilters[key] !== DEFAULT_FILTERS[key]
    ).length;

    // Report data
    useEffect(() => {
        let active = true;

        setLoading(true);
        setHasError(false);

        getRealMoneyFlowReport({            
            ...appliedFilters,            
            direction,            
            pageNumber,            
            pageSize: PAGE_SIZE,        
        })
            .then(result => {
                if (active) {
                    setData(result);
                }
            })
            .catch(() => {
                if (active) {
                    setHasError(true);
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
    },  [pageNumber, appliedFilters, direction]);

    // Close popup on outside click, Escape, scroll or resize.
    useEffect(() => {
        if (!infoPopup) return;

        const handleOutsideClick = (event: PointerEvent) => {
            const target = event.target;

            if (
                target instanceof Element &&
                target.closest("[data-flow-info]")
            ) {
                return;
            }

            setInfoPopup(null);
        };

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                setInfoPopup(null);
            }
        };

        const handleScroll = (event: Event) => {
            const target = event.target;

            if (
                target instanceof Node &&
                document
                    .getElementById("real-money-flow-info-popup")
                    ?.contains(target)
            ) {
                return;
            }

            setInfoPopup(null);
        };

        const handleResize = () => {
            setInfoPopup(null);
        };

        document.addEventListener(
            "pointerdown",
            handleOutsideClick
        );

        document.addEventListener(
            "keydown",
            handleKeyDown
        );

        window.addEventListener(
            "scroll",
            handleScroll,
            true
        );

        window.addEventListener(
            "resize",
            handleResize
        );

        return () => {
            document.removeEventListener(
                "pointerdown",
                handleOutsideClick
            );

            document.removeEventListener(
                "keydown",
                handleKeyDown
            );

            window.removeEventListener(
                "scroll",
                handleScroll,
                true
            );

            window.removeEventListener(
                "resize",
                handleResize
            );
        };
    }, [infoPopup]);

    const formatNumber = (
        value: number,
        decimals = 0
    ) => value.toLocaleString(locale, {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals,
    });

    const formatDate = (value: string) =>
        new Intl.DateTimeFormat(locale, {
            year: "numeric",
            month: "2-digit",
            day: "2-digit",
            timeZone: "UTC",
        }).format(new Date(`${value}T12:00:00Z`));

    const applyFilters = () => {
        const valid =
            Number.isInteger(draftFilters.lookbackDays) &&
            draftFilters.lookbackDays >= 2 &&
            draftFilters.lookbackDays <= 252 &&
            FILTER_KEYS.every(key =>
                Number.isFinite(draftFilters[key]) &&
                draftFilters[key] >= 0
            ) &&
            changedCount <= allowedChanges;

        if (!valid || planKey === null) {
            setFilterError(true);
            return;
        }

        setFilterError(false);
        setPageNumber(1);
        setAppliedFilters({ ...draftFilters });
        setInfoPopup(null);
    };

    const resetFilters = () => {
        setDraftFilters({ ...DEFAULT_FILTERS });
        setFilterError(false);
    };

    // Shared popup for parameter badges and table headers.
    const toggleInfo = (
        id: string,
        titleKey: string,
        guideKey: string,
        days: number,
        button: HTMLButtonElement
    ) => {
        if (infoPopup?.id === id) {
            setInfoPopup(null);
            return;
        }

        const rect = button.getBoundingClientRect();

        const popupWidth = Math.min(
            400,
            window.innerWidth - 24
        );

        const preferredLeft = isPersian
            ? rect.right - popupWidth
            : rect.left;

        const left = Math.max(
            12,
            Math.min(
                preferredLeft,
                window.innerWidth - popupWidth - 12
            )
        );

        const estimatedHeight = 190;

        const top =
            window.innerHeight - rect.bottom < estimatedHeight
                ? Math.max(12, rect.top - estimatedHeight - 8)
                : rect.bottom + 8;

        setInfoPopup({
            id,
            titleKey,
            guideKey,
            days,
            top,
            left,
        });
    };

    return (
        <>
            <main
                className="space-y-4"
                dir={isPersian ? "rtl" : "ltr"}>                
                <header className="-mt-6 flex flex-wrap items-center justify-between gap-3">
                   <h1 className="inline-flex items-baseline gap-1 whitespace-nowrap">
                       <span className="text-2xl font-semibold">
                           {isPersian
                               ? "جریان پول حقیقی غیرعادی"
                               : "Unusual Real Money Flow"}
                       </span>

                       <span className="text-sm font-normal text-muted-foreground">
                           ({tNav("sections.marketMonitoring")})
                       </span>
                   </h1>   
                   <div role="group"                   
                        aria-label={isPersian                         
                                      ? "جهت جریان پول"                                      
                                      : "Money flow direction"                                      
                                   }                                   
                        className="inline-flex items-center gap-1 rounded-xl
                                   border border-border bg-muted/40 p-1">                   
                       <button type="button"                       
                               aria-pressed={direction === "Inflow"}                               
                               onClick={() => {                               
                                 setDirection("Inflow");                                 
                                 setPageNumber(1);                                 
                                              }                                              
                                       }                                       
                               className={`rounded-lg px-4 py-2 text-sm font-semibold                               
                                           transition-colors ${direction === "Inflow"                                           
                                               ? "bg-emerald-600 text-white shadow-sm"                                               
                                               : "text-muted-foreground hover:bg-muted"}`}>                       
                          {isPersian ? "ورود پول حقیقی" : "Real Money Inflow"}
                       </button>
                       <button
                          type="button"
                          aria-pressed={direction === "Outflow"}
                          onClick={() => {
                              setDirection("Outflow");
                              setPageNumber(1);
                          }}
                          className={`rounded-lg px-4 py-2 text-sm font-semibold transition-colors ${
                              direction === "Outflow"
                                  ? "bg-rose-600 text-white shadow-sm"
                                  : "text-muted-foreground hover:bg-muted"
                          }`}>
                          {isPersian ? "خروج پول حقیقی" : "Real Money Outflow"}
                       </button>
                    </div>         
                </header>
                {/* Compact responsive parameter form */}                
                <form onSubmit={event => {                
                       event.preventDefault();                       
                       applyFilters(); }}
                      className={`-mt-2 px-4 rounded-lg                      
                                 ${ direction === "Inflow"                                 
                                    ? "bg-emerald-50/70 dark:bg-emerald-950/20"                                    
                                    : "bg-rose-100/80 dark:bg-rose-900/40"                                    
                                  }`}>
                   <div className="flex flex-wrap items-center justify-between gap-2">
                      <h2 className="text-sm font-semibold">
                         {t("realMoneyFlow.parameters.title")}
                      </h2>                      
                      <span className="text-xs text-muted-foreground">
                         {t("realMoneyFlow.parameters.usage", {used: changedCount,                          
                                                               limit: allowedChanges,}                                                              
                           )                   
                         }
                      </span>
                   </div>
                   <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-4">
                       {FILTER_KEYS.map(key => {const isChanged =
                                                draftFilters[key] !== DEFAULT_FILTERS[key];
                                                const isDisabled = planKey === null ||
                                                    (!isChanged && changedCount >= allowedChanges);
                                                const popupId = `parameter:${key}`;
                                                const guideKey = PARAMETER_GUIDE_KEYS[key];
                                                return (                                                
                                                   <div key={key}                                                
                                                        className="flex flex-col gap-1">
                                                      <div className="flex items-center gap-1.5">                                                
                                                            <label htmlFor={`flow-parameter-${key}`}                                                
                                                                   className="text-xs font-medium">                                                               
                                                               {key === "minZScore"                                                                
                                                                    ? direction === "Inflow"                                                                     
                                                                         ? (isPersian ? "حداقل امتیاز Z ورود" : "Minimum Inflow Z-Score")                                                                         
                                                                         : (isPersian ? "حداقل امتیاز Z خروج" : "Minimum Outflow Z-Score")                                                                         
                                                                    : key === "minFlowMultiple"                                                                    
                                                                         ? direction === "Inflow"                                                                          
                                                                               ? (isPersian ? "حداقل ضریب ورود پول" : "Minimum Inflow Multiple")                                                                               
                                                                               : (isPersian ? "حداقل ضریب خروج پول" : "Minimum Outflow Multiple")                                                                               
                                                                         : t(`realMoneyFlow.parameters.${key}`)}
                                                            </label>                                                
                                                            <button type="button"                                                
                                                                    data-flow-info                                                        
                                                                    aria-haspopup="dialog"                                                        
                                                                    aria-label={`${t("realMoneyFlow.headerGuide.title")}: ${t(`realMoneyFlow.parameters.${key}`)}`}                                                        
                                                                    aria-expanded={infoPopup?.id === popupId}                                                        
                                                                    aria-controls={infoPopup?.id === popupId                                                         
                                                                         ? "real-money-flow-info-popup"                                                             
                                                                         : undefined}                                                             
                                                                    onClick={event => toggleInfo(popupId,
                                                                             `realMoneyFlow.parameters.${key}`,
                                                                             `realMoneyFlow.headerGuide.${guideKey}`,
                                                                              draftFilters.lookbackDays,
                                                                              event.currentTarget)                                                                  
                                                                            }
                                                                    className="inline-flex h-4 w-4 shrink-0 
                                                                               items-center justify-center 
                                                                               rounded-full bg-pink-100 text-[10px] 
                                                                               font-bold text-pink-700 transition-colors 
                                                                               hover:bg-pink-200 focus-visible:outline-2
                                                                               focus-visible:outline-offset-2
                                                                               focus-visible:outline-pink-400
                                                                               dark:bg-pink-950 dark:text-pink-200
                                                                               dark:hover:bg-pink-900"                                                                   
                                                            >
                                                               i
                                                            </button>
                                                      </div>
                                                      <input
                                                           id={`flow-parameter-${key}`}
                                                           type="number"
                                                           min={key === "lookbackDays" ? 2 : 0}
                                                           max={
                                                               key === "lookbackDays"
                                                                   ? 252
                                                                   : undefined
                                                           }
                                                           step={
                                                               key === "lookbackDays"
                                                                   ? 1
                                                                   : "any"
                                                           }
                                                           value={draftFilters[key]}
                                                           disabled={isDisabled}
                                                           onChange={event => {
                                                               const value = Number(event.target.value);

                                                               setDraftFilters(previous => ({
                                                                   ...previous,
                                                                   [key]: value,
                                                               }));
                                                           }}
                                                           className={`h-8 w-full rounded-md border-0 px-3 text-sm shadow-sm outline-none transition-colors duration-200 focus:ring-2 focus:ring-emerald-300 dark:focus:ring-emerald-700 disabled:cursor-not-allowed disabled:opacity-50 ${
                                                               isChanged
                                                                   ? "bg-amber-100/80 dark:bg-amber-900/40"
                                                                   : "bg-white dark:bg-slate-900"
                                                           }`}
                                                      />
                                                   </div>
                                                );                                                       
                                               }                                               
                                       )                                      
                       }
                   </div>
                   {/* One row: signal count, information hint, actions */}
                   <div className="flex flex-wrap items-center justify-between gap-3">
                       {/* Right: help hint */}
                       <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                           <span className="inline-flex h-4 w-4 shrink-0
                                            items-center justify-center rounded-full 
                                            bg-pink-100 text-[10px] font-bold 
                                            text-pink-700 dark:bg-emerald-900
                                            dark:text-emerald-200">
                               i
                           </span>
                           <span>
                               {t("realMoneyFlow.headerGuide.hint")}
                           </span>
                       </p>
                       {/* Center: signal count */}
                       <div className="text-xs font-medium">
                           {!loading && !hasError && data && (
                               <span>
                                   {t("realMoneyFlow.totalCount", {
                                       count: data.totalCount,
                                   })}
                               </span>
                           )}
                       </div>
                       {/* Left: actions */}
                       <div className="flex gap-2">
                           <button
                               type="button"
                               onClick={resetFilters}
                               className="rounded-md border px-3 py-1.5 text-xs"
                           >
                               {t("realMoneyFlow.parameters.reset")}
                           </button>

                           <button
                               type="submit"
                               disabled={planKey === null}
                               className="rounded-md bg-primary px-3 py-1.5 text-xs text-primary-foreground disabled:opacity-50"
                           >
                               {t("realMoneyFlow.parameters.apply")}
                           </button>
                       </div>
                   </div>

                   {filterError && (
                       <p className="text-xs text-rose-600" role="alert">
                           {t("realMoneyFlow.parameters.invalid")}
                       </p>
                   )}

                   {planError && (
                       <p className="text-xs text-rose-600" role="alert">
                           {t("realMoneyFlow.parameters.planError")}
                       </p>
                   )}
                </form>
                {/* Report status */}
                {loading && (
                    <p className="text-muted-foreground">
                        {t("realMoneyFlow.loading")}
                    </p>
                )}

                {hasError && (
                    <p className="text-rose-600" role="alert">
                        {t("realMoneyFlow.loadError")}
                    </p>
                )}

                {!loading && !hasError && data && (
                    <section className="space-y-3">
                        {data.items.length === 0 ? (
                            <p className="text-muted-foreground">
                                {t("realMoneyFlow.noData")}
                            </p>
                        ) : (
                            <div className="overflow-x-auto rounded-lg border">
                                <table className="w-full min-w-[900px] text-sm">
                                    <thead className="border-b bg-muted/50">
                                        <tr>
                                            {TABLE_COLUMNS.map(key => {
                                                const popupId =
                                                    `column:${key}`;

                                                return (
                                                    <th
                                                        key={key}
                                                        scope="col"
                                                        className="whitespace-nowrap px-3 py-2 text-start font-medium"
                                                    >
                                                        <div className="flex items-center gap-1.5">
                                                            <span>                                                                
                                                              {key === "netFlow"                                                              
                                                                ? direction === "Inflow"                                                                
                                                                  ? (isPersian                                                                   
                                                                      ? "خالص ورود پول حقیقی"                                                                   
                                                                      : "Net Real Money Inflow")
                                                                  : (isPersian ? "خالص خروج پول حقیقی"                                                                 
                                                                               : "Net Real Money Outflow")                                                                               
                                                                : key === "zScore"                                                                
                                                                      ? direction === "Inflow"                                                                      
                                                                         ? (isPersian                                                                          
                                                                             ? "امتیاز Z ورود"                                                                              
                                                                             : "Inflow Z-Score")                                                                             
                                                                         : (isPersian                                                                          
                                                                             ? "امتیاز Z خروج"                                                                             
                                                                             : "Outflow Z-Score")                                                                             
                                                                      : key === "flowMultiple"                                                                      
                                                                             ? direction === "Inflow"                                                                             
                                                                                ? (isPersian                                                                                
                                                                                   ? "ضریب ورود پول"                                                                                   
                                                                                   : "Inflow Multiple")                                                                                   
                                                                                : (isPersian ? "ضریب خروج پول"                                                                                 
                                                                                             : "Outflow Multiple")                                                                                              
                                                                             : t(`realMoneyFlow.${key}`)}
                                                            </span>
                                                            {INFO_COLUMNS.has(key) && (
                                                                <button
                                                                    type="button"
                                                                    data-flow-info
                                                                    aria-haspopup="dialog"
                                                                    aria-label={`${t("realMoneyFlow.headerGuide.title")}: ${t(`realMoneyFlow.${key}`)}`}
                                                                    aria-expanded={infoPopup?.id === popupId}
                                                                    aria-controls={infoPopup?.id === popupId ? "real-money-flow-info-popup" : undefined}
                                                                    onClick={event =>
                                                                        toggleInfo(
                                                                            popupId,
                                                                            `realMoneyFlow.${key}`,
                                                                            `realMoneyFlow.headerGuide.${key}`,
                                                                            appliedFilters.lookbackDays,
                                                                            event.currentTarget
                                                                        )
                                                                    }
                                                                    className="inline-flex h-4 w-4 shrink-0 
                                                                               items-center justify-center
                                                                               rounded-full bg-pink-100 text-[10px] 
                                                                               font-bold text-pink-700 transition-colors
                                                                               hover:bg-pink-200 focus-visible:outline-2
                                                                               focus-visible:outline-offset-2 
                                                                               focus-visible:outline-pink-500 
                                                                               dark:bg-pink-900 dark:text-pink-200"
                                                                >
                                                                    i
                                                                </button>
                                                            )}
                                                        </div>
                                                    </th>
                                                );
                                            })}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {data.items.map(item => {
                                            const priceChange =
                                                item.adjustedPriceChangePercent ??
                                                item.rawPriceChangePercent;

                                            return (
                                                <tr
                                                    key={item.companyId}
                                                    className="border-b last:border-b-0"
                                                >
                                                    <td className="whitespace-nowrap px-3 py-2 font-medium">
                                                        {item.symbol}
                                                    </td>

                                                    <td className="whitespace-nowrap px-3 py-2">
                                                        {formatDate(item.endDate)}
                                                    </td>

                                                    <td className="whitespace-nowrap px-3 py-2 tabular-nums">
                                                        <span dir="ltr" className={direction === "Outflow"                                                        
                                                                                    ? "text-rose-600"                                                                                    
                                                                                    : "text-emerald-600"}>                                                        
                                                           {formatNumber(direction === "Outflow"                                                           
                                                               ? Math.abs(item.netFlow) / 1_000_000                                                               
                                                               : item.netFlow / 1_000_000, 0)} m                                                        
                                                        </span>
                                                    </td>

                                                    <td className="px-3 py-2 tabular-nums">
                                                        {formatNumber(item.zScore, 2)}
                                                    </td>

                                                    <td className="px-3 py-2 tabular-nums">
                                                        {formatNumber(item.flowMultiple, 2)}
                                                    </td>

                                                    <td className="px-3 py-2 tabular-nums">
                                                        {formatNumber(
                                                            item.tradeValueMultiple,
                                                            2
                                                        )}
                                                    </td>

                                                    <td
                                                        className={`whitespace-nowrap px-3 py-2 tabular-nums ${
                                                            priceChange >= 0
                                                                ? "text-emerald-600"
                                                                : "text-rose-600"
                                                        }`}
                                                    >
                                                        {priceChange > 0 ? "+" : ""}
                                                        {formatNumber(priceChange, 2)}%
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>
                        )}

                        {/* Pagination */}
                        <div className="flex items-center justify-between gap-3">
                            <button
                                type="button"
                                className="rounded-md border px-3 py-1.5 text-sm disabled:opacity-50"
                                disabled={!data.hasPrevious}
                                onClick={() =>
                                    setPageNumber(page =>
                                        Math.max(1, page - 1)
                                    )
                                }
                            >
                                {t("realMoneyFlow.previous")}
                            </button>

                            <span className="text-sm text-muted-foreground">
                                {t("realMoneyFlow.pagination", {
                                    page: data.pageNumber,
                                    total: data.totalPages,
                                })}
                            </span>

                            <button
                                type="button"
                                className="rounded-md border px-3 py-1.5 text-sm disabled:opacity-50"
                                disabled={!data.hasNext}
                                onClick={() =>
                                    setPageNumber(page => page + 1)
                                }
                            >
                                {t("realMoneyFlow.next")}
                            </button>
                        </div>
                    </section>
                )}
            </main>

            {/* Shared floating popup: no table or form layout shift */}
            {infoPopup &&
                createPortal(
                    <div
                        id="real-money-flow-info-popup"
                        data-flow-info
                        role="dialog"
                        aria-label={t(infoPopup.titleKey)}
                        dir={isPersian ? "rtl" : "ltr"}
                        className="fixed z-[1000] max-h-[65vh] overflow-y-auto rounded-lg border border-emerald-200 bg-white p-3 text-sm text-slate-800 shadow-xl dark:border-emerald-800 dark:bg-slate-900 dark:text-slate-100"
                        style={{
                            top: infoPopup.top,
                            left: infoPopup.left,
                            width: "min(400px, calc(100vw - 24px))",
                        }}
                    >
                        <div className="flex items-start justify-between gap-3">
                            <div className="min-w-0 space-y-2">
                                <p className="font-semibold text-emerald-700 dark:text-emerald-300">
                                    {t(infoPopup.titleKey)}
                                </p>

                                <p className="text-xs leading-6 text-slate-600 dark:text-slate-300">
                                    {t(infoPopup.guideKey, {
                                        days: infoPopup.days,
                                    })}
                                </p>
                            </div>

                            <button
                                type="button"
                                onClick={() => setInfoPopup(null)}
                                aria-label={t("actions.close")}
                                className="shrink-0 rounded px-2 py-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
                            >
                                ×
                            </button>
                        </div>
                    </div>,
                    document.body
                )}
        </>
    );
}
