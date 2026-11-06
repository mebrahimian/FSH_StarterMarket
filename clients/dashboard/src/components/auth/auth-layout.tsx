import type { PropsWithChildren } from "react";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher } from "@/components/layout/language-switcher";
export function AuthLayout({
    children,
}: PropsWithChildren) {
    const { i18n } = useTranslation("common");

    return (
        <div className="relative min-h-screen overflow-hidden bg-[#faf7f8]">
            {/* Backgrounds */}
            <div
                className="pointer-events-none absolute inset-0 z-0 overflow-hidden"
                aria-hidden="true"
            >
                <img
                    src="/branding/sadaf2/sadaf-splash-bgm.png"
                    alt=""
                    className="block h-full w-full object-contain object-top lg:hidden"
                />

                <img
                    src="/branding/sadaf2/sadaf-splash-bg.png"
                    alt=""
                    className="hidden h-full w-full object-cover object-top lg:block"
                />

                <div className="absolute inset-0 bg-white/10" />
            </div>

            {/* Language Switcher */}
            <div
                dir="ltr"
                className="absolute right-5 top-5 z-50 rounded-xl border border-slate-200 bg-white/90 shadow-sm backdrop-blur-sm"
            >
                <LanguageSwitcher />
            </div>

            {/* Auth Content */}
            <main
                dir="ltr"
                className="relative z-20 flex min-h-screen items-center justify-center px-4 py-20 sm:px-8 lg:justify-start lg:px-[2%]"
            >
                <div
                    dir={i18n.dir()}
                    className="w-full max-w-lg"
                >
                    {children}
                </div>
            </main>
        </div>
    );
}
