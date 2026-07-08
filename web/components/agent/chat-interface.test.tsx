import {
  act,
  fireEvent,
  render,
  screen,
  within,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { ChatInterface } from "@/components/agent/chat-interface";

afterEach(() => {
  vi.useRealTimers();
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

  it("adds a user message and typing indicator after sending a prompt", () => {
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
    expect(screen.getByText("Agent is checking MES signals")).toBeDefined();
  });

  it("replaces the typing indicator with an agent response and result data", async () => {
    vi.useFakeTimers();
    render(<ChatInterface />);

    fireEvent.change(
      screen.getByLabelText("Ask the MES Copilot about production operations"),
      {
        target: { value: "Summarize delayed orders now" },
      },
    );
    fireEvent.click(screen.getByRole("button", { name: "Send message" }));

    expect(screen.getByText("Agent is checking MES signals")).toBeDefined();

    await act(async () => {
      vi.advanceTimersByTime(650);
    });

    const conversation = screen.getByLabelText("Conversation history");

    expect(screen.queryByText("Agent is checking MES signals")).toBeNull();
    expect(
      within(conversation).getByText(/I found 2 delayed work orders/i),
    ).toBeDefined();
    expect(screen.getByText("WO-20260709-027")).toBeDefined();
  });
});
