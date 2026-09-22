namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- volumes

    /// <summary>Finished chapters of the series whose number lies outside every volume.</summary>
    internal List<Chapter> ChaptersOutsideVolumes(Series series, bool publishedOnly) =>
        series.Chapters.Where(c => c.Status == ChapterStatus.Complete && !series.IsInVolume(c.Number) &&
                                   (!publishedOnly || c.IsPublished)).ToList();

    private Volume CreateVolume(Series series, List<Chapter> chapters, bool isDoujin, DateTime releaseDate)
    {
        var volume = new Volume
        {
            Id = AllocateId(),
            Number = series.Volumes.Count + 1,
            Format = VolumeFormat.Tankobon,
            FirstChapter = chapters.Min(c => c.Number),
            LastChapter = chapters.Max(c => c.Number),
            ReleaseDate = releaseDate,
            IsDoujin = isDoujin,
            AverageQuality = chapters.Average(c => c.Quality ?? 0),
        };
        series.Volumes.Add(volume);
        return volume;
    }

    /// <summary>An Unpublished series with five finished chapters outside any volume releases a doujin volume now.</summary>
    internal void TryCreateDoujinVolume(Series series)
    {
        if (series.Publishing != PublishingStatus.Unpublished) return;
        var chapters = ChaptersOutsideVolumes(series, publishedOnly: false);
        if (chapters.Count < SalesRules.DoujinChaptersPerVolume) return;
        var volume = CreateVolume(series, chapters, isDoujin: true, Clock.Now);
        volume.IsReleased = true;
        Emit(EventType.VolumeReleased,
            $"{series.Title} doujin vol.{volume.Number} (ch.{volume.FirstChapter}-{volume.LastChapter}) goes on sale, average quality {volume.AverageQuality:0}.",
            new EventContext(SeriesId: series.Id, VolumeId: volume.Id));
        if (volume.AverageQuality >= 75) AdjustTrackRecord(ReputationRules.DoujinQualityVolume);
    }
}
