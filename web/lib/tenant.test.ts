import { afterEach, describe, expect, it } from "vitest";

import {
  activeTenantStorageKey,
  clearActiveTenantId,
  getActiveTenantId,
  setActiveTenantId,
} from "./tenant";

const tenants = [
  { id: "tenant-a", code: "A", name: "Plant A", role: "Operator", isActive: true },
  { id: "tenant-b", code: "B", name: "Plant B", role: "TeamLead", isActive: true },
];

afterEach(() => {
  localStorage.clear();
});

describe("tenant selection storage", () => {
  it("returns the stored tenant only when it is still available", () => {
    localStorage.setItem(activeTenantStorageKey, "tenant-b");

    expect(getActiveTenantId(tenants)).toBe("tenant-b");
  });

  it("clears a stale tenant instead of silently selecting another one", () => {
    localStorage.setItem(activeTenantStorageKey, "tenant-removed");

    expect(getActiveTenantId(tenants)).toBeNull();
    expect(localStorage.getItem(activeTenantStorageKey)).toBeNull();
  });

  it("stores and clears an explicit selection", () => {
    setActiveTenantId("tenant-a");
    expect(localStorage.getItem(activeTenantStorageKey)).toBe("tenant-a");

    clearActiveTenantId();
    expect(localStorage.getItem(activeTenantStorageKey)).toBeNull();
  });
});
