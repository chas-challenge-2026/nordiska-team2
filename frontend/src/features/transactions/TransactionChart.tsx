import { Chart as ChartJS, LineElement, PointElement, CategoryScale,LinearScale, Tooltip, Legend } from "chart.js";
import { Line } from "react-chartjs-2";

ChartJS.register(LineElement, PointElement, CategoryScale, LinearScale, Tooltip, Legend);

type LedgerEntry = {
    date: string;
    description: string;
    amount: number;
};

function buildSeries(history: LedgerEntry[]) {
    const byDate = new Map<string, { deposits: number; withdrawals: number; }>();

    for(const entry of history) {
        const day= entry.date.slice(0, 10)
        const bucket = byDate.get(day) ?? { deposits: 0, withdrawals: 0}
        if (entry.amount > 0) bucket.deposits+= entry.amount;
        else bucket.withdrawals += Math.abs(entry.amount);
        byDate.set(day, bucket)
    }

    const dateFormatter = new Intl.DateTimeFormat("sv-SE", { 
                                                day: "numeric", 
                                                month: "numeric", 
                                                year: "2-digit"})

    const dates = [...byDate.keys()].sort();
    return {
        labels: dates.map((d) => dateFormatter.format(new Date(d))),
        deposits: dates.map((d) => byDate.get(d)!.deposits),
        withdrawals: dates.map((d) => byDate.get(d)!.withdrawals)
    }
}

type TransactionChartProps = {
    history: LedgerEntry[];
}

export default function TransactionsChart({ history }: TransactionChartProps) {
    if (history.length === 0)
        return <p className="text-small text-muted">Inga transaktioner att visa</p>
    
    const { labels, deposits, withdrawals } = buildSeries(history);

    const data = {
        labels, datasets: [
            {
                label: "Insättningar",
                data: deposits,
                borderColor: "#2a78d6",
                backgroundColor: "#2a78d6",
                borderWidth: 1,
                pointRadius: 3,
            },
            {
                label: "Uttag",
                data: withdrawals,
                borderColor: "#eb6834",
                backgroundColor: "#eb6834",
                borderWidth: 1,
                pointRadius: 3,
            },
        ],
    };
    const options = {
        maintainAspectRatio: false,
        scales: {
            y: { beginAtZero: true, grid: { color: "#e1e0d9"} },
            x: { grid: { display: false } }
        },
        plugins: {
            legend: { position: "bottom" as const },
        },
    };

    return (
        <div className="relative h-[220px] w-full">
            <Line data={data}
                options={options} />
        </div>
    )
}