import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import "../language";

// lottie-react/lottie-web försöker skapa en canvas-kontext redan vid import,
// vilket jsdom inte stödjer utan det separata "canvas"-npm-paketet.
// Vi testar aldrig branchen som faktiskt renderar <Lottie>, så vi mockar bort den.
vi.mock("lottie-react", () => ({
  Lottie: () => null,
}));

import AccountCard from "../components/cards/AccountCard";

describe("AccountCard", () => {
  it("visar namn, kontonummer, saldo och ränta", () => {
    render(
      <AccountCard name="Privatkonto" accountNumber="1234-5678" balance={500} interest={3.5} />
    );

    expect(screen.getByText("Privatkonto")).toBeInTheDocument();
    expect(screen.getByText("1234-5678")).toBeInTheDocument();
    expect(screen.getByText("Tillgängligt saldo")).toBeInTheDocument();
    expect(screen.getByText(/500,00/)).toBeInTheDocument();
    expect(screen.getByText("Ränta 3.5 %")).toBeInTheDocument();
  });

  it("visar ingen räntarad om interest utelämnas", () => {
    render(<AccountCard name="Privatkonto" accountNumber="1234-5678" balance={500} />);

    expect(screen.queryByText(/Ränta/)).not.toBeInTheDocument();
  });
});
