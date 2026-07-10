export const localeCookieName = "MES_LOCALE";
export const supportedLocales = ["zh-CN", "en-US"] as const;
export type AppLocale = (typeof supportedLocales)[number];
export const defaultLocale: AppLocale = "zh-CN";

export function isAppLocale(value: string | undefined): value is AppLocale {
  return supportedLocales.includes(value as AppLocale);
}

export function resolveLocale(cookieLocale?: string, acceptLanguage?: string | null): AppLocale {
  if (isAppLocale(cookieLocale)) return cookieLocale;
  for (const item of (acceptLanguage ?? "").split(",")) {
    const language = item.split(";", 1)[0]?.trim().toLowerCase();
    if (language?.startsWith("en")) return "en-US";
    if (language?.startsWith("zh")) return "zh-CN";
  }
  return defaultLocale;
}

export function formatNumberForLocale(value: number, locale: AppLocale): string {
  return new Intl.NumberFormat(locale).format(value);
}

export function formatDateForLocale(value: Date, locale: AppLocale): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium" }).format(value);
}
