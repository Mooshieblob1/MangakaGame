namespace MangakaSim;

public partial class GameState
{
    public DateTime NextConvention(int scale)
    {
        if (scale is < 0 or > 2) throw new InvalidCommandException("Choose a local, regional or major convention.");
        for (var day=Clock.Now.Date.AddDays(2);day<Clock.Now.AddDays(370);day=day.AddDays(1))
        {
            if (scale==2 && day>=Clock.Now.Date.AddDays(7) && ((day.Month==8 && day.Day==(day.Year==1996?3:10)) || day.Month==12 && day.Day==28)) return day;
            if (scale==0 && day.DayOfWeek is DayOfWeek.Wednesday or DayOfWeek.Sunday || scale==1 && day.DayOfWeek==DayOfWeek.Sunday) return day;
        }
        throw new InvalidCommandException("No upcoming convention.");
    }
    private void OutreachAction(StudioActionCommand c)
    {
        if (c.Action==StudioAction.CancelConvention)
        {
            var b=Bookings.SingleOrDefault(b => b.Id==c.Target && b.BusinessId==ControlledBusinessId && !b.Settled && !b.Cancelled) ?? throw new InvalidCommandException("Choose an active booking.");
            CancelBooking(b); return;
        }
        if (c.Action==StudioAction.BookConvention)
        {
            if(c.Amount<0||c.Amount>int.MaxValue)throw new InvalidCommandException("Choose a valid title.");
            var title=c.Amount==0?null:RequireSeries((int)c.Amount);
            var person=RequirePerson(c.Target);
            var assistant=c.Secondary==0?null:RequirePerson(c.Secondary);
            var date=NextConvention(c.Value);
            if (date>Clock.Now.Date.AddDays(28)) throw new InvalidCommandException($"Booking opens 28 days before {date:d MMM}.");
            if (person==assistant || Bookings.Any(b => !b.Cancelled && !b.Settled && b.Date==date && (b.BusinessId==ControlledBusinessId || b.PersonId==person.Id || b.AssistantId==person.Id)))
                throw new InvalidCommandException("One booth per business/date and each attendee must be different.");
            if (person.Employment!.StartsAt>Clock.Now || person.Employment.NoticeEndsAt is not null || assistant is not null && (assistant.Employment!.StartsAt>Clock.Now || assistant.Employment.NoticeEndsAt is not null))
                throw new InvalidCommandException("Attendees must be active employees.");
            var location=Locations.Single(l => l.Id==person.Employment.LocationId);
            if (assistant is not null && assistant.Employment!.LocationId!=location.Id) throw new InvalidCommandException("Choose attendees based at the same workplace.");
            var district=c.Value==0?location.District:c.Value==1?"Toshima":"Ariake";
            var route=TokyoProperties.Travel(location.District,district);
            var fee=ConventionBoothFee(c.Value); var travel=route.Fare*2*(assistant is null?1:2)*(c.Value==2?2:1);
            var reserved=AllocateConventionStock(title?.Id,date,c.ReservedCopies);
            // A free table from the Doujin Days chapter (spec 2026-10-01) pays this booking's fee.
            if(fee==0&&c.Value>0&&Goals is {FreeConventionTables:>0} goals)goals.FreeConventionTables--;
            Spend(ControlledBusinessId,fee,"convention booking"); Spend(ControlledBusinessId,travel,"staff travel");
            Bookings.Add(new(){Id=AllocateId(),BusinessId=ControlledBusinessId,PersonId=person.Id,AssistantId=assistant?.Id,SeriesId=title?.Id,Date=date,District=district,Scale=c.Value,Fee=fee,TravelCost=travel,TravelHours=route.Hours,ReservedStock=reserved}); return;
        }
        var series=RequireSeries(c.Target);
        if (c.Action==StudioAction.Promote)
        {
            if (c.Secondary==0) { series.PromoterId=null; return; }
            var person=RequirePerson(c.Secondary);
            if (person.Employment!.LocationId!=series.LocationId || Series.Any(s => s.Id!=series.Id && s.PromoterId==person.Id))
                throw new InvalidCommandException("Choose a local promoter without another promotion assignment.");
            series.PromoterId=person.Id; series.PromotionPriority=c.Enabled; return;
        }
        if (series.CampaignUntil>Clock.Now || series.PromoterId is null) throw new InvalidCommandException("Assign a promoter first; only one campaign can run at a time.");
        Spend(series.BusinessId,1000,"campaign setup"); ChargeSeriesDirect(series,1000);
        series.CampaignUntil=Clock.Now.AddDays(14); series.CampaignHours=0;
    }
    /// <summary>The booth fee for a convention scale; a free table from the Doujin Days chapter makes a paid booth free (spec 2026-10-01).</summary>
    public long ConventionBoothFee(int scale) => scale > 0 && Goals is { FreeConventionTables: > 0 } ? 0 : new long[] { 0, 5000, 8000 }[scale];
    private void CancelBooking(ConventionBooking b)
    {
        if (b.Cancelled || b.Settled) return;
        b.Cancelled=true;
        if (Clock.Now.Date<=b.Date.AddDays(-7))
        {
            AccountPost(BusinessOf(b.BusinessId).Account,b.Fee+b.TravelCost,"convention refund",AccountEntryKind.Credit);
            if(b.Fee==0&&b.Scale>0&&b.BusinessId==ControlledBusinessId&&Goals is not null)Goals.FreeConventionTables++; // the free table comes back
        }
    }
    private void ChargeSeriesDirect(Series series,long cost)
    {
        var volumes=series.Volumes.Where(v => v.BusinessId==series.BusinessId && v.ReleasedAt is not null).ToArray();
        if (volumes.Length==0) return;
        for(var i=0;i<volumes.Length;i++) volumes[i].Contribution-=cost/volumes.Length+(i==0?cost%volumes.Length:0);
    }
    private bool ConventionHour(Person person,DateTime time) => Bookings.Any(b=>!b.Settled&&!b.Cancelled&&(b.PersonId==person.Id||b.AssistantId==person.Id)&&
        time.Date>=b.Date&&time.Date<b.Date.AddDays(b.Scale==2?2:1)&&time.Hour>=11-b.TravelHours&&time.Hour<16+b.TravelHours);
    private void ConventionStep()
    {
        foreach(var b in Bookings.Where(b => !b.Settled && !b.Cancelled))
        {
            var people=new[]{FindPerson(b.PersonId),b.AssistantId is { } id?FindPerson(id):null}.Where(p => p is not null).Cast<Person>().ToArray();
            if(people.Any(p => p.Employment?.BusinessId!=b.BusinessId || p.Employment.NoticeEndsAt is not null)||b.SeriesId is {} selected&&FindSeries(selected)?.BusinessId!=b.BusinessId) { CancelBooking(b); continue; }
            var days=b.Scale==2?2:1;
            if(TickStart.Date>=b.Date && TickStart.Date<b.Date.AddDays(days) && TickStart.Hour>=11-b.TravelHours && TickStart.Hour<16+b.TravelHours)
            {
                foreach(var p in people)
                {
                    if(TickStart.Hour is >=11 and <16 && PersonAvailable(p))
                    {
                        b.StaffedHours++; p.HoursWorkedToday++;
                        if(p.Schedule.IsDayOff(TickStart)) p.Employment!.AccruedPay+=p.Employment.MonthlySalary/(40m*52/12)*1.35m;
                        NeedsChange(p,-3,-4,-3);
                    }
                    p.BusyUntil=Clock.Now; Observe(p, TickStart.Hour is >=11 and <16 ? OfficeActivityKind.Convention : OfficeActivityKind.Travel);
                }
            }
            if(Clock.Now<b.Date.AddDays(days-1).AddHours(16+b.TravelHours)) continue;
            b.Settled=true;
            var capacity=(int)Math.Floor(new[]{50,150,400}[b.Scale]*days*Math.Min(1,b.StaffedHours/(5d*people.Length*days)));
            capacity=Math.Min(capacity,100*people.Length*days);
            var books=Series.Where(s=>b.SeriesId is null||s.Id==b.SeriesId).SelectMany(s => s.Volumes.Where(v => v.BusinessId==b.BusinessId && v.IsDoujin && v.ReleasedAt is not null).Select(v => (Series:s,Volume:v))).OrderByDescending(x => PrintRuns.Any(r=>r.VolumeId==x.Volume.Id&&b.ReservedStock.ContainsKey(r.Id))).ThenByDescending(x => x.Volume.ReleasedAt).ToArray();
            if(books.Length>0) books[0].Volume.Contribution-=b.Fee+b.TravelCost;
            long sold=0;
            foreach(var (s,v) in books)
            {
                var count=SellStock(s,v,capacity-sold,true); sold+=count; v.CopiesSold+=count; s.Fanbase+=count*.2;
                RecordSales(s,v,count,false);
                if(sold>=capacity) break;
            }
            b.CopiesSold=sold;
            var promoted=b.SeriesId is {} titleId?FindSeries(titleId):null;
            var readers=promoted is null?0:(int)Math.Floor(capacity*.1);
            if(promoted is not null)promoted.Fanbase+=readers;
            StudioMessage($"Convention in {b.District}{(promoted is null?"":" · "+promoted.Title)}: {sold} copies sold; {readers} readers reached through promotion; travel ¥{b.TravelCost:N0}, booth ¥{b.Fee:N0}.");
        }
    }
    private void PromotionStep()
    {
        foreach(var s in Series.Where(s => s.PromoterId is not null))
        {
            var p=FindPerson(s.PromoterId!.Value)!;
            if(p.Employment?.BusinessId!=s.BusinessId || p.Employment.LocationId!=s.LocationId) { s.PromoterId=null; continue; }
            if(!PersonAvailable(p) || !IsWorkingHour(p,TickStart,out var overtime) || overtime || FreeCash(s.BusinessId)<500 || TeamBudgetLeft(s.BusinessId)<500) continue;
            if(!s.PromotionPriority && p.CurrentTask is { } task && IsStartable(task)) continue;
            var campaign=s.CampaignUntil>Clock.Now && s.CampaignHours<8;
            if(!campaign && s.PromotionHours>=4) continue;
            Spend(s.BusinessId,500,"promotion"); ChargeSeriesDirect(s,500);
            p.BusyUntil=Clock.Now; p.HoursWorkedToday++; Observe(p,OfficeActivityKind.Promotion);
            s.Reach=Math.Min(20,s.Reach+(.5+(p.Skill(Stage.Name)+p.Skill(Stage.Pencils))/200d)*(campaign?2:1));
            if(campaign) s.CampaignHours++; else s.PromotionHours++;
        }
        if(Clock.Hour==0 && Clock.DayOfWeek==DayOfWeek.Monday)
            foreach(var s in Series) { s.Reach*=.75; s.PromotionHours=0; }
    }
}
