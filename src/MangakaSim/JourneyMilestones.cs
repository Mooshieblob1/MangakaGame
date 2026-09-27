namespace MangakaSim;
/// <summary>
/// T1.10 journey milestones for the session timeline. Classifies the game's own events and polls volumes once per
/// scan, so the timeline shows each T1.1 step without any change to the simulation. Milestones the career already
/// reached (a loaded save, the practice career) are pre-marked so they are not logged again as "first".
/// Known limitation: first-hire is judged by current staff, so a career whose only hire has left may log it again.
/// </summary>
public sealed class JourneyMilestones
{
    private static readonly HashSet<string> Repeating=["pitch-answer rejected","pitch-answer offered","cancellation-warning","cancellation"];
    private readonly HashSet<string> _done=new();
    private readonly HashSet<int> _selling=new();
    private readonly Dictionary<int,int> _runs=new();
    private DateTime? _lastPayday;
    public JourneyMilestones(GameState state)
    {
        foreach(var e in state.Events){var id=Classify(state,e);if(id is not null&&!Repeating.Contains(id))_done.Add(id);}
        Poll(state,new List<string>());
    }
    public IReadOnlyList<string> Observe(GameState state,IEnumerable<GameEvent> fresh)
    {
        var ids=new List<string>();
        foreach(var e in fresh)
        {
            var id=Classify(state,e);if(id is null)continue;
            if(id=="missed-payday")
            {
                if(_lastPayday is {} last&&last.Year==e.Time.Year&&last.Month==e.Time.Month)continue;
                _lastPayday=e.Time;ids.Add(id);continue;
            }
            if(Repeating.Contains(id)||_done.Add(id))ids.Add(id);
        }
        Poll(state,ids);
        return ids;
    }
    private void Poll(GameState state,List<string> ids)
    {
        foreach(var series in state.Series.Where(s=>s.BusinessId==state.ControlledBusinessId))
        {
            if(series.Volumes.Any(v=>v.IsDoujin)&&_done.Add("first-doujin-completed"))ids.Add("first-doujin-completed");
            foreach(var volume in series.Volumes.Where(v=>v.IsDoujin))
            {
                var runs=state.PrintRuns.Count(r=>r.VolumeId==volume.Id);
                var known=_runs.TryGetValue(volume.Id,out var before);_runs[volume.Id]=runs;
                if(volume.CopiesSold<=0)continue;
                if(_selling.Add(volume.Id))
                {
                    if(_done.Add("first-sale"))ids.Add("first-sale");
                    else ids.Add("later-sale");
                }
                else if(known&&runs>before&&runs>1)_pendingReprint.Add(volume.Id);
                if(_pendingReprint.Contains(volume.Id)&&volume.CopiesSold>_soldAtReprint.GetValueOrDefault(volume.Id))
                {
                    _pendingReprint.Remove(volume.Id);ids.Add("later-sale reprint");
                }
                if(!_pendingReprint.Contains(volume.Id))_soldAtReprint[volume.Id]=volume.CopiesSold;
            }
        }
    }
    private readonly HashSet<int> _pendingReprint=new();
    private readonly Dictionary<int,long> _soldAtReprint=new();
    private static string? Classify(GameState state,GameEvent e)
    {
        bool Owned()=>state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==state.ControlledBusinessId);
        return e.Type switch
        {
            EventType.PitchSubmitted when Owned()=>"first-pitch",
            EventType.PitchRejected when Owned()=>"pitch-answer rejected",
            EventType.SerializationOffered when Owned()=>"pitch-answer offered",
            EventType.OfferAccepted when Owned()=>"serialization-accepted",
            EventType.ChapterPublished when Owned()=>"first-magazine-chapter",
            EventType.DeadlineMissed or EventType.IssueMissed when Owned()=>"first-deadline-missed",
            EventType.CancellationWarning when Owned()=>"cancellation-warning",
            EventType.SeriesCancelled when Owned()=>"cancellation",
            EventType.StaffHired when e.PersonId is int id&&id!=state.ProtagonistPersonId&&state.ControlledStaff.Any(p=>p.Id==id)=>"first-hire",
            EventType.WageArrears=>"missed-payday",
            _=>null
        };
    }
}
