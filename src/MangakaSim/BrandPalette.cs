namespace MangakaSim;

/// <summary>The logo palette for the in-game UI (spec 2026-09-29): hex strings without '#', engine-free so contrast can be unit tested.</summary>
public sealed record BrandTheme(string Wash, string Card, string Hover, string Pressed, string Outline, string Text, string Muted,
    string Accent, string Progress, string Gain, string Loss, string QuietFace, string QuietBase);

public static class BrandPalette
{
    public const string Gold = "f0c878", GoldLit = "fbe0a6", GoldBase = "b88048", Mint = "a8f8e0", MintLit = "d2fff2", MintBase = "50c0b0", Ink = "4b2c2a";

    public static readonly BrandTheme Evening = new("221a1c", "2e2426", "43363a", "5a4644", "5a4644", "f6ead2", "c9b79a",
        "a8f8e0", "a8f8e0", "7fe0c0", "ff9a8a", "43363a", "241c1e");

    // Muted, accent, gain and loss are a shade darker than the mockup so they meet 4.5:1 on the page and hover surfaces.
    public static readonly BrandTheme Paper = new("efe4cf", "fbf5e8", "f3e8d2", "e6d3ae", "d8c7a8", "4b2c2a", "6b5343",
        "226658", "50c0b0", "17644a", "98372e", "fbf5e8", "d8c7a8");

    public static BrandTheme For(bool dark) => dark ? Evening : Paper;

    // WCAG 2 contrast ratio between two colours.
    public static double Contrast(string a, string b)
    {
        var (x, y) = (Luminance(a), Luminance(b));
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int at)
        {
            var c = Convert.ToInt32(hex.Substring(at, 2), 16) / 255.0;
            return c <= .03928 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);
        }
        return .2126 * Channel(0) + .7152 * Channel(2) + .0722 * Channel(4);
    }
}
