import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import KnowledgePage from "./page";

describe("KnowledgePage", () => {
  it("renders document management and knowledge search sections", () => {
    render(<KnowledgePage />);

    expect(
      screen.getByRole("heading", { name: "Knowledge Base" }),
    ).toBeDefined();
    expect(screen.getByRole("button", { name: "Upload document" })).toBeDefined();
    expect(screen.getByLabelText("Search knowledge base")).toBeDefined();
    expect(screen.getByRole("heading", { name: "Document Library" })).toBeDefined();
    expect(screen.queryByText(/entry point/i)).toBeNull();
  });
});
