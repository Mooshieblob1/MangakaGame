using System.Text.Json;
using MangakaSim.Rules;

namespace MangakaSim;

public enum OutsideJob { None, Afternoons, Evenings }
public sealed record SetOutsideJobCommand(OutsideJob Job) : ICommand;
public sealed record SetDoujinIssuesCommand(int SeriesId, bool Enabled = true) : ICommand;
public sealed record ContinueOneShotCommand(int SeriesId) : ICommand;
public partial class Person { public OutsideJob OutsideJob { get; set; } }
public partial class Series { public bool ReleaseShortIssues { get; set; } }
public sealed partial class Volume { public int EditionNumber { get; set; } }

public partial class GameState
{
    public bool HasPublisherDeadline(Chapter chapter) => !chapter.IsOneShot && !chapter.DoujinEligible &&
        chapter.PublishedAt is null && SeriesOf(chapter).Contract is not null;
    public static string EditionName(Volume book) => book.Format==VolumeFormat.DoujinIssue
        ?$"Issue {book.FirstChapter}":book.IsDoujin&&book.ChapterIds.Count==1?"One-shot":$"Collected book {(book.EditionNumber>0?book.EditionNumber:book.Number)}";
    public int ChaptersTowardCollection(Series series) => series.Chapters.Count(c=>c.Status==ChapterStatus.Complete&&c.DoujinEligible&&c.PublishedAt is null&&
        !series.Volumes.Any(v=>v.Format!=VolumeFormat.DoujinIssue&&v.ChapterIds.Contains(c.Id)));
    public bool AtOutsideJob(Person person, DateTime at) => person.Id==ProtagonistPersonId&&Control==ControlMode.OwnerDirector&&
        at.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday&&
        (person.OutsideJob==OutsideJob.Afternoons&&at.Hour is >=12 and <16||person.OutsideJob==OutsideJob.Evenings&&at.Hour is >=18 and <22);
    public long OutsideHourlyPay => (long)Math.Ceiling(700*Economy.PriceIndex(TrendCatalog,Clock.Now)/10)*10;
    private void OutsideJobStep()
    {
        if(!AtOutsideJob(Protagonist,TickStart))return;
        // Convention or career travel already booked for this hour takes precedence; no pay for an absent shift.
        if(ConventionHour(Protagonist,TickStart)||Protagonist.BusyUntil>TickStart)return;
        AccountPost(Protagonist.PersonalAccount,OutsideHourlyPay,"part-time job",AccountEntryKind.PersonalIncome);
        NeedsChange(Protagonist,-2,-2,-1);Protagonist.BusyUntil=Clock.Now;
        Observe(Protagonist,OfficeActivityKind.OutsideJob);
    }
    private void SetOutsideJob(SetOutsideJobCommand command)
    {
        if(!Enum.IsDefined(command.Job))throw new InvalidCommandException("Choose a part-time schedule.");
        if(command.Job!=OutsideJob.None&&Control!=ControlMode.OwnerDirector)throw new InvalidCommandException("Your studio employer already provides your job and salary.");
        Protagonist.OutsideJob=command.Job;
    }
    private void SetDoujinIssues(SetDoujinIssuesCommand command)
    {
        var series=RequireSeries(command.SeriesId);
        if(series.StandaloneDoujin||series.Publishing!=PublishingStatus.Unpublished)throw new InvalidCommandException("Choose an ongoing self-published series.");
        series.ReleaseShortIssues=command.Enabled;CollectDoujin(series);
    }
    private void ContinueOneShot(ContinueOneShotCommand command)
    {
        var series=RequireSeries(command.SeriesId);
        if(!series.StandaloneDoujin||series.Publishing!=PublishingStatus.Unpublished)
            throw new InvalidCommandException("Choose a self-published one-shot to continue.");
        // Keep the same identity: readers, books, stock, rights, artwork and history
        // continue to refer to this title rather than a duplicated series.
        series.StandaloneDoujin=false;
        series.ReleaseShortIssues=true;
        series.Status=SeriesStatus.Active;
        series.NextChapterDueOverride=CadenceRules.NextDue(Clock.Now,series.Cadence);
        StudioMessage($"{series.Title} continues as an ongoing series. Its genre, readers and existing books are retained.");
    }
    public static GameState ImportProductionV8(string json)
    {
        try
        {
            using var document=JsonDocument.Parse(json);
            if(document.RootElement.GetProperty(nameof(Version)).GetInt32()!=8)throw new InvalidDataException("Choose a version-8 career.");
            try{FromJson(json);}catch(InvalidDataException ex)when(ex.Message.StartsWith("Save file version 8 is not supported.",StringComparison.Ordinal)){}
            var state=JsonSerializer.Deserialize<GameState>(json,JsonOptions)!;state.ValidateSave();
            state.Version=CurrentVersion;
            foreach(var stage in StageOrder.All)state.Protagonist.Skills[stage]=Math.Max(95,state.Protagonist.Skill(stage));
            foreach(var series in state.Series)
            {
                if(!series.StandaloneDoujin&&series.Publishing==PublishingStatus.Unpublished)series.ReleaseShortIssues=true;
                foreach(var chapter in series.Chapters.Where(c=>!state.HasPublisherDeadline(c)))
                {chapter.IsAtRisk=false;chapter.IsLate=false;chapter.HoursOverdue=0;}
            }
            // Balance rules changed. Preserve history, and replay new actions from this explicit boundary.
            state.World.ReplayCheckpoint=null;state.World.ReplayLogStart=state.CommandLog.Count;
            state.ValidateSave();state.World.ReplayCheckpoint=state.ToJson();return state;
        }
        catch(Exception ex)when(ex is JsonException or InvalidOperationException or ArgumentException or NullReferenceException)
        {throw new InvalidDataException("Could not import this career.",ex);}
    }
}
