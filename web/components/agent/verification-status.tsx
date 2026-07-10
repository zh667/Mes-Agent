"use client";

import {
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  CircleHelp,
} from "lucide-react";
import { useState } from "react";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";
import type { components } from "@/shared/api/generated/schema";

export type VerificationResultDto =
  components["schemas"]["VerificationResultDto"];

const statusPresentation = {
  Verified: {
    icon: CheckCircle2,
    className: "border-emerald-600/30 bg-emerald-500/10 text-emerald-800",
  },
  Disputed: {
    icon: AlertTriangle,
    className: "border-amber-600/30 bg-amber-500/10 text-amber-900",
  },
  Unverified: {
    icon: CircleHelp,
    className: "border-border bg-muted text-muted-foreground",
  },
} as const;

export function VerificationStatus({ result }: { result: VerificationResultDto }) {
  const t = useTranslations("agent");
  const [expanded, setExpanded] = useState(false);
  const status = normalizeStatus(result.status);
  const presentation = statusPresentation[status];
  const Icon = presentation.icon;

  return (
    <section className="border-y border-border bg-background px-4 py-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div
          role="status"
          aria-label={t("verificationStatus", { status: t(status.toLowerCase()) })}
          className={cn(
            "inline-flex min-h-8 items-center gap-2 rounded-md border px-2.5 text-xs font-semibold",
            presentation.className,
          )}
        >
          <Icon className="h-4 w-4" aria-hidden="true" />
          {t(status.toLowerCase())}
        </div>
        {result.checks.length > 0 ? (
          <button
            type="button"
            aria-expanded={expanded}
            onClick={() => setExpanded((current) => !current)}
            className="inline-flex min-h-9 items-center gap-1.5 rounded-md px-2 text-xs font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            {t("details")}
            <ChevronDown
              className={cn("h-4 w-4 transition-transform", expanded && "rotate-180")}
              aria-hidden="true"
            />
          </button>
        ) : null}
      </div>
      <p className="mt-2 text-xs leading-5 text-muted-foreground">{result.summary}</p>
      {expanded ? (
        <ul className="mt-3 divide-y divide-border border-t border-border">
          {result.checks.map((check, index) => (
            <li key={`${check.rule}-${check.claim}-${index}`} className="grid gap-1 py-3 text-xs">
              <div className="flex items-start justify-between gap-3">
                <span className="font-medium text-foreground">{check.claim}</span>
                <span className="text-muted-foreground">{check.status}</span>
              </div>
              <div className="flex flex-wrap gap-x-4 gap-y-1 tabular-nums text-muted-foreground">
                <span>{t("claimed", { value: check.claimedValue })}</span>
                <span>{t("actual", { value: check.actualValue })}</span>
              </div>
              <code className="w-fit text-[11px] text-muted-foreground">{check.rule}</code>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

function normalizeStatus(status: string) {
  return status === "Verified" || status === "Disputed" ? status : "Unverified";
}
