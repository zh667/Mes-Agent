import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import AgentPage from "./page";

vi.mock("@/lib/hooks/use-agent-stream", () => ({
  useAgentStream: () => ({
    streamChat: vi.fn(),
    stopStreaming: vi.fn(),
    isStreaming: false,
  }),
}));

describe("AgentPage", () => {
  it("renders the Phase 6.2 chat page instead of the placeholder", () => {
    render(<AgentPage />);

    expect(
      screen.getByRole("heading", { name: "Agent Console" }),
    ).toBeDefined();
    expect(
      screen.getByLabelText("Ask the MES Copilot about production operations"),
    ).toBeDefined();
    expect(screen.queryByText(/entry point/i)).toBeNull();
  });
});
