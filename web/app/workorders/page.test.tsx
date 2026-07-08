import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import WorkOrdersPage from "./page";

describe("WorkOrdersPage", () => {
  it("renders an operational work order board instead of the placeholder", () => {
    render(<WorkOrdersPage />);

    expect(
      screen.getByRole("heading", { name: "Work Orders" }),
    ).toBeDefined();
    expect(screen.getByLabelText("Filter work orders")).toBeDefined();
    expect(screen.getByRole("button", { name: "Start" })).toBeDefined();
    expect(screen.getByRole("button", { name: "Report" })).toBeDefined();
    expect(screen.getByRole("button", { name: "Complete" })).toBeDefined();
    expect(screen.queryByText(/entry point/i)).toBeNull();
  });
});
