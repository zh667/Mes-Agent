import { type NextAuthOptions } from "next-auth";
import type { JWT } from "next-auth/jwt";
import CredentialsProvider from "next-auth/providers/credentials";

import type { components } from "@/shared/api/generated/schema";

type AuthResponse = components["schemas"]["AuthResponse"];

const accessTokenLifetimeMs = 15 * 60 * 1000;
const refreshSkewMs = 60 * 1000;

export function getServerApiBaseUrl() {
  return process.env.API_INTERNAL_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api";
}

async function refreshAccessToken(token: JWT): Promise<JWT> {
  if (!token.accessToken || !token.refreshToken) {
    return token;
  }

  let response: Response;
  try {
    response = await fetch(`${getServerApiBaseUrl()}/auth/refresh`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        accessToken: token.accessToken,
        refreshToken: token.refreshToken,
      }),
    });
  } catch {
    return {
      ...token,
      error: "RefreshAccessTokenError",
    };
  }

  if (!response.ok) {
    return {
      ...token,
      error: "RefreshAccessTokenError",
    };
  }

  const auth = (await response.json()) as AuthResponse;
  if (!auth.accessToken || !auth.refreshToken) {
    return {
      ...token,
      error: "RefreshAccessTokenError",
    };
  }

  return {
    ...token,
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    isPlatformAdmin: auth.user?.isPlatformAdmin ?? token.isPlatformAdmin,
    tenants: auth.user?.tenants ?? token.tenants,
    accessTokenExpires: Date.now() + accessTokenLifetimeMs,
    error: undefined,
  };
}

export const authOptions: NextAuthOptions = {
  session: {
    strategy: "jwt",
  },
  pages: {
    signIn: "/login",
  },
  providers: [
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials.password) {
          return null;
        }

        const response = await fetch(`${getServerApiBaseUrl()}/auth/login`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            email: credentials.email,
            password: credentials.password,
          }),
        });

        if (!response.ok) {
          return null;
        }

        const auth = (await response.json()) as AuthResponse;
        if (!auth.user?.id || !auth.user.email || !auth.accessToken) {
          return null;
        }

        const refreshToken = auth.refreshToken ?? "";

        return {
          id: auth.user.id,
          email: auth.user.email,
          name: auth.user.displayName ?? auth.user.email,
          isPlatformAdmin: auth.user.isPlatformAdmin ?? false,
          tenants: auth.user.tenants ?? [],
          accessToken: auth.accessToken,
          refreshToken,
        };
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        token.isPlatformAdmin = user.isPlatformAdmin;
        token.tenants = user.tenants;
        token.accessToken = user.accessToken;
        token.refreshToken = user.refreshToken;
        token.accessTokenExpires = Date.now() + accessTokenLifetimeMs;
      }

      if (
        typeof token.accessTokenExpires === "number" &&
        Date.now() < token.accessTokenExpires - refreshSkewMs
      ) {
        return token;
      }

      return refreshAccessToken(token);
    },
    async session({ session, token }) {
      if (session.user) {
        session.user.id = token.sub ?? "";
        session.user.isPlatformAdmin = token.isPlatformAdmin;
        session.user.tenants = token.tenants;
      }

      if (token.accessToken) {
        session.accessToken = token.accessToken;
      }

      if (token.refreshToken) {
        session.refreshToken = token.refreshToken;
      }

      if (token.error) {
        session.error = token.error;
      }

      return session;
    },
  },
};
