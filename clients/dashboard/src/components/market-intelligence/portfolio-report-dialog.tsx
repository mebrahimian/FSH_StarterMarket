import {
    useRef,
    useState,
    type PointerEvent as ReactPointerEvent,
} from "react";
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
};

export function PortfolioReportDialog({
    open,
    report,
    onClose,
    onPrevious,
    onNext,
}: PortfolioReportDialogProps) {
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

    return (
        <div
            dir="rtl"
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
                className="relative flex touch-none select-none items-center justify-center border-b border-[var(--color-border)] px-4 py-3 cursor-grab active:cursor-grabbing"
            >
                <div className="text-center">
                    <div className="font-semibold">
                        پرتفوی سرمایه‌گذاری
                    </div>

                    <div className="mt-0.5 text-xs text-[var(--color-muted-foreground)]">
                        دوره{" "}
                        <span
                            dir="ltr"
                            className="tabular-nums"
                        >
                            {report.periodEndDate}
                        </span>
                        {" · "}
                        کد رهگیری{" "}
                        <span className="tabular-nums">
                            {report.tracingNo.toLocaleString()}
                        </span>
                    </div>
                </div>

                <div className="absolute right-3 flex items-center overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] shadow-xs">
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
                        title="گزارش قبلی"
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
                        title="گزارش بعدی"
                    >
                        ›
                    </button>
                </div>

                <button
                    type="button"
                    onPointerDown={(event) =>
                        event.stopPropagation()
                    }
                    onClick={onClose}
                    className="absolute left-3 grid size-8 place-items-center rounded-md text-xl leading-none text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]"
                    aria-label="بستن"
                    title="بستن"
                >
                    ×
                </button>
            </div>

            <div className="max-h-[78vh] overflow-auto p-4">
                <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                    <SummaryCard
                        label="ارزش بازار بورسی گزارش‌شده"
                        value={
                            report.listedReportedMarketValue
                        }
                    />

                    <SummaryCard
                        label="بهای تمام‌شده غیربورسی"
                        value={
                            report.unlistedReportedValue
                        }
                    />

                    <SummaryCard
                        label="تعداد بورسی"
                        value={listedPositions.length}
                    />

                    <SummaryCard
                        label="تعداد غیربورسی"
                        value={unlistedPositions.length}
                    />
                </div>

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
                                        title="سرمایه‌گذاری‌های بورسی"
                                        count={
                                            listedPositions.length
                                        }
                                        isListed
                                    />

                                    {listedPositions.map(
                                        (position) => (
                                            <PortfolioRow
                                                key={
                                                    position.id
                                                }
                                                position={
                                                    position
                                                }
                                            />
                                        ),
                                    )}
                                </>
                            )}

                            {unlistedPositions.length >
                                0 && (
                                    <>
                                        <PortfolioGroupHeader
                                            title="سرمایه‌گذاری‌های غیربورسی"
                                            count={
                                                unlistedPositions.length
                                            }
                                            isListed={false}
                                        />

                                        {unlistedPositions.map(
                                            (position) => (
                                                <PortfolioRow
                                                    key={
                                                        position.id
                                                    }
                                                    position={
                                                        position
                                                    }
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
}: {
    label: string;
    value: number | null;
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
                {formatNumber(value)}
            </div>
        </div>
    );
}

function PortfolioGroupHeader({
    title,
    count,
    isListed,
}: {
    title: string;
    count: number;
    isListed: boolean;
}) {
    return (
        <tr>
            <td
                colSpan={8}
                className={
                    isListed
                        ? "border-y border-emerald-500/20 bg-emerald-500/10 px-3 py-2.5 text-right font-semibold text-emerald-700 dark:text-emerald-300"
                        : "border-y border-amber-500/20 bg-amber-500/10 px-3 py-2.5 text-right font-semibold text-amber-700 dark:text-amber-300"
                }
            >
                {title}

                <span className="mr-2 text-xs font-normal opacity-70">
                    ({count.toLocaleString()} مورد)
                </span>
            </td>
        </tr>
    );
}

function PortfolioRow({
    position,
}: {
    position: PortfolioPosition;
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
                    ? "بورسی"
                    : "غیربورسی"}
            </td>

            <NumberCell
                value={position.ownershipPercent}
            />

            <NumberCell
                value={position.endingQuantity}
            />

            <NumberCell
                value={position.endingCost}
            />

            <NumberCell
                value={position.endingMarketValue}
            />

            <NumberCell
                value={position.endingCostPerShare}
            />

            <NumberCell
                value={position.endingMarketPrice}
            />
        </tr>
    );
}

function NumberCell({
    value,
}: {
    value: number | null;
}) {
    return (
        <td
            dir="ltr"
            className="px-3 py-2 tabular-nums"
        >
            {formatNumber(value)}
        </td>
    );
}

function formatNumber(
    value: number | null,
) {
    return value?.toLocaleString() ?? "—";
}