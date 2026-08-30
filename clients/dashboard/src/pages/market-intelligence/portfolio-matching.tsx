
import {
    Building2,
    CheckCircle2,
    ChevronDown,
    ChevronLeft,
    ChevronUp,
    Link2,
    Search,
    Store,
    type LucideIcon,
} from "lucide-react";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {
    Fragment,
    useMemo,
    useState,
} from "react";
import { useNavigate } from "react-router-dom";
import {
    useMutation,
    useQuery,
    useQueryClient,
} from "@tanstack/react-query";
import { PortfolioCompanyTargetPicker } from "./portfolio-company-target-picker";

import { Button } from "@/components/ui/button";
import { PageHero } from "@/components/list";

import {
    getPortfolioCompanyUsage,
    getUnmatchedPortfolioCompanies,
    type UnmatchedPortfolioCompany,
    type PortfolioCompanyTarget,
    matchPortfolioCompany,
} from "@/api/market-intelligence";

type ListingFilter =
    | "all"
    | "listed"
    | "unlisted";


export function PortfolioMatchingPage() {
    const navigate = useNavigate();

    const [search, setSearch] =
        useState("");

    const [listingFilter, setListingFilter] =
        useState<ListingFilter>("all");

    const [expandedCompany, setExpandedCompany] =
        useState<string | null>(null);

    const [selectedTargets, setSelectedTargets] = useState<
        Record<string, PortfolioCompanyTarget | null>
    >({});
    const {
        data: companies = [],
        isLoading,
        isError,
    } = useQuery<UnmatchedPortfolioCompany[]>({
        queryKey: [
            "market-intelligence",
            "portfolio-matching",
            "unmatched",
        ],
        queryFn: getUnmatchedPortfolioCompanies,
    });

    const listedCount =
        companies.filter(company =>
            company.isListed).length;

    const unlistedCount =
        companies.length - listedCount;

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

                return (
                    company.rawCompanyName
                        .toLowerCase()
                        .includes(normalizedSearch) ||
                    company.fSortName
                        .toLowerCase()
                        .includes(normalizedSearch)
                );
            });
        }, [
            companies,
            listingFilter,
            search,
        ]);
    const queryClient = useQueryClient();
    const [pendingMatch, setPendingMatch] = useState<{
        company: UnmatchedPortfolioCompany;
        target: PortfolioCompanyTarget;
    } | null>(null);
    const matchMutation = useMutation({
        mutationFn: ({
            company,
            target,
        }: {
            company: UnmatchedPortfolioCompany;
            target: PortfolioCompanyTarget;
        }) =>
            matchPortfolioCompany({
                rawCompanyName: company.rawCompanyName,
                fSortName: company.fSortName,
                isListed: company.isListed,
                companyId: target.companyId,
            }),

        onSuccess: async () => {
            await queryClient.invalidateQueries();
        },
    });
    return (
        <div
            className="-mt-5 space-y-5"
            dir="rtl"
        >
            <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                <PageHero
                    title="تطبیق شرکت‌های پرتفوی"
                    subtitle="شناسایی و اتصال نام‌های پرتفوی که هنوز CompanyId ندارند"
                />

                <Button
                    type="button"
                    variant="outline"
                    className="gap-2 self-start"
                    onClick={() =>
                        navigate(
                            "/market-intelligence/health-center",
                        )
                    }
                >
                    بازگشت به مرکز سلامت
                    <ChevronLeft className="size-4" />
                </Button>
            </div>

            <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <MatchingStat
                    icon={Link2}
                    title="بدون CompanyId"
                    value={
                        isLoading
                            ? "..."
                            : companies.length
                                .toLocaleString()
                    }
                    hint="نام یکتای نیازمند تطبیق"
                />

                <MatchingStat
                    icon={Building2}
                    title="بورسی"
                    value={
                        isLoading
                            ? "..."
                            : listedCount.toLocaleString()
                    }
                    hint="شرکت‌های بورسی تطبیق‌نشده"
                />

                <MatchingStat
                    icon={Store}
                    title="غیربورسی"
                    value={
                        isLoading
                            ? "..."
                            : unlistedCount.toLocaleString()
                    }
                    hint="شرکت‌های غیربورسی تطبیق‌نشده"
                />

                <MatchingStat
                    icon={CheckCircle2}
                    title="وضعیت تطبیق"
                    value={
                        isLoading
                            ? "..."
                            : companies.length === 0
                                ? "کامل"
                                : "نیاز به بررسی"
                    }
                    hint={
                        companies.length === 0
                            ? "همه CompanyIdها مشخص هستند"
                            : "موارد باقی‌مانده را بررسی کنید"
                    }
                />
            </section>

            <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
                <div className="flex flex-col gap-3 border-b border-border p-4 lg:flex-row lg:items-center lg:justify-between">
                    <div>
                        <h2 className="text-base font-semibold">
                            شرکت‌های نیازمند بررسی
                        </h2>

                        <p className="mt-1 text-xs text-muted-foreground">
                            اولویت بر اساس تعداد نمادهای استفاده‌کننده
                            و سپس تعداد تکرار
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
                                placeholder="جستجو در نام شرکت..."
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
                                    setListingFilter(
                                        "listed",
                                    )
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
                                    setListingFilter(
                                        "unlisted",
                                    )
                                }
                            >
                                غیربورسی
                            </Button>
                        </div>
                    </div>
                </div>

                <div className="overflow-x-auto">
                    <div className="min-w-[1050px]">
                        <div className="grid grid-cols-[2.2fr_1.8fr_100px_100px_125px_2fr_115px] gap-3 border-b border-border bg-muted/30 px-5 py-3 text-xs font-semibold text-muted-foreground">
                            <div>نام کدال</div>
                            <div>نام نرمال‌شده</div>
                            <div className="text-center">
                                نوع
                            </div>
                            <div className="text-center">
                                تکرار
                            </div>
                            <div className="text-center">
                                استفاده در
                            </div>
                            <div>
                                شرکت مقصد
                            </div>
                            <div className="text-center">
                                عملیات
                            </div>
                        </div>

                        {isLoading && (
                            <div className="flex min-h-72 items-center justify-center text-sm text-muted-foreground">
                                در حال خواندن پرتفوی...
                            </div>
                        )}

                        {isError && (
                            <div className="flex min-h-72 items-center justify-center text-sm text-destructive">
                                خطا در دریافت اطلاعات پرتفوی
                            </div>
                        )}

                        {!isLoading &&
                            !isError &&
                            filteredCompanies.length === 0 && (
                                <div className="flex min-h-72 flex-col items-center justify-center text-center">
                                    <CheckCircle2 className="mb-3 size-8 text-muted-foreground" />

                                    <div className="text-sm font-semibold">
                                        موردی پیدا نشد
                                    </div>
                                </div>
                            )}

                        {!isLoading &&
                            !isError &&
                            filteredCompanies.map(company => {
                                const companyKey =
                                    `${company.fSortName}-${company.isListed}`;

                                const isExpanded =
                                    expandedCompany === companyKey;
                                const selectedTarget =
                                    selectedTargets[companyKey] ?? null;

                                return (
                                    <Fragment key={companyKey}>
                                        <div
                                            className={[
                                                "grid grid-cols-[2.2fr_1.8fr_100px_100px_125px_2fr_115px]",
                                                "items-center gap-3 border-b border-border/70",
                                                "px-5 py-3 text-sm transition",
                                                isExpanded
                                                    ? "bg-muted/30"
                                                    : "hover:bg-muted/25",
                                            ].join(" ")}
                                        >
                                            <div className="font-medium">
                                                {company.rawCompanyName}
                                            </div>

                                            <div className="truncate text-xs text-muted-foreground">
                                                {company.fSortName}
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
                                                <Button
                                                    type="button"
                                                    variant="ghost"
                                                    size="sm"
                                                    className="gap-1 font-semibold"
                                                    onClick={() =>
                                                        setExpandedCompany(
                                                            isExpanded
                                                                ? null
                                                                : companyKey,
                                                        )
                                                    }
                                                >
                                                    {company.parentSymbolCount
                                                        .toLocaleString()}{" "}
                                                    نماد

                                                    {isExpanded ? (
                                                        <ChevronUp className="size-3.5" />
                                                    ) : (
                                                        <ChevronDown className="size-3.5" />
                                                    )}
                                                </Button>
                                            </div>

                                            <div>
                                                <PortfolioCompanyTargetPicker
                                                    rawCompanyName={company.rawCompanyName}
                                                    fSortName={company.fSortName}
                                                    isListed={company.isListed}
                                                    value={selectedTarget}
                                                    onChange={(target) =>
                                                        setSelectedTargets((current) => ({
                                                            ...current,
                                                            [companyKey]: target,
                                                        }))
                                                    }
                                                />
                                            </div>

                                            <div className="text-center">
                                                <Button
                                                    type="button"
                                                    size="sm"
                                                    disabled={
                                                        selectedTarget === null ||
                                                        matchMutation.isPending
                                                    }
                                                    onClick={() => {
                                                        if (selectedTarget === null) {
                                                            return;
                                                        }

                                                        setPendingMatch({
                                                            company,
                                                            target: selectedTarget,
                                                        });
                                                    }}
                                                >
                                                    تطبیق
                                                </Button>
                                            </div>
                                        </div>

                                        {isExpanded && (
                                            <PortfolioUsageDetails
                                                fSortName={company.fSortName}
                                                isListed={company.isListed}
                                            />
                                        )}
                                    </Fragment>
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
                        نام یکتا
                    </div>
                )}
            </section>
            <Dialog
                open={pendingMatch !== null}
                onOpenChange={(open) => {
                    if (!open) {
                        setPendingMatch(null);
                    }
                }}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>
                            تأیید تطبیق
                        </DialogTitle>

                        <DialogDescription>
                            آیا «{pendingMatch?.company.rawCompanyName}»
                            {" "}به{" "}
                            «{pendingMatch?.target.companyName}»
                            تطبیق داده شود؟
                        </DialogDescription>
                    </DialogHeader>

                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setPendingMatch(null)}
                        >
                            انصراف
                        </Button>

                        <Button
                            type="button"
                            disabled={
                                pendingMatch === null ||
                                matchMutation.isPending
                            }
                            onClick={() => {
                                if (pendingMatch === null) {
                                    return;
                                }

                                const match = pendingMatch;

                                setPendingMatch(null);

                                matchMutation.mutate(match);
                            }}
                        >
                            {matchMutation.isPending
                                ? "در حال تطبیق..."
                                : "تأیید تطبیق"}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}

function PortfolioUsageDetails({
    fSortName,
    isListed,
}: {
    fSortName: string;
    isListed: boolean;
}) {
    const {
        data: usage = [],
        isLoading,
        isError,
    } = useQuery({
        queryKey: [
            "market-intelligence",
            "portfolio-matching",
            "usage",
            fSortName,
            isListed,
        ],
        queryFn: () =>
            getPortfolioCompanyUsage(
                fSortName,
                isListed,
            ),
    });

    return (
        <div className="border-b border-border bg-muted/15 px-5 py-4">
            <div className="overflow-hidden rounded-xl border border-border bg-background">
                <div className="grid grid-cols-[1.2fr_2fr_1fr_1.4fr_1.4fr] gap-3 border-b border-border bg-muted/30 px-4 py-2.5 text-xs font-semibold text-muted-foreground">
                    <div>
                        نماد دارنده
                    </div>

                    <div>
                        نام شرکت
                    </div>

                    <div className="text-center">
                        تعداد دوره
                    </div>

                    <div className="text-center">
                        اولین دوره
                    </div>

                    <div className="text-center">
                        آخرین دوره
                    </div>
                </div>

                {isLoading && (
                    <div className="px-4 py-6 text-center text-xs text-muted-foreground">
                        در حال خواندن محل‌های استفاده...
                    </div>
                )}

                {isError && (
                    <div className="px-4 py-6 text-center text-xs text-destructive">
                        خطا در دریافت محل‌های استفاده
                    </div>
                )}

                {!isLoading &&
                    !isError &&
                    usage.map(item => (
                        <div
                            key={item.symbol}
                            className="grid grid-cols-[1.2fr_2fr_1fr_1.4fr_1.4fr] gap-3 border-b border-border/60 px-4 py-2.5 text-xs last:border-b-0"
                        >
                            <div className="font-semibold">
                                {item.symbol}
                            </div>

                            <div className="text-muted-foreground">
                                {item.companyName}
                            </div>

                            <div className="text-center">
                                {item.occurrenceCount
                                    .toLocaleString()}
                            </div>

                            <div className="text-center text-muted-foreground">
                                {item.firstPeriod}
                            </div>

                            <div className="text-center text-muted-foreground">
                                {item.lastPeriod}
                            </div>
                        </div>
                    ))}
            </div>
        </div>
    );
}
function MatchingStat({
    icon: Icon,
    title,
    value,
    hint,
}: {
    icon: LucideIcon;
    title: string;
    value: string;
    hint: string;
}) {
    return (
        <div className="group rounded-2xl border border-border bg-card p-4 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md">
            <div className="flex items-center justify-between gap-4">
                <div className="min-w-0">
                    <div className="text-sm font-medium text-muted-foreground">
                        {title}
                    </div>

                    <div className="mt-2 text-2xl font-bold tracking-tight">
                        {value}
                    </div>

                    <div className="mt-1 truncate text-xs text-muted-foreground">
                        {hint}
                    </div>
                </div>

                <div className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-muted transition group-hover:scale-105">
                    <Icon className="size-5" />
                </div>
            </div>
        </div>
    );
}