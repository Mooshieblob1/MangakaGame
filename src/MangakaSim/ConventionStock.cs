using System.Text.Json;

namespace MangakaSim;

public sealed record ReserveConventionStockCommand(int BookingId,int Copies) : ICommand;

public partial class GameState
{
    public static GameState ImportConvenienceV9(string json)
    {
        try
        {
            using var document=JsonDocument.Parse(json);
            if(document.RootElement.GetProperty(nameof(Version)).GetInt32()!=9)throw new InvalidDataException("Choose a version-9 career.");
            try{FromJson(json);}catch(InvalidDataException ex)when(ex.Message.StartsWith("Save file version 9 is not supported.",StringComparison.Ordinal)){}
            var state=JsonSerializer.Deserialize<GameState>(json,JsonOptions)!;state.ValidateSave();
            state.Version=CurrentVersion;
            // Existing bookings keep their dates; new commands use the more frequent calendar.
            state.World.ReplayCheckpoint=null;state.World.ReplayLogStart=state.CommandLog.Count;
            state.ValidateSave();state.World.ReplayCheckpoint=state.ToJson();return state;
        }
        catch(Exception ex)when(ex is JsonException or InvalidOperationException or ArgumentException or NullReferenceException)
        {throw new InvalidDataException("Could not import this career.",ex);}
    }
    private int ReservedFromRun(int runId,int? exceptBooking=null)=>Bookings.Where(b=>!b.Cancelled&&!b.Settled&&b.Id!=exceptBooking)
        .Sum(b=>b.ReservedStock.GetValueOrDefault(runId));
    public int ConventionReserved(int volumeId,bool deliveredOnly=false)=>PrintRuns.Where(r=>r.VolumeId==volumeId&&(!deliveredOnly||r.Delivered))
        .Sum(r=>ReservedFromRun(r.Id));
    private IEnumerable<PrintRun> ReservableRuns(int? seriesId,DateTime date,int businessId)=>PrintRuns.Where(r=>r.BusinessId==businessId&&
        (r.Delivered||r.DueAt<=date.AddHours(11))&&(seriesId is null||FindSeries(seriesId.Value)!.Volumes.Any(v=>v.Id==r.VolumeId)))
        .OrderBy(r=>r.OrderedAt).ThenBy(r=>r.Id);
    public int ConventionReservable(int? seriesId,DateTime date,int? exceptBooking=null)=>ReservableRuns(seriesId,date,ControlledBusinessId)
        .Sum(r=>Math.Max(0,r.Remaining-ReservedFromRun(r.Id,exceptBooking)));
    private Dictionary<int,int> AllocateConventionStock(int? seriesId,DateTime date,int copies,int? exceptBooking=null)
    {
        if(copies<0||copies>ConventionReservable(seriesId,date,exceptBooking))
            throw new InvalidCommandException("Reserve only unclaimed copies in stock or paid print orders arriving before the event.");
        var result=new Dictionary<int,int>();var remaining=copies;
        foreach(var run in ReservableRuns(seriesId,date,ControlledBusinessId))
        {
            var count=Math.Min(remaining,Math.Max(0,run.Remaining-ReservedFromRun(run.Id,exceptBooking)));
            if(count>0){result.Add(run.Id,count);remaining-=count;}
            if(remaining==0)break;
        }
        return result;
    }
    private void ReserveConventionStock(ReserveConventionStockCommand command)
    {
        var booking=Bookings.SingleOrDefault(b=>b.Id==command.BookingId&&b.BusinessId==ControlledBusinessId&&!b.Cancelled&&!b.Settled)
            ??throw new InvalidCommandException("Choose an active convention booking.");
        if(Clock.Now>=booking.Date.AddHours(11-booking.TravelHours))throw new InvalidCommandException("The attendee has already left for this event.");
        if(booking.SeriesId is {} id)RequireSeries(id);
        booking.ReservedStock=AllocateConventionStock(booking.SeriesId,booking.Date,command.Copies,booking.Id);
    }
    public long SeriesCopiesSold(int seriesId)
    {
        var series=FindSeries(seriesId)??throw new InvalidCommandException("Choose a series.");
        return series.Volumes.Sum(v=>v.CopiesSold)+World.Receipts.Where(r=>series.Volumes.Any(v=>v.Id==r.VolumeId)).Sum(r=>r.Units);
    }
}
