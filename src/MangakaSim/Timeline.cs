using System.Text.Json.Serialization;
namespace MangakaSim;

public enum TimelineAction { Scout, Approach, CancelOffer, Retain, AcceptCareer, DeclineCareer, HireAssistant, RequestDigital, RequestOverseas }
public record TimelineCommand(TimelineAction Action, int Target = 0, int LocationId = 0, long Salary = 0, List<int>? Followers = null) : ICommand;
public enum RivalPhase { Active, Hiatus, Ended }
public enum NegotiationStatus { Pending, Accepted, Declined, Cancelled, Lapsed }
public enum ReleaseChannel { DomesticDigital, Overseas }
public sealed record IndustryNews(DateTime Time, string Message, bool Simulated = false);
public sealed class HistoricalRival
{
    [JsonRequired] public string Key { get; set; } = "";
    [JsonRequired] public int FillerId { get; set; }
    [JsonRequired] public RivalPhase Phase { get; set; }
    [JsonRequired] public DateTime? EndedAt { get; set; }
    [JsonRequired] public DateTime? LastFutureEvent { get; set; }
}
public sealed record MentorDay(DateTime Day, int LearnerId);
public sealed class CreatorOpportunity
{
    [JsonRequired] public string Key { get; set; } = "";
    [JsonRequired] public int? PersonId { get; set; }
    [JsonRequired] public int? DiscoveredBy { get; set; }
    [JsonRequired] public bool Departed { get; set; }
    [JsonRequired] public double Relationship { get; set; }
    [JsonRequired] public int? ContactBusiness { get; set; }
    [JsonRequired] public DateTime? LastContact { get; set; }
    [JsonRequired] public double HireRoll { get; set; }
}
public sealed class ScoutReport
{
    [JsonRequired] public int PersonId { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public DateTime ReadyAt { get; set; }
    [JsonRequired] public DateTime? ObservedAt { get; set; }
    [JsonRequired] public Dictionary<Stage,int> Skills { get; set; } = new();
    [JsonRequired] public long ExpectedSalary { get; set; }
}
public sealed class StaffNegotiation
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int PersonId { get; set; }
    [JsonRequired] public int FromBusiness { get; set; }
    [JsonRequired] public int ToBusiness { get; set; }
    [JsonRequired] public int LocationId { get; set; }
    [JsonRequired] public long Salary { get; set; }
    [JsonRequired] public DateTime CreatedAt { get; set; }
    [JsonRequired] public DateTime EndsAt { get; set; }
    [JsonRequired] public NegotiationStatus Status { get; set; }
    [JsonRequired] public double Roll { get; set; }
    [JsonRequired] public bool Warned { get; set; }
    [JsonRequired] public bool PlayerApproach { get; set; }
    [JsonRequired] public Dictionary<int,double> Followers { get; set; } = new();
    [JsonRequired] public string Outcome { get; set; } = "Awaiting a decision";
}
public sealed class ChannelAgreement
{
    public bool DirectDoujin { get; set; }
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public int SeriesId { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public ReleaseChannel Channel { get; set; }
    [JsonRequired] public DateTime CreatedAt { get; set; }
    [JsonRequired] public DateTime ResolvesAt { get; set; }
    [JsonRequired] public NegotiationStatus Status { get; set; }
    [JsonRequired] public long Cost { get; set; }
    [JsonRequired] public double Roll { get; set; }
    [JsonRequired] public string Reason { get; set; } = "Publisher is considering the proposal";
    [JsonRequired] public List<int> VolumeIds { get; set; } = new();
    [JsonRequired] public double InternationalInterest { get; set; }
}
public sealed class ChannelReceipt
{
    [JsonRequired] public int AgreementId { get; set; }
    [JsonRequired] public int VolumeId { get; set; }
    [JsonRequired] public DateTime Week { get; set; }
    [JsonRequired] public long Units { get; set; }
    [JsonRequired] public long NetYen { get; set; }
}
public sealed class TimelineWorld
{
    [JsonRequired] public int Revision { get; set; } = 1;
    [JsonRequired] public Rng MarketRng { get; set; } = Rng.FromSeed(1);
    [JsonRequired] public Rng HiringRng { get; set; } = Rng.FromSeed(2);
    [JsonRequired] public Rng FutureRng { get; set; } = Rng.FromSeed(3);
    [JsonRequired] public HashSet<string> Processed { get; set; } = new();
    [JsonRequired] public List<HistoricalRival> Rivals { get; set; } = new();
    [JsonRequired] public List<CreatorOpportunity> Assistants { get; set; } = new();
    [JsonRequired] public List<int> RivalBusinesses { get; set; } = new();
    [JsonRequired] public Dictionary<int,MentorDay> Mentoring { get; set; } = new();
    [JsonRequired] public List<IndustryNews> News { get; set; } = new();
    [JsonRequired] public List<ScoutReport> Reports { get; set; } = new();
    [JsonRequired] public List<StaffNegotiation> Offers { get; set; } = new();
    [JsonRequired] public List<ChannelAgreement> Channels { get; set; } = new();
    [JsonRequired] public List<ChannelReceipt> Receipts { get; set; } = new();
    [JsonRequired] public Dictionary<string,int> Relations { get; set; } = new();
    [JsonRequired] public Dictionary<int,DateTime> LastApproach { get; set; } = new();
    [JsonRequired] public DateTime? LastWeek { get; set; }
    [JsonRequired] public DateTime? LastMonth { get; set; }
    [JsonRequired] public DateTime? LastQuarter { get; set; }
    [JsonRequired] public bool SimulatedFuture { get; set; }
    [JsonRequired] public string? ReplayCheckpoint { get; set; }
    [JsonRequired] public int ReplayLogStart { get; set; }
}
public partial class GameState
{
    [JsonRequired] public TimelineWorld World { get; set; } = new();
}
