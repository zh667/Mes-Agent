import {
  act,
  renderHook,
  waitFor,
} from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import {
  useAgentStream,
  type AgentStreamEvent,
} from "@/lib/hooks/use-agent-stream";

let mockSession: {
  accessToken?: string;
  user?: { tenants?: Array<{ id: string; isActive?: boolean }> };
} | null = {
  accessToken: "test-token",
  user: { tenants: [{ id: "tenant-a", isActive: true }] },
};

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: mockSession,
  }),
}));

function createSseResponse(events: string[]) {
  const encoder = new TextEncoder();
  return new Response(
    new ReadableStream({
      start(controller) {
        for (const event of events) {
          controller.enqueue(encoder.encode(event));
        }
        controller.close();
      },
    }),
    {
      status: 200,
      headers: {
        "Content-Type": "text/event-stream",
      },
    },
  );
}

describe("useAgentStream", () => {
  beforeEach(() => {
    mockSession = {
      accessToken: "test-token",
      user: { tenants: [{ id: "tenant-a", isActive: true }] },
    };
    localStorage.setItem("mes.activeTenantId", "tenant-a");
    process.env.NEXT_PUBLIC_API_URL = "http://localhost:5000/api";
    vi.restoreAllMocks();
  });

  it("streams SSE events and sends the bearer token with the active tenant", async () => {
    const events: AgentStreamEvent[] = [];
    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValue(
        createSseResponse([
          "data: {\"type\":\"thinking\",\"content\":\"Selecting MES tool\"}\n\n",
          "data: {\"type\":\"token\",\"content\":\"Today has\"}\n\n",
          "data: {\"type\":\"verification\",\"data\":{\"status\":\"Verified\",\"summary\":\"Matched\",\"errorCode\":null,\"verifiedAtUtc\":\"2026-07-10T08:00:00Z\",\"checks\":[]}}\n\n",
          "data: {\"type\":\"done\",\"content\":null}\n\n",
        ]),
      );

    const { result } = renderHook(() =>
      useAgentStream({
        onEvent: (event) => events.push(event),
      }),
    );

    await act(async () => {
      await result.current.streamChat({
        mode: 0,
        message: "Which work orders are delayed today?",
        debugMode: true,
      });
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5000/api/agent/chat",
      expect.objectContaining({
        method: "POST",
        headers: expect.objectContaining({
          Authorization: "Bearer test-token",
          "X-Tenant-Id": "tenant-a",
        }),
      }),
    );
    expect(events.map((event) => event.type)).toEqual([
      "thinking",
      "token",
      "verification",
      "done",
    ]);
    expect(result.current.isStreaming).toBe(false);
  });

  it("ignores malformed SSE events and continues reading", async () => {
    const events: AgentStreamEvent[] = [];
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      createSseResponse([
        "data: {not-json}\n\n",
        "data: {\"type\":\"token\",\"content\":\"Recovered\"}\n\n",
        "data: {\"type\":\"done\"}\n\n",
      ]),
    );

    const { result } = renderHook(() =>
      useAgentStream({
        onEvent: (event) => events.push(event),
      }),
    );

    await act(async () => {
      await result.current.streamChat({
        mode: 0,
        message: "test malformed event",
      });
    });

    expect(events.map((event) => event.type)).toEqual(["token", "done"]);
    expect(events[0]?.content).toBe("Recovered");
  });

  it("keeps streamChat stable when inline callbacks change", () => {
    const { result, rerender } = renderHook(
      ({ callback }) =>
        useAgentStream({
          onEvent: callback,
        }),
      {
        initialProps: {
          callback: vi.fn(),
        },
      },
    );
    const firstStreamChat = result.current.streamChat;

    rerender({ callback: vi.fn() });

    expect(result.current.streamChat).toBe(firstStreamChat);
  });
  it("reports an authentication error when no access token exists", async () => {
    mockSession = null;
    const onError = vi.fn();
    const fetchMock = vi.spyOn(globalThis, "fetch");
    const { result } = renderHook(() => useAgentStream({ onError }));

    await act(async () => {
      await result.current.streamChat({
        mode: 0,
        message: "hello",
      });
    });

    await waitFor(() => {
      expect(onError).toHaveBeenCalledWith(expect.any(Error));
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
