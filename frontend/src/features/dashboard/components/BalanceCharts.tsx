import { Chart as ChartJS, ArcElement, Tooltip } from 'chart.js';
import { Doughnut } from 'react-chartjs-2';
import type { Account } from '../../../types/account';
ChartJS.register(ArcElement, Tooltip);

const COLORS = ["#6F8998", "#f5a623", "#A8B5B9", "#D9D5C9", "#F2EEE4", "#356A89"]

type BalanceChartProps = {
    accounts: Account[];
}

export default function BalanceChart({ accounts }: BalanceChartProps ) {
    const funded = accounts.filter((account) => account.balance > 0);
    if (funded.length === 0)
        return <p className="text-small text-muted">Inget saldo att visa</p>

    const total = funded.reduce((sum, account) => sum + account.balance, 0);

    const data = {
        labels: funded.map((account) => account.accountNumber),
        datasets: [
            {
                label: "Saldo (kr)",
                data: funded.map((account) => account.balance),
                backgroundColor: funded.map((_, i) => COLORS[i % COLORS.length]),
                // Ingen kantfärg: mellanrummen (spacing) visar kortets bakgrund,
                // så diagrammet funkar i både ljust och mörkt läge.
                borderWidth: 0,
                spacing: 2,
            }
        ]
    }

    const options = {
        cutout: "65%",
        plugins: { legend: { display: false } },
    }

    return (
        <div className="flex items-center gap-5">
            <div className="relative aspect-square w-full max-w-[140px] shrink-0">
                <Doughnut data={data} options={options} />
            </div>

            <ul className="flex flex-col gap-2 text-small">
                {funded.map((account, i) => (
                    <li key={account.accountNumber} className="flex items-center gap-2">
                        <span aria-hidden="true"
                              className="size-3 shrink-0 rounded-sm"
                              style={{ backgroundColor: COLORS[i % COLORS.length] }} />
                        <span className="text-muted">
                            {account.accountNumber} <span className="sm:inline hidden"> · {Math.round((account.balance / total) * 100)} % </span>
                        </span>
                    </li>
                ))}
            </ul>
        </div>
    )
}
