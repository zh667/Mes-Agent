"use client";

import {
  Activity,
  Bot,
  Bug,
  ChevronDown,
  ClipboardList,
  Database,
  Gauge,
  Search,
  Send,
  ShieldCheck,
} from "lucide-react";
import {
  FormEvent,
  useRef,
  useState,
} from "react";
import { useTranslations } from "next-intl";

import {
  useAgentStream,
  type AgentStreamEvent,
} from "@/lib/hooks/use-agent-stream";
import {
  VerificationStatus,
  type VerificationResultDto,
} from "@/components/agent/verification-status";
import { cn } from "@/lib/utils";

type AgentMode = "production" | "quality" | "oee" | "knowledge";

type ChatMessage = {
  id: string;
  role: "user" | "agent";
  mode: AgentMode;
  content: string;
  timestamp: string;
};

type StructuredRow = {
  workOrder: string;
  product: string;
  status: string;
  progress: string;
  risk: string;
};

type AgentDebug = {
  tool: string;
  dataSource: string;
  latency: string;
};

type StructuredResult = {
  summary: string;
  rows: StructuredRow[];
  debug: AgentDebug;
};

type StructuredResultLabels = {
  live: string;
  empty: string;
  unknown: string;
  unknownProduct: string;
  open: string;
  none: string;
  agentTool: string;
  dataSource: string;
  noRows: string;
  noRowsDescription: string;
  emptyStatus: string;
};

type AgentModeConfig = {
  id: AgentMode;
  label: string;
  description: string;
  icon: typeof Bot;
};

const agentModeDefinitions = [
  {
    id: "production",
    icon: ClipboardList,
  },
  {
    id: "quality",
    icon: ShieldCheck,
  },
  {
    id: "oee",
    icon: Gauge,
  },
  {
    id: "knowledge",
    icon: Search,
  },
] as const;

const agentModeValueById: Record<AgentMode, number> = {
  production: 0,
  quality: 1,
  oee: 2,
  knowledge: 3,
};

export function ChatInterface() {
  const t = useTranslations("agent");
  const common = useTranslations("common");
  const agentModes: [AgentModeConfig, ...AgentModeConfig[]] = agentModeDefinitions.map(
    ({ id, icon }) => ({
      id,
      icon,
      label: t(`modes.${id}.label`),
      description: t(`modes.${id}.description`),
    }),
  ) as [AgentModeConfig, ...AgentModeConfig[]];
  const defaultAgentMode = agentModes[0];
  const samplePrompts = [
    t("samplePrompts.delayed"),
    t("samplePrompts.trace"),
    t("samplePrompts.oee"),
  ];
  const [activeMode, setActiveMode] = useState<AgentMode>("production");
  const [messages, setMessages] = useState<ChatMessage[]>(() => [
    {
      id: "agent-seed",
      role: "agent",
      mode: "production",
      content: t("seedMessage"),
      timestamp: "08:30",
    },
  ]);
  const [prompt, setPrompt] = useState("");
  const [thinkingMessage, setThinkingMessage] = useState("");
  const [showDebug, setShowDebug] = useState(false);
  const [structuredResult, setStructuredResult] = useState<StructuredResult>(() => ({
    summary: t("structured.preview"),
    debug: {
      tool: "GetTodayWorkOrders",
      dataSource: "MES.WorkOrders",
      latency: "142 ms",
    },
    rows: [
      {
        workOrder: "WO-20260709-014",
        product: t("structured.previewRows.valveAssembly.product"),
        status: t("structured.previewRows.valveAssembly.status"),
        progress: "64%",
        risk: t("structured.previewRows.valveAssembly.risk"),
      },
      {
        workOrder: "WO-20260709-018",
        product: t("structured.previewRows.pumpHousing.product"),
        status: t("structured.previewRows.pumpHousing.status"),
        progress: "72%",
        risk: t("structured.previewRows.pumpHousing.risk"),
      },
      {
        workOrder: "WO-20260709-021",
        product: t("structured.previewRows.sensorBracket.product"),
        status: t("structured.previewRows.sensorBracket.status"),
        progress: "91%",
        risk: t("structured.previewRows.sensorBracket.risk"),
      },
    ],
  }));
  const [verification, setVerification] = useState<VerificationResultDto | null>(null);
  const messageSequenceRef = useRef(1);

  const activeAgent =
    agentModes.find((mode) => mode.id === activeMode) ?? defaultAgentMode;

  const { streamChat, stopStreaming, isStreaming } = useAgentStream({
    onEvent: (event: AgentStreamEvent) => {
      if (event.type === "thinking") {
        setThinkingMessage(event.content || t("checkingSignals"));
        return;
      }

      if (event.type === "tool_result") {
        setStructuredResult(toStructuredResult(event.data, {
          live: t("structured.live"),
          empty: t("structured.empty"),
          unknown: t("structured.unknown"),
          unknownProduct: t("structured.unknownProduct"),
          open: t("structured.open"),
          none: t("structured.none"),
          agentTool: t("debug.agentTool"),
          dataSource: t("debug.mesApi"),
          noRows: t("structured.noRows"),
          noRowsDescription: t("structured.noRowsDescription"),
          emptyStatus: t("structured.emptyStatus"),
        }));
        return;
      }

      if (event.type === "verification" && isVerificationResult(event.data)) {
        setVerification(event.data);
        return;
      }

      if (event.type === "token" && event.content) {
        appendAgentToken(event.content);
      }
    },
    onComplete: () => {
      setThinkingMessage("");
    },
    onError: () => {
      setThinkingMessage("");
      appendAgentToken(t("streamFailed"));
    },
  });

  function sendPrompt(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const normalizedPrompt = prompt.trim();
    if (!normalizedPrompt) {
      return;
    }

    messageSequenceRef.current += 1;
    const userMessageId = `user-${messageSequenceRef.current}`;

    setMessages((currentMessages) => [
      ...currentMessages,
      {
        id: userMessageId,
        role: "user",
        mode: activeMode,
        content: normalizedPrompt,
        timestamp: t("now"),
      },
    ]);
    setPrompt("");
    setVerification(null);
    setThinkingMessage(t("checkingSignals"));

    void streamChat({
      mode: agentModeValueById[activeMode],
      message: normalizedPrompt,
      debugMode: showDebug,
    });
  }

  function appendAgentToken(content: string) {
    setMessages((currentMessages) => {
      const lastMessage = currentMessages.at(-1);
      if (lastMessage?.role === "agent" && lastMessage.timestamp === t("now")) {
        return [
          ...currentMessages.slice(0, -1),
          {
            ...lastMessage,
            content: `${lastMessage.content}${content}`,
          },
        ];
      }

      messageSequenceRef.current += 1;
      return [
        ...currentMessages,
        {
          id: `agent-${messageSequenceRef.current}`,
          role: "agent",
          mode: activeMode,
          content,
          timestamp: t("now"),
        },
      ];
    });
  }

  return (
    <main className="min-h-dvh bg-background">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-5 px-4 py-5 sm:px-6 lg:px-8">
        <header className="flex flex-col gap-4 border-b border-border pb-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="flex items-center gap-2 text-sm font-medium text-primary">
              <Activity className="h-4 w-4" aria-hidden="true" />
              {common("brand")}
            </p>
            <h1 className="mt-2 text-balance text-2xl font-semibold tracking-normal text-foreground sm:text-3xl">
              {t("title")}
            </h1>
            <p className="mt-2 max-w-2xl text-pretty text-sm leading-6 text-muted-foreground">
              {t("description")}
            </p>
          </div>
          <div className="grid grid-cols-3 gap-2 rounded-md bg-muted p-2 text-center sm:w-[360px]">
            <Metric label={t("metrics.openOrders")} value="24" />
            <Metric label={t("metrics.atRisk")} value="7" />
            <Metric label={t("metrics.averageLatency")} value="142 ms" />
          </div>
        </header>

        <section className="grid gap-4 xl:grid-cols-[280px_minmax(0,1fr)_340px]">
          <aside
            aria-label={t("modeSelector")}
            className="rounded-lg bg-card p-3 shadow-[0_0_0_1px_rgba(0,0,0,0.06),0_2px_4px_rgba(0,0,0,0.04)]"
          >
            <h2 className="px-1 text-sm font-semibold text-foreground">
              {t("modesTitle")}
            </h2>
            <div className="mt-3 grid gap-2">
              {agentModes.map((mode) => {
                const Icon = mode.icon;
                const isActive = mode.id === activeMode;

                return (
                  <button
                    key={mode.id}
                    type="button"
                    aria-pressed={isActive}
                    onClick={() => setActiveMode(mode.id)}
                    className={cn(
                      "min-h-14 rounded-md px-3 py-2 text-left transition-[background-color,box-shadow,scale] duration-150 ease-out active:scale-[0.96]",
                      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
                      isActive
                        ? "bg-primary text-primary-foreground shadow-sm"
                        : "bg-background text-foreground hover:bg-muted",
                    )}
                  >
                    <span className="flex items-center gap-2 text-sm font-medium">
                      <Icon className="h-4 w-4" aria-hidden="true" />
                      {mode.label}
                    </span>
                    <span
                      className={cn(
                        "mt-1 block text-xs leading-5",
                        isActive
                          ? "text-primary-foreground/85"
                          : "text-muted-foreground",
                      )}
                    >
                      {mode.description}
                    </span>
                  </button>
                );
              })}
            </div>
          </aside>

          <section className="flex min-h-[620px] flex-col rounded-lg bg-card shadow-[0_0_0_1px_rgba(0,0,0,0.06),0_2px_4px_rgba(0,0,0,0.04)]">
            <div className="flex items-center justify-between border-b border-border px-4 py-3">
              <div>
                <h2 className="text-sm font-semibold text-foreground">
                  {activeAgent.label}
                </h2>
                <p className="text-xs text-muted-foreground">
                  {t("workspaceDescription")}
                </p>
              </div>
              <span className="rounded-sm bg-muted px-2 py-1 text-xs font-medium text-muted-foreground">
                {t("ready")}
              </span>
            </div>

            {verification ? <VerificationStatus result={verification} /> : null}

            <div
              aria-label={t("conversationHistory")}
              className="flex-1 space-y-3 overflow-y-auto px-4 py-4"
            >
              {messages.map((message) => (
                <article
                  key={message.id}
                  className={cn(
                    "max-w-[84%] rounded-lg px-4 py-3 text-sm leading-6",
                    message.role === "user"
                      ? "ml-auto bg-primary text-primary-foreground"
                      : "bg-muted text-foreground",
                  )}
                >
                  <div className="mb-1 flex items-center justify-between gap-3 text-xs opacity-80">
                    <span className="font-medium">
                      {message.role === "user" ? t("you") : common("brand")}
                    </span>
                    <span className="tabular-nums">{message.timestamp}</span>
                  </div>
                  {/* Agent content is rendered as escaped JSX text. If markdown or HTML is introduced later, sanitize the response before rendering. */}
                  <p className="text-pretty">{message.content}</p>
                </article>
              ))}

              {thinkingMessage ? (
                <div
                  role="status"
                  className="flex w-fit items-center gap-2 rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground"
                >
                  <span className="h-2 w-2 rounded-full bg-primary" />
                  {thinkingMessage}
                </div>
              ) : null}
            </div>

            <form
              onSubmit={sendPrompt}
              className="border-t border-border px-4 py-3"
            >
              <div className="mb-2 flex flex-wrap gap-2">
                {samplePrompts.map((sample) => (
                  <button
                    key={sample}
                    type="button"
                    onClick={() => setPrompt(sample)}
                    className="min-h-10 rounded-md bg-muted px-3 text-xs font-medium text-muted-foreground transition-[background-color,scale] duration-150 ease-out hover:bg-border active:scale-[0.96] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  >
                    {sample}
                  </button>
                ))}
              </div>
              <label
                htmlFor="agent-prompt"
                className="sr-only"
              >
                {t("promptLabel")}
              </label>
              <div className="flex gap-2">
                <textarea
                  id="agent-prompt"
                  value={prompt}
                  onChange={(event) => setPrompt(event.target.value)}
                  rows={2}
                  placeholder={t("promptPlaceholder")}
                  className="min-h-16 flex-1 resize-none rounded-md border border-input bg-background px-3 py-2 text-sm leading-6 text-foreground outline-none transition-[border-color,box-shadow] duration-150 placeholder:text-muted-foreground focus:border-ring focus:ring-2 focus:ring-ring/20"
                />
                <button
                  type="submit"
                  className="flex min-h-16 w-16 items-center justify-center rounded-md bg-primary text-primary-foreground transition-[background-color,scale] duration-150 ease-out hover:bg-primary/90 active:scale-[0.96] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  disabled={!prompt.trim() || isStreaming}
                  aria-label={t("sendMessage")}
                >
                  <Send className="h-5 w-5" aria-hidden="true" />
                </button>
                {isStreaming ? (
                  <button
                    type="button"
                    onClick={stopStreaming}
                    className="flex min-h-16 w-12 items-center justify-center rounded-md border border-border bg-background text-muted-foreground transition-[background-color,scale] duration-150 ease-out hover:bg-muted active:scale-[0.96] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                    aria-label={t("stopStreaming")}
                  >
                    <span className="h-3 w-3 rounded-sm bg-current" />
                  </button>
                ) : null}
              </div>
            </form>
          </section>

          <aside className="grid gap-4">
            <section className="rounded-lg bg-card p-4 shadow-[0_0_0_1px_rgba(0,0,0,0.06),0_2px_4px_rgba(0,0,0,0.04)]">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h2 className="text-sm font-semibold text-foreground">
                    {t("structured.title")}
                  </h2>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {structuredResult.summary}
                  </p>
                </div>
                <Database className="h-4 w-4 text-primary" aria-hidden="true" />
              </div>
              <div className="mt-4 overflow-hidden rounded-md border border-border">
                <table className="w-full text-left text-xs">
                  <thead className="bg-muted text-muted-foreground">
                    <tr>
                      <th className="px-3 py-2 font-medium">{t("structured.workOrder")}</th>
                      <th className="px-3 py-2 font-medium">{t("structured.progress")}</th>
                      <th className="px-3 py-2 font-medium">{t("structured.risk")}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {structuredResult.rows.map((row) => (
                      <tr key={row.workOrder}>
                        <td className="px-3 py-2">
                          <span className="block font-medium text-foreground">
                            {row.workOrder}
                          </span>
                          <span className="text-muted-foreground">
                            {row.product}
                          </span>
                        </td>
                        <td className="px-3 py-2 tabular-nums">
                          {row.progress}
                        </td>
                        <td className="px-3 py-2 text-muted-foreground">
                          {row.risk}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <section className="rounded-lg bg-card p-4 shadow-[0_0_0_1px_rgba(0,0,0,0.06),0_2px_4px_rgba(0,0,0,0.04)]">
              <button
                type="button"
                aria-expanded={showDebug}
                onClick={() => setShowDebug((current) => !current)}
                className="flex min-h-11 w-full items-center justify-between gap-3 text-left transition-[scale] duration-150 ease-out active:scale-[0.96] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              >
                <span>
                  <span className="block text-sm font-semibold text-foreground">
                    {t("debug.title")}
                  </span>
                  <span className="block text-xs text-muted-foreground">
                    {t("debug.description")}
                  </span>
                </span>
                <ChevronDown
                  className={cn(
                    "h-4 w-4 text-muted-foreground transition-transform duration-150",
                    showDebug ? "rotate-180" : "rotate-0",
                  )}
                  aria-hidden="true"
                />
              </button>

              {showDebug ? (
                <div className="mt-4 rounded-md bg-muted p-3">
                  <h2 className="flex items-center gap-2 text-sm font-semibold text-foreground">
                    <Bug className="h-4 w-4" aria-hidden="true" />
                    {t("debug.executionDetails")}
                  </h2>
                  <dl className="mt-3 grid gap-2 text-xs">
                    <DebugRow label={t("debug.tool")} value={structuredResult.debug.tool} />
                    <DebugRow
                      label={t("debug.dataSource")}
                      value={structuredResult.debug.dataSource}
                    />
                    <DebugRow
                      label={t("debug.latency")}
                      value={structuredResult.debug.latency}
                    />
                    <DebugRow label={t("debug.mode")} value={activeAgent.label} />
                  </dl>
                </div>
              ) : null}
            </section>
          </aside>
        </section>
      </div>
    </main>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-sm bg-background px-2 py-2">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-sm font-semibold tabular-nums text-foreground">
        {value}
      </div>
    </div>
  );
}

function DebugRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="font-medium tabular-nums text-foreground">{value}</dd>
    </div>
  );
}

function toStructuredResult(
  data: unknown,
  labels: StructuredResultLabels,
): StructuredResult {
  const toolResult = isRecord(data) ? data : {};
  const nestedData = isRecord(toolResult.data) ? toolResult.data : {};
  const workOrders = Array.isArray(nestedData.workOrders)
    ? nestedData.workOrders
    : [];
  const rows = workOrders
    .filter(isRecord)
    .slice(0, 5)
    .map((workOrder) => ({
      workOrder: toDisplayString(workOrder.code ?? workOrder.Code, labels.unknown),
      product: toDisplayString(
        workOrder.productName ?? workOrder.ProductName,
        labels.unknownProduct,
      ),
      status: toDisplayString(workOrder.status ?? workOrder.Status, labels.open),
      progress: formatProgress(workOrder.progress ?? workOrder.Progress),
      risk: toDisplayString(
        workOrder.delayReason ?? workOrder.risk ?? workOrder.Risk,
        labels.none,
      ),
    }));

  return {
    summary:
      rows.length > 0
        ? labels.live
        : labels.empty,
    debug: {
      tool: toDisplayString(toolResult.tool, labels.agentTool),
      dataSource: labels.dataSource,
      latency: "stream",
    },
    rows:
      rows.length > 0
        ? rows
        : [
            {
              workOrder: labels.noRows,
              product: labels.noRowsDescription,
              status: labels.emptyStatus,
              progress: "0%",
              risk: labels.none,
            },
          ],
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isVerificationResult(value: unknown): value is VerificationResultDto {
  return (
    isRecord(value) &&
    typeof value.status === "string" &&
    typeof value.summary === "string" &&
    typeof value.verifiedAtUtc === "string" &&
    Array.isArray(value.checks)
  );
}

function toDisplayString(value: unknown, fallback: string) {
  return typeof value === "string" && value.trim() ? value : fallback;
}

function formatProgress(value: unknown) {
  if (typeof value === "number") {
    return `${Math.round((value <= 1 ? value * 100 : value))}%`;
  }

  if (typeof value === "string" && value.trim()) {
    return value;
  }

  return "0%";
}
