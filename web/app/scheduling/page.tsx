"use client";

import { GanttChart } from "lucide-react";
import { useTranslations } from "next-intl";

import { OperationsPageShell, OperationsPanel } from "@/components/operations/operations-page-shell";
import { SchedulingWorkspace } from "@/components/scheduling/scheduling-workspace";

export default function SchedulingPage() {
  const t = useTranslations("scheduling");
  return <OperationsPageShell title={t("title")} eyebrow={t("eyebrow")} description={t("description")} icon={GanttChart} metrics={[{ label: t("metrics.batchLimit"), value: t("metrics.orders", { count: 50 }) }, { label: t("metrics.adjustment"), value: t("metrics.minutes", { count: 15 }) }, { label: t("metrics.concurrency"), value: "PostgreSQL xmin" }]}><OperationsPanel title={t("timeline")} description={t("timelineDescription")}><SchedulingWorkspace /></OperationsPanel></OperationsPageShell>;
}
