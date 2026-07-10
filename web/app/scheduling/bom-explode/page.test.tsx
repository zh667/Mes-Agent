import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import BomExplosionPage from "@/app/scheduling/bom-explode/page";

vi.mock("@/lib/api-client", () => ({
  apiClient: { get: vi.fn().mockResolvedValue({ data: [] }), post: vi.fn() },
}));

describe("BomExplosionPage", () => {
  it("renders the material planning workspace", () => {
    render(<QueryClientProvider client={new QueryClient()}><BomExplosionPage /></QueryClientProvider>);
    expect(screen.getByRole("heading", { name: /bom explosion/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/product/i)).toBeInTheDocument();
  });
});
