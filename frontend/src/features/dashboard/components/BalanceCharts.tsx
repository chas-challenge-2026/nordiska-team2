import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Pie } from 'react-chartjs-2';
import type { Account } from '../../../types/account';
ChartJS.register(ArcElement, Tooltip, Legend);

const COLORS = ["#6F8998", "#f5a623", "#A8B5B9", "#D9D5C9", "#F2EEE4", "#356A89"]

type BalanceChartProps = {
    accounts: Account[];
}

export default function BalanceChart({ accounts }: BalanceChartProps ) {
    const funded = accounts.filter((account) => account.balance > 0);
    if (funded.length === 0) 
        return <p className="text-small text-muted">Inget saldo att visa</p>

    const data = {
        labels: funded.map((account) => account.accountNumber),
        datasets: [
            {
                label: "Saldo (kr)",
                data: funded.map((account) => account.balance),
                backgroundColor: funded.map((_, i) => COLORS[i]),
                borderColor: "#fcfcfb",
                borderWidth: 2,
            }
        ]
    }

    return ( 
        <div className="relative aspect-square
                        mx-auto w-full max-w-[180px] md:max-w-[200px] lg:max-w-[220px]">
            <Pie data={data} />
        </div>
    )

}