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
  await context.addCookies([
    {
      name: "next-auth.session-token",
      value: sessionToken,
      domain: "127.0.0.1",
      path: "/",
      httpOnly: true,
      sameSite: "Lax",
    },
  ]);
  await page.addInitScript(
    ({ key, value }) => localStorage.setItem(key, value),
    { key: "mes.activeTenantId", value: tenantId },
  );
});

test("runs BOM, scheduling, and verified Agent workflows in one tenant", async ({ page }) => {
  let scheduleRequested = false;
  let agentTenantHeader: string | undefined;

  await page.route("**/api/bom/products", (route) => route.fulfill({
    contentType: "application/json",
    body: JSON.stringify([{ productId: 7, code: "P-007", name: "Drive Unit", bomVersion: "1.0" }]),
  }));
  await page.route("**/api/bom/explode", (route) => route.fulfill({
    contentType: "application/json",
    body: JSON.stringify({
      productId: 7,
      productCode: "P-007",
      productName: "Drive Unit",
      quantity: 2,
      items: [{ materialId: 9, materialCode: "M-009", materialName: "Bearing", unit: "pcs", requiredQuantity: 12, availableQuantity: 4, shortageQuantity: 8, depth: 1, path: ["P-007", "M-009"] }],
      totalMaterials: [{ materialId: 9, materialCode: "M-009", materialName: "Bearing", unit: "pcs", requiredQuantity: 12, availableQuantity: 4, shortageQuantity: 8 }],
    }),
  }));
  await page.route("**/api/scheduling/gantt?*", (route) => route.fulfill({
    contentType: "application/json",
    body: JSON.stringify({
      from: "2026-07-10T00:00:00Z",
      to: "2026-07-12T23:59:00Z",
      equipmentRows: [],
    }),
  }));
  await page.route("**/api/scheduling/generate", (route) => {
    scheduleRequested = true;
    return route.fulfill({ contentType: "application/json", body: JSON.stringify([]) });
  });
  await page.route("**/api/agent/chat", (route) => {
    agentTenantHeader = route.request().headers()["x-tenant-id"];
    return route.fulfill({
      status: 200,
      contentType: "text/event-stream",
      body: [
        'data: {"type":"thinking","content":"Checking delayed orders"}',
        'data: {"type":"token","content":"Two delayed work orders found."}',
        'data: {"type":"verification","data":{"status":"Verified","summary":"Primary data matched.","errorCode":null,"verifiedAtUtc":"2026-07-10T08:00:00Z","checks":[]}}',
        'data: {"type":"done"}',
        "",
      ].join("\n\n"),
    });
  });

  await page.goto("/scheduling/bom-explode");
  await page.getByLabel("Product").selectOption("7");
  await page.getByLabel("Build quantity").fill("2");
  await page.getByRole("button", { name: "Calculate requirements" }).click();
  await expect(page.getByText("Bearing")).toBeVisible();
  await expect(page.getByText("Short 8 pcs")).toBeVisible();

  await page.goto("/scheduling");
  await page.getByLabel("Work order IDs").fill("101, 102");
  await page.getByRole("button", { name: "Generate schedule" }).click();
  await expect.poll(() => scheduleRequested).toBe(true);
  await expect(page.getByText("No scheduled operations in this window.")).toBeVisible();

  await page.goto("/agent");
  await page.getByLabel("Ask the MES Copilot about production operations").fill("Which orders are delayed?");
  await page.getByRole("button", { name: "Send message" }).click();
  await expect(page.getByText("Two delayed work orders found.")).toBeVisible();
  await expect(page.getByLabel("Verification status: Verified")).toBeVisible();
  expect(agentTenantHeader).toBe(tenantId);
});
