import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import EquipmentPage from "./page";

describe("EquipmentPage", () => {
  it("renders the OEE dashboard with status cards and alarms", () => {
    render(<EquipmentPage />);

    expect(
      screen.getByRole("heading", { name: "Equipment OEE" }),
    ).toBeDefined();
    expect(screen.getByRole("heading", { name: "Realtime Status" })).toBeDefined();
    expect(screen.getByRole("heading", { name: "OEE Trend" })).toBeDefined();
    expect(screen.getByRole("heading", { name: "Alarm History" })).toBeDefined();
    expect(screen.queryByText(/entry point/i)).toBeNull();
  });

  it("renders OEE trend values as bounded percentages", () => {
    render(<EquipmentPage />);

    expect(screen.getByText("Availability")).toBeDefined();
    expect(screen.getByText("82%")).toBeDefined();
    expect(screen.getByText("Performance")).toBeDefined();
    expect(screen.getByText("76%")).toBeDefined();
    expect(screen.getByText("Quality")).toBeDefined();
    expect(screen.getByText("96%")).toBeDefined();
  });
});
