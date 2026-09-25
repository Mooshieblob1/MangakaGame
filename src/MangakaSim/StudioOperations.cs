using System.Text.Json.Serialization;

namespace MangakaSim;

public enum MoonlightingPolicy { Allowed, Limited, Prohibited }
public enum PrintTier { CopyShop, LocalPrinter, BulkPrinter }
public enum StudioAction { SetPipeline, SetWellbeing, SetMoonlighting, SetFounderSalary, SetEmployeeSalary, DismissBuyout, Incorporate, BorrowPersonal, BorrowCard, BorrowBusiness, RepayLoan, RecoveryCommission, Lease, Move, CloseLocation, TransferStaff, BreakRoom, Print, AutoPrint, BookConvention, CancelConvention, Promote, Campaign, AcceptProposal, ReleaseRights, QuoteCareer, CareerHome, CareerStudio, CareerEmployer }
public record StudioActionCommand(StudioAction Action, int Target = 0, int Secondary = 0, long Amount = 0, int Value = 0, bool Enabled = false, List<int>? Followers = null, int ReservedCopies = 0) : ICommand;

public sealed partial class Business
{
    [JsonRequired] public bool Incorporated { get; set; }
    [JsonRequired] public bool Independent { get; set; }
    [JsonRequired] public MoonlightingPolicy Moonlighting { get; set; }
    [JsonRequired] public long DiscretionarySpent { get; set; }
    [JsonRequired] public DateTime FoundedAt { get; set; } = GameClock.Start;
    [JsonRequired] public int EmployerTier { get; set; }
    [JsonRequired] public DateTime? LastIntroduction { get; set; }
}
public sealed partial class StudioLocation
{
    [JsonRequired] public int PropertyTier { get; set; }
    [JsonRequired] public int BreakSeats { get; set; } = 2;
    [JsonRequired] public int Storage { get; set; } = 200;
    [JsonRequired] public int Atmosphere { get; set; }
    [JsonRequired] public DateTime? NextRentAt { get; set; }
    [JsonRequired] public long Deposit { get; set; }
    [JsonRequired] public bool Closed { get; set; }
    [JsonRequired] public DateTime? ClosedAt { get; set; }
}
public partial class Person
{
    [JsonRequired] public double Food { get; set; } = 100;
    [JsonRequired] public double Drink { get; set; } = 100;
    [JsonRequired] public double Comfort { get; set; } = 100;
    [JsonRequired] public double Happiness { get; set; } = 70;
    [JsonRequired] public double Loyalty { get; set; } = 60;
    [JsonRequired] public int LowMoodDays { get; set; }
    [JsonRequired] public int WeeklyOvertimeLimit { get; set; } = 6;
    [JsonRequired] public int WeeklyOvertime { get; set; }
    [JsonRequired] public int DailyOvertimeLimit { get; set; } = 2;
    [JsonRequired] public int LowNeedHours { get; set; }
    [JsonRequired] public int BreakWait { get; set; }
    [JsonRequired] public DateTime? BusyUntil { get; set; }
    [JsonRequired] public DateTime? ProvisionsAt { get; set; }
    [JsonRequired] public bool HasProvisions { get; set; }
    [JsonRequired] public bool Resigning { get; set; }
    [JsonRequired] public MoonlightingPolicy? MoonlightingOverride { get; set; }
    [JsonRequired] public DateTime? SideProjectUntil { get; set; }
    [JsonRequired] public int SideProjectEvenings { get; set; }
    [JsonRequired] public Dictionary<Stage, double> Experience { get; set; } = new();
    [JsonRequired] public int ProductiveHours { get; set; }
    [JsonRequired] public bool HiddenTalent { get; set; }
    [JsonRequired] public DateTime? MentoredWeek { get; set; }
    [JsonRequired] public DateTime? RecoveryAt { get; set; }
    [JsonRequired] public int RecoveryHours { get; set; }
    [JsonIgnore] public double LowestNeed => Math.Min(Food, Math.Min(Drink, Comfort));
}
public partial class Chapter
{
    [JsonRequired] public int? PublishedBusinessId { get; set; }
}
public partial class Series
{
    [JsonRequired] public int PipelineLimit { get; set; } = 2;
    [JsonRequired] public int BufferLimit { get; set; } = 2;
    [JsonRequired] public int MasterLimit { get; set; } = 1;
    [JsonRequired] public bool AwaitingCreatorDestination { get; set; }
    [JsonRequired] public bool ReleaseWithCreator { get; set; }
    [JsonRequired] public int LastBreakthroughChapter { get; set; } = -12;
    [JsonRequired] public int? PromoterId { get; set; }
    [JsonRequired] public bool PromotionPriority { get; set; }
    [JsonRequired] public double Reach { get; set; }
    [JsonRequired] public int PromotionHours { get; set; }
    [JsonRequired] public int CampaignHours { get; set; }
    [JsonRequired] public DateTime? CampaignUntil { get; set; }
    [JsonRequired] public bool AutoPrint { get; set; }
    [JsonRequired] public long PrintBudget { get; set; }
    [JsonRequired] public int PrintTarget { get; set; } = 50;
    [JsonRequired] public int PrintTierMask { get; set; } = 7;
}
public sealed partial class Volume
{
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public Dictionary<int, int> CreatorShares { get; set; } = new();
    [JsonRequired] public int PrintedPages { get; set; }
    [JsonRequired] public long Price { get; set; }
    [JsonRequired] public long Contribution { get; set; }
    [JsonRequired] public long CreatorAccrued { get; set; }
    [JsonRequired] public int WeeklyDemand { get; set; }
    [JsonRequired] public DateTime? DemandWeek { get; set; }
}
public sealed class PrintRun
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int VolumeId { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public int LocationId { get; set; }
    [JsonRequired] public PrintTier Tier { get; set; }
    [JsonRequired] public int Quantity { get; set; }
    [JsonRequired] public int Remaining { get; set; }
    [JsonRequired] public long Cost { get; set; }
    [JsonRequired] public DateTime OrderedAt { get; set; }
    [JsonRequired] public DateTime DueAt { get; set; }
    [JsonRequired] public bool Delivered { get; set; }
}
public sealed class Bill
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public int? PersonId { get; set; }
    [JsonRequired] public int? LocationId { get; set; }
    [JsonRequired] public string Reason { get; set; } = "";
    [JsonRequired] public long Original { get; set; }
    [JsonRequired] public long Remaining { get; set; }
    [JsonRequired] public DateTime DueAt { get; set; }
}
public sealed class Loan
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int? BusinessId { get; set; }
    [JsonRequired] public int PersonId { get; set; }
    [JsonRequired] public long Principal { get; set; }
    [JsonRequired] public long Original { get; set; }
    [JsonRequired] public long InterestPaid { get; set; }
    [JsonRequired] public decimal Interest { get; set; }
    [JsonRequired] public decimal Apr { get; set; }
    [JsonRequired] public int Term { get; set; }
    [JsonRequired] public bool Card { get; set; }
    [JsonRequired] public long ScheduledPrincipalDue { get; set; }
    [JsonRequired] public long Arrears { get; set; }
    [JsonRequired] public DateTime NextPayment { get; set; }
}
public sealed class StaffProposal
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int PersonId { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public DateTime ExpiresAt { get; set; }
    [JsonRequired] public string Title { get; set; } = "";
}
public sealed class ConventionBooking
{
    public Dictionary<int,int> ReservedStock { get; set; } = new();
    public int? SeriesId { get; set; }
    public long CopiesSold { get; set; }
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public int PersonId { get; set; }
    [JsonRequired] public int? AssistantId { get; set; }
    [JsonRequired] public DateTime Date { get; set; }
    [JsonRequired] public string District { get; set; } = "Koto";
    [JsonRequired] public int Scale { get; set; }
    [JsonRequired] public long Fee { get; set; }
    [JsonRequired] public long TravelCost { get; set; }
    [JsonRequired] public int TravelHours { get; set; }
    [JsonRequired] public int StaffedHours { get; set; }
    [JsonRequired] public bool Settled { get; set; }
    [JsonRequired] public bool Cancelled { get; set; }
}
public sealed record PropertyOffer(int Id, string District, int Tier, int Seats, long Rent, int Storage, int Atmosphere);
public static class TokyoProperties
{
    // Area selection is geographic; amounts are game estimates, not historic property quotations.
    public static readonly PropertyOffer[] All = Enumerable.Range(0, 16).Select(i => new PropertyOffer(i + 1,
        new[] { "Nerima", "Itabashi", "Adachi", "Edogawa", "Suginami", "Nakano", "Katsushika", "Ota", "Toshima", "Bunkyo", "Sumida", "Koto", "Shinjuku", "Chiyoda", "Shibuya", "Minato" }[i],
        i / 4 + 1, new[] { 4, 8, 16, 32 }[i / 4],
        new long[] { 60000, 70000, 80000, 90000, 120000, 140000, 160000, 190000, 250000, 290000, 330000, 380000, 500000, 600000, 700000, 800000 }[i],
        new[] { 1000, 3000, 8000, 20000 }[i / 4], i % 4 * 2 - 3)).ToArray();
    // Ward/city centres rounded to ~1 km. Routing adds 25%; this is an estimate, not a journey planner.
    public static readonly Dictionary<string, (double Lat, double Lon)> Centres = new()
    {
        ["Chiyoda"]=(35.694,139.754), ["Chuo"]=(35.671,139.772), ["Minato"]=(35.659,139.751),
        ["Shinjuku"]=(35.694,139.703), ["Bunkyo"]=(35.708,139.752), ["Taito"]=(35.713,139.780),
        ["Sumida"]=(35.711,139.801), ["Koto"]=(35.673,139.817), ["Shinagawa"]=(35.609,139.730),
        ["Meguro"]=(35.641,139.698), ["Ota"]=(35.561,139.716), ["Setagaya"]=(35.646,139.653),
        ["Shibuya"]=(35.664,139.698), ["Nakano"]=(35.707,139.664), ["Suginami"]=(35.700,139.636),
        ["Toshima"]=(35.726,139.716), ["Kita"]=(35.753,139.734), ["Arakawa"]=(35.736,139.783),
        ["Itabashi"]=(35.751,139.709), ["Nerima"]=(35.736,139.652), ["Adachi"]=(35.775,139.804),
        ["Katsushika"]=(35.743,139.847), ["Edogawa"]=(35.706,139.868),
        ["Musashino"]=(35.718,139.566), ["Mitaka"]=(35.684,139.559), ["Kawasaki"]=(35.531,139.703),
        ["Yokohama"]=(35.444,139.638), ["Urawa"]=(35.861,139.646), ["Chiba"]=(35.608,140.106), ["Ariake"]=(35.630,139.795)
    };
    public static (double Km, long Fare, int Hours) Travel(string from, string to)
    {
        var a = Centres[from]; var b = Centres[to];
        var km = Math.Sqrt(Math.Pow((a.Lat-b.Lat)*111,2)+Math.Pow((a.Lon-b.Lon)*90,2))*1.25;
        var fare = km <= 3 ? 0 : km <= 4 ? 170 : km <= 9 ? 210 : km <= 15 ? 250 : 250 + 50*(long)Math.Ceiling((km-15)/6);
        return (km, fare, Math.Max(1, (int)Math.Ceiling(km / (fare == 0 ? 8 : 25) + (fare == 0 ? 0 : .25))));
    }
}
