import { useState } from "react";
import { Search, CreditCard, ArrowLeftRight, TrendingUp, FileText } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { apiClient } from "../../client";
import type { FaqSearchResult } from "../../types/faq";
import { useFaqCategories, useFaqCategoryEntries, useFaqPopular } from "../../hooks/useFaq";
import { FaqQuestionCard } from "./FaqQuestionCard";
import { FaqCategoryModal } from "./FaqCategoryModal";
import { useTranslation } from "react-i18next";
import PageHeader from "../../components/ui/Pageheader";

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
  const { t } = useTranslation();

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
      // Vi sparar nyckeln och översätter först när felet visas
      setError("faq.searchFailed");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div>
       <PageHeader
          title={t("faq.title")}
          subtitle={t("faq.subtitle")} />
      <form onSubmit={handleSubmit} className="relative mb-2">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" size={20} />
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={t("faq.searchPlaceholder")}
          className="w-full pl-12 pr-4 py-4 rounded-xl border border-gray-200 bg-white text-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </form>

      <p className="text-sm text-gray-500 mb-8">
        {t("faq.popularSearches")}
      </p>

      {isLoading && <p className="text-sm text-gray-500 mb-4">{t("faq.searching")}</p>}
      {error && <p className="text-sm text-cancel mb-4">{t(error)}</p>}

      {categories.isLoading && <p className="text-sm text-gray-500 mb-4">{t("faq.loadingCategories")}</p>}
      {categories.isError && (
        <p className="text-sm text-cancel mb-4">{t("faq.categoriesError")}</p>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {categories.data?.map((cat) => {
          const Icon = categoryIcons[cat.id] ?? FileText;
          return (
            <button
              type="button"
              key={cat.id}
              onClick={() => setSelectedCategoryId(cat.id)}
              className="text-left md:rounded-card border border-border bg-white p-5 hover:shadow-md transition-shadow cursor-pointer"
            >
              <Icon size={24} />
              <p className="font-semibold mt-3">{cat.label}</p>
              <p className="text-sm text-gray-500">{t("faq.questionCount", { count: cat.questionCount })}</p>
            </button>
          );
        })}
      </div>

      {result && !isLoading && (
        <div className="mb-8">
          <h2 className="text-lg font-semibold mb-4">{t("faq.searchResults")}</h2>
          <div className="rounded-xl border border-gray-200 bg-white p-5">
            {result.matched && <p className="font-semibold mb-2">{result.question}</p>}
            <p className="text-gray-600">{result.answer}</p>
          </div>
        </div>
      )}

      {!result && popular.data && popular.data.length > 0 && (
        <>
          <h2 className="text-lg font-semibold mb-4">{t("faq.popularNow")}</h2>
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
        {categoryEntries.isLoading && <p className="text-gray-500">{t("faq.loadingQuestions")}</p>}
        {categoryEntries.isError && (
          <p className="text-cancel">{t("faq.questionsError")}</p>
        )}
        {categoryEntries.data?.length === 0 && (
          <p className="text-gray-500">{t("faq.noQuestions")}</p>
        )}
        {categoryEntries.data?.map((entry) => (
          <FaqQuestionCard key={entry.id} entry={entry} />
        ))}
      </FaqCategoryModal>
    </div>
  );
}