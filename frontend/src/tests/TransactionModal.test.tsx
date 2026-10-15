import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "../language";
import TransactionModal from "../features/dashboard/components/modals/TransactionModal";

const { mockAccounts, mockPost, mockShowAlert } = vi.hoisted(() => ({
  mockAccounts: [
    { id: 1, accountNumber: "1111-1111", balance: 1000, name: "Privatkonto", type: "checking" as const, interest: 0 },
    { id: 2, accountNumber: "2222-2222", balance: 50, name: "Sparkonto", type: "savings" as const, interest: 1.5 },
  ],
  mockPost: vi.fn().mockResolvedValue({ data: {} }),
  mockShowAlert: vi.fn(),
}));

vi.mock("../hooks/useAccounts", () => ({
  useAccounts: () => ({ data: mockAccounts, isLoading: false }),
}));

vi.mock("../hooks/useAlert", () => ({
  useAlert: () => ({ showAlert: mockShowAlert }),
}));

vi.mock("../client", () => ({
  apiClient: { post: mockPost },
}));

function renderModal() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <TransactionModal isOpen={true} onClose={vi.fn()} />
    </QueryClientProvider>
  );
}

async function selectAccountInDropdown(label: string) {
  fireEvent.mouseDown(screen.getByText("Välj konto"));
  const option = await screen.findByRole("option", { name: label });
  fireEvent.click(option);
}

describe("TransactionModal", () => {
  it("blockerar uttag vid otillräckligt saldo", async () => {
    renderModal();
    await selectAccountInDropdown("2222-2222"); // balans 50
    fireEvent.click(screen.getByRole("button", { name: "Uttag" }));
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "100" } });

    fireEvent.click(screen.getByRole("button", { name: "Ta ut" }));

    expect(await screen.findByText("Otillräckligt saldo för uttag.")).toBeInTheDocument();
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("blockerar insättning/uttag med belopp 0", async () => {
    renderModal();
    await selectAccountInDropdown("1111-1111");
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "0" } });

    fireEvent.click(screen.getByRole("button", { name: "Sätt in" }));

    expect(
      await screen.findByText("Beloppet måste vara en siffra större än 0.")
    ).toBeInTheDocument();
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("genomför en giltig insättning", async () => {
    renderModal();
    await selectAccountInDropdown("1111-1111");
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "200" } });

    fireEvent.click(screen.getByRole("button", { name: "Sätt in" }));

    await vi.waitFor(() => expect(mockPost).toHaveBeenCalledTimes(1));
    expect(mockPost).toHaveBeenCalledWith("/transactions/deposit", {
      accountId: 1,
      amount: 200,
      description: undefined,
    });
  });

  it("genomför ett giltigt uttag inom saldot", async () => {
    renderModal();
    await selectAccountInDropdown("1111-1111"); // balans 1000
    fireEvent.click(screen.getByRole("button", { name: "Uttag" }));
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "200" } });

    fireEvent.click(screen.getByRole("button", { name: "Ta ut" }));

    await vi.waitFor(() => expect(mockPost).toHaveBeenCalledTimes(1));
    expect(mockPost).toHaveBeenCalledWith("/transactions/withdraw", {
      accountId: 1,
      amount: 200,
      description: undefined,
    });
  });
});
