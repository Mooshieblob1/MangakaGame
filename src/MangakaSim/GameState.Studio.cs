using System.Text.Json.Serialization;

namespace MangakaSim;

public partial class GameState
{
    [JsonRequired] public int ProtagonistPersonId { get; set; }
    [JsonRequired] public int ControlledBusinessId { get; set; }
    [JsonRequired] public OwnershipMode Ownership { get; set; }
    [JsonRequired] public ControlMode Control { get; set; }
    [JsonRequired] public List<Business> Businesses { get; set; } = new();
    [JsonRequired] public List<StudioLocation> Locations { get; set; } = new();
    [JsonRequired] public List<Candidate> Candidates { get; set; } = new();
    [JsonRequired] public List<WageObligation> WageObligations { get; set; } = new();
    [JsonRequired] public Rng StaffRng { get; set; } = Rng.FromSeed(1);
    [JsonRequired] public Rng LocationRng { get; set; } = Rng.FromSeed(2);
    [JsonRequired] public RecruitmentSearch? Recruitment { get; set; }
    [JsonRequired] public DateTime? LastRecruitmentAt { get; set; }
    [JsonIgnore] public Person Protagonist => People.Single(p => p.Id == ProtagonistPersonId);
    [JsonIgnore] public Business ControlledBusiness => Businesses.Single(b => b.Id == ControlledBusinessId);
    [JsonIgnore] public IEnumerable<Person> ControlledStaff => People.Where(p => p.Employment?.BusinessId == ControlledBusinessId);
    [JsonIgnore] public long PersonalMoney => Protagonist.PersonalAccount.Balance;
    [JsonIgnore] public long WageArrears => WageObligations.Where(o => o.BusinessId == ControlledBusinessId).Sum(o => o.Remaining);
    [JsonIgnore] public long ReservedWages => ControlledStaff.Sum(p => Math.Max((long)Math.Ceiling(p.Employment!.AccruedPay),
        p.Employment.StartsAt.AddDays(7) > Clock.Now ? StudioRules.HiringReserve(p.Employment.MonthlySalary) : 0)) + WageArrears;
    [JsonIgnore] public long AvailableBusinessCash => FreeCash(ControlledBusinessId);

    private void InitializeStudio(OwnershipMode ownership)
    {
        Ownership = ownership;
        ProtagonistPersonId = People.Single().Id;
        StaffRng = Rng.FromSeed(unchecked(RngSeed ^ 0x53544146));
        LocationRng = Rng.FromSeed(unchecked(RngSeed ^ 0x544f4b59));
        var business = new Business { Id = AllocateId(), Name = $"{Protagonist.Name} Studio" };
        Businesses.Add(business);
        ControlledBusinessId = business.Id;
        string[] wards = ["Nerima", "Itabashi", "Adachi", "Katsushika", "Edogawa", "Ota"];
        var home = new StudioLocation { Id = AllocateId(), BusinessId = business.Id, District = wards[LocationRng.NextInt(wards.Length)] };
        Locations.Add(home);
        Protagonist.IsProdigy = true;
        Protagonist.PersonalAccount = new() { OpeningBalance = 500000, Balance = 500000 };
        Protagonist.EmploymentHistory.Add(new() { BusinessId = business.Id, LocationId = home.Id, StartsAt = Clock.Now });
        Transfer(Protagonist.PersonalAccount, business.Account, 300000, "founding contribution", AccountEntryKind.Transfer);
        RefreshCandidates();
    }

    private void Transfer(CashAccount from, CashAccount to, long amount, string reason, AccountEntryKind kind)
    {
        if (amount <= 0 || Spendable(from) < amount || to.Balance > long.MaxValue - amount)
            throw new InvalidCommandException("Transfer amount exceeds the available balance.");
        Subsidize(from, amount);
        var id = AllocateId();
        from.Balance -= amount;
        to.Balance += amount;
        from.Entries.Add(new(Clock.Now, -amount, reason, null, kind, id));
        to.Entries.Add(new(Clock.Now, amount, reason, null, kind, id));
    }

    private void ApplyContribution(ContributeFundsCommand c)
    {
        RequireOwner();
        Transfer(Protagonist.PersonalAccount, ControlledBusiness.Account, c.Amount, "owner contribution", AccountEntryKind.Transfer);
    }

    private void RequireOwner()
    {
        if (Control != ControlMode.OwnerDirector) throw new InvalidCommandException("Only the owner can change this business's finances.");
    }

    private Candidate GenerateCandidate(bool recruited, Stage? target = null)
    {
        var roll = StaffRng.NextDouble();
        var profile = recruited
            ? roll < .20 ? CandidateProfile.Junior : roll < .60 ? CandidateProfile.Generalist : roll < .98 ? CandidateProfile.Specialist : CandidateProfile.Prodigy
            : roll < .55 ? CandidateProfile.Junior : roll < .85 ? CandidateProfile.Generalist : roll < .995 ? CandidateProfile.Specialist : CandidateProfile.Prodigy;
        var specialty = target ?? StageOrder.All[StaffRng.NextInt(StageOrder.All.Length)];
        var skills = new Dictionary<Stage, int>();
        foreach (var stage in StageOrder.All)
            skills[stage] = profile switch
            {
                CandidateProfile.Junior => StaffRng.NextInt(25, 46) + (stage == specialty ? 10 : 0),
                CandidateProfile.Generalist => StaffRng.NextInt(40, 61) + (stage == specialty ? 10 : 0),
                CandidateProfile.Specialist => stage == specialty ? StaffRng.NextInt(70, 86) : StaffRng.NextInt(35, 61),
                _ => StaffRng.NextInt(75, 91),
            };
        string[] names = ["Haruka", "Ren", "Misaki", "Sora", "Naoki", "Yui", "Takeshi", "Emi"];
        return new() { Id = AllocateId(), Name = names[StaffRng.NextInt(names.Length)], Profile = profile,
            Skills = skills, ExpectedSalary = StudioRules.ExpectedSalary(skills.Values.Max()),
            ExpiresAt = Clock.Now.AddDays(14), Recruited = recruited, AcceptanceRoll = StaffRng.NextDouble() };
    }

    private void RefreshCandidates()
    {
        Candidates.RemoveAll(c => !c.Recruited);
        for (var i = 0; i < 6; i++) Candidates.Add(GenerateCandidate(false));
        if(World.Assistants.Count>0)DiscoverAssistant(ControlledBusinessId,.02);
    }

    private void ApplyRecruit(RecruitStaffCommand c)
    {
        RequireOwner();
        if (c.Specialty is { } stage) RequireStage(stage);
        if (Recruitment is not null || LastRecruitmentAt is { } last && Clock.Now < last.AddDays(14))
            throw new InvalidCommandException("Recruitment is already underway or in its 14-day cooldown.");
        var cost = RecruitmentFee;
        if (AvailableBusinessCash < cost) throw new InvalidCommandException($"Recruitment needs ¥{cost:N0} after reserved wages.");
        PostLedger(-cost, "recruitment");
        LastRecruitmentAt = Clock.Now;
        Recruitment = new(ControlledBusinessId, c.Specialty, Clock.Now.AddDays(7));
    }

    private void ApplyHire(HireStaffCommand c)
    {
        RequireOwner();
        var candidate = Candidates.SingleOrDefault(x => x.Id == c.CandidateId && x.ExpiresAt > Clock.Now && (x.IntroductionBusinessId is null || x.IntroductionBusinessId == ControlledBusinessId))
            ?? throw new InvalidCommandException("This candidate is no longer available.");
        var location = Locations.SingleOrDefault(l => l.Id == c.LocationId && l.BusinessId == ControlledBusinessId && !l.Closed)
            ?? throw new InvalidCommandException("Choose one of this business's workplaces.");
        if (FreeTimelineDesks(location.Id) <= 0)
            throw new InvalidCommandException("This workplace has no free desk.");
        if (c.MonthlySalary < Math.Max(StudioRules.MinimumMonthlySalary, (long)Math.Ceiling(candidate.ExpectedSalary * .8m)) ||
            c.MonthlySalary > candidate.ExpectedSalary * 1.5m)
            throw new InvalidCommandException("Offer 80–150% of expected salary, above the ¥122,000 full-time floor.");
        if (AvailableBusinessCash < StudioRules.HiringReserve(c.MonthlySalary))
            throw new InvalidCommandException("Reserve seven days' salary before hiring.");
        var chance = Math.Clamp(((double)c.MonthlySalary / candidate.ExpectedSalary - .8) / .2, 0, 1);
        if (candidate.AcceptanceRoll >= chance && c.MonthlySalary < candidate.ExpectedSalary)
            throw new InvalidCommandException("The candidate declined this salary. A better offer may succeed.");
        var start = Clock.Now.Date.AddDays(1).AddHours(9);
        while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) start = start.AddDays(1);
        var person = new Person { Id = candidate.Id, Name = candidate.Name, Skills = new(candidate.Skills),
            IsProdigy = candidate.Profile == CandidateProfile.Prodigy, HiddenTalent = candidate.HiddenTalent, ExpectedSalary = candidate.ExpectedSalary,
            Reputation = candidate.Profile switch { CandidateProfile.Junior => 5, CandidateProfile.Generalist => 10, CandidateProfile.Specialist => 20, _ => 25 },
            Schedule = new() { WorkStartHour = 9, WorkEndHour = 18, DaysOff = [DayOfWeek.Saturday, DayOfWeek.Sunday] } };
        person.EmploymentHistory.Add(new() { BusinessId = ControlledBusinessId, LocationId = location.Id, StartsAt = start, MonthlySalary = c.MonthlySalary });
        People.Add(person);
        Candidates.Remove(candidate);
        Emit(EventType.StaffHired, $"{person.Name} starts {start:d MMM}, ¥{c.MonthlySalary:N0}/month.", personId: person.Id);
    }

    private void ApplyDismiss(DismissStaffCommand c)
    {
        RequireOwner();
        var person = RequirePerson(c.PersonId);
        if (person.Id == ProtagonistPersonId) throw new InvalidCommandException("The protagonist leaves only through a career decision.");
        var job = person.Employment!;
        if (job.NoticeEndsAt is not null) throw new InvalidCommandException("This person is already serving notice.");
        if (job.StartsAt > Clock.Now) throw new InvalidCommandException("Wait until their agreed start before giving notice.");
        job.NoticeEndsAt = Clock.Now.Date.AddDays(30);
        ClearAssignments(person);
        Emit(EventType.StaffNotice, $"{person.Name} has 30 days' paid notice; past pay and authorship remain.", personId: person.Id);
    }

    private void ClearAssignments(Person person)
    {
        foreach (var work in Series.SelectMany(s => s.Chapters).SelectMany(c => c.Stages).Where(w => !w.IsDone && w.AssignedTo == person.Id))
        { work.AssignedTo = null; work.ManualAssignee = null; }
        person.CurrentTask = null; person.Queue.Clear(); person.Pins.Clear(); person.ManualOrder = null;
    }

    private bool CanProduce(Person p) => p.Employment is { NoticeEndsAt: null } job && job.StartsAt <= Clock.Now && !Locations.Single(l => l.Id == job.LocationId).Closed && (Version < 4 || HasDesk(p.Id)) &&
        !WageObligations.Any(o => o.PersonId == p.Id && o.Remaining > 0 && o.DueAt.AddDays(21) <= Clock.Now);

    private void StudioStep()
    {
        Candidates.RemoveAll(c => c.ExpiresAt <= Clock.Now);
        if (Recruitment is { } search && search.ReadyAt <= Clock.Now)
        {
            for (var i = 0; i < 3; i++) Candidates.Add(GenerateCandidate(true, search.Specialty));
            DiscoverAssistant(search.BusinessId,.35);
            Recruitment = null;
            Emit(EventType.RecruitmentCompleted, "Three targeted recruitment candidates are available for 14 days.");
        }
        if (Clock.Hour == 8 && Clock.DayOfWeek == DayOfWeek.Monday && (Clock.Now.Day <= 7 || Clock.Now.Day is >= 15 and <= 21))
            RefreshCandidates();
        if (Clock.Hour == 0)
        {
            var date = Clock.Now.AddDays(-1).Date;
            foreach (var person in People.OrderBy(p => p.Id).ToArray())
            {
                if (person.Employment is not { } job) continue;
                if (job.StartsAt.Date <= date) job.AccruedPay += (decimal)job.MonthlySalary / DateTime.DaysInMonth(date.Year, date.Month);
                var ending = job.NoticeEndsAt is { } end && end <= Clock.Now;
                if (Clock.Now.Day == 1 || ending)
                {
                    // Remove sub-yen decimal division noise, retaining eight fractional
                    // digits so a full month's salary cannot settle one yen short.
                    job.AccruedPay = decimal.Round(job.AccruedPay, 8);
                    var due = (long)decimal.Floor(job.AccruedPay);
                    job.AccruedPay -= due;
                    if (due > 0) WageObligations.Add(new() { Id = AllocateId(), BusinessId = job.BusinessId, PersonId = person.Id,
                        DueAt = Clock.Now, OriginalAmount = due, Remaining = due });
                }
                if (ending) { job.EndsAt = Clock.Now; ClearAssignments(person); CreatorDeparture(person,job.BusinessId); Emit(EventType.StaffDeparted, $"{person.Name} leaves; earned pay remains owed.", personId: person.Id); }
            }
        }
        foreach (var due in WageObligations.Where(o => o.Remaining > 0).OrderBy(o => o.DueAt).ThenBy(o => o.Id))
        {
            var account = Businesses.Single(b => b.Id == due.BusinessId).Account;
            var amount = Math.Min(Spendable(account), due.Remaining);
            if (amount > 0) { Transfer(account, FindPerson(due.PersonId)!.PersonalAccount, amount, "salary", AccountEntryKind.Salary); due.Remaining -= amount; }
            if (due.Remaining > 0 && !due.WarningEmitted)
            { due.WarningEmitted = true; Emit(EventType.WageArrears, $"Unpaid wages: ¥{due.Remaining:N0} owed to {FindPerson(due.PersonId)!.Name}.", personId: due.PersonId); }
        }
    }
}
