import { ClipboardCheck, ClipboardList, Filter, Play } from "lucide-react";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

const workOrders = [
  {
    code: "WO-20260709-014",
    product: "Valve Assembly",
    line: "Line 1",
    status: "In production",
    progress: "64%",
    planned: "1,200",
    completed: "768",
    risk: "Material shortage",
  },
  {
    code: "WO-20260709-018",
    product: "Pump Housing",
    line: "Line 2",
    status: "Paused",
    progress: "72%",
    planned: "800",
    completed: "576",
    risk: "Downtime review",
  },
  {
    code: "WO-20260709-021",
    product: "Sensor Bracket",
    line: "Line 3",
    status: "Ready",
    progress: "0%",
    planned: "2,400",
    completed: "0",
    risk: "None",
  },
];

export default function WorkOrdersPage() {
  return (
    <OperationsPageShell
      title="Work Orders"
      eyebrow="Production Control"
      description="Dispatch, report, complete, and review shop-floor progress with a dense operator-ready work order board."
      icon={ClipboardList}
      metrics={[
        { label: "Open orders", value: "24" },
        { label: "Delayed", value: "7" },
        { label: "Output today", value: "8,420" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_340px]">
        <OperationsPanel
          title="Dispatch Board"
          description="Filter active orders, inspect progress, and take the next shop-floor action."
        >
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <label className="relative block flex-1">
              <span className="sr-only">Filter work orders</span>
              <Filter
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                aria-hidden="true"
              />
              <input
                aria-label="Filter work orders"
                defaultValue="Line 1 / delayed / today"
                className="min-h-11 w-full rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm outline-none transition-[border-color,box-shadow] duration-150 focus:border-ring focus:ring-2 focus:ring-ring/20"
              />
            </label>
            <div className="flex gap-2">
              <button className="min-h-11 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-[background-color,scale] duration-150 hover:bg-primary/90 active:scale-[0.96]">
                Start
              </button>
              <button className="min-h-11 rounded-md bg-muted px-4 text-sm font-medium text-foreground transition-[background-color,scale] duration-150 hover:bg-border active:scale-[0.96]">
                Report
              </button>
              <button className="min-h-11 rounded-md bg-muted px-4 text-sm font-medium text-foreground transition-[background-color,scale] duration-150 hover:bg-border active:scale-[0.96]">
                Complete
              </button>
            </div>
          </div>
          <div className="overflow-hidden rounded-md border border-border">
            <table className="w-full text-left text-sm">
              <thead className="bg-muted text-xs text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 font-medium">Work order</th>
                  <th className="px-3 py-2 font-medium">Line</th>
                  <th className="px-3 py-2 font-medium">Status</th>
                  <th className="px-3 py-2 font-medium">Progress</th>
                  <th className="px-3 py-2 font-medium">Risk</th>
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
                        tone={order.status === "Paused" ? "danger" : "neutral"}
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
          title="Operation Detail"
          description="Current operation, quantity, and next control action."
        >
          <div className="space-y-3 text-sm">
            <div className="rounded-md bg-muted p-3">
              <div className="flex items-center gap-2 font-medium text-foreground">
                <Play className="h-4 w-4 text-primary" aria-hidden="true" />
                Assembly step running
              </div>
              <p className="mt-2 text-muted-foreground">
                Operator Chen is reporting output against operation OP-20.
              </p>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <Detail label="Planned" value="1,200" />
              <Detail label="Completed" value="768" />
              <Detail label="Good qty" value="742" />
              <Detail label="Scrap" value="26" />
            </div>
            <div className="flex items-center gap-2 rounded-md bg-background p-3 shadow-[0_0_0_1px_rgba(0,0,0,0.06)]">
              <ClipboardCheck className="h-4 w-4 text-primary" aria-hidden="true" />
              <span className="text-muted-foreground">
                Completion requires quality confirmation.
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
