import { downloadTaxReport, useAvailableTaxReportYears, useGenerateTaxReport } from "../../hooks/useTaxReports"
import { useAccounts } from "../../hooks/useAccounts";
import { useState } from "react";

export default function TaxReportsPage() {
    const { data: accounts, isLoading: accountsLoading } = useAccounts();
    const accountId = accounts?.[0]?.id ?? NaN;

    const { data: years, isLoading: yearsLoading, isError } = useAvailableTaxReportYears(accountId);
    const generateMutation = useGenerateTaxReport(accountId);
    const [yearToGenerate, setYearToGenerate] = useState("");

    if (accountsLoading || yearsLoading) return <p>Laddar...</p>
    if (isError) return <p>Kunde inte hämta skatterapport.</p>

    return (
        <>
            <div>
                <h2>Skatterapporter</h2>
                <ul>
                    {years?.map((year) => (
                        <li key={year} className="flex">
                            
                            <button className="border border-brand p-1 rounded-default mb-2 hover:bg-white"
                            onClick={() => downloadTaxReport(accountId, year)}>
                                Ladda ner {year}
                            </button>
                        </li>
                    ))}
                </ul>
                {years?.length === 0 && <p>Inga rapporter hittades för kontot.</p>}
            </div>
            <div>
                <input className="border border-brand mr-5 mt-5 p-1 rounded-default"
                    type="year"
                    placeholder="År"
                    value={yearToGenerate}
                    onChange={(e) => setYearToGenerate(e.target.value)}
                />
                <button className="border border-brand p-1 rounded-default hover:bg-white"
                    onClick={() => generateMutation.mutate(Number(yearToGenerate))}
                >
                    Hämta dokument
                </button>

            </div>
        </>
    )
}
