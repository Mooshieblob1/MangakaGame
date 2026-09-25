using MangakaSim.Catalog;
namespace MangakaSim;

public partial class GameState
{
    private bool IsHistoricalAssistant(int id) => World.Assistants.Any(a=>a.PersonId==id);
    private HistoricalRival? HistoricalFiller(int id) => World.Rivals.FirstOrDefault(r=>r.FillerId==id);
    private void TimelineNews(string text, bool decision=false)
    {
        World.News.Add(new(Clock.Now,text,World.SimulatedFuture));
        if(World.News.Count>500)World.News.RemoveRange(0,World.News.Count-500);
        Emit(decision?EventType.IndustryDecision:EventType.IndustryNews,text);
    }
    private void InitializeTimeline(bool seedBusinesses=true)
    {
        World=new(){MarketRng=Rng.FromSeed(unchecked(RngSeed^0x574f524c)),HiringRng=Rng.FromSeed(unchecked(RngSeed^0x52495641)),FutureRng=Rng.FromSeed(unchecked(RngSeed^0x46555452))};
        foreach(var c in TimelineCatalog.Default.Creators)World.Assistants.Add(new(){Key=c.Id,HireRoll=World.HiringRng.NextDouble()});
        foreach(var r in TimelineCatalog.Default.Rivals.Where(r=>r.Start<=Clock.Now))
        {
            World.Processed.Add("launch:"+r.Id);
            if(r.End<=Clock.Now){World.Processed.Add("end:"+r.Id);World.Rivals.Add(new(){Key=r.Id,Phase=RivalPhase.Ended,EndedAt=r.End});continue;}
            LaunchRival(r,false);
            if(r.Hiatus<=Clock.Now){World.Processed.Add("hiatus:"+r.Id);World.Rivals.Single(x=>x.Key==r.Id).Phase=RivalPhase.Hiatus;}
        }
        foreach(var e in TimelineCatalog.Default.Events.Where(e=>e.At<=Clock.Now))World.Processed.Add("industry:"+e.Id);
        World.SimulatedFuture=Clock.Now>=TimelineCatalog.Cutoff;
        World.LastWeek=Monday(Clock.Now);World.LastMonth=new(Clock.Now.Year,Clock.Now.Month,1);
        World.LastQuarter=new(Clock.Now.Year,1+3*((Clock.Now.Month-1)/3),1);
        if(seedBusinesses)SeedRivalStudios();
        UpdateRivalPopularity();
    }
    private void LaunchRival(RivalDefinition definition,bool announce)
    {
        var market=Markets.Single(m=>m.MagazineId==definition.Magazine);
        var filler=market.Fillers.Where(f=>HistoricalFiller(f.Id) is null).OrderBy(f=>f.Popularity).ThenBy(f=>f.Id).FirstOrDefault()
            ??throw new InvalidDataException("Historical catalog exceeds the available magazine slots.");
        var index=market.Fillers.IndexOf(filler);market.RetiredFillers.Add(filler);
        var fresh=new FillerSeries{Id=AllocateId(),Title=definition.Title,Genre=definition.Genre,Popularity=55};
        market.Fillers[index]=fresh;
        World.Rivals.Add(new(){Key=definition.Id,FillerId=fresh.Id});
        if(announce)TimelineNews($"New serialization: {definition.Title}."+(definition.Precision=="day"?"":" (Approximate historical date.)"));
    }
    private void EndHistorical(HistoricalRival rival,bool future=false)
    {
        if(rival.Phase==RivalPhase.Ended)return;
        var definition=TimelineCatalog.Default.Rivals.Single(d=>d.Id==rival.Key);
        var market=Markets.Single(m=>m.MagazineId==definition.Magazine);
        var filler=market.Fillers.Single(f=>f.Id==rival.FillerId);var index=market.Fillers.IndexOf(filler);
        market.RetiredFillers.Add(filler);
        market.Fillers[index]=NewTimelineFiller(definition.Magazine);
        rival.Phase=RivalPhase.Ended;rival.EndedAt=future?Clock.Now:definition.End;rival.LastFutureEvent=future?Clock.Now:null;
        TimelineNews($"{definition.Title} concludes its serialization."+(future?" Simulated future outcome.":definition.Precision=="day"?"":" (Approximate historical date.)"));
    }
    private FillerSeries NewTimelineFiller(string magazine)
    {
        var genres=TrendCatalog.Genres.Where(g=>g!="other").ToArray();
        return new(){Id=AllocateId(),Title=PublisherCatalog.Adjectives[World.MarketRng.NextInt(PublisherCatalog.Adjectives.Count)]+" "+PublisherCatalog.Nouns[World.MarketRng.NextInt(PublisherCatalog.Nouns.Count)],Genre=genres[World.MarketRng.NextInt(genres.Length)],Popularity=World.MarketRng.NextInt(45,66)};
    }
    internal void TimelineMarketStep()
    {
        foreach(var d in TimelineCatalog.Default.Rivals.OrderBy(d=>d.Start).ThenBy(d=>d.Id,StringComparer.Ordinal))
        {
            if(d.Start<=Clock.Now&&World.Processed.Add("launch:"+d.Id))LaunchRival(d,true);
            if(d.End<=Clock.Now&&World.Processed.Add("end:"+d.Id))EndHistorical(World.Rivals.Single(r=>r.Key==d.Id));
            if(d.Hiatus<=Clock.Now&&World.Processed.Add("hiatus:"+d.Id))
            {World.Rivals.Single(r=>r.Key==d.Id).Phase=RivalPhase.Hiatus;TimelineNews($"{d.Title} pauses its serialization.");}
        }
        foreach(var e in TimelineCatalog.Default.Events.OrderBy(e=>e.At).ThenBy(e=>e.Id,StringComparer.Ordinal))
            if(e.At<=Clock.Now&&World.Processed.Add("industry:"+e.Id))TimelineNews(e.Message+(e.Precision=="day"?"":" (Approximate historical date.)"));
        if(!World.SimulatedFuture&&Clock.Now>=TimelineCatalog.Cutoff)
        {World.SimulatedFuture=true;TimelineNews("Researched history has ended. From now on, industry developments and rival outcomes are simulated.");}
        UpdateRivalPopularity();
    }
    private void UpdateRivalPopularity()
    {
        foreach(var r in World.Rivals.Where(r=>r.Phase!=RivalPhase.Ended))
        {
            var d=TimelineCatalog.Default.Rivals.Single(d=>d.Id==r.Key);
            var f=Markets.Single(m=>m.MagazineId==d.Magazine).Fillers.Single(f=>f.Id==r.FillerId);
            f.Popularity=r.Phase==RivalPhase.Hiatus?5:Math.Clamp(55+35*Math.Clamp((Clock.Now-d.Start).TotalDays/730,0,1),5,100);
        }
    }
    public double RivalDemand(string genre,double quality)
    {
        var key=MangakaSim.Rules.TrendRules.Normalise(genre,TrendCatalog);
        var intensity=World.Rivals.Sum(r=>{
            var d=TimelineCatalog.Default.Rivals.Single(d=>d.Id==r.Key);
            if(d.Genre!=key||r.Phase==RivalPhase.Hiatus)return 0;
            var grown=Math.Clamp((Clock.Now-(d.Breakthrough??d.Start.AddYears(1))).TotalDays/183,0,1);
            return grown*(r.EndedAt is {} end?Math.Clamp(1-(Clock.Now-end).TotalDays/365,0,1):1);
        });
        return Math.Clamp(1+Math.Min(.25,.12*intensity)*Math.Clamp((quality-40)/40,0,1)-Math.Min(.15,.08*intensity),.85,1.25);
    }
    private void SeedRivalStudios()
    {
        foreach(var (name,index) in new[]{("Hinode Atelier",0),("Paper Crane Studio",1),("Kobato Works",2)})
        {
            var b=new Business{Id=AllocateId(),Name=name,FoundedAt=Clock.Now,Independent=true,EmployerTier=2,Account=new(){OpeningBalance=3000000,Balance=3000000}};
            Businesses.Add(b);World.RivalBusinesses.Add(b.Id);
            var location=new StudioLocation{Id=AllocateId(),BusinessId=b.Id,Name=name,District=new[]{"Nakano","Bunkyo","Suginami"}[index],Seats=4,IsFamilyHome=false,MonthlyRent=120000,NextRentAt=Clock.Now.Date.AddMonths(1),Storage=3000};
            Locations.Add(location);
            for(var i=0;i<3;i++)
            {
                var person=new Person{Id=AllocateId(),Name=new[]{"Kei","Natsumi","Jun"}[i]+" "+new[]{"Arai","Mori","Sakai"}[index],ExpectedSalary=160000,Reputation=20,
                    Schedule=new(){WorkStartHour=9,WorkEndHour=18,DaysOff=[DayOfWeek.Saturday,DayOfWeek.Sunday]}};
                foreach(var stage in StageOrder.All)person.Skills[stage]=World.HiringRng.NextInt(35,71);
                person.ExpectedSalary=StudioRules.ExpectedSalary(person.Skills.Values.Max());
                person.EmploymentHistory.Add(new(){BusinessId=b.Id,LocationId=location.Id,StartsAt=Clock.Now,MonthlySalary=person.ExpectedSalary});People.Add(person);
            }
            InitializeOffice(location);
            FundOfficeFor(location,location.Seats,true);
        }
        RefreshOfficeAssignments();
    }
    internal void TimelineStaffStep()
    {
        foreach(var a in World.Assistants)
        {
            var d=TimelineCatalog.Default.Creators.Single(c=>c.Id==a.Key);
            var p=a.PersonId is {} id?FindPerson(id):null;
            if(p is not null&&!a.Departed&&p.Employment is null){a.Departed=true;a.Relationship=p.Loyalty;}
            if(!a.Departed&&p?.Employment is {} job)
            {
                foreach(var days in new[]{30,7})
                    if(Clock.Now>=d.Departure.AddDays(-days)&&World.Processed.Add($"warning:{d.Id}:{days}")&&job.BusinessId==ControlledBusinessId)
                        TimelineNews($"{p.Name}'s fixed contract ends {d.Departure:d MMM yyyy}.",days==7);
                if(Clock.Now>=d.Departure)
                {
                    a.Relationship=p.Loyalty;a.ContactBusiness=job.BusinessId;a.Departed=true;
                    LeaveJob(p);p.MainSeriesId=null;
                    TimelineNews($"{p.Name} leaves for their own career. Past contributions and earned wages remain credited.");
                }
            }
        }
        var month=new DateTime(Clock.Now.Year,Clock.Now.Month,1);
        if(World.LastMonth!=month)
        {
            World.LastMonth=month;
            foreach(var id in World.RivalBusinesses)
            {
                var staff=People.Where(p=>p.Employment?.BusinessId==id).ToArray();
                if(!Locations.Any(l=>l.BusinessId==id&&!l.Closed)||staff.Length==0)continue;
                var income=1000000+(long)(10000*staff.Average(p=>p.Skills.Values.Average()));
                AccountPost(BusinessOf(id).Account,income,"rival commission portfolio",AccountEntryKind.Publishing);
            }
            foreach(var key in World.Relations.Keys.ToArray())World.Relations[key]=Math.Min(50,World.Relations[key]+1);
            foreach(var a in World.Assistants.Where(a=>a.Departed&&a.Relationship>=60&&a.ContactBusiness==ControlledBusinessId&&
                (a.LastContact is null||a.LastContact.Value.AddMonths(3)<=Clock.Now)))
                if(World.HiringRng.NextDouble()<.05)
                {
                    a.LastContact=Clock.Now;
                    var candidate=GenerateCandidate(true);candidate.IntroductionBusinessId=ControlledBusinessId;Candidates.Add(candidate);
                    TimelineNews($"{FindPerson(a.PersonId!.Value)!.Name} introduces a promising assistant. They are in your candidate pool for fourteen days.",true);
                    DiscoverAssistant(ControlledBusinessId,.3);
                }
        }
        var week=Monday(Clock.Now);
        if(World.LastWeek!=week)
        {World.LastWeek=week;if(ControlledStaff.Any(p=>p.Id!=ProtagonistPersonId&&p.Loyalty>=60))DiscoverAssistant(ControlledBusinessId,.03);GenerateRivalOffer();}
        ResolveScouting();ResolveStaffOffers();ResolveChannels();
        var quarter=new DateTime(Clock.Now.Year,1+3*((Clock.Now.Month-1)/3),1);
        if(World.SimulatedFuture&&World.LastQuarter!=quarter)
        {
            World.LastQuarter=quarter;
            foreach(var r in World.Rivals.Where(r=>r.Phase!=RivalPhase.Ended).ToArray())
            {
                if(r.Phase==RivalPhase.Hiatus&&World.FutureRng.NextDouble()<.25)
                {r.Phase=RivalPhase.Active;r.LastFutureEvent=Clock.Now;TimelineNews($"{TimelineCatalog.Default.Rivals.Single(d=>d.Id==r.Key).Title} returns. Simulated future outcome.");}
                else if(Clock.Now>=TimelineCatalog.Cutoff.AddYears(1)&&(r.LastFutureEvent is null||r.LastFutureEvent.Value.AddYears(1)<=Clock.Now)&&World.FutureRng.NextDouble()<.05)EndHistorical(r,true);
            }
            var market=Markets[World.FutureRng.NextInt(Markets.Count)];
            var filler=market.Fillers.Where(f=>HistoricalFiller(f.Id) is null).OrderBy(f=>f.Popularity).FirstOrDefault();
            if(filler is not null)
            {var index=market.Fillers.IndexOf(filler);market.RetiredFillers.Add(filler);var next=NewTimelineFiller(market.MagazineId);market.Fillers[index]=next;TimelineNews($"New simulated rival: {next.Title}. Interest in {next.Genre} rises slightly.");
                var trend=Trends.Single(t=>t.Genre==next.Genre);trend.Noise=Math.Min(.15,trend.Noise+.02);}
        }
    }
    private void DiscoverAssistant(int business,double chance)
    {
        foreach(var a in World.Assistants.Where(a=>a.PersonId is null&&a.DiscoveredBy is null&&!a.Departed))
        {
            var d=TimelineCatalog.Default.Creators.Single(c=>c.Id==a.Key);
            if(Clock.Now<d.Start||Clock.Now>=d.Departure||World.HiringRng.NextDouble()>=chance)continue;
            a.DiscoveredBy=business;
            if(business==ControlledBusinessId)TimelineNews($"A connection introduces {d.Name}, available for a temporary assistant contract until {d.Departure:d MMM yyyy}.",true);
        }
    }
    private double HistoricalMentoring(Person student,Stage stage)
    {
        if(student.CurrentTask is null)return 1;
        var mentor=People.Where(p=>p.Id!=student.Id&&IsHistoricalAssistant(p.Id)&&p.Employment?.LocationId==student.Employment?.LocationId&&
            p.HoursWorkedToday>0&&PersonAvailable(p)&&p.CurrentTask is not null&&p.Skill(stage)>student.Skill(stage)&&IsWorkingHour(p,TickStart,out _)).OrderBy(p=>p.Id).FirstOrDefault();
        if(mentor is null)return 1;
        if(!World.Mentoring.TryGetValue(mentor.Id,out var today)||today.Day!=TickStart.Date)
            World.Mentoring[mentor.Id]=today=new(TickStart.Date,student.Id);
        return today.LearnerId==student.Id?1.1:1;
    }
}
