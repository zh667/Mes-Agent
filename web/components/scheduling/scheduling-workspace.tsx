"use client";

import { CalendarRange, LoaderCircle, WandSparkles } from "lucide-react";
import { useTranslations } from "next-intl";
import { type FormEvent, useState } from "react";

import { GanttTimeline } from "@/components/scheduling/gantt-timeline";
import { useAdjustOperation, useGantt, useGenerateSchedule, type ScheduledOperationDto } from "@/lib/hooks/use-scheduling";

export function SchedulingWorkspace() {
  const t = useTranslations("scheduling");
  const [from, setFrom] = useState(startOfToday());
  const [to, setTo] = useState(daysFromNow(2));
  const [workOrderIds, setWorkOrderIds] = useState("");
  const gantt = useGantt(from, to);
  const generate = useGenerateSchedule();
  const adjust = useAdjustOperation();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const ids = workOrderIds.split(/[,\s]+/).map(Number).filter((value) => Number.isInteger(value) && value > 0);
    if (ids.length > 0 && ids.length <= 50) generate.mutate({ workOrderIds: ids, scheduleStartUtc: from });
  }

  function move(operation: ScheduledOperationDto, minutes: number) {
    const start = new Date(operation.plannedStartTime ?? 0);
    const end = new Date(operation.plannedEndTime ?? 0);
    start.setUTCMinutes(start.getUTCMinutes() + minutes);
    end.setUTCMinutes(end.getUTCMinutes() + minutes);
    adjust.mutate({ id: operation.id ?? 0, request: { equipmentId: operation.equipmentId ?? 0, plannedStartUtc: start.toISOString(), plannedEndUtc: end.toISOString(), version: operation.version ?? 0 } });
  }

  return (
    <div className="space-y-5">
      <form onSubmit={submit} className="grid gap-3 border-b border-border pb-5 xl:grid-cols-[minmax(260px,1fr)_220px_220px_auto] xl:items-end">
        <label className="text-xs font-medium text-muted-foreground">{t("workOrderIds")}
          <input aria-label={t("workOrderIds")} placeholder="101, 102, 103" value={workOrderIds} onChange={(event) => setWorkOrderIds(event.target.value)} className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring" />
        </label>
        <DateTimeField label={t("windowStart")} value={from} onChange={setFrom} />
        <DateTimeField label={t("windowEnd")} value={to} onChange={setTo} />
        <button type="submit" disabled={generate.isPending || !workOrderIds.trim()} className="flex h-10 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground transition-transform active:scale-[0.96] disabled:opacity-50">
          {generate.isPending ? <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" /> : <WandSparkles className="h-4 w-4" aria-hidden="true" />}{t("generate")}
        </button>
      </form>
      {generate.isError ? <p role="alert" className="text-sm text-destructive">{t("generateError")}</p> : null}
      {adjust.isError ? <p role="alert" className="text-sm text-destructive">{t("adjustError")}</p> : null}
      {gantt.isLoading ? <p role="status" className="flex min-h-72 items-center justify-center gap-2 text-sm text-muted-foreground"><LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />{t("loading")}</p> : gantt.data ? <GanttTimeline schedule={gantt.data} onMove={move} /> : null}
      <p className="flex items-center gap-2 text-xs text-muted-foreground"><CalendarRange className="h-4 w-4" aria-hidden="true" />{t("timezoneNote")}</p>
    </div>
  );
}

function DateTimeField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return <label className="text-xs font-medium text-muted-foreground">{label}<input type="datetime-local" value={toLocalInput(value)} onChange={(event) => onChange(new Date(event.target.value).toISOString())} className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring" /></label>;
}

function startOfToday() { const date = new Date(); date.setHours(0, 0, 0, 0); return date.toISOString(); }
function daysFromNow(days: number) { const date = new Date(); date.setDate(date.getDate() + days); date.setHours(23, 59, 0, 0); return date.toISOString(); }
function toLocalInput(value: string) { const date = new Date(value); const offset = date.getTimezoneOffset() * 60_000; return new Date(date.getTime() - offset).toISOString().slice(0, 16); }
