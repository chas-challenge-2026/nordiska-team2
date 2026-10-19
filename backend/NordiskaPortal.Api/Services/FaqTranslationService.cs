using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    // One FAQ entry with its text in the language of the current request.
    public sealed record LocalizedFaqEntry(
        int Id,
        string Question,
        string Answer,
        string Category,      // label in the request's language
        string BaseCategory,  // stored default-language category; ids come from this
        string[] Keywords,
        string Language,      // the language the text above is really in
        bool IsPopular);

    public class FaqTranslationService
    {
        // The language FaqEntry itself is written in. Mirrors Localization:DefaultLanguage.
        public const string BaseLanguage = "sv";

        private readonly BankContext _db;

        public FaqTranslationService(BankContext db)
        {
            _db = db;
        }

        // UseRequestLocalization sets CurrentUICulture from Accept-Language.
        // CurrentCulture stays fixed on purpose, so it must not be used here.
        public static string CurrentLanguage =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

        // Every entry, in the current language. An entry with no translation
        // in that language falls back to the default-language text.
        public async Task<List<LocalizedFaqEntry>> GetEntriesAsync()
        {
            var language = CurrentLanguage;
            var entries = await _db.FaqEntries.OrderBy(e => e.Id).ToListAsync();

            var translations = new Dictionary<int, FaqEntryTranslation>();
            if (language != BaseLanguage)
            {
                translations = await _db.FaqEntryTranslations
                    .Where(t => t.Language == language)
                    .ToDictionaryAsync(t => t.FaqEntryId);
            }

            return entries
                .Select(e => translations.TryGetValue(e.Id, out var t)
                    ? new LocalizedFaqEntry(
                        e.Id, t.Question, t.Answer,
                        string.IsNullOrWhiteSpace(t.Category) ? e.Category : t.Category,
                        e.Category, t.Keywords, language, e.IsPopular)
                    : new LocalizedFaqEntry(
                        e.Id, e.Question, e.Answer, e.Category,
                        e.Category, e.Keywords, BaseLanguage, e.IsPopular))
                .ToList();
        }
    }
}