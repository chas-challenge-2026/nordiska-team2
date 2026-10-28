import { apiClient } from "../client";

export async function downloadBankStatement(accountId: number, year: number, month: number) {
    const response = await apiClient.get(`/bank-statements/${accountId}/${year}/${month}`, {
        responseType: "blob", // Skickar som PDF och inte JSON
    })

    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement("a")
    link.href = url
    link.download = `kontoutdrag-${year}-${String(month).padStart(2, "0")}.pdf`
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
}