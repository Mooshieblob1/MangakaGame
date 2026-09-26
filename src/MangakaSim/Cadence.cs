namespace MangakaSim;

public enum Cadence
{
    Weekly,
    Biweekly,
    Monthly,
}

public static class CadenceRules
{
    public static DateTime NextDue(DateTime from, Cadence cadence) => cadence switch
    {
        Cadence.Weekly => from.AddDays(7),
        Cadence.Biweekly => from.AddDays(14),
        Cadence.Monthly => from.AddMonths(1),
        _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, null),
    };
    // Typical magazine chapter lengths: weekly shonen about 19 pages, biweekly about 24, monthly about 32.
    public static int MagazinePages(Cadence cadence) => cadence switch
    {
        Cadence.Weekly => 19,
        Cadence.Biweekly => 24,
        Cadence.Monthly => 32,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, null),
    };
}
