import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { SchedulingWorkspace } from "@/components/scheduling/scheduling-workspace";

vi.mock("@/lib/api-client", () => ({
  apiClient: { get: vi.fn().mockResolvedValue({ data: { from: "2026-07-10T00:00:00Z", to: "2026-07-11T00:00:00Z", equipmentRows: [] } }), post: vi.fn(), put: vi.fn() },
}));

describe("SchedulingWorkspace", () => {
  it("renders stable generation controls and an empty gantt state", async () => {
    render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}><SchedulingWorkspace /></QueryClientProvider>);
    expect(screen.getByLabelText(/work order ids/i)).toBeInTheDocument();
    expect(await screen.findByText(/no scheduled operations/i)).toBeInTheDocument();
  });
});
