import type { components } from "@/shared/api/generated/schema";

export type TenantSummaryDto = components["schemas"]["TenantSummaryDto"];

export const activeTenantStorageKey = "mes.activeTenantId";
export const tenantChangedEventName = "mes:tenant-changed";

export function getActiveTenantId(
  availableTenants?: readonly TenantSummaryDto[] | null,
): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  const storedTenantId = window.localStorage.getItem(activeTenantStorageKey);
  if (!storedTenantId) {
    return null;
  }

  if (
    availableTenants &&
    !availableTenants.some(
      (tenant) => tenant.id === storedTenantId && tenant.isActive !== false,
    )
  ) {
    clearActiveTenantId();
    return null;
  }

  return storedTenantId;
}

export function setActiveTenantId(tenantId: string): void {
  if (typeof window === "undefined") {
    return;
  }

  const normalizedTenantId = tenantId.trim();
  if (!normalizedTenantId) {
    clearActiveTenantId();
    return;
  }

  window.localStorage.setItem(activeTenantStorageKey, normalizedTenantId);
  window.dispatchEvent(
    new CustomEvent(tenantChangedEventName, { detail: normalizedTenantId }),
  );
}

export function clearActiveTenantId(): void {
  if (typeof window !== "undefined") {
    window.localStorage.removeItem(activeTenantStorageKey);
    window.dispatchEvent(new CustomEvent(tenantChangedEventName, { detail: null }));
  }
}
