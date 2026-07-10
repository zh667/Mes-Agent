import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { LanguageSwitcher } from "@/components/language-switcher";

const refresh = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh }) }));
vi.mock("next-intl", () => ({
  useLocale: () => "zh-CN",
  useTranslations: () => (key: string) =>
    ({ language: "语言", chinese: "中文", english: "English" })[key] ?? key,
}));

describe("LanguageSwitcher", () => {
  beforeEach(() => {
    refresh.mockReset();
    document.cookie = "MES_LOCALE=; Max-Age=0; path=/";
  });

  it("persists the selected locale and refreshes server components", () => {
    render(<LanguageSwitcher />);

    fireEvent.change(screen.getByLabelText("语言"), { target: { value: "en-US" } });

    expect(document.cookie).toContain("MES_LOCALE=en-US");
    expect(refresh).toHaveBeenCalledOnce();
  });
});
