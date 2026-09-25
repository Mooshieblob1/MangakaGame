namespace MangakaSim;

public partial class GameState
{
    internal void Learn(Person person, Stage stage, double hours, bool productive = false)
    {
        var xp = person.Experience.GetValueOrDefault(stage) + hours*(person.IsProdigy ? 2 : 1)*(productive?HistoricalMentoring(person,stage):1);
        while (person.Skill(stage) < 100 && xp >= 40+2*person.Skill(stage))
        { xp -= 40+2*person.Skill(stage); person.Skills[stage] = person.Skill(stage)+1; }
        person.Experience[stage] = person.Skill(stage) == 100 ? 0 : xp;
    }
    private void NeedsChange(Person p, double food, double drink, double comfort)
    { p.Food = Math.Clamp(p.Food+food,0,100); p.Drink = Math.Clamp(p.Drink+drink,0,100); p.Comfort = Math.Clamp(p.Comfort+comfort,0,100); }
    private bool PersonAvailable(Person p) => p.BusyUntil is null || p.BusyUntil <= TickStart;
    private void WellbeingStep()
    {
        foreach (var p in People.OrderBy(p => p.Id))
        {
            if(AtOutsideJob(p,TickStart))continue;
            if (p.Employment is not { } job || !(p.Schedule.IsRegularHour(TickStart) || IsOvertimeHour(p,TickStart) || ConventionHour(p,TickStart)))
            {
                var policy = p.MoonlightingOverride ?? (p.Employment is { } employment ? BusinessOf(employment.BusinessId).Moonlighting : MoonlightingPolicy.Allowed);
                var evening = p.SideProjectUntil > TickStart && policy != MoonlightingPolicy.Prohibited && TickStart.Hour is 20 or 21 &&
                    (TickStart.DayOfWeek == DayOfWeek.Tuesday || policy == MoonlightingPolicy.Allowed && TickStart.DayOfWeek == DayOfWeek.Thursday);
                if (evening) { Learn(p,Stage.Name,1); if (TickStart.Hour == 21) p.SideProjectEvenings++; }
                else NeedsChange(p,10,15,10);
                continue;
            }
            if (job.StartsAt > TickStart) continue;
            if (p.ProvisionsAt != TickStart.Date)
            {
                p.ProvisionsAt = TickStart.Date;
                p.HasProvisions = FreeCash(job.BusinessId) >= 150;
                if (p.HasProvisions) Spend(job.BusinessId,150,"daily provisions");
            }
            if (p.LowestNeed < 35) p.BreakWait++;
            if (p.LowestNeed < 20) p.LowNeedHours++;
            if (PersonAvailable(p)) NeedsChange(p,-2,-3,-1);
            else NeedsChange(p,-4,-5,-3);
        }
        foreach (var location in Locations.Where(l => !l.Closed))
        {
            var queue = People.Where(p => p.Employment?.LocationId == location.Id && PersonAvailable(p) && (p.Schedule.IsRegularHour(TickStart) || ConventionHour(p,TickStart)) &&
                (p.LowestNeed < 35 || p.Id != ProtagonistPersonId && TickStart.Hour == p.Schedule.WorkStartHour+4))
                .OrderBy(p => p.LowestNeed).ThenByDescending(p => p.BreakWait).ThenBy(p => p.Id).ToArray();
            foreach (var p in queue.Take(location.BreakSeats))
            {
                p.BusyUntil = Clock.Now; p.BreakWait = 0; ObserveBreak(p,location,Array.IndexOf(queue,p));
                if (p.HasProvisions) NeedsChange(p,45,60,35 + BreakComfortBonus(location.Id,Array.IndexOf(queue,p)));
                else { p.Food = Math.Max(50,p.Food); p.Drink = Math.Max(50,p.Drink); p.Comfort = Math.Max(50,p.Comfort); }
            }
            foreach (var p in queue.Skip(location.BreakSeats)) { p.BusyUntil = Clock.Now; Observe(p,OfficeActivityKind.Waiting); }
        }
        if (Clock.Hour != 0) return;
        foreach (var p in People)
        {
            if (Clock.DayOfWeek == DayOfWeek.Monday) p.WeeklyOvertime = 0;
            if (p.SideProjectUntil is { } end && end <= Clock.Now)
            {
                if (p.SideProjectEvenings > 0) AccountPost(p.PersonalAccount,2000,"personal doujin",AccountEntryKind.PersonalIncome);
                p.SideProjectUntil = null; p.SideProjectEvenings = 0;
            }
            if (p.Employment is not { } job || job.StartsAt > Clock.Now) continue;
            var policy = p.MoonlightingOverride ?? BusinessOf(job.BusinessId).Moonlighting;
            var mood = policy == MoonlightingPolicy.Allowed ? 5 : policy == MoonlightingPolicy.Limited ? 3 : -3;
            var unpaid = WageObligations.Where(w => w.PersonId == p.Id && w.Remaining > 0).Select(w => (Clock.Now-w.DueAt).Days).DefaultIfEmpty(0).Max();
            var pay = p.ExpectedSalary == 0 ? 0 : Math.Clamp(40*((double)job.MonthlySalary/p.ExpectedSalary-1),-20,10);
            var target = Math.Clamp(70+pay+Locations.Single(l => l.Id == job.LocationId).Atmosphere- (p.HasProvisions?0:5)-2*p.WeeklyOvertime-4*Math.Min(7,unpaid)-3*Math.Min(10,p.LowNeedHours)+mood,0,100);
            p.Happiness = Math.Clamp(p.Happiness+Math.Clamp(target-p.Happiness,-3,2),0,100); p.LowNeedHours = 0;
            if (ManagedBusiness(job.BusinessId) && Assist(SandboxAssist.NoStress)) p.Happiness = Math.Max(70, p.Happiness);
            p.Loyalty = Math.Clamp(p.Loyalty+(p.Happiness-50)/100+(policy == MoonlightingPolicy.Allowed ? 1d/7 : policy == MoonlightingPolicy.Limited ? .5/7 : 0),0,100);
            p.LowMoodDays = p.Happiness < 30 ? p.LowMoodDays+1 : 0;
            if (p.Id == ProtagonistPersonId) continue;
            if (p.Resigning && p.Happiness >= 45 && unpaid == 0) { job.NoticeEndsAt = null; p.Resigning = false; }
            if (p.LowMoodDays == 7 || unpaid == 14) StudioMessage($"{p.Name} warns they may leave unless conditions improve.",p.Id);
            if ((p.LowMoodDays >= 14 || unpaid >= 28) && job.NoticeEndsAt is null)
            { p.Resigning = true; job.NoticeEndsAt = Clock.Now.AddDays(7); StudioMessage($"{p.Name} gives seven days' notice.",p.Id); }
            if ((Clock.Now-GameClock.Start).Days % 28 == 0)
            {
                if (p.SideProjectUntil is null && StaffRng.NextDouble() < .2) p.SideProjectUntil = Clock.Now.AddDays(14);
                if (!IsHistoricalAssistant(p.Id) && p.Skill(Stage.Name) >= 50 && p.Happiness >= 50 && StaffRng.NextDouble() < .1 && StaffProposals.Count(x => x.BusinessId == job.BusinessId) < 3)
                    StaffProposals.Add(new(){Id=AllocateId(),PersonId=p.Id,BusinessId=job.BusinessId,ExpiresAt=Clock.Now.AddDays(28),Title=$"{p.Name}'s new story"});
            }
        }
        StaffProposals.RemoveAll(p => p.ExpiresAt <= Clock.Now || FindPerson(p.PersonId)?.Employment?.BusinessId != p.BusinessId);
        if (Clock.Now.Day != 1) return;
        foreach (var business in Businesses.Where(b=>b.Id==ControlledBusinessId||!World.RivalBusinesses.Contains(b.Id)).OrderBy(b => b.Id))
        {
            var sponsor = People.Where(p => p.Employment?.BusinessId == business.Id && p.Id != ProtagonistPersonId && p.Loyalty >= 60 && p.Employment.StartsAt.AddDays(90) <= Clock.Now).OrderBy(p => p.Id).FirstOrDefault();
            if (sponsor is null || WageObligations.Any(w => w.BusinessId == business.Id && w.Remaining > 0)) continue;
            if (UncleEvents < 2 && (LastUncleEvent is null || LastUncleEvent.Value.AddYears(3) <= Clock.Now) && StaffRng.NextDouble() < .005)
            {
                var candidate = GenerateCandidate(true); candidate.Profile = CandidateProfile.Prodigy;
                candidate.Skills = StageOrder.All.ToDictionary(s => s,_ => StaffRng.NextInt(75,91));
                candidate.ExpectedSalary = StudioRules.ExpectedSalary(candidate.Skills.Values.Max()); candidate.Name = $"{sponsor.Name}'s uncle";
                candidate.ExpiresAt = Clock.Now.AddDays(7); candidate.HiddenTalent=true; candidate.IntroductionBusinessId=business.Id; Candidates.Add(candidate); UncleEvents++; LastUncleEvent = Clock.Now;
                StudioMessage($"{sponsor.Name}'s uncle would like to work here. His talent is unknown until eight productive hours.");
            }
            else if ((business.LastIntroduction is null || business.LastIntroduction.Value.AddDays(90) <= Clock.Now) && StaffRng.NextDouble() < .04)
            { var candidate=GenerateCandidate(true); candidate.IntroductionBusinessId=business.Id; Candidates.Add(candidate); business.LastIntroduction = Clock.Now; StudioMessage($"{sponsor.Name} introduces a candidate."); }
        }
    }
    private void SpareHoursStep()
    {
        // Mentoring uses one hour for each participant and never displaces deadline-risk work.
        if (TickStart.DayOfWeek == DayOfWeek.Friday)
        {
            var used = new HashSet<int>();
            foreach (var student in People.OrderBy(p => p.Id))
            {
                if (student.MentoredWeek == Monday(TickStart) || used.Contains(student.Id) || !PersonAvailable(student) || !IsWorkingHour(student,TickStart,out _)) continue;
                if (student.CurrentTask is { } task && FindChapter(task.ChapterId)!.IsAtRisk) continue;
                var stage = StageOrder.All.OrderBy(s => student.Skill(s)).First();
                var mentor = People.Where(p => p.Id != student.Id && p.MentoredWeek != Monday(TickStart) && !used.Contains(p.Id) && p.Employment?.LocationId == student.Employment?.LocationId &&
                    p.Skill(stage) >= student.Skill(stage)+15 && PersonAvailable(p) && IsWorkingHour(p,TickStart,out _) &&
                    (p.CurrentTask is null || !FindChapter(p.CurrentTask.Value.ChapterId)!.IsAtRisk)).OrderBy(p => p.Id).FirstOrDefault();
                if (mentor is null) continue;
                Observe(student,OfficeActivityKind.Mentoring,stage,mentor.Id); Observe(mentor,OfficeActivityKind.Mentoring,stage,student.Id);
                used.Add(student.Id); used.Add(mentor.Id); student.MentoredWeek=mentor.MentoredWeek=Monday(TickStart); student.BusyUntil=mentor.BusyUntil=Clock.Now; Learn(student,stage,4);
            }
        }
        foreach (var person in People.Where(p => p.RecoveryHours > 0 && PersonAvailable(p) && IsWorkingHour(p,TickStart,out _)))
        {
            Observe(person,OfficeActivityKind.Recovery);
            person.BusyUntil = Clock.Now; person.RecoveryHours--; person.HoursWorkedToday++;
            if (person.RecoveryHours != 0) continue;
            var business = person.Employment!.BusinessId;
            AccountPost(BusinessOf(business).Account,3000,"recovery commission",AccountEntryKind.Publishing);
            AddBill(business,600,"creator share",person.Id);
        }
        PromotionStep();
    }
}
