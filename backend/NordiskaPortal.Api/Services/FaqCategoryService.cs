using System.Globalization;
using System.Text;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services
{
    public class FaqCategoryService
    {
        private readonly FaqTranslationService _translations;

        public FaqCategoryService(FaqTranslationService translations)
        {
            _translations = translations;
        }

        // One card per category. The id comes from the default-language
        // category, so it is the same in every language. Only the label is
        // translated. Ordered by id so the cards never reshuffle on a
        // language switch.
        public async Task<List<FaqCategoryResponse>> GetCategoriesAsync()
        {
            var entries = await _translations.GetEntriesAsync();

            return entries
                .Where(e => e.BaseCategory != "")
                .GroupBy(e => ToSlug(e.BaseCategory))
                .Select(g => new FaqCategoryResponse(g.Key, g.First().Category, g.Count()))
                .OrderBy(c => c.Id, StringComparer.Ordinal)
                .ToList();
        }

        // Returns null when no category has this id, so the controller can
        // answer 404 instead of an empty list.
        public async Task<List<FaqEntryResponse>?> GetEntriesByCategoryAsync(string categoryId)
        {
            var wanted = categoryId.Trim().ToLowerInvariant();

            var entries = await _translations.GetEntriesAsync();

            var matching = entries
                .Where(e => e.BaseCategory != "" && ToSlug(e.BaseCategory) == wanted)
                .ToList();

            if (matching.Count == 0)
                return null;

            return matching.Select(ToResponse).ToList();
        }

        // Entries flagged as popular, for the start page.
        public async Task<List<FaqEntryResponse>> GetPopularAsync()
        {
            var entries = await _translations.GetEntriesAsync();

            return entries.Where(e => e.IsPopular).Select(ToResponse).ToList();
        }

        static FaqEntryResponse ToResponse(LocalizedFaqEntry entry)
        {
            return new FaqEntryResponse(entry.Id, entry.Question, entry.Answer, entry.Category);
        }

        // Lowercases, strips accents, drops symbols like '&', and joins the
        // remaining words with single hyphens so the result is URL-safe.
        static string ToSlug(string text)
        {
            var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var slug = new StringBuilder();

            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(c))
                {
                    slug.Append(c);
                }
                else if (char.IsWhiteSpace(c) || c == '-')
                {
                    if (slug.Length > 0 && slug[^1] != '-')
                        slug.Append('-');
                }
            }

            return slug.ToString().TrimEnd('-');
        }
    }
}