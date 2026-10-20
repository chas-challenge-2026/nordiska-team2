import { useState, useEffect } from "react";
import { useSearchParams, useLocation } from "react-router-dom";
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
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../language/useFormatters";
import { transactionLabel, transactionTypeLabel } from "../../features/transactions/transactionLabel";
import type { TransactionType } from "../../types/transaction";
import PageHeader from "../../components/ui/Pageheader";
import CreateAccountModal from "../dashboard/components/modals/NewAccountModal/CreateAccountModal"
import DeleteAccountModal from "../dashboard/components/modals/NewAccountModal/DeleteAccountModal";
import { apiErrorMessage } from "../../lib/apiErrorMessage"

interface LedgerEntry {
  date: string;
  description: string | null;
  type: TransactionType;
  counterpartAccountNumber: string | null;
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
  const [isGoalMenuOpen, setIsGoalMenuOpen] = useState(false)
  const [mode, setMode] = useState<"deposit" | "withdraw">("deposit");
  const { data: goals } = useSavingsGoals();
  const { t } = useTranslation();
  const { locale, currency: amountFormatter } = useFormatters();

const [isCreateAccountModalOpen, setIsCreateAccountModalOpen] = useState(false);
const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] = useState(false);
  const [isAccountMenuOpen, setIsAccountMenuOpen] = useState(false)

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
      queryClient.invalidateQueries({ queryKey: ["savings-goals"] });
      setAmount("");
      setDescription("");
    },
  });

  // The mutation for the tab the user is on, so the error shown always
  // belongs to what they just tried (each mutation remembers its last error).
  const activeMutation = mode === "deposit" ? depositMutation : withdrawMutation;

  // Switching account starts fresh: an error from another account's
  // attempt shouldn't stay on screen.
  function selectAccount(option: OptionType | null) {
    setExplicitAccountId(option ? Number(option.value) : null);
    depositMutation.reset();
    withdrawMutation.reset();
  }

  const location = useLocation();
  useEffect(() => {
    if (!accountsLoading && accounts && location.hash === "#recenttransactions") {
      document.getElementById("recenttransactions")?.scrollIntoView({ behavior: "smooth"});
    }
  }, [location.hash, accountsLoading, accounts])
  
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
            <PageHeader 
              title={t("accountsPage.title")}
              subtitle={t("accountsPage.subtitle")} />
          </div>

        <div className="hidden md:block">
          <p className="text-xsmall text-muted mb-1">{t("common.selectAccount")}</p>
          <SelectOptions 
            value={selectedOption}
            onChange={selectAccount}
            options={accountOptions}
            className="w-100 self-end text-small"
          />
        </div>
      </div>

      {/*========== Mobilversion ==========*/}
      <div className="flex flex-col md:hidden gap-0.3 sm:gap-3 ">
          <div>
            <p className="text-xsmall text-muted mb-1 hidden">{t("common.selectAccount")}</p>
            <SelectOptions 
              value={selectedOption}
              onChange={selectAccount}
              options={accountOptions}
              className="w-full self-end text-small rounded-none sm:rounded-card"
            />
          </div>
          <div>
            <div className="relative">
              {/*========== Hantera sparmål ==========*/}
              <Button 
                  label={t("accountsPage.manageGoals")}
                  className="w-full rounded-none sm:rounded-card"
                  variant="dropDown"
                  onClick={() => setIsGoalMenuOpen((open) => !open)}
                ><span>▼</span> </Button>
                {isGoalMenuOpen && (
                  <ul className="absolute z-10 w-full right-0 mt-0.5
                                  rounded-default border border-border bg-card 
                                  shadow-sm text-small"
                  >
                    <li>
                      <button className="w-full text-left hover:bg-background px-2 py-2"
                              onClick={() => {
                                setIsGoalMenuOpen(false)
                                setIsGoalModalOpen(true)
                              }}
                      >
                        {t("accountsPage.createGoal")}
                      </button>
                    </li>
                    <li>
                      <button className="w-full text-left hover:bg-background px-2 py-2"
                              onClick={() => {
                                  setIsGoalMenuOpen(false);
                                  setIsDeleteGoalModalOpen(true);
                              }}
                      >
                          {t("accountsPage.deleteGoal")}
                      </button>
                    </li>
                  </ul>
                )}
            </div>
        </div>
          {/*========== Hantera Konto ==========*/}
          <div className="relative">
            <Button 
              label={t("accountsPage.manageAccount")}
              className="w-full rounded-none sm:rounded-card"
              variant="dropDown"
              onClick={() => setIsAccountMenuOpen((open) => !open)}
            ><span>▼</span> </Button>
            {isAccountMenuOpen && (
              <ul className="absolute z-10 w-full right-0 mt-0.5
                              md:rounded-default border border-border bg-card 
                              shadow-sm text-small"
              >
                <li>
                  <button className="w-full text-left hover:bg-background px-2 py-2"
                          onClick={() => {
                            setIsAccountMenuOpen(false)
                            setIsCreateAccountModalOpen(true)
                          }}
                  >
                    {t("accountsPage.createAccount")}
                  </button>
                </li>
                <li>
                  <button className="w-full text-left hover:bg-background px-2 py-2"
                          onClick={() => {
                              setIsAccountMenuOpen(false);
                              setIsDeleteAccountModalOpen(true);
                          }}
                  >
                      {t("accountsPage.deleteAccount")}
                  </button>
                </li>
              </ul>
            )}
          </div>
        </div>


        {/* ========== Hantera sparmål ========== */}
      <div className="hidden md:flex md:flex-row gap-2">
      <div className="relative">
         <Button
            label={t("accountsPage.manageGoals")}
            variant="dropDown"
            onClick={() => setIsGoalMenuOpen((open) => !open)}
          ><span>▼</span> </Button>
          {isGoalMenuOpen && (
            <ul className="absolute z-10 w-full mt-0.5
                            rounded-default border border-border bg-card
                            shadow-sm text-small"
            >
              <li>
                <button className="w-full text-left hover:bg-background px-2 py-2"
                        onClick={() => {
                          setIsGoalMenuOpen(false)
                          setIsGoalModalOpen(true)
                        }}
                >
                  {t("accountsPage.createGoal")}
                </button>
              </li>
              <li>
                <button className="w-full text-left hover:bg-background px-2 py-2"
                        onClick={() => {
                            setIsGoalMenuOpen(false);
                            setIsDeleteGoalModalOpen(true);
                        }}
                >
                    {t("accountsPage.deleteGoal")}
                </button>
              </li>
            </ul>
          )}
      </div>
    
    {/* ========== Hantera konto ========== */}

      <div className="relative">
         <Button
            label={t("accountsPage.manageAccount")}
            variant="dropDown"
            onClick={() => setIsAccountMenuOpen((open) => !open)}
          ><span>▼</span> </Button>
          {isAccountMenuOpen && (
            <ul className="absolute z-10 w-full mt-0.5
                            rounded-default border border-border bg-card
                            shadow-sm text-small"
            >
              <li>
                <button className="w-full text-left hover:bg-background px-2 py-2"
                        onClick={() => {
                          setIsAccountMenuOpen(false)
                          setIsCreateAccountModalOpen(true)
                        }}
                >
                  {t("accountsPage.createAccount")}
                </button>
              </li>
              <li>
                <button className="w-full text-left hover:bg-background px-2 py-2"
                        onClick={() => {
                            setIsAccountMenuOpen(false);
                            setIsDeleteAccountModalOpen(true);
                        }}
                >
                    {t("accountsPage.deleteAccount")}
                </button>
              </li>
            </ul>
          )}
      </div>
      </div>


    {/* ============ UNDRE RADEN: de tre saldokorten ============ */}
    <div className="flex md:gap-3 flex-col md:flex-row">
      <Card 
        title={selectedAccount ? t(selectedAccount.name) : t("common.account")}
        subtitle={t("accountsPage.balance")} 
        headerVariant="secondary"
        headerClassName="min-h-25"
        >
          <div>
            <p className="mt-2 text-small text-muted">{t("accountCard.availableBalance")}</p>
            {selectedAccount?.balance !== undefined &&
              <p className="mt-2 text-balance font-bold text-brand">
                
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
                    {transactionLabel(history[0], t)}</p>
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
                            md:grid-cols-2 gap-3 md:mb-3">

      {/* ============ VÄNSTER SIDA ============ */}
      <div>
        <Card title={t("accountsPage.depositWithdrawTitle")}
              headerVariant="secondary"
              className="min-h-116"
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
                    ? t("accountsPage.afterDeposit", { amount: amountFormatter.format(previewDepositBalance) })
                    : t("accountsPage.afterWithdraw", { amount: amountFormatter.format(previewWithdrawBalance) }) }
                </p>

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
                {activeMutation.isError && (
                  <p role="alert" className="mt-3 text-xsmall text-cancel">
                    {apiErrorMessage(activeMutation.error, t)}
                  </p>
                )}
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
            className="hidden md:flex min-h-116"
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
                    title={transactionLabel(entry, t)}
                    subtitle={entry.description ? transactionTypeLabel(entry, t) : undefined}
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

      <CreateAccountModal isOpen={isCreateAccountModalOpen} onClose={() => setIsCreateAccountModalOpen(false)} />
      <DeleteAccountModal isOpen={isDeleteAccountModalOpen} onClose={() => setIsDeleteAccountModalOpen(false)} />

    </>
  );
}