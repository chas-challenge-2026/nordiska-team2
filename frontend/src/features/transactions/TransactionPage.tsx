import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../client";

interface Account {
  id: number;
  accountNumber: string;
  accountType: string;
  interestRate: number;
  balance: number;
}

interface LedgerEntry {
  date: string;
  description: string;
  amount: number;
}

export default function TransactionPage() {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  /*
    TODO: this fetches ALL of the logged-in customer's
          accounts but just uses the first one. Once there's a real account
          picker/selector UI, replace this with whichever account the user
          has actually selected.
  */
  const { data: accounts, isLoading: accountsLoading } = useQuery<Account[]>({
    queryKey: ["accounts"],
    queryFn: async () => {
      const response = await apiClient.get("/api/accounts");
      return response.data;
    },
  });

  const selectedAccount = accounts?.[0];

  const { data: history, isLoading: historyLoading } = useQuery<LedgerEntry[]>({
    queryKey: ["transactions", selectedAccount?.id],
    queryFn: async () => {
      const response = await apiClient.get(`/api/transactions/${selectedAccount!.id}`);
      return response.data;
    },
    enabled: !!selectedAccount, // NOTE: Don't run until actual account id are implemented
  });

  // Realtime balance preview 
  // Pure client-side math without API call.
  // Recalculates on every keystroke since `amount` is in the dependency path implicitly,
  // worth revisiting with useMemo if the page grows more expensive.
  const parsedAmount = parseFloat(amount) || 0;
  const previewDepositBalance = (selectedAccount?.balance ?? 0) + parsedAmount;
  const previewWithdrawBalance = (selectedAccount?.balance ?? 0) - parsedAmount;

  const depositMutation = useMutation({
    mutationFn: async () => {
      return apiClient.post("/api/transactions/deposit", {
        accountId: selectedAccount!.id,
        amount: parsedAmount,
      });
    },
    onSuccess: () => {
      // Refetch both, Balance changed (accounts) AND a new ledger entry exists (transactions). 
      // TODO: consider a success message/toast here instead of just silently refreshing.
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", selectedAccount?.id] });
      setAmount("");
    },
  });

  const withdrawMutation = useMutation({
    mutationFn: async () => {
      return apiClient.post("/api/transactions/withdraw", {
        accountId: selectedAccount!.id,
        amount: parsedAmount,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", selectedAccount?.id] });
      setAmount("");
    },
  });

  if (accountsLoading) return <p>Laddar konton...</p>;
  if (!selectedAccount) return <p>Inga konton hittades.</p>;

  return (
    <div>
      <h1>Transaktioner</h1>

      <p>
        Konto: {selectedAccount.accountNumber} — Saldo: {selectedAccount.balance.toFixed(2)} kr
      </p>

      {/* TODO: Replace with real styling/layout */}
      <div>
        <input
          type="number"
          placeholder="Belopp"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />

        {parsedAmount > 0 && (
          <p>
            Efter insättning: {previewDepositBalance.toFixed(2)} kr
            {" | "}
            Efter uttag: {previewWithdrawBalance.toFixed(2)} kr
          </p>
        )}

        <button
          onClick={() => depositMutation.mutate()}
          disabled={parsedAmount <= 0 || depositMutation.isPending}
        >
          {depositMutation.isPending ? "Sätter in..." : "Sätt in"}
        </button>

        <button
          onClick={() => withdrawMutation.mutate()}
          disabled={parsedAmount <= 0 || withdrawMutation.isPending}
        >
          {withdrawMutation.isPending ? "Tar ut..." : "Ta ut"}
        </button>

        {(depositMutation.isError || withdrawMutation.isError) && (
          <p>
            {/* TODO: The backend returns two different error
                      shapes, {"{ error: string }"} for business failures (e.g.
                      insufficient balance) and a property-keyed object for
                      validation failures. Handle both, don't assume one shape. */}
            Ett fel uppstod. Försök igen.
          </p>
        )}
      </div>

      <h2>Historik</h2>
      {/* TODO: spec calls for a VIRTUALIZED list here
                (e.g. @tanstack/react-virtual, which pairs naturally with react-query already in use). 
                Current plain list is fine for a handful of entries but won't scale to a long transaction
                history without one. */}
      {historyLoading ? (
        <p>Laddar historik...</p>
      ) : (
        <ul>
          {history?.map((entry, index) => (
            <li key={index}>
              {entry.date} — {entry.description} — {entry.amount.toFixed(2)} kr
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}