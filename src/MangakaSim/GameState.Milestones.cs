using MangakaSim.Catalog;
using System.Text.Json.Serialization;
using MangakaSim.Rules;

namespace MangakaSim;

// Aki's story milestones (spec docs/superpowers/specs/2026-10-05-aki-story-milestones-design.md): in the quiet years of a
// settled career, one-off choices about Aki's work, each with its cost stated before the player picks.
public partial class GameState
{
    public MilestoneState Milestones { get; set; } = new();

    /// <summary>The waiting milestone's card, or null when nothing waits.</summary>
    [JsonIgnore]
    public MilestoneScene? PendingMilestone => Milestones.Pending is { } id ? AkiMilestones.Describe(this, id, Milestones.Target, Milestones.Magazine) : null;

    // A settled career has finished the Serialized goal chapter: a team, a collected volume and the top 10.
    private bool MilestoneSettled => Goals is { } goals ? goals.Chapter > 2 : GoalStaff >= 1 && GoalCollectedVolume && GoalTopRank(10).Done;

    private bool QuietSince(DateTime from)
    {
        for (var i = Events.Count - 1; i >= 0 && Events[i].Time >= from; i--)
            if (CareerGuidance.FastSpeedStops.Contains(Events[i].Type)) return false;
        return true;
    }

    private double RunMonths(Series series) => series.Chapters.Where(c => c.PublishedAt is not null).Select(c => c.PublishedAt!.Value)
        .DefaultIfEmpty(Clock.Now).Min() is var first ? (Clock.Now - first).TotalDays / (365.25 / 12) : 0;

    private IEnumerable<Series> OwnRunningSeries => Series.Where(s => s.BusinessId == ControlledBusinessId && s.Status == SeriesStatus.Active &&
        s.Publishing == PublishingStatus.Serialized && s.Contract is not null && s.LeadPersonId == ProtagonistPersonId).OrderBy(s => s.Id);

    /// <summary>The first milestone whose condition holds now, with its target and magazine.</summary>
    private (string Id, int Target, string? Magazine)? NextMilestone()
    {
        var done = Milestones.Done;
        foreach (var series in OwnRunningSeries)
        {
            if (Milestones.FinalArcSeries == series.Id || RunMonths(series) < AkiMilestones.FinalArcMonths) continue;
            var last = done.LastOrDefault(r => r.Id == AkiMilestones.FinalArc && r.Target == series.Id);
            if (last is null || last.Answer == 1 && last.At.AddMonths(AkiMilestones.AskAgainMonths) <= Clock.Now) return (AkiMilestones.FinalArc, series.Id, null);
        }
        if (Since(AkiMilestones.BiggerMagazine) >= AkiMilestones.BiggerMagazineAgainMonths && OwnRunningSeries.Count() < 2)
            foreach (var series in OwnRunningSeries)
            {
                var magazine = PublisherCatalog.Get(series.Contract!.MagazineId);
                if (magazine.Tier == 1) continue;
                var ranks = series.Chapters.Where(c => c.Rank is not null && c.PublishedAt is not null).OrderBy(c => c.PublishedAt).TakeLast(AkiMilestones.TopRankIssues).ToArray();
                if (ranks.Length < AkiMilestones.TopRankIssues || ranks.Any(c => c.Rank > 3)) continue;
                var genre = TrendRules.Normalise(series.Genre, TrendCatalog);
                var bigger = PublisherCatalog.Magazines.Where(m => m.Tier == 1).OrderByDescending(m => m.Affinity(genre)).ThenBy(m => m.Id).FirstOrDefault();
                if (bigger is not null) return (AkiMilestones.BiggerMagazine, series.Id, bigger.Id);
            }
        // Recurring milestones (re-run 2026-10-06): a different assistant every two years, the bigger magazine again after three.
        if (Since(AkiMilestones.AssistantDebut) >= AkiMilestones.DebutEveryMonths)
            foreach (var person in ControlledStaff.Where(p => p.Id != ProtagonistPersonId).OrderBy(p => p.Id))
                if (person.Employment is { NoticeEndsAt: null } job && job.StartsAt.AddYears(AkiMilestones.DebutYears) <= Clock.Now &&
                    person.Skills.Values.DefaultIfEmpty(0).Max() >= AkiMilestones.DebutSkill &&
                    !done.Any(r => r.Id == AkiMilestones.AssistantDebut && r.Target == person.Id) &&
                    !Series.Any(s => s.LeadPersonId == person.Id && s.Status == SeriesStatus.Active))
                    return (AkiMilestones.AssistantDebut, person.Id, null);
        if (Milestones.TeachingUntil is null && (GoalShortlisted || GoalTopRank(10).Done) &&
            Since(AkiMilestones.Teaching) >= (done.LastOrDefault(r => r.Id == AkiMilestones.Teaching)?.Answer == 0 ? AkiMilestones.TeachAgainMonths : AkiMilestones.TeachDeclinedMonths))
            return (AkiMilestones.Teaching, ProtagonistPersonId, null);
        if (Clock.Now >= AkiMilestones.OverseasFrom && Since(AkiMilestones.OverseasConvention) >= AkiMilestones.OverseasEveryMonths &&
            Series.Where(s => s.BusinessId == ControlledBusinessId && s.LeadPersonId == ProtagonistPersonId && s.Volumes.Any(v => !v.IsDoujin && v.ReleasedAt is not null))
                .OrderByDescending(s => s.Fanbase).ThenBy(s => s.Id).FirstOrDefault() is { } famous)
            return (AkiMilestones.OverseasConvention, famous.Id, null);
        var parents = done.Where(r => r.Id == AkiMilestones.ParentsHouse).ToArray();
        if (GoalWorksFromStudio && PersonalMoney >= MilestoneYen(AkiMilestones.SavingsForRepairs) &&
            (parents.Length == 0 || parents.Length == 1 && parents[0].Answer == 1 && parents[0].At.AddMonths(AkiMilestones.ParentsAgainMonths) <= Clock.Now))
            return (AkiMilestones.ParentsHouse, ProtagonistPersonId, null);
        return null;
    }

    /// <summary>Months since this milestone was last answered, or a large number if never.</summary>
    private double Since(string id) => Milestones.Done.LastOrDefault(r => r.Id == id) is { } last ? (Clock.Now - last.At).TotalDays / (365.25 / 12) : 1_000;

    /// <summary>A 1996 yen amount at today's prices.</summary>
    internal long MilestoneYen(long yen) => Economy.Yen(yen * Economy.PriceIndex(TrendCatalog, Clock.Now));

    private bool MilestoneTargetValid() => Milestones.Pending switch
    {
        AkiMilestones.FinalArc or AkiMilestones.BiggerMagazine => OwnRunningSeries.Any(s => s.Id == Milestones.Target),
        AkiMilestones.AssistantDebut => ControlledStaff.Any(p => p.Id == Milestones.Target && p.Employment is { NoticeEndsAt: null }),
        AkiMilestones.Teaching or AkiMilestones.ParentsHouse => Milestones.Target == ProtagonistPersonId,
        AkiMilestones.OverseasConvention => FindSeries(Milestones.Target) is { } series && series.BusinessId == ControlledBusinessId,
        _ => false,
    };

    /// <summary>Daily at 08:00: closes a decision window with the safer answer, withdraws a milestone whose subject is gone,
    /// tidies running effects and offers the next milestone in a quiet stretch.</summary>
    private void MilestoneStep()
    {
        var m = Milestones;
        // Teaching (milestone 4): Wednesday afternoon goes to the class, and the lecture fee is paid to Aki.
        if (m.TeachingUntil is { } teaching)
        {
            if (teaching <= Clock.Now) m.TeachingUntil = null;
            else if (Clock.DayOfWeek == DayOfWeek.Wednesday && Clock.Hour == 14)
            {
                var classEnds = Clock.Now.AddHours(AkiMilestones.TeachingHours);
                if (!(Protagonist.BusyUntil > classEnds)) Protagonist.BusyUntil = classEnds;
                AccountPost(Protagonist.PersonalAccount, MilestoneYen(AkiMilestones.LectureFee), "manga school lecture", AccountEntryKind.PersonalIncome);
            }
        }
        if (Clock.Hour != 8 || Control != ControlMode.OwnerDirector) return;
        if (m.FinalArcSeries is { } arc && !OwnRunningSeries.Any(s => s.Id == arc)) m.FinalArcSeries = null;
        if (m.GuaranteedUntil <= Clock.Now) { m.GuaranteedMagazine = null; m.GuaranteedUntil = null; }
        if (m.ClosedUntil <= Clock.Now) { m.ClosedMagazine = null; m.ClosedUntil = null; }
        if (m.Pending is not null)
        {
            if (!MilestoneTargetValid()) { m.Pending = null; m.Magazine = null; m.Target = 0; }
            else if (Clock.Now >= m.DueAt) ResolveMilestone(AkiMilestones.DefaultAnswer, true);
            return;
        }
        if (!MilestoneSettled || m.OfferedAt.AddDays(AkiMilestones.SpacingDays) > Clock.Now || !QuietSince(Clock.Now.AddDays(-AkiMilestones.QuietDays))) return;
        if (NextMilestone() is not { } next) return;
        m.Pending = next.Id; m.Target = next.Target; m.Magazine = next.Magazine;
        m.OfferedAt = Clock.Now; m.DueAt = Clock.Now.Date.AddDays(AkiMilestones.DecisionDays).AddHours(8);
        var aboutSeries = next.Id is AkiMilestones.FinalArc or AkiMilestones.BiggerMagazine or AkiMilestones.OverseasConvention;
        Emit(EventType.MilestoneOffered, $"A decision is waiting: {PendingMilestone!.Title}.", aboutSeries ? next.Target : null,
            personId: aboutSeries ? null : next.Target);
    }

    private void ApplyMilestone(MilestoneCommand c)
    {
        if (c.Id is null || c.Id != Milestones.Pending || c.Answer is not (0 or 1) || !MilestoneTargetValid())
            throw new InvalidCommandException("That decision is no longer waiting.");
        if (c.Answer == 0 && c.Id == AkiMilestones.OverseasConvention && FreeCash(ControlledBusinessId) < MilestoneYen(AkiMilestones.TripCost))
            throw new InvalidCommandException($"The trip needs ¥{MilestoneYen(AkiMilestones.TripCost):N0} of business cash after wages and bills.");
        if (c.Answer == 0 && c.Id == AkiMilestones.ParentsHouse && Spendable(Protagonist.PersonalAccount) < MilestoneYen(AkiMilestones.RepairCost))
            throw new InvalidCommandException($"The repairs need ¥{MilestoneYen(AkiMilestones.RepairCost):N0} of personal savings.");
        ResolveMilestone(c.Answer, false);
    }

    private void ResolveMilestone(int answer, bool defaulted)
    {
        var m = Milestones;
        var scene = PendingMilestone!;
        switch (m.Pending)
        {
            case AkiMilestones.FinalArc:
            {
                var series = FindSeries(m.Target)!;
                if (answer == 0) { m.FinalArcSeries = series.Id; m.FinalArcChaptersLeft = AkiMilestones.FinalArcChapters; }
                else series.Contract = series.Contract! with { FeePerPage = series.Contract.FeePerPage * 11 / 10 };
                break;
            }
            case AkiMilestones.BiggerMagazine:
            {
                var series = FindSeries(m.Target)!;
                if (answer == 0) { m.GuaranteedMagazine = m.Magazine; m.GuaranteedUntil = Clock.Now.AddYears(1); }
                else
                {
                    series.Contract = series.Contract! with { FeePerPage = series.Contract.FeePerPage * 11 / 10 };
                    series.Fanbase += FanbaseRules.Saturated(series.Fanbase, series.Fanbase * .05);
                    m.ClosedMagazine = m.Magazine; m.ClosedUntil = Clock.Now.AddYears(2);
                }
                break;
            }
            case AkiMilestones.AssistantDebut:
            {
                var person = FindPerson(m.Target)!;
                if (answer == 0)
                {
                    var title = CreateSeries($"{person.Name}'s debut", "Drama", Cadence.Monthly, 19);
                    title.LeadPersonId = title.RightsLeadPersonId = person.Id; title.LocationId = person.Employment!.LocationId;
                    person.MainSeriesId = title.Id;
                }
                else { person.Happiness = Math.Max(0, person.Happiness - 30); person.Loyalty = Math.Max(0, person.Loyalty - 20); }
                break;
            }
            case AkiMilestones.Teaching when answer == 0:
                m.TeachingUntil = Clock.Now.AddYears(1);
                ChangeTrackRecord(2);
                ChangeReputation(Protagonist, 3);
                break;
            case AkiMilestones.OverseasConvention:
            {
                var series = FindSeries(m.Target)!;
                var go = answer == 0;
                if (go) Spend(ControlledBusinessId, MilestoneYen(AkiMilestones.TripCost), "overseas convention trip");
                var until = Clock.Now.AddDays(go ? AkiMilestones.TripDays : 1);
                if (!(Protagonist.BusyUntil > until)) Protagonist.BusyUntil = until;
                series.Fanbase += FanbaseRules.Saturated(series.Fanbase, series.Fanbase * (go ? .03 : .01));
                foreach (var channel in World.Channels.Where(a => a.SeriesId == series.Id && a.Channel == ReleaseChannel.Overseas && a.Status == NegotiationStatus.Accepted))
                    channel.InternationalInterest = Math.Clamp(channel.InternationalInterest + (go ? 20 : 5), 0, 100);
                if (go) ChangeTrackRecord(1);
                break;
            }
            case AkiMilestones.ParentsHouse when answer == 0:
                AccountPost(Protagonist.PersonalAccount, -MilestoneYen(AkiMilestones.RepairCost), "parents' house repairs", AccountEntryKind.Expense);
                Protagonist.Happiness = 100;
                break;
        }
        var chosen = answer == 0 ? $"{scene.First}: {scene.FirstCost}" : $"{scene.Second}: {scene.SecondCost}";
        Career.Journal.Add(new(scene.Id, answer, Clock.Now, scene.Text + "\n\n" + (defaulted ? "No answer in time, so: " : "") + chosen));
        m.Done.Add(new(scene.Id, m.Target, answer, Clock.Now, defaulted));
        if (defaulted) StudioMessage($"{scene.Title}: no answer in time, so Helper-Chan chose \"{scene.Second}\".");
        m.Pending = null; m.Magazine = null; m.Target = 0;
    }

    /// <summary>Counts a published chapter of the series heading for its planned ending.</summary>
    private void CountFinalArcChapter(Series series)
    {
        if (Milestones.FinalArcSeries == series.Id) Milestones.FinalArcChaptersLeft = Math.Max(0, Milestones.FinalArcChaptersLeft - 1);
    }

    /// <summary>Ends a series whose planned final arc is complete, after its last ranking.</summary>
    private void EndFinishedArc(IEnumerable<Series> competitors)
    {
        if (Milestones.FinalArcSeries is not { } id || Milestones.FinalArcChaptersLeft > 0 || competitors.FirstOrDefault(s => s.Id == id) is not { Status: SeriesStatus.Active } series) return;
        Milestones.FinalArcSeries = null;
        ApplyEnd(new EndSeriesCommand(series.Id));
        StudioMessage($"{series.Title} reached the ending you planned. Its readers are ready for your next series.");
    }

    /// <summary>The magazine closed to pitches after staying loyal, and until when.</summary>
    internal DateTime? MilestoneClosed(string magazineId) =>
        Milestones.ClosedMagazine == magazineId && Milestones.ClosedUntil > Clock.Now ? Milestones.ClosedUntil : null;

    private bool TakeGuaranteedPitch(Series series, Magazine magazine)
    {
        if (series.BusinessId != ControlledBusinessId || Milestones.GuaranteedMagazine != magazine.Id || !(Milestones.GuaranteedUntil > Clock.Now)) return false;
        Milestones.GuaranteedMagazine = null; Milestones.GuaranteedUntil = null;
        return true;
    }

    private void ValidateMilestones()
    {
        static void Check(bool ok, string field) { if (!ok) throw new InvalidDataException($"Save file has invalid story milestone {field}."); }
        var m = Milestones;
        Check(m is not null && m.Done is not null && m.Done.All(r => r is not null && AkiMilestones.Known(r.Id) && r.Answer is 0 or 1), "records");
        Check(m!.Pending is null || AkiMilestones.Known(m.Pending), "pending");
        Check(m.FinalArcChaptersLeft is >= 0 and <= AkiMilestones.FinalArcChapters, "final arc");
        Check(m.TeachingUntil is null || m.TeachingUntil <= Clock.Now.AddYears(1), "teaching");
    }
}
