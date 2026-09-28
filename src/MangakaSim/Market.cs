using System.Text.Json.Serialization;
using MangakaSim.Catalog;
using MangakaSim.Rules;

namespace MangakaSim;

public enum PublishingStatus { Unpublished, Pitching, Offered, Serialized }
public enum EditorStatus { NotRequired, AwaitingReview, Approved, RedoRequested }
public enum VolumeFormat { Tankobon, DoujinIssue }
public sealed record Contract(int Id, string MagazineId, long FeePerPage, DateTime SignedAt, DateTime FirstIssueClose)
{
    [JsonRequired]
    public int ChaptersPublished { get; set; }
}
public sealed record SerializationOffer(string MagazineId, long FeePerPage, DateTime FirstIssueClose, DateTime ExpiresAt);
public sealed record LedgerEntry(DateTime Time, long Amount, string Reason, int? SeriesId,
    AccountEntryKind Kind = AccountEntryKind.Publishing, int? TransferId = null);
public sealed record RankEntry(int Rank, string Title, int? SeriesId, int? FillerId, double Score);
internal sealed record IssueCloseContext(string MagazineId, DateTime CloseTime, int IssueNumber);

public class FillerSeries
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string Title { get; set; } = "";
    [JsonRequired]
    public string Genre { get; set; } = "other";
    [JsonRequired]
    public double Popularity { get; set; }
    [JsonRequired]
    public bool IsIconic { get; set; }
    [JsonRequired]
    public int IssuesBelowLine { get; set; }
}
public class MagazineState
{
    [JsonRequired]
    public string MagazineId { get; set; } = "";
    [JsonRequired]
    public DateTime NextIssueClose { get; set; }
    [JsonRequired]
    public List<FillerSeries> Fillers { get; set; } = new();
    [JsonRequired]
    public List<FillerSeries> RetiredFillers { get; set; } = new();
    [JsonRequired]
    public List<RankEntry> LastRanking { get; set; } = new();
    [JsonRequired]
    public int IssuesClosed { get; set; }
}
public class GenreTrend
{
    [JsonRequired]
    public string Genre { get; set; } = "other";
    [JsonRequired]
    public double Noise { get; set; }
    [JsonRequired]
    public double Boom { get; set; }
    [JsonRequired]
    public double BoomPeak { get; set; }
    [JsonRequired]
    public double BoomFloor { get; set; }
    [JsonRequired]
    public double PermanentBoom { get; set; }
    [JsonRequired]
    public bool BoomFloorChosen { get; set; }
    [JsonRequired]
    public DateTime? BoomEndsAt { get; set; }
    [JsonRequired]
    public DateTime? BoomFadeEndsAt { get; set; }
    [JsonRequired]
    public double PlayerInfluence { get; set; }
}
public sealed partial class Volume
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public int Number { get; set; }
    [JsonRequired]
    public VolumeFormat Format { get; set; }
    [JsonRequired]
    public int FirstChapter { get; set; }
    [JsonRequired]
    public int LastChapter { get; set; }
    [JsonRequired]
    public List<int> ChapterIds { get; set; } = new();
    [JsonRequired]
    public DateTime ReleaseDate { get; set; }
    [JsonRequired]
    public DateTime? ReleasedAt { get; set; }
    [JsonRequired]
    public long CopiesSold { get; set; }
    [JsonRequired]
    public int WeeksOnSale { get; set; }
    [JsonRequired]
    public int SalesWindowWeeks { get; set; }
    [JsonRequired]
    public bool SalesClosed { get; set; }
    [JsonRequired]
    public bool IsDoujin { get; set; }
    [JsonRequired]
    public double AverageQuality { get; set; }
}
public partial class Series
{
    [JsonRequired]
    public PublishingStatus Publishing { get; set; }
    [JsonRequired]
    public Contract? Contract { get; set; }
    [JsonRequired]
    public List<Contract> PastContracts { get; set; } = new();
    [JsonRequired]
    public SerializationOffer? PendingOffer { get; set; }
    [JsonRequired]
    public Cadence DoujinCadence { get; set; }
    [JsonRequired]
    public DateTime? NextChapterDueOverride { get; set; }
    [JsonRequired]
    public int NextChapterNumber { get; set; } = 1;
    [JsonRequired]
    public double Fanbase { get; set; }
    [JsonRequired]
    public double CulturalImpact { get; set; }
    [JsonRequired]
    public bool IsIconic { get; set; }
    [JsonRequired]
    public List<DateTime> Strikes { get; set; } = new();
    [JsonRequired]
    public int WeeksBelowLine { get; set; }
    [JsonRequired]
    public DateTime? WarningIssuedAt { get; set; }
    [JsonRequired]
    public Dictionary<string, DateTime> PitchCooldowns { get; set; } = new();
    [JsonRequired]
    public List<Volume> Volumes { get; set; } = new();
    [JsonRequired]
    public int ChaptersPublished { get; set; }
    [JsonRequired]
    public int? LastRank { get; set; }
    [JsonRequired]
    public bool MillionCopyInfluenceAwarded { get; set; }
    [JsonRequired]
    public Dictionary<int, long> LifetimeHoursByPerson { get; set; } = new();
}
public partial class Chapter
{
    [JsonRequired]
    public int Pages { get; set; }
    [JsonRequired]
    public int? Quality { get; set; }
    [JsonRequired]
    public EditorStatus Editor { get; set; }
    [JsonRequired]
    public DateTime? EditorDecisionAt { get; set; }
    [JsonRequired]
    public string? EditorMagazineId { get; set; }
    [JsonRequired]
    public int RedoCount { get; set; }
    [JsonRequired]
    public bool IsOneShot { get; set; }
    [JsonRequired]
    public string? PitchMagazineId { get; set; }
    [JsonRequired]
    public bool PitchResolved { get; set; }
    [JsonRequired]
    public bool DoujinEligible { get; set; }
    /// <summary>An older draft of a revised contest manuscript: finished work kept for history, never a magazine or doujin chapter. Optional for older saves.</summary>
    public bool Superseded { get; set; }
    /// <summary>A chapter that belongs to the magazine run: not a pitch sample, not doujin material, not an old contest draft.</summary>
    [JsonIgnore] public bool MagazineBound => !IsOneShot && !DoujinEligible && !Superseded;
    [JsonRequired]
    public DateTime? PublishedAt { get; set; }
    [JsonRequired]
    public string? PublishedMagazineId { get; set; }
    [JsonRequired]
    public int? PublishedContractId { get; set; }
    [JsonRequired]
    public int? Rank { get; set; }
}
public partial class StageWork
{
    [JsonRequired]
    public double OvertimeHours { get; set; }
    [JsonRequired]
    public double Contribution { get; set; }
    [JsonRequired]
    public Dictionary<int, long> HoursByPerson { get; set; } = new();
}
public partial class Person
{
    [JsonRequired]
    public double Reputation { get; set; } = 10;
}
public partial class GameState
{
    [JsonIgnore]
    public long Money { get => ControlledBusiness.Account.Balance; set => ControlledBusiness.Account.Balance = value; }
    [JsonIgnore]
    public List<LedgerEntry> Ledger => ControlledBusiness.Account.Entries;
    [JsonIgnore]
    public double StudioTrackRecord { get => ControlledBusiness.TrackRecord; set => ControlledBusiness.TrackRecord = value; }
    [JsonRequired]
    public List<MagazineState> Markets { get; set; } = new();
    [JsonRequired]
    public List<GenreTrend> Trends { get; set; } = new();
    [JsonIgnore]
    public bool HasInternet { get => ControlledBusiness.HasInternet; set => ControlledBusiness.HasInternet = value; }
    [JsonRequired]
    public DateTime? LastTrendUpdateMonth { get; set; }
    [JsonRequired]
    public long DoujinCopiesThisMonth { get; set; }
    [JsonRequired]
    public double DoujinFansThisMonth { get; set; }
    [JsonRequired]
    public DateTime? LastSalesAt { get; set; }
    [JsonIgnore] public PublisherCatalog PublisherCatalog => PublisherCatalog.LoadDefault();
    [JsonIgnore] public TrendCatalog TrendCatalog => TrendCatalog.LoadDefault();
    [JsonIgnore] public double EffectiveReputation => ReputationRules.Effective(StudioTrackRecord, ControlledStaff.Select(p => p.Reputation));
    [JsonIgnore] public double StaffReputation => ReputationRules.StaffTerm(ControlledStaff.Select(p => p.Reputation));
    public double GenrePopularity(string genre)
    {
        var key = TrendRules.Normalise(genre, TrendCatalog);
        var trend = Trends.First(t => t.Genre == key);
        return TrendRules.Effective(TrendRules.Baseline(TrendCatalog, key, Clock.Now), trend.Noise, trend.Boom, trend.PlayerInfluence);
    }
    public double Protection(Series series) => ReputationRules.Protection(series.ChaptersPublished, series.Fanbase, series.CulturalImpact);
}
