import { Chart as ChartJS, BarElement, CategoryScale, LinearScale, Tooltip, Legend } from "chart.js";
import type { ChartEvent, LegendItem } from "chart.js"
import { useState } from "react";
import { Bar } from "react-chartjs-2";

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend);

type LedgerEntry = {
    date: string;
    description: string;
    amount: number;
};

const WINDOW_SIZE = 5;

function buildSeries(history: LedgerEntry[], page: number, currentBalance: number) {
    const byDate = new Map<string, { deposits: number; withdrawals: number; }>();

    for(const entry of history) {
        const day = entry.date.slice(0, 10)
        const bucket = byDate.get(day) ?? { deposits: 0, withdrawals: 0 }
        if (entry.amount > 0) bucket.deposits += entry.amount;
        else bucket.withdrawals += Math.abs(entry.amount);
        byDate.set(day, bucket)
    }

    const allDates = [...byDate.keys()].sort(); // äldst -> nyast

    // Räknar ut startsaldo per dag genom att gå baklänges från dagens saldo.
    const startBalanceByDate = new Map<string, number>()
    let runningEndBalance = currentBalance;
    for (let i = allDates.length -1; i >=0; i--) {
        const d = allDates[i];
        const { deposits, withdrawals } = byDate.get(d)!;
        const net = deposits - withdrawals;
        const startBalance = runningEndBalance - net;
        startBalanceByDate.set(d, startBalance);
        runningEndBalance = startBalance;
    }

    const end = allDates.length - page * WINDOW_SIZE;
    const start = Math.max(0, end - WINDOW_SIZE);
    const dates = allDates.slice(start, end);

    const dateFormatter = new Intl.DateTimeFormat("sv-SE", { 
        day: "numeric", 
        month: "numeric", 
        year: "2-digit"})

    return {
        labels: dates.map((d) => dateFormatter.format(new Date(d))),
        startBalances: dates.map((d) => startBalanceByDate.get(d)!),
        netAmounts: dates.map((d) => {
            const { deposits, withdrawals } = byDate.get(d)!;
            return Math.abs(deposits - withdrawals);
        }),
        netColors: dates.map((d) => {
            const { deposits, withdrawals } = byDate.get(d)!;
            return deposits - withdrawals >= 0 ? "#1a5276" : "#f5a623";
        }),
        hasOlder: start > 0,
        hasNewer: page > 0,
    };
}

type TransactionChartProps = {
    history: LedgerEntry[];
    currentBalance: number;
}

export default function TransactionsChart({ history, currentBalance }: TransactionChartProps) {
    const [page, setPage] = useState(0);
    const [hidden, setHidden] = useState({ start: false, deposit: false, withdrawal: false })

    if (history.length === 0)
        return <p className="text-small text-muted">Inga transaktioner att visa</p>
    
    const { labels, startBalances, netAmounts, netColors, hasNewer, hasOlder } = 
        buildSeries(history, page, currentBalance);

    const displayedStartBalances = startBalances.map((v) => (hidden.start ? 0 : v ));
    const displayedNetAmounts = netAmounts.map((amt, i) => {
        const isDeposit = netColors[i] ==="#1a5276";
        if (isDeposit && hidden.deposit) return 0;
        if (!isDeposit && hidden.withdrawal) return 0;
        return amt;
    })

    const data = {
        labels, 
        datasets: [
            {
                label: "Startsaldo",
                data: displayedStartBalances,
                backgroundColor: "#C6C6C6",
                stack: "day",
                borderRadius: 4,
                maxBarThickness: 34,

            },
            {
                label: "Insättning/uttag (netto)",
                data: displayedNetAmounts,
                backgroundColor: netColors,
                stack:"day",
                borderRadius: 4,
                maxBarThickness: 34,
            },
        ],
    };
    const options = {
        maintainAspectRatio: false,
        scales: {
            y: { stacked: true, beginAtZero: true, grid: { color: "#e1e0d9"} },
            x: { stacked: true, grid: { display: false } }
        },
        plugins: {
            legend: { position: "bottom" as const,
                labels: {
                    generateLabels: () => [
                    { text: "Startsaldo", fillStyle: "#C6C6C6", strokeStyle: "#C6C6C6" },
                    { text: "Insättning (netto)", fillStyle: "#1a5276", strokeStyle: "#1a5276" },
                    { text: "Uttag (netto)", fillStyle: "#f5a623", strokeStyle: "#f5a623" },
                    ]
                },
                onClick: (_event: ChartEvent, legendItem: LegendItem) => {
                    if (legendItem.text === "Startsaldo") setHidden((h) => ({ ...h, start: !h.start }));
                    else if (legendItem.text === "Insättning (netto)") setHidden((h) => ({ ...h, deposit: !h.deposit }));
                    else if (legendItem.text === "Uttag (netto)") setHidden((h) => ({ ...h, withdrawal: !h.withdrawal }));
                },
                onHover: (event: ChartEvent) => {
                    if (event.native?.target) {
                        (event.native.target as HTMLElement).style.cursor = "pointer";
                    }
                },
                onLeave: (event: ChartEvent) => {
                    if (event.native?.target) {
                        (event.native.target as HTMLElement).style.cursor = "default";
                    }
                },
            },
        },
    };

    return (
        <div className="mt-auto">
            <div className="relative h-[290px] w-full">
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