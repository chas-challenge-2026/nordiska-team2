using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class FaqCategoryService
    {
        private readonly BankContext _db;

        public FaqCategoryService(BankContext db)
        {
            _db = db;
        }

        // One card per distinct category, with how many entries it holds.
        public async Task<List<FaqCategoryResponse>> GetCategoriesAsync()
        {
            var groups = await _db.FaqEntries
                .Where(e => e.Category != "")
                .GroupBy(e => e.Category)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToListAsync();

            return groups
                .Select(g => new FaqCategoryResponse(ToSlug(g.Label), g.Label, g.Count))
                .OrderBy(c => c.Label)
                .ToList();
        }

        // Returns null when no category has this id, so the controller can
        // answer 404 instead of an empty list.
        public async Task<List<FaqEntryResponse>?> GetEntriesByCategoryAsync(string categoryId)
        {
            var wanted = categoryId.Trim().ToLowerInvariant();

            var entries = await _db.FaqEntries
                .Where(e => e.Category != "")
                .OrderBy(e => e.Id)
                .ToListAsync();

            var matching = entries
                .Where(e => ToSlug(e.Category) == wanted)
                .ToList();

            if (matching.Count == 0)
                return null;

            return matching.Select(ToResponse).ToList();
        }

        // Entries flagged as popular, for the start page.
        public async Task<List<FaqEntryResponse>> GetPopularAsync()
        {
            var popular = await _db.FaqEntries
                .Where(e => e.IsPopular)
                .OrderBy(e => e.Id)
                .ToListAsync();

            return popular.Select(ToResponse).ToList();
        }

        static FaqEntryResponse ToResponse(FaqEntry entry)
        {
            return new FaqEntryResponse(entry.Id, entry.Question, entry.Answer, entry.Category);
        }

        // Lowercases, strips accents, drops symbols like '&', and joins the
        // remaining words with single hyphens so the result is URL-safe.
        // "Konto & Inlogg" becomes "konto-inlogg".
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