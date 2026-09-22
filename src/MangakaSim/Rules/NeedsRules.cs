namespace MangakaSim;

/// <summary>Hunger, thirst and comfort deplete while working and recover on breaks.</summary>
public static class NeedsRules
{
    public const double BreakThreshold = 25;
    public const double BareRecovery = 20;
    public const double SideRecovery = 10;

    /// <summary>One working hour: hunger -5 (-8 overtime), thirst -6 (-9), comfort -rate (-2 x rate).</summary>
    public static Needs Deplete(Needs needs, bool overtime, double comfortRate)
    {
        var next = needs.Clone();
        next.Hunger = Math.Max(0, next.Hunger - (overtime ? 8 : 5));
        next.Thirst = Math.Max(0, next.Thirst - (overtime ? 9 : 6));
        next.Comfort = Math.Max(0, next.Comfort - (overtime ? 2 * comfortRate : comfortRate));
        return next;
    }

    public static bool NeedsBreak(Needs needs) => needs.Min < BreakThreshold;

    /// <summary>The lowest need gains the amenity's recovery, the other two gain 10; all capped at 100.</summary>
    public static Needs Recover(Needs needs, string lowestNeed, double recovery)
    {
        var next = needs.Clone();
        foreach (var need in StaffCatalog.NeedNames)
            next.Set(need, Math.Min(100, next.Get(need) + (need == lowestNeed ? recovery : SideRecovery)));
        return next;
    }

    /// <summary>Comfort depletion per regular hour from the best comfort amenity: office chairs 1, sofa 2, otherwise 3.</summary>
    public static double ComfortRate(IEnumerable<Amenity> owned)
    {
        var best = owned.Where(a => a.Need == "comfort").Select(a => a.RecoveryPerBreak).DefaultIfEmpty(0).Max();
        return best >= 60 ? 1 : best >= 50 ? 2 : 3;
    }

    /// <summary>Recovery for a need on a break: the best amenity serving it, or 20 when there is none.</summary>
    public static double Recovery(string need, IEnumerable<Amenity> owned)
    {
        var best = owned.Where(a => a.Need == need).Select(a => a.RecoveryPerBreak).DefaultIfEmpty(0).Max();
        return best > 0 ? best : BareRecovery;
    }
}
