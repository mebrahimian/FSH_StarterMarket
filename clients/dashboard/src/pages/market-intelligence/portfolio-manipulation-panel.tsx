import {
    RotateCcw,
    Search,
} from "lucide-react";
import {
    useMemo,
    useState,
} from "react";
import {
    useMutation,
    useQuery,
    useQueryClient,
} from "@tanstack/react-query";

import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

import {
    getMatchedPortfolioCompanies,
    type MatchedPortfolioCompany,
    unmatchPortfolioCompany,
} from "@/api/market-intelligence";

type ListingFilter =
    | "all"
    | "listed"
    | "unlisted";

export function PortfolioManipulationPanel() {
    const queryClient = useQueryClient();

    const [search, setSearch] =
        useState("");

    const [listingFilter, setListingFilter] =
        useState<ListingFilter>("all");

    const [pendingUnmatch, setPendingUnmatch] =
        useState<MatchedPortfolioCompany | null>(null);

    const {
        data: companies = [],
        isLoading,
        isError,
    } = useQuery<MatchedPortfolioCompany[]>({
        queryKey: [
            "market-intelligence",
            "portfolio-matching",
            "matched",
        ],
        queryFn: getMatchedPortfolioCompanies,
    });

    const filteredCompanies =
        useMemo(() => {
            const normalizedSearch =
                search.trim().toLowerCase();

            return companies.filter(company => {
                if (
                    listingFilter === "listed" &&
                    !company.isListed
                ) {
                    return false;
                }

                if (
                    listingFilter === "unlisted" &&
                    company.isListed
                ) {
                    return false;
                }

                if (!normalizedSearch) {
                    return true;
                }

                return [
                    company.rawCompanyName,
                    company.fSortName,
                    company.symbol ?? "",
                    company.companyName ?? "",
                    String(company.companyId),
                ].some(value =>
                    value
                        .toLowerCase()
                        .includes(normalizedSearch),
                );
            });
        }, [
            companies,
            listingFilter,
            search,
        ]);

    const unmatchMutation =
        useMutation({
            mutationFn: (
                company: MatchedPortfolioCompany,
            ) =>
                unmatchPortfolioCompany({
                    fSortName: company.fSortName,
                    isListed: company.isListed,
                    companyId: company.companyId,
                }),

            onSuccess: async () => {
                setPendingUnmatch(null);

                await queryClient.invalidateQueries({
                    queryKey: [
                        "market-intelligence",
                        "portfolio-matching",
                    ],
                });
            },
        });

    return (
        <>
            <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
                <div className="flex flex-col gap-3 border-b border-border p-4 lg:flex-row lg:items-center lg:justify-between">
                    <div>
                        <h2 className="text-base font-semibold">
                            مدیریت تطبیق‌های پرتفوی
                        </h2>

                        <p className="mt-1 text-xs text-muted-foreground">
                            مشاهده و اصلاح CompanyId فعلی شرکت‌های پرتفوی
                        </p>
                    </div>

                    <div className="flex flex-col gap-2 sm:flex-row">
                        <div className="relative min-w-72">
                            <Search className="absolute right-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                            <input
                                type="text"
                                value={search}
                                onChange={event =>
                                    setSearch(
                                        event.target.value,
                                    )
                                }
                                placeholder="نام، نماد یا CompanyId..."
                                className="h-9 w-full rounded-lg border border-input bg-background pr-9 pl-3 text-sm outline-none transition focus:border-ring focus:ring-2 focus:ring-ring/20"
                            />
                        </div>

                        <div className="flex gap-1 rounded-lg bg-muted/60 p-1">
                            <Button
                                type="button"
                                variant={
                                    listingFilter === "all"
                                        ? "secondary"
                                        : "ghost"
                                }
                                size="sm"
                                onClick={() =>
                                    setListingFilter("all")
                                }
                            >
                                همه
                            </Button>

                            <Button
                                type="button"
                                variant={
                                    listingFilter === "listed"
                                        ? "secondary"
                                        : "ghost"
                                }
                                size="sm"
                                onClick={() =>
                                    setListingFilter("listed")
                                }
                            >
                                بورسی
                            </Button>

                            <Button
                                type="button"
                                variant={
                                    listingFilter === "unlisted"
                                        ? "secondary"
                                        : "ghost"
                                }
                                size="sm"
                                onClick={() =>
                                    setListingFilter("unlisted")
                                }
                            >
                                غیربورسی
                            </Button>
                        </div>
                    </div>
                </div>

                <div className="overflow-x-auto">
                    <div className="min-w-[1150px]">
                        <div className="grid grid-cols-[2fr_2fr_90px_90px_100px_160px_90px_100px] gap-3 border-b border-border bg-muted/30 px-5 py-3 text-xs font-semibold text-muted-foreground">
                            <div>نام پرتفوی</div>
                            <div>مقصد فعلی</div>
                            <div className="text-center">
                                نوع
                            </div>
                            <div className="text-center">
                                Position
                            </div>
                            <div className="text-center">
                                استفاده در
                            </div>
                            <div className="text-center">
                                دوره
                            </div>
                            <div className="text-center">
                                Alias
                            </div>
                            <div className="text-center">
                                عملیات
                            </div>
                        </div>

                        {isLoading && (
                            <div className="flex min-h-72 items-center justify-center text-sm text-muted-foreground">
                                در حال خواندن تطبیق‌ها...
                            </div>
                        )}

                        {isError && (
                            <div className="flex min-h-72 items-center justify-center text-sm text-destructive">
                                خطا در دریافت تطبیق‌های پرتفوی
                            </div>
                        )}

                        {!isLoading &&
                            !isError &&
                            filteredCompanies.length === 0 && (
                                <div className="flex min-h-72 items-center justify-center text-sm text-muted-foreground">
                                    موردی پیدا نشد
                                </div>
                            )}

                        {!isLoading &&
                            !isError &&
                            filteredCompanies.map(company => {
                                const key =
                                    `${company.fSortName}-${company.isListed}-${company.companyId}`;

                                return (
                                    <div
                                        key={key}
                                        className="grid grid-cols-[2fr_2fr_90px_90px_100px_160px_90px_100px] items-center gap-3 border-b border-border/70 px-5 py-3 text-sm transition hover:bg-muted/25"
                                    >
                                        <div>
                                            <div className="font-medium">
                                                {company.rawCompanyName}
                                            </div>

                                            <div className="mt-1 truncate text-xs text-muted-foreground">
                                                {company.fSortName}
                                            </div>
                                        </div>

                                        <div>
                                            <div className="font-semibold">
                                                {company.symbol ?? "—"}
                                            </div>

                                            <div className="mt-1 text-xs text-muted-foreground">
                                                {company.companyName ?? "—"}
                                            </div>

                                            <div className="mt-1 text-[11px] text-muted-foreground">
                                                CompanyId:{" "}
                                                {company.companyId}
                                            </div>
                                        </div>

                                        <div className="text-center">
                                            <span className="rounded-full bg-muted px-2 py-1 text-xs">
                                                {company.isListed
                                                    ? "بورسی"
                                                    : "غیربورسی"}
                                            </span>
                                        </div>

                                        <div className="text-center font-semibold">
                                            {company.occurrenceCount
                                                .toLocaleString()}
                                        </div>

                                        <div className="text-center">
                                            {company.parentSymbolCount
                                                .toLocaleString()}
                                            {" "}نماد
                                        </div>

                                        <div className="text-center text-xs">
                                            <div>
                                                {company.firstPeriod}
                                            </div>

                                            <div className="my-0.5 text-muted-foreground">
                                                تا
                                            </div>

                                            <div>
                                                {company.lastPeriod}
                                            </div>
                                        </div>

                                        <div className="text-center">
                                            <span
                                                className={
                                                    company.hasAlias
                                                        ? "text-emerald-600"
                                                        : "text-muted-foreground"
                                                }
                                            >
                                                {company.hasAlias
                                                    ? "دارد"
                                                    : "ندارد"}
                                            </span>
                                        </div>

                                        <div className="text-center">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                className="gap-1"
                                                disabled={
                                                    unmatchMutation.isPending
                                                }
                                                onClick={() =>
                                                    setPendingUnmatch(
                                                        company,
                                                    )
                                                }
                                            >
                                                <RotateCcw className="size-3.5" />
                                                برگشت
                                            </Button>
                                        </div>
                                    </div>
                                );
                            })}
                    </div>
                </div>

                {!isLoading && !isError && (
                    <div className="border-t border-border px-5 py-3 text-xs text-muted-foreground">
                        نمایش{" "}
                        <strong className="text-foreground">
                            {filteredCompanies.length
                                .toLocaleString()}
                        </strong>{" "}
                        از{" "}
                        <strong className="text-foreground">
                            {companies.length
                                .toLocaleString()}
                        </strong>{" "}
                        تطبیق
                    </div>
                )}
            </section>

            <Dialog
                open={pendingUnmatch !== null}
                onOpenChange={open => {
                    if (!open) {
                        setPendingUnmatch(null);
                    }
                }}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>
                            برگرداندن تطبیق
                        </DialogTitle>

                        <DialogDescription>
                            آیا تطبیق «
                            {pendingUnmatch?.rawCompanyName}
                            » با «
                            {pendingUnmatch?.companyName ??
                                pendingUnmatch?.symbol ??
                                pendingUnmatch?.companyId}
                            » حذف شود؟
                            این شرکت دوباره به فهرست نیازمند
                            تطبیق برمی‌گردد.
                        </DialogDescription>
                    </DialogHeader>

                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() =>
                                setPendingUnmatch(null)
                            }
                        >
                            انصراف
                        </Button>

                        <Button
                            type="button"
                            variant="destructive"
                            disabled={
                                pendingUnmatch === null ||
                                unmatchMutation.isPending
                            }
                            onClick={() => {
                                if (pendingUnmatch === null) {
                                    return;
                                }

                                unmatchMutation.mutate(
                                    pendingUnmatch,
                                );
                            }}
                        >
                            {unmatchMutation.isPending
                                ? "در حال برگشت..."
                                : "برگرداندن تطبیق"}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </>
    );
}