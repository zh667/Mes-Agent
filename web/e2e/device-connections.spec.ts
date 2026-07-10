import { expect, test } from "@playwright/test";
import { encode } from "next-auth/jwt";

const tenantId = "00000000-0000-0000-0000-000000000001";
const secret = "phase3-playwright-secret-at-least-32-bytes";

test.beforeEach(async ({ context, page }) => {
  const sessionToken = await encode({
    secret,
    maxAge: 60 * 60,
    token: {
      sub: "phase3-e2e-admin",
      email: "admin@example.test",
      name: "Phase 3 Admin",
      accessToken: "e2e-access-token",
      refreshToken: "e2e-refresh-token",
      accessTokenExpires: Date.now() + 60 * 60 * 1000,
      isPlatformAdmin: true,
      tenants: [{ id: tenantId, code: "DEMO", name: "Demo Plant", role: "Admin", isActive: true }],
    },
  });
  await context.addCookies([{ name: "next-auth.session-token", value: sessionToken, domain: "127.0.0.1", path: "/", httpOnly: true, sameSite: "Lax" }]);
  await page.addInitScript(
    ({ key, value }) => localStorage.setItem(key, value),
    { key: "mes.activeTenantId", value: tenantId },
  );

  await page.route("http://localhost:5000/api/device-connections", async (route) => {
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify([{ id: 12, name: "Line 1 broker", equipmentId: 3, equipmentCode: "CNC-03", protocol: 0, host: "mqtt", port: 1883, endpoint: "mes/equipment/CNC-03/status", hasCredentials: true, useTls: true, allowInsecure: false, isEnabled: true, lastConnectedAt: "2026-07-10T08:15:00Z", lastErrorCode: null }]),
    });
  });
  await page.route("http://localhost:5000/api/Equipment", async (route) => {
    await route.fulfill({ contentType: "application/json", body: JSON.stringify([{ id: 3, code: "CNC-03", name: "CNC 03", isActive: true }]) });
  });
});

test("manages read-only protocol configuration without exposing credentials", async ({ page }) => {
  await page.goto("/admin/device-connections");

  await expect(page.getByRole("heading", { name: "Device connections" })).toBeVisible();
  await expect(page.getByText("Line 1 broker")).toBeVisible();
  await expect(page.getByText("Credentials stored")).toBeVisible();
  await expect(page.getByLabel("Replacement password")).toHaveValue("");
  await expect(page.getByText("e2e-refresh-token")).toHaveCount(0);

  await page.getByLabel("Protocol").selectOption("1");
  await expect(page.getByLabel("Node ID")).toBeVisible();
  await expect(page.getByLabel("Allow insecure SecurityMode.None")).not.toBeChecked();
  await page.getByLabel("Protocol").selectOption("2");
  await expect(page.getByLabel("Unit ID")).toBeVisible();
  await expect(page.getByRole("spinbutton", { name: "Register", exact: true })).toBeVisible();
});
