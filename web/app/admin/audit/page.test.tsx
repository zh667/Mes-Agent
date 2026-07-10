import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import AuditAdministrationPage from "@/app/admin/audit/page";

vi.mock("@/lib/api-client", () => ({
  apiClient: {
    get: vi.fn().mockResolvedValue({
      data: {
        items: [
          {
            id: "00000000-0000-0000-0000-000000000011",
            timestamp: "2026-07-10T08:00:00Z",
            method: "POST",
            routeTemplate: "api/agent/chat",
            queryKeys: "",
            statusCode: 200,
            durationMilliseconds: 42,
          },
        ],
        total: 1,
        skip: 0,
        take: 25,
      },
    }),
  },
}));

describe("AuditAdministrationPage", () => {
  it("renders a filterable operations audit table", async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    render(
      <QueryClientProvider client={queryClient}>
        <AuditAdministrationPage />
      </QueryClientProvider>,
    );

    expect(screen.getByRole("heading", { name: /audit trail/i })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /operations/i })).toHaveAttribute("aria-selected", "true");
    expect(await screen.findByText("api/agent/chat")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /export audit/i })).toBeInTheDocument();
  });
});
