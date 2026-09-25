using System.Diagnostics.CodeAnalysis;

namespace MangakaSim;

public partial class GameState
{
    private void ValidateOperationsSave(HashSet<int> ids)
    {
        static void Check([DoesNotReturnIf(false)] bool ok,string field) { if(!ok) throw new InvalidDataException($"Save file has invalid {field}."); }
        void Id(int id) => Check(id>0 && ids.Add(id),"operation id");
        bool Business(int id) => Businesses.Any(b => b.Id==id);
        bool Person(int id) => People.Any(p => p.Id==id);
        bool Date(DateTime t) => t>=GameClock.Start && t.Ticks%TimeSpan.TicksPerHour==0;
        bool Unit(double x) => double.IsFinite(x) && x is >=0 and <=100;
        Check(Bills is not null && Loans is not null && PrintRuns is not null && Bookings is not null && StaffProposals is not null && FollowerRolls is not null && FollowerDecisionHistory is not null,"operation collections");
        foreach(var p in People)
        {
            Check(Enum.IsDefined(p.OutsideJob),"outside job");
            Check(Unit(p.Food)&&Unit(p.Drink)&&Unit(p.Comfort)&&Unit(p.Happiness)&&Unit(p.Loyalty)&&p.LowMoodDays>=0&&p.LowNeedHours>=0&&p.BreakWait>=0,"wellbeing");
            Check(p.DailyOvertimeLimit is >=0 and <=2 && p.WeeklyOvertimeLimit is >=0 and <=12 && p.WeeklyOvertime is >=0 and <=12 && p.ProductiveHours>=0,"staff hours");
            Check(p.Experience is not null && p.Experience.All(e => Enum.IsDefined(e.Key)&&double.IsFinite(e.Value)&&e.Value>=0&&e.Value<240),"experience");
            Check(p.MoonlightingOverride is null || Enum.IsDefined(p.MoonlightingOverride.Value),"personal moonlighting policy");
            Check(p.RecoveryHours is >=0 and <=4 && (p.RecoveryAt is null || Date(p.RecoveryAt.Value)&&p.RecoveryAt<=Clock.Now),"recovery commission");
        }
        foreach(var b in Businesses) Check(Enum.IsDefined(b.Moonlighting)&&b.EmployerTier is >=0 and <=3,"business policy");
        foreach(var l in Locations) Check(l.PropertyTier is >=0 and <=4 && l.BreakSeats>=2 && l.BreakSeats<=1002 && l.Storage>=0 && l.Deposit>=0 &&
            l.Closed==l.ClosedAt.HasValue && TokyoProperties.Centres.ContainsKey(l.District),"property");
        foreach(var s in Series)
        {
            Check(s.PipelineLimit is >=1 and <=3 && s.BufferLimit is >=0 and <=4 && s.MasterLimit is >=1 and <=3,"pipeline limits");
            Check(Unit(s.Reach)&&s.Reach<=20 && s.PromotionHours is >=0 and <=4 && s.CampaignHours is >=0 and <=8 &&
                (s.PromoterId is null || Person(s.PromoterId.Value)) && s.PrintTarget is >=1 and <=5000 && s.PrintTierMask is >=1 and <=7 && s.PrintBudget>=0,"outreach and printing settings");
            foreach(var chapter in s.Chapters) Check((chapter.PublishedAt is null)==(chapter.PublishedBusinessId is null) && (chapter.PublishedBusinessId is null || Business(chapter.PublishedBusinessId.Value)),"publication business");
            foreach(var v in s.Volumes)
            {
                Check(Business(v.BusinessId)&&v.CreatorShares is {Count:>0} && v.CreatorShares.All(p => Person(p.Key)&&p.Value>0)&&v.CreatorShares.Values.Sum()==v.ChapterIds.Count,"book entitlement");
                Check(v.PrintedPages>=4 && v.PrintedPages%4==0 && v.Price>=300 && v.CreatorAccrued>=0 && v.WeeklyDemand>=0,"book production");
                if(v.IsDoujin) Check(v.CopiesSold+PrintRuns.Where(r => r.VolumeId==v.Id).Sum(r => r.Remaining)==PrintRuns.Where(r => r.VolumeId==v.Id).Sum(r => (long)r.Quantity),"physical stock reconciliation");
            }
        }
        foreach(var b in Bills)
        {
            Check(b is not null,"bill"); Id(b.Id);
            Check(Business(b.BusinessId)&&(b.PersonId is null || Person(b.PersonId.Value))&&b.Original>0&&b.Remaining>=0&&b.Remaining<=b.Original&&Date(b.DueAt)&&b.DueAt<=Clock.Now.AddMonths(1)&&!string.IsNullOrWhiteSpace(b.Reason),"bill details");
            Check(b.LocationId is null || Locations.Any(l => l.Id==b.LocationId&&l.BusinessId==b.BusinessId),"bill location");
        }
        foreach(var l in Loans)
        {
            Check(l is not null,"loan"); Id(l.Id);
            Check((l.BusinessId is null || Business(l.BusinessId.Value))&&Person(l.PersonId)&&l.ScheduledPrincipalDue>=0&&l.ScheduledPrincipalDue<=l.Original&&l.Original>0&&l.Principal>=0&&l.Principal<=l.Original&&l.Interest>=0&&l.Arrears>=0&&l.Apr is .08m or .24m && l.Term is 12 or 24&&Date(l.NextPayment),"loan terms");
            Check(l.InterestPaid>=0,"paid interest");
        }
        foreach(var group in Loans.GroupBy(l=>(l.BusinessId,l.PersonId)))
        {
            var account=group.Key.BusinessId is { } business ? BusinessOf(business).Account : FindPerson(group.Key.PersonId)!.PersonalAccount;
            Check(account.Entries.Where(e=>e.Reason=="loan advance").Sum(e=>e.Amount)==group.Sum(l=>l.Original) &&
                -account.Entries.Where(e=>e.Reason=="loan repayment").Sum(e=>e.Amount)==group.Sum(l=>l.Original-l.Principal+l.InterestPaid),"loan reconciliation");
        }
        foreach(var candidate in Candidates) Check(candidate.IntroductionBusinessId is null || Business(candidate.IntroductionBusinessId.Value),"introduction sponsor business");
        foreach(var r in PrintRuns)
        {
            Check(r is not null,"print run"); Id(r.Id);
            var v=Series.SelectMany(s => s.Volumes).SingleOrDefault(v => v.Id==r.VolumeId);
            Check(v is not null && v.IsDoujin && v.BusinessId==r.BusinessId && Locations.Any(l => l.Id==r.LocationId&&l.BusinessId==r.BusinessId)&&Enum.IsDefined(r.Tier)&&
                r.Quantity>0&&r.Remaining>=0&&r.Remaining<=r.Quantity&&r.Cost==PrintingCost(r.Tier,v.PrintedPages,r.Quantity)&&Date(r.OrderedAt)&&r.OrderedAt<=Clock.Now&&r.DueAt>r.OrderedAt&&r.Delivered==(r.DueAt<=Clock.Now),"print run details");
        }
        foreach(var b in Bookings)
        {
            Check(b is not null,"booking"); Id(b.Id);
            Check(b.ReservedStock is not null&&b.ReservedStock.All(x=>x.Key>0&&x.Value>0),"reserved convention stock");
            if(!b.Cancelled&&!b.Settled)
                Check(b.ReservedStock!.All(x=>PrintRuns.Any(r=>r.Id==x.Key&&r.BusinessId==b.BusinessId&&
                    (b.SeriesId is null||FindSeries(b.SeriesId.Value)!.Volumes.Any(v=>v.Id==r.VolumeId))&&
                    (r.Delivered||r.DueAt<=b.Date.AddHours(11))&&ReservedFromRun(r.Id)<=r.Remaining)),"available convention stock");
            Check(b.CopiesSold>=0&&(b.SeriesId is null||FindSeries(b.SeriesId.Value) is not null),"booking title");
            Check(Business(b.BusinessId)&&Person(b.PersonId)&&(b.AssistantId is null || Person(b.AssistantId.Value)&&b.AssistantId!=b.PersonId)&&Date(b.Date)&&b.Scale is >=0 and <=2&&b.Fee>=0&&b.TravelCost>=0&&b.TravelHours>=1&&b.StaffedHours>=0&&TokyoProperties.Centres.ContainsKey(b.District),"booking details");
        }
        foreach(var p in StaffProposals) { Check(p is not null,"proposal"); Id(p.Id); Check(Person(p.PersonId)&&Business(p.BusinessId)&&Date(p.ExpiresAt)&&p.ExpiresAt>Clock.Now&&!string.IsNullOrWhiteSpace(p.Title),"proposal details"); }
        Check(UncleEvents is >=0 and <=2 && (LastUncleEvent is null || Date(LastUncleEvent.Value)&&LastUncleEvent<=Clock.Now),"rare introduction history");
        Check(FollowerRolls.All(p => Person(p.Key)&&double.IsFinite(p.Value)&&p.Value is >=0 and <1),"follower decisions");
        Check((CareerQuote is null)==(CareerQuoteExpires is null) && (CareerQuote is null || CareerQuote.Action is StudioAction.CareerHome or StudioAction.CareerStudio or StudioAction.CareerEmployer),"career quote");
        Check(FollowerDecisionHistory.All(p=>!string.IsNullOrWhiteSpace(p.Key)&&double.IsFinite(p.Value)&&p.Value is >=0 and <1),"saved follower outcomes");
        Check(PendingCareer is null || PendingCareer.Action is StudioAction.CareerHome or StudioAction.CareerStudio or StudioAction.CareerEmployer,"career move");
    }
}
