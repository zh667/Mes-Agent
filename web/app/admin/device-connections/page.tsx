"use client";

import { RadioTower } from "lucide-react";
import { useTranslations } from "next-intl";

import { DeviceConnectionsWorkspace } from "@/components/devices/device-connections-workspace";
import { OperationsPageShell, OperationsPanel } from "@/components/operations/operations-page-shell";

export default function DeviceConnectionsPage() {
  const t = useTranslations("devices");
  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={RadioTower}
      metrics={[
        { label: t("metrics.protocols"), value: t("metrics.readOnly") },
        { label: t("metrics.secrets"), value: t("metrics.encrypted") },
        { label: t("metrics.statusCache"), value: t("metrics.seconds") },
      ]}
    >
      <OperationsPanel title={t("registry")} description={t("registryDescription")}>
        <DeviceConnectionsWorkspace />
      </OperationsPanel>
    </OperationsPageShell>
  );
}
