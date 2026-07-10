import "next-auth";
import "next-auth/jwt";

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
      role?: string | undefined;
    };
  }

  interface User {
    role?: string | undefined;
    accessToken?: string | undefined;
    refreshToken?: string | undefined;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    role?: string | undefined;
    accessToken?: string | undefined;
    refreshToken?: string | undefined;
    accessTokenExpires?: number | undefined;
    error?: string | undefined;
  }
}
