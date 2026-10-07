namespace MangakaSim.Rules;

public static class FanbaseRules
{
    public static double RankFactor(int rank, int line, int roster) => rank <= line
        ? 3 - 2.5 * (Math.Max(1, rank) - 1) / (line - 1)
        : Math.Max(.2, .5 - .3 * (rank - line) / (roster - line));
    public static double Gain(int tier, double quality, double rankFactor) =>
        (tier switch { 1 => 3000, 2 => 1500, 3 => 800, _ => throw new ArgumentOutOfRangeException(nameof(tier)) }) * rankFactor * quality / 70;
    /// <summary>Fan gain from one published chapter. A long run (see <see cref="FatigueGain"/>) joins readers more slowly and loses them faster.</summary>
    public static double AfterPublication(double fans, int tier, int quality, int rank, int line, int roster, bool iconic,
        double runMonths = 0, double chaptersPerMonth = 1) =>
        fans * ((iconic ? 1 : .995) - FatigueDecay(runMonths) / chaptersPerMonth) +
        Saturated(fans, Gain(tier, quality, iconic ? 1.5 : RankFactor(rank, line, roster)) * FatigueGain(runMonths));

    // Ten-year balance pass (2026-10-05). The best-selling magazine of the period, Weekly Shonen Jump, peaked at about
    // 6.5 million copies a week in 1995; no series should outgrow about 10 million readers.
    public const double ReadershipCeiling = 10_000_000;
    /// <summary>Shrinks a fan gain as the series nears the readership ceiling. Losses pass through unchanged.</summary>
    public static double Saturated(double fans, double gain) => gain <= 0 ? gain : gain * Math.Clamp(1 - fans / ReadershipCeiling, 0, 1);

    // Long runs tire: after about 70 months (70 monthly chapters) new readers arrive more slowly and old ones drift away.
    public const double FatigueStartMonths = 70;
    /// <summary>Share of the usual per-chapter gain a series still earns after running this many months.</summary>
    public static double FatigueGain(double runMonths) => Math.Clamp(1 - (runMonths - FatigueStartMonths) / 60, .25, 1);
    /// <summary>Extra share of readers lost per month of publication, on top of the usual turnover.</summary>
    public static double FatigueDecay(double runMonths) => Math.Clamp((runMonths - FatigueStartMonths) * .0002, 0, .006);
    public static double ChaptersPerMonth(Cadence cadence) => cadence switch
    {
        Cadence.Weekly => 52 / 12d,
        Cadence.Biweekly => 26 / 12d,
        _ => 1,
    };

    /// <summary>Share of a creator's readers from their other titles who follow them to a new magazine series.</summary>
    public const double FollowingShare = .3;
}
