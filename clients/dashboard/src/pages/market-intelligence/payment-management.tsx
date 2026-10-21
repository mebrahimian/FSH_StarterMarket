import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";import {
    getAdminPaymentRequests,
    getPaymentReceiptUrl,
    approveUserPayment,
    type AdminPaymentRequest,
    type PaymentRequestStatus,
} from "@/api/market-intelligence";
import { Badge } from "@/components/ui/badge";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogBody,
} from "@/components/ui/dialog";

const statusLabels: Record<PaymentRequestStatus, string> = {
    Pending: "در انتظار",
    Approved: "تأییدشده",
    Rejected: "ردشده",
    Cancelled: "لغو شده"
};
const statusVariants = {
    Pending: "warning",
    Approved: "success",
    Rejected: "danger",
    Cancelled: "warning",
} as const;

function formatDate(value: string) {
    return new Date(value).toLocaleString("fa-IR");
}

export function PaymentManagementPage() {
    const [status, setStatus] =
        useState<PaymentRequestStatus | "all">("all");
    const { t } = useTranslation("marketIntelligence");

    const [pageNumber, setPageNumber] = useState(1);
    const [selected, setSelected] = useState<AdminPaymentRequest | null>(null);
    const [paidAmountInput, setPaidAmountInput] = useState<string>("");


    const pageSize = 20;

    const { data, isLoading, isError } = useQuery({
        queryKey: ["admin-payment-requests", status, pageNumber],
        queryFn: () =>
            getAdminPaymentRequests(
                status === "all" ? undefined : status,
                pageNumber,
                pageSize
            ),
    });

    const {
    data: receiptData,
    isLoading: receiptLoading,
    isError: receiptError,
} = useQuery({
    queryKey: ["payment-receipt-url", selected?.receiptFileId],
    queryFn: () => getPaymentReceiptUrl(selected!.receiptFileId!),
    enabled: !!selected?.receiptFileId,
    staleTime: 0,
    refetchOnWindowFocus: false,
});

    const receiptUrl = receiptData?.url
       ? new URL(receiptData.url, import.meta.env.VITE_API_BASE_URL).href
       : null;

    const queryClient = useQueryClient();
    const [confirmApprove, setConfirmApprove] = useState(false);
    const approveMutation = useMutation({
       mutationFn: approveUserPayment,
       onSuccess: async () => {
        setConfirmApprove(false);
        setSelected(null);

        await queryClient.invalidateQueries({
            queryKey: ["admin-payment-requests"],
        });
    },
});

    const totalPages = Math.max(
        1,
        Math.ceil((data?.totalCount ?? 0) / pageSize)
    );

    return (
        <div className="space-y-5 p-6" dir="rtl">
            <div>
                <h1 className="text-2xl font-semibold">
                    مدیریت پرداخت‌ها
                </h1>
                <p className="mt-1 text-sm text-muted-foreground">
                    بررسی درخواست‌های خرید اشتراک کاربران
                </p>
            </div>

            <div className="grid gap-4 lg:grid-cols-[1fr_230px]">
                <section className="min-w-0 overflow-hidden rounded-xl border bg-card">
                    <div className="border-b p-4">
                        <h2 className="font-semibold">
                            فهرست درخواست‌ها
                        </h2>
                        <p className="text-sm text-muted-foreground">
                            تعداد: {data?.totalCount ?? 0}
                        </p>
                    </div>

                    {isLoading && (
                        <p className="p-5 text-sm">
                            در حال دریافت پرداخت‌ها...
                        </p>
                    )}

                    {isError && (
                        <p className="p-5 text-sm text-destructive">
                            خطا در دریافت پرداخت‌ها
                        </p>
                    )}

                    {data && (
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[850px] text-sm">
                                <thead className="bg-muted/50 text-muted-foreground">
                                    <tr className="border-b">
                                        <th className="p-3 text-right">تاریخ</th>
                                        <th className="p-3 text-right">کاربر</th>
                                        <th className="p-3 text-right">پلن</th>
                                        <th className="p-3 text-right">مدت</th>
                                        <th className="p-3 text-right">مبلغ</th>
                                        <th className="p-3 text-right">وضعیت</th>
                                        <th className="p-3 text-right">رسید</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {data.items.map((payment) => (
                                        <tr key={payment.id}
                                            onClick={() => { setSelected(payment);
                                                             setPaidAmountInput(String(payment.amount));
                                                           }                                                           
                                                    }
                                            className="cursor-pointer border-b transition-colors hover:bg-muted/40"
                                        >
                                            <td className="p-3 whitespace-nowrap">
                                                {formatDate(payment.createdAtUtc)}
                                            </td>
                                            <td className="p-3" dir="ltr">
                                                <span className="font-mono text-xs">
                                                    {payment.userId.slice(0, 8)}…
                                                </span>
                                            </td>
                                            <td className="p-3">{payment.planKey}</td>
                                            <td className="p-3">
                                                {payment.durationMonths} ماه
                                            </td>
                                            <td className="p-3 whitespace-nowrap">
                                                {payment.amount.toLocaleString("fa-IR")} ریال
                                            </td>
                                            <td className="p-3">
                                                <Badge variant={statusVariants[payment.status]}>
                                                    {statusLabels[payment.status]}
                                                </Badge>
                                            </td>
                                            <td className="p-3">
                                                {payment.receiptFileId ? "دارد" : "ندارد"}
                                            </td>
                                        </tr>
                                    ))}

                                    {data.items.length === 0 && (
                                        <tr>
                                            <td colSpan={7} className="p-8 text-center text-muted-foreground">
                                                درخواستی یافت نشد.
                                            </td>
                                        </tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                    )}

                    <div className="flex items-center justify-between border-t p-4">
                        <button
                            type="button"
                            disabled={pageNumber <= 1}
                            onClick={() => setPageNumber((p) => p - 1)}
                            className="rounded-lg border px-3 py-2 text-sm disabled:opacity-40"
                        >
                            قبلی
                        </button>

                        <span className="text-sm text-muted-foreground">
                            صفحه {pageNumber} از {totalPages}
                        </span>

                        <button
                            type="button"
                            disabled={pageNumber >= totalPages}
                            onClick={() => setPageNumber((p) => p + 1)}
                            className="rounded-lg border px-3 py-2 text-sm disabled:opacity-40"
                        >
                            بعدی
                        </button>
                    </div>
                </section>

                <aside className="h-fit rounded-xl border bg-card p-4">
                    <h2 className="mb-4 font-semibold">فیلترها</h2>

                    <label
                        htmlFor="payment-status"
                        className="mb-2 block text-sm text-muted-foreground"
                    >
                        وضعیت پرداخت
                    </label>

                    <select
                        id="payment-status"
                        value={status}
                        onChange={(event) => {
                            setStatus(event.target.value as PaymentRequestStatus | "all");
                            setPageNumber(1);
                        }}
                        className="w-full rounded-lg border bg-background p-2 text-sm"
                    >
                        <option value="all">همه وضعیت‌ها</option>
                        <option value="Pending">در انتظار</option>
                        <option value="Approved">تأییدشده</option>
                        <option value="Rejected">ردشده</option>
                    </select>
                </aside>
            </div>

            <Dialog open={selected !== null}
                    onOpenChange={(open) => {
                   if (!open && !approveMutation.isPending)                    
                   {
                       setSelected(null);
                       setConfirmApprove(false);
                       approveMutation.reset();
                   }
                }}
            >
                <DialogContent dir="rtl" className="flex max-h-[90dvh] flex-col overflow-hidden">
                    <DialogHeader className="text-right">
                        <DialogTitle>جزئیات درخواست پرداخت</DialogTitle>
                    </DialogHeader>

                    {selected && (
                        <DialogBody className="min-h-0 flex-1 space-y-3 overflow-y-auto text-sm">
                            <div>شناسه کاربر: {selected.userId}</div>
                            <div>پلن: {selected.planKey}</div>
                            <div>مدت: {selected.durationMonths} ماه</div>
                            <div>
                                مبلغ: {selected.amount.toLocaleString("fa-IR")} ریال
                            </div>
                            <div>
                                وضعیت: {statusLabels[selected.status]}
                            </div>
                            <div>
                                تاریخ درخواست: {formatDate(selected.createdAtUtc)}
                            </div>
                            <div>
                                رسید: {selected.receiptFileId ? "ثبت شده" : "ثبت نشده"}
                            </div>
                            {selected.status === "Pending" && selected.receiptFileId && (
                            <div className="border-t pt-4">
                                 <div className="mb-4 space-y-2">
                                     <label
                                         htmlFor="paidAmount"
                                         className="block text-sm font-medium"
                                     >
                                         {t("subscription.actualPaidAmount")}
                                     </label>

                                     <input
                                         id="paidAmount"
                                         type="number"
                                         min="0"
                                         step="1"
                                         value={paidAmountInput}
                                         onChange={(e) => setPaidAmountInput(e.target.value)}
                                         className="w-full rounded-lg border bg-background px-3 py-2 text-sm"
                                         dir="ltr"
                                     />
                                 </div>
                                 {approveMutation.isError && (
                                 <p className="mb-3 text-sm text-destructive">
                                    تأیید پرداخت انجام نشد. لطفاً خطا را بررسی کنید              .
                                 </p>
                                 )}
                                 {!confirmApprove ? (
                                 <button
                                    type="button"
                                    onClick={() => setConfirmApprove(true)}
                                    className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700"
                                 >
                                     تأیید پرداخت
                                 </button>
                                 ) : (
                                 <div className="space-y-3 rounded-lg border p-3">
                                         <p>آیا از تأیید این پرداخت و فعال‌سازی اشتراک مطمئن هستید؟</p>

                                         <div className="flex gap-2">
                                         <button type="button"                                         
                                                 disabled={ approveMutation.isPending ||
                                                            paidAmountInput.trim() === "" ||
                                                            !Number.isFinite(Number(paidAmountInput)) ||
                                                            Number(paidAmountInput) < 0                                                            
                                                          }
                                                 onClick={() => approveMutation.mutate({
                                                           paymentRequestId: selected.id,
                                                           paidAmount: Number(paidAmountInput),
                                                            })                                                            
                                                         }
                                                 className="rounded-lg bg-emerald-600 px-4 py-2 text-white disabled:opacity-50"
                                         >
                                         {approveMutation.isPending
                                             ? "در حال تأیید..."
                                             : "بله، تأیید شود"}
                                         </button>

                                         <button type="button" disabled={approveMutation.isPending}
                                                 onClick={() => setConfirmApprove(false)}
                                                 className="rounded-lg border px-4 py-2"
                                         >
                                                انصراف
                                         </button>
                                 </div>
                            </div>
                            )}
                            </div>
                            )}
                            {selected.receiptFileId && (
                            <div className="space-y-2 border-t pt-4">
                               <p className="font-medium">تصویر رسید پرداخت</p>
                               {receiptLoading && <p>در حال دریافت رسید...</p>}
                               {receiptError && (
                                  <p className="text-destructive">
                                      دریافت رسید امکان‌پذیر نیست.
                                  </p>
                               )}
                               {receiptUrl && (
                               <div className="space-y-2">
                                  <a
                                     href={receiptUrl}
                                     target="_blank"
                                     rel="noopener noreferrer"
                                     className="text-sm text-primary underline"
                                  >
                                     مشاهده رسید در صفحه جدید
                                  </a>

                                  <img
                                     src={receiptUrl}
                                     alt="رسید پرداخت"
                                     className="max-h-80 w-full rounded-lg border object-contain"
                                  />
                               </div>
                               )}
                            </div>
                            )}
                            {selected.reviewedAtUtc && (
                                <div>
                                    تاریخ بررسی: {formatDate(selected.reviewedAtUtc)}
                                </div>
                            )}

                            
                        </DialogBody>
                    )}
                </DialogContent>
            </Dialog>
        </div>
    );
}