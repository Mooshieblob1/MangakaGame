using MangakaSim.Rules;
namespace MangakaSim;

public partial class GameState
{
    public static string ChannelName(ReleaseChannel channel) => channel==ReleaseChannel.DomesticDigital?"Domestic digital":"Overseas";
    public bool ChannelAvailable(ReleaseChannel channel) => Assist(SandboxAssist.FutureTechnology) || World.Processed.Contains(channel==ReleaseChannel.DomesticDigital?"industry:mobile":"industry:overseas-print");
    public double DigitalPreference => !World.Processed.Contains("industry:mobile")?0:Math.Clamp(.05+(Clock.Now-new DateTime(2006,5,1)).TotalDays/(365.25*19)*.45,.05,.5);
    private void RequestChannel(TimelineCommand c)
    {
        var series=RequireSeries(c.Target);
        var channel=c.Action==TimelineAction.RequestDigital?ReleaseChannel.DomesticDigital:ReleaseChannel.Overseas;
        if(!ChannelAvailable(channel))throw new InvalidCommandException("This distribution route is not available yet.");
        if(channel==ReleaseChannel.DomesticDigital&&!ControlledBusiness.HasInternet)throw new InvalidCommandException("Connect this studio to the internet first.");
        if(channel==ReleaseChannel.Overseas&&series.Contract is null)throw new InvalidCommandException("Overseas release requires a publishing partner in this milestone.");
        if(series.Contract is {} contract&&channel==ReleaseChannel.Overseas&&PublisherCatalog.Get(contract.MagazineId).PublisherId!="tokiwa"&&!World.Processed.Contains("industry:global")&&!Assist(SandboxAssist.FutureTechnology))
            throw new InvalidCommandException("This publisher's overseas partnership opens with the broader international platform era.");
        if(World.Channels.Any(a=>a.SeriesId==series.Id&&a.BusinessId==ControlledBusinessId&&a.Channel==channel&&
            (a.Status is NegotiationStatus.Pending or NegotiationStatus.Accepted||a.ResolvesAt.AddDays(90)>Clock.Now)))
            throw new InvalidCommandException("This channel is already open, under review, or in its ninety-day retry period.");
        var volumes=series.Volumes.Where(v=>v.BusinessId==ControlledBusinessId&&!v.SalesClosed).ToArray();
        if(volumes.Length==0)throw new InvalidCommandException("Prepare an eligible volume with an open sales window first.");
        var cost=ChannelSetupFee(channel);
        if(FreeCash(ControlledBusinessId)<cost||TeamBudgetLeft(ControlledBusinessId)<cost)throw new InvalidCommandException("The quoted setup needs available business cash and team authority.");
        var direct=channel==ReleaseChannel.DomesticDigital&&series.Contract is null;
        var agreement=new ChannelAgreement{Id=AllocateId(),SeriesId=series.Id,BusinessId=ControlledBusinessId,Channel=channel,Cost=cost,
            CreatedAt=Clock.Now,ResolvesAt=direct?Clock.Now:Clock.Now.AddDays(7),Roll=World.HiringRng.NextDouble(),VolumeIds=volumes.Select(v=>v.Id).ToList()};
        World.Channels.Add(agreement);
        if(direct)ActivateChannel(agreement);else TimelineNews($"Publisher proposal sent for {series.Title}: {ChannelName(channel)}. ¥{cost:N0} reserved; decision {agreement.ResolvesAt:d MMM}. No fee if refused.");
    }
    private void ActivateChannel(ChannelAgreement a)
    {
        a.Status=NegotiationStatus.Accepted;
        Spend(a.BusinessId,a.Cost,a.Channel==ReleaseChannel.DomesticDigital?"digital edition setup":"overseas localization");
        a.Reason="Approved";a.InternationalInterest=a.Channel==ReleaseChannel.Overseas?10:0;
        var series=FindSeries(a.SeriesId)!;
        foreach(var v in series.Volumes.Where(v=>a.VolumeIds.Contains(v.Id)&&v.IsDoujin&&v.ReleasedAt is null))ReleaseVolume(series,v);
        TimelineNews($"{series.Title}: {ChannelName(a.Channel)} approved. Setup ¥{a.Cost:N0}; earnings retain each edition's original business and creator shares.");
    }
    private void ResolveChannels()
    {
        foreach(var a in World.Channels.Where(a=>a.Status==NegotiationStatus.Pending&&a.ResolvesAt<=Clock.Now).OrderBy(a=>a.Id))
        {
            var series=FindSeries(a.SeriesId)!;
            a.Status=NegotiationStatus.Declined;
            if(series.BusinessId!=a.BusinessId||!a.VolumeIds.Any(id=>series.Volumes.Any(v=>v.Id==id&&v.BusinessId==a.BusinessId&&!v.SalesClosed)))a.Reason="Rights or eligible editions changed";
            else if(FreeCash(a.BusinessId)<a.Cost||TeamBudgetLeft(a.BusinessId)<a.Cost)a.Reason="The business no longer has the setup funds or team budget";
            else
            {
                var quality=series.Volumes.Where(v=>a.VolumeIds.Contains(v.Id)).Average(v=>v.AverageQuality);
                var fit=series.Contract is {} contract?PublisherCatalog.Get(contract.MagazineId).Affinity(TrendRules.Normalise(series.Genre,TrendCatalog)):1;
                var chance=Math.Clamp(.25+(quality-40)/100+Math.Min(.25,series.Fanbase/100000)+.5*(fit-1),.05,.95);
                if(a.Roll<chance){ActivateChannel(a);continue;}
                a.Reason=quality<60?"Publisher wants stronger recent work":series.Fanbase<10000?"Publisher needs more evidence of reader demand":"The publisher is prioritizing other titles in this genre";
            }
            if(a.BusinessId==ControlledBusinessId)TimelineNews($"{series.Title}: {a.Reason}. No setup fee charged; reconsider after ninety days with improved prospects.");
        }
    }
    private ChannelAgreement? ActiveChannel(Series s,Volume v,ReleaseChannel channel)
    {
        var a=World.Channels.LastOrDefault(a=>a.SeriesId==s.Id&&a.BusinessId==v.BusinessId&&a.Channel==channel&&a.Status==NegotiationStatus.Accepted&&
            (a.VolumeIds.Contains(v.Id)||!a.DirectDoujin&&s.BusinessId==a.BusinessId&&v.ReleaseDate>=a.CreatedAt));
        if(a is not null&&!a.VolumeIds.Contains(v.Id))a.VolumeIds.Add(v.Id);
        return a;
    }
    private long ChannelPhysicalDemand(Series s,Volume v,long total)
    {
        var active=ActiveChannel(s,v,ReleaseChannel.DomesticDigital);
        return (long)Math.Floor(total*(1-(active is not null?Math.Max(active.DirectDoujin?.15:.05,DigitalPreference):DigitalPreference*.5)));
    }
    // Streaming sales (spec 2026-10-03): a week's channel units become plans; the receipt fills as they are released.
    private void PlanChannelDemand(Series s,Volume v,long domesticPotential,int hours)
    {
        foreach(var channel in Enum.GetValues<ReleaseChannel>())
        {
            var a=ActiveChannel(s,v,channel);if(a is null)continue;
            if(v.SalesClosed&&!a.DirectDoujin)continue;
            var week=Monday(Clock.Now);
            if(World.Receipts.Any(r=>r.AgreementId==a.Id&&r.VolumeId==v.Id&&r.Week==week))continue;
            var units=a.DirectDoujin?DirectDownloadUnits(s,v,a):(long)Math.Floor(domesticPotential*(channel==ReleaseChannel.DomesticDigital?Math.Max(.05,DigitalPreference):.2*a.InternationalInterest/100));
            World.Receipts.Add(new(){AgreementId=a.Id,VolumeId=v.Id,Week=week,Units=0,NetYen=0});
            (v.SalesPlans??=new()).Add(new(){Kind=a.DirectDoujin?SaleKind.Download:SaleKind.Channel,AgreementId=a.Id,Week=week,Total=units,Hours=hours});
        }
    }
    private long ReleaseChannelUnits(Series s,Volume v,SalesPlan plan,long units)
    {
        var a=World.Channels.Single(x=>x.Id==plan.AgreementId);
        var income=a.DirectDoujin?checked(units*DoujinDownloadNet(v)):v.IsDoujin?(long)Math.Floor(units*v.Price*.7):SalesRules.Income(units,Economy.PriceIndex(TrendCatalog,v.ReleaseDate),false);
        if(income>0){AccountPostDaily(BusinessOf(v.BusinessId).Account,income,a.Channel==ReleaseChannel.DomesticDigital?"domestic digital receipts":"overseas licensed receipts",AccountEntryKind.Publishing,s.Id);VolumeContribution(v,income);}
        var receipt=World.Receipts.Single(r=>r.AgreementId==a.Id&&r.VolumeId==v.Id&&r.Week==plan.Week);
        receipt.Units=checked(receipt.Units+units);receipt.NetYen=checked(receipt.NetYen+income);
        if(a.Channel==ReleaseChannel.Overseas)a.InternationalInterest=Math.Clamp(a.InternationalInterest+units/1000d,0,100);
        return units;
    }
}
