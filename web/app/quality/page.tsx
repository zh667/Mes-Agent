import { Route, Search, ShieldCheck } from "lucide-react";
import { useTranslations } from "next-intl";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

export default function QualityPage() {
  const t = useTranslations("quality");
  const traceSteps = [
    { step: t("preview.dispatch"), time: "07:55", detail: t("preview.dispatchDetail") },
    { step: t("preview.materialIssue"), time: "08:10", detail: t("preview.materialDetail") },
    { step: t("preview.inspection"), time: "10:35", detail: t("preview.inspectionDetail") },
  ];
  const defects = [
    { type: t("preview.surfaceScratch"), count: "12", operation: "OP-20" },
    { type: t("preview.dimensionDrift"), count: "5", operation: "OP-30" },
    { type: t("preview.missingLabel"), count: "3", operation: t("preview.packing") },
  ];
  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={ShieldCheck}
      metrics={[
        { label: t("metrics.inspections"), value: "42" },
        { label: t("metrics.failed"), value: "5" },
        { label: t("metrics.defects"), value: "20" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[360px_minmax(0,1fr)]">
        <OperationsPanel
          title={t("batchLookup")}
          description={t("batchDescription")}
        >
          <label className="relative block">
            <span className="sr-only">{t("searchBatch")}</span>
            <Search
              className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden="true"
            />
            <input
              aria-label={t("searchBatch")}
              defaultValue="B20260709-A102"
              className="min-h-11 w-full rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm outline-none transition-[border-color,box-shadow] duration-150 focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
          </label>
          <div className="mt-4 rounded-md bg-muted p-3 text-sm">
            <div className="flex items-center justify-between gap-3">
              <span className="font-medium text-foreground">{t("traceStatus")}</span>
              <StatusPill tone="warning">{t("partialReview")}</StatusPill>
            </div>
            <p className="mt-2 text-muted-foreground">
              {t("reviewNote")}
            </p>
          </div>
        </OperationsPanel>

        <div className="grid gap-4">
          <OperationsPanel
            title={t("timeline")}
            description={t("timelineDescription")}
          >
            <ol className="space-y-3">
              {traceSteps.map((step) => (
                <li key={step.step} className="flex gap-3 rounded-md bg-muted p-3">
                  <div className="mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                    <Route className="h-4 w-4" aria-hidden="true" />
                  </div>
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-sm font-medium text-foreground">
                        {step.step}
                      </span>
                      <span className="text-xs tabular-nums text-muted-foreground">
                        {step.time}
                      </span>
                    </div>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {step.detail}
                    </p>
                  </div>
                </li>
              ))}
            </ol>
          </OperationsPanel>

          <OperationsPanel
            title={t("defectPatterns")}
            description={t("defectDescription")}
          >
            <div className="overflow-hidden rounded-md border border-border">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted text-xs text-muted-foreground">
                  <tr>
                    <th scope="col" className="px-3 py-2 font-medium">
                      {t("columns.defect")}
                    </th>
                    <th scope="col" className="px-3 py-2 font-medium">
                      {t("columns.count")}
                    </th>
                    <th scope="col" className="px-3 py-2 font-medium">
                      {t("columns.operation")}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {defects.map((defect) => (
                    <tr key={defect.type}>
                      <td className="px-3 py-2 font-medium text-foreground">
                        {defect.type}
                      </td>
                      <td className="px-3 py-2 tabular-nums">{defect.count}</td>
                      <td className="px-3 py-2 text-muted-foreground">
                        {defect.operation}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </OperationsPanel>
        </div>
      </section>
    </OperationsPageShell>
  );
}
