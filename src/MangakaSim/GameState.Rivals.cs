using System.Text.Json;
using MangakaSim.Catalog;
namespace MangakaSim;

public partial class GameState
{
    private long TimelineReserve(int business) => World.Offers.Where(o=>o.Status==NegotiationStatus.Pending&&o.ToBusiness==business).Sum(o=>StudioRules.HiringReserve(o.Salary))+
        World.Channels.Where(c=>c.Status==NegotiationStatus.Pending&&c.BusinessId==business).Sum(c=>c.Cost);
    private int FreeTimelineDesks(int location) => UsableWorkspaces(location)-People.Count(p=>p.Employment?.LocationId==location)-
        World.Offers.Count(o=>o.Status==NegotiationStatus.Pending&&o.LocationId==location);
    private void CancelFormerStudioCommitments(int business)
    {
        foreach(var offer in World.Offers.Where(o=>o.Status==NegotiationStatus.Pending&&o.PlayerApproach&&o.ToBusiness==business))
        {offer.Status=NegotiationStatus.Cancelled;offer.Outcome="Approach withdrawn after your career move";}
        foreach(var channel in World.Channels.Where(c=>c.Status==NegotiationStatus.Pending&&c.BusinessId==business))
        {channel.Status=NegotiationStatus.Cancelled;channel.Reason="Proposal withdrawn after your career move";}
    }
    private void ApplyTimeline(TimelineCommand command)
    {
        if(!Enum.IsDefined(command.Action))throw new InvalidCommandException("Unknown industry action.");
        var probe=JsonSerializer.Deserialize<GameState>(ToJson(),JsonOptions)!;
        probe.ExecuteTimeline(command);
        ExecuteTimeline(command);
    }
    private void ExecuteTimeline(TimelineCommand c)
    {
        if(c.Action is TimelineAction.RequestDigital or TimelineAction.RequestOverseas){RequestChannel(c);return;}
        if(c.Action==TimelineAction.HireAssistant){HireHistorical(c);return;}
        if(c.Action==TimelineAction.Scout)
        {
            var target=RecruitableRival(c.Target);
            if(World.Reports.Any(r=>r.PersonId==target.Id&&r.BusinessId==ControlledBusinessId&&(r.ObservedAt is null||r.ObservedAt.Value.AddDays(30)>Clock.Now)))
                throw new InvalidCommandException("A scouting report is underway or still current.");
            Spend(ControlledBusinessId,10000,"rival scouting");
            World.Reports.RemoveAll(r=>r.PersonId==target.Id&&r.BusinessId==ControlledBusinessId);
            World.Reports.Add(new(){PersonId=target.Id,BusinessId=ControlledBusinessId,ReadyAt=Clock.Now.AddDays(3)});return;
        }
        if(c.Action==TimelineAction.Approach)
        {
            var target=RecruitableRival(c.Target);
            var location=Locations.SingleOrDefault(l=>l.Id==c.LocationId&&l.BusinessId==ControlledBusinessId&&!l.Closed)
                ??throw new InvalidCommandException("Select an open workplace in your business.");
            if(Control==ControlMode.EmployedLead&&(location.Id!=Protagonist.Employment!.LocationId||c.Salary>TeamBudgetLeft(ControlledBusinessId)))
                throw new InvalidCommandException("The employer cannot approve this salary within your remaining team budget.");
            ValidateApproach(target,location,c.Salary);
            Spend(ControlledBusinessId,20000,"rival recruitment approach");
            if(FreeCash(ControlledBusinessId)<StudioRules.HiringReserve(c.Salary))throw new InvalidCommandException("Reserve seven days of wages after the approach fee.");
            AddStaffOffer(target,location,c.Salary,true);return;
        }
        var offer=World.Offers.SingleOrDefault(o=>o.Id==c.Target&&o.Status==NegotiationStatus.Pending&&(o.FromBusiness==ControlledBusinessId||o.ToBusiness==ControlledBusinessId))
            ??throw new InvalidCommandException("That offer is no longer open to your business.");
        if(c.Action==TimelineAction.CancelOffer)
        {
            if(offer.ToBusiness!=ControlledBusinessId||!offer.PlayerApproach)throw new InvalidCommandException("You can only cancel your own recruitment approach.");
            offer.Status=NegotiationStatus.Cancelled;offer.Outcome="Approach withdrawn";return;
        }
        if(c.Action==TimelineAction.Retain)
        {
            if(offer.FromBusiness!=ControlledBusinessId||offer.PersonId==ProtagonistPersonId)throw new InvalidCommandException("Select an offer to one of your employees.");
            var person=RequirePerson(offer.PersonId);
            if(c.Salary<person.Employment!.MonthlySalary||c.Salary>person.ExpectedSalary*2)throw new InvalidCommandException("A retention offer must raise salary, up to twice the expected rate.");
            if(FreeCash(ControlledBusinessId)<StudioRules.HiringReserve(c.Salary))throw new InvalidCommandException("Keep seven days of the proposed salary available.");
            if(Control==ControlMode.EmployedLead&&(person.Employment.LocationId!=Protagonist.Employment!.LocationId||c.Salary-person.Employment.MonthlySalary>TeamBudgetLeft(ControlledBusinessId)))
                throw new InvalidCommandException("Your employer declined the raise: it exceeds this team's authority or budget.");
            if(Control==ControlMode.EmployedLead)ControlledBusiness.DiscretionarySpent+=c.Salary-person.Employment.MonthlySalary;
            person.Employment.MonthlySalary=c.Salary;TimelineNews($"{person.Name}'s salary is now ¥{c.Salary:N0}/month. Their decision is still pending.");return;
        }
        if(offer.PersonId!=ProtagonistPersonId||offer.FromBusiness!=ControlledBusinessId)throw new InvalidCommandException("This is not your career offer.");
        if(c.Action==TimelineAction.DeclineCareer){offer.Status=NegotiationStatus.Declined;offer.Outcome="Career offer declined";return;}
        if(c.Action!=TimelineAction.AcceptCareer)throw new InvalidCommandException("Choose a valid response.");
        if(PendingCareer is not null)throw new InvalidCommandException("Finish or cancel the pending career move first.");
        ValidateOfferDestination(offer);
        var old=ControlledBusinessId;
        var destination=Locations.Single(l=>l.Id==offer.LocationId);
        var selected=c.Followers??new();
        if(selected.Distinct().Count()!=selected.Count||selected.Any(id=>id==ProtagonistPersonId||FindPerson(id)?.Employment?.BusinessId!=old||IsHistoricalAssistant(id)))
            throw new InvalidCommandException("Choose distinct ordinary employees from your current studio as followers.");
        offer.Status=NegotiationStatus.Accepted;
        CancelFormerStudioCommitments(old);
        TransferEmployee(Protagonist,destination,offer.Salary);
        ControlledBusinessId=offer.ToBusiness;Control=ControlMode.EmployedLead;BusinessOf(old).Independent=true;
        foreach(var id in selected.OrderBy(id=>id))
        {
            var follower=FindPerson(id)!;var salary=Math.Max(StudioRules.MinimumMonthlySalary,follower.Employment!.MonthlySalary);
            if(!offer.Followers.TryGetValue(id,out var draw)||draw>=Math.Clamp((follower.Loyalty-40)/60,0,.95)||follower.Skills.Values.Max()<55||follower.Employment.NoticeEndsAt is not null||FreeTimelineDesks(destination.Id)<=0||FreeCash(destination.BusinessId)<StudioRules.HiringReserve(salary))continue;
            TransferEmployee(follower,destination,salary);
        }
        Recruitment=null;CareerQuote=null;CareerQuoteExpires=null;FollowerRolls.Clear();
        offer.Outcome="Career offer accepted";TimelineNews($"Your perspective follows {Protagonist.Name} to {ControlledBusiness.Name}.");
    }
    private Person RecruitableRival(int id)
    {
        var p=FindPerson(id);
        if(p is null||p.Id==ProtagonistPersonId||IsHistoricalAssistant(id)||p.Employment is not {NoticeEndsAt:null} job||job.BusinessId==ControlledBusinessId||
            !World.RivalBusinesses.Contains(job.BusinessId)||job.StartsAt>Clock.Now||Locations.Single(l=>l.Id==job.LocationId).Closed)
            throw new InvalidCommandException("Select an ordinary employee currently working at a rival studio.");
        return p;
    }
    private void ValidateApproach(Person p,StudioLocation destination,long salary)
    {
        if(World.Offers.Any(o=>o.PersonId==p.Id&&o.Status==NegotiationStatus.Pending))throw new InvalidCommandException("This person is already considering an offer.");
        if(World.LastApproach.TryGetValue(p.Id,out var last)&&last.AddDays(30)>Clock.Now)throw new InvalidCommandException("Wait thirty days before another approach.");
        if(salary<Math.Max(StudioRules.MinimumMonthlySalary,p.ExpectedSalary*.8)||salary>p.ExpectedSalary*1.5)
            throw new InvalidCommandException("Offer 80–150% of expected salary, above the full-time floor.");
        if(FreeTimelineDesks(destination.Id)<=0)throw new InvalidCommandException("A usable desk must be free and unreserved.");
        if(FreeCash(destination.BusinessId)<StudioRules.HiringReserve(salary))throw new InvalidCommandException("Reserve seven days of wages.");
    }
    private void AddStaffOffer(Person p,StudioLocation destination,long salary,bool player)
    {
        var offer=new StaffNegotiation{Id=AllocateId(),PersonId=p.Id,FromBusiness=p.Employment!.BusinessId,ToBusiness=destination.BusinessId,
            LocationId=destination.Id,Salary=salary,CreatedAt=Clock.Now,EndsAt=Clock.Now.AddDays(7),Roll=World.HiringRng.NextDouble(),PlayerApproach=player};
        if(p.Id==ProtagonistPersonId)
            offer.Followers=ControlledStaff.Where(f=>f.Id!=p.Id&&!IsHistoricalAssistant(f.Id)).OrderBy(f=>f.Id).ToDictionary(f=>f.Id,f=>World.HiringRng.NextDouble());
        World.Offers.Add(offer);World.LastApproach[p.Id]=Clock.Now;
        if(offer.FromBusiness==ControlledBusinessId||offer.ToBusiness==ControlledBusinessId)
            TimelineNews($"{p.Name}: offer from {BusinessOf(destination.BusinessId).Name}, ¥{salary:N0}/month, decision {offer.EndsAt:d MMM}."+(p.Id==ProtagonistPersonId?" Moving requires your explicit acceptance.":""),true);
    }
    private void GenerateRivalOffer()
    {
        foreach(var person in ControlledStaff.Where(p=>!IsHistoricalAssistant(p.Id)&&p.Employment is {NoticeEndsAt:null} job&&job.StartsAt.AddDays(90)<=Clock.Now&&
            (p.Id!=ProtagonistPersonId||PartShown("industry"))).OrderBy(p=>p.Id)) // Q57: Aki is approached once Industry is open
        {
            if(World.Offers.Any(o=>o.PersonId==person.Id&&o.Status==NegotiationStatus.Pending)||World.LastApproach.TryGetValue(person.Id,out var last)&&last.AddDays(90)>Clock.Now)continue;
            var destination=Locations.Where(l=>World.RivalBusinesses.Contains(l.BusinessId)&&l.BusinessId!=ControlledBusinessId&&!l.Closed&&FreeTimelineDesks(l.Id)>0).OrderBy(l=>l.Id).FirstOrDefault();
            if(destination is null)return;
            var salary=Math.Max(StudioRules.MinimumMonthlySalary,Math.Max(person.ExpectedSalary,(long)(person.Employment!.MonthlySalary*1.15)));
            if(FreeCash(destination.BusinessId)<StudioRules.HiringReserve(salary))continue;
            var relation=World.Relations.GetValueOrDefault(RelationKey(destination.BusinessId,ControlledBusinessId),50);
            if(World.HiringRng.NextDouble()>=.05+(50-relation)*.0005)continue;
            AddStaffOffer(person,destination,salary,false);break;
        }
    }
    private void ResolveScouting()
    {
        foreach(var report in World.Reports.Where(r=>r.ObservedAt is null&&r.ReadyAt<=Clock.Now))
        {
            var p=FindPerson(report.PersonId)!;report.ObservedAt=Clock.Now;
            report.Skills=p.Skills.ToDictionary(k=>k.Key,k=>5*(int)Math.Round(k.Value/5d));report.ExpectedSalary=p.ExpectedSalary;
            if(report.BusinessId==ControlledBusinessId)TimelineNews($"Scouting report ready: {p.Name}. Skill estimates are rounded; willingness to leave is unknown.");
        }
    }
    private void ValidateOfferDestination(StaffNegotiation o)
    {
        var person=FindPerson(o.PersonId)!;var location=Locations.Single(l=>l.Id==o.LocationId);
        if(person.Employment?.BusinessId!=o.FromBusiness||person.Employment.NoticeEndsAt is not null||location.Closed||
            FreeTimelineDesks(location.Id)<0)
            throw new InvalidCommandException("The offer lapsed because the job, desk or funding changed.");
        // FreeCash is floored at zero, so independently verify the reserved amount is still backed.
        if(BusinessOf(o.ToBusiness).Account.Balance<TimelineReserve(o.ToBusiness)+Bills.Where(b=>b.BusinessId==o.ToBusiness).Sum(b=>b.Remaining)+WageObligations.Where(w=>w.BusinessId==o.ToBusiness).Sum(w=>w.Remaining)+
            People.Where(p=>p.Employment?.BusinessId==o.ToBusiness).Sum(p=>Math.Max((long)Math.Ceiling(p.Employment!.AccruedPay),p.Employment.StartsAt.AddDays(7)>Clock.Now?StudioRules.HiringReserve(p.Employment.MonthlySalary):0)))
            throw new InvalidCommandException("The destination no longer has the reserved wages.");
    }
    public static double RecruitmentChance(long offered,long current,double happiness,double loyalty) => Math.Clamp(.3+
        Math.Clamp((double)offered/Math.Max(1,current)-1,-.5,.5)*.5+Math.Clamp((50-happiness)/50,0,1)*.2-Math.Clamp(loyalty/100,0,1)*.3,.05,.9);
    private void ResolveStaffOffers()
    {
        foreach(var o in World.Offers.Where(o=>o.Status==NegotiationStatus.Pending).OrderBy(o=>o.Id).ToArray())
        {
            if(!o.Warned&&Clock.Now>=o.EndsAt.AddDays(-1)){o.Warned=true;if(o.FromBusiness==ControlledBusinessId||o.ToBusiness==ControlledBusinessId)TimelineNews($"Last day to respond to {FindPerson(o.PersonId)!.Name}'s recruitment offer.",true);}
            try{ValidateOfferDestination(o);}catch(InvalidCommandException ex){o.Status=NegotiationStatus.Lapsed;o.Outcome=ex.Message;continue;}
            if(Clock.Now<o.EndsAt)continue;
            if(o.PersonId==ProtagonistPersonId){o.Status=NegotiationStatus.Declined;o.Outcome="Career offer expired without acceptance";continue;}
            var p=FindPerson(o.PersonId)!;
            if(o.PlayerApproach&&World.Relations.GetValueOrDefault(RelationKey(o.FromBusiness,o.ToBusiness),50)<45&&FreeCash(o.FromBusiness)>StudioRules.HiringReserve(o.Salary))
                p.Employment!.MonthlySalary=Math.Max(p.Employment.MonthlySalary,Math.Min(o.Salary,(long)(p.Employment.MonthlySalary*1.05)));
            if(o.Roll<RecruitmentChance(o.Salary,p.Employment!.MonthlySalary,p.Happiness,p.Loyalty))
            {
                o.Status=NegotiationStatus.Accepted;TransferEmployee(p,Locations.Single(l=>l.Id==o.LocationId),o.Salary);o.Outcome="Offer accepted";
                if(World.Offers.Count(x=>x.Status==NegotiationStatus.Accepted&&x.FromBusiness==o.FromBusiness&&x.ToBusiness==o.ToBusiness&&x.EndsAt.AddDays(90)>=Clock.Now)>1)
                {var key=RelationKey(o.FromBusiness,o.ToBusiness);World.Relations[key]=Math.Max(0,World.Relations.GetValueOrDefault(key,50)-5);}
            }
            else{o.Status=NegotiationStatus.Declined;o.Outcome="Employee chose to stay";}
            if(o.FromBusiness==ControlledBusinessId||o.ToBusiness==ControlledBusinessId)TimelineNews($"{p.Name}: {o.Outcome}.");
        }
    }
    private static string RelationKey(int a,int b)=>$"{Math.Min(a,b)}:{Math.Max(a,b)}";
    private void TransferEmployee(Person person,StudioLocation destination,long salary)
    {
        var old=person.Employment!.BusinessId;LeaveJob(person);
        person.EmploymentHistory.Add(new(){BusinessId=destination.BusinessId,LocationId=destination.Id,StartsAt=Clock.Now,MonthlySalary=salary});
        person.MainSeriesId=null;person.Resigning=false;person.BusyUntil=Clock.Now.AddHours(8);
        person.Schedule=new(){WorkStartHour=9,WorkEndHour=18,DaysOff=[DayOfWeek.Saturday,DayOfWeek.Sunday]};
        foreach(var s in Series.Where(s=>s.BusinessId==old&&(s.LeadPersonId==person.Id||s.RightsLeadPersonId==person.Id)&&s.Status!=SeriesStatus.Ended).ToArray())
        {
            if(s.RightsLeadPersonId==person.Id&&(person.Id==ProtagonistPersonId||Ownership==OwnershipMode.CreatorRetention||s.ReleaseWithCreator))
            {TransferFutureTitle(s,destination.BusinessId,destination.Id);s.LeadPersonId=person.Id;}
            else if(s.LeadPersonId==person.Id)
            {
                var next=People.Where(p=>p.Employment?.BusinessId==old&&!IsHistoricalAssistant(p.Id)&&CanProduce(p)).OrderByDescending(p=>p.Skill(Stage.Name)).ThenBy(p=>p.Id).FirstOrDefault();
                if(next is null){s.Status=SeriesStatus.Paused;}else{s.LeadPersonId=next.Id;s.LocationId=next.Employment!.LocationId;}
            }
        }
        foreach(var b in Bookings.Where(b=>!b.Settled&&(b.PersonId==person.Id||b.AssistantId==person.Id)))CancelBooking(b);
        RefreshOfficeAssignments();
    }
    private void HireHistorical(TimelineCommand command)
    {
        RequireOwner();
        if(command.Target<0||command.Target>=World.Assistants.Count)throw new InvalidCommandException("Select a discovered temporary assistant.");
        var a=World.Assistants[command.Target];var d=TimelineCatalog.Default.Creators.Single(c=>c.Id==a.Key);
        if(a.DiscoveredBy!=ControlledBusinessId||a.PersonId is not null||a.Departed||Clock.Now<d.Start||Clock.Now>=d.Departure)
            throw new InvalidCommandException("This temporary assistant is unavailable.");
        var location=Locations.SingleOrDefault(l=>l.Id==command.LocationId&&l.BusinessId==ControlledBusinessId&&!l.Closed)
            ??throw new InvalidCommandException("Choose an open workplace.");
        var expected=StudioRules.ExpectedSalary(d.Skills.Max());
        if(command.Salary<Math.Max(StudioRules.MinimumMonthlySalary,expected*.8)||command.Salary>expected*1.5)throw new InvalidCommandException($"Offer ¥{Math.Max(StudioRules.MinimumMonthlySalary,expected*.8):N0}–¥{expected*1.5:N0} per month.");
        if(FreeTimelineDesks(location.Id)<=0||FreeCash(ControlledBusinessId)<StudioRules.HiringReserve(command.Salary))throw new InvalidCommandException("Reserve a usable desk and seven days of wages.");
        if(command.Salary<expected&&a.HireRoll>=Math.Clamp(((double)command.Salary/expected-.8)/.2,0,1))throw new InvalidCommandException("This assistant declined the salary. A higher offer may succeed.");
        var start=Clock.Now.Date.AddDays(1).AddHours(9);while(start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)start=start.AddDays(1);
        if(start>=d.Departure)throw new InvalidCommandException("The agreed start would be after this contract ends.");
        var p=new Person{Id=AllocateId(),Name=d.Name,Skills=StageOrder.All.Select((s,i)=>(s,i)).ToDictionary(v=>v.s,v=>d.Skills[v.i]),ExpectedSalary=expected,Reputation=15,
            Schedule=new(){WorkStartHour=9,WorkEndHour=18,DaysOff=[DayOfWeek.Saturday,DayOfWeek.Sunday]}};
        p.EmploymentHistory.Add(new(){BusinessId=ControlledBusinessId,LocationId=location.Id,StartsAt=start,MonthlySalary=command.Salary});
        People.Add(p);a.PersonId=p.Id;a.ContactBusiness=ControlledBusinessId;
        TimelineNews($"{p.Name} starts {start:d MMM}. Fixed departure: {d.Departure:d MMM yyyy}. Supporting roles only.");
    }
    public string RivalStaffProfile(int id)
    {
        var p=FindPerson(id)??throw new InvalidCommandException("Unknown person.");
        var report=World.Reports.LastOrDefault(r=>r.PersonId==id&&r.BusinessId==ControlledBusinessId&&r.ObservedAt is not null);
        var specialty=p.Skills.OrderByDescending(s=>s.Value).ThenBy(s=>s.Key).First().Key;
        return $"{p.Name}: {specialty} specialist, reputation {p.Reputation:0}."+(report is null?" Scout for skill estimates and salary expectations. Scouting: ¥10,000, three days.":
            $" Report {report.ObservedAt:d MMM yyyy}: {string.Join(", ",report.Skills.Select(k=>$"{k.Key} ~{k.Value}"))}; expected salary ~¥{report.ExpectedSalary:N0}/month. Willingness unknown.");
    }
}
