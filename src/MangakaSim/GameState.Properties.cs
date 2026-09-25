namespace MangakaSim;

public partial class GameState
{
    private StudioLocation LeaseProperty(int business, PropertyOffer offer)
    {
        Spend(business,offer.Rent*3,"lease deposit and first rent");
        var location = new StudioLocation { Id=AllocateId(), BusinessId=business, Name=$"{offer.District} studio", District=offer.District,
            PropertyOfferId=offer.Id, Seats=offer.Seats, MonthlyRent=offer.Rent, IsFamilyHome=false, PropertyTier=offer.Tier, Storage=offer.Storage, Atmosphere=offer.Atmosphere, Deposit=offer.Rent*2, NextRentAt=Clock.Now.Date.AddMonths(1) };
        Locations.Add(location); InitializeOffice(location); return location;
    }
    private void PropertyAction(StudioActionCommand c)
    {
        RequireOwner();
        if (c.Action is StudioAction.Lease or StudioAction.Move)
        {
            var offer = TokyoProperties.All.SingleOrDefault(p => p.Id == c.Target) ?? throw new InvalidCommandException("Select a property.");
            var old = Locations.Single(l => l.Id == Protagonist.Employment!.LocationId);
            if (c.Action == StudioAction.Lease)
            {
                var count = Locations.Count(l => l.BusinessId == ControlledBusinessId && !l.Closed);
                if (!Assist(SandboxAssist.UnlockLocations) && (ControlledStaff.Count() < 6+Math.Max(0,count-1)*4 || Series.Count(s => s.BusinessId == ControlledBusinessId && s.Status == SeriesStatus.Active) < 2))
                    throw new InvalidCommandException("A branch needs six staff and two active series, then four more staff per additional branch.");
                LeaseProperty(ControlledBusinessId,offer);
            }
            else
            {
                var people = ControlledStaff.Where(p => p.Employment!.LocationId == old.Id).ToArray();
                if (people.Length > offer.Seats || PrintRuns.Where(r => r.LocationId == old.Id).Sum(r => r.Remaining) > offer.Storage)
                    throw new InvalidCommandException("The property cannot fit this team and stock.");
                Spend(ControlledBusinessId,20000+2000*people.Length,"moving");
                var next = LeaseProperty(ControlledBusinessId,offer);
                StoreOffice(old); FundOfficeFor(next,people.Length,false);
                Relocate(old,next,people); CloseLocation(old);
            }
        }
        else
        {
            var location = Locations.SingleOrDefault(l => l.Id == c.Target && l.BusinessId == ControlledBusinessId && !l.Closed)
                ?? throw new InvalidCommandException("Choose an open workplace.");
            if (c.Action == StudioAction.BreakRoom)
            { if (location.BreakSeats >= location.Seats) throw new InvalidCommandException("There is already enough break seating."); AddBreakFurniture(location); }
            if (c.Action == StudioAction.CloseLocation)
            {
                if (People.Any(p => p.Employment?.LocationId == location.Id)) throw new InvalidCommandException("Move staff before closing this workplace.");
                CloseLocation(location);
            }
            if (c.Action == StudioAction.TransferStaff)
            {
                var person = RequirePerson(c.Secondary);
                if (person.Employment!.LocationId == location.Id || People.Count(p => p.Employment?.LocationId == location.Id) >= UsableWorkspaces(location.Id))
                    throw new InvalidCommandException("Select a different workplace with a free desk.");
                var from = Locations.Single(l => l.Id == person.Employment.LocationId);
                var route = TokyoProperties.Travel(from.District,location.District);
                Spend(location.BusinessId,route.Fare,"staff travel");
                person.Employment.LocationId = location.Id; person.BusyUntil=Clock.Now.AddHours(route.Hours); ClearAssignments(person);
                foreach (var series in Series.Where(s => s.LeadPersonId == person.Id && s.BusinessId == location.BusinessId)) series.LocationId=location.Id;
            }
        }
    }
    private void Relocate(StudioLocation old, StudioLocation next, IEnumerable<Person> people)
    {
        foreach (var p in people) { p.Employment!.LocationId=next.Id; p.BusyUntil=Clock.Now.AddDays(1); ClearAssignments(p); }
        foreach (var s in Series.Where(s => s.LocationId==old.Id)) s.LocationId=next.Id;
        foreach (var r in PrintRuns.Where(r => r.LocationId==old.Id)) r.LocationId=next.Id;
    }
    private void CloseLocation(StudioLocation location)
    {
        StoreOffice(location); location.Closed=true; location.ClosedAt=Clock.Now;
        foreach (var s in Series.Where(s => s.LocationId==location.Id && s.Status==SeriesStatus.Active)) s.Status=SeriesStatus.Paused;
        StudioMessage($"{location.Name} closed. Its stock and liabilities still belong to {BusinessOf(location.BusinessId).Name}.");
    }
    private void CareerStep()
    {
        if (Clock.Hour != 0) return;
        if (PendingCareer is { } move)
        {
            if (move.Amount > Spendable(Protagonist.PersonalAccount))
            { PendingCareer=null; FollowerRolls.Clear(); StudioMessage("Career move cancelled: the promised personal contribution is no longer available."); return; }
            var old=ControlledBusiness;
            CancelFormerStudioCommitments(old.Id);
            var formerStatuses=Series.Where(s=>s.BusinessId==old.Id).ToDictionary(s=>s.Id,s=>s.Status);
            var familyDistrict=Locations.First(l => l.IsFamilyHome).District;
            var family=Locations.FirstOrDefault(l => l.IsFamilyHome && l.BusinessId==old.Id && !l.Closed);
            old.Independent=true;
            var next=new Business { Id=AllocateId(), FoundedAt=Clock.Now, Name=move.Action==StudioAction.CareerEmployer ? $"Employer {Businesses.Count}" : $"{Protagonist.Name} Studio {Businesses.Count}", EmployerTier=move.Action==StudioAction.CareerEmployer ? move.Value : 0 };
            Businesses.Add(next); ControlledBusinessId=next.Id;
            Control=move.Action==StudioAction.CareerEmployer ? ControlMode.EmployedLead : ControlMode.OwnerDirector;
            StudioLocation destination;
            if (Control==ControlMode.EmployedLead)
            {
                var opening=new long[]{0,3000000,6000000,12000000}[move.Value];
                next.Account.OpeningBalance=next.Account.Balance=opening;
                destination=new(){Id=AllocateId(),BusinessId=next.Id,Name="Employer's studio",District="Bunkyo",Seats=new[]{0,2,4,8}[move.Value],IsFamilyHome=false,Storage=3000};
                Locations.Add(destination);
            }
            else
            {
                if (move.Amount>0) Transfer(Protagonist.PersonalAccount,next.Account,move.Amount,"owner contribution",AccountEntryKind.Transfer);
                if (move.Action==StudioAction.CareerStudio)
                {
                    Spend(next.Id,20000,"moving"); destination=LeaseProperty(next.Id,TokyoProperties.All.Single(p => p.Id==move.Target));
                }
                else
                {
                    // A fresh employment-address record preserves the former studio's history.
                    destination=new(){Id=AllocateId(),BusinessId=next.Id,District=familyDistrict}; Locations.Add(destination);
                    if (family is not null) CloseLocation(family);
                }
            }
            InitializeOffice(destination);
            if(move.Action==StudioAction.CareerStudio) FundOfficeFor(destination,1,false);
            LeaveJob(Protagonist);
            Protagonist.EmploymentHistory.Add(new(){BusinessId=next.Id,LocationId=destination.Id,StartsAt=Clock.Now,MonthlySalary=Control==ControlMode.EmployedLead ? new long[]{0,150000,250000,400000}[move.Value] : 0});
            Protagonist.Schedule=new(){WorkStartHour=Control==ControlMode.EmployedLead?9:8,WorkEndHour=18,
                DaysOff=Control==ControlMode.EmployedLead?[DayOfWeek.Saturday,DayOfWeek.Sunday]:[DayOfWeek.Sunday]};
            var movers=new List<Person>{Protagonist};
            foreach (var entry in FollowerRolls.OrderBy(p => p.Key))
            {
                var p=FindPerson(entry.Key)!;
                if (p.Employment?.BusinessId!=old.Id || p.Employment.NoticeEndsAt is not null) continue;
                var salary=Math.Max(StudioRules.MinimumMonthlySalary,p.Employment.MonthlySalary);
                var chance=Math.Clamp((p.Loyalty-40)/60,0,.95);
                var accepted=Control!=ControlMode.EmployedLead || p.Skills.Values.Max() >= 45+(move.Value-1)*10;
                if (entry.Value >= chance || !accepted || movers.Count >= destination.Seats || FreeCash(next.Id) < StudioRules.HiringReserve(salary) + (move.Action==StudioAction.CareerStudio ? 10000 : 0)) continue;
                if(move.Action==StudioAction.CareerStudio) FundOfficeFor(destination,movers.Count+1,false);
                LeaveJob(p); p.EmploymentHistory.Add(new(){BusinessId=next.Id,LocationId=destination.Id,StartsAt=Clock.Now,MonthlySalary=salary}); movers.Add(p);
            }
            foreach (var s in Series.Where(s => s.BusinessId==old.Id))
            {
                var follows=movers.Any(p => p.Id==s.RightsLeadPersonId && (p.Id==ProtagonistPersonId || Ownership==OwnershipMode.CreatorRetention || s.ReleaseWithCreator));
                if (follows)
                {
                    TransferFutureTitle(s,next.Id,destination.Id);s.Status=formerStatuses[s.Id];
                    StudioMessage($"{s.Title}'s future production follows its creator. Existing books still pay the former business.");
                }
                else if (movers.Any(p => p.Id==s.LeadPersonId))
                {
                    var lead=People.Where(p => p.Employment?.BusinessId==old.Id && !IsHistoricalAssistant(p.Id) && CanProduce(p)).OrderByDescending(p => p.Skill(Stage.Name)).FirstOrDefault();
                    if (lead is null) s.Status=SeriesStatus.Paused;
                    else { s.LeadPersonId=s.RightsLeadPersonId=lead.Id; s.LocationId=lead.Employment!.LocationId; }
                }
            }
            foreach (var p in movers) { ClearAssignments(p); p.BusyUntil=Clock.Now.AddHours(8); }
            foreach (var booking in Bookings.Where(b => !b.Settled && (movers.Any(p => p.Id==b.PersonId || p.Id==b.AssistantId)))) CancelBooking(booking);
            PendingCareer=null; CareerQuote=null; CareerQuoteExpires=null; FollowerRolls.Clear(); Recruitment=null;
            StudioMessage($"Your perspective follows {Protagonist.Name} to {next.Name}. {old.Name} continues independently.");
        }
        if (Clock.DayOfWeek!=DayOfWeek.Monday) return;
        foreach(var person in People.Where(p=>p.Employment is null && p.PersonalAccount.Balance>=200000).ToArray())
        foreach(var business in Series.Where(s=>s.AwaitingCreatorDestination&&s.RightsLeadPersonId==person.Id).Select(s=>s.BusinessId).Distinct().ToArray()) CreatorDeparture(person,business);
        foreach (var business in Businesses.Where(b => b.Independent && !World.RivalBusinesses.Contains(b.Id)))
        {
            var workplace = Locations.FirstOrDefault(l => l.BusinessId==business.Id && !l.Closed && People.Count(p => p.Employment?.LocationId==l.Id)<UsableWorkspaces(l.Id));
            if (workplace is not null && Series.Any(s => s.BusinessId==business.Id && s.Status==SeriesStatus.Active) && FreeCash(business.Id)>400000)
            {
                var candidate=GenerateCandidate(false);
                if (FreeCash(business.Id)>2*(candidate.ExpectedSalary+People.Where(p=>p.Employment?.BusinessId==business.Id).Sum(p=>p.Employment!.MonthlySalary)+Locations.Where(l=>l.BusinessId==business.Id&&!l.Closed).Sum(l=>l.MonthlyRent)))
                {
                    var employee=new Person{Id=candidate.Id,Name=candidate.Name,Skills=candidate.Skills,ExpectedSalary=candidate.ExpectedSalary,IsProdigy=candidate.Profile==CandidateProfile.Prodigy,
                        Schedule=new(){WorkStartHour=9,WorkEndHour=18,DaysOff=[DayOfWeek.Saturday,DayOfWeek.Sunday]}};
                    employee.EmploymentHistory.Add(new(){BusinessId=business.Id,LocationId=workplace.Id,StartsAt=Clock.Now.AddHours(9),MonthlySalary=candidate.ExpectedSalary}); People.Add(employee);
                }
            }
            if (Clock.Now.Day<=7 && Clock.Now.Month%3==0)
            {
                var author=People.Where(p=>p.Employment?.BusinessId==business.Id && !IsHistoricalAssistant(p.Id) && p.Skill(Stage.Name)>=50 && !Series.Any(s=>s.LeadPersonId==p.Id&&s.Status==SeriesStatus.Active)).OrderBy(p=>p.Id).FirstOrDefault();
                if(author is not null && FreeCash(business.Id)>50000)
                {
                    var title=CreateSeries($"{author.Name}'s independent story","drama",Cadence.Monthly,19);
                    title.BusinessId=business.Id;title.LocationId=author.Employment!.LocationId;title.LeadPersonId=title.RightsLeadPersonId=author.Id;
                }
            }
            foreach (var s in Series.Where(s => s.BusinessId==business.Id && !s.AwaitingCreatorDestination && s.Status!=SeriesStatus.Ended))
            {
                var lead=FindPerson(s.LeadPersonId)!;
                if (lead.Employment?.BusinessId!=business.Id || !CanProduce(lead))
                    lead=People.Where(p => p.Employment?.BusinessId==business.Id && !IsHistoricalAssistant(p.Id) && CanProduce(p)).OrderByDescending(p => p.Skill(Stage.Name)).FirstOrDefault();
                if (lead is null) { s.Status=SeriesStatus.Paused; continue; }
                s.LeadPersonId=lead.Id; s.LocationId=lead.Employment!.LocationId;
                if (!Locations.Single(l => l.Id==s.LocationId).Closed) s.Status=SeriesStatus.Active;
                if (FreeCash(business.Id)>50000) { s.AutoPrint=true; s.PrintBudget=20000; }
            }
        }
    }
    private void LeaveJob(Person person)
    {
        var job=person.Employment!;
        var amount=(long)decimal.Floor(job.AccruedPay);
        if (amount>0) WageObligations.Add(new(){Id=AllocateId(),BusinessId=job.BusinessId,PersonId=person.Id,DueAt=Clock.Now,OriginalAmount=amount,Remaining=amount});
        job.AccruedPay-=amount; job.EndsAt=Clock.Now; ClearAssignments(person);
    }

    private void CreatorDeparture(Person person,int oldBusiness)
    {
        var titles=Series.Where(s=>s.BusinessId==oldBusiness&&s.RightsLeadPersonId==person.Id&&s.Status!=SeriesStatus.Ended&&
            (Ownership==OwnershipMode.CreatorRetention||s.ReleaseWithCreator)).ToArray();
        if(titles.Length==0) return;
        if(person.PersonalAccount.Balance<200000)
        { foreach(var title in titles){title.AwaitingCreatorDestination=true;title.Status=SeriesStatus.Paused;} return; }
        var business=new Business{Id=AllocateId(),Name=$"{person.Name}'s studio",Independent=true,FoundedAt=Clock.Now};Businesses.Add(business);
        Transfer(person.PersonalAccount,business.Account,200000,"owner contribution",AccountEntryKind.Transfer);
        var location=LeaseProperty(business.Id,TokyoProperties.All[0]);
        FundOfficeFor(location,1,false);
        person.EmploymentHistory.Add(new(){BusinessId=business.Id,LocationId=location.Id,StartsAt=Clock.Now,MonthlySalary=StudioRules.MinimumMonthlySalary});
        foreach(var title in titles)
        {
            TransferFutureTitle(title,business.Id,location.Id);title.LeadPersonId=person.Id;
        }
        StudioMessage($"{person.Name} establishes an independent studio with their retained titles.");
    }

    public string CareerPreview()
    {
        if(CareerQuote is not { } move) return "Choose a destination.";
        var employer=move.Action==StudioAction.CareerEmployer;
        var seats=employer?new[]{0,2,4,8}[move.Value]:move.Action==StudioAction.CareerStudio?TokyoProperties.All.Single(p=>p.Id==move.Target).Seats:2;
        var cash=employer?new long[]{0,3000000,6000000,12000000}[move.Value]:move.Amount-(move.Action==StudioAction.CareerStudio?TokyoProperties.All.Single(p=>p.Id==move.Target).Rent*3+30000:0);
        var followers=new List<string>();
        foreach(var roll in FollowerRolls.OrderBy(p=>p.Key))
        {
            var person=FindPerson(roll.Key)!;
            if(person.Employment?.BusinessId!=ControlledBusinessId || person.Employment.NoticeEndsAt is not null) continue;
            var reserve=StudioRules.HiringReserve(Math.Max(StudioRules.MinimumMonthlySalary,person.Employment.MonthlySalary));
            var accepted=roll.Value<Math.Clamp((person.Loyalty-40)/60,0,.95) && followers.Count<seats-1 && cash>=reserve+(move.Action==StudioAction.CareerStudio?10000:0) && (!employer||person.Skills.Values.Max()>=45+(move.Value-1)*10);
            if(accepted) {followers.Add(person.Name);cash-=reserve+(move.Action==StudioAction.CareerStudio?10000:0);}
        }
        var titles=Series.Where(s=>s.BusinessId==ControlledBusinessId&&s.RightsLeadPersonId==ProtagonistPersonId&&s.Status!=SeriesStatus.Ended).Select(s=>s.Title);
        var cancellation=PrintRuns.Where(r=>!r.Delivered&&Series.Any(s=>s.RightsLeadPersonId==ProtagonistPersonId&&s.Volumes.Any(v=>v.Id==r.VolumeId))).Sum(r=>r.Cost-r.Cost/2);
        return $"Move: {move.Action}. Personal cash after contribution: ¥{PersonalMoney-move.Amount:N0}. Personal debt stays with you: ¥{Loans.Where(l=>l.BusinessId is null&&l.PersonId==ProtagonistPersonId).Sum(l=>l.Principal):N0}.\n"+
            $"Future titles: {string.Join(", ",titles)}. Released books keep their original business and creator shares.\n"+
            $"Accepted followers: {(followers.Count==0?"None":string.Join(", ",followers))}. Available desks: {seats}.\n"+
            $"Undelivered print cancellation: ¥{cancellation:N0} remains spent by the old business; half of each quote is refunded.\n"+
            (employer?$"Monthly salary: ¥{new long[]{0,150000,250000,400000}[move.Value]:N0}. Team budget: ¥{new long[]{0,30000,250000,500000}[move.Value]:N0}.\n":"")+
            $"Old business keeps furniture, cash, stock, branches and debts. New rented workspaces cost ¥10,000 per incoming person. Move takes effect at midnight; quote expires {CareerQuoteExpires:d MMM}.";
    }

    private void TransferFutureTitle(Series title,int business,int location)
    {
        var old=BusinessOf(title.BusinessId);
        foreach(var booking in Bookings.Where(b=>!b.Settled&&!b.Cancelled&&b.SeriesId==title.Id))CancelBooking(booking);
        title.BusinessId=business;title.LocationId=location;title.AwaitingCreatorDestination=false;
        foreach(var chapter in title.Chapters.Where(c=>c.Status!=ChapterStatus.Complete))
        foreach(var work in chapter.Stages.Where(w=>!w.IsDone))
        {
            if(work.ManualAssignee is { } manual && FindPerson(manual)?.Employment?.BusinessId!=business) work.ManualAssignee=null;
            work.AssignedTo=null;
        }
        foreach(var volume in title.Volumes.Where(v=>v.ReleasedAt is null))
        {
            foreach(var run in PrintRuns.Where(r=>r.VolumeId==volume.Id&&!r.Delivered).ToArray())
            {
                AccountPost(old.Account,run.Cost/2,"cancelled print refund",AccountEntryKind.Credit);
                foreach(var booking in Bookings)booking.ReservedStock.Remove(run.Id);
                volume.Contribution+=run.Cost/2; PrintRuns.Remove(run);
            }
            volume.BusinessId=business;
        }
    }
}
