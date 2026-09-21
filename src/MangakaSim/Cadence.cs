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
}
