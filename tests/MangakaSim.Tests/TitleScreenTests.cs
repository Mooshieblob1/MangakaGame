using Xunit;
namespace MangakaSim.Tests;

public class TitleScreenTests
{
    [Fact] public void Fades_are_short_out_and_gentle_in()
    {
        Assert.Equal(.6, TitleScreen.FadeSeconds(fadeIn: false, reducedMotion: false));
        Assert.Equal(1.0, TitleScreen.FadeSeconds(fadeIn: true, reducedMotion: false));
    }

    [Fact] public void Reduced_motion_shortens_both_fades()
    {
        Assert.Equal(.2, TitleScreen.FadeSeconds(false, true));
        Assert.Equal(.2, TitleScreen.FadeSeconds(true, true));
    }

    [Fact] public void Continue_caption_names_the_studio_and_game_date()
    {
        var save = new CareerSaveInfo("c", "s", "Daily autosave", true, new DateTime(2026, 9, 28), new DateTime(1997, 6, 3), "Haruka Studio");
        Assert.Equal("Haruka Studio · 3 Jun 1997", TitleScreen.ContinueLabel(save));
    }

    [Theory]
    [InlineData(1920, 1080, 552.96)]  // 16:9: height-limited
    [InlineData(3440, 1440, 737.28)]  // 21:9: height-limited, never swamps the screen
    [InlineData(1280, 720, 368.64)]
    [InlineData(1000, 1200, 300)]     // tall window: width-limited
    public void Logo_width_follows_the_window_not_the_text_size(double width, double height, double expected) =>
        Assert.Equal(expected, TitleScreen.LogoWidth(width, height, 1.6), 2);
}
