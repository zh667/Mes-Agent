import { expect, test } from "@playwright/test";
import { encode } from "next-auth/jwt";

const secret = "phase3-playwright-secret-at-least-32-bytes";

test("language selection persists without changing the route", async ({ context, page }) => {
  await context.addCookies([
    { name: "MES_LOCALE", value: "zh-CN", domain: "127.0.0.1", path: "/", sameSite: "Lax" },
  ]);
  await page.goto("/login");
  await page.getByLabel("语言").selectOption("en-US");

  await expect(page).toHaveURL(/\/login$/);
  await page.reload();
  await expect(page.getByLabel("Language")).toHaveValue("en-US");
  await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
});

test("operations workspaces render Chinese catalog copy", async ({ context, page }) => {
  const sessionToken = await encode({
    secret,
    maxAge: 60 * 60,
    token: {
      sub: "i18n-e2e-user",
      email: "operator@example.test",
      name: "I18n Operator",
      accessToken: "e2e-access-token",
      refreshToken: "e2e-refresh-token",
      accessTokenExpires: Date.now() + 60 * 60 * 1000,
      tenants: [],
    },
  });
  await context.addCookies([
    { name: "next-auth.session-token", value: sessionToken, domain: "127.0.0.1", path: "/", httpOnly: true, sameSite: "Lax" },
    { name: "MES_LOCALE", value: "zh-CN", domain: "127.0.0.1", path: "/", sameSite: "Lax" },
  ]);

  await page.goto("/workorders");
  await expect(page.getByRole("heading", { name: "工单控制台" })).toBeVisible();
  await expect(page.getByText("阀门组件")).toBeVisible();
  await expect(page.getByText("Valve Assembly")).toHaveCount(0);

  await page.goto("/equipment");
  await expect(page.getByRole("heading", { name: "设备与 OEE" })).toBeVisible();
  await expect(page.getByText("传感器异常")).toBeVisible();

  await page.goto("/quality");
  await expect(page.getByRole("heading", { name: "质量追溯" })).toBeVisible();
  await expect(page.getByText("表面划伤")).toBeVisible();

  await page.goto("/knowledge");
  await expect(page.getByRole("heading", { name: "知识库" })).toBeVisible();
  await expect(page.getByText("A102 报警处理 SOP")).toBeVisible();

  await page.goto("/agent");
  await expect(page.getByRole("heading", { name: "Agent 控制台" })).toBeVisible();
  await expect(page.getByRole("button", { name: /生产 Agent/ })).toBeVisible();
  await expect(page.getByText(/我可以检查工单进度/)).toBeVisible();
});
