"use client";

import { Building2, LoaderCircle } from "lucide-react";
import { useSession } from "next-auth/react";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import { apiClient } from "@/lib/api-client";
import {
  clearActiveTenantId,
  getActiveTenantId,
  setActiveTenantId,
} from "@/lib/tenant";

export function TenantSelector() {
  const t = useTranslations("tenants");
  const { data: session, status } = useSession();
  const tenants = session?.user?.tenants;
  const selectableTenants = (tenants ?? []).filter(
    (tenant): tenant is typeof tenant & { id: string } =>
      typeof tenant.id === "string" && tenant.id.length > 0,
  );
  const [activeTenantId, setActiveTenant] = useState("");
  const [isSwitching, setIsSwitching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setActiveTenant(getActiveTenantId(tenants) ?? "");
  }, [tenants]);

  if (status !== "authenticated" || selectableTenants.length === 0) {
    return null;
  }

  async function handleTenantChange(tenantId: string) {
    setError(null);
    if (!tenantId) {
      clearActiveTenantId();
      setActiveTenant("");
      return;
    }

    setIsSwitching(true);
    try {
      await apiClient.post(`/tenants/switch/${tenantId}`);
      setActiveTenantId(tenantId);
      setActiveTenant(tenantId);
    } catch {
      clearActiveTenantId();
      setActiveTenant("");
      setError(t("denied"));
    } finally {
      setIsSwitching(false);
    }
  }

  return (
    <div className="border-b border-border bg-card/95 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div className="mx-auto flex min-h-12 w-full max-w-7xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
        <div className="flex min-w-0 items-center gap-2 text-xs font-medium text-muted-foreground">
          <Building2 className="h-4 w-4 shrink-0 text-primary" aria-hidden="true" />
          <span className="hidden sm:inline">{t("context")}</span>
        </div>
        <div className="flex min-w-0 items-center gap-2">
          <label htmlFor="active-tenant" className="sr-only">
            {t("active")}
          </label>
          <select
            id="active-tenant"
            aria-label={t("active")}
            value={activeTenantId}
            disabled={isSwitching}
            onChange={(event) => void handleTenantChange(event.target.value)}
            className="h-10 min-w-0 max-w-64 rounded-md bg-background px-3 text-sm font-medium text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.08)] outline-none transition-shadow focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-wait disabled:opacity-60"
          >
            <option value="">{t("select")}</option>
            {selectableTenants.map((tenant) => (
              <option key={tenant.id} value={tenant.id}>
                {tenant.code} - {tenant.name}
              </option>
            ))}
          </select>
          <span className="flex h-10 w-10 shrink-0 items-center justify-center" aria-live="polite">
            {isSwitching ? (
              <LoaderCircle className="h-4 w-4 animate-spin text-primary" aria-label={t("switching")} />
            ) : null}
          </span>
        </div>
      </div>
      {error ? (
        <p role="alert" className="mx-auto w-full max-w-7xl px-4 pb-2 text-xs text-destructive sm:px-6 lg:px-8">
          {error}
        </p>
      ) : null}
    </div>
  );
}
