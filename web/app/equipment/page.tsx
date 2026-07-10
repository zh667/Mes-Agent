import { Activity, Bell, Gauge } from "lucide-react";
import { useTranslations } from "next-intl";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

const oeeTrends = [
  { label: "Availability", value: 82 },
  { label: "Performance", value: 76 },
  { label: "Quality", value: 96 },
];

export default function EquipmentPage() {
  const t = useTranslations("equipment");
  const equipmentCards = [
    { name: t("preview.line1Press"), state: t("preview.running"), tone: "good" as const, oee: "84.2%", alarm: t("preview.clear") },
    { name: t("preview.line2Cnc"), state: t("preview.alarm"), tone: "danger" as const, oee: "61.5%", alarm: "A102" },
    { name: t("preview.line3Pack"), state: t("preview.idle"), tone: "warning" as const, oee: "77.8%", alarm: t("preview.materialWait") },
  ];
  const alarms = [
    { time: "09:42", equipment: t("preview.line2Cnc"), code: "A102", message: t("preview.sensorAnomaly") },
    { time: "08:55", equipment: t("preview.line3Pack"), code: "W018", message: t("preview.materialWait") },
    { time: "07:30", equipment: t("preview.line1Press"), code: "M004", message: t("preview.maintenanceCleared") },
  ];
  const trendLabels = [t("availability"), t("performance"), t("quality")];
  return (
    <OperationsPageShell
      title={t("title")}
      eyebrow={t("eyebrow")}
      description={t("description")}
      icon={Gauge}
      metrics={[
        { label: t("metrics.running"), value: "12" },
        { label: t("metrics.alarm"), value: "2" },
        { label: t("metrics.averageOee"), value: "78.4%" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="grid gap-4">
          <OperationsPanel
            title={t("realtimeStatus")}
            description={t("realtimeDescription")}
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
                        {t("currentAlarm", { alarm: item.alarm })}
                      </p>
                    </div>
                    <StatusPill
                      tone={item.tone}
                    >
                      {item.state}
                    </StatusPill>
                  </div>
                  <div className="mt-4 text-2xl font-semibold tabular-nums text-foreground">
                    {item.oee}
                  </div>
                  <div className="mt-1 text-xs text-muted-foreground">{t("oee")}</div>
                </article>
              ))}
            </div>
          </OperationsPanel>

          <OperationsPanel
            title={t("oeeTrend")}
            description={t("oeeDescription")}
          >
            <div className="grid gap-3">
              {oeeTrends.map((trend, index) => (
                <div key={trend.label}>
                  <div className="mb-1 flex justify-between text-xs">
                    <span className="text-muted-foreground">{trendLabels[index]}</span>
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
          title={t("alarmHistory")}
          description={t("alarmDescription")}
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
            {t("signalrReady")}
          </div>
        </OperationsPanel>
      </section>
    </OperationsPageShell>
  );
}
