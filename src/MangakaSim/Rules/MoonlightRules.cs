namespace MangakaSim;

/// <summary>Unhappy assistants take outside work, then leave.</summary>
public static class MoonlightRules
{
    public const double StartBelow = 40;
    public const double StopAt = 55;
    public const double QuitBelow = 30;
    public const int EarlyLeaveHours = 2;

    public static double StartChance(double happiness) => happiness < StartBelow ? 0.15 : 0;

    public static bool Stops(double happiness) => happiness >= StopAt;

    /// <summary>(30 - happiness) / 100 scaled by loyalty, which falls 5% per month employed to a floor of 0.3.</summary>
    public static double QuitChance(double happiness, int monthsEmployed)
    {
        if (happiness >= QuitBelow) return 0;
        var loyalty = Math.Max(0.3, 1 - 0.05 * monthsEmployed);
        return (QuitBelow - happiness) / 100.0 * loyalty;
    }
}
