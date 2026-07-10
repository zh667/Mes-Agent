import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { BomExplosionWorkspace } from "@/components/bom/bom-explosion-workspace";
import { apiClient } from "@/lib/api-client";

vi.mock("@/lib/api-client", () => ({
  apiClient: { get: vi.fn(), post: vi.fn() },
}));

describe("BomExplosionWorkspace", () => {
  beforeEach(() => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: [{ productId: 7, code: "P-007", name: "Drive Unit", bomVersion: "1.0" }] } as never);
    vi.mocked(apiClient.post).mockResolvedValue({ data: {
      productId: 7,
      productCode: "P-007",
      productName: "Drive Unit",
      quantity: 2,
      items: [{ materialId: 9, materialCode: "M-009", materialName: "Bearing", unit: "pcs", requiredQuantity: 12, availableQuantity: 4, shortageQuantity: 8, depth: 2, path: ["P-007", "B-1", "P-008", "B-2", "M-009"] }],
      totalMaterials: [{ materialId: 9, materialCode: "M-009", materialName: "Bearing", unit: "pcs", requiredQuantity: 12, availableQuantity: 4, shortageQuantity: 8 }],
    } } as never);
  });

  it("shows hierarchy and highlights material shortage after calculation", async () => {
    render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}><BomExplosionWorkspace /></QueryClientProvider>);
    await screen.findByRole("option", { name: /P-007/ });
    fireEvent.change(screen.getByLabelText(/product/i), { target: { value: "7" } });
    fireEvent.click(screen.getByRole("button", { name: /calculate requirements/i }));
    expect(await screen.findByText("Bearing")).toBeInTheDocument();
    expect(screen.getByText("Short 8 pcs")).toBeInTheDocument();
    expect(screen.getByText(/P-007.*M-009/)).toBeInTheDocument();
  });
});
