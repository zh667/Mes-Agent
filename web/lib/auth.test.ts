import { afterEach, describe, expect, it, vi } from "vitest";
import type { Session } from "next-auth";

import { authOptions, getServerApiBaseUrl } from "./auth";

const originalApiUrl = process.env.NEXT_PUBLIC_API_URL;
const originalInternalApiUrl = process.env.API_INTERNAL_URL;
const jwtCallback = authOptions.callbacks?.jwt;
const sessionCallback = authOptions.callbacks?.session;
type JwtCallbackParams = Parameters<NonNullable<typeof jwtCallback>>[0];
type SessionCallbackParams = Parameters<NonNullable<typeof sessionCallback>>[0];

async function runJwtCallback(params: Partial<JwtCallbackParams> & Pick<JwtCallbackParams, "token">) {
  if (!jwtCallback) {
    throw new Error("JWT callback is not configured.");
  }

  return jwtCallback({
    account: null,
    user: undefined as never,
    ...params,
  });
}

async function runSessionCallback(params: SessionCallbackParams) {
  if (!sessionCallback) {
    throw new Error("Session callback is not configured.");
  }

  return sessionCallback(params);
}

afterEach(() => {
  vi.useRealTimers();
  vi.restoreAllMocks();

  if (originalApiUrl === undefined) {
    delete process.env.NEXT_PUBLIC_API_URL;
  } else {
    process.env.NEXT_PUBLIC_API_URL = originalApiUrl;
  }

  if (originalInternalApiUrl === undefined) {
    delete process.env.API_INTERNAL_URL;
  } else {
    process.env.API_INTERNAL_URL = originalInternalApiUrl;
  }
});

describe("getServerApiBaseUrl", () => {
  it("prefers the container-internal API URL for server-side authentication", () => {
    process.env.NEXT_PUBLIC_API_URL = "http://localhost:5000/api";
    process.env.API_INTERNAL_URL = "http://api:8080/api";

    expect(getServerApiBaseUrl()).toBe("http://api:8080/api");
  });
});

describe("authOptions jwt callback", () => {
  it("stores access token expiry when credentials login succeeds", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-07-09T10:00:00.000Z"));

    const token = await runJwtCallback({
      token: {},
      user: {
        id: "user-1",
        email: "operator@example.com",
        isPlatformAdmin: false,
        tenants: [],
        accessToken: "access-token",
        refreshToken: "refresh-token",
      },
      account: null,
      trigger: "signIn",
    });

    expect(token?.accessToken).toBe("access-token");
    expect(token?.refreshToken).toBe("refresh-token");
    expect(token?.accessTokenExpires).toBe(Date.now() + 15 * 60 * 1000);
  });

  it("keeps the current token when it is not near expiry", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-07-09T10:05:00.000Z"));
    const fetchMock = vi.spyOn(globalThis, "fetch");

    const token = await runJwtCallback({
      token: {
        accessToken: "current-access-token",
        refreshToken: "current-refresh-token",
        accessTokenExpires: new Date("2026-07-09T10:15:00.000Z").getTime(),
      },
      account: null,
    });

    expect(fetchMock).not.toHaveBeenCalled();
    expect(token?.accessToken).toBe("current-access-token");
    expect(token?.refreshToken).toBe("current-refresh-token");
  });

  it("refreshes the token when the stored access token is near expiry", async () => {
    process.env.NEXT_PUBLIC_API_URL = "https://api.example.test/api";
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-07-09T10:14:30.000Z"));
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(
        JSON.stringify({
          accessToken: "new-access-token",
          refreshToken: "new-refresh-token",
          user: {
            id: "user-1",
            email: "operator@example.com",
            displayName: "Operator",
            role: "Operator",
          },
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    const token = await runJwtCallback({
      token: {
        accessToken: "old-access-token",
        refreshToken: "old-refresh-token",
        accessTokenExpires: new Date("2026-07-09T10:15:00.000Z").getTime(),
      },
      account: null,
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "https://api.example.test/api/auth/refresh",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          accessToken: "old-access-token",
          refreshToken: "old-refresh-token",
        }),
      }),
    );
    expect(token?.accessToken).toBe("new-access-token");
    expect(token?.refreshToken).toBe("new-refresh-token");
    expect(token?.accessTokenExpires).toBe(Date.now() + 15 * 60 * 1000);
  });

  it("returns a refresh error token when the refresh request throws", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-07-09T10:14:30.000Z"));
    vi.spyOn(globalThis, "fetch").mockRejectedValue(new Error("network down"));

    await expect(
      runJwtCallback({
        token: {
          accessToken: "old-access-token",
          refreshToken: "old-refresh-token",
          accessTokenExpires: new Date("2026-07-09T10:15:00.000Z").getTime(),
        },
        account: null,
      }),
    ).resolves.toMatchObject({
      accessToken: "old-access-token",
      refreshToken: "old-refresh-token",
      error: "RefreshAccessTokenError",
    });
  });
});

describe("authOptions session callback", () => {
  it("exposes refresh errors on the session", async () => {
    const session = (await runSessionCallback({
      session: {
        expires: new Date("2026-07-09T11:00:00.000Z").toISOString(),
        user: {
          id: "",
          name: "Operator",
          email: "operator@example.com",
        },
      },
      token: {
        sub: "user-1",
        role: "Operator",
        accessToken: "access-token",
        refreshToken: "refresh-token",
        error: "RefreshAccessTokenError",
      },
      user: undefined as never,
      newSession: undefined,
      trigger: "update",
    })) as Session;

    expect(session.error).toBe("RefreshAccessTokenError");
  });
});
