import { Route, Search, ShieldCheck } from "lucide-react";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

const traceSteps = [
  { step: "WO dispatch", time: "07:55", detail: "Line 1 / OP-10 / Operator Li" },
  { step: "Material issue", time: "08:10", detail: "Batch MAT-AL-204 confirmed" },
  { step: "Inspection", time: "10:35", detail: "2 defects found at OP-20" },
];

const defects = [
  { type: "Surface scratch", count: "12", operation: "OP-20" },
  { type: "Dimension drift", count: "5", operation: "OP-30" },
  { type: "Missing label", count: "3", operation: "Packing" },
];

export default function QualityPage() {
  return (
    <OperationsPageShell
      title="Quality Trace"
      eyebrow="Quality Control"
      description="Trace batches across work orders, materials, operations, operators, equipment, and defect patterns."
      icon={ShieldCheck}
      metrics={[
        { label: "Inspections", value: "42" },
        { label: "Failed", value: "5" },
        { label: "Defects", value: "20" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[360px_minmax(0,1fr)]">
        <OperationsPanel
          title="Batch Lookup"
          description="Search by batch number to retrieve full production and quality trace."
        >
          <label className="relative block">
            <span className="sr-only">Search batch number</span>
            <Search
              className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden="true"
            />
            <input
              aria-label="Search batch number"
              defaultValue="B20260709-A102"
              className="min-h-11 w-full rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm outline-none transition-[border-color,box-shadow] duration-150 focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
          </label>
          <div className="mt-4 rounded-md bg-muted p-3 text-sm">
            <div className="flex items-center justify-between gap-3">
              <span className="font-medium text-foreground">Trace status</span>
              <StatusPill tone="warning">Partial review</StatusPill>
            </div>
            <p className="mt-2 text-muted-foreground">
              Material and operation records are complete. Defect disposition is
              pending supervisor approval.
            </p>
          </div>
        </OperationsPanel>

        <div className="grid gap-4">
          <OperationsPanel
            title="Trace Timeline"
            description="Chronological MES evidence for the selected batch."
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
            title="Defect Patterns"
            description="Top defect classes by count and operation."
          >
            <div className="overflow-hidden rounded-md border border-border">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted text-xs text-muted-foreground">
                  <tr>
                    <th scope="col" className="px-3 py-2 font-medium">
                      Defect
                    </th>
                    <th scope="col" className="px-3 py-2 font-medium">
                      Count
                    </th>
                    <th scope="col" className="px-3 py-2 font-medium">
                      Operation
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
