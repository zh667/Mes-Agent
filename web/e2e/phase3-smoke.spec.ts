import { expect, test } from "@playwright/test";

test("protected Phase 3 workspaces preserve the authentication boundary", async ({ page }) => {
  for (const path of [
    "/agent",
    "/admin/audit",
    "/scheduling",
    "/scheduling/bom-explode",
    "/admin/device-connections",
  ]) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole("heading", { name: "MES Copilot" })).toBeVisible();
  }
});
