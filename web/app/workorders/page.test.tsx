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

  it("exposes table headers and action buttons with explicit semantics", () => {
    render(<WorkOrdersPage />);

    for (const header of ["Work order", "Line", "Status", "Progress", "Risk"]) {
      expect(screen.getByRole("columnheader", { name: header })).toHaveAttribute(
        "scope",
        "col",
      );
    }

    for (const action of ["Start", "Report", "Complete"]) {
      expect(screen.getByRole("button", { name: action })).toHaveAttribute(
        "type",
        "button",
      );
    }

    expect(screen.getByText("WO-20260709-014")).toBeDefined();
  });
});
