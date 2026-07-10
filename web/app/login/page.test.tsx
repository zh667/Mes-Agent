import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import LoginPage from "./page";

const signInMock = vi.fn();
const pushMock = vi.fn();

vi.mock("next-auth/react", () => ({
  signIn: (...args: unknown[]) => signInMock(...args),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: pushMock,
  }),
}));

vi.mock("next-intl", () => ({
  useTranslations: (namespace?: string) => (key: string) => {
    const messages: Record<string, Record<string, string>> = {
      common: { brand: "MES Copilot" },
      auth: {
        badge: "Manufacturing operations workspace",
        description: "Secure access",
        signIn: "Sign in",
        signingIn: "Signing in...",
        email: "Email",
        password: "Password",
        hint: "Use your MES Copilot operator credentials.",
        invalid: "Invalid email or password",
      },
    };
    return messages[namespace ?? ""]?.[key] ?? key;
  },
}));

describe("LoginPage", () => {
  beforeEach(() => {
    signInMock.mockReset();
    pushMock.mockReset();
  });

  it("renders credential fields and submit action", () => {
    render(<LoginPage />);

    expect(screen.getByRole("heading", { name: "MES Copilot" })).toBeDefined();
    expect(screen.getByLabelText("Email")).toBeDefined();
    expect(screen.getByLabelText("Password")).toBeDefined();
    expect(screen.getByRole("button", { name: "Sign in" })).toBeDefined();
  });

  it("signs in with credentials and redirects to the workspace", async () => {
    signInMock.mockResolvedValue({ ok: true, error: null });
    render(<LoginPage />);

    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "operator@example.com" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "Test123!" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await vi.waitFor(() => {
      expect(signInMock).toHaveBeenCalledWith("credentials", {
        email: "operator@example.com",
        password: "Test123!",
        redirect: false,
      });
      expect(pushMock).toHaveBeenCalledWith("/");
    });
  });

  it("shows an error when credentials are rejected", async () => {
    signInMock.mockResolvedValue({ ok: false, error: "CredentialsSignin" });
    render(<LoginPage />);

    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "operator@example.com" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "wrong" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Invalid email or password",
    );
    expect(pushMock).not.toHaveBeenCalled();
  });
});
