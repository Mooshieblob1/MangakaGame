using System.Text.Json.Serialization;

namespace MangakaSim;

public enum OwnershipMode { StudioRetention, CreatorRetention }
public enum ControlMode { OwnerDirector, EmployedLead }
public enum CandidateProfile { Junior, Generalist, Specialist, Prodigy }
public enum AccountEntryKind { Publishing, Expense, Transfer, Salary, Credit, PersonalIncome, SandboxSubsidy, LicenseIncome, AwardPrize }

public sealed class CashAccount
{
    [JsonRequired] public long OpeningBalance { get; set; }
    [JsonRequired] public long Balance { get; set; }
    [JsonRequired] public List<LedgerEntry> Entries { get; set; } = new();
}

public sealed partial class Business
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public string Name { get; set; } = "";
    [JsonRequired] public CashAccount Account { get; set; } = new();
    [JsonRequired] public double TrackRecord { get; set; }
    [JsonRequired] public bool HasInternet { get; set; }
}

public sealed partial class StudioLocation
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public string Name { get; set; } = "Parents' home";
    [JsonRequired] public string District { get; set; } = "";
    [JsonRequired] public int Seats { get; set; } = 2;
    [JsonRequired] public long MonthlyRent { get; set; }
    [JsonRequired] public bool IsFamilyHome { get; set; } = true;
}

public sealed class Employment
{
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public int LocationId { get; set; }
    [JsonRequired] public DateTime StartsAt { get; set; }
    [JsonRequired] public DateTime? EndsAt { get; set; }
    [JsonRequired] public DateTime? NoticeEndsAt { get; set; }
    [JsonRequired] public long MonthlySalary { get; set; }
    [JsonRequired] public decimal AccruedPay { get; set; }
}

public sealed class WageObligation
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public int PersonId { get; set; }
    [JsonRequired] public DateTime DueAt { get; set; }
    [JsonRequired] public long OriginalAmount { get; set; }
    [JsonRequired] public long Remaining { get; set; }
    [JsonRequired] public bool WarningEmitted { get; set; }
}

public sealed class Candidate
{
    [JsonRequired] public int? IntroductionBusinessId { get; set; }
    [JsonRequired] public bool HiddenTalent { get; set; }
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public string Name { get; set; } = "";
    [JsonRequired] public CandidateProfile Profile { get; set; }
    [JsonRequired] public Dictionary<Stage, int> Skills { get; set; } = new();
    [JsonRequired] public long ExpectedSalary { get; set; }
    [JsonRequired] public DateTime ExpiresAt { get; set; }
    [JsonRequired] public bool Recruited { get; set; }
    [JsonRequired] public double AcceptanceRoll { get; set; }
}

public sealed record RecruitmentSearch(int BusinessId, Stage? Specialty, DateTime ReadyAt);

public partial class Person
{
    [JsonRequired] public CashAccount PersonalAccount { get; set; } = new();
    [JsonRequired] public List<Employment> EmploymentHistory { get; set; } = new();
    [JsonRequired] public bool IsProdigy { get; set; }
    [JsonRequired] public long ExpectedSalary { get; set; }
    [JsonRequired] public int? MainSeriesId { get; set; }
    [JsonIgnore] public Employment? Employment => EmploymentHistory.LastOrDefault(e => e.EndsAt is null);
}

public partial class Series
{
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public int LocationId { get; set; }
    [JsonRequired] public int LeadPersonId { get; set; }
    [JsonRequired] public int RightsLeadPersonId { get; set; }
}

public partial class StageWork
{
    [JsonRequired] public int? ManualAssignee { get; set; }
    [JsonRequired] public double QualityWeightedWork { get; set; }
}

public partial class Chapter
{
    [JsonRequired] public int? CreatorPersonId { get; set; }
}

public static class StudioRules
{
    // Tokyo MHLW annual historical figures: 1995 ¥650/h, 1996 ¥664/h.
    // ¥700 and the salary curve are deliberately proposed game balance, not observed assistant pay.
    public const long MinimumMonthlySalary = 122000;
    public static long ExpectedSalary(int bestSkill) => Math.Max(MinimumMonthlySalary,
        (long)Math.Round((60000 + 1400 * bestSkill) / 1000d, MidpointRounding.AwayFromZero) * 1000);
    public static long HiringReserve(long salary) => (long)Math.Ceiling(salary * 7m / 30m);
    /// <summary>Months of wages and running costs, counting confirmed page fees, before hiring is called safe.</summary>
    public const int SafeRunwayMonths = 3;
    public const int ConfirmedIncomeDays = 90;
}

/// <param name="Cash">Free business cash after reserved wages and bills.</param>
/// <param name="ConfirmedIncome">Page fees from signed serializations over the next 90 days, after the creator share.</param>
/// <param name="MonthlyCosts">Salaries, rent, utilities, recurring charges and loan repayments per month.</param>
public sealed record HiringRunway(long Cash, long ConfirmedIncome, long MonthlyCosts)
{
    /// <summary>Months covered, or null when there are no monthly costs.</summary>
    public double? Months => MonthlyCosts <= 0 ? null : (double)(Cash + ConfirmedIncome) / MonthlyCosts;
    public bool Safe => Months is not { } months || months >= StudioRules.SafeRunwayMonths;
}
