using MangakaSim.Rules;

namespace MangakaSim;

public sealed record DoujinDistribution(string Status,string LocalSales,string Channels,int Stock,long Sold,DateTime? NextCheck);

public partial class GameState
{
    /// <summary>Presentation query only; unlike settlement, it never registers editions or rolls demand.</summary>
    public DoujinDistribution DescribeDoujin(int volumeId)
    {
        var (series,book)=FindVolume(volumeId);var stock=Stock(volumeId);
        var pending=PrintRuns.FirstOrDefault(r=>r.VolumeId==volumeId&&!r.Delivered);
        string status=book.SalesClosed?"Local sales window ended":stock>0&&book.ReleasedAt is not null?"On sale locally":
            pending is not null?"Printing · awaiting delivery":book.ReleasedAt is null?"Ready to print":
            PrintRuns.Any(r=>r.VolumeId==volumeId&&r.Delivered)?"Sold out · reprint to resume local sales":"No physical stock · print to start local sales";
        var from=pending is not null&&stock==0&&pending.DueAt>Clock.Now?pending.DueAt:Clock.Now;
        // Copies delivered on a shop hour sell on that very tick, so the first release is at or after the delivery, not after it.
        var next=SalesRules.NextShopHour(from==Clock.Now?from:from.AddHours(-1));
        string local=book.SalesClosed?"Automatic local sales have ended for this book. Reprinting does not reopen that window; remaining copies can still be sold at conventions.":
            book.ReleasedAt is null&&pending is null?"No physical copies distributed yet. Order copies here. Delivery automatically starts local distribution; no separate distribute action is needed.":
            stock==0&&pending is null?"No stock available. Order more copies to supply the remaining local sales window.":
            $"{(stock>0?"Local distribution is active":"Local distribution starts when copies arrive")}. Shops sell through the day, 10:00 to 20:00. Demand determines how many sell; a sale is not guaranteed.";
        var reserved=ConventionReserved(volumeId);
        if(reserved>0)local+=$"\n{reserved:N0} copies reserved for conventions (including pending deliveries); {stock-ConventionReserved(volumeId,true):N0} currently available for regular sales.";
        if(pending is not null)local+=$"\n{pending.Quantity:N0} copies due {pending.DueAt:ddd d MMM · HH:mm}.";
        if(!book.SalesClosed)local+=$"\nSales weeks remaining: {Math.Max(0,book.SalesWindowWeeks-book.WeeksOnSale)}.";
        string Channel(ReleaseChannel channel)
        {
            var agreements=World.Channels.Where(a=>a.SeriesId==series.Id&&a.BusinessId==book.BusinessId&&a.Channel==channel&&
                (a.VolumeIds.Contains(book.Id)||!a.DirectDoujin&&series.BusinessId==a.BusinessId&&book.ReleaseDate>=a.CreatedAt));
            var agreement=agreements.LastOrDefault(a=>a.Status==NegotiationStatus.Accepted)??agreements.LastOrDefault(a=>a.Status==NegotiationStatus.Pending);
            if(agreement?.DirectDoujin==true)return $"on sale online · {DoujinDownloadsSold(book.Id):N0} downloads sold";
            return agreement?.Status==NegotiationStatus.Accepted?(book.SalesClosed?"agreement held; sales window ended":"agreement active"):
                agreement?.Status==NegotiationStatus.Pending?"proposal pending":"not arranged";
        }
        if(DirectDownload(book) is not null)status=stock>0&&!book.SalesClosed?"On sale locally & online":"On sale online";
        return new(status,local,$"Digital: {Channel(ReleaseChannel.DomesticDigital)}\nOverseas: {Channel(ReleaseChannel.Overseas)}",stock,book.CopiesSold,
            !book.SalesClosed&&(stock>0||pending is not null)?next:null);
    }
}
