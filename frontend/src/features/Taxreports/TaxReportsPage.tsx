import { downloadTaxReport, useAvailableTaxReportYears } from "../../hooks/useTaxReports"
import { useAccounts } from "../../hooks/useAccounts";
import Button from "../../components/ui/Button";
import Card from "../../components/cards/Card";
import ListItem from "../../components/ui/ListItem";

export default function TaxReportsPage() {
    const { data: accounts, isLoading: accountsLoading } = useAccounts();
    const accountId = accounts?.[0]?.id ?? NaN;

    const { data: years, isLoading: yearsLoading, isError } = useAvailableTaxReportYears(accountId);


    if (accountsLoading || yearsLoading) return <p>Laddar...</p>
    if (isError) return <p>Kunde inte hämta skatterapport.</p>

    return (
        <>
            <div>
                <h1 className="text-xl sm:text-title mb-5">Skatterapporter</h1>
                 <Card title="Tillgängliga skatterapporter"
                        headerVariant="secondary">
                    <ul className="divide-y divide-border-brand">
                        {years?.map((year) => (
                            <ListItem
                                key={year} 
                                title={`Skatterapport för ${year}`}
                                subtitle="Innehåller ekonomiska information"
                                right= {
                                    <Button className="cursor-pointer "
                                        label="Ladda ner"
                                        onClick={() => downloadTaxReport(accountId, year)} 
                                    />
                                }
                            />
                        ))}
                    </ul>
                    {years?.length === 0 && <p>Inga rapporter hittades för kontot.</p>}
                </Card>
            </div>
        </>
    )
}
