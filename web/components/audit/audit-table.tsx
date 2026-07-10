"use client";

import { useQuery } from "@tanstack/react-query";
import { Download, LoaderCircle, X } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";

import { StatusPill } from "@/components/operations/operations-page-shell";
import { apiClient } from "@/lib/api-client";
import type { components } from "@/shared/api/generated/schema";

type AuditCategory = "operations" | "changes" | "agent";
type AuditLogDto = components["schemas"]["AuditLogDto"];
type DataChangeLogDto = components["schemas"]["DataChangeLogDto"];
type AgentAuditLogDto = components["schemas"]["AgentAuditLogDto"];
type AuditPage =
  | components["schemas"]["AuditLogDtoPagedAuditResultDto"]
  | components["schemas"]["DataChangeLogDtoPagedAuditResultDto"]
  | components["schemas"]["AgentAuditLogDtoPagedAuditResultDto"];
type AuditItem = AuditLogDto | DataChangeLogDto | AgentAuditLogDto;

const categories: AuditCategory[] = ["operations", "changes", "agent"];

export function AuditTable() {
  const t = useTranslations("audit");
  const common = useTranslations("common");
  const locale = useLocale();
  const [category, setCategory] = useState<AuditCategory>("operations");
  const [from, setFrom] = useState(isoDateDaysAgo(7));
  const [to, setTo] = useState(isoDateDaysAgo(0));
  const [skip, setSkip] = useState(0);
  const [format, setFormat] = useState<"xlsx" | "pdf">("xlsx");
  const [selected, setSelected] = useState<AuditItem | null>(null);
  const take = 25;

  const query = useQuery({
    queryKey: ["audit", category, from, to, skip, take],
    queryFn: async () =>
      (await apiClient.get<AuditPage>(`/audit/${category}`, {
        params: { from, to, skip, take },
      })).data,
  });

  const items = useMemo(() => (query.data?.items ?? []) as AuditItem[], [query.data]);
  const total = query.data?.total ?? 0;

  function changeCategory(next: AuditCategory) {
    setCategory(next);
    setSkip(0);
    setSelected(null);
  }

  async function exportAudit() {
    const response = await apiClient.get<Blob>("/audit/report", {
      params: { category, format, from, to },
      responseType: "blob",
    });
    const url = URL.createObjectURL(response.data);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `audit-${category}-${from}-${to}.${format}`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 border-b border-border pb-4 xl:flex-row xl:items-end xl:justify-between">
        <div role="tablist" aria-label={t("categoryLabel")} className="flex min-w-0 gap-1 overflow-x-auto">
          {categories.map((item) => (
            <button
              key={item}
              type="button"
              role="tab"
              aria-selected={category === item}
              onClick={() => changeCategory(item)}
              className={`h-10 shrink-0 border-b-2 px-3 text-sm font-medium transition-colors ${
                category === item
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground"
              }`}
            >
              {t(`categories.${item}`)}
            </button>
          ))}
        </div>
        <div className="flex flex-wrap items-end gap-2">
          <DateFilter label={t("from")} value={from} onChange={(value) => { setFrom(value); setSkip(0); }} />
          <DateFilter label={t("to")} value={to} onChange={(value) => { setTo(value); setSkip(0); }} />
          <label className="text-xs font-medium text-muted-foreground">
            {t("format")}
            <select
              aria-label={t("exportFormat")}
              value={format}
              onChange={(event) => setFormat(event.target.value as "xlsx" | "pdf")}
              className="mt-1 block h-10 rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <option value="xlsx">{t("excel")}</option>
              <option value="pdf">{t("pdf")}</option>
            </select>
          </label>
          <button
            type="button"
            aria-label={t("exportAudit")}
            onClick={() => void exportAudit()}
            className="flex h-10 items-center gap-2 rounded-md bg-primary px-3 text-sm font-semibold text-primary-foreground transition-transform active:scale-[0.96]"
          >
            <Download className="h-4 w-4" aria-hidden="true" />
            {t("export")}
          </button>
        </div>
      </div>

      <div className="grid min-h-[420px] gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0 overflow-x-auto">
          {query.isLoading ? (
            <p role="status" className="flex h-40 items-center justify-center gap-2 text-sm text-muted-foreground">
              <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" /> {t("loading")}
            </p>
          ) : query.isError ? (
            <p role="alert" className="py-10 text-center text-sm text-destructive">{t("loadError")}</p>
          ) : items.length === 0 ? (
            <p className="py-10 text-center text-sm text-muted-foreground">{t("empty")}</p>
          ) : (
            <table className="w-full min-w-[760px] border-collapse text-left text-sm">
              <thead className="text-xs uppercase text-muted-foreground">
                <tr className="border-b border-border">
                  <th scope="col" className="px-2 py-3 font-medium">{t("columns.time")}</th>
                  <th scope="col" className="px-2 py-3 font-medium">{t("columns.actor")}</th>
                  <th scope="col" className="px-2 py-3 font-medium">{t("columns.action")}</th>
                  <th scope="col" className="px-2 py-3 font-medium">{t("columns.target")}</th>
                  <th scope="col" className="px-2 py-3 font-medium">{t("columns.status")}</th>
                  <th scope="col" className="px-2 py-3 text-right font-medium">{t("columns.duration")}</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => {
                  const row = toRow(item, category, {
                    anonymous: t("anonymous"),
                    system: t("system"),
                    recorded: t("recorded"),
                    newConversation: t("newConversation"),
                    unknown: t("unknown"),
                  });
                  return (
                    <tr
                      key={item.id}
                      onClick={() => setSelected(item)}
                      className="cursor-pointer border-b border-border/70 transition-colors hover:bg-muted/60 last:border-0"
                    >
                      <td className="whitespace-nowrap px-2 py-3 text-xs tabular-nums text-muted-foreground">{formatTimestamp(item.timestamp, locale)}</td>
                      <td className="max-w-40 truncate px-2 py-3 text-xs text-muted-foreground">{row.actor}</td>
                      <td className="px-2 py-3 font-medium text-foreground">{row.action}</td>
                      <td className="max-w-72 truncate px-2 py-3 font-mono text-xs text-foreground">{row.target}</td>
                      <td className="px-2 py-3"><AuditStatus value={row.status} /></td>
                      <td className="px-2 py-3 text-right text-xs tabular-nums text-muted-foreground">{t("milliseconds", { value: row.duration })}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>

        <aside className="border-l border-border pl-4" aria-label={t("detailsLabel")}>
          <div className="flex h-10 items-center justify-between">
            <h3 className="text-sm font-semibold text-foreground">{t("recordDetails")}</h3>
            {selected ? (
              <button type="button" aria-label={t("closeDetails")} onClick={() => setSelected(null)} className="flex h-10 w-10 items-center justify-center rounded-md text-muted-foreground hover:bg-muted hover:text-foreground">
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            ) : null}
          </div>
          {selected ? (
            <dl className="mt-3 space-y-3 text-xs">
              {Object.entries(selected).map(([key, value]) => (
                <div key={key} className="border-b border-border/70 pb-3">
                  <dt className="font-medium text-muted-foreground">{key}</dt>
                  <dd className="mt-1 break-words font-mono leading-5 text-foreground">{renderValue(value)}</dd>
                </div>
              ))}
            </dl>
          ) : (
            <p className="mt-3 text-xs leading-5 text-muted-foreground">{t("selectDetails")}</p>
          )}
        </aside>
      </div>

      <div className="flex items-center justify-between border-t border-border pt-3 text-xs text-muted-foreground">
        <span>{t("showing", { visible: items.length, total })}</span>
        <div className="flex gap-2">
          <button type="button" disabled={skip === 0} onClick={() => setSkip(Math.max(0, skip - take))} className="h-10 rounded-md px-3 font-medium hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40">{common("previous")}</button>
          <button type="button" disabled={skip + take >= total} onClick={() => setSkip(skip + take)} className="h-10 rounded-md px-3 font-medium hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40">{common("next")}</button>
        </div>
      </div>
    </div>
  );
}

function DateFilter({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label className="text-xs font-medium text-muted-foreground">
      {label}
      <input type="date" value={value} onChange={(event) => onChange(event.target.value)} className="mt-1 block h-10 rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring" />
    </label>
  );
}

function AuditStatus({ value }: { value: string }) {
  const normalized = value.toLowerCase();
  const tone = normalized === "completed" || normalized.startsWith("2") || normalized === "recorded"
    ? "good"
    : normalized === "failed" || normalized.startsWith("5")
      ? "danger"
      : normalized === "canceled" || normalized.startsWith("4")
        ? "warning"
        : "neutral";
  return <StatusPill tone={tone}>{value}</StatusPill>;
}

type AuditFallbacks = {
  anonymous: string;
  system: string;
  recorded: string;
  newConversation: string;
  unknown: string;
};

function toRow(item: AuditItem, category: AuditCategory, fallbacks: AuditFallbacks) {
  if (category === "operations") {
    const operation = item as AuditLogDto;
    return { actor: operation.userId ?? fallbacks.anonymous, action: operation.method ?? "", target: operation.routeTemplate ?? "", status: String(operation.statusCode ?? ""), duration: operation.durationMilliseconds ?? 0 };
  }
  if (category === "changes") {
    const change = item as DataChangeLogDto;
    return { actor: change.userId ?? fallbacks.system, action: change.changeType ?? "", target: `${change.entityType ?? ""}:${change.entityId ?? ""}`, status: fallbacks.recorded, duration: 0 };
  }
  const agent = item as AgentAuditLogDto;
  return { actor: agent.userId ?? "", action: agent.toolName ?? String(agent.mode ?? "Agent"), target: agent.conversationId ?? fallbacks.newConversation, status: agent.status ?? fallbacks.unknown, duration: agent.durationMilliseconds ?? 0 };
}

function formatTimestamp(value: string | undefined, locale: string) {
  return value ? new Intl.DateTimeFormat(locale, { dateStyle: "short", timeStyle: "medium" }).format(new Date(value)) : "-";
}

function renderValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

function isoDateDaysAgo(days: number) {
  const date = new Date();
  date.setUTCDate(date.getUTCDate() - days);
  return date.toISOString().slice(0, 10);
}
