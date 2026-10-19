using Microsoft.Extensions.Localization;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Errors;

namespace NordiskaPortal.Api.Services
{
    public class FaqService
    {
        // Below this score we don't trust the match enough to show it.
        private const double ConfidenceThreshold = 0.34;

        private readonly FaqTranslationService _translations;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public FaqService(FaqTranslationService translations, IStringLocalizer<SharedResource> localizer)
        {
            _translations = translations;
            _localizer = localizer;
        }

        public async Task<FaqSearchResponse> SearchAsync(FaqSearchRequest request)
        {
            // The query is written in the user's language.
            var queryWords = Normalize(request.Query, FaqTranslationService.CurrentLanguage);
            if (queryWords.Count == 0)
                return NoAnswer();

            var entries = await _translations.GetEntriesAsync();

            LocalizedFaqEntry? bestEntry = null;
            double bestScore = 0;

            foreach (var entry in entries)
            {
                double score = ScoreEntry(entry, queryWords);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestEntry = entry;
                }
            }

            if (bestEntry == null || bestScore < ConfidenceThreshold)
                return NoAnswer();

            return new FaqSearchResponse(
                Matched: true,
                Question: bestEntry.Question,
                Answer: bestEntry.Answer,
                Category: bestEntry.Category
            );
        }

        // Two views of the same overlap, and the better one wins:
        //  - share of the query's words that hit this entry, and
        //  - share of the entry's keywords that appear in the query.
        // Keywords are normalized in the language they are written in.
        static double ScoreEntry(LocalizedFaqEntry entry, List<string> queryWords)
        {
            var keywordWords = entry.Keywords
                .SelectMany(k => Normalize(k, entry.Language))
                .Distinct()
                .ToList();

            if (keywordWords.Count == 0 || queryWords.Count == 0)
                return 0;

            int matched = queryWords.Count(qw => keywordWords.Contains(qw));

            double byQuery = (double)matched / queryWords.Count;
            double byKeywords = (double)matched / keywordWords.Count;

            return Math.Max(byQuery, byKeywords);
        }

        // Lowercase, strip punctuation, split on whitespace, drop filler words,
        // then stem with the rules of the given language.
        static List<string> Normalize(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var profile = Profiles.TryGetValue(language, out var found)
                ? found
                : Profiles[FaqTranslationService.BaseLanguage];

            var lowered = text.ToLowerInvariant();

            var lettersAndSpacesOnly = new string(lowered
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

            return lettersAndSpacesOnly
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !profile.StopWords.Contains(word))
                .Select(profile.Stem)
                .Where(word => word.Length > 1)
                .ToList();
        }

        // ---- Swedish ----

        static readonly HashSet<string> SwedishStopWords = new()
        {
            "hur", "jag", "ett", "en", "och", "är", "på", "min", "mitt", "mina",
            "mig", "vad", "vill", "kan", "får", "det", "den", "de", "som", "för",
            "av", "till", "med", "om", "att", "var", "gör", "mycket", "eller",
            "inte", "vi", "du", "dig", "har"
        };

        // Longest first, so "räntorna" loses "orna" rather than just "a".
        static readonly string[] SwedishSuffixes = new[]
        {
            "arna", "erna", "orna", "ade",
            "ar", "er", "or", "en", "an", "et", "na", "as",
            "a", "s"
        }
        .OrderByDescending(s => s.Length)
        .ToArray();

        static string StemSwedish(string word)
        {
            foreach (var suffix in SwedishSuffixes)
            {
                if (word.Length > suffix.Length + 2 && word.EndsWith(suffix, StringComparison.Ordinal))
                    return word[..^suffix.Length];
            }
            return word;
        }

        // ---- English ----

        // "when" is deliberately not here: it is meaningful in a FAQ question.
        static readonly HashSet<string> EnglishStopWords = new()
        {
            "how", "do", "does", "did", "i", "a", "an", "the", "is", "are", "was",
            "my", "me", "what", "can", "could", "to", "of", "on", "in", "and", "or",
            "it", "for", "you", "your", "we", "with", "be", "at", "by", "from",
            "this", "that", "much", "many", "there", "will", "would", "should",
            "get", "make", "where", "have", "has"
        };

        // Plural first, then verb ending, so "savings" and "saving" meet.
        static string StemEnglish(string word)
        {
            if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal))
                word = word[..^3] + "y";
            else if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal))
                word = word[..^1];

            if (word.Length > 5 && word.EndsWith("ing", StringComparison.Ordinal))
                word = word[..^3];
            else if (word.Length > 4 && word.EndsWith("ed", StringComparison.Ordinal))
                word = word[..^2];

            return word;
        }

        // ---- Per-language rules (declared last: static fields initialize in order) ----

        sealed record LanguageProfile(HashSet<string> StopWords, Func<string, string> Stem);

        static readonly Dictionary<string, LanguageProfile> Profiles = new()
        {
            ["sv"] = new(SwedishStopWords, StemSwedish),
            ["en"] = new(EnglishStopWords, StemEnglish)
        };

        FaqSearchResponse NoAnswer()
        {
            return new FaqSearchResponse(
                Matched: false,
                Question: null,
                Answer: _localizer["faq_no_answer"].Value,
                Category: null
            );
        }
    }
}