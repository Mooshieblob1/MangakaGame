using MangakaSim.Rules;

namespace MangakaSim;

public sealed record PublishDoujinOnlineCommand(int VolumeId) : ICommand;

public partial class GameState
{
    public static long DoujinDownloadPrice(Volume book)=>Math.Max(1,book.Price/2);
    public static long DoujinDownloadNet(Volume book)=>(long)decimal.Floor(DoujinDownloadPrice(book)*.7m);
    private ChannelAgreement? DirectDownload(Volume book)=>World.Channels.LastOrDefault(a=>a.DirectDoujin&&
        a.BusinessId==book.BusinessId&&a.Status==NegotiationStatus.Accepted&&a.VolumeIds.Contains(book.Id));
    public bool DoujinOnlineListed(int volumeId)
    {
        var (series,book)=FindVolume(volumeId);
        return World.Channels.Any(a=>a.SeriesId==series.Id&&a.BusinessId==book.BusinessId&&a.Channel==ReleaseChannel.DomesticDigital&&
            a.Status==NegotiationStatus.Accepted&&(a.VolumeIds.Contains(book.Id)||!a.DirectDoujin&&series.BusinessId==a.BusinessId&&book.ReleaseDate>=a.CreatedAt));
    }
    public long DoujinDownloadsSold(int volumeId)=>World.Receipts.Where(r=>r.VolumeId==volumeId&&
        World.Channels.Any(a=>a.Id==r.AgreementId&&a.Channel==ReleaseChannel.DomesticDigital)).Sum(r=>r.Units);
    private void PublishDoujinOnline(PublishDoujinOnlineCommand command)
    {
        var (series,book)=FindVolume(command.VolumeId);RequireSeries(series.Id);
        if(!book.IsDoujin||book.BusinessId!=ControlledBusinessId)throw new InvalidCommandException("Choose a finished doujin edition owned by this business.");
        if(DoujinOnlineListed(book.Id)||World.Channels.Any(a=>a.SeriesId==series.Id&&a.BusinessId==book.BusinessId&&
            a.Channel==ReleaseChannel.DomesticDigital&&a.Status==NegotiationStatus.Pending))
            throw new InvalidCommandException("This edition is already online or has a digital agreement under review.");
        World.Channels.Add(new(){Id=AllocateId(),SeriesId=series.Id,BusinessId=book.BusinessId,Channel=ReleaseChannel.DomesticDigital,
            DirectDoujin=true,CreatedAt=Clock.Now,ResolvesAt=Clock.Now,Status=NegotiationStatus.Accepted,Cost=0,VolumeIds=[book.Id],Reason="Direct download shop · no upfront fee"});
        if(book.ReleasedAt is null)ReleaseVolume(series,book);
        else{ // already selling: the listing's downloads start this week, not next Monday (streaming sales, spec 2026-10-03)
            var hours=Math.Max(SalesRules.MinimumFirstWeekHours,SalesRules.ShopHoursUntil(Clock.Now,Monday(Clock.Now).AddDays(7)));
            PlanChannelDemand(series,book,0,hours);
        }
        TimelineNews($"{series.Title} · {EditionName(book)} is on sale online. ¥{DoujinDownloadPrice(book):N0} per download; ¥{DoujinDownloadNet(book):N0} to the business after the shop's 30% fee. No upfront charge. Downloads sell through the day.");
    }
    private long DirectDownloadUnits(Series series,Volume book,ChannelAgreement listing)
    {
        // Download weeks count from the listing's first planned week: the listing plans week 1 at once (streaming sales,
        // spec 2026-10-03), so the next Monday is week 2. Older saves first planned on the Monday after listing: same ages.
        var first=World.Receipts.Where(r=>r.AgreementId==listing.Id&&r.VolumeId==book.Id).Select(r=>r.Week).DefaultIfEmpty(Monday(Clock.Now)).Min();
        var age=Math.Max(1,1+(int)((Monday(Clock.Now)-first).TotalDays/7));
        var demand=SalesRules.DoujinCopies(series.Fanbase,book.AverageQuality,series.IsIconic?1:GenrePopularity(series.Genre),0,age);
        // Small early online audience; later historical adoption expands reach.
        var share=Math.Max(.15,DigitalPreference);
        return (long)Math.Floor(demand*share*(age>8?.1:1)*(1+series.Reach/100)*RivalDemand(series.Genre,book.AverageQuality)*RecognitionLift(series.Id));
    }
}
