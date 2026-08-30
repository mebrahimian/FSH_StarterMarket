import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";

import {
    createUnlistedPortfolioCompany,
    searchPortfolioCompanies,
    type PortfolioCompanyTarget,
} from "@/api/market-intelligence";
import { Input } from "@/components/ui/input";

export function PortfolioCompanyTargetPicker({
    rawCompanyName,
    fSortName,
    isListed,
    value,
    onChange,
}: {
    rawCompanyName: string;
    fSortName: string;
    isListed: boolean;
    value: PortfolioCompanyTarget | null;
    onChange: (company: PortfolioCompanyTarget | null) => void;
}) {
    const [searchText, setSearchText] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const [open, setOpen] = useState(false);

    useEffect(() => {
        const timer = window.setTimeout(() => {
            setDebouncedSearch(searchText.trim());
        }, 300);

        return () => window.clearTimeout(timer);
    }, [searchText]);

    const searchQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "portfolio-matching",
            "company-search",
            debouncedSearch,
            isListed,
        ],
        queryFn: () =>
            searchPortfolioCompanies(
                debouncedSearch,
                isListed,
            ),
        enabled:
            value === null &&
            debouncedSearch.length >= 2,
        staleTime: 30_000,
    });

    const createMutation = useMutation({
        mutationFn: () =>
            createUnlistedPortfolioCompany({
                rawCompanyName,
                fSortName,
            }),
        onSuccess: (company) => {
            onChange(company);
            setSearchText(company.companyName);
            setOpen(false);
        },
    });

    const selectCompany = (
        company: PortfolioCompanyTarget,
    ) => {
        onChange(company);

        setSearchText(
            company.symbol
                ? `${company.symbol} - ${company.companyName}`
                : company.companyName,
        );

        setOpen(false);
    };

    return (
        <div className="relative min-w-[260px]">
            <Input
                value={searchText}
                placeholder={
                    isListed
                        ? "جستجوی نماد یا نام شرکت..."
                        : "جستجوی شرکت غیربورسی..."
                }
                onFocus={() => setOpen(true)}
                onChange={(event) => {
                    setSearchText(event.target.value);

                    if (value !== null) {
                        onChange(null);
                    }

                    setOpen(true);
                }}
            />

            {open &&
                value === null &&
                debouncedSearch.length >= 2 && (
                    <div className="absolute z-50 mt-1 max-h-72 w-full overflow-y-auto rounded-lg border bg-background shadow-lg">
                        {searchQuery.isLoading ? (
                            <div className="p-3 text-sm text-muted-foreground">
                                در حال جستجو...
                            </div>
                        ) : (
                            <>
                                {(searchQuery.data ?? []).map(
                                    (company) => (
                                        <button
                                            key={company.companyId}
                                            type="button"
                                            className="flex w-full items-center justify-between gap-3 border-b px-3 py-2 text-start text-sm hover:bg-muted"
                                            onMouseDown={(event) =>
                                                event.preventDefault()
                                            }
                                            onClick={() =>
                                                selectCompany(company)
                                            }
                                        >
                                            <span>
                                                {company.companyName}
                                            </span>

                                            {company.symbol && (
                                                <span className="shrink-0 text-xs text-muted-foreground">
                                                    {company.symbol}
                                                </span>
                                            )}
                                        </button>
                                    ),
                                )}

                                {!isListed && (
                                    <button
                                        type="button"
                                        disabled={
                                            createMutation.isPending
                                        }
                                        className="w-full px-3 py-3 text-start text-sm font-medium hover:bg-muted disabled:opacity-50"
                                        onMouseDown={(event) =>
                                            event.preventDefault()
                                        }
                                        onClick={() =>
                                            createMutation.mutate()
                                        }
                                    >
                                        {createMutation.isPending
                                            ? "در حال ایجاد..."
                                            : `+ ایجاد «${rawCompanyName}» به‌عنوان شرکت غیربورسی`}
                                    </button>
                                )}

                                {isListed &&
                                    (searchQuery.data ?? [])
                                        .length === 0 && (
                                        <div className="p-3 text-sm text-muted-foreground">
                                            شرکت بورسی پیدا نشد
                                        </div>
                                    )}

                                {createMutation.isError && (
                                    <div className="px-3 pb-3 text-sm text-destructive">
                                        خطا در ایجاد شرکت غیربورسی
                                    </div>
                                )}
                            </>
                        )}
                    </div>
                )}
        </div>
    );
}