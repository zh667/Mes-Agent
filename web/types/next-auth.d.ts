import "next-auth";
import "next-auth/jwt";
import type { components } from "@/shared/api/generated/schema";

type TenantSummaryDto = components["schemas"]["TenantSummaryDto"];

declare module "next-auth" {
  interface Session {
    accessToken?: string | undefined;
    refreshToken?: string | undefined;
    error?: string | undefined;
    user?: {
      id: string;
      name?: string | null;
      email?: string | null;
      image?: string | null;
      isPlatformAdmin?: boolean | undefined;
      tenants?: TenantSummaryDto[] | null | undefined;
    };
  }

  interface User {
    isPlatformAdmin?: boolean | undefined;
    tenants?: TenantSummaryDto[] | null | undefined;
    accessToken?: string | undefined;
    refreshToken?: string | undefined;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    isPlatformAdmin?: boolean | undefined;
    tenants?: TenantSummaryDto[] | null | undefined;
    accessToken?: string | undefined;
    refreshToken?: string | undefined;
    accessTokenExpires?: number | undefined;
    error?: string | undefined;
  }
}
