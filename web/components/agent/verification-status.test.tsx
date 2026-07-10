import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { VerificationStatus } from "@/components/agent/verification-status";
import type { components } from "@/shared/api/generated/schema";

type VerificationResultDto = components["schemas"]["VerificationResultDto"];

const verified: VerificationResultDto = {
  status: "Verified",
  summary: "Primary MES data matched.",
  errorCode: null,
  verifiedAtUtc: "2026-07-10T08:00:00Z",
  checks: [
    {
      claim: "Delayed work order count",
      claimedValue: "2",
      actualValue: "2",
      rule: "DelayedOrdersVerificationRule",
      status: "Verified",
    },
  ],
};

describe("VerificationStatus", () => {
  it.each(["Verified", "Disputed", "Unverified"])(
    "renders an accessible %s state",
    (status) => {
      render(<VerificationStatus result={{ ...verified, status }} />);

      expect(screen.getByText(status)).toBeDefined();
      expect(screen.getByRole("status")).toHaveAccessibleName(
        `Verification status: ${status}`,
      );
    },
  );

  it("expands the individual verification checks", () => {
    render(<VerificationStatus result={verified} />);

    fireEvent.click(screen.getByRole("button", { name: /view verification details/i }));

    expect(screen.getByText("Delayed work order count")).toBeDefined();
    expect(screen.getByText("Claimed: 2")).toBeDefined();
    expect(screen.getByText("Actual: 2")).toBeDefined();
    expect(screen.getByText("DelayedOrdersVerificationRule")).toBeDefined();
  });
});
