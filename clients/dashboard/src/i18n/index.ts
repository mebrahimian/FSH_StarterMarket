import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import commonEn from "./locales/en/common.json";
import commonFa from "./locales/fa/common.json";
import navigationEn from "./locales/en/navigation.json";
import navigationFa from "./locales/fa/navigation.json";
import disclosuresEn from "./locales/en/disclosures.json";
import disclosuresFa from "./locales/fa/disclosures.json";

export const appLanguages = ["en", "fa"] as const;

export type AppLanguage =
    (typeof appLanguages)[number];

const LANGUAGE_STORAGE_KEY = "fsh.language";

function getInitialLanguage(): AppLanguage {
    try {
        const savedLanguage =
            window.localStorage.getItem(
                LANGUAGE_STORAGE_KEY,
            );

        if (
            savedLanguage === "en" ||
            savedLanguage === "fa"
        ) {
            return savedLanguage;
        }
    } catch {
        // Local storage may be unavailable.
    }

    return "fa";
}

export function isRtlLanguage(
    language: string,
): boolean {
    return language
        .toLowerCase()
        .startsWith("fa");
}

function applyDocumentLanguage(
    language: string,
): void {
    const normalizedLanguage: AppLanguage =
        isRtlLanguage(language) ? "fa" : "en";

    const direction =
        normalizedLanguage === "fa"
            ? "rtl"
            : "ltr";

    document.documentElement.lang =
        normalizedLanguage;

    document.documentElement.dir =
        direction;

    document.body?.setAttribute(
        "dir",
        direction,
    );

    try {
        window.localStorage.setItem(
            LANGUAGE_STORAGE_KEY,
            normalizedLanguage,
        );
    } catch {
        // Local storage may be unavailable.
    }
}

const initialLanguage = getInitialLanguage();

applyDocumentLanguage(initialLanguage);

i18n.on(
    "languageChanged",
    applyDocumentLanguage,
);

void i18n
    .use(initReactI18next)
    .init({
        resources: {
            en: {
                common: commonEn,
                navigation: navigationEn,
                disclosures: disclosuresEn,
            },
            fa: {
                common: commonFa,
                navigation: navigationFa,
                disclosures: disclosuresFa,
            },
        },
        lng: initialLanguage,
        fallbackLng: "en",
        supportedLngs: appLanguages,
        defaultNS: "common",
        ns: [
            "common",
            "navigation",
            "disclosures",
        ],
        interpolation: {
            escapeValue: false,
        },
        react: {
            useSuspense: false,
        },
    });

export async function changeAppLanguage(
    language: AppLanguage,
): Promise<void> {
    await i18n.changeLanguage(language);
}

export default i18n;