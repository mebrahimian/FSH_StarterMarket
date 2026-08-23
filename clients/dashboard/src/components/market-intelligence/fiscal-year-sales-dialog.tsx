import {
    useRef,
    useState,
    type PointerEvent as ReactPointerEvent,
} from "react";
import { useTranslation } from "react-i18next";
import type { FiscalYearSales } from "@/api/market-intelligence";

type FiscalYearSalesDialogProps = {
    open: boolean;
    sales: FiscalYearSales | null;
    onClose: () => void;
    onPrevious?: () => void;
    onNext?: () => void;
};

export function FiscalYearSalesDialog({
    open,
    sales,
    onClose,
    onPrevious,
    onNext,
}: FiscalYearSalesDialogProps) {
    const { t: tMarket } = useTranslation("marketIntelligence");

    const [windowOffset, setWindowOffset] =  useState({ x: 0, y: 0 });

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

    if (!open || !sales) {
        return null;
    }

    return (
        <div
            className="text-center fixed left-1/2 top-20 z-50 w-[min(600px,calc(100vw-2rem))] overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-2xl"
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
                
                <div className="text-center font-semibold">
                    {tMarket(
                        "healthCenter.fiscalYearSalesTitle",
                        {
                            symbol: sales.symbol,
                        },
                    )}{" "}
                    <span dir="ltr">
                        {sales.yearEndDate}
                    </span>
                </div>
                <div className="absolute right-3 flex items-center overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] shadow-xs">
                    
                    <button
                        
                        type="button"
                        onPointerDown={(event) =>
                            event.stopPropagation()
                        }
                        onClick={onPrevious}
                        disabled={
                            !sales.previousYearEndDate ||
                            !onPrevious
                        }
                        className="grid size-8 place-items-center text-xl font-medium text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-muted)] disabled:cursor-not-allowed disabled:opacity-30"
                        title="سال مالی قبلی"
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
                            !sales.nextYearEndDate ||
                            !onNext
                        }
                        className="grid size-8 place-items-center text-xl font-medium text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-muted)] disabled:cursor-not-allowed disabled:opacity-30"
                        title="سال مالی بعدی"
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
                    aria-label={tMarket(
                        "actions.close",
                    )}
                    title={tMarket(
                        "actions.close",
                    )}
                >
                    ×
                </button>
            </div>

            <div className="max-h-[70vh] overflow-auto p-4">
                <table className="w-full border-collapse text-center text-sm">
                    <thead>
                        <tr className="border-b text-xs font-semibold text-[var(--color-muted-foreground)]">
                            <th className="px-3 py-2">
                                {tMarket(
                                    "salesTable.period",
                                )}
                            </th>

                            <th className="px-3 py-2">
                                {tMarket(
                                    "salesTable.monthSales",
                                )}
                            </th>

                            <th className="px-3 py-2">
                                {tMarket(
                                    "salesTable.yearToDate",
                                )}
                            </th>

                            <th className="px-3 py-2">
                                {tMarket(
                                    "salesTable.previousYearToDate",
                                )}
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        {sales.rows.map((row) => (
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
    );
}