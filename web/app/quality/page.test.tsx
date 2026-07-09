import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import QualityPage from "./page";

describe("QualityPage", () => {
  it("renders batch traceability and defect pattern sections", () => {
    render(<QualityPage />);

    expect(
      screen.getByRole("heading", { name: "Quality Trace" }),
    ).toBeDefined();
    expect(screen.getByLabelText("Search batch number")).toBeDefined();
    expect(screen.getByRole("heading", { name: "Trace Timeline" })).toBeDefined();
    expect(screen.getByRole("heading", { name: "Defect Patterns" })).toBeDefined();
    expect(screen.queryByText(/entry point/i)).toBeNull();
  });

  it("labels defect table columns for assistive technology", () => {
    render(<QualityPage />);

    for (const header of ["Defect", "Count", "Operation"]) {
      expect(screen.getByRole("columnheader", { name: header })).toHaveAttribute(
        "scope",
        "col",
      );
    }

    expect(screen.getByText("Surface scratch")).toBeDefined();
  });
});
