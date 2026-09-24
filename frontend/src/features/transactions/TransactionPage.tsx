import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../client";
import { useAccounts } from "../../hooks/useAccounts";
import SelectOptions from "../../components/ui/Select";
import type { OptionType } from "../../components/ui/Select";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";
import Button from "../../components/ui/Button";
import InputField from "../../components/ui/Input";
import TransactionsChart from "./TransactionChart";
import CreateSavingsGoalsModal from "../dashboard/components/modals/CreateSavingsGoalsModal";

interface LedgerEntry {
  date: string;
  description: string;
  amount: number;
}

export default function TransactionPage() {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  const [selectedAccountId, setSelectedAccountId] = useState<number | null>(null);
  const [isGoalModalOpen, setIsGoalModalOpen] = useState(false);

  const { data: accounts, isLoading: accountsLoading } = useAccounts();
  const accountOptions: OptionType[] = accounts?.map((account) => ({
    value: String(account.id),
    label: account.accountNumber,
  })) ?? [];

const selectedOption = accountOptions.find((o) => o.value === String(selectedAccountId)) ?? null;

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

  const dateFormatter = new Intl.DateTimeFormat("sv-SE", { dateStyle: "short" })
  
  return (
    <>
      <h1 className="text-xl sm:text-title mb-5">Kontohantering</h1>
        <div className="mb-5">
          {/* <Button 
            label="Öppna ett konto"
            variant="secondary"
            onClick={() => setIsGoalModalOpen(true)}
          /> */}
          <Button 
            label="Hantera sparmål"
            variant="secondary"
            onClick={() => setIsGoalModalOpen(true)}
          />
        </div>

    <div className="grid grid-cols-1 justify-items-stretch 
                            md:grid-cols-1 xl:grid-cols-2 gap-3 mb-6">
      {/* ============ VÄNSTER SIDA ============ */}
      <div>
        <Card title="Insättning/Uttag"
              headerVariant="secondary"
                >
          <div className="flex flex-col gap-6
                          text-small">
            <div>
              <SelectOptions 
                  value={selectedOption}
                  onChange={(option) => setSelectedAccountId(option ? Number(option.value): null)}
                  options={accountOptions}
              />

              {selectedAccount && (
                <p className="mt-2">
                  Valt konto: {selectedAccount.accountNumber} — Saldo: {selectedAccount.balance.toFixed(2)} kr
                </p>
              )}
            </div>

            <div>
              <InputField className="p-1"
                placeholder="Belopp"
                value={amount}
                onChange={setAmount}
                type="number"
              />

              {parsedAmount > 0 && (
                <p>
                  Efter insättning: {previewDepositBalance.toFixed(2)} kr
                  {" | "}
                  Efter uttag: {previewWithdrawBalance.toFixed(2)} kr
                </p>
              )}

              <Button
                label={depositMutation.isPending ? "Sätter in..." : "Sätt in"}
                onClick={() => depositMutation.mutate()}
                disabled={parsedAmount <= 0 || depositMutation.isPending}
              />

              <Button
                label={withdrawMutation.isPending ? "Tar ut..." : "Ta ut"}
                onClick={() => withdrawMutation.mutate()}
                disabled={parsedAmount <= 0 || withdrawMutation.isPending}
              />
                
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
          </div>
        </Card>
    </div>

       {/* ============ HÖGER SIDA ============ */}
      <div>
        <Card 
            headerVariant="noHeader"
            >
              <TransactionsChart history={history ?? []} />
        </Card>
      </div>

    </div>


      <Card title="Historik"
            headerVariant="secondary">
  
            {historyLoading ? (
              <p>Laddar historik...</p>
            ) : (
              <ul className="divide-y divide-border-light">
                {history?.map((entry, index) => (
                  <ListItem 
                    key={index}
                    title={entry.description}
                    subtitle={dateFormatter.format(new Date(entry.date))}
                    right={<span>{entry.amount.toFixed(2)} kr</span>}  
                  />
                ))}
              </ul>
            )}
      </Card>
      <CreateSavingsGoalsModal isOpen={isGoalModalOpen} onClose={() => setIsGoalModalOpen(false)} />
    </>
  );
}