import { useState } from "react";
import { useTranslation } from "react-i18next";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";
import Button from "../../components/ui/Button";
import SelectOptions from "../../components/ui/Select";
import type { OptionType } from "../../components/ui/Select";
import { downloadBankStatement } from "../../hooks/useBankStatements";

function getLastMonths(count: number) {
  const now = new Date();
  return Array.from({ length: count }, (_, i) => {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    return { year: d.getFullYear(), month: d.getMonth() + 1 }; // getMonth() är 0-indexerad, backend vill ha 1–12
  });
}

// Första bokstaven versal ("oktober 2026" -> "Oktober 2026")
function capitalize(text: string) {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

export default function BankStatementsCard({ accountId }: { accountId: number }) {
  const { t, i18n } = useTranslation();
  const months = getLastMonths(24);

  // "" betyder "alla", så att filtret alltid har ett valt värde
  const allOption: OptionType = { value: "", label: t("bankStatements.all") };
  const [yearFilter, setYearFilter] = useState<OptionType>(allOption);
  const [monthFilter, setMonthFilter] = useState<OptionType>(allOption);

  const yearOptions: OptionType[] = [
    allOption,
    ...[...new Set(months.map((m) => m.year))].map((year) => ({
      value: String(year),
      label: String(year),
    })),
  ];

  const monthOptions: OptionType[] = [
    allOption,
    ...Array.from({ length: 12 }, (_, i) => ({
      value: String(i + 1),
      label: capitalize(new Date(2000, i).toLocaleString(i18n.language, { month: "long" })),
    })),
  ];

  const filteredMonths = months.filter(
    ({ year, month }) =>
      (yearFilter.value === "" || year === Number(yearFilter.value)) &&
      (monthFilter.value === "" || month === Number(monthFilter.value))
  );

  return (
    <Card title={t("bankStatements.title")} headerVariant="secondary">
      <div className="flex gap-2 mb-2">
        <SelectOptions
          value={yearFilter}
          onChange={(option) => setYearFilter(option ?? allOption)}
          options={yearOptions}
          placeholder={t("bankStatements.year")}
          className="w-32 text-small"
        />
        <SelectOptions
          value={monthFilter}
          onChange={(option) => setMonthFilter(option ?? allOption)}
          options={monthOptions}
          placeholder={t("bankStatements.month")}
          className="w-40 text-small"
        />
      </div>

      {filteredMonths.length === 0 ? (
        <p className="text-small text-muted py-2">{t("bankStatements.empty")}</p>
      ) : (
        <ul className="divide-y divide-border-brand">
          {filteredMonths.map(({ year, month }) => {
            const label = capitalize(
              new Date(year, month - 1).toLocaleString(i18n.language, {
                month: "long",
                year: "numeric",
              })
            );
            return (
              <ListItem
                key={`${year}-${month}`}
                title={label}
                subtitle={t("taxReports.contains")}
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
      )}
    </Card>
  );
}
