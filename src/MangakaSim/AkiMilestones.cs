namespace MangakaSim;

/// <summary>Answers a waiting story milestone: 0 for the first answer, 1 for the second (spec 2026-10-05).</summary>
public sealed record MilestoneCommand(string Id, int Answer) : ICommand;
public sealed record MilestoneRecord(string Id, int Target, int Answer, DateTime At, bool Defaulted);
/// <summary>A milestone card: the scene, then each answer with one plain line of what it costs and gives.</summary>
public sealed record MilestoneScene(string Id, string Title, string Text, string First, string FirstCost, string Second, string SecondCost, string Expression = "neutral");

/// <summary>Aki's story milestones (spec 2026-10-05): one-off choices with a stated cost for settled careers. Saves
/// written before them start with none offered.</summary>
public sealed class MilestoneState
{
    public string? Pending { get; set; }
    /// <summary>The series or person the waiting milestone is about.</summary>
    public int Target { get; set; }
    public string? Magazine { get; set; }
    public DateTime OfferedAt { get; set; } = GameClock.Start;
    public DateTime DueAt { get; set; } = GameClock.Start;
    public List<MilestoneRecord> Done { get; set; } = new();
    public int? FinalArcSeries { get; set; }
    public int FinalArcChaptersLeft { get; set; }
    public string? GuaranteedMagazine { get; set; }
    public DateTime? GuaranteedUntil { get; set; }
    public string? ClosedMagazine { get; set; }
    public DateTime? ClosedUntil { get; set; }
}

public static class AkiMilestones
{
    public const string FinalArc = "final-arc", BiggerMagazine = "bigger-magazine", AssistantDebut = "assistant-debut";
    public static readonly string[] All = [FinalArc, BiggerMagazine, AssistantDebut];
    /// <summary>Days with no 32x stop before a milestone may arrive, days between milestones and days to decide.</summary>
    public const int QuietDays = 45, SpacingDays = 120, DecisionDays = 30;
    public const int FinalArcMonths = 60, FinalArcChapters = 12, AskAgainMonths = 24, TopRankIssues = 12, DebutYears = 2, DebutSkill = 70;
    /// <summary>When the decision window closes, the safer answer applies (open question 3, recommended default).</summary>
    public const int DefaultAnswer = 1;
    public static bool Known(string id) => All.Contains(id);

    public static MilestoneScene Describe(GameState state, string id, int target, string? magazineId)
    {
        switch (id)
        {
            case FinalArc:
            {
                var title = state.FindSeries(target)?.Title ?? "Your series";
                return new(id, "The final arc?", $"{title} has run for five years. Your editor asked me, very politely, whether the story is heading for its ending.",
                    "Plan the ending", $"Ends in about {FinalArcChapters} chapters with a farewell bonus, and its readers follow you to your next series. Its page fees stop.",
                    "Keep it running", "Page fee up 10%. Readers keep tiring as the run goes on, and the editor asks again in two years.", "concerned");
            }
            case BiggerMagazine:
            {
                var series = state.FindSeries(target);
                var current = series?.Contract is { } contract ? state.PublisherCatalog.Get(contract.MagazineId).Name : "Your magazine";
                var bigger = magazineId is null ? "A bigger magazine" : state.PublisherCatalog.Get(magazineId).Name;
                return new(id, "A bigger magazine calls", $"An editor from {bigger} called about you! They want you to launch a new series with them. {current} has heard already.",
                    "Accept their invitation", $"Your next pitch to {bigger} within a year is accepted. A second series needs a free desk, an assistant and your time.",
                    "Stay loyal", $"{current} raises your page fee 10% and runs a cover feature (more readers). {bigger} won't take your pitches for two years.", "happy");
            }
            case AssistantDebut:
            {
                var name = state.FindPerson(target)?.Name ?? "Your assistant";
                return new(id, $"{name} wants to debut", $"{name} has been drawing their own pages after hours. They asked me to ask you, because they're too nervous.",
                    "Back their debut", $"{name} leads a new series in your studio and stays. Their hours go to their own title, so you may need another hire.",
                    "Ask them to wait", $"Nothing changes now. {name}'s happiness and loyalty fall, so a rival studio could lure them away.");
            }
            default: throw new InvalidCommandException("Unknown decision.");
        }
    }
}
