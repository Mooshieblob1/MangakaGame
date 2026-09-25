using System.Diagnostics.CodeAnalysis;

namespace MangakaSim;

public partial class GameState
{
    private void ValidateStudioSave(HashSet<int> ids)
    {
        static void Check([DoesNotReturnIf(false)] bool condition, string field)
        { if (!condition) throw new InvalidDataException($"Save file has invalid {field}."); }
        bool Time(DateTime value) => value >= GameClock.Start && value <= Clock.Now && value.Ticks % TimeSpan.TicksPerHour == 0;
        void Id(int id) => Check(id > 0 && ids.Add(id), "studio id");
        Check(Enum.IsDefined(Ownership) && Enum.IsDefined(Control), "ownership/control mode");
        Check(StaffRng is not null && LocationRng is not null, "staff/location random state");
        Check(Businesses is { Count: > 0 } && Businesses.All(b => b is not null), "businesses");
        Check(Locations is { Count: > 0 } && Locations.All(l => l is not null), "locations");
        Check(Candidates is not null && Candidates.All(c => c is not null) && WageObligations is not null && WageObligations.All(o => o is not null), "staff records");
        Check(People.Count(p => p.Id == ProtagonistPersonId) == 1 && Businesses.Count(b => b.Id == ControlledBusinessId) == 1, "controlled identities");
        Check(People.All(p => p.EmploymentHistory is not null && p.EmploymentHistory.All(e => e is not null)), "employment records");
        var accounts = new List<CashAccount>();
        foreach (var business in Businesses)
        {
            Id(business.Id);
            Check(!string.IsNullOrWhiteSpace(business.Name) && double.IsFinite(business.TrackRecord) && business.TrackRecord is >= 0 and <= 100, "business details");
            accounts.Add(business.Account);
        }
        foreach (var location in Locations)
        {
            Id(location.Id);
            Check(Businesses.Any(b => b.Id == location.BusinessId) && !string.IsNullOrWhiteSpace(location.District) &&
                !string.IsNullOrWhiteSpace(location.Name) && location.Seats is > 0 and <= 1000 && location.MonthlyRent >= 0 &&
                (!location.IsFamilyHome || location.MonthlyRent == 0 && location.Seats == 2), "location terms");
            Check(People.Count(p => p.Employment?.LocationId == location.Id) <= location.Seats, "desk capacity");
        }
        foreach (var person in People)
        {
            Check(person.EmploymentHistory is not null && person.EmploymentHistory.All(e => e is not null) &&
                person.EmploymentHistory.Count(e => e.EndsAt is null) <= 1 && person.ExpectedSalary >= 0, "employment history");
            DateTime? previousEnd = null;
            var employmentIndex = 0;
            foreach (var job in person.EmploymentHistory)
            {
                Check(employmentIndex++ == person.EmploymentHistory.Count - 1 || job.EndsAt is not null, "employment interval order");
                Check(Businesses.Any(b => b.Id == job.BusinessId) && Locations.Any(l => l.Id == job.LocationId && l.BusinessId == job.BusinessId), "employment workplace");
                Check(job.StartsAt >= GameClock.Start && job.StartsAt.Ticks % TimeSpan.TicksPerHour == 0 && job.StartsAt <= Clock.Now.AddDays(7) &&
                    (previousEnd is null || job.StartsAt >= previousEnd) &&
                    (job.EndsAt is null || Time(job.EndsAt.Value) && job.EndsAt >= job.StartsAt) &&
                    (job.NoticeEndsAt is null || job.NoticeEndsAt >= job.StartsAt && job.NoticeEndsAt.Value.Hour == 0) &&
                    job.MonthlySalary >= 0 && job.AccruedPay is >= 0 and <= 1000000000000m, "employment terms");
                Check(person.Id == ProtagonistPersonId || job.MonthlySalary >= StudioRules.MinimumMonthlySalary, "salary floor");
                previousEnd = job.EndsAt;
            }
            Check(person.MainSeriesId is null || Series.Any(s => s.Id == person.MainSeriesId), "main team");
            accounts.Add(person.PersonalAccount);
        }
        Check(Protagonist.Employment?.BusinessId == ControlledBusinessId, "protagonist employer");
        foreach (var account in accounts)
        {
            Check(account is not null && account.OpeningBalance >= 0 && account.Balance >= 0 && account.Entries is not null, "cash account");
            var running = account.OpeningBalance;
            var last = GameClock.Start;
            foreach (var entry in account.Entries)
            {
                Check(entry is not null && Time(entry.Time) && entry.Time >= last && entry.Reason is not null && Enum.IsDefined(entry.Kind), "account entry");
                Check(entry.SeriesId is null || Series.Any(s => s.Id == entry.SeriesId), "account series");
                Check((entry.Kind is AccountEntryKind.Transfer or AccountEntryKind.Salary) == entry.TransferId.HasValue, "transfer kind");
                running = checked(running + entry.Amount);
                Check(running >= 0, "cash overdraft");
                last = entry.Time;
            }
            Check(running == account.Balance, "account reconciliation");
        }
        // Keep account identity alongside each entry instead of rescanning every ledger for
        // each historical transfer (quadratic in the length of a career).
        foreach (var group in accounts.SelectMany((a,index) => a.Entries.Where(e => e.TransferId is not null)
                     .Select(e => (Entry:e,Account:index))).GroupBy(e => e.Entry.TransferId!.Value))
        {
            Id(group.Key);
            var entries = group.Select(e=>e.Entry).ToArray();
            Check(entries.Length == 2 && entries[0].Amount != 0 && entries[0].Amount == -entries[1].Amount &&
                entries[0].Time == entries[1].Time && entries[0].Reason == entries[1].Reason && entries[0].Kind == entries[1].Kind &&
                group.Select(e=>e.Account).Distinct().Count() == 2, "balanced transfer");
        }
        foreach (var candidate in Candidates)
        {
            Id(candidate.Id);
            Check(!string.IsNullOrWhiteSpace(candidate.Name) && Enum.IsDefined(candidate.Profile) && candidate.Skills is not null &&
                StageOrder.All.All(s => candidate.Skills.TryGetValue(s, out var skill) && skill is >= 0 and <= 100) &&
                candidate.ExpectedSalary == StudioRules.ExpectedSalary(candidate.Skills.Values.Max()) && candidate.ExpiresAt > Clock.Now &&
                candidate.ExpiresAt <= Clock.Now.AddDays(14) && double.IsFinite(candidate.AcceptanceRoll) && candidate.AcceptanceRoll is >= 0 and < 1, "candidate");
        }
        foreach (var obligation in WageObligations)
        {
            Id(obligation.Id);
            Check(Businesses.Any(b => b.Id == obligation.BusinessId) && People.Any(p => p.Id == obligation.PersonId) &&
                Time(obligation.DueAt) && obligation.OriginalAmount > 0 && obligation.Remaining >= 0 && obligation.Remaining <= obligation.OriginalAmount, "wage obligation");
        }
        foreach (var group in WageObligations.GroupBy(o => (o.BusinessId, o.PersonId)))
        {
            var incoming = FindPerson(group.Key.PersonId)!.PersonalAccount.Entries.Where(e => e.Kind == AccountEntryKind.Salary)
                .Select(e => e.TransferId).ToHashSet();
            var paid = -Businesses.Single(b => b.Id == group.Key.BusinessId).Account.Entries
                .Where(e => e.Kind == AccountEntryKind.Salary && incoming.Contains(e.TransferId)).Sum(e => e.Amount);
            Check(group.Sum(o => o.OriginalAmount - o.Remaining) == paid, "wage settlement reconciliation");
        }
        Check(LastRecruitmentAt is null || Time(LastRecruitmentAt.Value), "recruitment cooldown");
        Check(Recruitment is null || LastRecruitmentAt is { } lastSearch && Recruitment.ReadyAt == lastSearch.AddDays(7) &&
            Recruitment.ReadyAt > Clock.Now && Businesses.Any(b => b.Id == Recruitment.BusinessId) &&
            (Recruitment.Specialty is null || Enum.IsDefined(Recruitment.Specialty.Value)), "recruitment search");
        foreach (var series in Series)
        {
            Check(Businesses.Any(b => b.Id == series.BusinessId) && Locations.Any(l => l.Id == series.LocationId && l.BusinessId == series.BusinessId) &&
                People.Any(p => p.Id == series.LeadPersonId) && People.Any(p => p.Id == series.RightsLeadPersonId), "series ownership");
            foreach (var chapter in series.Chapters)
            {
                Check(chapter.CreatorPersonId is null || People.Any(p => p.Id == chapter.CreatorPersonId), "chapter creator");
                foreach (var work in chapter.Stages)
                    Check(double.IsFinite(work.QualityWeightedWork) && work.QualityWeightedWork >= 0 && work.QualityWeightedWork <= work.HoursRequired + 1e-8 &&
                        (work.ManualAssignee is null || People.Any(p => p.Id == work.ManualAssignee)), "work attribution");
            }
        }
    }
}
