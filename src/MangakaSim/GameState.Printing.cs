using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    public static long PrintingCost(PrintTier tier, int pages, int quantity) => tier switch
    {
        PrintTier.CopyShop => quantity*(10L*(long)Math.Ceiling((pages-4)/2d)+40),
        PrintTier.LocalPrinter => 6000+quantity*(60L+2*pages),
        PrintTier.BulkPrinter => 18000+(long)Math.Ceiling(quantity*(30+.8*pages)),
        _ => throw new InvalidCommandException("Unknown printer.")
    };
    public int Stock(int volume) => PrintRuns.Where(r => r.VolumeId==volume && r.Delivered).Sum(r => r.Remaining);
    private (Series Series, Volume Volume) FindVolume(int id)
    {
        var series=Series.FirstOrDefault(s => s.Volumes.Any(v => v.Id==id)) ?? throw new InvalidCommandException("Select a volume.");
        return (series,series.Volumes.Single(v => v.Id==id));
    }
    private void PrintingAction(StudioActionCommand c)
    {
        if (c.Action==StudioAction.AutoPrint)
        {
            var s=RequireSeries(c.Target);
            if (c.Amount<0 || c.Value is < 1 or > 5000 || c.Secondary is < 1 or > 7) throw new InvalidCommandException("Set a 28-day budget, target 1–5,000 and permitted printers.");
            s.AutoPrint=c.Enabled; s.PrintBudget=c.Amount; s.PrintTarget=c.Value; s.PrintTierMask=c.Secondary; return;
        }
        var (series,volume)=FindVolume(c.Target);
        if (volume.BusinessId!=ControlledBusinessId) throw new InvalidCommandException("This master belongs to another business.");
        OrderPrinting(series,volume,(PrintTier)c.Value,checked((int)c.Amount));
    }
    private void OrderPrinting(Series series, Volume volume, PrintTier tier, int quantity)
    {
        if (!volume.IsDoujin || !Enum.IsDefined(tier)) throw new InvalidCommandException("Choose a doujin master and printer.");
        var min=tier==PrintTier.CopyShop?1:tier==PrintTier.LocalPrinter?50:300;
        var max=tier==PrintTier.CopyShop?100:tier==PrintTier.LocalPrinter?1000:5000;
        if (quantity<min || quantity>max) throw new InvalidCommandException($"This printer accepts {min:N0}–{max:N0} copies.");
        if (PrintRuns.Any(r => r.VolumeId==volume.Id && !r.Delivered)) throw new InvalidCommandException("This master already has a print order underway.");
        var location=Locations.FirstOrDefault(l => l.BusinessId==volume.BusinessId && !l.Closed &&
            l.Storage-PrintRuns.Where(r => r.LocationId==l.Id).Sum(r => r.Remaining)>=quantity) ?? throw new InvalidCommandException("There is not enough storage in an open workplace.");
        var cost=PrintingCost(tier,volume.PrintedPages,quantity);
        Spend(volume.BusinessId,cost,"printing"); volume.Contribution-=cost;
        PrintRuns.Add(new(){Id=AllocateId(),VolumeId=volume.Id,BusinessId=volume.BusinessId,LocationId=location.Id,Tier=tier,Quantity=quantity,Remaining=quantity,
            Cost=cost,OrderedAt=Clock.Now,DueAt=Clock.Now.AddDays(tier==PrintTier.CopyShop?1:tier==PrintTier.LocalPrinter?4:7)});
        StudioMessage($"{series.Title} vol.{volume.Number}: {quantity} copies ordered for ¥{cost:N0}.");
    }
    private void PrintingStep()
    {
        foreach (var run in PrintRuns.Where(r => !r.Delivered && (r.DueAt<=Clock.Now || ManagedBusiness(r.BusinessId) && Assist(SandboxAssist.InstantDelivery))))
        {
            if (run.DueAt > Clock.Now) run.DueAt = Clock.Now;
            run.Delivered=true; var (s,v)=FindVolume(run.VolumeId);
            if (v.ReleasedAt is null) { v.ReleaseDate=Clock.Now; ReleaseVolume(s,v); }
        }
        if (Clock.Hour!=8) return;
        foreach (var series in Series.Where(s => s.AutoPrint))
        foreach (var volume in series.Volumes.Where(v => v.IsDoujin && !v.SalesClosed && v.BusinessId==series.BusinessId).OrderByDescending(v => v.Number))
        {
            if (PrintRuns.Any(r => r.VolumeId==volume.Id && !r.Delivered) || Stock(volume.Id)>=Math.Max(1,series.PrintTarget/2)) continue;
            var quantity=series.PrintTarget-Stock(volume.Id);
            var spent=PrintRuns.Where(r => series.Volumes.Any(v => v.Id==r.VolumeId) && r.OrderedAt>Clock.Now.AddDays(-28)).Sum(r => r.Cost);
            var choices=Enum.GetValues<PrintTier>().Where(t => (series.PrintTierMask&(1<<(int)t))!=0)
                .Select(t => (Tier:t, Quantity:Math.Max(quantity,t==PrintTier.CopyShop?1:t==PrintTier.LocalPrinter?50:300)))
                .Where(x => x.Quantity <= (x.Tier==PrintTier.CopyShop?100:x.Tier==PrintTier.LocalPrinter?1000:5000))
                .OrderBy(x => PrintingCost(x.Tier,volume.PrintedPages,x.Quantity));
            foreach (var choice in choices)
            {
                if (PrintingCost(choice.Tier,volume.PrintedPages,choice.Quantity)+spent>series.PrintBudget) continue;
                try { OrderPrinting(series,volume,choice.Tier,choice.Quantity); break; } catch (InvalidCommandException) { }
            }
        }
    }
    private static DateTime Monday(DateTime date) => date.Date.AddDays(-((int)date.DayOfWeek+6)%7);
    private void DemandFor(Series s, Volume v)
    {
        var week=Monday(Clock.Now);
        if (v.DemandWeek==week) return;
        v.DemandWeek=week;
        var online=BusinessOf(v.BusinessId).HasInternet ? Economy.InternetReach(TrendCatalog,Clock.Now) : 0;
        var raw=SalesRules.DoujinCopies(s.Fanbase,v.AverageQuality,s.IsIconic?1:GenrePopularity(s.Genre),online,v.WeeksOnSale+1);
        v.WeeklyDemand=(int)Math.Min(int.MaxValue,ChannelPhysicalDemand(s,v,(long)Math.Floor(raw*(1+s.Reach/100)*(v.SalesClosed?.1:1)*RivalDemand(s.Genre,v.AverageQuality)*RecognitionLift(s.Id))));
    }
    private long SellStock(Series s, Volume v, long requested, bool convention)
    {
        DemandFor(s,v);
        var limit=(int)Math.Min(int.MaxValue,Math.Min(requested,v.WeeklyDemand));
        var sold=0;
        foreach (var run in PrintRuns.Where(r => r.VolumeId==v.Id && r.Delivered && r.Remaining>0).OrderBy(r => r.OrderedAt).ThenBy(r => r.Id))
        {
            var presentation=run.Tier==PrintTier.CopyShop?.25:run.Tier==PrintTier.LocalPrinter?.7:1;
            var established=Math.Clamp(Math.Max(s.ChaptersPublished/500d,s.Fanbase/500000),0,1);
            var demand=(int)Math.Floor((limit-sold)*(1-(1-presentation)*(.02+.58*established*established)));
            var count=Math.Min(Math.Max(0,run.Remaining-ReservedFromRun(run.Id)),demand); run.Remaining-=count; sold+=count;
        }
        v.WeeklyDemand-=sold;
        var revenue=(long)Math.Floor(sold*v.Price*(convention?1:.7));
        if (revenue>0)
        {
            AccountPost(BusinessOf(v.BusinessId).Account,revenue,"doujin sales",AccountEntryKind.Publishing,s.Id);
            VolumeContribution(v,revenue);
        }
        return sold;
    }
    private void VolumeContribution(Volume v, long amount)
    {
        v.Contribution+=amount;
        var entitlement=Math.Max(0,(long)decimal.Floor(v.Contribution*.2m));
        var newlyDue=Math.Max(0,entitlement-v.CreatorAccrued);
        if (newlyDue==0 || v.CreatorShares.Count==0) return;
        var total=v.CreatorShares.Values.Sum(); long allocated=0;
        var shares=v.CreatorShares.OrderBy(p => p.Key).ToArray();
        for (var i=0;i<shares.Length;i++)
        {
            var pay=i==shares.Length-1 ? newlyDue-allocated : newlyDue*shares[i].Value/total;
            AddBill(v.BusinessId,pay,"creator share",shares[i].Key); allocated+=pay;
        }
        v.CreatorAccrued+=newlyDue;
    }
}
