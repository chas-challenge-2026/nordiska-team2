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
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../language/useFormatters";


interface LedgerEntry {
  date: string;
  description: string;
  amount: number;
}

export default function TransactionPage() {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  const [explicitAccountId, setExplicitAccountId] = useState<number | null>(null);
  const [isGoalModalOpen, setIsGoalModalOpen] = useState(false);
  const [isDeleteGoalModalOpen, setIsDeleteGoalModalOpen] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [mode, setMode] = useState<"deposit" | "withdraw">("deposit");
  const { data: goals } = useSavingsGoals();
  const { t } = useTranslation();
  const { locale, currency: amountFormatter } = useFormatters();



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

  if (accountsLoading) return <p>{t("accountsPage.loadingAccounts")}</p>;
  if (!accounts || accounts.length === 0) return <p>{t("accountsPage.noAccounts")}</p>;

  const dateFormatter = new Intl.DateTimeFormat(locale, { 
    dateStyle: "short" })

  return (
    <>
     <div className="flex flex-col gap-3 mb-3">
    {/* ============ ÖVRE RADEN: rubrik + kontoval ============ */}
      <div className="flex flex-col sm:flex-row gap-3 sm:justify-between">
          <div>
            <h1 className="text-xl sm:text-title">{t("accountsPage.title")}</h1>
            <p className="text-muted text-small mb-5">
                {t("accountsPage.subtitle")}
            </p>
          </div>

        <div className="hidden md:block">
          <p className="text-xsmall text-muted mb-1">{t("common.selectAccount")}</p>
          <SelectOptions 
            value={selectedOption}
            onChange={(option) => setExplicitAccountId(option ? Number(option.value): null)}
            options={accountOptions}
            className="w-100 self-end text-small"
          />
        </div>
      </div>

      <div className="hidden md:block">
         <Button 
            label={t("accountsPage.manageGoals")}
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
                  {t("accountsPage.createGoal")}
                </button>
              </li>
              <li>
                <button className="w-full text-left p-2 hover:bg-background"
                        onClick={() => {
                            setIsMenuOpen(false);
                            setIsDeleteGoalModalOpen(true);
                        }}
                >
                    {t("accountsPage.deleteGoal")}
                </button>
              </li>
            </ul>
          )}
      </div>

    {/* ============ UNDRE RADEN: de tre saldokorten ============ */}
    <div className="flex gap-3 flex-col md:flex-row">
      <Card 
        title={selectedAccount?.name ?? t("common.account")}
        subtitle={t("accountsPage.balance")} 
        headerVariant="secondary"
        headerClassName="min-h-25"
        >
          <div>
            <p className="mt-2 text-small text-muted">{t("accountCard.availableBalance")}</p>
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
              <h2 className="font-semibold text-medium">{accountGoal?.name ?? t("common.savingsGoal")}</h2>
            )}
              <p className="text-small opacity-85">{t("common.savingsGoal")}</p>
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
                  aria-label={t("accountsPage.goalAria", { name: accountGoal.name })}
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
                <p className="mt-2 text-muted text-small">{t("accountsPage.noGoalForAccount")}</p>
            )}
          </div>
      </Card>
      <Card 
        title={t("accountsPage.latestActivity")}
        subtitle={t("accountsPage.latestActivitySubtitle")}
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
                  <p className="mt-2 text-muted text-small">{t("common.noActivityYet")}</p>
                )}
          </div>
      </Card>
    </div>
</div>

    <div className="grid grid-cols-1 justify-items-stretch 
                            md:grid-cols-2 gap-3 md:mb-6">

      {/* ============ VÄNSTER SIDA ============ */}
      <div>
        <Card title={t("accountsPage.depositWithdrawTitle")}
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
                    {t("accountsPage.deposit")}
                </button>
                <button
                  type="button"
                  className={`relative z-10 flex-1 p-2 transition-colors
                              ${mode === "withdraw" ? "text-brand" : "text-muted bg-border"}`}
                  onClick={() => setMode("withdraw")}>
                    {t("accountsPage.withdraw")}
                </button>
              </div>

            <div>
              <InputField className="p-1"
                placeholder={t("common.amount")}
                value={amount}
                onChange={setAmount}
                type="number"
              />
                <p className="mt-2 text-xsmall text-muted mb-3">
                    {mode === "deposit"
                    ? t("accountsPage.afterDeposit", { amount: amountFormatter.format(previewDepositBalance) })
                    : t("accountsPage.afterWithdraw", { amount: amountFormatter.format(previewWithdrawBalance) }) }
                </p>
              <div className="flex gap-3 mt-2">
                {[100, 500, 1000].map((present) => (
                  <Button 
                    key={present}
                    label={t("accountsPage.presetAmount", { amount: present })}
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
                    ? (depositMutation.isPending ? t("accountsPage.depositing") : t("accountsPage.depositMoney"))
                    : (withdrawMutation.isPending ? t("accountsPage.withdrawing") : t("accountsPage.withdrawMoney"))
                }
                onClick={() => (mode === "deposit" ? depositMutation.mutate() : withdrawMutation.mutate())}
                disabled={parsedAmount <= 0 || depositMutation.isPending|| withdrawMutation.isPending}
              />
                
              {(depositMutation.isError || withdrawMutation.isError) && (
                <p>
                  {t("common.genericError")}
                </p>
              )}
            </div>
          </div>
        </Card>
    </div>

       {/* ============ HÖGER SIDA ============ */}
      <div>
        <Card 
            title={t("accountsPage.chartTitle")}
            headerVariant="secondary"
            className="hidden md:block"
            >
              <TransactionsChart 
                history={history ?? []}
                currentBalance={selectedAccount?.balance ?? 0} />
        </Card>
      </div>

    </div>


      <Card title={t("accountsPage.history")}
            headerVariant="secondary">
  
            {historyLoading ? (
              <p>{t("accountsPage.loadingHistory")}</p>
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