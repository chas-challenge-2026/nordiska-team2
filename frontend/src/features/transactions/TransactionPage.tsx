import { useState, useEffect } from "react";
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
  const [selectedAccountId, setSelectedAccountId] = useState<number | null>(null);

  const { data: accounts, isLoading: accountsLoading } = useQuery<Account[]>({
    queryKey: ["accounts"],
    queryFn: async () => {
      const response = await apiClient.get("/accounts");
      return response.data;
    },
  });

  // Default to the first account once accounts load, but only if nothing's been explicitly selected yet.
  // Doesn't override a user's own choice on every refetch (ex. after a deposit).
  useEffect(() => {
    if (accounts && accounts.length > 0 && selectedAccountId === null) {
      setSelectedAccountId(accounts[0].id);
    }
  }, [accounts, selectedAccountId]);

  const selectedAccount = accounts?.find((a) => a.id === selectedAccountId);

  const { data: history, isLoading: historyLoading } = useQuery<LedgerEntry[]>({
    queryKey: ["transactions", selectedAccountId],
    queryFn: async () => {
      const response = await apiClient.get(`/transactions/${selectedAccountId}`);
      return response.data;
    },
    enabled: selectedAccountId !== null,
  });

  const parsedAmount = parseFloat(amount) || 0;
  const previewDepositBalance = (selectedAccount?.balance ?? 0) + parsedAmount;
  const previewWithdrawBalance = (selectedAccount?.balance ?? 0) - parsedAmount;

  const depositMutation = useMutation({
    mutationFn: async () => {
      return apiClient.post("/transactions/deposit", {
        accountId: selectedAccountId,
        amount: parsedAmount,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", selectedAccountId] });
      setAmount("");
    },
  });

  const withdrawMutation = useMutation({
    mutationFn: async () => {
      return apiClient.post("/transactions/withdraw", {
        accountId: selectedAccountId,
        amount: parsedAmount,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", selectedAccountId] });
      setAmount("");
    },
  });

  if (accountsLoading) return <p>Laddar konton...</p>;
  if (!accounts || accounts.length === 0) return <p>Inga konton hittades.</p>;

  return (
    <div>
      <h1>Transaktioner</h1>

      {/* TODO: replace with a real styled dropdown/selector */}
      <label>
        Konto:{" "}
        <select
          value={selectedAccountId ?? ""}
          onChange={(e) => setSelectedAccountId(Number(e.target.value))}
        >
          {accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.accountNumber} — {account.balance.toFixed(2)} kr
            </option>
          ))}
        </select>
      </label>

      {selectedAccount && (
        <p>
          Valt konto: {selectedAccount.accountNumber} — Saldo: {selectedAccount.balance.toFixed(2)} kr
        </p>
      )}

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
            {/* TODO: the backend returns two different error
                      shapes, {"{ error: string }"} for business failures (ex.
                      insufficient balance) and a property-keyed object for
                      validation failures. Should handle both. */}
            Ett fel uppstod. Försök igen.
          </p>
        )}
      </div>

      <h2>Historik</h2>
      {/* TODO: spec calls for a VIRTUALIZED list here
                (ex. @tanstack/react-virtual) */}
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