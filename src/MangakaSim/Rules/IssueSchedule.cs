using MangakaSim.Catalog;

namespace MangakaSim.Rules;

public static class IssueSchedule
{
    public static int Days(Cadence cadence) => cadence switch
    {
        Cadence.Weekly => 7, Cadence.Biweekly => 14, Cadence.Monthly => 28,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence)),
    };
    public static DateTime Anchor(Magazine m)
    {
        var first = GameClock.Start.Date.AddHours(m.IssueCloseHour);
        first = first.AddDays(((int)m.IssueCloseDay - (int)first.DayOfWeek + 7) % 7);
        return first < GameClock.Start ? first.AddDays(7) : first;
    }
    public static DateTime FirstCloseAtOrAfter(Magazine m, DateTime date)
    {
        var anchor = Anchor(m);
        return date <= anchor ? anchor : anchor.AddDays(Math.Ceiling((date - anchor).TotalDays / Days(m.Cadence)) * Days(m.Cadence));
    }
    public static DateTime FirstCloseAfter(Magazine m, DateTime date)
    {
        var close = FirstCloseAtOrAfter(m, date);
        return close > date ? close : AddIssues(m, close, 1);
    }
    public static DateTime AddIssues(Magazine m, DateTime close, int count) => close.AddDays((long)Days(m.Cadence) * count);
}
