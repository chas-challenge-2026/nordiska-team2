import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { MemoryRouter } from "react-router-dom";
import "../language";
import Footer from "../features/dashboard/components/Footer";

describe("Footer", () => {
  it("visar alla footer-länkar", () => {
    render(<Footer />, { wrapper: MemoryRouter });

    expect(screen.getByText("Säkerhet")).toBeInTheDocument();
    expect(screen.getByText("Integritet")).toBeInTheDocument();
    expect(screen.getByText("Villkor")).toBeInTheDocument();
  });

  it("visar aktuellt år i copyright-texten", () => {
    render(<Footer />, { wrapper: MemoryRouter });
    const year = new Date().getFullYear().toString();

    expect(screen.getByText(new RegExp(year))).toBeInTheDocument();
  });
});
