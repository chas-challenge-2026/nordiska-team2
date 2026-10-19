import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import ListItem from "../components/ui/ListItem";

describe("ListItem", () => {
  it("visar titel och undertitel", () => {
    render(<ListItem title="Insättning" subtitle="2026-10-14" />);
    expect(screen.getByText("Insättning")).toBeInTheDocument();
    expect(screen.getByText("2026-10-14")).toBeInTheDocument();
  });

  it("visar right-innehåll när det skickas in", () => {
    render(<ListItem title="Insättning" right={<span>+100 kr</span>} />);
    expect(screen.getByText("+100 kr")).toBeInTheDocument();
  });

  it("renderar inget right-innehåll om det utelämnas", () => {
    render(<ListItem title="Insättning" />);
    expect(screen.queryByText("+100 kr")).not.toBeInTheDocument();
  });
});
