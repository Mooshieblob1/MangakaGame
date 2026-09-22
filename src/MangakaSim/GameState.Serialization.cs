using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace MangakaSim;

public partial class GameState
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializes the whole state, including the event list, RNG state and command log.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserializes a save. Throws InvalidDataException when the Version is missing
    /// or newer than this build understands.
    /// </summary>
    public static GameState FromJson(string json)
    {
        int version;
        using (var document = JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(nameof(Version), out var element) ||
                element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out version))
                throw new InvalidDataException("Save file has no Version number.");

            if (version == CurrentVersion)
            {
                foreach (var property in new[] { nameof(Clock), nameof(People), nameof(Series), nameof(Events),
                             nameof(Rng), nameof(RngSeed), nameof(Settings), nameof(NextId), nameof(CommandLog),
                             nameof(RecapWindowStart), nameof(RecapFiredToday),
                             nameof(Money), nameof(Ledger), nameof(StudioTrackRecord), nameof(Markets), nameof(Trends),
                             nameof(HasInternet), nameof(LastTrendUpdateMonth), nameof(DoujinCopiesThisMonth),
                             nameof(DoujinFansThisMonth), nameof(LedgerWindowStart) })
                    if (!document.RootElement.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
                        throw new InvalidDataException($"Save file is missing {property}.");
            }
        }

        if (version > CurrentVersion)
            throw new InvalidDataException(
                $"Save file version {version} is newer than the supported version {CurrentVersion}. Update the game to load it.");

        if (version != CurrentVersion)
            throw new InvalidDataException($"Save file version {version} is not supported.");

        var state = JsonSerializer.Deserialize<GameState>(json, JsonOptions)
                    ?? throw new InvalidDataException("Save file is empty.");
        state.ValidateSave();
        return state;
    }

    private void ValidateSave()
    {
        static void Check([DoesNotReturnIf(false)] bool valid, string field)
        {
            if (!valid) throw new InvalidDataException($"Save file has invalid {field}.");
        }

        Check(Clock is not null && Clock.Now.Ticks % TimeSpan.TicksPerHour == 0, "clock");
        Check(People is { Count: 1 } && People[0] is not null, "people (one mangaka is required)");
        Check(Series is not null && Events is not null && Rng is not null && CommandLog is not null, "state");
        Check(Settings?.Balance is not null && Settings.AutoPause is not null, "settings");
        var balance = Settings!.Balance;
        Check(balance.OvertimeCap is >= 0 and <= 24, "overtime cap");
        Check(new[] { balance.MultiplierAtSkill0, balance.MultiplierAtSkill50, balance.MultiplierAtSkill100 }
            .All(x => double.IsFinite(x) && x > 0), "skill multipliers");
        Check(balance.BaseHoursPerPage is not null && StageOrder.All.All(stage =>
            balance.BaseHoursPerPage.TryGetValue(stage, out var value) && double.IsFinite(value) && value > 0), "stage costs");
        Check(RecapWindowStart >= 0 && RecapWindowStart <= Events!.Count, "recap window");

        var ids = new HashSet<int>();
        void CheckId(int id) => Check(id > 0 && ids.Add(id), "unique id");
        var person = People![0];
        CheckId(person.Id);
        Check(person.Name is not null && person.Skills is not null && StageOrder.All.All(stage =>
            person.Skills.TryGetValue(stage, out var skill) && skill is >= 0 and <= 100), "person skills");
        Check(person.Schedule is not null && person.Schedule.DaysOff is not null &&
            person.Schedule.DaysOff.All(Enum.IsDefined) && person.Schedule.WorkStartHour >= 0 &&
            person.Schedule.WorkStartHour < person.Schedule.WorkEndHour &&
            person.Schedule.WorkEndHour <= 24 - balance.OvertimeCap, "schedule");
        Check(person.HoursWorkedToday is >= 0 and <= 24 && person.OvertimeHoursToday >= 0 &&
            person.OvertimeHoursToday <= person.HoursWorkedToday, "work counters");
        Check(person.Queue is not null && person.Pins is not null, "queue");

        foreach (var series in Series!)
        {
            Check(series is not null, "series");
            CheckId(series!.Id);
            Check(!string.IsNullOrWhiteSpace(series.Title) && series.Genre is not null &&
                Enum.IsDefined(series.Cadence) && Enum.IsDefined(series.Status) && series.PagesPerChapter > 0 &&
                series.Chapters is not null, "series details");
            Check(series.StartDate.Ticks % TimeSpan.TicksPerHour == 0, "series start date");
            var number = 1;
            foreach (var chapter in series.Chapters!)
            {
                Check(chapter is not null, "chapter");
                CheckId(chapter!.Id);
                Check(chapter.Number == number++ && Enum.IsDefined(chapter.Status) &&
                    chapter.DueDate.Ticks % TimeSpan.TicksPerHour == 0 && chapter.HoursOverdue >= 0, "chapter details");
                Check(chapter.Stages is { Count: 5 } && chapter.Stages.All(s => s is not null) &&
                    chapter.Stages.Select(s => s.Stage).SequenceEqual(StageOrder.All), "chapter stages");
                foreach (var work in chapter.Stages!)
                    Check(Enum.IsDefined(work.Status) && double.IsFinite(work.HoursRequired) && work.HoursRequired > 0 &&
                        double.IsFinite(work.HoursDone) && work.HoursDone >= 0 && work.HoursDone <= work.HoursRequired &&
                        (work.AssignedTo is null || work.AssignedTo == person.Id), "stage work");
                Check((chapter.Status == ChapterStatus.Complete) == chapter.IsFinished &&
                    (chapter.Status == ChapterStatus.Complete) == chapter.CompletedAt.HasValue, "chapter completion");
            }
        }
        Check(NextId > ids.Max(), "next id");
        bool ValidRef(QueueRef reference) => Enum.IsDefined(reference.Stage) && FindChapter(reference.ChapterId) is not null;
        Check(person.Queue!.All(r => ValidRef(r) && IsStartableOrPending(r, person.Id)) &&
            person.Queue.Distinct().Count() == person.Queue.Count, "queue references");
        Check(person.Pins!.All(ValidRef) && person.Pins.Distinct().Count() == person.Pins.Count, "pins");
        Check(person.ManualOrder is null || person.ManualOrder.All(ValidRef), "manual queue");
        Check(person.CurrentTask is null || (person.Queue.Contains(person.CurrentTask.Value) && IsStartable(person.CurrentTask.Value)), "current task");
        Check(Events!.All(e => e is not null && Enum.IsDefined(e.Type) && e.Message is not null), "events");
        foreach (var ev in Events.Where(e => e.Type == EventType.DailyRecap))
        {
            var recap = ev.Recap;
            Check(recap is not null && recap.HoursPerPerson is not null && recap.HoursPerPerson.All(p => p is not null) &&
                recap.StagesStarted is not null && recap.StagesStarted.All(s => s is not null) &&
                recap.StagesCompleted is not null && recap.StagesCompleted.All(s => s is not null) &&
                recap.ChaptersCompleted is not null && recap.ChaptersCompleted.All(c => c is not null) &&
                recap.ChaptersAtRisk is not null && recap.ChaptersAtRisk.All(c => c is not null) &&
                recap.DeadlinesMissed is not null && recap.DeadlinesMissed.All(c => c is not null), "daily recap");
        }
        Check(CommandLog!.All(e => e is not null && e.Command is not null &&
            e.Time.Ticks % TimeSpan.TicksPerHour == 0 && e.Time <= Clock!.Now), "command log");

        ValidatePublishingState(Check, CheckId, ids);
    }

    private bool IsStartableOrPending(QueueRef reference, int personId)
    {
        var chapter = FindChapter(reference.ChapterId)!;
        var work = chapter.StageWork(reference.Stage);
        return SeriesOf(chapter).Status == SeriesStatus.Active && !work.IsDone && work.AssignedTo == personId;
    }

    private void ValidatePublishingState(Action<bool, string> check, Action<int> checkId, HashSet<int> ids)
    {
        check(Ledger is not null && Ledger.All(l => l is not null && l.Reason is not null &&
            l.Time.Ticks % TimeSpan.TicksPerHour == 0 && l.Time <= Clock.Now &&
            (l.SeriesId is null || FindSeries(l.SeriesId.Value) is not null)), "ledger");
        check(Money == Economy.StartingMoney + Ledger!.Sum(l => l.Amount), "money (must equal the ledger sum)");
        check(LedgerWindowStart >= 0 && LedgerWindowStart <= Ledger!.Count, "ledger window");
        check(StudioTrackRecord is >= 0 and <= 100 && double.IsFinite(StudioTrackRecord), "studio track record");
        check(LastTrendUpdateMonth.Day == 1 && LastTrendUpdateMonth.TimeOfDay == TimeSpan.Zero, "trend update month");
        check(DoujinCopiesThisMonth >= 0 && DoujinFansThisMonth >= 0 && double.IsFinite(DoujinFansThisMonth), "doujin month counters");

        check(Markets is not null && Markets.Count == Publishers.Magazines.Count &&
            Markets.Select(m => m?.MagazineId).SequenceEqual(Publishers.Magazines.Select(m => m.Id)), "markets (one per catalog magazine)");
        foreach (var market in Markets!)
        {
            var magazine = Publishers.Require(market.MagazineId);
            check(market.NextIssueClose.Ticks % TimeSpan.TicksPerHour == 0 && market.NextIssueClose >= Clock.Now &&
                market.NextIssueClose.DayOfWeek == magazine.IssueCloseDay && market.NextIssueClose.Hour == magazine.IssueCloseHour, "next issue close");
            check(market.LastIssueClose is null || market.LastIssueClose < market.NextIssueClose, "last issue close");
            check(market.IssuesClosed >= 0 && market.Fillers is not null && market.LastRanking is not null, "market state");
            var fillerIds = new HashSet<int>();
            foreach (var filler in market.Fillers!)
            {
                check(filler is not null && filler.Id >= 1 && filler.Id < market.NextFillerId && fillerIds.Add(filler.Id), "filler id");
                check(!string.IsNullOrWhiteSpace(filler!.Title) && TrendData.HasGenre(filler.Genre) &&
                    filler.Popularity >= FillerRules.MinPopularity && filler.Popularity <= FillerRules.MaxPopularity &&
                    filler.IssuesBelowLine >= 0, "filler details");
            }
            check(market.Fillers.Count(f => f.IsIconic) <= 1, "iconic fillers");
            var rank = 1;
            foreach (var entry in market.LastRanking!)
            {
                check(entry is not null && entry.Rank == rank++ && entry.Title is not null && double.IsFinite(entry.Score) &&
                    (entry.SeriesId is null) != (entry.FillerId is null), "rank entry");
                check(entry!.SeriesId is null ? market.Fillers.Any(f => f.Id == entry.FillerId) : FindSeries(entry.SeriesId.Value) is not null,
                    "rank entry target");
            }
        }

        check(Trends is not null && Trends.Select(t => t?.Genre).OrderBy(g => g).SequenceEqual(TrendData.Genres.OrderBy(g => g)) &&
            Trends.Count == TrendData.Genres.Count, "trends (one per catalog genre)");
        foreach (var trend in Trends!)
        {
            check(Math.Abs(trend.Noise) <= TrendRules.MaxNoise + 1e-9 && trend.Boom is >= 0 and <= 1.0 &&
                trend.PlayerInfluence >= 0 && trend.PlayerInfluence <= TrendRules.MaxPlayerInfluence + 1e-9 &&
                trend.BoomPeak is >= 0 and <= 1.0 && trend.BoomFloor is >= 0 and <= 1.0, "trend values");
            check((trend.BoomEndsAt is null) == (trend.BoomFadeEndsAt is null) &&
                (trend.BoomEndsAt is null || trend.BoomFadeEndsAt > trend.BoomEndsAt) &&
                (!trend.BoomFading || trend.BoomEndsAt is not null), "boom fields");
        }

        foreach (var person in People)
            check(person.Reputation is >= 0 and <= 100 && double.IsFinite(person.Reputation), "person reputation");

        foreach (var series in Series)
        {
            check(Enum.IsDefined(series.Publishing) && series.Fanbase >= 0 && double.IsFinite(series.Fanbase) &&
                series.CulturalImpact is >= 0 and <= 100 && series.ChaptersPublished >= 0 && series.WeeksBelowLine >= 0 &&
                series.Strikes is not null && series.PitchCooldowns is not null && series.Volumes is not null, "series publishing details");
            check(!series.IsIconic || series.CulturalImpact >= FanbaseRules.IconicImpact, "iconic series impact");
            check(series.Strikes!.All(s => s <= Clock.Now), "strikes in the future");
            check(series.WarningIssuedAt is null || series.WarningIssuedAt <= Clock.Now, "warning time");
            check(series.PitchCooldowns!.Keys.All(id => Publishers.Find(id) is not null), "cooldown magazine ids");
            switch (series.Publishing)
            {
                case PublishingStatus.Serialized:
                    check(series.Contract is not null && series.PendingOffer is null && series.Status != SeriesStatus.Ended, "serialized series must hold a contract and no offer");
                    break;
                case PublishingStatus.Offered:
                    check(series.PendingOffer is not null && series.Contract is null, "offered series must hold an offer and no contract");
                    break;
                case PublishingStatus.Pitching:
                    check(series.Contract is null && series.PendingOffer is null &&
                        series.Chapters.Any(c => c.IsOneShot && c.PitchMagazineId is not null && Publishers.Find(c.PitchMagazineId) is not null),
                        "pitching series must have a pitched one-shot");
                    break;
                default:
                    check(series.Contract is null && series.PendingOffer is null, "unpublished series must hold no contract or offer");
                    break;
            }
            check(series.Status != SeriesStatus.Ended || series.Publishing == PublishingStatus.Unpublished, "ended series must be unpublished");
            check(series.Contract is null || (Publishers.Find(series.Contract.MagazineId) is not null && series.Contract.FeePerPage > 0 &&
                series.Contract.ChaptersPublished >= 0 && series.Contract.ChaptersPublished <= series.ChaptersPublished), "contract");
            check(series.PendingOffer is null || (Publishers.Find(series.PendingOffer.MagazineId) is not null && series.PendingOffer.FeePerPage > 0 &&
                series.PendingOffer.ExpiresAt >= Clock.Now), "pending offer");

            foreach (var chapter in series.Chapters)
            {
                check(Enum.IsDefined(chapter.Editor) && chapter.RedoCount >= 0 && (chapter.Rank is null || chapter.Rank >= 1), "chapter publishing details");
                check(chapter.Editor == EditorStatus.NotRequired || series.IsSerialized || chapter.IsOneShot ||
                    chapter.IsPublished, "editor status on a doujin chapter");
                check(chapter.Editor != EditorStatus.AwaitingReview ||
                    (chapter.StageWork(Stage.Name).IsDone && chapter.StageWork(Stage.Pencils).Status == StageStatus.NotStarted &&
                     chapter.EditorDecisionAt is not null), "awaiting review state");
                check((chapter.Quality is not null) == (chapter.Status == ChapterStatus.Complete) &&
                    (chapter.Quality is null || chapter.Quality is >= 0 and <= 100), "chapter quality");
                check(!chapter.IsOneShot || chapter.PitchMagazineId is null || Publishers.Find(chapter.PitchMagazineId) is not null, "one-shot magazine");
                check(!chapter.IsPublished || chapter.Status == ChapterStatus.Complete, "published chapter must be complete");
                check(chapter.Stages.All(w => w.OvertimeHours >= 0 && double.IsFinite(w.Contribution) && w.Contribution >= 0), "stage quality data");
            }

            var number = 0;
            var lastChapter = 0;
            foreach (var volume in series.Volumes!)
            {
                check(volume is not null, "volume");
                checkId(volume!.Id);
                check(volume.Number == ++number && Enum.IsDefined(volume.Format) && volume.FirstChapter >= 1 &&
                    volume.LastChapter >= volume.FirstChapter && volume.FirstChapter > lastChapter &&
                    volume.LastChapter <= series.Chapters.Count, "volume chapter range");
                lastChapter = volume.LastChapter;
                check(volume.CopiesSold >= 0 && volume.WeeksOnSale >= 0 &&
                    volume.WeeksOnSale <= (volume.IsDoujin ? SalesRules.DoujinWindow(true) : SalesRules.TankobonWeeks) &&
                    volume.AverageQuality is >= 0 and <= 100 && volume.ReleaseDate.Ticks % TimeSpan.TicksPerHour == 0 &&
                    (!volume.IsReleased || volume.ReleaseDate <= Clock.Now) && (volume.IsReleased || volume.WeeksOnSale == 0), "volume sales data");
            }
        }
        check(NextId > ids.Max(), "next id");
    }
}
