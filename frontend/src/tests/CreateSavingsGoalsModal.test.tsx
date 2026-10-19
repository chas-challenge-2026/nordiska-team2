import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "../language";
import CreateSavingsGoalsModal from "../features/dashboard/components/modals/SavingsGoalsModal/CreateSavingsGoalsModal";

const { mockAccounts, mockPost, mockShowAlert } = vi.hoisted(() => ({
  mockAccounts: [
    { id: 1, accountNumber: "1111-1111", balance: 1000, name: "Privatkonto", type: "checking" as const, interest: 0 },
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
      <CreateSavingsGoalsModal isOpen={true} onClose={vi.fn()} />
    </QueryClientProvider>
  );
}

async function fillNameAndAccount() {
  fireEvent.change(screen.getByPlaceholderText("Ex: Resa till Japan"), {
    target: { value: "Resa till Japan" },
  });
  fireEvent.mouseDown(screen.getByText("Välj konto"));
  const option = await screen.findByText("1111-1111");
  fireEvent.click(option);
}

describe("CreateSavingsGoalsModal", () => {
  it("skickar INTE sparmålet om målbeloppet är 0 (regression för handleCreate-buggen)", async () => {
    renderModal();
    await fillNameAndAccount();

    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "0" } });
    fireEvent.click(screen.getByRole("button", { name: "Skapa sparmål" }));

    expect(await screen.findByText("Målbeloppet måste vara en siffra större än 0.")).toBeInTheDocument();
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("skickar INTE sparmålet om målbeloppet saknas", async () => {
    renderModal();
    await fillNameAndAccount();

    fireEvent.click(screen.getByRole("button", { name: "Skapa sparmål" }));

    expect(await screen.findByText("Du måste fylla i ett målbelopp.")).toBeInTheDocument();
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("skickar sparmålet när alla fält är giltiga", async () => {
    renderModal();
    await fillNameAndAccount();

    fireEvent.change(screen.getByPlaceholderText("Belopp"), { target: { value: "5000" } });
    fireEvent.click(screen.getByRole("button", { name: "Skapa sparmål" }));

    await vi.waitFor(() => expect(mockPost).toHaveBeenCalledTimes(1));
    expect(mockPost).toHaveBeenCalledWith("/savings-goals", {
      name: "Resa till Japan",
      targetAmount: 5000,
      accountId: 1,
    });
  });
});
