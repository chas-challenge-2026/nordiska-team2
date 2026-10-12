import { downloadTaxReport, useAvailableTaxReportYears } from "../../hooks/useTaxReports"
import { useAccounts } from "../../hooks/useAccounts";
import Button from "../../components/ui/Button";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";
import { useTranslation } from "react-i18next";
import PageHeader from "../../components/ui/Pageheader";

export default function TaxReportsPage() {
    const { data: accounts, isLoading: accountsLoading } = useAccounts();
    const { t } = useTranslation();
    const accountId = accounts?.[0]?.id ?? NaN;

    const { data: years, isLoading: yearsLoading, isError } = useAvailableTaxReportYears(accountId);


    if (accountsLoading || yearsLoading) return <p>{t("common.loading")}</p>
    if (isError) return <p>{t("taxReports.error")}</p>

    return (
        <>
            <div>
                <PageHeader title={t("taxReports.title")}/>
                 <Card title={t("taxReports.available")}
                        headerVariant="secondary">
                    <ul className="divide-y divide-border-brand">
                        {years?.map((year) => (
                            <ListItem
                                key={year} 
                                title={t("taxReports.reportFor", { year })}
                                subtitle={t("taxReports.contains")}
                                right= {
                                    <Button className="cursor-pointer "
                                        label={t("taxReports.download")}
                                        onClick={() => downloadTaxReport(accountId, year)} 
                                    />
                                }
                            />
                        ))}
                    </ul>
                    {years?.length === 0 && <p>{t("taxReports.none")}</p>}
                </Card>
            </div>
        </>
    )
}
