using System.Diagnostics.CodeAnalysis;
using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void ValidateMarketSave(HashSet<int> ids)
    {
        static void Check([DoesNotReturnIf(false)] bool valid, string field)
        {
            if (!valid) throw new InvalidDataException($"Save file has invalid {field}.");
        }
        static bool Range(double value, double min, double max) => double.IsFinite(value) && value >= min && value <= max;
        bool Time(DateTime value) => value.Ticks % TimeSpan.TicksPerHour == 0 && value >= GameClock.Start && value <= Clock.Now;
        bool Magazine(string? id) => id is not null && PublisherCatalog.Magazines.Any(m => m.Id == id);
        void Id(int value) => Check(value > 0 && ids.Add(value), "unique market id");
        void Hours(Dictionary<int, long>? hours) => Check(hours is not null && hours.All(p => People.Any(x => x.Id == p.Key) && p.Value > 0), "contributor hours");

        Check(Range(StudioTrackRecord, 0, 100) && People.All(p => Range(p.Reputation, 0, 100)), "reputation");
        Check(Ledger is not null && Money >= 0, "ledger");
        var total = ControlledBusiness.Account.OpeningBalance;
        foreach (var entry in Ledger)
        {
            Check(entry is not null && Time(entry.Time) && (entry.SeriesId is null || FindSeries(entry.SeriesId.Value) is not null), "ledger reference");
            Check(!string.IsNullOrWhiteSpace(entry.Reason), "ledger reason");
            Check(entry.Kind != AccountEntryKind.Expense || entry.Amount < 0, "ledger amount");
            try { total = checked(total + entry.Amount); } catch (OverflowException ex) { throw new InvalidDataException("Ledger overflow.", ex); }
            Check(total >= 0, "ledger balance");
        }
        Check(total == Money && Ledger.Zip(Ledger.Skip(1)).All(p => p.First.Time <= p.Second.Time), "ledger reconciliation");
        Check(!HasInternet || Ledger.Any(e => e.Reason == "internet"), "internet purchase");
        Check(DoujinCopiesThisMonth >= 0 && Range(DoujinFansThisMonth, 0, double.MaxValue), "convention totals");
        Check(LastSalesAt is null || (Time(LastSalesAt.Value) && LastSalesAt.Value.Hour == 0 && LastSalesAt.Value.DayOfWeek == DayOfWeek.Monday), "sales timestamp");
        Check(LastTrendUpdateMonth is null || (LastTrendUpdateMonth.Value == new DateTime(LastTrendUpdateMonth.Value.Year, LastTrendUpdateMonth.Value.Month, 1) &&
            LastTrendUpdateMonth.Value <= Clock.Now && LastTrendUpdateMonth.Value >= GameClock.Start.Date), "trend month");
        Check(Markets is not null && Markets.All(m => m is not null) && Markets.Count == PublisherCatalog.Magazines.Count &&
            Markets.Select(m => m.MagazineId).SequenceEqual(PublisherCatalog.Magazines.Select(m => m.Id)), "market catalog order");
        foreach (var market in Markets)
        {
            var magazine = PublisherCatalog.Get(market.MagazineId);
            Check(market.IssuesClosed >= 0 && market.NextIssueClose >= Clock.Now && market.NextIssueClose.Ticks % TimeSpan.TicksPerHour == 0, "issue calendar");
            Check((market.NextIssueClose - IssueSchedule.Anchor(magazine)).TotalDays == (long)market.IssuesClosed * IssueSchedule.Days(magazine.Cadence), "issue alignment");
            Check(market.Fillers is not null && market.RetiredFillers is not null && market.LastRanking is not null &&
                market.Fillers.Count == magazine.RosterSize - 1, "filler roster");
            foreach (var filler in market.Fillers.Concat(market.RetiredFillers))
            {
                Check(filler is not null && !string.IsNullOrWhiteSpace(filler.Title) && TrendCatalog.Genres.Contains(filler.Genre) &&
                    Range(filler.Popularity, 5, 100) && filler.IssuesBelowLine >= 0, "filler");
                Id(filler.Id);
            }
            var rank = 1;
            var targets = new HashSet<string>();
            foreach (var row in market.LastRanking)
            {
                Check(row is not null && row.Rank == rank++ && !string.IsNullOrWhiteSpace(row.Title) && Range(row.Score, 0, double.MaxValue) &&
                    (row.SeriesId is not null) != (row.FillerId is not null), "ranking");
                Check(row.FillerId is { } fillerId ? market.Fillers.Concat(market.RetiredFillers).Any(f => f.Id == fillerId) :
                    FindSeries(row.SeriesId!.Value) is not null, "ranking target");
                Check(targets.Add($"{row.SeriesId}:{row.FillerId}"), "duplicate ranking row");
            }
            Check(market.IssuesClosed == 0 ? market.LastRanking.Count == 0 : market.LastRanking.Count >= magazine.RosterSize - 1, "ranking size");
        }
        Check(Trends is not null && Trends.All(t => t is not null) && Trends.Select(t => t.Genre).SequenceEqual(TrendCatalog.Genres), "trend catalog order");
        foreach (var trend in Trends)
        {
            Check(Range(trend.Noise, -.15, .15) && Range(trend.Boom, 0, 1) && Range(trend.PermanentBoom, 0, 1) &&
                Range(trend.BoomPeak, 0, .5) && Range(trend.BoomFloor, 0, .25) && Range(trend.PlayerInfluence, 0, .5), "trend bounds");
            Check(trend.Genre != "other" || (trend.Noise == 0 && trend.Boom == 0 && trend.BoomEndsAt is null), "other trend");
            Check(trend.BoomEndsAt.HasValue == trend.BoomFadeEndsAt.HasValue &&
                (trend.BoomEndsAt is null ? trend.BoomPeak == 0 && trend.BoomFloor == 0 && !trend.BoomFloorChosen && trend.Boom == trend.PermanentBoom :
                trend.BoomPeak >= .3 && trend.BoomEndsAt.Value.Year < 9999 && trend.BoomFadeEndsAt == trend.BoomEndsAt.Value.AddYears(1) &&
                trend.BoomEndsAt.Value > GameClock.Start && trend.BoomEndsAt.Value.Ticks % TimeSpan.TicksPerHour == 0), "boom phase");
        }
        foreach (var series in Series)
        {
            Check(Enum.IsDefined(series.Publishing) && Enum.IsDefined(series.DoujinCadence) &&
                Range(series.Fanbase, 0, double.MaxValue) && Range(series.CulturalImpact, 0, 100) &&
                (!series.IsIconic || series.CulturalImpact >= 90 && series.Fanbase >= 1000000) && series.ChaptersPublished >= 0 &&
                series.NextChapterNumber > series.Chapters.Select(c => c.Number).DefaultIfEmpty(0).Max(), "publishing details");
            Check(series.PastContracts is not null && series.Volumes is not null && series.Strikes is not null && series.PitchCooldowns is not null, "publishing collections");
            Hours(series.LifetimeHoursByPerson);
            Check(series.Status != SeriesStatus.Ended || series.Publishing == PublishingStatus.Unpublished, "ended publishing status");
            Check((series.Publishing == PublishingStatus.Serialized) == (series.Contract is not null) &&
                (series.Publishing == PublishingStatus.Offered) == (series.PendingOffer is not null), "contract/offer exclusivity");
            Check(series.Chapters.Count(c => c.Status != ChapterStatus.Complete) <= 3, "open chapters");
            Check(series.Chapters.Count(c => c.IsOneShot && !c.PitchResolved) == (series.Publishing == PublishingStatus.Pitching ? 1 : 0), "unresolved pitch");
            Check(series.Strikes.All(Time) && series.Strikes.SequenceEqual(series.Strikes.Order()) && series.WeeksBelowLine >= 0 &&
                (series.WarningIssuedAt is null || Time(series.WarningIssuedAt.Value)) &&
                series.PitchCooldowns.All(p => Magazine(p.Key) && p.Value.Ticks % TimeSpan.TicksPerHour == 0), "cancellation state");
            Check(series.Publishing == PublishingStatus.Serialized && !series.IsIconic ||
                (series.Strikes.Count == 0 && series.WeeksBelowLine == 0 && series.WarningIssuedAt is null), "inactive cancellation state");
            var contracts = series.PastContracts.Concat(series.Contract is { } contract ? new[] { contract } : Array.Empty<Contract>()).ToArray();
            foreach (var past in contracts)
            {
                Check(past is not null && Magazine(past.MagazineId) && past.FeePerPage > 0 && Time(past.SignedAt) &&
                    past.FirstIssueClose > past.SignedAt && past.ChaptersPublished >= 0, "contract");
                Id(past.Id);
                Check(IssueSchedule.FirstCloseAtOrAfter(PublisherCatalog.Get(past.MagazineId), past.FirstIssueClose) == past.FirstIssueClose &&
                    series.Chapters.Count(c => c.PublishedContractId == past.Id) == past.ChaptersPublished, "contract publications");
            }
            Check(contracts.Sum(c => c.ChaptersPublished) == series.ChaptersPublished, "lifetime publications");
            if (series.Contract is { } current)
            {
                var magazine = PublisherCatalog.Get(current.MagazineId);
                var waiting = series.Chapters.Where(c => c.MagazineBound && c.PublishedAt is null).ToArray();
                Check(series.Cadence == magazine.Cadence && waiting.Select(c => c.DueDate).Distinct().Count() == waiting.Length &&
                    waiting.All(c => c.DueDate >= current.FirstIssueClose && c.DueDate >= Clock.Now &&
                        IssueSchedule.FirstCloseAtOrAfter(magazine, c.DueDate) == c.DueDate), "publication slots");
            }
            if (series.PendingOffer is { } offer)
            {
                Check(Magazine(offer.MagazineId) && offer.FeePerPage > 0 && offer.ExpiresAt == offer.FirstIssueClose && offer.ExpiresAt > Clock.Now, "offer");
                Check(IssueSchedule.FirstCloseAtOrAfter(PublisherCatalog.Get(offer.MagazineId), offer.FirstIssueClose) == offer.FirstIssueClose &&
                    series.Chapters.Any(c => c.IsOneShot && c.PitchResolved && !c.DoujinEligible && c.PitchMagazineId == offer.MagazineId && c.Status == ChapterStatus.Complete), "offer sample");
            }
            foreach (var chapter in series.Chapters)
            {
                Check(chapter.Pages > 0 && Enum.IsDefined(chapter.Editor) && chapter.RedoCount is >= 0 and <= 2 &&
                    (chapter.Status == ChapterStatus.Complete) == chapter.Quality.HasValue && (chapter.Quality is null or >= 0 and <= 100), "chapter quality");
                Check(chapter.CompletedAt is null || Time(chapter.CompletedAt.Value), "completion time");
                Check(chapter.Quality is null || chapter.Quality == QualityRules.Total(chapter.Stages.Select(w => w.Contribution)), "frozen chapter quality");
                Check(chapter.EditorMagazineId is null || Magazine(chapter.EditorMagazineId), "editor magazine");
                Check(chapter.IsOneShot ? Magazine(chapter.PitchMagazineId) && chapter.Pages == 31 : chapter.PitchMagazineId is null && !chapter.PitchResolved, "pitch sample");
                Check(chapter.Editor == EditorStatus.NotRequired || chapter.EditorMagazineId is not null, "editor identity");
                Check(chapter.EditorDecisionAt.HasValue == (chapter.Editor == EditorStatus.AwaitingReview), "review timestamp");
                if (chapter.Editor == EditorStatus.AwaitingReview)
                    Check(chapter.StageWork(Stage.Name).IsDone && chapter.EditorDecisionAt > Clock.Now &&
                        chapter.EditorDecisionAt.Value.Ticks % TimeSpan.TicksPerHour == 0 &&
                        chapter.Stages.Where(w => w.Stage != Stage.Name).All(w => w.HoursDone == 0), "editor gate");
                if (chapter.Editor == EditorStatus.RedoRequested)
                    Check(chapter.RedoCount > 0 && !chapter.StageWork(Stage.Name).IsDone, "editor redo");
                if (chapter.Status != ChapterStatus.Complete && chapter.IsFinished)
                    Check(chapter.EditorMagazineId is not null && chapter.Editor != EditorStatus.Approved, "pending completion gate");
                Check(chapter.PublishedAt.HasValue == chapter.PublishedContractId.HasValue && chapter.PublishedAt.HasValue == (chapter.PublishedMagazineId is not null) &&
                    chapter.PublishedAt.HasValue == chapter.Rank.HasValue, "publication identity");
                if (chapter.PublishedAt is { } published)
                    Check(Time(published) && chapter.Status == ChapterStatus.Complete && chapter.Editor == EditorStatus.Approved &&
                        chapter.CompletedAt <= published && chapter.EditorMagazineId == chapter.PublishedMagazineId &&
                        !chapter.IsOneShot && !chapter.DoujinEligible && chapter.Rank > 0 &&
                        contracts.Any(c => c.Id == chapter.PublishedContractId && c.MagazineId == chapter.PublishedMagazineId), "published chapter");
                foreach (var work in chapter.Stages)
                {
                    Hours(work.HoursByPerson);
                    Check(Range(work.OvertimeHours, 0, work.HoursByPerson.Values.Sum()) && Range(work.Contribution, 0, QualityRules.Weight(work.Stage) * 100) &&
                        (work.Status == StageStatus.Complete || work.Contribution == 0) &&
                        (work.HoursDone == 0 || work.HoursByPerson.Count > 0 || work.SandboxCompleted) &&
                        (!work.SandboxCompleted || Version >= 7 && Progression.EverSandbox && work.Status == StageStatus.Complete), "stage contribution");
                }
            }
            var members = new HashSet<int>();
            var issueMembers=new HashSet<int>();
            var number = 1;
            foreach (var volume in series.Volumes)
            {
                Check(volume is not null && volume.Number == number++ && Enum.IsDefined(volume.Format) &&
                    volume.ChapterIds is { Count: > 0 } && volume.CopiesSold >= 0 && Range(volume.AverageQuality, 0, 100), "volume details");
                Id(volume.Id);
                Check(volume.EditionNumber>=0&&(volume.Format!=VolumeFormat.DoujinIssue||volume.IsDoujin&&volume.ChapterIds.Count==1),"issue format");
                Check(volume.ChapterIds.All(id => (volume.Format==VolumeFormat.DoujinIssue?issueMembers:members).Add(id) && series.Chapters.Any(c => c.Id == id && c.Status == ChapterStatus.Complete)), "volume membership");
                var chapters = volume.ChapterIds.Select(id => series.Chapters.Single(c => c.Id == id)).ToArray();
                Check(volume.FirstChapter == chapters.Min(c => c.Number) && volume.LastChapter == chapters.Max(c => c.Number) &&
                    Math.Abs(volume.AverageQuality - chapters.Average(c => c.Quality!.Value)) < 1e-9 &&
                    chapters.All(c => volume.IsDoujin ? c.DoujinEligible && c.PublishedAt is null : c.PublishedAt is not null), "volume chapters");
                Check(volume.IsDoujin ? volume.SalesWindowWeeks is 4 or 8 : volume.SalesWindowWeeks == 52, "sales window");
                Check(volume.WeeksOnSale >= 0 && volume.WeeksOnSale <= volume.SalesWindowWeeks &&
                    volume.SalesClosed == (volume.WeeksOnSale == volume.SalesWindowWeeks) &&
                    volume.ReleaseDate.Ticks % TimeSpan.TicksPerHour == 0 && volume.ReleaseDate >= GameClock.Start &&
                    (volume.ReleasedAt is { } released ? Time(released) && released >= volume.ReleaseDate :
                    (volume.IsDoujin || volume.ReleaseDate > Clock.Now) && volume.WeeksOnSale == 0 && volume.CopiesSold == 0), "volume release");
            }
            foreach (var person in People)
                Check(series.Chapters.SelectMany(c => c.Stages).Sum(w => w.HoursByPerson.GetValueOrDefault(person.Id)) <=
                    series.LifetimeHoursByPerson.GetValueOrDefault(person.Id), "lifetime contributor hours");
            Check(series.MillionCopyInfluenceAwarded == series.Volumes.Any(v => !v.IsDoujin && v.CopiesSold >= 1000000), "million-copy award");
        }
        foreach (var ev in Events)
            Check(ev is not null && Time(ev.Time) && ev.ActivityDate.TimeOfDay == TimeSpan.Zero &&
                (ev.ActivityDate == ev.Time.Date || ev.Time.Hour == 0 && ev.ActivityDate == ev.Time.Date.AddDays(-1)) &&
                (ev.MagazineId is null || Magazine(ev.MagazineId)), "event context");
    }
}
