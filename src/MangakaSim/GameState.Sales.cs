namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- volumes

    /// <summary>Finished chapters of the series whose number lies outside every volume.</summary>
    internal List<Chapter> ChaptersOutsideVolumes(Series series, bool publishedOnly) =>
        series.Chapters.Where(c => c.Status == ChapterStatus.Complete && !series.IsInVolume(c.Number) &&
                                   (!publishedOnly || c.IsPublished)).ToList();

    private Volume CreateVolume(Series series, List<Chapter> chapters, bool isDoujin, DateTime releaseDate)
    {
        var volume = new Volume
        {
            Id = AllocateId(),
            Number = series.Volumes.Count + 1,
            Format = VolumeFormat.Tankobon,
            FirstChapter = chapters.Min(c => c.Number),
            LastChapter = chapters.Max(c => c.Number),
            ReleaseDate = releaseDate,
            IsDoujin = isDoujin,
            AverageQuality = chapters.Average(c => c.Quality ?? 0),
        };
        series.Volumes.Add(volume);
        return volume;
    }

    /// <summary>An Unpublished series with five finished chapters outside any volume releases a doujin volume now.</summary>
    internal void TryCreateDoujinVolume(Series series)
    {
        if (series.Publishing != PublishingStatus.Unpublished) return;
        var chapters = ChaptersOutsideVolumes(series, publishedOnly: false);
        if (chapters.Count < SalesRules.DoujinChaptersPerVolume) return;
        var volume = CreateVolume(series, chapters, isDoujin: true, Clock.Now);
        volume.IsReleased = true;
        Emit(EventType.VolumeReleased,
            $"{series.Title} doujin vol.{volume.Number} (ch.{volume.FirstChapter}-{volume.LastChapter}) goes on sale, average quality {volume.AverageQuality:0}.",
            new EventContext(SeriesId: series.Id, VolumeId: volume.Id));
        if (volume.AverageQuality >= 75) AdjustTrackRecord(ReputationRules.DoujinQualityVolume);
    }

    /// <summary>After a publish: enough published chapters outside any volume make a tankobon due six weeks later.</summary>
    private void TryScheduleTankobon(Series series, Magazine magazine, DateTime closeTime)
    {
        var chapters = ChaptersOutsideVolumes(series, publishedOnly: true);
        if (chapters.Count < magazine.ChaptersPerVolume) return;
        ScheduleTankobon(series, chapters, closeTime);
    }

    /// <summary>On ending or cancellation: leftover published chapters (at least three) become a final volume.</summary>
    private void ScheduleFinalVolume(Series series, Magazine magazine)
    {
        var chapters = ChaptersOutsideVolumes(series, publishedOnly: true);
        if (chapters.Count < SalesRules.MinLeftoverForFinalVolume) return;
        ScheduleTankobon(series, chapters, Clock.Now);
    }

    private void ScheduleTankobon(Series series, List<Chapter> chapters, DateTime from)
    {
        var volume = CreateVolume(series, chapters, isDoujin: false, from.AddDays(7 * SalesRules.ReleaseDelayWeeks));
        Emit(EventType.VolumeScheduled,
            $"{series.Title} vol.{volume.Number} (ch.{volume.FirstChapter}-{volume.LastChapter}) goes on sale {volume.ReleaseDate:d MMM yyyy}.",
            new EventContext(SeriesId: series.Id, VolumeId: volume.Id));
    }

    // ---------------------------------------------------------------- sales

    /// <summary>Releases due volumes every tick; on Mondays at 00:00 sells volumes, pays the ledger and runs the convention recap.</summary>
    internal void SalesStep()
    {
        foreach (var series in Series)
        {
            foreach (var volume in series.Volumes)
            {
                if (volume.IsReleased || Clock.Now < volume.ReleaseDate) continue;
                volume.IsReleased = true;
                Emit(EventType.VolumeReleased,
                    $"{series.Title} vol.{volume.Number} (ch.{volume.FirstChapter}-{volume.LastChapter}) is in the shops.",
                    new EventContext(SeriesId: series.Id, VolumeId: volume.Id));
            }
        }

        if (Clock.DayOfWeek != DayOfWeek.Monday || Clock.Hour != 0) return;

        if (Clock.Now.Day <= 7) ConventionRecap();

        var reach = HasInternet ? Economy.InternetReach(TrendData, Clock.Now) : 0;
        foreach (var series in Series)
        {
            var trend = GenreTrendFor(series);
            foreach (var volume in series.Volumes.Where(v => v.IsReleased).OrderBy(v => v.Number))
            {
                if (volume.IsDoujin) SellDoujinWeek(series, volume, trend, reach);
                else SellTankobonWeek(series, volume, trend);
            }
            if (HasInternet && series.Publishing == PublishingStatus.Unpublished && series.Volumes.Any(v => v.IsReleased))
                series.Fanbase += series.Fanbase * 0.01 * reach;
            CheckIconic(series);
        }
    }

    private void SellTankobonWeek(Series series, Volume volume, double trend)
    {
        if (volume.WeeksOnSale >= SalesRules.TankobonWeeks) return;
        var week = volume.WeeksOnSale + 1;
        var copies = SalesRules.TankobonCopies(week, series.Fanbase, volume.AverageQuality, trend);
        var before = volume.CopiesSold;
        volume.CopiesSold += copies;
        volume.WeeksOnSale++;
        var royalty = SalesRules.Royalty(copies, SalesRules.TankobonCover(Economy.PriceIndex(TrendData, volume.ReleaseDate)));
        if (royalty != 0) AddLedger(royalty, "royalties", series.Id);
        series.Fanbase += copies * FanbaseRules.VolumeFanShare;
        CheckMilestones(series, volume, before);
    }

    private void SellDoujinWeek(Series series, Volume volume, double trend, double reach)
    {
        if (volume.WeeksOnSale >= SalesRules.DoujinWindow(HasInternet)) return;
        var week = volume.WeeksOnSale + 1;
        var copies = SalesRules.DoujinCopies(week, series.Fanbase, volume.AverageQuality, trend, reach);
        var before = volume.CopiesSold;
        volume.CopiesSold += copies;
        volume.WeeksOnSale++;
        var releaseIndex = Economy.PriceIndex(TrendData, volume.ReleaseDate);
        var income = SalesRules.DoujinIncome(copies, SalesRules.DoujinCover(releaseIndex), SalesRules.PrintCost(releaseIndex));
        if (income != 0) AddLedger(income, "doujin sales", series.Id);
        var fans = copies * FanbaseRules.DoujinFanShare;
        series.Fanbase += fans;
        DoujinCopiesThisMonth += copies;
        DoujinFansThisMonth += fans;
        CheckMilestones(series, volume, before);
    }

    private void CheckMilestones(Series series, Volume volume, long before)
    {
        foreach (var (milestone, track, influence) in new[]
                 {
                     (SalesRules.Milestone100k, ReputationRules.Volume100k, 0.05),
                     (SalesRules.Milestone1M, ReputationRules.Volume1M, 0.15),
                 })
        {
            if (before >= milestone || volume.CopiesSold < milestone) continue;
            Emit(EventType.VolumeMilestone,
                $"{series.Title} vol.{volume.Number} passes {milestone:N0} copies.",
                new EventContext(SeriesId: series.Id, VolumeId: volume.Id, Amount: volume.CopiesSold));
            AdjustTrackRecord(track);
            if (series.IsIconic) continue;
            if (milestone == SalesRules.Milestone1M)
            {
                if (series.MillionInfluenceGiven) continue;
                series.MillionInfluenceGiven = true;
            }
            AddPlayerInfluence(series.Genre, influence);
        }
    }

    /// <summary>First Monday of the month: sums last month's doujin sales, then resets the counters.</summary>
    private void ConventionRecap()
    {
        if (DoujinCopiesThisMonth > 0)
        {
            var table = Economy.Inflate(SalesRules.ConventionTable1996, PriceIndexNow);
            AddLedger(-table, "convention table");
            Emit(EventType.ConventionRecap,
                $"Convention season: {DoujinCopiesThisMonth:N0} doujin copies sold last month and {DoujinFansThisMonth:N0} new fans; the table cost {table:N0} yen.",
                new EventContext(Amount: DoujinCopiesThisMonth));
        }
        DoujinCopiesThisMonth = 0;
        DoujinFansThisMonth = 0;
    }

    // ---------------------------------------------------------------- internet

    public long InternetCostNow => Economy.Inflate(Economy.InternetCost1996, PriceIndexNow);

    private void ApplyGetOnline(GetOnlineCommand c)
    {
        if (HasInternet) throw new InvalidCommandException("The studio is already online.");
        var cost = InternetCostNow;
        if (Money < cost) throw new InvalidCommandException($"Getting online costs {cost:N0} yen; the studio has {Money:N0}.");
        AddLedger(-cost, "internet");
        HasInternet = true;
        Emit(EventType.WentOnline,
            $"The studio is online for {cost:N0} yen. Doujin sales reach further and word of mouth starts to spread.",
            new EventContext(Amount: -cost));
    }

}
