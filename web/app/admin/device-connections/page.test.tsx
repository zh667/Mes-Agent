import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import DeviceConnectionsPage from "@/app/admin/device-connections/page";

vi.mock("@/lib/api-client", () => ({
  apiClient: {
    get: vi.fn((path: string) => {
      if (path === "/device-connections") {
        return Promise.resolve({
          data: [
            {
              id: 12,
              name: "Line 1 broker",
              equipmentId: 3,
              equipmentCode: "CNC-03",
              protocol: 0,
              host: "mqtt",
              port: 1883,
              endpoint: "mes/equipment/CNC-03/status",
              hasCredentials: true,
              useTls: true,
              allowInsecure: false,
              isEnabled: true,
              lastConnectedAt: "2026-07-10T08:15:00Z",
              lastErrorCode: null,
            },
          ],
        });
      }

      return Promise.resolve({ data: [{ id: 3, code: "CNC-03", name: "CNC 03" }] });
    }),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("DeviceConnectionsPage", () => {
  it("shows protocol state and secret replacement controls", async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    render(
      <QueryClientProvider client={queryClient}>
        <DeviceConnectionsPage />
      </QueryClientProvider>,
    );

    expect(screen.getByRole("heading", { name: /device connections/i })).toBeInTheDocument();
    expect(await screen.findByText("Line 1 broker")).toBeInTheDocument();
    expect(screen.getAllByText("MQTT")).not.toHaveLength(0);
    expect(screen.getByText(/credentials stored/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /test line 1 broker/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /stop line 1 broker/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/replacement password/i)).toHaveValue("");
  });
});
