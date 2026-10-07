using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void PostLedger(long amount, string reason, int? seriesId = null)
    {
        var business = seriesId is { } id ? FindSeries(id)!.BusinessId : ControlledBusinessId;
        AccountPost(BusinessOf(business).Account, amount, reason, amount < 0 ? AccountEntryKind.Expense : AccountEntryKind.Publishing, seriesId);
    }

    private void ApplyGetOnline()
    {
        RequireOwner();
        if (HasInternet) throw new InvalidCommandException("The studio is already online.");
        if (Ledger.Any(e => e.Reason == "internet")) throw new InvalidCommandException("Settle the subscription arrears; connection resumes next morning.");
        var cost = Economy.InternetCost(TrendCatalog, Clock.Now);
        if (AvailableBusinessCash < cost) throw new InvalidCommandException($"Getting online costs {cost:N0} yen after reserved wages.");
        PostLedger(-cost, "internet");
        HasInternet = true;
        foreach (var volume in Series.Where(s => s.BusinessId == ControlledBusinessId).SelectMany(s => s.Volumes).Where(v => v.IsDoujin && !v.SalesClosed)) volume.SalesWindowWeeks = 8;
        Emit(EventType.WentOnline, "The studio is online.", context: new(Amount: -cost));
    }
    private static HashSet<int> Collected(Series series) => series.Volumes.Where(v=>v.Format!=VolumeFormat.DoujinIssue).SelectMany(v => v.ChapterIds).ToHashSet();
    private void CollectDoujin(Series series)
    {
        if (series.Publishing != PublishingStatus.Unpublished) return;
        var collected = Collected(series);
        var chapters = series.Chapters.Where(c => c.Status == ChapterStatus.Complete && c.DoujinEligible &&
            c.PublishedAt is null && !collected.Contains(c.Id)).OrderBy(c => c.Number).ToArray();
        if(series.ReleaseShortIssues&&!series.StandaloneDoujin)
            foreach(var chapter in chapters.Where(c=>!series.Volumes.Any(v=>v.Format==VolumeFormat.DoujinIssue&&v.ChapterIds.Contains(c.Id))))
                CreateVolume(series,[chapter],true,Clock.Now,VolumeFormat.DoujinIssue);
        var count = series.StandaloneDoujin ? 1 : 5;
        for (var i = 0; i + count <= chapters.Length; i += count) CreateVolume(series, chapters.Skip(i).Take(count).ToArray(), true, Clock.Now);
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
    private void CreateVolume(Series series, Chapter[] chapters, bool doujin, DateTime release,VolumeFormat format=VolumeFormat.Tankobon)
    {
        var volume = new Volume
        {
            BusinessId = series.BusinessId,
            CreatorShares = chapters.GroupBy(c => c.CreatorPersonId ?? series.LeadPersonId).ToDictionary(g => g.Key,g => g.Count()),
            PrintedPages = 4*(int)Math.Ceiling((chapters.Sum(c => c.Pages)+4)/4d),
            Price = Math.Max(300, 100*(long)Math.Ceiling((4*Math.Ceiling((chapters.Sum(c => c.Pages)+4)/4d)*10)/100)),
            Id = AllocateId(), Number = series.Volumes.Count + 1, Format = format,
            EditionNumber=series.Volumes.Count(v=>v.Format==format)+1,
            ChapterIds = chapters.Select(c => c.Id).ToList(), FirstChapter = chapters.Min(c => c.Number),
            LastChapter = chapters.Max(c => c.Number), AverageQuality = chapters.Average(c => c.Quality!.Value),
            IsDoujin = doujin, ReleaseDate = release, SalesWindowWeeks = doujin ? (BusinessOf(series.BusinessId).HasInternet ? 8 : 4) : 52,
        };
        series.Volumes.Add(volume);
        if (doujin) StudioMessage($"{series.Title} · {EditionName(volume)}: print-ready master ({volume.PrintedPages} pages). Choose a print run.");
        else Emit(EventType.VolumeScheduled, $"{series.Title} volume {volume.Number} scheduled for {release:d MMM yyyy}.", series.Id,
            context: new(VolumeId: volume.Id));
    }
    private void ReleaseVolume(Series series, Volume volume)
    {
        if (volume.ReleasedAt is not null) return;
        volume.ReleasedAt = Clock.Now;
        if (!volume.IsDoujin) PayPrintRun(series, volume, 0, SalesRules.Rung(volume.CopiesSold), "royalties, first print run");
        if (volume.IsDoujin && volume.AverageQuality >= 75) ChangeTrackRecord(.5, series.BusinessId);
        Emit(EventType.VolumeReleased, $"{series.Title} volume {volume.Number} released ({(volume.IsDoujin ? "doujin" : "tankobon")}).",
            series.Id, context: new(VolumeId: volume.Id));
        // Streaming sales (spec 2026-10-03): a book on sale midweek starts selling now. Its first week (the same
        // first-week demand the next Monday would give) spreads over the shop hours left, at least 30.
        var hours = Math.Max(SalesRules.MinimumFirstWeekHours, SalesRules.ShopHoursUntil(Clock.Now, Monday(Clock.Now).AddDays(7)));
        PlanVolumeWeek(series, volume, series.Fanbase, series.IsIconic ? 1 : GenrePopularity(series.Genre), hours);
    }
    // Royalties on the copies added to the print run, paid when the run is ordered.
    private long PayPrintRun(Series series, Volume volume, long printedBefore, long printedAfter, string description)
    {
        var revenue = SalesRules.Income(printedAfter - printedBefore, Economy.PriceIndex(TrendCatalog, volume.ReleaseDate), false);
        if (revenue <= 0) return 0;
        AccountPost(BusinessOf(volume.BusinessId).Account, revenue, description, AccountEntryKind.Publishing, series.Id);
        VolumeContribution(volume, revenue);
        return revenue;
    }
    /// <summary>True once a series has run in a magazine, under its current contract or an earlier one.</summary>
    internal static bool MagazineHistory(Series series) => series.Contract is not null || series.PastContracts.Count > 0;
    private int SeriesTier(Series series) =>
        (series.Contract ?? series.PastContracts.LastOrDefault()) is { } contract ? PublisherCatalog.Get(contract.MagazineId).Tier : 1;
    internal void SalesStep()
    {
        foreach (var series in Series.OrderBy(s => s.Id))
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.ReleasedAt is null && v.ReleaseDate <= Clock.Now &&
                (!v.IsDoujin || ActiveChannel(series, v, ReleaseChannel.DomesticDigital) is not null))) ReleaseVolume(series, volume);
        if (Clock.DayOfWeek == DayOfWeek.Monday && Clock.Hour == 0 && LastSalesAt != Clock.Now) PlanSalesWeek();
        // Streaming sales (spec 2026-10-03): each shop hour releases the share of every plan due so far.
        if (SalesRules.IsShopHour(Clock.Now) && LastShopHourAt != Clock.Now) { LastShopHourAt = Clock.Now; ReleaseSalesHour(); }
    }
    private void PlanSalesWeek()
    {
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
            var trend = series.IsIconic ? 1 : GenrePopularity(series.Genre);
            // A book that went on sale on this very tick was planned at release (Task 4).
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.ReleasedAt is not null && v.ReleasedAt != Clock.Now &&
                (!v.SalesClosed || DirectDownload(v) is not null)))
                PlanVolumeWeek(series, volume, fans, trend, SalesRules.ShopHoursPerWeek);
            if (BusinessOf(series.BusinessId).HasInternet && series.Publishing == PublishingStatus.Unpublished && series.Volumes.Any(v => v.ReleasedAt is not null))
            {
                var mouth = FanbaseRules.Saturated(series.Fanbase, series.Fanbase * .01 * Economy.InternetReach(TrendCatalog, Clock.Now));
                series.Fanbase += mouth;
                DoujinFansThisMonth += mouth;
            }
            CheckIconic(series);
        }
    }
    // The week's demand, with today's formulas, becomes plans released through shop hours.
    private void PlanVolumeWeek(Series series, Volume volume, double fans, double trend, int hours)
    {
        var copies = volume.IsDoujin ? SalesRules.DoujinCopies(fans, volume.AverageQuality, trend,
            BusinessOf(volume.BusinessId).HasInternet ? Economy.InternetReach(TrendCatalog, Clock.Now) : 0, volume.WeeksOnSale + 1) :
            SalesRules.CommercialCopies(fans, volume.AverageQuality, trend, volume.WeeksOnSale + 1, SeriesTier(series));
        var potential = (long)Math.Floor(copies*(1+series.Reach/100)*RivalDemand(series.Genre,volume.AverageQuality)*RecognitionLift(series.Id));
        volume.SalesPlans ??= new();
        PlanChannelDemand(series, volume, potential, hours);
        RecordSales(series, volume, 0, weekly: true);
        if (volume.SalesClosed) return;
        long total;
        if (volume.IsDoujin) { DemandFor(series, volume); total = (long)Math.Floor(volume.WeeklyDemand * .3); }
        else total = ChannelPhysicalDemand(series, volume, potential);
        volume.SalesPlans.Add(new() { Kind = volume.IsDoujin ? SaleKind.Shop : SaleKind.Commercial, Week = Monday(Clock.Now), Total = total, Hours = hours });
        volume.WeeksOnSale++;
    }
    private void ReleaseSalesHour()
    {
        foreach (var series in Series.OrderBy(s => s.Id))
        {
            var milestones = new List<(Volume Volume, long Threshold)>();
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.SalesPlans is { Count: > 0 }))
            {
                long copies = 0, units = 0;
                var plans = volume.SalesPlans!;
                foreach (var plan in plans.ToList())
                {
                    plan.HoursDone++;
                    var due = SalesRules.DueBy(plan.Total, plan.HoursDone, plan.Hours) - plan.Released;
                    plan.Released += due;
                    if (due > 0)
                        switch (plan.Kind)
                        {
                            case SaleKind.Shop: copies += SellStock(series, volume, due, false, daily: true, plan: plan); break;
                            case SaleKind.Commercial: copies += due; break;
                            default: units += ReleaseChannelUnits(series, volume, plan, due); break;
                        }
                    if (plan.HoursDone >= plan.Hours) plans.Remove(plan);
                }
                ApplySales(series, volume, copies, units, milestones);
                if (volume.WeeksOnSale >= volume.SalesWindowWeeks && !plans.Any(p => p.Kind != SaleKind.Download)) volume.SalesClosed = true;
            }
            AwardMilestones(series, milestones);
            CheckIconic(series);
        }
    }
    private void ApplySales(Series series, Volume volume, long copies, long units, List<(Volume Volume, long Threshold)> milestones)
    {
        if (copies == 0 && units == 0) return;
        var old = volume.CopiesSold;
        volume.CopiesSold = checked(old + copies);
        RecordSales(series, volume, copies, weekly: true);
        if (!volume.IsDoujin)
        {
            var (printed, reprint) = (SalesRules.Rung(old), SalesRules.Rung(volume.CopiesSold));
            if (reprint > printed && PayPrintRun(series, volume, printed, reprint, "royalties, reprint") > 0)
                StudioMessage($"{series.Title} volume {volume.Number} goes back to press: {reprint:N0} copies in print.");
            foreach (var threshold in new[] { 100000L, 1000000L })
                if (old < threshold && volume.CopiesSold >= threshold) milestones.Add((volume, threshold));
        }
        // A magazine series' old doujin reaches readers it already has, so it adds fans at the collected-volume rate (balance pass 2026-10-05).
        var fanGain = FanbaseRules.Saturated(series.Fanbase, (copies + units) * (volume.IsDoujin && !MagazineHistory(series) ? .3 : SalesRules.CommercialFanGain));
        series.Fanbase += fanGain;
        if (volume.IsDoujin) { DoujinCopiesThisMonth = checked(DoujinCopiesThisMonth + copies); DoujinFansThisMonth += fanGain; }
    }
    private void AwardMilestones(Series series, List<(Volume Volume, long Threshold)> milestones)
    {
        foreach (var (volume, threshold) in milestones)
        {
            ChangeTrackRecord(threshold == 100000 ? 3 : 10, volume.BusinessId);
            if (threshold == 100000) AddInfluence(series, .05);
            else if (!series.MillionCopyInfluenceAwarded) { AddInfluence(series, .15); series.MillionCopyInfluenceAwarded = true; }
            Emit(EventType.VolumeMilestone, $"{series.Title} volume {volume.Number} reached {threshold:N0} copies.", series.Id,
                context: new(VolumeId: volume.Id, Amount: threshold));
        }
    }
}
