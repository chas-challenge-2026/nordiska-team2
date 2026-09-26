using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class FaqService
    {
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

            var entries = await _db.FaqEntries.ToListAsync();

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

        double ScoreEntry(FaqEntry entry, List<string> queryWords)
        {
            var keywordWords = entry.Keywords
                .SelectMany(Normalize)
                .Distinct()
                .ToList();

            if (keywordWords.Count == 0 || queryWords.Count == 0)
                return 0;

            int matched = queryWords.Count(qw => keywordWords.Contains(qw));
            return (double)matched / queryWords.Count;
        }

        List<string> Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var lowered = text.ToLowerInvariant();

            var lettersAndSpacesOnly = new string(lowered
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

            return lettersAndSpacesOnly
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Stem)
                .Where(word => word.Length > 1)
                .ToList();
        }

        static readonly string[] StemSuffixes = { "ing", "ies", "ed", "es", "s" };

        string Stem(string word)
        {
            foreach (var suffix in StemSuffixes.OrderByDescending(s => s.Length))
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