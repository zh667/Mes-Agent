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
});
