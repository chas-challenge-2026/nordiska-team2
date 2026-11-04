import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import "../language";
import BankStatementsCard from "../features/Taxreports/BankStatementsCard";


const { mockDownload, mockShowAlert } = vi.hoisted(() => ({
  mockDownload: vi.fn(),
  mockShowAlert: vi.fn(),
}));


vi.mock("../hooks/useBankStatements", () => ({
  downloadBankStatement: mockDownload,
}));

vi.mock("../hooks/useAlert", () => ({
  useAlert: () => ({ showAlert: mockShowAlert }),
}));


function axiosError(status: number) {
  return { isAxiosError: true, response: { status, data: {} } };
}

describe("BankStatementsCard", () => {
  beforeEach(() => {
    
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date(2026, 9, 15));
    mockDownload.mockReset();
    mockShowAlert.mockReset();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("visar de senaste 12 månaderna med stor bokstav", () => {
    render(<BankStatementsCard accountId={1} />);

    expect(screen.getAllByRole("button", { name: "Ladda ner" })).toHaveLength(12);
    expect(screen.getByText("Oktober 2026")).toBeInTheDocument();
    expect(screen.getByText("November 2025")).toBeInTheDocument();
  });

  it("anropar nedladdning med rätt konto, år och månad (1–12, inte 0–11)", async () => {
    mockDownload.mockResolvedValue(undefined);
    render(<BankStatementsCard accountId={1} />);

    fireEvent.click(screen.getAllByRole("button", { name: "Ladda ner" })[0]);

    await waitFor(() => expect(mockDownload).toHaveBeenCalledWith(1, 2026, 10));
    expect(mockShowAlert).not.toHaveBeenCalled();
  });

  it("stänger av knapparna och visar laddning medan filen hämtas", async () => {
    let finish!: () => void;
    mockDownload.mockReturnValue(new Promise<void>((resolve) => (finish = resolve)));
    render(<BankStatementsCard accountId={1} />);

    fireEvent.click(screen.getAllByRole("button", { name: "Ladda ner" })[0]);

    expect(await screen.findByRole("button", { name: "Laddar..." })).toBeDisabled();
    expect(screen.getAllByRole("button", { name: "Ladda ner" })[0]).toBeDisabled();

    finish();
    await waitFor(() =>
      expect(screen.getAllByRole("button", { name: "Ladda ner" })[0]).toBeEnabled()
    );
  });

  it("visar ett vänligt meddelande när kontoutdraget saknas (404)", async () => {
    mockDownload.mockRejectedValue(axiosError(404));
    render(<BankStatementsCard accountId={1} />);

    fireEvent.click(screen.getAllByRole("button", { name: "Ladda ner" })[0]);

    await waitFor(() =>
      expect(mockShowAlert).toHaveBeenCalledWith({
        type: "error",
        title: "Kunde inte hämta kontoutdraget",
        message: "Det finns inget kontoutdrag för den månaden.",
      })
    );
  });

  it("visar ett generellt felmeddelande vid andra fel", async () => {
    mockDownload.mockRejectedValue(new Error("boom"));
    render(<BankStatementsCard accountId={1} />);

    fireEvent.click(screen.getAllByRole("button", { name: "Ladda ner" })[0]);

    await waitFor(() =>
      expect(mockShowAlert).toHaveBeenCalledWith(
        expect.objectContaining({ type: "error", message: "Något gick fel. Försök igen." })
      )
    );
  });
});
