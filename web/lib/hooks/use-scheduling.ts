import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { apiClient } from "@/lib/api-client";
import type { components } from "@/shared/api/generated/schema";

export type GanttScheduleDto = components["schemas"]["GanttScheduleDto"];
export type ScheduledOperationDto = components["schemas"]["ScheduledOperationDto"];
export type GenerateScheduleRequest = components["schemas"]["GenerateScheduleRequest"];
export type AdjustOperationRequest = components["schemas"]["AdjustOperationRequest"];

export function useGantt(from: string, to: string) {
  return useQuery({
    queryKey: ["scheduling", "gantt", from, to],
    queryFn: async () => (await apiClient.get<GanttScheduleDto>("/scheduling/gantt", { params: { from, to } })).data,
  });
}

export function useGenerateSchedule() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async (request: GenerateScheduleRequest) => (await apiClient.post("/scheduling/generate", request)).data,
    onSuccess: async () => client.invalidateQueries({ queryKey: ["scheduling", "gantt"] }),
  });
}

export function useAdjustOperation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: AdjustOperationRequest }) =>
      (await apiClient.put<ScheduledOperationDto>(`/scheduling/operations/${id}`, request)).data,
    onSuccess: async () => client.invalidateQueries({ queryKey: ["scheduling", "gantt"] }),
  });
}
