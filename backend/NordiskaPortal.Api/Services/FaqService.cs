using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class FaqService
    {
        // Below this score we don't trust the match enough to show it.
        private const double ConfidenceThreshold = 0.34;

        private const string NoAnswerMessage =
            "Vi kunde tyvärr inte hitta något svar på din fråga. " +
            "Kontakta gärna kundservice på 08‑123 456 78, vardagar 9–17, så hjälper vi dig vidare.";

        private readonly BankContext _db;

        public FaqService(BankContext db)
        {
            _db = db;
        }

        public async Task<FaqSearchResponse> SearchAsync(FaqSearchRequest request)
        {
            var queryWords = Normalize(request.Query);
            if (queryWords.Count == 0)
                return NoAnswer();

            // Ordered by Id so a tie always resolves the same way.
            var entries = await _db.FaqEntries.OrderBy(e => e.Id).ToListAsync();

            FaqEntry? bestEntry = null;
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
        //  - share of the query's words that hit this entry (a short,
        //    precise query should score high), and
        //  - share of the entry's keywords that appear in the query (a
        //    long query that covers the entry should score high).
        double ScoreEntry(FaqEntry entry, List<string> queryWords)
        {
            var keywordWords = entry.Keywords
                .SelectMany(Normalize)
                .Distinct()
                .ToList();

            if (keywordWords.Count == 0 || queryWords.Count == 0)
                return 0;

            int matched = queryWords.Count(qw => keywordWords.Contains(qw));

            double byQuery = (double)matched / queryWords.Count;
            double byKeywords = (double)matched / keywordWords.Count;

            return Math.Max(byQuery, byKeywords);
        }

        // Lowercases, strips punctuation, splits on any whitespace, drops
        // filler words, then stems what is left. The same pipeline runs on
        // the query and on every entry's keywords, so both sides end up in
        // the same form.
        List<string> Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var lowered = text.ToLowerInvariant();

            var lettersAndSpacesOnly = new string(lowered
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

            return lettersAndSpacesOnly
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !StopWords.Contains(word))
                .Select(Stem)
                .Where(word => word.Length > 1)
                .ToList();
        }

        // Common Swedish filler words that say nothing about the topic.
        static readonly HashSet<string> StopWords = new()
        {
            "hur", "jag", "ett", "en", "och", "är", "på", "min", "mitt", "mina",
            "mig", "vad", "vill", "kan", "får", "det", "den", "de", "som", "för",
            "av", "till", "med", "om", "att", "var", "gör", "mycket", "eller",
            "inte", "vi", "du", "dig", "har"
        };

        // Common Swedish plural, definite and verb endings, longest first
        // so "räntorna" loses "orna" rather than just "a".
        static readonly string[] StemSuffixes = new[]
        {
            "arna", "erna", "orna", "ade",
            "ar", "er", "or", "en", "an", "et", "na", "as",
            "a", "s"
        }
        .OrderByDescending(s => s.Length)
        .ToArray();

        // Strips one ending, but only if enough of the word remains, so
        // short words are left alone.
        string Stem(string word)
        {
            foreach (var suffix in StemSuffixes)
            {
                if (word.Length > suffix.Length + 2 && word.EndsWith(suffix, StringComparison.Ordinal))
                    return word[..^suffix.Length];
            }
            return word;
        }

        FaqSearchResponse NoAnswer()
        {
            return new FaqSearchResponse(
                Matched: false,
                Question: null,
                Answer: NoAnswerMessage,
                Category: null
            );
        }
    }
}