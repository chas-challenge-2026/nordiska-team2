using System.Collections;
using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NordiskaPortal.Api;

namespace NordiskaPortal.Api.Tests
{
    public class LocalizationTests
    {
        // Built the same way ASP.NET Core builds it, without starting the app.
        private static IStringLocalizer<SharedResource> CreateLocalizer()
        {
            var options = Options.Create(new LocalizationOptions { ResourcesPath = "Resources" });
            var factory = new ResourceManagerStringLocalizerFactory(options, NullLoggerFactory.Instance);
            return new StringLocalizer<SharedResource>(factory);
        }

        [Theory]
        [InlineData("sv", "Otillräckligt saldo.")]
        [InlineData("en", "Insufficient funds.")]
        [InlineData("en-GB", "Insufficient funds.")]   // falls back to its parent language "en"
        [InlineData("fr", "Otillräckligt saldo.")]     // unknown language: falls back to Swedish
        public void Localizer_ReturnsTheRequestedLanguage_WithFallback(string culture, string expected)
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                Assert.Equal(expected, CreateLocalizer()["insufficient_funds"].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        // Every language must have exactly the same keys as Swedish. Finds the
        // languages by itself (one folder per language in the build output), so
        // a new SharedResource.de.resx is checked automatically.
        [Fact]
        public void EveryLanguage_HasExactlyTheSameKeys_AsSwedish()
        {
            var resources = new ResourceManager("NordiskaPortal.Api.Resources.SharedResource", typeof(SharedResource).Assembly);
            var swedish = Keys(resources, CultureInfo.InvariantCulture);
            Assert.NotEmpty(swedish);

            var languages = Directory.GetDirectories(AppContext.BaseDirectory)
                .Where(dir => File.Exists(Path.Combine(dir, "NordiskaPortal.Api.resources.dll")))
                .Select(dir => Path.GetFileName(dir)!)
                .ToList();
            Assert.Contains("en", languages);

            foreach (var language in languages)
            {
                var keys = Keys(resources, new CultureInfo(language));
                Assert.True(swedish.SetEquals(keys),
                    $"'{language}' differs from Swedish. Missing: [{string.Join(", ", swedish.Except(keys))}] " +
                    $"Extra: [{string.Join(", ", keys.Except(swedish))}]");
            }
        }

        private static HashSet<string> Keys(ResourceManager resources, CultureInfo culture) =>
            resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
                .Cast<DictionaryEntry>()
                .Select(entry => (string)entry.Key)
                .ToHashSet();
    }
}