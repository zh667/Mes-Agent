import {
  act,
  fireEvent,
  render,
  screen,
  within,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { ChatInterface } from "@/components/agent/chat-interface";

const agentStreamMock = vi.hoisted(() => ({
  streamChat: vi.fn(),
  stopStreaming: vi.fn(),
  isStreaming: false,
  options: undefined as
    | {
        onEvent?: (event: {
          type: "thinking" | "token" | "tool_result" | "done" | "error";
          content?: string | null;
          data?: unknown;
        }) => void;
        onComplete?: () => void;
        onError?: (error: Error) => void;
      }
    | undefined,
}));

vi.mock("@/lib/hooks/use-agent-stream", () => ({
  useAgentStream: (
    options: typeof agentStreamMock.options,
  ) => {
    agentStreamMock.options = options;
    return {
      streamChat: agentStreamMock.streamChat,
      stopStreaming: agentStreamMock.stopStreaming,
      isStreaming: agentStreamMock.isStreaming,
    };
  },
}));

afterEach(() => {
  vi.useRealTimers();
  agentStreamMock.streamChat.mockReset();
  agentStreamMock.stopStreaming.mockReset();
  agentStreamMock.isStreaming = false;
  agentStreamMock.options = undefined;
});

describe("ChatInterface", () => {
  it("renders the operations chat workspace with agent mode controls", () => {
    render(<ChatInterface />);

    expect(
      screen.getByRole("heading", { name: "Agent Console" }),
    ).toBeDefined();
    expect(
      screen.getByRole("button", { name: /production agent/i }),
    ).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /quality agent/i })).toBeDefined();
    expect(screen.getByRole("button", { name: /oee agent/i })).toBeDefined();
    expect(
      screen.getByRole("button", { name: /knowledge agent/i }),
    ).toBeDefined();
  });

  it("shows structured results and a collapsible debug panel", () => {
    render(<ChatInterface />);

    expect(
      screen.getByRole("heading", { name: "Structured Result" }),
    ).toBeDefined();

    const debugToggle = screen.getByRole("button", {
      name: /show execution details/i,
    });
    fireEvent.click(debugToggle);

    expect(
      screen.getByRole("heading", { name: "Execution Details" }),
    ).toBeDefined();
    const executionDetails = screen
      .getByRole("heading", { name: "Execution Details" })
      .closest("div");

    expect(executionDetails).not.toBeNull();
    expect(within(executionDetails!).getByText("GetTodayWorkOrders")).toBeDefined();
    expect(within(executionDetails!).getByText("142 ms")).toBeDefined();
  });

  it("sends a prompt through the streaming hook", () => {
    render(<ChatInterface />);

    fireEvent.change(
      screen.getByLabelText("Ask the MES Copilot about production operations"),
      {
        target: { value: "Which work orders are delayed today?" },
      },
    );
    fireEvent.click(screen.getByRole("button", { name: "Send message" }));

    const conversation = screen.getByLabelText("Conversation history");

    expect(
      within(conversation).getByText("Which work orders are delayed today?"),
    ).toBeDefined();
    expect(agentStreamMock.streamChat).toHaveBeenCalledWith({
      mode: 0,
      message: "Which work orders are delayed today?",
      debugMode: false,
    });
  });

  it("renders streamed thinking, token, and structured result events", async () => {
    render(<ChatInterface />);

    fireEvent.change(
      screen.getByLabelText("Ask the MES Copilot about production operations"),
      {
        target: { value: "Summarize delayed orders now" },
      },
    );
    fireEvent.click(screen.getByRole("button", { name: "Send message" }));

    expect(agentStreamMock.options).toBeDefined();

    await act(async () => {
      agentStreamMock.options?.onEvent?.({
        type: "thinking",
        content: "Selecting MES tool",
      });
    });

    expect(screen.getByText("Agent is checking MES signals")).toBeDefined();

    await act(async () => {
      agentStreamMock.options?.onEvent?.({
        type: "tool_result",
        data: {
          tool: "AnalyzeDelayedOrders",
          data: {
            workOrders: [
              {
                code: "WO-20260709-027",
                productName: "Drive Motor",
                progress: 0.58,
                delayReason: "Line 2 alarm recovery",
              },
            ],
          },
        },
      });
      agentStreamMock.options?.onEvent?.({
        type: "token",
        content: "I found 1 delayed work order.",
      });
      agentStreamMock.options?.onComplete?.();
    });

    const conversation = screen.getByLabelText("Conversation history");

    expect(screen.queryByText("Agent is checking MES signals")).toBeNull();
    expect(
      within(conversation).getByText(/I found 1 delayed work order/i),
    ).toBeDefined();
    expect(screen.getByText("WO-20260709-027")).toBeDefined();
  });
});
