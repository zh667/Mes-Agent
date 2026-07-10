"use client";

import { Layers3 } from "lucide-react";
import { useTranslations } from "next-intl";

import { BomExplosionWorkspace } from "@/components/bom/bom-explosion-workspace";
import { OperationsPageShell, OperationsPanel } from "@/components/operations/operations-page-shell";

export default function BomExplosionPage() {
  const t = useTranslations("bom");
  return (
    <OperationsPageShell title={t("title")} eyebrow={t("eyebrow")} description={t("description")} icon={Layers3} metrics={[{ label: t("metrics.depth"), value: "20" }, { label: t("metrics.quantity"), value: "1,000,000" }, { label: t("metrics.inventory"), value: t("metrics.liveSnapshot") }]}>
      <OperationsPanel title={t("requirements")} description={t("requirementsDescription")}>
        <BomExplosionWorkspace />
      </OperationsPanel>
    </OperationsPageShell>
  );
}
