namespace NordiskaPortal.Api.Localization
{
    // The single list of languages the backend supports. Everything that
    // needs to know (request handling, later the profile setting and the
    // assistant) reads it from here, so adding a language is a config change.
    public class LanguageSettings
    {
        public const string SectionName = "Localization";

        public string DefaultLanguage { get; set; } = "sv";
        public string[] SupportedLanguages { get; set; } = { "sv" };
    }
}