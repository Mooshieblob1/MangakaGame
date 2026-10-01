using Xunit;
namespace MangakaSim.Tests;

public class BrandPaletteTests
{
    [Fact] public void Contrast_follows_the_standard_formula()
    {
        Assert.Equal(21, BrandPalette.Contrast("ffffff", "000000"), 2);
        Assert.Equal(1, BrandPalette.Contrast("4b2c2a", "4b2c2a"), 3);
    }

    [Theory, InlineData(true), InlineData(false)]
    public void Every_text_colour_reads_on_every_surface(bool dark)
    {
        var t = BrandPalette.For(dark);
        foreach (var (fg, name) in new[] { (t.Text, "text"), (t.Muted, "muted"), (t.Accent, "accent"), (t.Gain, "gain"), (t.Loss, "loss") })
            foreach (var bg in new[] { t.Card, t.Wash, t.Hover })
                Assert.True(BrandPalette.Contrast(fg, bg) >= 4.5, $"{(dark ? "evening" : "paper")} {name} #{fg} on #{bg}: {BrandPalette.Contrast(fg, bg):0.00}");
        Assert.True(BrandPalette.Contrast(t.Text, t.Pressed) >= 4.5, "text on pressed");
        Assert.True(BrandPalette.Contrast(t.Text, t.QuietFace) >= 4.5, "text on quiet slab face");
    }

    [Fact] public void Slab_ink_reads_on_gold_and_mint_faces()
    {
        foreach (var face in new[] { BrandPalette.Gold, BrandPalette.GoldLit, BrandPalette.Mint, BrandPalette.MintLit })
            Assert.True(BrandPalette.Contrast(BrandPalette.Ink, face) >= 4.5, face);
    }
}
