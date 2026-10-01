using System.Globalization;

namespace MangakaSim;

/// <summary>Title screen timings and sizes (spec 2026-09-28), kept engine-free so they can be unit tested.</summary>
public static class TitleScreen
{
    public const double FadeOutSeconds = .6, FadeInSeconds = 1.0, ReducedFadeSeconds = .2;
    public const double PushInScale = 1.03, PushInSeconds = 40;

    public static double FadeSeconds(bool fadeIn, bool reducedMotion) =>
        reducedMotion ? ReducedFadeSeconds : fadeIn ? FadeInSeconds : FadeOutSeconds;

    public static string ContinueLabel(CareerSaveInfo save) =>
        $"{save.Studio} · {save.GameDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";

    // About 30% of the window width, but never taller than about a third of the window, so wide and short screens keep room for the menu.
    public static double LogoWidth(double width, double height, double logoAspect) =>
        Math.Min(width * .30, height * .32 * logoAspect);
}
