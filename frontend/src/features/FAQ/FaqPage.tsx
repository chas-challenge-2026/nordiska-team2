import { useState } from "react";
import { Search, CreditCard, ArrowLeftRight, TrendingUp, FileText } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { apiClient } from "../../client";
import type { FaqSearchResult } from "../../types/faq";
import { useFaqCategories, useFaqCategoryEntries, useFaqPopular } from "../../hooks/useFaq";
import { FaqQuestionCard } from "./FaqQuestionCard";
import { FaqCategoryModal } from "./FaqCategoryModal";

// Behöver dubbelkolla Ivans Seed-data.
const categoryIcons: Record<string, LucideIcon> = {
  account: CreditCard,
  transactions: ArrowLeftRight,
  interest: TrendingUp,
  tax: FileText,
};

export function FaqPage() {
  const [query, setQuery] = useState("");
  const [result, setResult] = useState<FaqSearchResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);

  const categories = useFaqCategories();
  const popular = useFaqPopular();
  const categoryEntries = useFaqCategoryEntries(selectedCategoryId);

  const selectedCategory = categories.data?.find((c) => c.id === selectedCategoryId);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!query.trim()) return;

    setIsLoading(true);
    setError(null);

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000);

    try {
      const response = await apiClient.post<FaqSearchResult>(
        "/faq/search",
        { query: query.trim() },
        { signal: controller.signal }
      );

      clearTimeout(timeoutId);
      setResult(response.data);
    } catch {
      clearTimeout(timeoutId);
      setError("Sökningen lyckades inte, försök igen!");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div>
      <form onSubmit={handleSubmit} className="relative mb-2">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" size={20} />
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Hur tar jag ut pengar?"
          className="w-full pl-12 pr-4 py-4 rounded-xl border border-border bg-white text-small focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </form>

      <p className="text-xsmall text-gray-500 mb-8 ml-2">
        Populära sökord: uttagstid · räntebesked · skatterapport 2025
      </p>

      {isLoading && <p className="text-sm text-gray-500 mb-4">Söker...</p>}
      {error && <p className="text-sm text-red-600 mb-4">{error}</p>}

      {categories.isLoading && <p className="text-sm text-gray-500 mb-4">Laddar kategorier...</p>}
      {categories.isError && (
        <p className="text-sm text-red-600 mb-4">Kunde inte hämta kategorier.</p>
      )}

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {categories.data?.map((cat) => {
          const Icon = categoryIcons[cat.id] ?? FileText;
          return (
            <button
              type="button"
              key={cat.id}
              onClick={() => setSelectedCategoryId(cat.id)}
              className="text-left rounded-card border border-border bg-white p-5 hover:shadow-md transition-shadow cursor-pointer"
            >
              <Icon size={24} />
              <p className="font-semibold mt-3 text-medium">{cat.label}</p>
              <p className="text-small text-gray-500">{cat.questionCount} frågor</p>
            </button>
          );
        })}
      </div>

      {result && !isLoading && (
        <div className="mb-8">
          <h2 className="text-lg font-semibold mb-4">Sökresultat</h2>
          <div className="rounded-xl border border-gray-200 bg-white p-5">
            {result.matched && <p className="font-semibold mb-2">{result.question}</p>}
            <p className="text-gray-600">{result.answer}</p>
          </div>
        </div>
      )}

      {!result && popular.data && popular.data.length > 0 && (
        <>
          <h2 className="text-medium font-semibold ml-1 mb-4">Vanliga frågor just nu</h2>
          {popular.data.map((entry, index) => (
            <FaqQuestionCard key={entry.id} entry={entry} defaultOpen={index === 0} />
          ))}
        </>
      )}

      <FaqCategoryModal
        isOpen={selectedCategoryId !== null}
        onClose={() => setSelectedCategoryId(null)}
        title={selectedCategory?.label ?? ""}
      >
        {categoryEntries.isLoading && <p className="text-gray-500">Laddar frågor...</p>}
        {categoryEntries.isError && (
          <p className="text-red-600">Kunde inte hämta frågorna.</p>
        )}
        {categoryEntries.data?.length === 0 && (
          <p className="text-gray-500">Inga frågor i den här kategorin än.</p>
        )}
        {categoryEntries.data?.map((entry) => (
          <FaqQuestionCard key={entry.id} entry={entry} />
        ))}
      </FaqCategoryModal>
    </div>
  );
}