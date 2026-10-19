import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "../language";
import TransferModal from "../features/dashboard/components/modals/TransferModal";

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
      <TransferModal isOpen={true} onClose={vi.fn()} />
    </QueryClientProvider>
  );
}

async function selectAccountInDropdown(placeholder: string, label: string) {
  fireEvent.mouseDown(screen.getByText(placeholder));
  const option = await screen.findByRole("option", { name: label });
  fireEvent.click(option);
}

describe("TransferModal", () => {
  it("blockerar överföring vid otillräckligt saldo", async () => {
    renderModal();
    await selectAccountInDropdown("Från konto", "2222-2222"); // balans 50
    await selectAccountInDropdown("Till konto", "1111-1111");
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "100" } });

    fireEvent.click(screen.getByRole("button", { name: "Skicka" }));

    expect(await screen.findByText("Otillräckligt saldo på kontot.")).toBeInTheDocument();
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("blockerar överföring till och från samma konto", async () => {
    renderModal();
    await selectAccountInDropdown("Från konto", "1111-1111");
    await selectAccountInDropdown("Till konto", "1111-1111");
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "100" } });

    fireEvent.click(screen.getByRole("button", { name: "Skicka" }));

    expect(
      await screen.findByText("Överföring kan inte ske till och från samma konto.")
    ).toBeInTheDocument();
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("genomför en giltig överföring", async () => {
    renderModal();
    await selectAccountInDropdown("Från konto", "1111-1111"); // balans 1000
    await selectAccountInDropdown("Till konto", "2222-2222");
    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "100" } });

    fireEvent.click(screen.getByRole("button", { name: "Skicka" }));

    await vi.waitFor(() => expect(mockPost).toHaveBeenCalledTimes(1));
    expect(mockPost).toHaveBeenCalledWith("/transactions/transfer", {
      fromAccountId: 1,
      toAccountId: 2,
      amount: 100,
      description: undefined,
    });
  });
});
