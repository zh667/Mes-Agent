import { cookies, headers } from "next/headers";
import { getRequestConfig } from "next-intl/server";

import { localeCookieName, resolveLocale } from "@/lib/locale";

export default getRequestConfig(async () => {
  const locale = resolveLocale(cookies().get(localeCookieName)?.value, headers().get("accept-language"));
  return { locale, messages: (await import(`../messages/${locale}.json`)).default };
});
