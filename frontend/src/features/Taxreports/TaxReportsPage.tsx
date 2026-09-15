import { downloadTaxReport, useAvailableTaxReportYears } from "../../hooks/useTaxReports"


// TILLFÄLLIGT KOD MED ANNA (id 1)
const ANNA_ACCOUNT_ID = 1; 

export default function TaxReportsPage() {
    const { data: years, isLoading, isError } = useAvailableTaxReportYears(ANNA_ACCOUNT_ID)

    if (isLoading) return <p>Laddar...</p>
    if (isError) return <p>Kunde inte hämta skatterapport.</p>

    return (
        <div>
            <h2>Skatterapporter</h2>
            <ul>
                {years?.map((year) => (
                    <li key={year} className="flex">
                        {year}
                        <button onClick={() => downloadTaxReport(ANNA_ACCOUNT_ID, year)}>
                            Ladda ner
                        </button>
                    </li>
                ))}
            </ul>
            {years?.length === 0 && <p>Inga rapporter hittades för kontot.</p>}
        </div>
    )
}
