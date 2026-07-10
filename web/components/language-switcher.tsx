"use client";

import { Languages } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";

import { localeCookieName, type AppLocale } from "@/lib/locale";

export function LanguageSwitcher() {
  const locale = useLocale();
  const t = useTranslations("common");
  const router = useRouter();

  function changeLocale(nextLocale: AppLocale) {
    document.cookie = `${localeCookieName}=${nextLocale}; Max-Age=31536000; Path=/; SameSite=Lax`;
    router.refresh();
  }

  return (
    <div className="inline-flex h-10 items-center gap-2 rounded-md border border-border bg-background px-2 text-muted-foreground">
      <Languages className="h-4 w-4" aria-hidden="true" />
      <label htmlFor="app-locale" className="sr-only">{t("language")}</label>
      <select id="app-locale" aria-label={t("language")} value={locale}
        onChange={(event) => changeLocale(event.target.value as AppLocale)}
        className="h-8 bg-transparent text-xs font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring">
        <option value="zh-CN">{t("chinese")}</option>
        <option value="en-US">{t("english")}</option>
      </select>
    </div>
  );
}
