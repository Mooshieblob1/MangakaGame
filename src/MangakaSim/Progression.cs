using System.Text.Json.Serialization;

namespace MangakaSim;

public enum CareerDifficulty { Relaxed, Standard, Challenging, Custom, Sandbox }
[Flags] public enum SandboxAssist
{
    None = 0, PersonalFunds = 1, BusinessFunds = 2, InstantProduction = 4,
    InstantDelivery = 8, UnlockLocations = 16, UnlockEquipment = 32,
    NoStress = 64, NoDeadlinePenalties = 128, FutureTechnology = 256
}
public record DifficultyCommand(CareerDifficulty Mode, SandboxAssist Assists = SandboxAssist.None,
    int Pressure = 1, int Recovery = 1, int Cushion = 1) : ICommand;
public record DifficultyChange(DateTime At, CareerDifficulty Mode, SandboxAssist Assists, int Pressure, int Recovery);
public enum RecognitionAction { CreateManuscript, Revise, Submit, ReleaseManuscript }
public record RecognitionCommand(RecognitionAction Action, int Target = 0, string Text = "", string Category = "story") : ICommand;
public record AdoptManuscriptCommand(int ChapterId) : ICommand;
public enum LicenseKind { Anime, Figures, Clothing, Stationery }
public enum LicenseAction { Pitch, Accept, Decline, Payment, Control, Schedule, Involvement, Wait, SideStories, OriginalEnding, RespondDelay }
public record LicenseCommand(LicenseAction Action, int Target, LicenseKind Kind = LicenseKind.Anime, int Value = 0) : ICommand;
public enum LicensePhase { Offer, PreProduction, Production, Released, Completed, Cancelled, Declined, Expired }

public sealed class ContestManuscript
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int ChapterId { get; set; }
    public int Revision { get; set; } = 1;
    public int CreatorId { get; set; }
    public double Originality { get; set; }
    public string Category { get; set; } = "story";
    public bool Released { get; set; }
}
public sealed class AwardEntry
{
    public int Id { get; set; }
    public int ManuscriptId { get; set; }
    public int SeriesId { get; set; }
    public int CreatorId { get; set; }
    public int Revision { get; set; }
    public string Award { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public DateTime ResolvesAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public double Quality { get; set; }
    public double Originality { get; set; }
    public double Fit { get; set; }
    public double Jury { get; set; }
    public List<double> Competitors { get; set; } = new();
    public int Placement { get; set; }
    public string Result { get; set; } = "Awaiting judging";
    public string Feedback { get; set; } = "";
    public long Prize { get; set; }
}
public sealed class LicenseProject
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int BusinessId { get; set; }
    public int CreatorId { get; set; }
    public LicenseKind Kind { get; set; }
    public LicensePhase Phase { get; set; }
    public string Partner { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? LastReceipt { get; set; }
    public DateTime? DecisionAt { get; set; }
    public DateTime? EmployerRequestedAt { get; set; }
    public DateTime? ReputationRecoveryAt { get; set; }
    public double ReputationLoss { get; set; }
    public bool EmployerApproved { get; set; }
    public long Payment { get; set; }
    public int CreatorPercent { get; set; }
    public int Control { get; set; }
    public int Involvement { get; set; }
    public int Rounds { get; set; }
    public int Season { get; set; } = 1;
    public int SourceChapters { get; set; }
    public int SourceStart { get; set; }
    public int Weeks { get; set; }
    public int ConsultationHours { get; set; }
    public double Reliability { get; set; }
    public double Fit { get; set; }
    public double Reception { get; set; }
    public double Roll { get; set; }
    public bool WarningChecked { get; set; }
    public bool Settled { get; set; }
    public bool Original { get; set; }
    public bool OriginalEnding { get; set; }
    public bool ShortSeason { get; set; }
    public string Decision { get; set; } = "";
    public string Outcome { get; set; } = "Offer available";
    public List<string> Negotiations { get; set; } = new();
}
public record LicenseReceipt(int ProjectId, DateTime At, long Gross, long CreatorShare);
public record RecognitionEffect(int SourceId, int SeriesId, DateTime Start, DateTime End, double Lift);
public record CareerMilestone(string Key, int Entity, DateTime At, string Text);
public record AchievementEvidence(string Key, DateTime At);

public sealed class ProgressionState
{
    [JsonRequired] public int CatalogVersion { get; set; } = 1;
    [JsonRequired] public DateTime AvailableFrom { get; set; } = GameClock.Start;
    [JsonRequired] public DateTime? LastDay { get; set; }
    [JsonRequired] public Rng AwardsRng { get; set; } = Rng.FromSeed(1);
    [JsonRequired] public Rng LicensingRng { get; set; } = Rng.FromSeed(2);
    [JsonRequired] public CareerDifficulty Difficulty { get; set; } = CareerDifficulty.Standard;
    [JsonRequired] public SandboxAssist Assists { get; set; }
    [JsonRequired] public int Pressure { get; set; } = 1;
    [JsonRequired] public int Recovery { get; set; } = 1;
    [JsonRequired] public bool EverSandbox { get; set; }
    [JsonRequired] public DateTime? SandboxSince { get; set; }
    [JsonRequired] public bool VerifiedOrigin { get; set; } = true;
    [JsonRequired] public List<DifficultyChange> Changes { get; set; } = new();
    [JsonRequired] public List<ContestManuscript> Manuscripts { get; set; } = new();
    [JsonRequired] public List<AwardEntry> Awards { get; set; } = new();
    [JsonRequired] public List<LicenseProject> Projects { get; set; } = new();
    [JsonRequired] public List<LicenseReceipt> Receipts { get; set; } = new();
    [JsonRequired] public List<RecognitionEffect> Effects { get; set; } = new();
    [JsonRequired] public List<CareerMilestone> Milestones { get; set; } = new();
    [JsonRequired] public List<AchievementEvidence> Achievements { get; set; } = new();
    [JsonRequired] public Dictionary<string, DateTime> PitchCooldowns { get; set; } = new();
    [JsonRequired] public Dictionary<int, DateTime> ConsultationDays { get; set; } = new();
    [JsonRequired] public Dictionary<string, double> AnnualImpact { get; set; } = new();
}

public interface IAchievementSink { void Unlock(string key); }
public sealed class NoOpAchievementSink : IAchievementSink { public void Unlock(string key) { } }
public static class AchievementDelivery
{
    public static bool Eligible(GameState state) => state.Progression.VerifiedOrigin && !state.Progression.EverSandbox &&
        state.Progression.Difficulty != CareerDifficulty.Sandbox && state.Progression.Assists == SandboxAssist.None;
    public static void Deliver(GameState state, IAchievementSink sink)
    {
        if (!Eligible(state)) return;
        foreach (var item in state.Progression.Achievements)
            if (ProgressionCatalog.Achievements.FirstOrDefault(a => a.Key == item.Key) is {} definition) sink.Unlock(definition.ApiName);
    }
}

// The platform must make unlocks idempotent across app restarts. This wrapper also
// prevents repeated requests during a session, and retries only failed requests.
public sealed class AchievementSession : IAchievementSink
{
    private readonly HashSet<string> _sent = new();
    private readonly IAchievementSink _sink;
    public AchievementSession(IAchievementSink sink) => _sink = sink;
    public void Unlock(string key)
    {
        if (_sent.Contains(key)) return;
        _sink.Unlock(key);
        _sent.Add(key);
    }
    public void AccountChanged() => _sent.Clear();
}
