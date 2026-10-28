import { useTranslation } from "react-i18next";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";
import Button from "../../components/ui/Button";
import { downloadBankStatement } from "../../hooks/useBankStatements";

function getLastMonths(count: number) {
  const now = new Date();
  return Array.from({ length: count }, (_, i) => {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    return { year: d.getFullYear(), month: d.getMonth() + 1 }; // +1 se nedan!
  });
}

export default function BankStatementsCard({ accountId }: { accountId: number }) {
  const { t, i18n } = useTranslation();
  const months = getLastMonths(12);

  return (
    <Card title={t("bankStatements.title")} headerVariant="secondary">
      <ul className="divide-y divide-border-brand">
        {months.map(({ year, month }) => {
          const label = new Date(year, month - 1).toLocaleString(i18n.language, {
            month: "long",
            year: "numeric",
          });
          return (
            <ListItem
              key={`${year}-${month}`}
              title={label}
              right={
                <Button
                  className="cursor-pointer"
                  label={t("bankStatements.download")}
                  onClick={() => downloadBankStatement(accountId, year, month)}
                />
              }
            />
          );
        })}
      </ul>
    </Card>
  );
}