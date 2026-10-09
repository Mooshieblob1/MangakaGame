using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

// i18n (2026-10-10): the text language is a per-computer display setting (Automatic follows Windows). English strings
// are the gettext msgids in godot/Localization/*.po, so Godot's controls translate themselves and anything not yet
// translated stays in English. Code that sets text and then measures it (the typed dialogue line) calls Tr itself.
public partial class DebugMain
{
    internal string LanguageCode { get; private set; } = "en";

    private void ApplyLanguage()
    {
        LanguageCode = Languages.Resolve(_display.Language, OS.GetLocale());
        TranslationServer.SetLocale(LanguageCode);
        // Arabic text reads right to left inside each label, but the layout itself is not mirrored yet: Godot would flip the
        // whole interface for an RTL locale, and the panels and dialogue box are placed for left to right.
        GetTree().Root.SetLayoutDirection(Window.LayoutDirection.Ltr);
    }

    private void SetLanguage(string? code)
    {
        _display.Language = code; SaveDisplaySettings(); ApplyLanguage();
        if (_managementReady) { BuildManagementPage(); RefreshManagement(); }
    }

    /// <summary>The Language picker. Languages are listed in their own names, so the list itself is never translated.</summary>
    private void LanguageChoice(Control parent)
    {
        Words(parent, "Language", 14);
        var choice = new OptionButton { Name = "Language", AutoTranslateMode = AutoTranslateModeEnum.Disabled }; parent.AddChild(choice);
        choice.AddItem(Tr("Automatic, follows Windows"));
        foreach (var language in Languages.Supported) choice.AddItem(language.Name);
        choice.Select(Languages.Known(_display.Language) ? Languages.Supported.ToList().FindIndex(l => l.Code == _display.Language) + 1 : 0);
        choice.ItemSelected += i => SetLanguage(i == 0 ? null : Languages.Supported[i - 1].Code);
    }
}
