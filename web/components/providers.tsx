"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SessionProvider } from "next-auth/react";
import { useEffect, useState } from "react";

import { TenantSelector } from "@/components/tenant-selector";
import { LanguageSwitcher } from "@/components/language-switcher";
import { tenantChangedEventName } from "@/lib/tenant";

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: 1,
          },
        },
      }),
  );

  useEffect(() => {
    const clearTenantCache = () => queryClient.clear();
    window.addEventListener(tenantChangedEventName, clearTenantCache);
    return () => window.removeEventListener(tenantChangedEventName, clearTenantCache);
  }, [queryClient]);

  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>
        <div className="border-b border-border bg-card/95">
          <div className="mx-auto flex w-full max-w-7xl justify-end px-4 py-1 sm:px-6 lg:px-8">
            <LanguageSwitcher />
          </div>
        </div>
        <TenantSelector />
        {children}
      </QueryClientProvider>
    </SessionProvider>
  );
}
