"use client";

import { DndContext, type DragEndEvent, useDraggable } from "@dnd-kit/core";
import { ChevronLeft, ChevronRight, GripVertical } from "lucide-react";
import { useTranslations } from "next-intl";

import type { GanttScheduleDto, ScheduledOperationDto } from "@/lib/hooks/use-scheduling";

export function GanttTimeline({ schedule, onMove }: { schedule: GanttScheduleDto; onMove: (operation: ScheduledOperationDto, minutes: number) => void }) {
  const t = useTranslations("scheduling");
  const from = new Date(schedule.from ?? 0).getTime();
  const to = new Date(schedule.to ?? 0).getTime();
  const duration = Math.max(1, to - from);
  const operations = (schedule.equipmentRows ?? []).flatMap((row) => row.operations ?? []);

  function dragEnd(event: DragEndEvent) {
    const operation = operations.find((item) => String(item.id) === String(event.active.id));
    if (!operation || event.delta.x === 0) return;
    const minutes = Math.round((event.delta.x / 960) * (duration / 60_000) / 15) * 15;
    if (minutes !== 0) onMove(operation, minutes);
  }

  if ((schedule.equipmentRows ?? []).length === 0) {
    return <div className="flex min-h-72 items-center justify-center border-y border-dashed border-border text-sm text-muted-foreground">{t("empty")}</div>;
  }

  return (
    <DndContext onDragEnd={dragEnd}>
      <div className="overflow-x-auto border-y border-border">
        <div className="min-w-[960px]">
          <div className="grid h-10 grid-cols-[140px_repeat(8,1fr)] items-center border-b border-border bg-muted/60 text-xs text-muted-foreground">
            <span className="px-3 font-medium">{t("equipment")}</span>
            {Array.from({ length: 8 }, (_, index) => <span key={index} className="border-l border-border px-2 tabular-nums">{new Date(from + (duration * index) / 8).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</span>)}
          </div>
          {(schedule.equipmentRows ?? []).map((row) => (
            <div key={row.equipmentId} className="grid h-16 grid-cols-[140px_minmax(0,1fr)] border-b border-border/70 last:border-0">
              <div className="flex flex-col justify-center px-3"><span className="font-mono text-xs font-semibold text-foreground">{row.equipmentCode}</span><span className="truncate text-xs text-muted-foreground">{row.equipmentName}</span></div>
              <div className="relative border-l border-border bg-[linear-gradient(to_right,hsl(var(--border))_1px,transparent_1px)] bg-[length:12.5%_100%]">
                {(row.operations ?? []).map((operation) => <OperationBar key={operation.id} operation={operation} from={from} duration={duration} onMove={onMove} moveLeftLabel={t("moveLeft", { code: operation.workOrderCode })} moveRightLabel={t("moveRight", { code: operation.workOrderCode })} />)}
              </div>
            </div>
          ))}
        </div>
      </div>
    </DndContext>
  );
}

function OperationBar({ operation, from, duration, onMove, moveLeftLabel, moveRightLabel }: { operation: ScheduledOperationDto; from: number; duration: number; onMove: (operation: ScheduledOperationDto, minutes: number) => void; moveLeftLabel: string; moveRightLabel: string }) {
  const draggable = useDraggable({ id: String(operation.id), data: operation });
  const start = new Date(operation.plannedStartTime ?? 0).getTime();
  const end = new Date(operation.plannedEndTime ?? 0).getTime();
  const left = Math.max(0, ((start - from) / duration) * 100);
  const width = Math.max(1.5, ((end - start) / duration) * 100);
  return (
    <div ref={draggable.setNodeRef} style={{ left: `${left}%`, width: `${Math.min(width, 100 - left)}%`, transform: draggable.transform ? `translate3d(${draggable.transform.x}px,0,0)` : undefined }} className="absolute top-2 flex h-12 min-w-28 items-center overflow-hidden rounded-md bg-primary text-primary-foreground shadow-sm">
      <button type="button" aria-label={moveLeftLabel} onClick={() => onMove(operation, -15)} className="flex h-full w-8 shrink-0 items-center justify-center hover:bg-black/10"><ChevronLeft className="h-4 w-4" aria-hidden="true" /></button>
      <button type="button" {...draggable.listeners} {...draggable.attributes} className="flex min-w-0 flex-1 cursor-grab items-center gap-1 px-1 text-left active:cursor-grabbing"><GripVertical className="h-4 w-4 shrink-0" aria-hidden="true" /><span className="min-w-0"><span className="block truncate text-xs font-semibold">{operation.workOrderCode}</span><span className="block truncate text-[11px] opacity-80">{operation.processStepName}</span></span></button>
      <button type="button" aria-label={moveRightLabel} onClick={() => onMove(operation, 15)} className="flex h-full w-8 shrink-0 items-center justify-center hover:bg-black/10"><ChevronRight className="h-4 w-4" aria-hidden="true" /></button>
    </div>
  );
}
