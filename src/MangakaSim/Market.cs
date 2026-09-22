namespace MangakaSim;

public enum PublishingStatus { Unpublished, Pitching, Offered, Serialized }
public enum EditorStatus { NotRequired, AwaitingReview, Approved, RedoRequested }
public enum VolumeFormat { Tankobon }

public class LedgerEntry
{
    public DateTime Time { get; set; }
    public long Amount { get; set; }
    public string Reason { get; set; } = "";
    public int? SeriesId { get; set; }
}

public class MagazineState
{
    public string MagazineId { get; set; } = "";
    public DateTime NextIssueClose { get; set; }
    /// <summary>The most recent close, so a step later in the same tick can see that an issue closed now.</summary>
    public DateTime? LastIssueClose { get; set; }
    public List<FillerSeries> Fillers { get; set; } = new();
    public List<RankEntry> LastRanking { get; set; } = new();
    public int IssuesClosed { get; set; }
    /// <summary>Filler ids are unique per magazine and come from this counter.</summary>
    public int NextFillerId { get; set; } = 1;
}

public class FillerSeries
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public double Popularity { get; set; }
    public bool IsIconic { get; set; }
    public int IssuesBelowLine { get; set; }
}

public class RankEntry
{
    public int Rank { get; set; }
    public string Title { get; set; } = "";
    public int? SeriesId { get; set; }
    public int? FillerId { get; set; }
    public double Score { get; set; }
}

public class GenreTrend
{
    public string Genre { get; set; } = "";
    public double Noise { get; set; }
    public double Boom { get; set; }
    public double BoomPeak { get; set; }
    public double BoomFloor { get; set; }
    public bool BoomFading { get; set; }
    public DateTime? BoomEndsAt { get; set; }
    public DateTime? BoomFadeEndsAt { get; set; }
    public double PlayerInfluence { get; set; }
}

public class Volume
{
    public int Id { get; set; }
    public int Number { get; set; }
    public VolumeFormat Format { get; set; } = VolumeFormat.Tankobon;
    /// <summary>Chapter numbers, inclusive.</summary>
    public int FirstChapter { get; set; }
    public int LastChapter { get; set; }
    public DateTime ReleaseDate { get; set; }
    public bool IsReleased { get; set; }
    public long CopiesSold { get; set; }
    public int WeeksOnSale { get; set; }
    public bool IsDoujin { get; set; }
    /// <summary>Mean chapter quality, frozen at creation.</summary>
    public double AverageQuality { get; set; }

    public bool Covers(int chapterNumber) => chapterNumber >= FirstChapter && chapterNumber <= LastChapter;
}

public class Contract
{
    public string MagazineId { get; set; } = "";
    public int FeePerPage { get; set; }
    public DateTime SignedAt { get; set; }
    /// <summary>Chapters published under this contract; the first eight are the grace period.</summary>
    public int ChaptersPublished { get; set; }
}

public class SerializationOffer
{
    public string MagazineId { get; set; } = "";
    public int FeePerPage { get; set; }
    public DateTime FirstIssueClose { get; set; }
    public DateTime ExpiresAt { get; set; }
}
