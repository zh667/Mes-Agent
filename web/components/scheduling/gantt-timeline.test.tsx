import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { GanttTimeline } from "@/components/scheduling/gantt-timeline";

describe("GanttTimeline", () => {
  it("supports keyboard-accessible time adjustment", () => {
    const onMove = vi.fn();
    render(<GanttTimeline schedule={{ from: "2026-07-10T08:00:00Z", to: "2026-07-10T16:00:00Z", equipmentRows: [{ equipmentId: 1, equipmentCode: "EQ-01", equipmentName: "Lathe", operations: [{ id: 11, workOrderId: 4, workOrderCode: "WO-04", processStepId: 3, processStepName: "Cut", sequence: 1, equipmentId: 1, equipmentCode: "EQ-01", plannedStartTime: "2026-07-10T09:00:00Z", plannedEndTime: "2026-07-10T10:00:00Z", version: 2 }] }] }} onMove={onMove} />);
    fireEvent.click(screen.getByRole("button", { name: /move WO-04 right/i }));
    expect(onMove).toHaveBeenCalledWith(expect.objectContaining({ id: 11 }), 15);
  });
});
