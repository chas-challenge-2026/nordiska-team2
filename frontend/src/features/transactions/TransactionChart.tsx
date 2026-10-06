import { Chart as ChartJS, BarElement, CategoryScale, LinearScale, Tooltip, Legend } from "chart.js";
import { useState } from "react";
import { Bar } from "react-chartjs-2";

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend);

type LedgerEntry = {
    date: string;
    description: string;
    amount: number;
};

const WINDOW_SIZE = 5;

function buildSeries(history: LedgerEntry[], page: number) {
    const byDate = new Map<string, { deposits: number; withdrawals: number; }>();

    for(const entry of history) {
        const day= entry.date.slice(0, 10)
        const bucket = byDate.get(day) ?? { deposits: 0, withdrawals: 0}
        if (entry.amount > 0) bucket.deposits+= entry.amount;
        else bucket.withdrawals += Math.abs(entry.amount);
        byDate.set(day, bucket)
    }

    const allDates = [...byDate.keys()].sort(); // äldst -> nyast
    const end = allDates.length - page * WINDOW_SIZE;
    const start = Math.max(0, end - WINDOW_SIZE);
    const dates = allDates.slice(start, end);

    const dateFormatter = new Intl.DateTimeFormat("sv-SE", { 
                                                day: "numeric", 
                                                month: "numeric", 
                                                year: "2-digit"})

    return {
        labels: dates.map((d) => dateFormatter.format(new Date(d))),
        deposits: dates.map((d) => byDate.get(d)!.deposits),
        withdrawals: dates.map((d) => byDate.get(d)!.withdrawals),
        hasOlder: start > 0,
        hasNewer: page > 0,
        allDatesCount: allDates.length,
    };
}

type TransactionChartProps = {
    history: LedgerEntry[];
}

export default function TransactionsChart({ history }: TransactionChartProps) {
    const [page, setPage] = useState(0);
    if (history.length === 0)
        return <p className="text-small text-muted">Inga transaktioner att visa</p>
    
    const { labels, deposits, withdrawals, hasNewer, hasOlder } = buildSeries(history, page);

    const data = {
        labels, datasets: [
            {
                label: "Insättningar",
                data: deposits,
                backgroundColor: "#2a78d6",
                borderRadius: 4,
                maxBarThickness: 24,
                categoryPercentage: 0.1,
                barPercentage: 1.0
            },
            {
                label: "Uttag",
                data: withdrawals,
                backgroundColor: "#eb6834",
                borderRadius: 4,
                maxBarThickness: 24,
                categoryPercentage: 0.1,
                barPercentage: 1.0
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
        <div>
            <div className="relative h-[220px] w-full">
                <Bar data={data} options={options} />
            </div>
            <div className="flex justify-between mt-2">
                <button
                    type="button"
                    className="text-small disabled:opacity-40 disabled:cursor-default"
                    disabled={!hasOlder}
                    onClick={() => setPage((p) => p + 1)}
                >
                    ← Föregående
                </button>
                <button
                    type="button"
                    className="text-small disabled:opacity-40 disabled:cursor-default"
                    disabled={!hasNewer}
                    onClick={() => setPage((p) => p - 1)}
                >
                    Nästa →
                </button>
            </div>
        </div>
    );
}