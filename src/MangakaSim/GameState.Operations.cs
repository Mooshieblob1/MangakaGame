using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangakaSim;

public partial class GameState
{
    [JsonRequired] public List<Bill> Bills { get; set; } = new();
    [JsonRequired] public List<Loan> Loans { get; set; } = new();
    [JsonRequired] public List<PrintRun> PrintRuns { get; set; } = new();
    [JsonRequired] public List<StaffProposal> StaffProposals { get; set; } = new();
    [JsonRequired] public List<ConventionBooking> Bookings { get; set; } = new();
    [JsonRequired] public int UncleEvents { get; set; }
    [JsonRequired] public DateTime? LastUncleEvent { get; set; }
    [JsonRequired] public StudioActionCommand? PendingCareer { get; set; }
    [JsonRequired] public Dictionary<int, double> FollowerRolls { get; set; } = new();
    [JsonRequired] public Dictionary<string,double> FollowerDecisionHistory { get; set; } = new();
    [JsonRequired] public StudioActionCommand? CareerQuote { get; set; }
    [JsonRequired] public DateTime? CareerQuoteExpires { get; set; }

    private Business BusinessOf(int id) => Businesses.Single(b => b.Id == id);
    private void AccountPost(CashAccount account, long amount, string reason, AccountEntryKind kind, int? seriesId = null)
    {
        if (amount < 0) Subsidize(account, checked(-amount));
        var next = checked(account.Balance + amount);
        if (next < 0) throw new InvalidCommandException("There is not enough money for this purchase.");
        account.Balance = next;
        account.Entries.Add(new(Clock.Now, amount, reason, seriesId, kind));
    }
    // Streaming sales (spec 2026-10-03): hourly income adds to today's line for the same title and reason.
    private void AccountPostDaily(CashAccount account, long amount, string reason, AccountEntryKind kind, int? seriesId)
    {
        if (amount <= 0) { if (amount < 0) AccountPost(account, amount, reason, kind, seriesId); return; }
        for (var i = account.Entries.Count - 1; i >= 0 && account.Entries[i].Time.Date == Clock.Now.Date; i--)
        {
            var entry = account.Entries[i];
            if (entry.Reason != reason || entry.SeriesId != seriesId || entry.Kind != kind || entry.TransferId is not null) continue;
            account.Balance = checked(account.Balance + amount);
            account.Entries[i] = entry with { Amount = checked(entry.Amount + amount) };
            return;
        }
        AccountPost(account, amount, reason, kind, seriesId);
    }
    private long FreeCash(int business) => Math.Max(0, Spendable(BusinessOf(business).Account) -
        People.Where(p => p.Employment?.BusinessId == business).Sum(p => Math.Max((long)Math.Ceiling(p.Employment!.AccruedPay),
            p.Employment.StartsAt.AddDays(7) > Clock.Now ? StudioRules.HiringReserve(p.Employment.MonthlySalary) : 0)) -
        WageObligations.Where(w => w.BusinessId == business).Sum(w => w.Remaining) - Bills.Where(b => b.BusinessId == business).Sum(b => b.Remaining) - TimelineReserve(business));
    private long TeamBudgetLeft(int business) => business==ControlledBusinessId && Control==ControlMode.EmployedLead ? Math.Max(0,new long[]{0,30000,250000,500000}[BusinessOf(business).EmployerTier]-BusinessOf(business).DiscretionarySpent) : long.MaxValue;
    private void Spend(int business, long amount, string reason, bool protect = true)
    {
        if (amount < 0 || (protect ? FreeCash(business) : Spendable(BusinessOf(business).Account)) < amount)
            throw new InvalidCommandException("Keep enough business cash for wages and outstanding bills.");
        var owner = BusinessOf(business);
        if (business == ControlledBusinessId && Control == ControlMode.EmployedLead && protect && reason is not ("daily provisions" or "staff travel"))
        {
            var cap = new long[] { 0,30000,250000,500000 }[owner.EmployerTier];
            if (owner.DiscretionarySpent + amount > cap) throw new InvalidCommandException("This month's employer team budget is exhausted.");
            owner.DiscretionarySpent += amount;
        }
        if (amount > 0) AccountPost(BusinessOf(business).Account, -amount, reason, AccountEntryKind.Expense);
    }
    private void AddBill(int business, long amount, string reason, int? person = null, int? location = null)
    {
        if (amount <= 0) return;
        // Hourly sales accrue creator shares often; one unpaid bill per person and due date keeps the ledger short.
        if (reason == "creator share" && person is not null && Bills.LastOrDefault(b => b.BusinessId == business && b.PersonId == person &&
            b.Reason == reason && b.Remaining == b.Original && b.DueAt == new DateTime(Clock.Now.Year, Clock.Now.Month, 1).AddMonths(1)) is { } open)
        { open.Original = checked(open.Original + amount); open.Remaining = checked(open.Remaining + amount); return; }
        Bills.Add(new() { Id = AllocateId(), BusinessId = business, PersonId = person, LocationId = location,
            Original = amount, Remaining = amount, Reason = reason, DueAt = person is null ? Clock.Now : new DateTime(Clock.Now.Year,Clock.Now.Month,1).AddMonths(1) });
    }
    private void StudioMessage(string message, int? person = null) => Emit(EventType.CommandApplied, message, personId: person);

    private void ApplyStudioAction(StudioActionCommand c)
    {
        // Compound purchases and career moves validate against a private snapshot first.
        // Neither RNG nor IDs nor money in the live simulation change after a rejected action.
        var probe = JsonSerializer.Deserialize<GameState>(ToJson(), JsonOptions)!;
        probe.ExecuteStudioAction(c);
        ExecuteStudioAction(c);
    }
    private void ExecuteStudioAction(StudioActionCommand c)
    {
        if (!Enum.IsDefined(c.Action)) throw new InvalidCommandException("Unknown studio action.");
        switch (c.Action)
        {
            case StudioAction.SetPipeline:
                var pipeline = RequireSeries(c.Target);
                if (c.Value is < 1 or > 3 || c.Secondary is < 0 or > 4 || c.Amount is < 1 or > 3)
                    throw new InvalidCommandException("Pipeline 1–3, buffer 0–4, unprinted masters 1–3.");
                pipeline.PipelineLimit = c.Value; pipeline.BufferLimit = c.Secondary; pipeline.MasterLimit = (int)c.Amount; break;
            case StudioAction.SetWellbeing:
                var staff = RequirePerson(c.Target);
                if (c.Value is < 0 or > 2 || c.Secondary is < 0 or > 12) throw new InvalidCommandException("Overtime: 0–2 hours/day, 0–12/week.");
                staff.DailyOvertimeLimit = c.Value; staff.WeeklyOvertimeLimit = c.Secondary; break;
            case StudioAction.SetMoonlighting:
                if (!Enum.IsDefined((MoonlightingPolicy)c.Value)) throw new InvalidCommandException("Choose a moonlighting policy.");
                if (c.Target == 0) { RequireOwner(); ControlledBusiness.Moonlighting = (MoonlightingPolicy)c.Value; }
                else RequirePerson(c.Target).MoonlightingOverride = c.Enabled ? null : (MoonlightingPolicy)c.Value;
                break;
            case StudioAction.SetFounderSalary:
                RequireOwner(); if (c.Amount is < 0 or > 1000000) throw new InvalidCommandException("Salary must be ¥0–1,000,000/month.");
                Protagonist.Employment!.MonthlySalary = c.Amount; break;
            case StudioAction.SetEmployeeSalary:
                RequireOwner(); var employee=RequirePerson(c.Target);
                if (employee.Id==ProtagonistPersonId || c.Amount<StudioRules.MinimumMonthlySalary || c.Amount>Math.Max(StudioRules.MinimumMonthlySalary,employee.ExpectedSalary)*1.5m)
                    throw new InvalidCommandException("Choose a hired employee and salary between the full-time floor and 150% of expected pay.");
                if (employee.HiddenTalent || employee.ProductiveHours>=8 && employee.Name.EndsWith("uncle") && employee.Employment!.StartsAt.AddDays(90)>Clock.Now)
                    throw new InvalidCommandException("The introduction's quoted salary is fixed for the first 90 days.");
                employee.Employment!.MonthlySalary=c.Amount; break;
            case StudioAction.DismissBuyout:
                RequireOwner(); var dismissed = RequirePerson(c.Target);
                if (dismissed.Id == ProtagonistPersonId || dismissed.Employment!.StartsAt > Clock.Now) throw new InvalidCommandException("Choose an active hired employee.");
                var job = dismissed.Employment!;
                var pay = job.MonthlySalary + (long)decimal.Ceiling(job.AccruedPay);
                if (FreeCash(job.BusinessId) < pay) throw new InvalidCommandException("Immediate dismissal requires all accrued pay plus 30 days' pay.");
                WageObligations.Add(new(){Id=AllocateId(),BusinessId=job.BusinessId,PersonId=dismissed.Id,DueAt=Clock.Now,OriginalAmount=pay,Remaining=0});
                Transfer(BusinessOf(job.BusinessId).Account,dismissed.PersonalAccount,pay,"salary",AccountEntryKind.Salary);
                job.AccruedPay=0; job.EndsAt=Clock.Now; ClearAssignments(dismissed); CreatorDeparture(dismissed,job.BusinessId); break;
            case StudioAction.Incorporate:
                RequireOwner();
                if (ControlledBusiness.Incorporated) throw new InvalidCommandException("Already incorporated.");
                var capital = Clock.Now < new DateTime(2006, 5, 1) ? 3000000 : 0;
                if (FreeCash(ControlledBusinessId) - Loans.Where(l => l.BusinessId == ControlledBusinessId).Sum(l => l.Principal) < capital + 200000)
                    throw new InvalidCommandException($"Incorporation needs ¥{capital:N0} capital plus a ¥200,000 setup expense.");
                Spend(ControlledBusinessId, 200000, "incorporation"); ControlledBusiness.Incorporated = true; break;
            case StudioAction.BorrowPersonal: case StudioAction.BorrowCard: case StudioAction.BorrowBusiness:
                Borrow(c); break;
            case StudioAction.RepayLoan:
                var loan = Loans.SingleOrDefault(l => l.Id == c.Target && (l.BusinessId == ControlledBusinessId || l.BusinessId is null && l.PersonId == ProtagonistPersonId))
                    ?? throw new InvalidCommandException("Choose your loan.");
                if (loan.BusinessId is not null) RequireOwner();
                if (c.Amount <= 0 || c.Amount > loan.Principal + (long)decimal.Ceiling(loan.Interest)) throw new InvalidCommandException("Choose an amount within the remaining balance.");
                PayLoan(loan, c.Amount); break;
            case StudioAction.RecoveryCommission:
                if (Protagonist.RecoveryHours > 0 || Protagonist.RecoveryAt is { } last && Clock.Now < last.AddDays(7))
                    throw new InvalidCommandException("One four-hour commission per seven days.");
                Protagonist.RecoveryHours = 4; Protagonist.RecoveryAt = Clock.Now; break;
            case StudioAction.Lease: case StudioAction.Move: case StudioAction.CloseLocation: case StudioAction.TransferStaff: case StudioAction.BreakRoom:
                PropertyAction(c); break;
            case StudioAction.Print: case StudioAction.AutoPrint: PrintingAction(c); break;
            case StudioAction.BookConvention: case StudioAction.CancelConvention: case StudioAction.Promote: case StudioAction.Campaign:
                OutreachAction(c); break;
            case StudioAction.AcceptProposal:
                var proposal = StaffProposals.SingleOrDefault(p => p.Id == c.Target && p.BusinessId == ControlledBusinessId && p.ExpiresAt > Clock.Now)
                    ?? throw new InvalidCommandException("Proposal expired.");
                var creator = RequirePerson(proposal.PersonId);
                if (Series.Count(s => s.LeadPersonId == creator.Id && s.Status == SeriesStatus.Active) >= 2) throw new InvalidCommandException("This lead already has two active titles.");
                var title = CreateSeries(proposal.Title, "Drama", Cadence.Monthly, 19);
                title.LeadPersonId = title.RightsLeadPersonId = creator.Id; title.LocationId = creator.Employment!.LocationId;
                creator.MainSeriesId = title.Id; StaffProposals.Remove(proposal); break;
            case StudioAction.ReleaseRights:
                RequireOwner(); var released=RequireSeries(c.Target); released.ReleaseWithCreator=c.Enabled;
                if(c.Enabled && FindPerson(released.RightsLeadPersonId)?.Employment is { } destination && destination.BusinessId!=released.BusinessId)
                    TransferFutureTitle(released,destination.BusinessId,destination.LocationId);
                break;
            case StudioAction.QuoteCareer:
                if ((StudioAction)c.Target is not (StudioAction.CareerHome or StudioAction.CareerStudio or StudioAction.CareerEmployer)) throw new InvalidCommandException("Choose a career destination.");
                var quoted=new StudioActionCommand((StudioAction)c.Target,c.Secondary,Amount:c.Amount,Value:c.Value,Followers:c.Followers);
                ExecuteStudioAction(quoted); PendingCareer=null; CareerQuote=quoted; CareerQuoteExpires=Clock.Now.AddDays(7); break;
            case StudioAction.CareerHome: case StudioAction.CareerStudio: case StudioAction.CareerEmployer:
                if (PendingCareer is not null) throw new InvalidCommandException("A career move is already arranged for midnight.");
                if (c.Amount < 0 || c.Amount > Spendable(Protagonist.PersonalAccount)) throw new InvalidCommandException("The startup contribution must come from personal savings.");
                if (c.Action == StudioAction.CareerEmployer && (c.Value is < 1 or > 3 || c.Value == 2 && Protagonist.Reputation < 30 || c.Value == 3 && Protagonist.Reputation < 60))
                    throw new InvalidCommandException("Employer tiers 2/3 require personal reputation 30/60; entry employment is always available.");
                if (c.Action == StudioAction.CareerStudio && !TokyoProperties.All.Any(p => p.Id == c.Target && p.Rent*3+30000 <= c.Amount))
                    throw new InvalidCommandException("Choose a property and contribute three months' rent plus ¥20,000 moving costs and ¥10,000 for your workstation, or return home for free.");
                if(c.Followers is not null && (c.Followers.Distinct().Count()!=c.Followers.Count || c.Followers.Any(id=>id==ProtagonistPersonId || !ControlledStaff.Any(p=>p.Id==id))))
                    throw new InvalidCommandException("Invite current colleagues once each.");
                PendingCareer = c;
                if (CareerQuote!=c || CareerQuoteExpires<=Clock.Now)
                    FollowerRolls = ControlledStaff.Where(p => p.Id != ProtagonistPersonId && (c.Followers is null || c.Followers.Contains(p.Id))).OrderBy(p => p.Id).ToDictionary(p => p.Id, p =>
                    {
                        var key=$"{ControlledBusinessId}:{p.Id}:{c.Action}:{c.Target}:{c.Value}";
                        if(!FollowerDecisionHistory.TryGetValue(key,out var roll)) FollowerDecisionHistory[key]=roll=StaffRng.NextDouble();
                        return roll;
                    });
                break;
        }
    }

    private void Borrow(StudioActionCommand c)
    {
        var business = c.Action == StudioAction.BorrowBusiness;
        var card = c.Action == StudioAction.BorrowCard;
        if (business) RequireOwner();
        if (!business && ControlledBusiness.Incorporated) throw new InvalidCommandException("New personal startup borrowing ends at incorporation; existing loans remain personal.");
        var cap = 500000L;
        if (Protagonist.PersonalAccount.Entries.Where(e => e.Time > Clock.Now.AddDays(-90) && e.Amount > 0 && e.Kind is AccountEntryKind.Salary or AccountEntryKind.PersonalIncome).Sum(e => e.Amount) >= 1000000) cap = 1000000;
        if (business)
        {
            var receipts = ControlledBusiness.Account.Entries.Where(e => e.Time > Clock.Now.AddDays(-90) && e.Kind == AccountEntryKind.Publishing).Sum(e => e.Amount);
            if (!ControlledBusiness.Incorporated || Clock.Now < ControlledBusiness.FoundedAt.AddDays(90) || WageArrears > 0 || Bills.Any(b => b.BusinessId == ControlledBusinessId && b.Remaining > 0))
                throw new InvalidCommandException("Business credit needs incorporation, 90 days' history and no overdue bills.");
            cap = Math.Min(3000000, receipts);
        }
        var outstanding = Loans.Where(l => business ? l.BusinessId == ControlledBusinessId : l.BusinessId is null && l.PersonId == ProtagonistPersonId).Sum(l => l.Principal);
        if (c.Amount < (business ? 500000 : card ? 1000 : 100000) || c.Amount + outstanding > cap || card &&
            c.Amount + Loans.Where(l => l.Card && l.PersonId == ProtagonistPersonId).Sum(l => l.Principal) > 100000)
            throw new InvalidCommandException($"This loan exceeds the available credit ceiling (¥{Math.Max(0,cap-outstanding):N0}).");
        var account = business ? ControlledBusiness.Account : Protagonist.PersonalAccount;
        AccountPost(account, c.Amount, "loan advance", AccountEntryKind.Credit);
        Loans.Add(new() { Id = AllocateId(), BusinessId = business ? ControlledBusinessId : null, PersonId = ProtagonistPersonId,
            Principal = c.Amount, Original = c.Amount, Apr = business ? .08m : .24m, Term = business ? 24 : 12, Card = card, NextPayment = Clock.Now.Date.AddMonths(1) });
    }
    private void PayLoan(Loan loan, long amount)
    {
        var account = loan.BusinessId is { } bid ? BusinessOf(bid).Account : FindPerson(loan.PersonId)!.PersonalAccount;
        AccountPost(account, -amount, "loan repayment", AccountEntryKind.Credit);
        var interest = Math.Min(amount, (long)decimal.Ceiling(loan.Interest));
        loan.InterestPaid+=interest; loan.Interest = Math.Max(0, loan.Interest-interest); loan.Principal -= amount-interest;
        loan.Arrears = Math.Max(0,loan.ScheduledPrincipalDue-loan.Original+loan.Principal)+(long)decimal.Ceiling(loan.Interest);
    }
    private void FinanceStep()
    {
        if (Clock.Hour == 0)
        {
            foreach (var loan in Loans.Where(l => l.Principal > 0))
            {
                loan.Interest += loan.Principal*loan.Apr/365;
                if (loan.NextPayment > Clock.Now) continue;
                var principalDue = Math.Min(loan.Principal, loan.Card ? Math.Max(5000, (long)Math.Ceiling(loan.Principal*.1)) : (long)Math.Ceiling((double)loan.Original/loan.Term));
                loan.ScheduledPrincipalDue = Math.Min(loan.Original,loan.ScheduledPrincipalDue+principalDue);
                loan.Arrears = Math.Max(0,loan.ScheduledPrincipalDue-loan.Original+loan.Principal)+(long)decimal.Ceiling(loan.Interest);
                loan.NextPayment = loan.NextPayment.AddMonths(1);
            }
            foreach(var location in Locations.Where(l=>!l.Closed && l.NextRentAt<=Clock.Now))
            { AddBill(location.BusinessId,location.MonthlyRent,"rent",location:location.Id); location.NextRentAt=location.NextRentAt!.Value.AddMonths(1); }
            if (Clock.Now.Day == 1)
            {
                foreach (var l in Locations.Where(l => !l.Closed))
                {
                    AddBill(l.BusinessId,new long[]{5000,10000,20000,35000,60000}[l.PropertyTier],"utilities",location:l.Id);
                }
                foreach (var b in Businesses)
                {
                    b.DiscretionarySpent=0;
                    if (b.Incorporated) AddBill(b.Id,20000,"company administration");
                    if (b.Account.Entries.Any(e=>e.Reason=="internet")) AddBill(b.Id,3000,"internet subscription");
                }
            }
        }
        // Wages cannot change during bill settlement. Check their history once per hour,
        // rather than once for every unpaid bill in a long-running business.
        var businessesOwingWages = WageObligations.Where(w => w.Remaining > 0).Select(w => w.BusinessId).ToHashSet();
        foreach (var bill in Bills.Where(b => b.Remaining > 0 && b.DueAt <= Clock.Now).OrderBy(b => b.PersonId is null ? 1 : 0).ThenBy(b => b.DueAt).ThenBy(b => b.Id))
        {
            var account = BusinessOf(bill.BusinessId).Account;
            if (businessesOwingWages.Contains(bill.BusinessId)) continue;
            var amount = Math.Min(Spendable(account),bill.Remaining);
            if (amount > 0)
            {
                if (bill.PersonId is { } person) Transfer(account,FindPerson(person)!.PersonalAccount,amount,bill.Reason,AccountEntryKind.Transfer);
                else AccountPost(account,-amount,bill.Reason,AccountEntryKind.Expense);
                bill.Remaining -= amount;
            }
            if (bill.Remaining == 0) continue;
            if (Clock.Now == bill.DueAt.AddDays(7)) StudioMessage($"Unpaid {bill.Reason}: ¥{bill.Remaining:N0}. Rent closes a workplace after 30 days.");
            if (bill.Reason == "rent" && bill.DueAt.AddDays(RecoveryDays(30,bill.BusinessId)) <= Clock.Now && bill.LocationId is { } location && !Locations.Single(l => l.Id == location).Closed)
                CloseLocation(Locations.Single(l => l.Id == location));
            if (bill.Reason == "internet subscription" && bill.DueAt.AddDays(7) <= Clock.Now) BusinessOf(bill.BusinessId).HasInternet = false;
        }
        if (Clock.Hour==8)
            foreach (var business in Businesses.Where(b=>!b.HasInternet && b.Account.Entries.Any(e=>e.Reason=="internet")))
                if (!Bills.Any(b=>b.BusinessId==business.Id && b.Reason=="internet subscription" && b.Remaining>0)) business.HasInternet=true;
        foreach (var l in Loans.Where(l => l.Arrears > 0))
        {
            var available = l.BusinessId is { } bid ? FreeCash(bid) : Spendable(FindPerson(l.PersonId)!.PersonalAccount);
            var pay = Math.Min(available,l.Arrears); if (pay > 0) PayLoan(l,pay);
        }
        foreach (var location in Locations.Where(l => l.Closed && l.Deposit > 0 && l.ClosedAt!.Value.AddDays(7) <= Clock.Now))
        {
            var refund = location.Deposit;
            foreach (var bill in Bills.Where(b => b.LocationId == location.Id && b.Remaining > 0))
            { var used = Math.Min(refund,bill.Remaining); bill.Remaining -= used; refund -= used; }
            if (refund > 0) AccountPost(BusinessOf(location.BusinessId).Account,refund,"deposit return",AccountEntryKind.Credit);
            location.Deposit = 0;
        }
    }
}
