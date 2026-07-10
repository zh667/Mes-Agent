import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { useSession } from "next-auth/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { activeTenantStorageKey } from "@/lib/tenant";

import { TenantSelector } from "./tenant-selector";

vi.mock("next-auth/react", () => ({
  useSession: vi.fn(),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => ({
    context: "Operating context",
    active: "Active tenant",
    select: "Select tenant",
    switching: "Switching tenant",
    denied: "Tenant access could not be verified.",
  })[key] ?? key,
}));

describe("TenantSelector", () => {
  afterEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("switches only after the server validates the membership", async () => {
    vi.mocked(useSession).mockReturnValue({
      status: "authenticated",
      update: vi.fn(),
      data: {
        expires: "2099-01-01T00:00:00.000Z",
        accessToken: "access-token",
        user: {
          id: "user-1",
          name: "Operator",
          email: "operator@example.com",
          tenants: [
            { id: "tenant-a", code: "A", name: "Plant A", role: "Operator", isActive: true },
            { id: "tenant-b", code: "B", name: "Plant B", role: "TeamLead", isActive: true },
          ],
        },
      },
    });
    vi.spyOn(apiClient, "post").mockResolvedValue({ data: {} });

    render(<TenantSelector />);
    fireEvent.change(screen.getByRole("combobox", { name: "Active tenant" }), {
      target: { value: "tenant-b" },
    });

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith("/tenants/switch/tenant-b");
      expect(localStorage.getItem(activeTenantStorageKey)).toBe("tenant-b");
    });
  });
});
