import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { FaqCategory, FaqEntry } from "../types/faq";

export function useFaqCategories() {
  return useQuery({
    queryKey: ["faq", "categories"],
    queryFn: async () => {
      const response = await apiClient.get<FaqCategory[]>("/faq/categories");
      return response.data;
    },
  });
}

export function useFaqCategoryEntries(categoryId: string | null) {
  return useQuery({
    queryKey: ["faq", "categories", categoryId, "entries"],
    queryFn: async () => {
      const response = await apiClient.get<FaqEntry[]>(`/faq/categories/${categoryId}/entries`);
      return response.data;
    },
    enabled: categoryId !== null,
  });
}

export function useFaqPopular() {
  return useQuery({
    queryKey: ["faq", "popular"],
    queryFn: async () => {
      const response = await apiClient.get<FaqEntry[]>("/faq/popular");
      return response.data;
    },
  });
}