using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    [JsonRequired] public ProgressionState Progression { get; set; } = new();
    private const long SandboxFundsLimit = 1_000_000_000_000;
    public bool Assist(SandboxAssist assist) => (Progression.Assists & assist) != 0;
    public bool EquipmentAvailable(string kind) => kind switch
    {
        "desk-studio" => Assist(SandboxAssist.UnlockEquipment) || Goals?.Unlocked.Contains("desk-studio") == true || ControlledBusiness.TrackRecord >= 20,
        "desk-digital" => (Assist(SandboxAssist.FutureTechnology) || World.Processed.Contains("industry:mobile")) &&
            (Assist(SandboxAssist.UnlockEquipment) || ControlledBusiness.TrackRecord >= 40),
        _ => true
    };
    private bool ManagedBusiness(int id) => id == ControlledBusinessId;
    public long RecruitmentFee => DiscretionaryCost(20000, ControlledBusinessId, "recruitment");
    public long ChannelSetupFee(ReleaseChannel channel) => DiscretionaryCost(channel == ReleaseChannel.DomesticDigital ? 30000 : 100000,
        ControlledBusinessId, channel == ReleaseChannel.DomesticDigital ? "digital edition setup" : "overseas localization");
    private bool Unlimited(CashAccount account) =>
        Assist(SandboxAssist.PersonalFunds) && ReferenceEquals(account, Protagonist.PersonalAccount) ||
        Assist(SandboxAssist.BusinessFunds) && ReferenceEquals(account, ControlledBusiness.Account);
    private long Spendable(CashAccount account) => Unlimited(account) ? SandboxFundsLimit : account.Balance;
    private void Subsidize(CashAccount account, long needed)
    {
        if (needed <= account.Balance || !Unlimited(account)) return;
        if (needed > SandboxFundsLimit) throw new InvalidCommandException("That amount exceeds the supported transaction limit.");
        var grant = needed - account.Balance;
        account.Balance += grant;
        account.Entries.Add(new(Clock.Now, grant, "Sandbox subsidy", null, AccountEntryKind.SandboxSubsidy));
    }
    private void InitializeProgression(bool imported = false)
    {
        Progression = new()
        {
            AvailableFrom = Clock.Now,
            AwardsRng = Rng.FromSeed(unchecked(RngSeed ^ 0x41574152)),
            LicensingRng = Rng.FromSeed(unchecked(RngSeed ^ 0x414e494d)),
            VerifiedOrigin = !imported || JsonSerializer.Serialize(Settings.Balance) == JsonSerializer.Serialize(new Balance())
        };
    }
    private void ApplyDifficulty(DifficultyCommand command)
    {
        if (!Enum.IsDefined(command.Mode) || ((int)command.Assists & ~511) != 0 ||
            command.Pressure is < 0 or > 2 || command.Recovery is < 0 or > 2 || command.Cushion is < 0 or > 2)
            throw new InvalidCommandException("Choose supported difficulty options.");
        var p = Progression;
        // Opening resources may change only in the first setup command, before any work or spending.
        bool opening = Clock.Now == GameClock.Start && !CommandLog.Any(c => c.Command is not SetAppearanceCommand) &&
            p.Changes.Count == 0 && Series.Count == 0;
        if (opening)
        {
            double factor = command.Mode == CareerDifficulty.Relaxed ? 2 : command.Mode == CareerDifficulty.Challenging ? .5 :
                command.Mode == CareerDifficulty.Custom ? new[] { .5, 1, 2 }[command.Cushion] : 1;
            long personal = (long)(200000 * (factor - 1)), business = (long)(300000 * (factor - 1));
            if (personal != 0) AccountPost(Protagonist.PersonalAccount, personal, "Difficulty opening allocation", AccountEntryKind.Credit);
            if (business != 0) AccountPost(ControlledBusiness.Account, business, "Difficulty opening allocation", AccountEntryKind.Credit);
        }
        p.Difficulty = command.Mode;
        p.Assists = command.Assists;
        p.Pressure = command.Mode == CareerDifficulty.Relaxed ? 0 : command.Mode == CareerDifficulty.Challenging ? 2 : command.Mode == CareerDifficulty.Custom ? command.Pressure : 1;
        p.Recovery = command.Mode == CareerDifficulty.Relaxed ? 2 : command.Mode == CareerDifficulty.Challenging ? 0 : command.Mode == CareerDifficulty.Custom ? command.Recovery : 1;
        if (command.Mode == CareerDifficulty.Sandbox || command.Assists != SandboxAssist.None)
        {
            p.EverSandbox = true;
            p.SandboxSince ??= Clock.Now;
            p.Achievements.Clear(); // Unsynchronized evidence cannot escape after activation.
        }
        p.Changes.Add(new(Clock.Now, p.Difficulty, p.Assists, p.Pressure, p.Recovery));
    }
    private int RecoveryDays(int days, int business) => ManagedBusiness(business) ?
        Math.Max(1, (int)Math.Ceiling(days * DifficultyRules.RecoveryFactor(Progression.Recovery))) : days;
    private long DiscretionaryCost(long amount, int business, string reason) => ManagedBusiness(business) &&
        reason is "recruitment" or "digital edition setup" or "overseas localization" or "advertising" ?
        (long)Math.Ceiling(amount * DifficultyRules.CostFactor(Progression.Pressure)) : amount;
    /// <summary>Editor patience for this series: recovery grace lengthens or shortens the warning and cancellation waits.</summary>
    public (int Warning, int Cancel, int StrikeLifetime) CancellationClocks(Series series)
    {
        var clocks = CancellationRules.Clocks(Protection(series));
        return ManagedBusiness(series.BusinessId) ? DifficultyRules.Clocks(clocks, Progression.Recovery) : clocks;
    }
    public int GraceChapters(Series series) => ManagedBusiness(series.BusinessId) ? DifficultyRules.GraceChapters(Progression.Recovery) : 6;
    public double PitchFactor(int business) => ManagedBusiness(business) ? DifficultyRules.PitchFactor(Progression.Pressure) : 1;
    // Fillers are the magazines' other series, so business pressure sets how hard they are to outrank.
    private double RivalStrength => DifficultyRules.RivalStrength(Progression.Pressure);
    private bool DeadlineProtected(Series series) => ManagedBusiness(series.BusinessId) && Assist(SandboxAssist.NoDeadlinePenalties);
    public double RecognitionLift(int series)
    {
        var effects = Progression.Effects.Where(e => e.SeriesId == series && e.Start <= Clock.Now && e.End > Clock.Now).ToArray();
        var awards = Progression.Awards.Select(a => a.Id).ToHashSet();
        return 1 + Math.Min(.5, Math.Min(.25, effects.Where(e => awards.Contains(e.SourceId)).Sum(e => e.Lift)) +
            effects.Where(e => !awards.Contains(e.SourceId)).Sum(e => e.Lift));
    }
    private void RecognitionImpact(Series series, double amount)
    {
        var key = $"{series.Id}:{Clock.Now.Year}";
        var earned = Progression.AnnualImpact.GetValueOrDefault(key);
        var gain = Math.Min(Math.Max(0, 10 - earned), amount);
        Progression.AnnualImpact[key] = earned + gain;
        series.CulturalImpact = Math.Min(100, series.CulturalImpact + gain);
        CheckIconic(series);
    }
    private void MarkMilestone(string key, int entity, string text, bool achievement = true)
    {
        bool first = !Progression.Milestones.Any(m => m.Key == key && m.Entity == entity);
        if (first) Progression.Milestones.Add(new(key, entity, Clock.Now, text));
        if (achievement && (entity == ProtagonistPersonId || Series.Any(s => s.Id == entity && s.RightsLeadPersonId == ProtagonistPersonId)) &&
            AchievementDelivery.Eligible(this) && !Progression.Achievements.Any(a => a.Key == key))
            Progression.Achievements.Add(new(key, Clock.Now));
        if (first && (entity == ProtagonistPersonId || Series.Any(s => s.Id == entity && s.LeadPersonId == ProtagonistPersonId)))
            Emit(EventType.CareerMilestone, text, personId: ProtagonistPersonId);
    }
    private void ProgressionStep()
    {
        if (Progression.LastDay == Clock.Now.Date) return;
        Progression.LastDay = Clock.Now.Date;
        ResolveAwards();
        LicensingStep();
        foreach (var s in Series.Where(s => s.LeadPersonId == ProtagonistPersonId))
        {
            if (s.Chapters.Any(c => c.PublishedAt is not null) || s.Volumes.Any(v => v.ReleasedAt is not null))
                MarkMilestone("first_publication", ProtagonistPersonId, "Helper-Chan: Your first publication belongs in the archive.");
            if (s.Fanbase >= 10000) MarkMilestone("readers_10000", s.Id, $"{s.Title} has reached 10,000 readers.");
            if (s.Fanbase >= 1000000) MarkMilestone("readers_million", s.Id, $"A million readers for {s.Title}.");
            if (s.IsIconic) MarkMilestone("iconic_series", s.Id, $"{s.Title} is part of manga history.");
        }
        var cutoff = Clock.Now.AddDays(-365);
        if (Clock.Now >= Progression.AvailableFrom.AddDays(365) && Clock.Now >= ControlledBusiness.FoundedAt.AddDays(365) &&
            ControlledBusiness.Account.Entries.Where(e => e.Time >= cutoff && (e.Kind is AccountEntryKind.Publishing or AccountEntryKind.LicenseIncome or AccountEntryKind.Expense or AccountEntryKind.Salary ||
                e.Kind == AccountEntryKind.Transfer && e.Reason.EndsWith("creator share", StringComparison.Ordinal))).Sum(e => e.Amount) > 1000000 &&
            !Bills.Any(b => b.BusinessId == ControlledBusinessId && b.Remaining > 0 && b.DueAt <= Clock.Now) && WageArrears == 0)
            MarkMilestone("sustainable_studio", ProtagonistPersonId, "A profitable year, with obligations paid. A studio worth celebrating.");
    }
    private void SandboxProductionStep()
    {
        if (!Assist(SandboxAssist.InstantProduction)) return;
        // Snapshot prevents normal planner completion hooks from creating an infinite chapter loop.
        var chapters = Series.Where(s => s.BusinessId == ControlledBusinessId && s.Status == SeriesStatus.Active &&
            (Control == ControlMode.OwnerDirector || s.LeadPersonId == ProtagonistPersonId))
            .SelectMany(s => s.Chapters.Where(c => c.Status != ChapterStatus.Complete)).ToArray();
        foreach (var chapter in chapters)
        {
            var series = SeriesOf(chapter);
            foreach (var work in chapter.Stages.Where(w => !w.IsDone))
            {
                if (work.Stage != Stage.Name && chapter.EditorMagazineId is not null && chapter.Editor != EditorStatus.Approved) break;
                var person = work.AssignedTo is {} id ? FindPerson(id) : null;
                if (person is null || !CanProduce(person)) break;
                chapter.CreatorPersonId ??= series.LeadPersonId;
                work.QualityWeightedWork += (work.HoursRequired - work.HoursDone) * QualityRules.SkillFactor(person.Skill(work.Stage));
                work.HoursDone = work.HoursRequired;
                work.SandboxCompleted = true;
                work.Contribution = QualityRules.Weight(work.Stage) * 100 * Math.Min(1, work.QualityWeightedWork / work.HoursRequired);
                work.Status = StageStatus.Complete;
                SubmitNameIfReady(chapter);
            }
            CompleteChapterIfDone(chapter);
        }
    }
    public static GameState ImportProgressionV6(string json)
    {
        try
        {
            var node = JsonNode.Parse(json)!.AsObject();
            if (node[nameof(Version)]?.GetValue<int>() != 6) throw new InvalidDataException("Choose a version-6 career.");
            try { FromJson(json); }
            catch (InvalidDataException ex) when (ex.Message.StartsWith("Save file version 6 is not supported.", StringComparison.Ordinal)) { }
            node[nameof(Progression)] = JsonSerializer.SerializeToNode(new ProgressionState(), JsonOptions);
            var state = JsonSerializer.Deserialize<GameState>(node.ToJsonString(), JsonOptions)!;
            state.ValidateSave();
            state.Version = CurrentVersion;
            state.InitializeProgression(true);
            state.World.ReplayCheckpoint = null;
            state.World.ReplayLogStart = state.CommandLog.Count;
            state.ValidateSave();
            state.World.ReplayCheckpoint = state.ToJson();
            return state;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException or NullReferenceException)
        { throw new InvalidDataException("Could not import this career: " + ex.Message, ex); }
    }
}
