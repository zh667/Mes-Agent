import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

import enMessages from "@/messages/en-US.json";

vi.mock("next-intl", async (importOriginal) => {
  const original = await importOriginal<typeof import("next-intl")>();
  return {
    ...original,
    useLocale: () => "en-US",
    useTranslations: (namespace?: string) => (
      key: string,
      values?: Record<string, string | number>,
    ) => {
      const path = namespace ? `${namespace}.${key}` : key;
      const value = path.split(".").reduce<unknown>(
        (current, segment) =>
          typeof current === "object" && current !== null
            ? (current as Record<string, unknown>)[segment]
            : undefined,
        enMessages,
      );
      if (typeof value !== "string") {
        return key;
      }

      return value.replace(/\{(\w+)\}/g, (placeholder, name: string) =>
        values?.[name] === undefined ? placeholder : String(values[name]),
      );
    },
  };
});

afterEach(() => {
  cleanup();
});
