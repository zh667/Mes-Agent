import { useMutation, useQuery } from "@tanstack/react-query";

import { apiClient } from "@/lib/api-client";
import type { components } from "@/shared/api/generated/schema";

export type BomProductOptionDto = components["schemas"]["BomProductOptionDto"];
export type BomExplosionRequest = components["schemas"]["BomExplosionRequest"];
export type BomExplosionResultDto = components["schemas"]["BomExplosionResultDto"];

export function useBomProducts() {
  return useQuery({
    queryKey: ["bom", "products"],
    queryFn: async () => (await apiClient.get<BomProductOptionDto[]>("/bom/products")).data,
  });
}

export function useBomExplosion() {
  return useMutation({
    mutationFn: async (request: BomExplosionRequest) =>
      (await apiClient.post<BomExplosionResultDto>("/bom/explode", request)).data,
  });
}
