import { useState, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../client";
import { useAccounts } from "../../hooks/useAccounts";
import SelectOptions from "../../components/ui/Select";
import type { OptionType } from "../../components/ui/Select";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";
import Button from "../../components/ui/Button";
import TransactionsChart from "./TransactionChart";
import DepositWithdrawFields from "../../components/forms/DepositWithdrawFields";
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
  const [searchParams] = useSearchParams();
  const [explicitAccountId, setExplicitAccountId] = useState<number | null>(() => {
      const accountIdParam = searchParams.get("accountId");
      return accountIdParam ? Number(accountIdParam) : null;
  });

  const [description, setDescription] = useState("");
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

  const selectedAccountId = explicitAccountId ?? accounts?.[0]?.id ?? null;
  const selectedOption = accountOptions.find((o) => o.value === String(selectedAccountId)) ?? null;

  const selectedAccount = accounts?.find((a) => a.id === selectedAccountId);
  const accountGoals = goals?.filter((goal) => goal.accountId === selectedAccountId) ?? [];
  const goalOptions: OptionType[] = accountGoals.map((goal) => ({
    value: String(goal.id),
    label: goal.name,
  }))
  const [explicitGoalOption, setSelectedGoalOption] = useState<OptionType | null>(null)
  const isExplicitGoalValid = explicitGoalOption && accountGoals.some((g) => String(g.id) === explicitGoalOption.value);
  const selectedGoalOption = isExplicitGoalValid 
    ? explicitGoalOption 
    : (accountGoals[0] 
      ? { value: String(accountGoals[0].id), label: accountGoals[0].name } 
      : null);

  const accountGoal = accountGoals.find((g) => String(g.id) === selectedGoalOption?.value);
  const progress = accountGoal?.progressPercent ?? 0;
  const currentAmount = accountGoal?.currentAmount ?? 0;

  useEffect(() => {
    if (!accountGoal || progress < 100) return;

    const storageKey = `goal-celebrated-${accountGoal.name}`;
    const alreadyCelebrated = localStorage.getItem(storageKey) === "true";

    if(!alreadyCelebrated) {
      confetti({
        particleCount: 150,
        spread: 70,
        origin: { y: 0.6 },
      });
      localStorage.setItem(storageKey, "true")
    }
  }, [accountGoal?.name, progress >= 100]);

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
        description: description.trim() === "" ? undefined : description.trim(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", selectedAccountId] });
      queryClient.invalidateQueries({ queryKey: ["savings-goals"]})
      setAmount("");
      setDescription("");
    },
  });

  const withdrawMutation = useMutation({
    mutationFn: async () => {
      return apiClient.post("/transactions/withdraw", {
        accountId: selectedAccountId,
        amount: parsedAmount,
        description: description.trim() === "" ? undefined : description.trim(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", selectedAccountId] });
      setAmount("");
      setDescription("");
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
      <div className="flex flex-col sm:flex-row gap-3 sm:justify-between">
          <div>
            <h1 className="text-xl sm:text-title">Kontohantering</h1>
            <p className="text-muted text-small mb-5">
                Sätt in, ta ut, överför pengar mellan konto och följ dina sparmål.
            </p>
          </div>

        <div className="hidden md:block">
          <p className="text-xsmall text-muted mb-1">Välj konto</p>
          <SelectOptions 
            value={selectedOption}
            onChange={(option) => setExplicitAccountId(option ? Number(option.value): null)}
            options={accountOptions}
            className="w-100 self-end text-small"
          />
        </div>
      </div>

      <div className=" relative hidden md:block">
         <Button 
            label="Hantera sparmål"
            variant="dropDown"
            onClick={() => setIsMenuOpen((open) => !open)}
          ><span>▼</span> </Button>
          {isMenuOpen && (
            <ul className="absolute z-10 w-1/4 mt-0.5
                            rounded-default border border-border bg-card 
                            shadow-sm text-small"
            >
              <li>
                <button className="w-full text-left hover:bg-background px-2 py-2"
                        onClick={() => {
                          setIsMenuOpen(false)
                          setIsGoalModalOpen(true)
                        }}
                >
                  Skapa sparmål
                </button>
              </li>
              <li>
                <button className="w-full text-left hover:bg-background px-2 py-2"
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
    <div className="flex gap-3 flex-col md:flex-row">
      <Card 
        title={selectedAccount?.name ?? "Konto"}
        subtitle="Saldo" 
        headerVariant="secondary"
        headerClassName="min-h-25"
        >
          <div>
            <p className="mt-2 text-small text-muted">Tillgänglig saldo</p>
            {selectedAccount && <p className="mt-2 text-balance font-bold text-brand">
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
                className="-ml-1 w-full border-none text-medium font-semibold" />
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
        title="Senaste händelse" 
        subtitle="Insättning/Uttag/Överföring"
        headerVariant="secondary"
        headerClassName="min-h-25"
        >
          <div>
            {history && history.length > 0 
              ? ( 
                <> 
                  <p className="mt-2 text-small text-muted">
                    {history[0].description}</p>
                    <p className={history[0].amount >= 0 
                      ? "text-success font-bold text-balance"
                      : "text-balance font-bold text-brand"}>
                          {amountFormatter.format(history[0].amount)}
                    </p>
                  </>
            ) : (
                  <p className="mt-2 text-muted text-small">Inga händelser än.</p>
                )}
          </div>
      </Card>
    </div>
</div>

    <div className="grid grid-cols-1 justify-items-stretch 
                            md:grid-cols-2 gap-3 md:mb-3">

      {/* ============ VÄNSTER SIDA ============ */}
      <div>
        <Card title="Insättning/Uttag"
              headerVariant="secondary"
              className="min-h-106"
                >
          <div className="flex flex-col
                          text-small">

            <DepositWithdrawFields
              mode={mode}
              setMode={setMode}
              amount={amount}
              setAmount={setAmount}
              amountInputClassName="p-1"
              description={description}
              setDescription={setDescription}
            />

            <div>
                <p className="mt-2 text-xsmall text-muted mb-3">
                    {mode === "deposit"
                    ? `Efter insättning: ${amountFormatter.format(previewDepositBalance)} kr`
                    : `Efter uttag: ${amountFormatter.format(previewWithdrawBalance)} kr` }
                </p>

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
            title="Översikt"
            headerVariant="secondary"
            className="hidden md:flex min-h-106"
            >
              <TransactionsChart 
                history={history ?? []}
                currentBalance={selectedAccount?.balance ?? 0} />
        </Card>
      </div>

    </div>


      <Card title="Historik"
            headerVariant="secondary">
  
            {historyLoading ? (
              <p>Laddar historik...</p>
            ) : (
              <ul className="divide-y divide-border-light ">
                {history?.map((entry, index) => (
                  <ListItem 
                    key={index}
                    title={entry.description}
                    subtitle={selectedAccount?.accountNumber}
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