
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { LockKeyhole, Plus } from "lucide-react";

export type SubscriptionSelectedSymbol = {
    companyId: number;
    symbol: string;
    companyName?: string | null;
};

type SubscriptionSymbolSlotsProps = {
    title: string;
    description: string;
    limit: number;
    selected: SubscriptionSelectedSymbol[];
    onAdd?: () => void;
    loading?: boolean;
    systemFixed?: boolean;
};

export function SubscriptionSymbolSlots({
    title,
    description,
    limit,
    selected,
    onAdd,
    loading = false,
    systemFixed = false,
}: SubscriptionSymbolSlotsProps) {
    const { t, i18n } = useTranslation("marketIntelligence");

    const formatter = useMemo(
        () => new Intl.NumberFormat(
            i18n.resolvedLanguage?.startsWith("fa")
                ? "fa-IR"
                : "en-US"
        ),
        [i18n.resolvedLanguage]
    );

    const unlimited = limit === -1;
    const count = selected.length;
    const remaining = unlimited
        ? 0
        : Math.max(limit - count, 0);

    const canAdd =
        !systemFixed &&
        !unlimited &&
        limit > 0 &&
        remaining > 0 &&
        !!onAdd &&
        !loading;

    const slots = useMemo(
        () => Array.from(
            {
                length: Math.max(
                    0,
                    limit,
                    selected.length
                ),
            },
            (_, i) => selected[i] ?? null
        ),
        [limit, selected]
    );

    const statusText = unlimited
        ? t("subscription.symbolSlots.unlimited")
        : systemFixed
            ? t("subscription.symbolSlots.fixedCount", {
                count: formatter.format(count),
            })
            : t("subscription.symbolSlots.remaining", {
                count: formatter.format(remaining),
            });

    return (
        <section
            dir={i18n.dir()}
            className="rounded-xl border border-border/70 bg-card p-3"
        >
            <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                <div className="min-w-0">
                    <h3 className="text-sm font-semibold">
                        {title}
                    </h3>
                    <p className="mt-1 text-xs text-muted-foreground">
                        {description}
                    </p>
                </div>

                <span
                    className="rounded-lg border border-border/70 bg-muted/40 px-2 py-1 text-xs text-muted-foreground"
                    aria-live="polite"
                >
                    {statusText}
                </span>
            </div>

            {unlimited ? (
                <div className="rounded-lg border border-border/70 bg-muted/30 px-3 py-3 text-center text-sm">
                    {t("subscription.symbolSlots.allAvailable")}
                </div>
            ) : limit === 0 ? (
                <div className="rounded-lg border border-border/70 bg-muted/30 px-3 py-3 text-center text-sm text-muted-foreground">
                    {t("subscription.symbolSlots.unavailable")}
                </div>
            ) : (
                <div className="grid grid-cols-2 overflow-hidden rounded-xl border border-border/70 bg-background sm:grid-cols-3 lg:grid-cols-5">
                    {slots.map((symbol, index) =>
                        symbol ? (
                            <div
                                key={`selected-${symbol.companyId}`}
                                title={
                                    symbol.companyName ??
                                    symbol.symbol
                                }
                                className="flex min-h-12 items-center justify-between gap-2
                                           border-b border-e border-border/70
                                           bg-emerald-100/60 px-3 py-2 text-xs
                                           shadow-none
                                           dark:bg-emerald-950/40"
                            >
                                <span className="truncate font-semibold">
                                    {symbol.symbol}
                                </span>

                                <LockKeyhole
                                    className="size-3.5 shrink-0 text-emerald-600"
                                    aria-hidden="true"
                                />

                                <span className="sr-only">
                                    {t("subscription.symbolSlots.locked")}
                                </span>
                            </div>
                        ) : (
                            <button
                                key={`empty-${index}`}
                                type="button"
                                disabled={!canAdd}
                                onClick={onAdd}
                                aria-label={t(
                                    "subscription.symbolSlots.chooseSlot",
                                    {
                                        index: formatter.format(index + 1),
                                    }
                                )}
                                className="flex min-h-12 items-center justify-center gap-1.5
                                           border-b border-e border-border/70
                                           bg-gradient-to-b from-white via-slate-50 to-slate-200
                                           px-2 py-2 text-xs text-foreground
                                           shadow-[inset_0_2px_1px_rgba(255,255,255,0.9),inset_0_-3px_0_rgba(100,116,139,0.25),0_2px_3px_rgba(15,23,42,0.12)]
                                           transition-[transform,box-shadow,background-color]
                                           duration-150
                                           hover:from-white hover:to-sky-100
                                           active:translate-y-[2px]
                                           active:shadow-none
                                           focus-visible:outline-none
                                           focus-visible:ring-2
                                           focus-visible:ring-inset
                                           focus-visible:ring-primary
                                           disabled:cursor-not-allowed
                                           disabled:opacity-50
                                           dark:from-slate-700
                                           dark:via-slate-800
                                           dark:to-slate-900
                                           dark:hover:to-slate-700"
                            >
                                <Plus
                                    className="size-3.5 shrink-0"
                                    aria-hidden="true"
                                />

                                <span className="truncate">
                                    {t("subscription.symbolSlots.choose")}
                                </span>

                                <span className="text-[10px] text-muted-foreground">
                                    {formatter.format(index + 1)}
                                </span>
                            </button>
                        )
                    )}
                </div>
            )}

            {!unlimited && !systemFixed && count > 0 && (
                <p className="mt-2 text-xs text-muted-foreground">
                    {t("subscription.symbolSlots.permanentNotice")}
                </p>
            )}
        </section>
    );
}
