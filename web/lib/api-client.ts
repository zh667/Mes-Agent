import axios from "axios";
import { getSession } from "next-auth/react";

import { clearActiveTenantId, getActiveTenantId } from "@/lib/tenant";
import type { components } from "@/shared/api/generated/schema";

export type WorkOrderDto = components["schemas"]["WorkOrderDto"];
export type CreateWorkOrderRequest =
  components["schemas"]["CreateWorkOrderRequest"];
export type ReportProductionRequest =
  components["schemas"]["ReportProductionRequest"];

export function getApiBaseUrl(): string {
  return process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api";
}

export function createApiClient() {
  const client = axios.create({
    baseURL: getApiBaseUrl(),
    headers: {
      "Content-Type": "application/json",
    },
  });

  client.interceptors.request.use(async (config) => {
    const session = await getSession();
    if (session?.accessToken) {
      config.headers.set("Authorization", `Bearer ${session.accessToken}`);
    }

    const tenantId = getActiveTenantId(session?.user?.tenants);
    if (tenantId) {
      config.headers.set("X-Tenant-Id", tenantId);
    }

    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    (error: unknown) => {
      if (axios.isAxiosError(error) && error.response?.status === 403) {
        const data = error.response.data as { code?: unknown } | undefined;
        if (data?.code === "TENANT_FORBIDDEN") {
          clearActiveTenantId();
        }
      }

      return Promise.reject(error);
    },
  );

  return client;
}

export const apiClient = createApiClient();

const workOrderRoutes = {
  getAll: "/WorkOrders",
  getById: (id: number) => `/WorkOrders/${id}`,
} as const;

export const workOrdersApi = {
  routes: workOrderRoutes,
  getAll: () => apiClient.get<WorkOrderDto[]>(workOrderRoutes.getAll),
  getById: (id: number) =>
    apiClient.get<WorkOrderDto>(workOrderRoutes.getById(id)),
};
