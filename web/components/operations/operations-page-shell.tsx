import type { LucideIcon } from "lucide-react";
import Link from "next/link";
import type { ReactNode } from "react";

type PageMetric = {
  label: string;
  value: string;
};

type OperationsPageShellProps = {
  title: string;
  eyebrow: string;
  description: string;
  icon: LucideIcon;
  metrics: PageMetric[];
  children: ReactNode;
};

export function OperationsPageShell({
  title,
  eyebrow,
  description,
  icon: Icon,
  metrics,
  children,
}: OperationsPageShellProps) {
  return (
    <main className="min-h-dvh bg-background">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-5 px-4 py-5 sm:px-6 lg:px-8">
        <Link
          href="/"
          className="w-fit text-sm font-medium text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          MES Copilot
        </Link>
        <header className="flex flex-col gap-4 border-b border-border pb-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="flex items-center gap-2 text-sm font-medium text-primary">
              <Icon className="h-4 w-4" aria-hidden="true" />
              {eyebrow}
            </p>
            <h1 className="mt-2 text-balance text-2xl font-semibold tracking-normal text-foreground sm:text-3xl">
              {title}
            </h1>
            <p className="mt-2 max-w-2xl text-pretty text-sm leading-6 text-muted-foreground">
              {description}
            </p>
          </div>
          <dl className="grid gap-2 rounded-md bg-muted p-2 text-center sm:grid-cols-3 lg:w-[420px]">
            {metrics.map((metric) => (
              <div key={metric.label} className="rounded-sm bg-background px-2 py-2">
                <dt className="text-xs text-muted-foreground">{metric.label}</dt>
                <dd className="mt-1 text-sm font-semibold tabular-nums text-foreground">
                  {metric.value}
                </dd>
              </div>
            ))}
          </dl>
        </header>
        {children}
      </div>
    </main>
  );
}

export function OperationsPanel({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <section className="rounded-lg bg-card p-4 shadow-[0_0_0_1px_rgba(0,0,0,0.06),0_2px_4px_rgba(0,0,0,0.04)]">
      <div>
        <h2 className="text-sm font-semibold text-foreground">{title}</h2>
        {description ? (
          <p className="mt-1 text-xs leading-5 text-muted-foreground">
            {description}
          </p>
        ) : null}
      </div>
      <div className="mt-4">{children}</div>
    </section>
  );
}

export function StatusPill({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: "neutral" | "good" | "warning" | "danger";
}) {
  const toneClass =
    tone === "good"
      ? "bg-primary/10 text-primary"
      : tone === "warning"
        ? "bg-secondary/20 text-secondary-foreground"
        : tone === "danger"
          ? "bg-destructive/10 text-destructive"
          : "bg-muted text-muted-foreground";

  return (
    <span className={`rounded-sm px-2 py-1 text-xs font-medium ${toneClass}`}>
      {children}
    </span>
  );
}
