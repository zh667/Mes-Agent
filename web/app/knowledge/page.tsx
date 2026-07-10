import { FileText, Search, Upload } from "lucide-react";
import { useTranslations } from "next-intl";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

export default function KnowledgePage() {
  const t = useTranslations("knowledge");
  const documents = [
    { name: t("preview.alarmSop"), type: "SOP", chunks: "18", status: t("preview.indexed"), tone: "good" as const },
    { name: t("preview.maintenanceManual"), type: t("preview.manual"), chunks: "42", status: t("preview.indexed"), tone: "good" as const },
    { name: t("preview.qualityStandard"), type: t("preview.standard"), chunks: "24", status: t("preview.review"), tone: "warning" as const },
  ];
  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={Search}
      metrics={[
        { label: t("metrics.documents"), value: "36" },
        { label: t("metrics.chunks"), value: "1,284" },
        { label: t("metrics.indexed"), value: "94%" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[360px_minmax(0,1fr)]">
        <OperationsPanel
          title={t("intake")}
          description={t("intakeDescription")}
        >
          <button className="flex min-h-11 w-full items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-[background-color,scale] duration-150 hover:bg-primary/90 active:scale-[0.96]">
            <Upload className="h-4 w-4" aria-hidden="true" />
            {t("upload")}
          </button>
          <label className="relative mt-3 block">
            <span className="sr-only">{t("search")}</span>
            <Search
              className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden="true"
            />
            <input
              aria-label={t("search")}
              defaultValue={t("searchExample")}
              className="min-h-11 w-full rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm outline-none transition-[border-color,box-shadow] duration-150 focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
          </label>
          <div className="mt-4 rounded-md bg-muted p-3 text-sm text-muted-foreground">
            {t("queryPreview")}
          </div>
        </OperationsPanel>

        <OperationsPanel
          title={t("library")}
          description={t("libraryDescription")}
        >
          <div className="grid gap-3">
            {documents.map((document) => (
              <article
                key={document.name}
                className="flex flex-col gap-3 rounded-md bg-muted p-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="flex items-start gap-3">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-background text-primary">
                    <FileText className="h-4 w-4" aria-hidden="true" />
                  </div>
                  <div>
                    <h3 className="text-sm font-medium text-foreground">
                      {document.name}
                    </h3>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {document.type} · {t("chunkCount", { count: document.chunks })}
                    </p>
                  </div>
                </div>
                <StatusPill tone={document.tone}>
                  {document.status}
                </StatusPill>
              </article>
            ))}
          </div>
        </OperationsPanel>
      </section>
    </OperationsPageShell>
  );
}
