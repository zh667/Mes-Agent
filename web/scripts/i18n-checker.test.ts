import { describe, expect, it } from "vitest";

import {
  compareMessageCatalogs,
  findHardcodedJsx,
} from "./i18n-checker.mjs";
import enMessages from "../messages/en-US.json";
import zhMessages from "../messages/zh-CN.json";

describe("compareMessageCatalogs", () => {
  it("keeps the checked-in locale catalogs complete and non-empty", () => {
    expect(compareMessageCatalogs({ "en-US": enMessages, "zh-CN": zhMessages })).toEqual([]);
  });

  it("reports missing and empty translations across locales", () => {
    const issues = compareMessageCatalogs({
      "en-US": { common: { save: "Save", cancel: "Cancel" } },
      "zh-CN": { common: { save: "", search: "搜索" } },
    });

    expect(issues).toEqual([
      "en-US is missing key common.search",
      "zh-CN has an empty value for common.save",
      "zh-CN is missing key common.cancel",
    ]);
  });
});

describe("findHardcodedJsx", () => {
  it("reports visible JSX text and accessible attributes", () => {
    const issues = findHardcodedJsx(
      `export function Sample() {
        return <button aria-label="Save work order">Save changes</button>;
      }`,
      "sample.tsx",
    );

    expect(issues.map((issue) => issue.text)).toEqual([
      "Save work order",
      "Save changes",
    ]);
  });

  it("accepts message lookups and explicit domain identifiers", () => {
    const issues = findHardcodedJsx(
      `export function Sample({ t }: { t: (key: string) => string }) {
        return <div aria-label={t("actions.save")}>
          {t("actions.save")}
          {/* i18n-ignore: protocol identifier */}
          <code>OPC UA</code>
        </div>;
      }`,
      "sample.tsx",
    );

    expect(issues).toEqual([]);
  });
});
