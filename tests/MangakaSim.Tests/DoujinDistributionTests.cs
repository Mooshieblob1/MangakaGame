using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class DoujinDistributionTests
{
    private static GameState Book()
    {
        var s=GameState.NewGame();s.Apply(new CreateDoujinCommand("Small story","adventure"));
        for(int day=0;day<180&&s.Series[0].Volumes.Count==0;day++)s.Advance(24);
        Assert.Single(s.Series[0].Volumes);return s;
    }
    [Fact] public void Reports_order_delivery_and_sale_without_altering_the_simulation()
    {
        var s=Book();var v=s.Series[0].Volumes[0];var json=s.ToJson();
        Assert.Equal("Ready to print",s.DescribeDoujin(v.Id).Status);Assert.Null(s.DescribeDoujin(v.Id).NextCheck);
        Assert.Equal(json,s.ToJson());
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10));
        var pending=s.DescribeDoujin(v.Id);Assert.Contains("awaiting delivery",pending.Status);
        // Streaming sales (2026-10-03): the next check is the first shop hour at or after the delivery.
        var due=s.PrintRuns.Single().DueAt;var firstShopHour=due;while(!SalesRules.IsShopHour(firstShopHour))firstShopHour=firstShopHour.AddHours(1);
        Assert.True(SalesRules.IsShopHour(pending.NextCheck!.Value));Assert.Equal(firstShopHour,pending.NextCheck.Value);
        s.Advance(24);Assert.Equal("On sale locally",s.DescribeDoujin(v.Id).Status);
        var next=s.DescribeDoujin(v.Id).NextCheck!.Value;Assert.True(SalesRules.IsShopHour(next));Assert.True(next>s.Clock.Now);s.Advance((int)(next-s.Clock.Now).TotalHours);
        s.Advance(24*7); // shops now sell by the hour: a small first week still sells copies by its end
        Assert.True(v.CopiesSold>0);Assert.Equal(v.CopiesSold,s.DescribeDoujin(v.Id).Sold);
        json=s.ToJson();s.DescribeDoujin(v.Id);Assert.Equal(json,s.ToJson());
    }
    [Fact] public void Delivery_at_monday_midnight_includes_that_sales_check()
    {
        var s=Book();var v=s.Series[0].Volumes[0];
        while(s.Clock.DayOfWeek!=DayOfWeek.Sunday||s.Clock.Hour!=0)s.Advance(1);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10));
        var delivery=s.PrintRuns.Single().DueAt;Assert.Equal(DayOfWeek.Monday,delivery.DayOfWeek);
        Assert.Equal(delivery.AddHours(SalesRules.ShopOpens+1),s.DescribeDoujin(v.Id).NextCheck); // 11:00, the first shop hour at or after the delivery
        // Streaming sales (2026-10-03): delivery on the Monday tick plans that whole week (70 shop hours) at once.
        s.Advance(24);Assert.Equal(DayOfWeek.Monday,s.Clock.DayOfWeek);var plan=Assert.Single(v.SalesPlans!);
        Assert.Equal((SaleKind.Shop,s.Clock.Now,SalesRules.ShopHoursPerWeek,1),(plan.Kind,plan.Week,plan.Hours,v.WeeksOnSale));
        s.Advance(24*6+20);Assert.True(v.CopiesSold>0);Assert.Equal(10,v.CopiesSold+s.Stock(v.Id));Assert.Equal(1,v.WeeksOnSale);
    }
    [Fact] public void Sold_out_and_closed_books_do_not_claim_active_local_distribution()
    {
        var s=Book();var v=s.Series[0].Volumes[0];s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:1));
        for(int day=0;day<15&&v.CopiesSold==0;day++)s.Advance(24);
        Assert.Contains("Sold out",s.DescribeDoujin(v.Id).Status);Assert.Null(s.DescribeDoujin(v.Id).NextCheck);
        s.Advance(24*60);Assert.True(v.SalesClosed);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:1));s.Advance(24);
        Assert.Equal(1,s.Stock(v.Id));Assert.Equal("Local sales window ended",s.DescribeDoujin(v.Id).Status);
        Assert.Contains("does not reopen",s.DescribeDoujin(v.Id).LocalSales);Assert.Null(s.DescribeDoujin(v.Id).NextCheck);
    }
    [Fact] public void Channel_query_reports_eligible_agreement_without_registering_the_edition()
    {
        var s=Book();var v=s.Series[0].Volumes[0];
        var agreement=new ChannelAgreement{Id=s.AllocateId(),SeriesId=s.Series[0].Id,BusinessId=v.BusinessId,
            Channel=ReleaseChannel.DomesticDigital,CreatedAt=v.ReleaseDate,Status=NegotiationStatus.Accepted};
        s.World.Channels.Add(agreement);var json=s.ToJson();
        Assert.Contains("Digital: agreement active",s.DescribeDoujin(v.Id).Channels);
        Assert.Empty(agreement.VolumeIds);Assert.Equal(json,s.ToJson());
        v.ReleasedAt=s.Clock.Now;
        Assert.Contains("No physical stock",s.DescribeDoujin(v.Id).Status);
        Assert.Contains("Digital: agreement active",s.DescribeDoujin(v.Id).Channels);
    }
}
