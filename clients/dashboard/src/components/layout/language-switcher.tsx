import { Languages } from "lucide-react";
import { useTranslation } from "react-i18next";

import {
    changeAppLanguage,
    type AppLanguage,
} from "@/i18n";
import { cn } from "@/lib/cn";

export function LanguageSwitcher() {
    const { i18n, t } =
        useTranslation("common");

    const currentLanguage: AppLanguage =
        i18n.resolvedLanguage
            ?.toLowerCase()
            .startsWith("fa")
            ? "fa"
            : "en";

    const nextLanguage: AppLanguage =
        currentLanguage === "fa"
            ? "en"
            : "fa";

    const nextLanguageName =
        nextLanguage === "fa"
            ? t("language.persian")
            : t("language.english");

    return (
        <button
            type="button"
            onClick={() =>
                void changeAppLanguage(nextLanguage)
            }
            title={`${t("language.label")}: ${nextLanguageName}`}
            aria-label={`${t("language.label")}: ${nextLanguageName}`}
            className={cn(
                "inline-flex h-9 cursor-pointer items-center gap-1.5 rounded-md px-2.5",
                "text-sm font-semibold text-[var(--color-muted-foreground)]",
                "transition-colors duration-[var(--duration-fast)]",
                "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
        >
            <Languages
                className="size-4"
                aria-hidden
            />

            <span>
                {nextLanguage === "fa"
                    ? "فا"
                    : "EN"}
            </span>
        </button>
    );
}