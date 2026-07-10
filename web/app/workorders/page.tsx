import { ClipboardCheck, ClipboardList, Filter, Play } from "lucide-react";
import { useTranslations } from "next-intl";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

export default function WorkOrdersPage() {
  const t = useTranslations("workorders");
  const workOrders = [
    { code: "WO-20260709-014", product: t("preview.valveAssembly"), line: t("preview.line1"), status: t("preview.inProduction"), tone: "neutral" as const, progress: "64%", risk: t("preview.materialShortage") },
    { code: "WO-20260709-018", product: t("preview.pumpHousing"), line: t("preview.line2"), status: t("preview.paused"), tone: "danger" as const, progress: "72%", risk: t("preview.downtimeReview") },
    { code: "WO-20260709-021", product: t("preview.sensorBracket"), line: t("preview.line3"), status: t("preview.ready"), tone: "neutral" as const, progress: "0%", risk: t("preview.noRisk") },
  ];
  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={ClipboardList}
      metrics={[
        { label: t("metrics.open"), value: "24" },
        { label: t("metrics.delayed"), value: "7" },
        { label: t("metrics.output"), value: "8,420" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_340px]">
        <OperationsPanel
          title={t("dispatchBoard")}
          description={t("dispatchDescription")}
        >
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <label className="relative block flex-1">
              <span className="sr-only">{t("filter")}</span>
              <Filter
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                aria-hidden="true"
              />
              <input
                aria-label={t("filter")}
                defaultValue={t("filterExample")}
                className="min-h-11 w-full rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm outline-none transition-[border-color,box-shadow] duration-150 focus:border-ring focus:ring-2 focus:ring-ring/20"
              />
            </label>
            <div className="flex gap-2">
              <button
                type="button"
                className="min-h-11 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-[background-color,scale] duration-150 hover:bg-primary/90 active:scale-[0.96]"
              >
                {t("actions.start")}
              </button>
              <button
                type="button"
                className="min-h-11 rounded-md bg-muted px-4 text-sm font-medium text-foreground transition-[background-color,scale] duration-150 hover:bg-border active:scale-[0.96]"
              >
                {t("actions.report")}
              </button>
              <button
                type="button"
                className="min-h-11 rounded-md bg-muted px-4 text-sm font-medium text-foreground transition-[background-color,scale] duration-150 hover:bg-border active:scale-[0.96]"
              >
                {t("actions.complete")}
              </button>
            </div>
          </div>
          <div className="overflow-hidden rounded-md border border-border">
            <table className="w-full text-left text-sm">
              <thead className="bg-muted text-xs text-muted-foreground">
                <tr>
                  <th scope="col" className="px-3 py-2 font-medium">
                    {t("columns.workOrder")}
                  </th>
                  <th scope="col" className="px-3 py-2 font-medium">
                    {t("columns.line")}
                  </th>
                  <th scope="col" className="px-3 py-2 font-medium">
                    {t("columns.status")}
                  </th>
                  <th scope="col" className="px-3 py-2 font-medium">
                    {t("columns.progress")}
                  </th>
                  <th scope="col" className="px-3 py-2 font-medium">
                    {t("columns.risk")}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {workOrders.map((order) => (
                  <tr key={order.code}>
                    <td className="px-3 py-3">
                      <span className="block font-medium text-foreground">
                        {order.code}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {order.product}
                      </span>
                    </td>
                    <td className="px-3 py-3 text-muted-foreground">
                      {order.line}
                    </td>
                    <td className="px-3 py-3">
                      <StatusPill
                        tone={order.tone}
                      >
                        {order.status}
                      </StatusPill>
                    </td>
                    <td className="px-3 py-3 tabular-nums">
                      {order.progress}
                    </td>
                    <td className="px-3 py-3 text-muted-foreground">
                      {order.risk}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </OperationsPanel>

        <OperationsPanel
          title={t("operationDetail")}
          description={t("operationDescription")}
        >
          <div className="space-y-3 text-sm">
            <div className="rounded-md bg-muted p-3">
              <div className="flex items-center gap-2 font-medium text-foreground">
                <Play className="h-4 w-4 text-primary" aria-hidden="true" />
                {t("assemblyRunning")}
              </div>
              <p className="mt-2 text-muted-foreground">
                {t("operatorReporting")}
              </p>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <Detail label={t("planned")} value="1,200" />
              <Detail label={t("completed")} value="768" />
              <Detail label={t("goodQuantity")} value="742" />
              <Detail label={t("scrap")} value="26" />
            </div>
            <div className="flex items-center gap-2 rounded-md bg-background p-3 shadow-[0_0_0_1px_rgba(0,0,0,0.06)]">
              <ClipboardCheck className="h-4 w-4 text-primary" aria-hidden="true" />
              <span className="text-muted-foreground">
                {t("qualityConfirmation")}
              </span>
            </div>
          </div>
        </OperationsPanel>
      </section>
    </OperationsPageShell>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md bg-background p-3 shadow-[0_0_0_1px_rgba(0,0,0,0.06)]">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 font-semibold tabular-nums text-foreground">
        {value}
      </div>
    </div>
  );
}
