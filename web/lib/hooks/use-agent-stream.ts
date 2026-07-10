"use client";

import { useCallback, useRef, useState } from "react";
import { useSession } from "next-auth/react";

import { getActiveTenantId } from "@/lib/tenant";
import type { components } from "@/shared/api/generated/schema";

export type VerificationResultDto = components["schemas"]["VerificationResultDto"];

export type AgentStreamEventType =
  | "thinking"
  | "token"
  | "tool_result"
  | "verification"
  | "done"
  | "error";

export type AgentStreamEvent = {
  type: AgentStreamEventType;
  content?: string | null;
  data?: unknown;
};

export type AgentStreamRequest = {
  conversationId?: string;
  mode: number;
  message: string;
  debugMode?: boolean;
};

type UseAgentStreamOptions = {
  onEvent?: (event: AgentStreamEvent) => void;
  onComplete?: () => void;
  onError?: (error: Error) => void;
};

export function useAgentStream(options: UseAgentStreamOptions = {}) {
  const { data: session } = useSession();
  const [isStreaming, setIsStreaming] = useState(false);
  const abortControllerRef = useRef<AbortController | null>(null);
  const optionsRef = useRef(options);
  const sessionRef = useRef(session);
  optionsRef.current = options;
  sessionRef.current = session;

  const stopStreaming = useCallback(() => {
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
    setIsStreaming(false);
  }, []);

  const streamChat = useCallback(
    async (request: AgentStreamRequest) => {
      const currentSession = sessionRef.current;
      if (!currentSession?.accessToken) {
        optionsRef.current.onError?.(new Error("Not authenticated"));
        return;
      }

      const tenantId = getActiveTenantId(currentSession.user?.tenants);
      if (!tenantId) {
        optionsRef.current.onError?.(new Error("No active tenant selected"));
        return;
      }

      const controller = new AbortController();
      abortControllerRef.current = controller;
      setIsStreaming(true);

      try {
        const response = await fetch(`${getApiBaseUrl()}/agent/chat`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${currentSession.accessToken}`,
            "X-Tenant-Id": tenantId,
          },
          body: JSON.stringify({
            conversationId: request.conversationId,
            mode: request.mode,
            message: request.message,
            debugMode: request.debugMode ?? false,
          }),
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error(`Agent stream failed with HTTP ${response.status}`);
        }

        if (!response.body) {
          throw new Error("Agent stream response body is empty");
        }

        await readSseStream(response.body, (event) => {
          optionsRef.current.onEvent?.(event);
          if (event.type === "done") {
            optionsRef.current.onComplete?.();
          }
        });
      } catch (error) {
        if (!isAbortError(error)) {
          optionsRef.current.onError?.(toError(error));
        }
      } finally {
        abortControllerRef.current = null;
        setIsStreaming(false);
      }
    },
    [],
  );

  return {
    isStreaming,
    streamChat,
    stopStreaming,
  };
}

async function readSseStream(
  body: ReadableStream<Uint8Array>,
  onEvent: (event: AgentStreamEvent) => void,
) {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const parts = buffer.split("\n\n");
    buffer = parts.pop() ?? "";

    for (const part of parts) {
      const event = parseSseEvent(part);
      if (event) {
        onEvent(event);
      }
    }
  }

  if (buffer.trim()) {
    const event = parseSseEvent(buffer);
    if (event) {
      onEvent(event);
    }
  }
}

function parseSseEvent(rawEvent: string): AgentStreamEvent | null {
  const dataLine = rawEvent
    .split("\n")
    .find((line) => line.startsWith("data: "));

  if (!dataLine) {
    return null;
  }

  let parsed: Partial<AgentStreamEvent>;
  try {
    parsed = JSON.parse(dataLine.slice("data: ".length)) as Partial<AgentStreamEvent>;
  } catch {
    return null;
  }
  if (!parsed.type) {
    return null;
  }

  const event: AgentStreamEvent = {
    type: parsed.type,
  };

  if ("content" in parsed) {
    event.content = parsed.content ?? null;
  }

  if ("data" in parsed) {
    event.data = parsed.data;
  }

  return event;
}

function getApiBaseUrl() {
  const configuredUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api";
  return configuredUrl.replace(/\/$/, "");
}

function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError";
}

function toError(error: unknown) {
  return error instanceof Error ? error : new Error(String(error));
}
