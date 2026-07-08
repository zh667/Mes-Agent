import {
  Bot,
  ClipboardList,
  Gauge,
  RadioTower,
  Search,
  ShieldCheck,
} from "lucide-react";

const modules = [
  {
    href: "/agent",
    label: "Agent Console",
    description: "Production, quality, OEE, and knowledge-base conversations",
    icon: Bot,
    status: "Online",
  },
  {
    href: "/workorders",
    label: "Work Orders",
    description: "Dispatch, report, complete, and review production progress",
    icon: ClipboardList,
    status: "Scaffolded",
  },
  {
    href: "/equipment",
    label: "Equipment OEE",
    description: "Monitor equipment state, downtime, alarms, and OEE changes",
    icon: Gauge,
    status: "Scaffolded",
  },
  {
    href: "/quality",
    label: "Quality Trace",
    description: "Trace batches, defects, materials, operators, and equipment",
    icon: ShieldCheck,
    status: "Scaffolded",
  },
  {
    href: "/knowledge",
    label: "Knowledge Base",
    description: "Manage SOP, maintenance, and process documents for RAG",
    icon: Search,
    status: "Scaffolded",
  },
];

const signals = [
  { label: "API contracts", value: "Typed client" },
  { label: "Realtime channel", value: "SignalR ready" },
  { label: "UI system", value: "Operational shell" },
  { label: "Package manager", value: "pnpm" },
];

export default function Home() {
  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 px-5 py-6 sm:px-8 lg:px-10">
        <header className="flex flex-col gap-4 border-b border-border pb-5 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-medium text-primary">MES Copilot</p>
            <h1 className="mt-2 text-3xl font-semibold tracking-normal text-foreground">
              Manufacturing Operations Workspace
            </h1>
          </div>
          <div className="flex items-center gap-2 rounded-md border border-border bg-card px-3 py-2 text-sm text-muted-foreground">
            <RadioTower className="h-4 w-4 text-accent" aria-hidden="true" />
            Operations workspace
          </div>
        </header>

        <section className="grid gap-5 lg:grid-cols-[1fr_320px]">
          <div className="grid gap-3 md:grid-cols-2">
            {modules.map((item) => {
              const Icon = item.icon;
              return (
                <a
                  key={item.href}
                  href={item.href}
                  className="group flex min-h-36 flex-col justify-between rounded-md border border-border bg-card p-4 transition-colors hover:border-primary"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-muted text-primary">
                      <Icon className="h-5 w-5" aria-hidden="true" />
                    </div>
                    <span className="rounded-sm border border-border px-2 py-1 text-xs font-medium text-muted-foreground">
                      {item.status}
                    </span>
                  </div>
                  <div>
                    <h2 className="text-base font-semibold text-foreground">
                      {item.label}
                    </h2>
                    <p className="mt-2 text-sm leading-6 text-muted-foreground">
                      {item.description}
                    </p>
                  </div>
                </a>
              );
            })}
          </div>

          <aside className="rounded-md border border-border bg-card p-4">
            <h2 className="text-sm font-semibold text-foreground">
              Foundation Status
            </h2>
            <dl className="mt-4 divide-y divide-border">
              {signals.map((signal) => (
                <div
                  key={signal.label}
                  className="flex items-start justify-between gap-4 py-3 first:pt-0 last:pb-0"
                >
                  <dt className="text-sm text-muted-foreground">
                    {signal.label}
                  </dt>
                  <dd className="text-right text-sm font-medium text-foreground">
                    {signal.value}
                  </dd>
                </div>
              ))}
            </dl>
          </aside>
        </section>
      </div>
    </main>
  );
}
