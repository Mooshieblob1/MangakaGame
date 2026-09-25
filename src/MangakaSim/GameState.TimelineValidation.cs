using System.Text.Json;
using System.Text.Json.Nodes;
using MangakaSim.Catalog;
namespace MangakaSim;

public partial class GameState
{
    private void ValidateTimelineSave(HashSet<int> ids)
    {
        void Check([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool value,string reason){if(!value)throw new InvalidDataException("Save file has invalid timeline "+reason+".");}
        bool Unit(double x)=>double.IsFinite(x)&&x>=0&&x<=100;
        bool Time(DateTime t)=>t>=GameClock.Start&&t.Ticks%TimeSpan.TicksPerHour==0;
        Check(World is not null,"state");var w=World!;
        Check(w.Revision==TimelineCatalog.Default.Revision,"catalog revision");
        Check(w.MarketRng is not null&&w.HiringRng is not null&&w.FutureRng is not null&&w.Processed is not null&&w.Rivals is not null&&w.Assistants is not null&&w.RivalBusinesses is not null&&
            w.Mentoring is not null&&w.News is not null&&w.Reports is not null&&w.Offers is not null&&w.Channels is not null&&w.Receipts is not null&&w.Relations is not null&&w.LastApproach is not null,"required collections");
        Check(w.Rivals!.All(r=>r is not null&&TimelineCatalog.Default.Rivals.Any(d=>d.Id==r.Key)&&Enum.IsDefined(r.Phase))&&w.Rivals.Select(r=>r.Key).Distinct().Count()==w.Rivals.Count,"rival identity");
        foreach(var r in w.Rivals)
        {
            var d=TimelineCatalog.Default.Rivals.Single(d=>d.Id==r.Key);
            var m=Markets.Single(m=>m.MagazineId==d.Magazine);
            Check(d.Start<=Clock.Now,"rival date");
            if(Clock.Now<TimelineCatalog.Cutoff)Check(r.Phase==(d.End<=Clock.Now?RivalPhase.Ended:d.Hiatus<=Clock.Now?RivalPhase.Hiatus:RivalPhase.Active),"protected phase");
            Check(r.Phase==RivalPhase.Ended?(r.EndedAt is {} end&&end<=Clock.Now&&(r.FillerId==0||m.RetiredFillers.Any(f=>f.Id==r.FillerId))):
                r.EndedAt is null&&m.Fillers.Any(f=>f.Id==r.FillerId&&f.Title==d.Title),"rival phase/slot");
        }
        Check(w.Mentoring!.All(m=>IsHistoricalAssistant(m.Key)&&m.Value is not null&&m.Value.Day==m.Value.Day.Date&&m.Value.Day<=Clock.Now&&People.Any(p=>p.Id==m.Value.LearnerId)),"mentoring");
        Check(w.Assistants!.All(a=>a is not null)&&w.Assistants.Count==2&&w.Assistants.Select(a=>a.Key).Distinct().Count()==2,"creator identity");
        foreach(var a in w.Assistants)
        {
            Check(TimelineCatalog.Default.Creators.Any(c=>c.Id==a.Key)&&a.HireRoll is >=0 and <1&&Unit(a.Relationship)&&
                (a.PersonId is null||People.Any(p=>p.Id==a.PersonId))&&(a.DiscoveredBy is null||Businesses.Any(b=>b.Id==a.DiscoveredBy))&&
                (a.ContactBusiness is null||Businesses.Any(b=>b.Id==a.ContactBusiness)),"assistant references");
            Check(a.PersonId is null||!Series.Any(s=>s.LeadPersonId==a.PersonId||s.RightsLeadPersonId==a.PersonId),"historical lead restriction");
        }
        Check(w.RivalBusinesses!.Distinct().Count()==w.RivalBusinesses.Count&&w.RivalBusinesses.All(id=>Businesses.Any(b=>b.Id==id)),"rival businesses");
        Check(w.News!.Count<=500&&w.News.All(n=>n is not null&&Time(n.Time)&&n.Time<=Clock.Now&&!string.IsNullOrWhiteSpace(n.Message)),"news");
        Check(w.Reports!.All(r=>r is not null&&People.Any(p=>p.Id==r.PersonId)&&Businesses.Any(b=>b.Id==r.BusinessId)&&Time(r.ReadyAt)&&
            r.Skills is not null&&r.Skills.All(k=>Enum.IsDefined(k.Key)&&k.Value is >=0 and <=100)&&r.ExpectedSalary>=0&&
            (r.ObservedAt is null||r.ObservedAt<=Clock.Now&&r.ObservedAt>=r.ReadyAt)),"scouting");
        Check(w.Reports.Select(r=>(r.PersonId,r.BusinessId)).Distinct().Count()==w.Reports.Count,"duplicate scouting");
        foreach(var o in w.Offers!)
        {
            Check(o is not null&&o.Id>0&&ids.Add(o.Id),"offer IDs");
            Check(People.Any(p=>p.Id==o!.PersonId)&&Businesses.Any(b=>b.Id==o!.FromBusiness)&&Businesses.Any(b=>b.Id==o!.ToBusiness)&&o!.FromBusiness!=o.ToBusiness&&
                Locations.Any(l=>l.Id==o.LocationId&&l.BusinessId==o.ToBusiness)&&o.Salary>=StudioRules.MinimumMonthlySalary&&o.Salary<=10000000&&
                Time(o.CreatedAt)&&o.CreatedAt<=Clock.Now&&o.EndsAt==o.CreatedAt.AddDays(7)&&o.Roll is >=0 and <1&&Enum.IsDefined(o.Status)&&o.Outcome is not null&&o.Followers is not null&&o.Followers.All(f=>People.Any(p=>p.Id==f.Key)&&f.Value is >=0 and <1),"offer terms");
        }
        Check(w.Offers.Where(o=>o.Status==NegotiationStatus.Pending).GroupBy(o=>o.PersonId).All(g=>g.Count()==1),"duplicate pending offer");
        foreach(var a in w.Channels!)
        {
            Check(a is not null&&a.Id>0&&ids.Add(a.Id),"channel IDs");
            Check(FindSeries(a!.SeriesId) is not null&&Businesses.Any(b=>b.Id==a.BusinessId)&&Enum.IsDefined(a.Channel)&&Enum.IsDefined(a.Status)&&
                a.Cost==(a.DirectDoujin?0:a.Channel==ReleaseChannel.DomesticDigital?30000:100000)&&a.Roll is >=0 and <1&&Unit(a.InternationalInterest)&&
                Time(a.CreatedAt)&&a.CreatedAt<=Clock.Now&&a.ResolvesAt>=a.CreatedAt&&a.VolumeIds is not null&&a.VolumeIds.Distinct().Count()==a.VolumeIds.Count&&
                a.VolumeIds.All(id=>FindSeries(a.SeriesId)!.Volumes.Any(v=>v.Id==id))&&a.Reason is not null,"channel terms");
            Check(!a.DirectDoujin||a.Channel==ReleaseChannel.DomesticDigital&&a.Status==NegotiationStatus.Accepted&&a.ResolvesAt==a.CreatedAt&&
                a.VolumeIds.Count==1&&FindSeries(a.SeriesId)!.Volumes.Any(v=>v.Id==a.VolumeIds[0]&&v.IsDoujin&&v.BusinessId==a.BusinessId&&v.ReleasedAt is not null),"direct download terms");
        }
        Check(w.Receipts!.All(r=>r is not null&&w.Channels.Any(a=>a.Id==r.AgreementId&&a.Status==NegotiationStatus.Accepted&&a.VolumeIds.Contains(r.VolumeId))&&
            Time(r.Week)&&r.Week<=Clock.Now&&r.Week.DayOfWeek==DayOfWeek.Monday&&r.Units>=0&&r.NetYen>=0),"channel receipts");
        Check(w.Receipts.Select(r=>(r.AgreementId,r.VolumeId,r.Week)).Distinct().Count()==w.Receipts.Count,"duplicate channel receipt");
        Check(w.Relations!.All(k=>k.Value is >=0 and <=50)&&w.LastApproach!.All(k=>People.Any(p=>p.Id==k.Key)&&Time(k.Value)&&k.Value<=Clock.Now),"relations/cooldowns");
        Check(w.Rivals.Count==TimelineCatalog.Default.Rivals.Count(d=>d.Start<=Clock.Now),"missing historical rivals");
        foreach(var d in TimelineCatalog.Default.Rivals)
        {
            Check(w.Processed!.Contains("launch:"+d.Id)==(d.Start<=Clock.Now)&&w.Processed.Contains("end:"+d.Id)==(d.End<=Clock.Now)&&w.Processed.Contains("hiatus:"+d.Id)==(d.Hiatus<=Clock.Now),"processed historical dates");
            if(d.End<=Clock.Now)Check(w.Rivals.Single(r=>r.Key==d.Id).Phase==RivalPhase.Ended,"protected ending");
        }
        foreach(var e in TimelineCatalog.Default.Events)Check(w.Processed!.Contains("industry:"+e.Id)==(e.At<=Clock.Now),"industry unlock date");
        Check(w.SimulatedFuture==(Clock.Now>=TimelineCatalog.Cutoff),"future boundary");
        Check(w.LastWeek is {} week&&week==Monday(week)&&week<=Clock.Now&&w.LastMonth is {} month&&month.Day==1&&month==month.Date&&month<=Clock.Now&&
            w.LastQuarter is {} quarter&&quarter.Day==1&&quarter.Month%3==1&&quarter==quarter.Date&&quarter<=Clock.Now,"scheduler dates");
        Check(w.Offers.All(o=>o.Status!=NegotiationStatus.Pending||o.EndsAt>Clock.Now),"expired negotiation");
        Check(w.ReplayLogStart>=0&&w.ReplayLogStart<=CommandLog.Count,"replay boundary");
        if(w.ReplayCheckpoint is {} checkpoint)
        {
            using var doc=JsonDocument.Parse(checkpoint);var root=doc.RootElement;
            Check(root.TryGetProperty(nameof(Version),out var version)&&version.GetInt32()==Version&&root.TryGetProperty(nameof(World),out var world)&&
                world.TryGetProperty(nameof(TimelineWorld.ReplayCheckpoint),out var nested)&&nested.ValueKind==JsonValueKind.Null&&
                root.GetProperty(nameof(CommandLog)).GetArrayLength()==w.ReplayLogStart&&root.GetProperty(nameof(Clock)).GetProperty("Now").GetDateTime()<=Clock.Now,"import checkpoint");
        }
    }
    public static GameState ImportTimelineV4(string json)
    {
        try
        {
            var node=JsonNode.Parse(json)?.AsObject()??throw new InvalidDataException("Save is empty.");
            if(node[nameof(Version)]?.GetValue<int>()!=4)throw new InvalidDataException("Choose a version-4 office save. Keep your original file.");
            foreach(var name in new[]{nameof(Clock),nameof(People),nameof(Series),nameof(Events),nameof(Rng),nameof(RngSeed),nameof(Settings),nameof(NextId),nameof(CommandLog),nameof(RecapWindowStart),nameof(RecapFiredToday),nameof(Businesses),nameof(Locations),nameof(Markets),nameof(Trends),nameof(DoujinCopiesThisMonth),nameof(DoujinFansThisMonth)})
                if(node[name] is null)throw new InvalidDataException($"Save file is missing {name}.");
            foreach(var name in new[]{nameof(LastTrendUpdateMonth),nameof(LastSalesAt)})if(!node.ContainsKey(name))throw new InvalidDataException($"Save file is missing {name}.");
            foreach(var location in node[nameof(Locations)]!.AsArray())
                foreach(var field in new[]{"PropertyOfferId","FloorPlanId","OfficeRevision","BaseAtmosphere","FixedBreakSeats"})
                    if(location?[field] is null)throw new InvalidDataException($"Save file is missing location {field}.");
            node[nameof(Career)]=JsonSerializer.SerializeToNode(new CareerHistory(),JsonOptions);
            node[nameof(Progression)]=JsonSerializer.SerializeToNode(new ProgressionState(),JsonOptions);
            node[nameof(World)]=JsonSerializer.SerializeToNode(new TimelineWorld(),JsonOptions);
            var state=JsonSerializer.Deserialize<GameState>(node.ToJsonString(),JsonOptions)??throw new InvalidDataException("Save is empty.");
            state.ValidateSave();
            state.Version=CurrentVersion;state.InitializeTimeline();state.InitializeCareer();state.InitializeProgression(true);state.RefreshOfficeAssignments();
            state.Settings.AutoPause.TryAdd(EventType.IndustryNews,false);state.Settings.AutoPause.TryAdd(EventType.IndustryDecision,true);
            state.TimelineNews("Historical industry initialized at this checkpoint. Earlier income and events were not replayed.");
            state.World.ReplayLogStart=state.CommandLog.Count;
            state.ValidateSave();state.World.ReplayCheckpoint=state.ToJson();return state;
        }
        catch(Exception ex)when(ex is JsonException or InvalidOperationException or ArgumentException or OverflowException)
        {throw new InvalidDataException("Could not import this office save: "+ex.Message,ex);}
    }
    public GameState ReplayTimeline()
    {
        var replay=World.ReplayCheckpoint is {} checkpoint?FromJson(checkpoint):NewGame(RngSeed,Ownership,Protagonist.Name);
        replay.World.ReplayCheckpoint=World.ReplayCheckpoint;
        foreach(var entry in CommandLog.Skip(World.ReplayLogStart))
        {replay.Advance(replay.Clock.HoursUntil(entry.Time));replay.Apply(entry.Command);}
        replay.Advance(replay.Clock.HoursUntil(Clock.Now));return replay;
    }
}
