import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { forwardRef } from "react";
import { CreditCard, type LucideProps } from "lucide-react";
import { useState } from "react";

import {
    getMyMarketSubscription,
    getMyPendingPaymentRequest,
    attachPaymentReceipt,
    cancelUserPaymentRequest,
} from "@/api/market-intelligence";
import {
    EntityPageHeader,
    EntityDetailSection,
} from "@/components/list";
import { SubscriptionPlans } from "./subscription-plans";
import { FileDropzone } from "@/components/file/file-dropzone";

export function MarketSubscriptionPage() {
    const { t, i18n } = useTranslation("marketIntelligence");
    const queryClient = useQueryClient();
    const [subscriptionTab, setSubscriptionTab] = useState<"renew" | "upgrade">("renew");
    const { data, isLoading, isError } = useQuery({
        queryKey: ["market-intelligence", "subscription", "me"],
        queryFn: getMyMarketSubscription,
    });

    const numberFormatter = new Intl.NumberFormat(
        i18n.language.startsWith("fa") ? "fa-IR" : "en-US"
    );
    const {
    data: pendingPayment,
    isLoading: pendingLoading,
    isError: pendingError,
} = useQuery({
    queryKey: ["market-intelligence", "payment-request", "pending"],
    queryFn: getMyPendingPaymentRequest,
});

    const cancelMutation = useMutation({
    mutationFn: (input: {
        paymentRequestId: string;
        confirmedNoPayment: boolean;
    }) =>
        cancelUserPaymentRequest(
            input.paymentRequestId,
            input.confirmedNoPayment
        ),

    onSuccess: async () => {
        await queryClient.invalidateQueries({
            queryKey: ["market-intelligence", "payment-request", "pending"],
        });
    },
});
    const hasActivePaidSubscription =
    !!data &&
    !data.isFree &&
    (!data.endUtc || new Date(data.endUtc).getTime() > Date.now());

const canPurchase =
    !!data &&
    !isLoading &&
    !isError &&
    !hasActivePaidSubscription &&
    !pendingLoading &&
    !pendingError &&
    !pendingPayment;
    
    const CurrentPlanIcon = forwardRef<SVGSVGElement, Omit<LucideProps, "ref">>    
        (({ className, ...props }, ref) => {
    const medalFile =
        data?.isFree ? null :
        data?.planKey === "economic"
            ? "sadaf-bronze-medal-red-ribbon.png"
            : data?.planKey === "professional"
                ? "sadaf-silver-medal-red-ribbon.png"
                : data?.planKey === "premium"
                    ? "sadaf-gold-medal-red-ribbon.png"
                    : null;

    if (!medalFile) {
        return <CreditCard ref={ref} className={className} {...props} />;
    }

    return (
        <svg
            {...props}
            ref={ref}
            className={`${className ?? ""} !h-10 !w-10 shrink-0`}
            viewBox="0 0 48 48"
        >
            <image
                href={`/branding/sadaf2/${medalFile}`}
                width="48"
                height="48"
            />
        </svg>
      );
    });

    
return (
    <div className="space-y-6">
        <EntityPageHeader
            icon={CreditCard}
            title={t("subscription.title")}
            description={t("subscription.description")}
        />

        <EntityDetailSection
            title={t("subscription.currentPlan")}
            icon={CurrentPlanIcon}
        >
            {isLoading ? (
                <p>{t("subscription.loading")}</p>
            ) : isError || !data ? (
                <p>{t("subscription.loadError")}</p>
            ) : (
                <div
                    dir={i18n.language.startsWith("fa") ? "rtl" : "ltr"}
                    className="grid gap-6 lg:grid-cols-2"
                >
                    <div className="space-y-3">
                        <div>
                            {t("subscription.plan")}:{" "}
                            {t(`subscription.plans.${data.planKey}`, {
                                defaultValue: data.planKey,
                            })}
                        </div>

                        <div>
                            {t("subscription.selectedSymbols")}:{" "}
                            {numberFormatter.format(data.selectedSymbolsCount)}
                        </div>

                        <div>
                            {t("subscription.maxSelectedSymbols")}:{" "}
                            {data.maxSelectedSymbols === -1
                                ? t("subscription.unlimited")
                                : numberFormatter.format(data.maxSelectedSymbols)}
                        </div>

                        <div>
                            {t("subscription.maxSymbolsPerIndustry")}:{" "}
                            {data.maxSymbolsPerIndustry === -1
                                ? t("subscription.unlimited")
                                : numberFormatter.format(data.maxSymbolsPerIndustry)}
                        </div>
                    </div>

                    {hasActivePaidSubscription && (
                        <div className="space-y-4">
                            <div
                                className="flex flex-wrap gap-2"
                                role="tablist"
                            >
                                <button
                                    type="button"
                                    role="tab"
                                    aria-selected={subscriptionTab === "renew"}
                                    onClick={() => setSubscriptionTab("renew")}
                                    className={`rounded-lg border px-4 py-2 text-sm ${
                                        subscriptionTab === "renew"
                                            ? "border-primary bg-primary/10 font-semibold"
                                            : "border-border"
                                    }`}
                                >
                                    {t("subscription.renewTab")}
                                </button>

                                <button
                                    type="button"
                                    role="tab"
                                    aria-selected={subscriptionTab === "upgrade"}
                                    onClick={() => setSubscriptionTab("upgrade")}
                                    className={`rounded-lg border px-4 py-2 text-sm ${
                                        subscriptionTab === "upgrade"
                                            ? "border-primary bg-primary/10 font-semibold"
                                            : "border-border"
                                    }`}
                                >
                                    {t("subscription.upgradeTab")}
                                </button>
                            </div>

                            <div
                                role="tabpanel"
                                className="space-y-4"
                            >
                                <p className="text-sm">
                                    {subscriptionTab === "renew"
                                        ? t("subscription.renewDescription")
                                        : t("subscription.upgradeDescription")}
                                </p>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </EntityDetailSection>

        {hasActivePaidSubscription &&
            !pendingLoading &&
            !pendingError &&
            !pendingPayment && (
                <SubscriptionPlans
                    mode={
                        subscriptionTab === "renew"
                            ? "renewal"
                            : "upgrade"
                    }
                />
            )}

        {pendingPayment && (
            <EntityDetailSection
                title="درخواست پرداخت در انتظار"
                icon={CreditCard}
            >
                <div className="space-y-3">
                    <div>
                        پلن: {pendingPayment.planKey}
                    </div>

                    <div>
                        مدت: {pendingPayment.durationMonths} ماه
                    </div>

                    <div>
                        مبلغ قابل پرداخت:{" "}
                        {numberFormatter.format(pendingPayment.amount)} ریال
                    </div>

                    <div>
                        وضعیت رسید:{" "}
                        {pendingPayment.amount === 0
                            ? "ارتقای اعتباری — بدون نیاز به رسید"
                            : pendingPayment.hasReceipt
                                ? "رسید ثبت شده — در انتظار تأیید مدیر"
                                : "در انتظار بارگذاری رسید"}
                    </div>

                    {!pendingPayment.hasReceipt &&
                        pendingPayment.amount > 0 && (
                            <FileDropzone
                                options={{
                                    ownerType: "UserPaymentRequest",
                                    ownerId: pendingPayment.id,
                                    category: (file) =>
                                        file.name.toLowerCase().endsWith(".pdf")
                                            ? "Document"
                                            : "Image",
                                    visibility: "Private",
                                    allowedExtensions: [
                                        ".pdf",
                                        ".jpg",
                                        ".jpeg",
                                        ".png",
                                    ],
                                }}
                                accept=".pdf,.jpg,.jpeg,.png"
                                onUploaded={async (asset) => {
                                    await attachPaymentReceipt(
                                        pendingPayment.id,
                                        {
                                            receiptFileId: asset.id,
                                            bankReferenceNumber: null,
                                        }
                                    );

                                    await queryClient.invalidateQueries({
                                        queryKey: [
                                            "market-intelligence",
                                            "payment-request",
                                            "pending",
                                        ],
                                    });
                                }}
                            />
                        )}

                    {/* لغو درخواست فقط در صورت نداشتن رسید */}
                    {!pendingPayment.hasReceipt && (
                        <div className="space-y-2 border-t pt-3">
                            <button
                                type="button"
                                disabled={cancelMutation.isPending}
                                className="rounded-lg border border-destructive px-4 py-2 text-sm text-destructive disabled:opacity-50"
                                onClick={() => {
                                    const confirmed = window.confirm(
                                        pendingPayment.amount > 0
                                            ? "تأیید می‌کنم هیچ وجهی واریز نکرده‌ام و می‌خواهم این درخواست را لغو کنم."
                                            : "آیا از لغو این درخواست مطمئن هستید؟"
                                    );

                                    if (!confirmed) return;

                                    cancelMutation.mutate({
                                        paymentRequestId: pendingPayment.id,
                                        confirmedNoPayment:
                                            pendingPayment.amount > 0,
                                    });
                                }}
                            >
                                {cancelMutation.isPending
                                    ? "در حال لغو..."
                                    : "لغو درخواست"}
                            </button>

                            {cancelMutation.isError && (
                                <p className="text-sm text-destructive">
                                    لغو درخواست انجام نشد. لطفاً دوباره تلاش کنید.
                                </p>
                            )}
                        </div>
                    )}
                </div>
            </EntityDetailSection>
        )}

        {canPurchase && <SubscriptionPlans />}
    </div>
);

}