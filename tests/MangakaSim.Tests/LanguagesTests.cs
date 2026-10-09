using Xunit;
namespace MangakaSim.Tests;

// i18n (2026-10-10): English, Japanese and Singlish text, chosen per computer, Automatic by default.
public class LanguagesTests
{
    static string TempFile() => Path.Combine(Path.GetTempPath(), "language-" + Guid.NewGuid().ToString("N"), "display-settings.json");

    [Theory]
    [InlineData("ja", "ja")]
    [InlineData("ja_JP", "ja")]
    [InlineData("en_US", "en")]
    [InlineData("en_SG", "en")] // Singlish only by hand
    [InlineData("de_DE", "de")]
    [InlineData("es_MX", "es")]
    [InlineData("pt_PT", "pt_PT")]
    [InlineData("pt_BR", "en")] // Brazilian Portuguese is not European Portuguese
    [InlineData("nl_NL", "en")] // not translated yet
    [InlineData("", "en")]
    public void Automatic_follows_the_computer_when_the_game_has_that_language(string system, string expected) =>
        Assert.Equal(expected, Languages.Automatic(system));

    [Fact] public void A_chosen_language_wins_over_the_computer()
    {
        Assert.Equal("en_SG", Languages.Resolve("en_SG", "ja_JP"));
        Assert.Equal("en", Languages.Resolve("en", "ja_JP"));
        Assert.Equal("ja", Languages.Resolve(null, "ja_JP"));
        Assert.Equal("ja", Languages.Resolve("klingon", "ja_JP"));
    }

    [Fact] public void English_comes_first_as_the_source_and_fallback()
    {
        Assert.Equal("en", Languages.Supported[0].Code);
        Assert.Equal(Languages.Supported.Length, Languages.Supported.Select(l => l.Code).Distinct().Count());
    }

    [Fact] public void The_language_choice_is_saved_and_unknown_codes_go_back_to_automatic()
    {
        var path = TempFile();
        new DisplaySettings { Language = "ja" }.Save(path);
        Assert.Equal("ja", DisplaySettings.Load(path).Language);
        new DisplaySettings { Language = "tlh" }.Save(path);
        Assert.Null(DisplaySettings.Load(path).Language);
        Assert.Null(new DisplaySettings().Language);
    }
}
