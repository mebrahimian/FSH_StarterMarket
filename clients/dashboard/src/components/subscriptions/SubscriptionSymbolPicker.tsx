import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Check, LockKeyhole, Search, X } from "lucide-react";
import {
    searchSubscriptionCompanies,
    selectSubscriptionSymbol,
    type SubscriptionCompany,
    type SubscriptionSymbolPurpose,
} from "@/api/market-intelligence";

type SubscriptionSymbolPickerProps = {
    open: boolean;
    purpose: SubscriptionSymbolPurpose;
    selectedCompanyIds: number[];
    onClose: () => void;
    onSelected: () => void | Promise<void>;
};

export function SubscriptionSymbolPicker({
    open,
    purpose,
    selectedCompanyIds,
    onClose,
    onSelected,
}: SubscriptionSymbolPickerProps) {
    const [search, setSearch] = useState("");
    const [candidate, setCandidate] = useState<SubscriptionCompany | null>(null);
    const [debouncedSearch, setDebouncedSearch] = useState("");

    useEffect(() => {
        if (!open) {
            setSearch("");
            setCandidate(null);
            setDebouncedSearch("");
            return;
        }
        const timer = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
        return () => window.clearTimeout(timer);
    }, [open, search]);

    const canSearch = open && debouncedSearch.length >= 2;
    const companiesQuery = useQuery({
        queryKey: ["market-intelligence", "subscriptions", "companies", purpose, debouncedSearch],
        queryFn: () => searchSubscriptionCompanies(debouncedSearch, purpose),
        enabled: canSearch,
        staleTime: 60_000,
    });

    const selectMutation = useMutation({
        mutationFn: (companyId: number) =>
            selectSubscriptionSymbol({ companyId, purpose }),
        onSuccess: async () => {
            await onSelected();
            onClose();
        },
    });

    useEffect(() => {
        if (!open || selectMutation.isPending) return;
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") onClose();
        };
        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [open, onClose, selectMutation.isPending]);

    if (!open) return null;

    const results = companiesQuery.data ?? [];
    const isAlreadySelected = (id: number) => selectedCompanyIds.includes(id);

    return (
        <div dir="rtl" className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-3 sm:p-6">
            <div role="dialog" aria-modal="true" aria-labelledby="subscription-picker-title"
                className="flex w-full max-w-xl flex-col overflow-hidden rounded-2xl border bg-background shadow-2xl">
                <div className="flex items-start justify-between gap-3 border-b p-5">
                    <div>
                        <h2 id="subscription-picker-title" className="text-lg font-semibold">انتخاب سهم</h2>
                        <p className="mt-1 text-xs text-muted-foreground">
                            {purpose === 2 ? "ارزش‌گذاری پرتفوی" : "دسترسی به اطلاعات جدید"}
                            {" · "}هر سهم پس از ثبت برای همیشه قفل می‌شود.
                        </p>
                    </div>
                    <button type="button" aria-label="بستن" onClick={onClose}
                        disabled={selectMutation.isPending}
                        className="rounded-lg p-2 hover:bg-muted disabled:opacity-40">
                        <X className="size-5" />
                    </button>
                </div>

                <div className="space-y-3 p-5">
                    <label className="relative block">
                        <Search className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                        <input
                            autoFocus
                            value={search}
                            onChange={(event) => { setSearch(event.target.value); setCandidate(null); selectMutation.reset(); }}
                            placeholder="نام یا نماد شرکت را جست‌وجو کنید..."
                            disabled={selectMutation.isPending}
                            className="w-full rounded-xl border bg-background py-3 pe-3 ps-3 pr-10 text-sm outline-none focus-visible:ring-2 focus-visible:ring-primary"
                        />
                    </label>

                    <div className="max-h-64 min-h-32 space-y-1 overflow-y-auto rounded-xl border p-2">
                        {debouncedSearch.length < 2 ? (
                            <p className="p-4 text-center text-sm text-muted-foreground">حداقل دو حرف وارد کنید.</p>
                        ) : companiesQuery.isLoading || companiesQuery.isFetching ? (
                            <p className="p-4 text-center text-sm text-muted-foreground">در حال جست‌وجو...</p>
                        ) : companiesQuery.isError ? (
                            <p className="p-4 text-center text-sm text-destructive">جست‌وجو ناموفق بود. دوباره تلاش کنید.</p>
                        ) : results.length === 0 ? (
                            <p className="p-4 text-center text-sm text-muted-foreground">نمادی یافت نشد.</p>
                        ) : (
                            results.map((company) => {
                                const alreadySelected = isAlreadySelected(company.companyId);
                                const isCandidate = candidate?.companyId === company.companyId;
                                return (
                                    <button type="button" key={company.companyId}
                                        disabled={alreadySelected || selectMutation.isPending}
                                        onClick={() => { setCandidate(company); selectMutation.reset(); }}
                                        className={`flex w-full items-center justify-between gap-3 rounded-lg p-3 text-right transition-colors disabled:opacity-50 ${isCandidate ? "bg-primary/10 ring-1 ring-primary" : "hover:bg-muted"}`}>
                                        <span className="min-w-0">
                                            <span className="block font-semibold">{company.symbol}</span>
                                            <span className="block truncate text-xs text-muted-foreground">{company.companyName}</span>
                                        </span>
                                        {alreadySelected ? <span className="shrink-0 text-xs text-muted-foreground">قبلاً انتخاب شده</span> : isCandidate ? <Check className="size-5 text-primary" /> : null}
                                    </button>
                                );
                            })
                        )}
                    </div>

                    {candidate && (
                        <div className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-100">
                            <div className="flex items-center gap-2 font-semibold">
                                <LockKeyhole className="size-4" />
                                تأیید انتخاب دائمی {candidate.symbol}
                            </div>
                            <p className="mt-2 text-xs leading-6">
                                پس از ثبت، این سهم قابل حذف یا تعویض نیست و یک جایگاه از سهمیه شما مصرف می‌شود.
                            </p>
                        </div>
                    )}

                    {selectMutation.isError && (
                        <p role="alert" className="text-sm text-destructive">
                            {selectMutation.error instanceof Error ? selectMutation.error.message : "ثبت انتخاب انجام نشد."}
                        </p>
                    )}
                </div>

                <div className="flex justify-end gap-2 border-t p-4">
                    <button type="button" onClick={onClose} disabled={selectMutation.isPending}
                        className="rounded-lg border px-5 py-2 text-sm disabled:opacity-50">انصراف</button>
                    <button type="button"
                        disabled={!candidate || selectMutation.isPending || (candidate != null && isAlreadySelected(candidate.companyId))}
                        onClick={() => { if (candidate) selectMutation.mutate(candidate.companyId); }}
                        className="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-primary-foreground disabled:opacity-50">
                        {selectMutation.isPending ? "در حال ثبت..." : "تأیید و قفل دائمی"}
                    </button>
                </div>
            </div>
        </div>
    );
}


