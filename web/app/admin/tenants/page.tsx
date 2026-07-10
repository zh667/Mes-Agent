"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, Plus } from "lucide-react";
import { useSession } from "next-auth/react";
import { useTranslations } from "next-intl";
import { type FormEvent, useState } from "react";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";
import { apiClient } from "@/lib/api-client";
import type { components } from "@/shared/api/generated/schema";

type TenantSummaryDto = components["schemas"]["TenantSummaryDto"];
type CreateTenantRequest = components["schemas"]["CreateTenantRequest"];

export default function TenantAdministrationPage() {
  const t = useTranslations("tenants");
  const { data: session, status } = useSession();
  const queryClient = useQueryClient();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");

  const tenantsQuery = useQuery({
    queryKey: ["tenants", "admin"],
    queryFn: async () => (await apiClient.get<TenantSummaryDto[]>("/tenants")).data,
    enabled: session?.user?.isPlatformAdmin === true,
  });

  const createTenant = useMutation({
    mutationFn: async (request: CreateTenantRequest) =>
      (await apiClient.post<TenantSummaryDto>("/tenants", request)).data,
    onSuccess: async () => {
      setCode("");
      setName("");
      await queryClient.invalidateQueries({ queryKey: ["tenants", "admin"] });
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedCode = code.trim();
    const normalizedName = name.trim();
    if (normalizedCode && normalizedName) {
      createTenant.mutate({ code: normalizedCode, name: normalizedName });
    }
  }

  const tenants = tenantsQuery.data ?? [];

  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={Building2}
      metrics={[
        { label: t("metrics.total"), value: String(tenants.length) },
        { label: t("metrics.active"), value: String(tenants.filter((tenant) => tenant.isActive).length) },
        { label: t("metrics.access"), value: session?.user?.isPlatformAdmin ? t("metrics.platformAdmin") : t("metrics.restricted") },
      ]}
    >
      {status === "loading" ? (
        <p role="status" className="text-sm text-muted-foreground">{t("checking")}</p>
      ) : !session?.user?.isPlatformAdmin ? (
        <OperationsPanel title={t("restrictedWorkspace")}>
          <p className="text-sm text-muted-foreground">
            {t("adminRequired")}
          </p>
        </OperationsPanel>
      ) : (
        <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
          <OperationsPanel title={t("workspaces")} description={t("workspacesDescription")}>
            {tenantsQuery.isLoading ? (
              <p role="status" className="text-sm text-muted-foreground">{t("loading")}</p>
            ) : tenantsQuery.isError ? (
              <p role="alert" className="text-sm text-destructive">{t("loadError")}</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[520px] border-collapse text-left text-sm">
                  <thead className="text-xs uppercase text-muted-foreground">
                    <tr className="border-b border-border">
                      <th scope="col" className="px-2 py-3 font-medium">{t("columns.code")}</th>
                      <th scope="col" className="px-2 py-3 font-medium">{t("columns.workspace")}</th>
                      <th scope="col" className="px-2 py-3 font-medium">{t("columns.state")}</th>
                      <th scope="col" className="px-2 py-3 font-medium">{t("columns.identifier")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tenants.map((tenant) => (
                      <tr key={tenant.id} className="border-b border-border/70 last:border-0">
                        <td className="px-2 py-3 font-mono text-xs font-semibold text-foreground">{tenant.code}</td>
                        <td className="px-2 py-3 font-medium text-foreground">{tenant.name}</td>
                        <td className="px-2 py-3">
                          <StatusPill tone={tenant.isActive ? "good" : "neutral"}>
                            {tenant.isActive ? t("active") : t("inactive")}
                          </StatusPill>
                        </td>
                        <td className="px-2 py-3 font-mono text-xs text-muted-foreground">{tenant.id}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </OperationsPanel>

          <OperationsPanel title={t("createWorkspace")} description={t("createDescription")}>
            <form className="space-y-4" onSubmit={handleSubmit}>
              <div>
                <label htmlFor="tenant-code" className="text-xs font-medium text-foreground">{t("columns.code")}</label>
                <input
                  id="tenant-code"
                  value={code}
                  onChange={(event) => setCode(event.target.value.toUpperCase())}
                  maxLength={50}
                  required
                  className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm uppercase text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.08)] outline-none focus-visible:ring-2 focus-visible:ring-ring"
                />
              </div>
              <div>
                <label htmlFor="tenant-name" className="text-xs font-medium text-foreground">{t("workspaceName")}</label>
                <input
                  id="tenant-name"
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  maxLength={200}
                  required
                  className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.08)] outline-none focus-visible:ring-2 focus-visible:ring-ring"
                />
              </div>
              <button
                type="submit"
                disabled={createTenant.isPending}
                className="flex h-10 w-full items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground transition-transform active:scale-[0.96] disabled:cursor-not-allowed disabled:opacity-60"
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                {createTenant.isPending ? t("creating") : t("createTenant")}
              </button>
              {createTenant.isError ? (
                <p role="alert" className="text-xs text-destructive">{t("createError")}</p>
              ) : null}
            </form>
          </OperationsPanel>
        </div>
      )}
    </OperationsPageShell>
  );
}
