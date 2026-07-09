import { Activity, Bell, Gauge } from "lucide-react";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

const equipmentCards = [
  { name: "Line 1 Press", state: "Running", oee: "84.2%", alarm: "Clear" },
  { name: "Line 2 CNC", state: "Alarm", oee: "61.5%", alarm: "A102" },
  { name: "Line 3 Pack", state: "Idle", oee: "77.8%", alarm: "Material wait" },
];

const alarms = [
  { time: "09:42", equipment: "Line 2 CNC", code: "A102", message: "Sensor anomaly" },
  { time: "08:55", equipment: "Line 3 Pack", code: "W018", message: "Material wait" },
  { time: "07:30", equipment: "Line 1 Press", code: "M004", message: "Maintenance cleared" },
];

const oeeTrends = [
  { label: "Availability", value: 82 },
  { label: "Performance", value: 76 },
  { label: "Quality", value: 96 },
];

export default function EquipmentPage() {
  return (
    <OperationsPageShell
      title="Equipment OEE"
      eyebrow="Realtime Equipment"
      description="Monitor equipment state, downtime, alarm history, and OEE trends in one shop-floor dashboard."
      icon={Gauge}
      metrics={[
        { label: "Running", value: "12" },
        { label: "Alarm", value: "2" },
        { label: "Avg OEE", value: "78.4%" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="grid gap-4">
          <OperationsPanel
            title="Realtime Status"
            description="SignalR-ready card layout for live equipment state updates."
          >
            <div className="grid gap-3 md:grid-cols-3">
              {equipmentCards.map((item) => (
                <article key={item.name} className="rounded-md bg-muted p-3">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <h3 className="text-sm font-semibold text-foreground">
                        {item.name}
                      </h3>
                      <p className="mt-1 text-xs text-muted-foreground">
                        Current alarm: {item.alarm}
                      </p>
                    </div>
                    <StatusPill
                      tone={
                        item.state === "Running"
                          ? "good"
                          : item.state === "Alarm"
                            ? "danger"
                            : "warning"
                      }
                    >
                      {item.state}
                    </StatusPill>
                  </div>
                  <div className="mt-4 text-2xl font-semibold tabular-nums text-foreground">
                    {item.oee}
                  </div>
                  <div className="mt-1 text-xs text-muted-foreground">OEE</div>
                </article>
              ))}
            </div>
          </OperationsPanel>

          <OperationsPanel
            title="OEE Trend"
            description="Compact trend bars for availability, performance, and quality."
          >
            <div className="grid gap-3">
              {oeeTrends.map((trend) => (
                <div key={trend.label}>
                  <div className="mb-1 flex justify-between text-xs">
                    <span className="text-muted-foreground">{trend.label}</span>
                    <span className="font-medium tabular-nums text-foreground">
                      {trend.value}%
                    </span>
                  </div>
                  <div className="h-2 rounded-full bg-muted">
                    <div
                      className="h-2 rounded-full bg-primary"
                      style={{ width: `${trend.value}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </OperationsPanel>
        </div>

        <OperationsPanel
          title="Alarm History"
          description="Recent alarms and downtime reasons for supervisor review."
        >
          <div className="space-y-3">
            {alarms.map((alarm) => (
              <article key={`${alarm.time}-${alarm.code}`} className="rounded-md bg-muted p-3">
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-2 text-sm font-medium text-foreground">
                    <Bell className="h-4 w-4 text-destructive" aria-hidden="true" />
                    {alarm.code}
                  </div>
                  <span className="text-xs tabular-nums text-muted-foreground">
                    {alarm.time}
                  </span>
                </div>
                <p className="mt-2 text-sm text-muted-foreground">
                  {alarm.equipment} - {alarm.message}
                </p>
              </article>
            ))}
          </div>
          <div className="mt-4 flex items-center gap-2 rounded-md bg-background p-3 text-sm text-muted-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.06)]">
            <Activity className="h-4 w-4 text-primary" aria-hidden="true" />
            SignalR connection placeholder ready for live patch events.
          </div>
        </OperationsPanel>
      </section>
    </OperationsPageShell>
  );
}
