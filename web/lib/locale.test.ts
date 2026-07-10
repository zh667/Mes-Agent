import { describe, expect, it } from "vitest";

import enMessages from "@/messages/en-US.json";
import zhMessages from "@/messages/zh-CN.json";
import { formatDateForLocale, formatNumberForLocale, resolveLocale } from "@/lib/locale";

describe("resolveLocale", () => {
  it("prefers a supported locale cookie", () => {
    expect(resolveLocale("en-US", "zh-CN,zh;q=0.9")).toBe("en-US");
  });

  it("uses Accept-Language when the cookie is missing", () => {
    expect(resolveLocale(undefined, "en-GB,en;q=0.9,zh-CN;q=0.8")).toBe("en-US");
  });

  it("ignores invalid values and falls back to Chinese", () => {
    expect(resolveLocale("fr-FR", "de-DE")).toBe("zh-CN");
  });

  it("keeps Chinese and English message key sets identical", () => {
    expect(flattenKeys(zhMessages)).toEqual(flattenKeys(enMessages));
  });

  it("formats dates and numbers with the selected locale", () => {
    expect(formatNumberForLocale(1234.5, "en-US")).toBe("1,234.5");
    expect(formatNumberForLocale(1234.5, "zh-CN")).toBe("1,234.5");
    expect(formatDateForLocale(new Date("2026-07-10T00:00:00Z"), "en-US")).toContain("2026");
  });
});

function flattenKeys(value: object, prefix = ""): string[] {
  return Object.entries(value)
    .flatMap(([key, item]) => {
      const path = prefix ? `${prefix}.${key}` : key;
      return typeof item === "object" && item !== null ? flattenKeys(item, path) : [path];
    })
    .sort();
}
