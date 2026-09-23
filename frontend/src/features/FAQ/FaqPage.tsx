import { useState } from "react";
import { Search, CreditCard, ArrowLeftRight, TrendingUp, FileText } from "lucide-react";
import { mockFaqEntries, mockCategories } from "./mockData";
import type { FaqEntry } from "../../types/faq";
import type { LucideIcon } from "lucide-react";
import { FaqQuestionCard } from "./FaqQuestionCard.tsx";



const categoryIcons: Record<string, LucideIcon> = {
  "credit-card": CreditCard,
  "arrows": ArrowLeftRight,
  "trending-up": TrendingUp,
  "file": FileText,
};

export function FaqPage() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<FaqEntry[]>([]);
  const [hasSearched, setHasSearched] = useState(false);
  

  // TODO(API): byt ut mot fetch("/api/faq/search?q=...")
  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!query.trim()) return;

    const normalized = query.trim().toLowerCase();
    const matches = mockFaqEntries.filter(
      (entry) =>
        entry.keywords.some((k) => k.toLowerCase().includes(normalized)) ||
        entry.question.toLowerCase().includes(normalized)
    );

    setResults(matches);
    setHasSearched(true);
  }

  return (
    <div>
      <form onSubmit={handleSubmit} className="relative mb-2">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" size={20} />
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Hur tar jag ut pengar?"
          className="w-full pl-12 pr-4 py-4 rounded-xl border border-gray-200 bg-white text-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </form>

      <p className="text-sm text-gray-500 mb-8">
        Populära sökord: uttagstid · räntebesked · skatterapport 2025
      </p>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {mockCategories.map((cat) => {
          const Icon = categoryIcons[cat.icon];
          return (
            <div
              key={cat.id}
              className="rounded-xl border border-gray-200 bg-white p-5 hover:shadow-md transition-shadow cursor-pointer"
            >
              {Icon && <Icon size={24} />}
              <p className="font-semibold mt-3">{cat.label}</p>
              <p className="text-sm text-gray-500">{cat.questionCount} frågor</p>
            </div>
          );
        })}
      </div>

      <h2 className="text-lg font-semibold mb-4">Vanliga frågor just nu</h2>
      {mockFaqEntries.slice(0, 3).map((entry, index) => (
        <FaqQuestionCard
          key={entry.id}
          entry={entry}
          defaultOpen={index === 0}
        />
      ))}

        
      
    </div>
  );
}