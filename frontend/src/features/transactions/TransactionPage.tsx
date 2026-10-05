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
import CreateSavingsGoalsModal from "../dashboard/components/modals/SavingsGoalsModal/CreateSavingsGoalsModal";
import DeleteSavingsGoalModal from "../dashboard/components/modals/SavingsGoalsModal/DeleteSavingsGoalModal";
import { useSavingsGoals } from "../../hooks/useSavingsGoals";
import confetti from "canvas-confetti"


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
  const [isDeleteGoalModalOpen, setIsDeleteGoalModalOpen] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [mode, setMode] = useState<"deposit" | "withdraw">("deposit");
  const { data: goals } = useSavingsGoals();



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
  const accountGoals = goals?.filter((goal) => goal.accountId === selectedAccountId) ?? [];
  const goalOptions: OptionType[] = accountGoals.map((goal) => ({
    value: String(goal.id),
    label: goal.name,
  }))
  const [selectedGoalOption, setSelectedGoalOption] = useState<OptionType | null>(null)

  useEffect(() => {
    if (accountGoals.length === 0) {
      setSelectedGoalOption(null);
      return;
    }

    const stillValid = selectedGoalOption && accountGoals.some((g) => String(g.id) === selectedGoalOption.value);
    if (!stillValid) {
      setSelectedGoalOption({ value: String(accountGoals[0].id), label: accountGoals[0].name })
      }
     }, [selectedAccountId, goals]);


  const accountGoal = accountGoals.find((g) => String(g.id) === selectedGoalOption?.value);
  const progress = accountGoal?.progressPercent ?? 0;
  const currentAmount = accountGoal?.currentAmount ?? 0;

  useEffect(() => {
    if (!accountGoal || progress < 100) return;

    const storageKey = `goal-celebrated-${accountGoal.id}`;
    const alreadyCelebrated = localStorage.getItem(storageKey) === "true";

    if(!alreadyCelebrated) {
      confetti({
        particleCount: 150,
        spread: 70,
        origin: { y: 0.6 },
      });
      localStorage.setItem(storageKey, "true")
    }
  }, [accountGoal?.id, progress >= 100]);

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
      queryClient.invalidateQueries({ queryKey: ["savings-goals"]})
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

  const dateFormatter = new Intl.DateTimeFormat("sv-SE", { 
    dateStyle: "short" })

  const amountFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK"
});
  return (
    <>
     <div className="flex flex-col gap-3 mb-3">
    {/* ============ ÖVRE RADEN: rubrik + kontoval ============ */}
      <div className="flex justify-between">
        <div>
          <h1 className="text-xl sm:text-title">Kontohantering</h1>
          <p className="text-muted text-small mb-5">
              Sätt in, ta ut, överför pengar mellan konto och följ dina sparmål.
          </p>
        </div>
        <div>
          <p className="text-xsmall text-muted mb-1">Välj konto</p>
          <SelectOptions 
            value={selectedOption}
            onChange={(option) => setSelectedAccountId(option ? Number(option.value): null)}
            options={accountOptions}
            className="w-100 self-end text-small"
          />
        </div>
      </div>
      <div>
         <Button 
            label="Hantera sparmål"
            variant="dropDown"
            onClick={() => setIsMenuOpen((open) => !open)}
          ><span>▼</span> </Button>
          {isMenuOpen && (
            <ul className="absolute z-10 w-1/4 ml-2 p-2
                            rounded-default border-border bg-card 
                            shadow-sm text-small"
            >
              <li>
                <button className="w-full text-left p-2 hover:bg-background"
                        onClick={() => {
                          setIsMenuOpen(false)
                          setIsGoalModalOpen(true)
                        }}
                >
                  Skapa sparmål
                </button>
              </li>
              <li>
                <button className="w-full text-left p-2 hover:bg-background"
                        onClick={() => {
                            setIsMenuOpen(false);
                            setIsDeleteGoalModalOpen(true);
                        }}
                >
                    Ta bort sparmål
                </button>
              </li>
            </ul>
          )}
      </div>

    {/* ============ UNDRE RADEN: de tre saldokorten ============ */}
    <div className="flex gap-3">
      <Card 
        title="hej"
        subtitle="Saldo" 
        headerVariant="secondary"
        headerClassName="min-h-25"
        >
          <div>
            {selectedAccount && <p className="mt-2">
              {amountFormatter.format(selectedAccount.balance)}
              </p>}
          </div>
      </Card>
      <Card 
        headerContent = {
          <>
            {accountGoals.length > 1 ? (
              <SelectOptions
                value={selectedGoalOption}
                onChange= {setSelectedGoalOption}
                options={goalOptions}
                className="w-full border-none text-medium font-semibold" />
            ) : ( 
              <h2 className="font-semibold text-medium">{accountGoal?.name ?? "Sparmål"}</h2>
            )}
              <p className="text-small opacity-85">Sparmål</p>
          </>
        }
        headerVariant="secondary"
        headerClassName="min-h-25"
        
        >
          <div>
            {accountGoal ? ( 
              <>
                <div
                    role="progressbar"
                    aria-label={`Sparmål: ${accountGoal.name}`}
                    aria-valuemin={0}
                    aria-valuemax={accountGoal.targetAmount}
                    aria-valuenow={currentAmount}
                    className="h-3 overflow-hidden rounded-full bg-border-light">
                      <div className={`h-full rounded-full bg-accent ${progress >= 100 ? "bg-success" : "bg-accent"}`}
                            style={{ width: `${progress}%` }} />
                </div> 
                <p className="mt-2 text-small">
                  {amountFormatter.format(currentAmount)} / {amountFormatter.format(accountGoal.targetAmount)}
                </p>
              </>              
            ) : (
                <p className="mt-2 text-muted text-small">Ingen sparmål för detta konto.</p>
            )}
          </div>
      </Card>
      <Card 
        subtitle="Saldo" 
        headerVariant="secondary"
        headerClassName="min-h-25"
        >
          <div>
            {selectedAccount && <p className="mt-2">
              {selectedAccount.balance.toFixed(2)} kr</p>}
          </div>
      </Card>
    </div>
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

            <div className="relative bg-border flex rounded-default border border-border overflow-hidden">
              <div className={`absolute inset-y-0 w-1/2 bg-white border-2 border-border rounded-default
                  transition-transform duration-500 ease-in-out
                  ${mode === "deposit" ? "translate-x-0" : "translate-x-full"}`} />
                <button
                  type="button"
                  className={`relative z-10 flex-1 p-2 transition-colors 
                              ${mode === "deposit" ? "text-brand" : "text-muted bg-border"}`}
                  onClick={() => setMode("deposit")}>
                    Insättning
                </button>
                <button
                  type="button"
                  className={`relative z-10 flex-1 p-2 transition-colors
                              ${mode === "withdraw" ? "text-brand" : "text-muted bg-border"}`}
                  onClick={() => setMode("withdraw")}>
                    Uttag
                </button>
              </div>

            <div>
              <InputField className="p-1"
                placeholder="Belopp"
                value={amount}
                onChange={setAmount}
                type="number"
              />
                <p className="mt-2 text-xsmall text-muted mb-3">
                    {mode === "deposit"
                    ? `Efter insättning: ${amountFormatter.format(previewDepositBalance)} kr`
                    : `Efter uttag: ${amountFormatter.format(previewWithdrawBalance)} kr` }
                </p>
              <div className="flex gap-3 mt-2">
                {[100, 500, 1000].map((present) => (
                  <Button 
                    key={present}
                    label={`${present} kr`}
                    variant="primary"
                    onClick={() => setAmount(String(present))} 
                    />
                ))}
              </div>


              <Button
                variant="secondary"
                className="w-full mt-5"
                label={ 
                  mode === "deposit"
                    ? (depositMutation.isPending ? "Sätter in..." : "Sätt in pengar")
                    : (withdrawMutation.isPending ? "Tar ut..." : "Ta ut pengar")
                }
                onClick={() => (mode === "deposit" ? depositMutation.mutate() : withdrawMutation.mutate())}
                disabled={parsedAmount <= 0 || depositMutation.isPending|| withdrawMutation.isPending}
              />
                
              {(depositMutation.isError || withdrawMutation.isError) && (
                <p>
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
                    subtitle={entry.description}
                    right={
                          <div className="flex flex-col items-end gap-1">
                            <p className={entry.amount >= 0
                                                ? "font-semibold text-success text-medium whitespace-nowrap"
                                                : "font-semibold text-foreground text-medium"}>
                                                    {entry.amount > 0 
                                                        ? "+"
                                                        : ""}
                                                    {amountFormatter.format(entry.amount)}</p>
                            <p className="text-xsmall text-muted whitespace-nowrap">{dateFormatter.format(new Date(entry.date))}</p> 
                          </div>
                          }  
                  />
                ))}
              </ul>
            )}
      </Card>
      <CreateSavingsGoalsModal isOpen={isGoalModalOpen} onClose={() => setIsGoalModalOpen(false)} />
      <DeleteSavingsGoalModal isOpen={isDeleteGoalModalOpen} onClose={() => setIsDeleteGoalModalOpen(false)} />

    </>
  );
}