import { useTranslation } from "react-i18next";

import {
    useRef,
    useState,
    type PointerEvent as ReactPointerEvent,
} from "react";
import {
    PortfolioAuditStatus,
    PortfolioSourceType,
} from "@/api/market-intelligence";

import type {
    PortfolioPosition,
    PortfolioReport,
} from "@/api/market-intelligence";

type PortfolioReportDialogProps = {
    open: boolean;
    report: PortfolioReport | null;
    onClose: () => void;
    onPrevious?: () => void;
    onNext?: () => void;
    onNavigate?: (disclosureId: string) => void;
};

export function PortfolioReportDialog({
    open,
    report,
    onClose,
    onPrevious,
    onNext,
    onNavigate,
}: PortfolioReportDialogProps) {
    const { t: tMarket, i18n } = useTranslation("marketIntelligence");

    const numberLocale =
        i18n.resolvedLanguage
            ?.toLowerCase()
            .startsWith("fa")
            ? "fa-IR"
            : "en-US";

    const [windowOffset, setWindowOffset] =
        useState({ x: 0, y: 0 });

    const windowDragRef = useRef<{
        startX: number;
        startY: number;
        offsetX: number;
        offsetY: number;
    } | null>(null);

    const handlePointerDown = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        if (event.button !== 0) {
            return;
        }

        event.currentTarget.setPointerCapture(
            event.pointerId,
        );

        windowDragRef.current = {
            startX: event.clientX,
            startY: event.clientY,
            offsetX: windowOffset.x,
            offsetY: windowOffset.y,
        };
    };

    const handlePointerMove = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        const drag = windowDragRef.current;

        if (!drag) {
            return;
        }

        setWindowOffset({
            x:
                drag.offsetX +
                event.clientX -
                drag.startX,
            y:
                drag.offsetY +
                event.clientY -
                drag.startY,
        });
    };

    const handlePointerUp = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        if (
            event.currentTarget.hasPointerCapture(
                event.pointerId,
            )
        ) {
            event.currentTarget.releasePointerCapture(
                event.pointerId,
            );
        }

        windowDragRef.current = null;
    };

    if (!open || !report) {
        return null;
    }
    const monthlyTarget =
        report.navigationTargets.find(
            (target) =>
                target.sourceType ===
                PortfolioSourceType.MonthlyActivity,
        );

    const financialUnauditedTarget =
        report.navigationTargets.find(
            (target) =>
                target.sourceType ===
                PortfolioSourceType.FinancialStatement &&
                target.auditStatus ===
                PortfolioAuditStatus.Unaudited,
        );

    const financialAuditedTarget =
        report.navigationTargets.find(
            (target) =>
                target.sourceType ===
                PortfolioSourceType.FinancialStatement &&
                target.auditStatus ===
                PortfolioAuditStatus.Audited,
        );

    const isMonthly =
        report.sourceType ===
        PortfolioSourceType.MonthlyActivity;

    const isFinancial =
        report.sourceType ===
        PortfolioSourceType.FinancialStatement;

    const isUnaudited =
        report.auditStatus ===
        PortfolioAuditStatus.Unaudited;

    const isAudited =
        report.auditStatus ===
        PortfolioAuditStatus.Audited;

    const listedPositions = report.positions
        .filter((position) => position.isListed)
        .sort(
            (a, b) =>
                (b.endingMarketValue ?? 0) -
                (a.endingMarketValue ?? 0),
        );

    const unlistedPositions = report.positions
        .filter((position) => !position.isListed)
        .sort(
            (a, b) =>
                (b.endingCost ?? 0) -
                (a.endingCost ?? 0),
        );
    const totalPositions = listedPositions.length + unlistedPositions.length;
    const totalReportedPortfolioValue =
        (report.listedReportedMarketValue ?? 0) +
        (report.unlistedReportedValue ?? 0);
    const financialTarget =
        isFinancial
            ? (
                isAudited
                    ? financialAuditedTarget
                    : financialUnauditedTarget
            )
            : (
                financialUnauditedTarget ??
                financialAuditedTarget
            );
    return (
        <div
            dir={i18n.dir()}
            className="fixed left-1/2 top-12 z-50 w-[min(1200px,calc(100vw-2rem))] overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-2xl"
            style={{
                transform:
                    `translate(calc(-50% + ${windowOffset.x}px), ${windowOffset.y}px)`,
            }}
        >
            <div
                onPointerDown={handlePointerDown}
                onPointerMove={handlePointerMove}
                onPointerUp={handlePointerUp}
                onPointerCancel={handlePointerUp}
                className="relative flex min-h-[64px] touch-none select-none items-center justify-center border-b border-[var(--color-border)] px-4 py-2 cursor-grab active:cursor-grabbing"
            >
                {/* Right side: Previous / Next + Monthly / Financial */}
                <div className="absolute right-3 flex items-center gap-2">
                    <div className="flex items-center overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] shadow-xs">
                        <button
                            type="button"
                            onPointerDown={(event) =>
                                event.stopPropagation()
                            }
                            onClick={onPrevious}
                            disabled={
                                !report.previousDisclosureId ||
                                !onPrevious
                            }
                            className="grid size-8 place-items-center text-xl font-medium transition-colors hover:bg-[var(--color-muted)] disabled:cursor-not-allowed disabled:opacity-30"
                            title={tMarket(
                                "portfolioViewer.previousReport",
                            )}
                        >
                            ‹
                        </button>

                        <div className="h-5 w-px bg-[var(--color-border)]" />

                        <button
                            type="button"
                            onPointerDown={(event) =>
                                event.stopPropagation()
                            }
                            onClick={onNext}
                            disabled={
                                !report.nextDisclosureId ||
                                !onNext
                            }
                            className="grid size-8 place-items-center text-xl font-medium transition-colors hover:bg-[var(--color-muted)] disabled:cursor-not-allowed disabled:opacity-30"
                            title={tMarket(
                                "portfolioViewer.nextReport",
                            )}
                        >
                            ›
                        </button>
                    </div>

                    <div className="flex items-center gap-1 rounded-lg bg-[var(--color-muted)] p-1">
                        <button
                            type="button"
                            onPointerDown={(event) =>
                                event.stopPropagation()
                            }
                            disabled={!monthlyTarget || !onNavigate}
                            onClick={() => {
                                if (
                                    monthlyTarget &&
                                    !isMonthly
                                ) {
                                    onNavigate?.(
                                        monthlyTarget.disclosureId,
                                    );
                                }
                            }}
                            className={
                                `rounded-md px-3 py-1.5 text-sm font-medium transition-colors ` +
                                (
                                    isMonthly
                                        ? "bg-[var(--color-background)] shadow-sm"
                                        : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
                                ) +
                                " disabled:cursor-not-allowed disabled:opacity-40"
                            }
                        >
                            {tMarket(
                                "portfolioViewer.tabs.monthlyActivity",
                            )}
                        </button>

                        <button
                            type="button"
                            onPointerDown={(event) =>
                                event.stopPropagation()
                            }
                            disabled={!financialTarget || !onNavigate}
                            onClick={() => {
                                if (
                                    financialTarget &&
                                    !isFinancial
                                ) {
                                    onNavigate?.(
                                        financialTarget.disclosureId,
                                    );
                                }
                            }}
                            className={
                                `rounded-md px-3 py-1.5 text-sm font-medium transition-colors ` +
                                (
                                    isFinancial
                                        ? "bg-[var(--color-background)] shadow-sm"
                                        : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
                                ) +
                                " disabled:cursor-not-allowed disabled:opacity-40"
                            }
                        >
                            {tMarket(
                                "portfolioViewer.tabs.financialStatements",
                            )}
                        </button>
                    </div>
                </div>

                {/* Center: report title */}
                <div className="max-w-[420px] text-center">
                    <div className="font-semibold">
                        {tMarket("portfolioViewer.title")}
                    </div>

                    <div className="mt-0.5 text-xs text-[var(--color-muted-foreground)]">
                        {tMarket("portfolioViewer.period")}{" "}
                        <span
                            dir="ltr"
                            className="tabular-nums"
                        >
                            {report.periodEndDate}
                        </span>

                        {" — "}

                        <span className="font-bold text-[var(--color-destructive)]">
                            {isMonthly
                                ? tMarket(
                                    "portfolioViewer.tabs.monthlyActivity",
                                )
                                : isFinancial
                                    ? tMarket(
                                        "portfolioViewer.tabs.financialStatements",
                                    )
                                    : null}
                        </span>

                        {" · "}

                        {tMarket("portfolioViewer.tracingNo")}{" "}
                        <span className="tabular-nums">
                            {report.tracingNo.toLocaleString()}
                        </span>
                    </div>
                </div>

                {/* Left side: Audit status + Close */}
                <div className="absolute left-3 flex items-center gap-2">
                    {isFinancial && (
                        <div className="flex items-center gap-1 rounded-lg bg-[var(--color-muted)] p-1">
                            <button
                                type="button"
                                onPointerDown={(event) =>
                                    event.stopPropagation()
                                }
                                disabled={
                                    !financialUnauditedTarget ||
                                    !onNavigate
                                }
                                onClick={() => {
                                    if (
                                        financialUnauditedTarget &&
                                        !isUnaudited
                                    ) {
                                        onNavigate?.(
                                            financialUnauditedTarget
                                                .disclosureId,
                                        );
                                    }
                                }}
                                className={
                                    `rounded-md px-3 py-1.5 text-xs font-medium transition-colors ` +
                                    (
                                        isUnaudited
                                            ? "bg-[var(--color-background)] shadow-sm"
                                            : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
                                    ) +
                                    " disabled:cursor-not-allowed disabled:opacity-40"
                                }
                            >
                                {tMarket(
                                    "portfolioViewer.tabs.unaudited",
                                )}
                            </button>

                            <button
                                type="button"
                                onPointerDown={(event) =>
                                    event.stopPropagation()
                                }
                                disabled={
                                    !financialAuditedTarget ||
                                    !onNavigate
                                }
                                onClick={() => {
                                    if (
                                        financialAuditedTarget &&
                                        !isAudited
                                    ) {
                                        onNavigate?.(
                                            financialAuditedTarget
                                                .disclosureId,
                                        );
                                    }
                                }}
                                className={
                                    `rounded-md px-3 py-1.5 text-xs font-medium transition-colors ` +
                                    (
                                        isAudited
                                            ? "bg-[var(--color-background)] shadow-sm"
                                            : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
                                    ) +
                                    " disabled:cursor-not-allowed disabled:opacity-40"
                                }
                            >
                                {tMarket(
                                    "portfolioViewer.tabs.audited",
                                )}
                            </button>
                        </div>
                    )}

                    <button
                        type="button"
                        onPointerDown={(event) =>
                            event.stopPropagation()
                        }
                        onClick={onClose}
                        className="grid size-8 place-items-center rounded-md text-xl leading-none text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]"
                        aria-label={tMarket("actions.close")}
                        title={tMarket("actions.close")}
                    >
                        ×
                    </button>
                </div>
            </div>
            <div className="max-h-[78vh] overflow-auto p-4">
                <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-[2fr_2fr_2fr_1fr_1fr_1fr]">
                    <SummaryCard
                        label={tMarket(
                            "portfolioViewer.summary.listedReportedMarketValue",
                        )}
                        value={report.listedReportedMarketValue}
                        locale={numberLocale}
                    />

                    <SummaryCard
                        label={tMarket(
                            "portfolioViewer.summary.unlistedReportedValue",
                        )}
                        value={report.unlistedReportedValue}
                        locale={numberLocale}
                    />
                    <SummaryCard
                        label={tMarket(
                            "portfolioViewer.summary.totalReportedPortfolioValue",
                        )}
                        value={totalReportedPortfolioValue}
                        locale={numberLocale}
                    />
                    <SummaryCard
                        label={tMarket(
                            "portfolioViewer.summary.totalCompanies",
                        )}
                        value={totalPositions}
                        locale={numberLocale}
                    />

                    <SummaryCard
                        label={tMarket(
                            "portfolioViewer.summary.listedCompanies",
                        )}
                        value={listedPositions.length}
                        locale={numberLocale}
                    />

                    <SummaryCard
                        label={tMarket(
                            "portfolioViewer.summary.unlistedCompanies",
                        )}
                        value={unlistedPositions.length}
                        locale={numberLocale}
                    />
                </div>
                {report.isLatestPortfolio && (
                    <div className="mb-4 grid gap-3 sm:grid-cols-3">
                        <SummaryCard
                            label="ارزش روز پرتفوی (میلیون ریال)"
                            value={report.currentPortfolioValue}
                            locale={numberLocale}
                        />

                        <SummaryCard
                            label="سرمایه ثبت‌شده (میلیون ریال)"
                            value={report.registeredCapital}
                            locale={numberLocale}
                        />

                        <SummaryCard
                            label="ارزش روز پرتفوی به ازای هر سهم (ریال)"
                            value={report.currentPortfolioValuePerShare}
                            locale={numberLocale}
                        />
                    </div>
                )}
                <div className="overflow-x-auto rounded-lg border border-[var(--color-border)]">
                    <table className="w-full min-w-[1000px] border-collapse text-center text-sm">
                        <thead>
                            <tr className="border-b bg-[var(--color-muted)]/40 text-xs font-semibold text-[var(--color-muted-foreground)]">
                                <th className="px-3 py-2 text-right">
                                    شرکت
                                </th>

                                <th className="px-3 py-2">
                                    نوع
                                </th>

                                <th className="px-3 py-2">
                                    درصد مالکیت
                                </th>

                                <th className="px-3 py-2">
                                    تعداد پایان دوره
                                </th>

                                <th className="px-3 py-2">
                                    بهای تمام‌شده
                                </th>

                                <th className="px-3 py-2">
                                    ارزش بازار
                                </th>

                                <th className="px-3 py-2">
                                    بهای هر سهم
                                </th>

                                <th className="px-3 py-2">
                                    قیمت بازار
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {listedPositions.length > 0 && (
                                <>
                                    <PortfolioGroupHeader
                                        title={tMarket(
                                            "portfolioViewer.groups.listedInvestments",
                                        )}
                                        countLabel={tMarket(
                                            "portfolioViewer.groups.countItems",
                                            {
                                                count:
                                                    listedPositions.length.toLocaleString(
                                                        numberLocale,
                                                    ),
                                            },
                                        )}
                                        isListed
                                    />

                                    {listedPositions.map(
                                        (position) => (
                                            <PortfolioRow
                                                key={position.id}
                                                position={position}
                                                locale={numberLocale}
                                                listedLabel={tMarket(
                                                    "portfolioViewer.types.listed",
                                                )}
                                                unlistedLabel={tMarket(
                                                    "portfolioViewer.types.unlisted",
                                                )}
                                            />
                                        ),
                                    )}
                                </>
                            )}

                            {unlistedPositions.length >
                                0 && (
                                    <>
                                    <PortfolioGroupHeader
                                        title={tMarket(
                                            "portfolioViewer.groups.unlistedInvestments",
                                        )}
                                        countLabel={tMarket(
                                            "portfolioViewer.groups.countItems",
                                            {
                                                count:
                                                    unlistedPositions.length.toLocaleString(
                                                        numberLocale,
                                                    ),
                                            },
                                        )}
                                        isListed={false}
                                    />

                                        {unlistedPositions.map(
                                            (position) => (
                                                <PortfolioRow
                                                    key={position.id}
                                                    position={position}
                                                    locale={numberLocale}
                                                    listedLabel={tMarket(
                                                        "portfolioViewer.types.listed",
                                                    )}
                                                    unlistedLabel={tMarket(
                                                        "portfolioViewer.types.unlisted",
                                                    )}
                                                />
                                            ),
                                        )}
                                    </>
                                )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}

function SummaryCard({
    label,
    value,
    locale,
}: {
    label: string;
    value: number | null;
    locale: string;
}) {
    return (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-4 py-3">
            <div className="text-xs text-[var(--color-muted-foreground)]">
                {label}
            </div>

            <div
                dir="ltr"
                className="mt-1 text-lg font-semibold tabular-nums"
            >
                {formatNumber(value, locale)}
            </div>
        </div>
    );

}

function PortfolioGroupHeader({
    title,
    countLabel,
    isListed,
}: {
    title: string;
    countLabel: string;
    isListed: boolean;
}) {
    return (
        <tr>
            <td
                colSpan={8}
                className={
                    isListed
                        ? "border-y border-emerald-500/20 bg-emerald-500/10 px-3 py-2.5 text-start font-semibold text-emerald-700 dark:text-emerald-300"
                        : "border-y border-amber-500/20 bg-amber-500/10 px-3 py-2.5 text-start font-semibold text-amber-700 dark:text-amber-300"
                }
            >
                {title}

                <span className="ms-2 text-xs font-normal opacity-70">
                    {countLabel}
                </span>
            </td>
        </tr>
    );
}

function PortfolioRow({
    position,
    locale,
    listedLabel,
    unlistedLabel,
}: {
    position: PortfolioPosition;
    locale: string;
    listedLabel: string;
    unlistedLabel: string;
}) {
    const displayName =
        position.symbol ??
        position.companyName ??
        position.rawCompanyName;

    return (
        <tr
            className={
                position.isListed
                    ? "border-b bg-emerald-500/[0.04] transition-colors last:border-0 hover:bg-emerald-500/[0.09]"
                    : "border-b bg-amber-500/[0.055] transition-colors last:border-0 hover:bg-amber-500/[0.11]"
            }
        >
            <td className="px-3 py-2 text-right">
                <div className="font-medium">
                    {displayName}
                </div>

                {position.symbol &&
                    position.companyName && (
                        <div className="mt-0.5 text-xs text-[var(--color-muted-foreground)]">
                            {position.companyName}
                        </div>
                    )}
            </td>

            <td className="px-3 py-2">
                {position.isListed
                    ? listedLabel
                    : unlistedLabel}
            </td>

            <NumberCell
                value={position.ownershipPercent}
                locale={locale}
            />

            <NumberCell
                value={position.endingQuantity}
                locale={locale}
            />

            <NumberCell
                value={position.endingCost}
                locale={locale}
            />

            <NumberCell
                value={position.endingMarketValue}
                locale={locale}
            />

            <NumberCell
                value={position.endingCostPerShare}
                locale={locale}
            />

            <NumberCell
                value={position.endingMarketPrice}
                locale={locale}
            />
        </tr>
    );
}
function NumberCell({
    value,
    locale,
}: {
    value: number | null;
    locale: string;
}) {
    return (
        <td
            dir="ltr"
            className="px-3 py-2 tabular-nums"
        >
            {formatNumber(value, locale)}
        </td>
    );
}
function formatNumber(
    value: number | null,
    locale: string,
) {
    return value?.toLocaleString(locale) ?? "—";
}