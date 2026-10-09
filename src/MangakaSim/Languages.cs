namespace MangakaSim;

/// <summary>
/// The languages the game's text can be shown in (i18n, 2026-10-10). The simulation stays in English; the presentation
/// translates each English string through gettext catalogues (godot/Localization/*.po), so anything not yet translated
/// simply shows in English. Codes are Godot locale codes; a new language is one entry here plus one .po file.
/// </summary>
public static class Languages
{
    public sealed record Language(string Code, string Name);

    /// <summary>English first: it is the source text and the fallback.</summary>
    public static readonly Language[] Supported = [new("en", "English"), new("ja", "日本語"), new("en_SG", "Singlish"),
        new("es", "Español"), new("fr", "Français"), new("de", "Deutsch"), new("it", "Italiano"), new("pt_PT", "Português (Portugal)")];

    public static bool Known(string? code) => code is not null && Supported.Any(l => l.Code == code);

    /// <summary>Automatic picks the computer's language when the game has it, otherwise English. Singlish is only chosen by hand,
    /// since a Singapore locale usually means standard English. European Portuguese only matches Portugal, not Brazil.</summary>
    public static string Automatic(string systemLocale) =>
        Supported.FirstOrDefault(l => l.Code != "en_SG" && l.Code != "en" && systemLocale.StartsWith(l.Code, StringComparison.OrdinalIgnoreCase))?.Code ?? "en";

    /// <summary>The language in use: the chosen one, or Automatic when none (or an unknown one) is set.</summary>
    public static string Resolve(string? chosen, string systemLocale) => Known(chosen) ? chosen! : Automatic(systemLocale);
}
