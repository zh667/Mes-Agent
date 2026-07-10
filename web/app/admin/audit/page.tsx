"use client";

import { ShieldCheck } from "lucide-react";
import { useTranslations } from "next-intl";

import { AuditTable } from "@/components/audit/audit-table";
import { OperationsPageShell, OperationsPanel } from "@/components/operations/operations-page-shell";

export default function AuditAdministrationPage() {
  const t = useTranslations("audit");
  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={ShieldCheck}
      metrics={[
        { label: t("metrics.scope"), value: t("metrics.activeTenant") },
        { label: t("metrics.retention"), value: t("metrics.policyManaged") },
        { label: t("metrics.exportLimit"), value: "10,000" },
      ]}
    >
      <OperationsPanel title={t("evidence")} description={t("evidenceDescription")}>
        <AuditTable />
      </OperationsPanel>
    </OperationsPageShell>
  );
}
