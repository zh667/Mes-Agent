import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { apiClient } from "@/lib/api-client";
import type { components } from "@/shared/api/generated/schema";

export type DeviceConnectionDto = components["schemas"]["DeviceConnectionDto"];
export type DeviceConnectionRequest = components["schemas"]["DeviceConnectionRequest"];
export type DeviceProtocol = components["schemas"]["DeviceProtocol"];
export type EquipmentDto = components["schemas"]["EquipmentDto"];

const connectionKey = ["device-connections"] as const;

export function useDeviceConnections() {
  return useQuery({
    queryKey: connectionKey,
    queryFn: async () => (await apiClient.get<DeviceConnectionDto[]>("/device-connections")).data,
  });
}

export function useDeviceEquipment() {
  return useQuery({
    queryKey: ["equipment", "device-connections"],
    queryFn: async () => (await apiClient.get<EquipmentDto[]>("/Equipment")).data,
  });
}

export function useSaveDeviceConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id?: number; request: DeviceConnectionRequest }) =>
      id
        ? (await apiClient.put<DeviceConnectionDto>(`/device-connections/${id}`, request)).data
        : (await apiClient.post<DeviceConnectionDto>("/device-connections", request)).data,
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: connectionKey }),
  });
}

export function useDeleteDeviceConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: number) => apiClient.delete(`/device-connections/${id}`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: connectionKey }),
  });
}

export function useDeviceConnectionCommand(command: "test" | "start" | "stop") {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: number) => apiClient.post(`/device-connections/${id}/${command}`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: connectionKey }),
  });
}
