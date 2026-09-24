import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { FaqQuestionCard } from "../features/FAQ/FaqQuestionCard";
import type { FaqEntry } from "../types/faq";

const mockEntry: FaqEntry = {
  id: "1",
  question: "Hur tar jag ut pengar?",
  answer: "Gå till Sparkonto och välj Uttag.",
  category: "transactions",
  keywords: ["uttag"],
};

describe("FaqQuestionCard", () => {
  it("visar frågan", () => {
    render(<FaqQuestionCard entry={mockEntry} />);
    expect(screen.getByText("Hur tar jag ut pengar?")).toBeInTheDocument();
  });

  it("döljer svaret som standard", () => {
    render(<FaqQuestionCard entry={mockEntry} />);
    expect(screen.queryByText("Gå till Sparkonto och välj Uttag.")).not.toBeInTheDocument();
  });

  it("visar svaret efter klick", () => {
    render(<FaqQuestionCard entry={mockEntry} />);
    fireEvent.click(screen.getByText("Hur tar jag ut pengar?"));
    expect(screen.getByText("Gå till Sparkonto och välj Uttag.")).toBeInTheDocument();
  });
});