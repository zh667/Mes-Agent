import axios from "axios";

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
  return axios.create({
    baseURL: getApiBaseUrl(),
    headers: {
      "Content-Type": "application/json",
    },
  });
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
