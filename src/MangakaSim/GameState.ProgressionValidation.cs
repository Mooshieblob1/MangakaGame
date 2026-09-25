namespace MangakaSim;

public partial class GameState
{
    private void ValidateProgression(HashSet<int> ids)
    {
        void Check([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok, string field) { if (!ok) throw new InvalidDataException("Invalid progression " + field + "."); }
        bool Range(double value, double min = 0, double max = 100) => double.IsFinite(value) && value >= min && value <= max;
        void Id(int id) => Check(id > 0 && ids.Add(id), "unique identity");
        bool Past(DateTime at) => at >= GameClock.Start && at <= Clock.Now;
        var p = Progression ?? throw new InvalidDataException("Missing progression.");
        Check(p.CatalogVersion == ProgressionCatalog.Version && Past(p.AvailableFrom) && (p.LastDay is null || p.LastDay >= GameClock.Start.Date && p.LastDay <= Clock.Now.Date), "catalog or dates");
        Check(p.AwardsRng is not null && p.LicensingRng is not null && p.Manuscripts is not null && p.Awards is not null &&
            p.Projects is not null && p.Receipts is not null && p.Effects is not null && p.Milestones is not null && p.Achievements is not null &&
            p.Changes is not null && p.PitchCooldowns is not null && p.ConsultationDays is not null && p.AnnualImpact is not null, "collections");
        Check(Enum.IsDefined(p.Difficulty) && ((int)p.Assists & ~511) == 0 && p.Pressure is >= 0 and <= 2 && p.Recovery is >= 0 and <= 2, "difficulty");
        Check(p.EverSandbox == p.SandboxSince.HasValue && (p.SandboxSince is null || Past(p.SandboxSince.Value)), "Sandbox origin");
        Check(p.EverSandbox || p.Difficulty != CareerDifficulty.Sandbox && p.Assists == SandboxAssist.None &&
            p.Changes!.All(c => c.Mode != CareerDifficulty.Sandbox && c.Assists == SandboxAssist.None), "Sandbox permanence");
        Check(!p.EverSandbox || p.Achievements!.Count == 0, "Sandbox platform evidence");
        Check(p.Changes!.All(c => c is not null && Past(c.At) && Enum.IsDefined(c.Mode) && ((int)c.Assists & ~511) == 0 &&
            c.Pressure is >= 0 and <= 2 && c.Recovery is >= 0 and <= 2) && p.Changes.Zip(p.Changes.Skip(1)).All(x => x.First.At <= x.Second.At), "difficulty history");
        if (p.Changes.Count > 0)
        { var c = p.Changes[^1]; Check(c.Mode == p.Difficulty && c.Assists == p.Assists && c.Pressure == p.Pressure && c.Recovery == p.Recovery, "current difficulty"); }
        foreach (var m in p.Manuscripts!)
        {
            Check(m is not null, "manuscript"); Id(m!.Id);
            Check(FindSeries(m.SeriesId) is {} title && (title.Chapters.Any(c => c.Id == m.ChapterId) || m.Released) && FindPerson(m.CreatorId) is not null &&
                m.Revision > 0 && Range(m.Originality) && m.Category is "story" or "comedy", "manuscript reference");
        }
        Check(p.Manuscripts.Select(m => m.SeriesId).Distinct().Count() == p.Manuscripts.Count, "one manuscript per title");
        foreach (var a in p.Awards!)
        {
            Check(a is not null, "award"); Id(a!.Id);
            Check(FindSeries(a.SeriesId) is not null && FindPerson(a.CreatorId) is not null && Past(a.SubmittedAt) &&
                a.ResolvesAt >= a.SubmittedAt && (a.ResolvedAt is null || Past(a.ResolvedAt.Value) && a.ResolvedAt >= a.ResolvesAt) &&
                Range(a.Quality) && Range(a.Originality) && Range(a.Fit) && Range(a.Jury, -5, 5) && a.Competitors is not null &&
                a.Competitors.Count <= 100 && a.Competitors.All(x => Range(x)) && a.Prize is >= 0 and <= 500000 &&
                !string.IsNullOrWhiteSpace(a.Award) && a.Result is not null && a.Feedback is not null, "award snapshot");
            Check(a.ManuscriptId == 0 ? a.Award.StartsWith("annual:", StringComparison.Ordinal) :
                p.Manuscripts.Any(m => m.Id == a.ManuscriptId && m.SeriesId == a.SeriesId && a.Revision > 0 && a.Revision <= m.Revision), "submission reference");
            Check(a.ResolvedAt is null ? a.Placement == 0 && a.Prize == 0 : a.Placement > 0, "award resolution");
        }
        Check(p.Awards.Where(a => a.ManuscriptId > 0 && a.ResolvedAt is null).GroupBy(a => a.ManuscriptId).All(g => g.Count() == 1), "duplicate active entries");
        Check(p.Awards.Select(a => (a.SeriesId, a.Award)).Distinct().Count() == p.Awards.Count, "duplicate edition entries");
        foreach (var l in p.Projects!)
        {
            Check(l is not null, "project"); Id(l!.Id);
            Check(FindSeries(l.SeriesId) is not null && FindPerson(l.CreatorId) is not null && Businesses.Any(b => b.Id == l.BusinessId) &&
                Enum.IsDefined(l.Kind) && Enum.IsDefined(l.Phase) && !string.IsNullOrWhiteSpace(l.Partner) && Past(l.CreatedAt) &&
                l.DueAt >= l.CreatedAt && l.Payment is >= 0 and <= 100000000 && l.CreatorPercent is >= 0 and <= 100 &&
                l.Control is >= 0 and <= 2 && l.Involvement >= 0 && l.Involvement <= l.Control && l.Rounds is >= 0 and <= 2 &&
                l.Season > 0 && l.Weeks is >= 1 and <= 100 && l.SourceChapters >= 0 && l.SourceStart >= 0 && l.ConsultationHours >= 0 &&
                Range(l.Reliability, 0, 1) && Range(l.Roll, 0, 1) && Range(l.Fit) && Range(l.Reception) && l.Negotiations is not null &&
                l.Negotiations.Count == l.Rounds && l.Outcome is not null && l.Decision is not null, "project terms");
            Check(l.SignedAt is null || Past(l.SignedAt.Value) && l.SignedAt >= l.CreatedAt, "signature");
            Check(!LiveLicense(l) || l.SignedAt is not null, "active signature");
            Check(l.ReleasedAt is null || Past(l.ReleasedAt.Value) && l.SignedAt is not null && l.ReleasedAt >= l.SignedAt, "release");
            Check(l.Decision!.Length == 0 || l.DecisionAt is {} d && Past(d) && l.Decision is "Source material" or "Schedule pressure", "decision");
            Check(!l.OriginalEnding || l.Original, "original ending");
            Check(Range(l.ReputationLoss,0,2) && (l.ReputationRecoveryAt is not null || l.ReputationLoss == 0), "reputation recovery");
            Check(l.LastReceipt is null || Past(l.LastReceipt.Value) && l.ReleasedAt is not null && l.LastReceipt >= l.ReleasedAt, "receipt date");
            Check(l.EmployerRequestedAt is null || Past(l.EmployerRequestedAt.Value) && l.EmployerRequestedAt >= l.CreatedAt, "employer request");
            Check(l.Phase is not (LicensePhase.Released or LicensePhase.Completed) || l.ReleasedAt is not null && l.Settled, "release settlement");
        }
        Check(p.Projects.Where(LiveLicense).GroupBy(l => (l.SeriesId,l.Kind)).All(g => g.Count() == 1), "exclusive category");
        foreach (var r in p.Receipts!)
            Check(r is not null && p.Projects.Any(l => l.Id == r.ProjectId && l.SignedAt is not null && r.At >= l.SignedAt) &&
                Past(r.At) && r.Gross > 0 && r.CreatorShare >= 0 && r.CreatorShare <= r.Gross, "receipt");
        Check(p.Effects!.All(e => e is not null && FindSeries(e.SeriesId) is not null &&
            (p.Awards.Any(a => a.Id == e.SourceId) || p.Projects.Any(l => l.Id == e.SourceId)) && Past(e.Start) && e.End > e.Start && Range(e.Lift, 0, .5)), "effects");
        Check(p.Milestones!.All(m => m is not null && !string.IsNullOrWhiteSpace(m.Key) && m.Text is not null && Past(m.At) &&
            (FindSeries(m.Entity) is not null || FindPerson(m.Entity) is not null)) &&
            p.Milestones.Select(m => (m.Key, m.Entity)).Distinct().Count() == p.Milestones.Count, "milestones");
        Check(p.Achievements!.All(a => a is not null && Past(a.At) && p.Milestones.Any(m => m.Key == a.Key && m.At <= a.At)) &&
            p.Achievements.Select(a => a.Key).Distinct().Count() == p.Achievements.Count, "achievement evidence");
        Check(p.AnnualImpact!.All(a => Range(a.Value, 0, 10)) && p.ConsultationDays!.All(a => FindPerson(a.Key) is not null && a.Value >= GameClock.Start.Date && a.Value <= Clock.Now.Date), "impact and consultation");
    }
}
