import { useState } from "react";
import axios from "axios";
import { useTranslation } from "react-i18next";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";
import Button from "../../components/ui/Button";
import { downloadBankStatement } from "../../hooks/useBankStatements";
import { useAlert } from "../../hooks/useAlert";
import { apiErrorMessage } from "../../lib/apiErrorMessage";

function getLastMonths(count: number) {
  const now = new Date();
  return Array.from({ length: count }, (_, i) => {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    return { year: d.getFullYear(), month: d.getMonth() + 1 }; // getMonth() är 0-indexerad, backend vill ha 1–12
  });
}

export default function BankStatementsCard({ accountId }: { accountId: number }) {
  const { t, i18n } = useTranslation();
  const months = getLastMonths(12);
  const { showAlert } = useAlert();
  // Vilken månad som laddas ner just nu, t.ex. "2026-10". null = ingen.
  const [downloading, setDownloading] = useState<string | null>(null);

  async function handleDownload(year: number, month: number) {
    setDownloading(`${year}-${month}`);
    try {
      await downloadBankStatement(accountId, year, month);
    } catch (err) {
      // 404 = inget kontoutdrag för den månaden. Övriga fel tolkas av apiErrorMessage.
      const message =
        axios.isAxiosError(err) && err.response?.status === 404
          ? t("bankStatements.notFound")
          : apiErrorMessage(err, t);
      showAlert({ type: "error", title: t("bankStatements.error"), message });
    } finally {
      setDownloading(null);
    }
  }

  return (
    <Card title={t("bankStatements.title")} headerVariant="secondary">
      <ul className="divide-y divide-border-brand">
        {months.map(({ year, month }) => {
          const monthName = new Date(year, month - 1).toLocaleString(i18n.language, {
            month: "long",
            year: "numeric",
          });
          const label = monthName.charAt(0).toUpperCase() + monthName.slice(1);
          return (
            <ListItem
              key={`${year}-${month}`}
              title={label}
              subtitle={t("taxReports.contains")}
              right={
                <Button
                  className="cursor-pointer"
                  label={
                    downloading === `${year}-${month}`
                      ? t("common.loading")
                      : t("bankStatements.download")
                  }
                  disabled={downloading !== null}
                  onClick={() => handleDownload(year, month)}
                />
              }
            />
          );
        })}
      </ul>
    </Card>
  );
}