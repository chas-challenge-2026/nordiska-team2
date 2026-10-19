import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";

export function useAvailableTaxReportYears(accountId: number) {
    return useQuery({
        queryKey: ["tax-report-years", accountId],
        queryFn: async () => {
            const response = await apiClient.get<number[]>(`/tax-reports/${accountId}/available-years`);
            return response.data
        },
        enabled: Number.isFinite(accountId),
    })
}

// GET {accountId}/{year} - PDF
export async function downloadTaxReport(accountId: number, year: number) {
    const response = await apiClient.get(`/tax-reports/${accountId}/${year}`, {
        responseType: "blob", // Skickar som PDF och inte JSON
    })

    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement("a")
    link.href = url
    link.download = `skatterapport-${accountId}-${year}.pdf`
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
}