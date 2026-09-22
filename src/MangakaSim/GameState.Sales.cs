using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void PostLedger(long amount, string reason, int? seriesId = null)
    {
        var balance = checked(Money + amount);
        if (balance < 0) throw new InvalidCommandException("There is not enough money for this purchase.");
        Money = balance;
        Ledger.Add(new(Clock.Now, amount, reason, seriesId));
    }
    private void ApplyGetOnline()
    {
        if (HasInternet) throw new InvalidCommandException("The studio is already online.");
        var cost = Economy.InternetCost(TrendCatalog, Clock.Now);
        if (Money < cost) throw new InvalidCommandException($"Getting online costs {cost:N0} yen.");
        PostLedger(-cost, "internet");
        HasInternet = true;
        foreach (var volume in Series.SelectMany(s => s.Volumes).Where(v => v.IsDoujin && !v.SalesClosed)) volume.SalesWindowWeeks = 8;
        Emit(EventType.WentOnline, "The studio is online.", context: new(Amount: -cost));
    }
    private static HashSet<int> Collected(Series series) => series.Volumes.SelectMany(v => v.ChapterIds).ToHashSet();
    private void CollectDoujin(Series series)
    {
        if (series.Publishing != PublishingStatus.Unpublished) return;
        var collected = Collected(series);
        var chapters = series.Chapters.Where(c => c.Status == ChapterStatus.Complete && c.DoujinEligible &&
            c.PublishedAt is null && !collected.Contains(c.Id)).OrderBy(c => c.Number).ToArray();
        for (var i = 0; i + 5 <= chapters.Length; i += 5) CreateVolume(series, chapters.Skip(i).Take(5).ToArray(), true, Clock.Now);
    }
    private void CollectCommercial(Series series, int count, bool final = false)
    {
        var collected = Collected(series);
        var chapters = series.Chapters.Where(c => c.PublishedAt is not null && !collected.Contains(c.Id))
            .OrderBy(c => c.PublishedAt).ThenBy(c => c.Number).ToArray();
        if (final)
        {
            if (chapters.Length >= 3) CreateVolume(series, chapters, false, Clock.Now.AddDays(42));
        }
        else
        {
            for (var i = 0; i + count <= chapters.Length; i += count)
            {
                var group = chapters.Skip(i).Take(count).ToArray();
                CreateVolume(series, group, false, group[^1].PublishedAt!.Value.AddDays(42));
            }
        }
    }
    private void CreateVolume(Series series, Chapter[] chapters, bool doujin, DateTime release)
    {
        var volume = new Volume
        {
            Id = AllocateId(), Number = series.Volumes.Count + 1, Format = VolumeFormat.Tankobon,
            ChapterIds = chapters.Select(c => c.Id).ToList(), FirstChapter = chapters.Min(c => c.Number),
            LastChapter = chapters.Max(c => c.Number), AverageQuality = chapters.Average(c => c.Quality!.Value),
            IsDoujin = doujin, ReleaseDate = release, SalesWindowWeeks = doujin ? (HasInternet ? 8 : 4) : 52,
        };
        series.Volumes.Add(volume);
        if (doujin) ReleaseVolume(series, volume);
        else Emit(EventType.VolumeScheduled, $"{series.Title} volume {volume.Number} scheduled for {release:d MMM yyyy}.", series.Id,
            context: new(VolumeId: volume.Id));
    }
    private void ReleaseVolume(Series series, Volume volume)
    {
        if (volume.ReleasedAt is not null) return;
        volume.ReleasedAt = Clock.Now;
        if (volume.IsDoujin && volume.AverageQuality >= 75) ChangeTrackRecord(.5);
        Emit(EventType.VolumeReleased, $"{series.Title} volume {volume.Number} released ({(volume.IsDoujin ? "doujin" : "tankobon")}).",
            series.Id, context: new(VolumeId: volume.Id));
    }
    internal void SalesStep()
    {
        foreach (var series in Series.OrderBy(s => s.Id))
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.ReleasedAt is null && v.ReleaseDate <= Clock.Now)) ReleaseVolume(series, volume);
        if (Clock.DayOfWeek != DayOfWeek.Monday || Clock.Hour != 0 || LastSalesAt == Clock.Now) return;
        LastSalesAt = Clock.Now;
        if (Clock.Now.Day <= 7)
        {
            if (DoujinCopiesThisMonth > 0) Emit(EventType.ConventionRecap,
                $"Last month: {DoujinCopiesThisMonth:N0} doujin copies, {DoujinFansThisMonth:N0} new fans.");
            DoujinCopiesThisMonth = 0;
            DoujinFansThisMonth = 0;
        }
        foreach (var series in Series.OrderBy(s => s.Id))
        {
            var fans = series.Fanbase;
            double gains = 0;
            var trend = series.IsIconic ? 1 : GenrePopularity(series.Genre);
            var milestones = new List<(Volume Volume, long Threshold)>();
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.ReleasedAt is not null && !v.SalesClosed))
            {
                var copies = volume.IsDoujin ? SalesRules.DoujinCopies(fans, volume.AverageQuality, trend,
                    HasInternet ? Economy.InternetReach(TrendCatalog, Clock.Now) : 0, volume.WeeksOnSale + 1) :
                    SalesRules.CommercialCopies(fans, volume.AverageQuality, trend, volume.WeeksOnSale + 1);
                var old = volume.CopiesSold;
                volume.CopiesSold = checked(old + copies);
                volume.WeeksOnSale++;
                volume.SalesClosed = volume.WeeksOnSale >= volume.SalesWindowWeeks;
                PostLedger(SalesRules.Income(copies, Economy.PriceIndex(TrendCatalog, volume.ReleaseDate), volume.IsDoujin),
                    volume.IsDoujin ? "doujin sales" : "royalties", series.Id);
                var fanGain = copies * (volume.IsDoujin ? .3 : .05);
                gains += fanGain;
                if (volume.IsDoujin)
                {
                    DoujinCopiesThisMonth = checked(DoujinCopiesThisMonth + copies);
                    DoujinFansThisMonth += fanGain;
                }
                else
                {
                    foreach (var threshold in new[] { 100000L, 1000000L })
                    {
                        if (old >= threshold || volume.CopiesSold < threshold) continue;
                        milestones.Add((volume, threshold));
                    }
                }
            }
            series.Fanbase = fans + gains;
            CheckIconic(series);
            foreach (var (volume, threshold) in milestones)
            {
                ChangeTrackRecord(threshold == 100000 ? 3 : 10);
                if (threshold == 100000) AddInfluence(series, .05);
                else if (!series.MillionCopyInfluenceAwarded)
                {
                    AddInfluence(series, .15);
                    series.MillionCopyInfluenceAwarded = true;
                }
                Emit(EventType.VolumeMilestone, $"{series.Title} volume {volume.Number} reached {threshold:N0} copies.", series.Id,
                    context: new(VolumeId: volume.Id, Amount: threshold));
            }
            if (HasInternet && series.Publishing == PublishingStatus.Unpublished && series.Volumes.Any(v => v.ReleasedAt is not null))
            {
                var mouth = series.Fanbase * .01 * Economy.InternetReach(TrendCatalog, Clock.Now);
                series.Fanbase += mouth;
                DoujinFansThisMonth += mouth;
            }
            CheckIconic(series);
        }
    }
}
