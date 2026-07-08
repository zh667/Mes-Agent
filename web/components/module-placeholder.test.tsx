import { render, screen } from "@testing-library/react";
import { Bot } from "lucide-react";
import { describe, expect, it } from "vitest";

import { ModulePlaceholder } from "@/components/module-placeholder";

describe("ModulePlaceholder", () => {
  it("renders a module title and home navigation link", () => {
    render(
      <ModulePlaceholder
        title="Agent Console"
        description="Production assistant workspace"
        icon={Bot}
      />,
    );

    expect(
      screen.getByRole("heading", { name: "Agent Console" }),
    ).toBeDefined();
    expect(screen.getByRole("link", { name: "MES Copilot" })).toHaveAttribute(
      "href",
      "/",
    );
  });
});
