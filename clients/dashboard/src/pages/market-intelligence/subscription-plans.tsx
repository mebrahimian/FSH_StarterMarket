import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
    getSubscriptionPlans,
    type SubscriptionPlan,
    createUserPaymentRequest,
} from "@/api/market-intelligence";

const durations = [1, 3, 6, 12] as const;

const planOrder = ["economic", "professional", "premium"];

type SubscriptionAction = "purchase" | "renewal" | "upgrade";

export function SubscriptionPlans({
    mode = "purchase",
}: {
    mode?: SubscriptionAction;
}) {
    const { t, i18n } = useTranslation("marketIntelligence");
    const [duration, setDuration] = useState<number>(1);
    const queryClient = useQueryClient();
const [confirmPlanKey, setConfirmPlanKey] = useState<string | null>(null);

const purchaseMutation = useMutation({
    mutationFn: createUserPaymentRequest,
    onSuccess: async () => {
        setConfirmPlanKey(null);

        await queryClient.invalidateQueries({
              queryKey: ["market-intelligence", "payment-request", "pending"],
             });
    },
});
const { data, isLoading, isError } = useQuery({
        queryKey: ["market-intelligence", "subscription", "plans"],
        queryFn: getSubscriptionPlans,
    });

const locale = i18n.language.startsWith("fa")
        ? "fa-IR"
        : "en-US";

const formatNumber = (value: number) =>
        new Intl.NumberFormat(locale).format(value);

const plans = [...(data ?? [])]
        .filter((plan) => planOrder.includes(plan.planKey))
        .sort(
            (a, b) =>
                planOrder.indexOf(a.planKey) -
                planOrder.indexOf(b.planKey),
        );

    return (
        <section className="space-y-4">
            <h2 className="text-lg font-semibold">
                {t("subscription.availablePlans")}
            </h2>

            <div className="flex flex-wrap gap-2">
                {durations.map((months) => (
                    <button
                        key={months}
                        type="button"
                        onClick={() => setDuration(months)}
                        className={`rounded-lg border px-4 py-2 text-sm ${
                            duration === months
                                ? "border-primary bg-primary/10 font-semibold"
                                : "border-border"
                        }`}
                    >
                        {t("subscription.durationMonths", {
                            count: months,
                        })}
                    </button>
                ))}
            </div>

            {isLoading ? (
                <p>{t("subscription.loading")}</p>
            ) : isError ? (
                <p>{t("subscription.loadError")}</p>
            ) : (
                <div className="grid gap-4 md:grid-cols-3">
                    {plans.map((plan: SubscriptionPlan) => {
                        const term = plan.terms.find(
                            (x) => x.durationMonths === duration,
                        );

                        if (!term) return null;

                        const priceInToman =
                            plan.currency === "IRR"
                                ? term.finalPrice / 10
                                : term.finalPrice;

                        return (
                            <div
                                key={plan.planKey}
                                className="space-y-4 rounded-xl border border-border p-5"
                            >
                                <div className="flex items-center gap-2">
                                   <img  src={`/branding/sadaf2/${plan.planKey === "economic"
                                       ? "sadaf-bronze-medal-red-ribbon.png"
                                       : plan.planKey === "professional"
                                       ? "sadaf-silver-medal-red-ribbon.png"
                                       : "sadaf-gold-medal-red-ribbon.png"
                                             }`}
                                          alt=""  className="h-12 w-12 object-contain"
                                   />
                                   <h3 className="font-semibold">
                                       {t(`subscription.plans.${plan.planKey}`)}
                                   </h3>
                                </div>

                                <div className="text-2xl font-bold tabular-nums">
                                    {formatNumber(priceInToman)}
                                    <span className="ms-2 text-sm font-normal">
                                        {t("subscription.toman")}
                                    </span>
                                </div>

                                {term.discountPercent > 0 && (
                                    <p className="text-sm text-green-600">
                                        {formatNumber(term.discountPercent)}
                                        ٪ {t("subscription.discount")}
                                    </p>
                                )}

                                <p className="text-xs text-muted-foreground">
                                    {t("subscription.durationMonths", {
                                        count: duration,
                                    })}
                                </p>

                                {confirmPlanKey === plan.planKey ? (
                                <div className="space-y-3 border-t pt-4">
                                    <p className="text-sm">
                                        آیا درخواست خرید این اشتراک با مبلغ{" "}
                                        <strong>{formatNumber(priceInToman)} تومان</strong>
                                        {" "}برای مدت {duration} ماه ثبت شود؟
                                    </p>

                                    <div className="flex gap-2">
                                        <button
                                            type="button"
                                            disabled={purchaseMutation.isPending}
                                            onClick={() =>
                                                purchaseMutation.mutate({
                                                    planKey: plan.planKey,
                                                    durationMonths: duration,
                                                    requestType: mode === "renewal" ? 1 :
                                                                 mode === "upgrade" ? 2 : 0, })
                                                    }
                                            className="rounded-lg bg-primary px-4 py-2 text-sm text-primary-foreground disabled:opacity-50"
                                        >
                                            {purchaseMutation.isPending
                                                ? "در حال ثبت..."
                                                : "تأیید و ثبت درخواست"}
                                        </button>

                                        <button
                                            type="button"
                                            disabled={purchaseMutation.isPending}
                                            onClick={() => setConfirmPlanKey(null)}
                                            className="rounded-lg border px-4 py-2 text-sm"
                                        >
                                            انصراف
                                        </button>
                                    </div>
                                </div>
                                ) : (
                                    <button
                                        type="button"
                                        onClick={() => {
                                            purchaseMutation.reset();
                                            setConfirmPlanKey(plan.planKey); }}
                                        className="w-full rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
                                    >
                                        خرید اشتراک
                                    </button>
                                )}
                                
                                {purchaseMutation.isError &&
                                    confirmPlanKey === plan.planKey && (
                                        <p role="alert" className="text-sm text-destructive">
                                            ثبت درخواست خرید انجام نشد. ممکن است درخواست در انتظار
                                            یا اشتراک فعال داشته باشید.
                                        </p>
                                    )}
                            </div>
                          );
                     })}
                </div>
            )}
        </section>
    );
    }
                                
                                
                                
                                
                                
                                