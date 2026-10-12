import { Chart as ChartJS, BarElement, CategoryScale, LinearScale, Tooltip, Legend } from "chart.js";
import { useState } from "react";
import { Bar } from "react-chartjs-2";
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../language/useFormatters";

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend);

type LedgerEntry = {
    date: string;
    description: string;
    amount: number;
};

const WINDOW_SIZE = 5;

function buildSeries(history: LedgerEntry[], page: number, currentBalance: number, locale: string) {
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

    const dateFormatter = new Intl.DateTimeFormat(locale, { 
        day: "numeric", 
        month: "numeric", 
        year: "2-digit"})

    return {
        labels: dates.map((d) => dateFormatter.format(new Date(d))),
        startBalances: dates.map((d) => startBalanceByDate.get(d)!),
        deposits: dates.map((d) => byDate.get(d)!.deposits),
        withdrawals: dates.map((d) => byDate.get(d)!.withdrawals), // negativ -> ritas nedåt
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
    const { t } = useTranslation();
    const { locale } = useFormatters();
    if (history.length === 0)
        return <p className="text-small text-muted">{t("accountsPage.chart.noData")}</p>
    
    const { labels, startBalances, deposits, withdrawals, hasNewer, hasOlder } = 
        buildSeries(history, page, currentBalance, locale);

    const data = {
        labels, 
        datasets: [
            {
                label: t("accountsPage.chart.startBalance"),
                data: startBalances,
                backgroundColor: "#9aa0a6",
                stack: "day",
                borderRadius: 4,
                maxBarThickness: 34,

            },
            {
                label: t("accountsPage.chart.deposits"),
                data: deposits,
                backgroundColor: "#1a5276",
                stack:"day",
                borderRadius: 4,
                maxBarThickness: 34,
            },
            {
                label: t("accountsPage.chart.withdrawals"),
                data: withdrawals,
                backgroundColor: "#f5a623",
                stack: "day",
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
                    {t("accountsPage.chart.previous")}
                </button>
                <button
                    type="button"
                    className="text-small disabled:opacity-40 disabled:cursor-default"
                    disabled={!hasNewer}
                    onClick={() => setPage((p) => p - 1)}
                >
                    {t("accountsPage.chart.next")}
                </button>
            </div>
        </div>
    );
}