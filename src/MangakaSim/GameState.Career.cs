using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MangakaSim;

public partial class GameState
{
    [JsonRequired] public CareerHistory Career { get; set; } = new();
    private void InitializeCareer()
    {
        Career = new() { AvailableFrom=Clock.Now, LastCheck=Clock.Now, NextOffer=Clock.Now,
            LastBusiness=ControlledBusinessId, NarrativeRng=Rng.FromSeed(unchecked(RngSeed ^ 0x48454c50)), PendingScene="beside" };
    }
    private void CareerNarrativeStep()
    {
        if(Career.LastBusiness!=ControlledBusinessId){Career.LastBusiness=ControlledBusinessId;Career.MovedAt=Clock.Now;}
        if(Clock.Hour!=8 || Career.LastCheck.Date==Clock.Now.Date)return;
        Career.LastCheck=Clock.Now;
        if(Career.PendingScene is not null || Clock.Now<Career.NextOffer)return;
        var next=HelperStories.Arc.FirstOrDefault(id=>!Career.Journal.Any(e=>e.Scene==id));
        var last=Career.Journal.LastOrDefault(e=>HelperStories.Arc.Contains(e.Scene))?.At??Career.AvailableFrom;
        bool milestone=Series.Any(s=>s.Volumes.Any(v=>v.ReleasedAt>=last)||s.Contract?.SignedAt>=last);
        bool ready=next switch
        {
            "beside"=>true,
            "page"=>Series.Any(s=>s.Chapters.Any(c=>c.Status==ChapterStatus.Complete)),
            "reader"=>milestone||Clock.Now>=last.AddDays(90),
            "same"=>Career.MovedAt>=last||Events.Any(e=>e.Time>=last&&e.Type is EventType.SeriesCancelled or EventType.DeadlineMissed)||Clock.Now>=last.AddDays(60),
            "ordinary"=>Clock.Now>=last.AddDays(30),
            "still"=>Clock.Now>=last.AddDays(60)||Clock.Now>=last.AddDays(30)&&(milestone||Career.MovedAt>=last),
            _=>false
        };
        if(ready){Career.PendingScene=next;return;}
        if(Clock.DayOfWeek!=DayOfWeek.Monday || Career.Journal.LastOrDefault()?.At.AddDays(14)>Clock.Now)return;
        if(Career.NarrativeRng.NextDouble()>=.25)return;
        var pool=HelperStories.Everyday.Where(id=>(id!="migration"||Career.MovedAt is not null)&&
            !Career.Journal.Any(e=>e.Scene==id&&e.At.AddDays(90)>Clock.Now)).ToArray();
        if(pool.Length>0)Career.PendingScene=pool[Career.NarrativeRng.NextInt(pool.Length)];
    }
    private void ApplyStory(StoryCommand c)
    {
        if(c.Scene!=Career.PendingScene || !HelperStories.Known(c.Scene) || c.Answer is < -1 or > 1)
            throw new InvalidCommandException("That conversation or answer is no longer available.");
        if(c.Defer){Career.DeferredUntil=Clock.Now.Date.AddDays(1).AddHours(8);return;}
        var scene=HelperStories.Describe(this,c.Scene);
        Career.Journal.Add(new(c.Scene,c.Answer,Clock.Now,scene.Text+"\n\n"+(c.Answer==0?scene.First:c.Answer==1?scene.Second:"Conversation skipped")));
        Career.PendingScene=null;Career.DeferredUntil=null;Career.NextOffer=Clock.Now.AddDays(7);
    }
    private void RecordSales(Series s,Volume v,long copies,bool channels=true)
    {
        var week=Monday(Clock.Now);
        var receipts=World.Receipts.Where(r=>r.VolumeId==v.Id&&r.Week==week).ToArray();
        long Units(ReleaseChannel channel)=>channels?receipts.Where(r=>World.Channels.Any(a=>a.Id==r.AgreementId&&a.Channel==channel)).Sum(r=>r.Units):0;
        var existing=Career.Sales.FindIndex(x=>x.At==Clock.Now&&x.Volume==v.Id);
        var sample=new SalesSample(Clock.Now,v.BusinessId,s.Id,v.Id,copies,Units(ReleaseChannel.DomesticDigital),Units(ReleaseChannel.Overseas));
        if(existing<0)Career.Sales.Add(sample);
        else{var old=Career.Sales[existing];Career.Sales[existing]=sample with{Physical=old.Physical+copies,Digital=Math.Max(old.Digital,sample.Digital),Overseas=Math.Max(old.Overseas,sample.Overseas)};}
    }
    private void ValidateCareer()
    {
        void Check([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok,string field){if(!ok)throw new InvalidDataException("Invalid career "+field+".");}
        Check(Career is not null,"history");var c=Career!;
        Check(c.AvailableFrom>=GameClock.Start&&c.AvailableFrom<=Clock.Now&&c.LastCheck<=Clock.Now&&c.NextOffer>=GameClock.Start,"dates");
        Check(c.NarrativeRng is not null&&c.Journal is not null&&c.Sales is not null&&c.Rankings is not null,"collections");
        Check(c.PendingScene is null||HelperStories.Known(c.PendingScene),"pending scene");
        Check(Businesses.Any(b=>b.Id==c.LastBusiness)&& (c.MovedAt is null||c.MovedAt<=Clock.Now),"workplace");
        Check(c.Journal.All(e=>e is not null&&HelperStories.Known(e.Scene)&&e.Answer is >=-1 and <=1&&e.At>=c.AvailableFrom&&e.At<=Clock.Now&&e.Text is not null),"journal");
        var arc=c.Journal.Where(e=>HelperStories.Arc.Contains(e.Scene)).Select(e=>e.Scene).ToArray();
        Check(arc.SequenceEqual(HelperStories.Arc.Take(arc.Length)),"story order");
        Check(c.PendingScene is null||!HelperStories.Arc.Contains(c.PendingScene)||c.PendingScene==HelperStories.Arc.ElementAtOrDefault(arc.Length),"story progression");
        Check(c.Sales.All(s=>s is not null&&s.At>=c.AvailableFrom&&s.At<=Clock.Now&&s.Physical>=0&&s.Digital>=0&&s.Overseas>=0&&
            FindSeries(s.Series)?.Volumes.Any(v=>v.Id==s.Volume&&v.BusinessId==s.Business)==true),"sales");
        Check(c.Sales.Select(s=>(s.At,s.Volume)).Distinct().Count()==c.Sales.Count,"duplicate sales");
        Check(c.Rankings.All(r=>r is not null&&r.At>=c.AvailableFrom&&r.At<=Clock.Now&&Markets.Any(m=>m.MagazineId==r.Magazine)&&r.Rows is not null&&r.Rows.All(x=>x is not null&&x.Rank>0&&double.IsFinite(x.Score))),"rankings");
        Check(c.Rankings.Select(r=>(r.At,r.Magazine)).Distinct().Count()==c.Rankings.Count,"duplicate ranking");
    }
    public static GameState ImportCareerV5(string json)
    {
        try
        {
            var node=JsonNode.Parse(json)!.AsObject();
            if(node[nameof(Version)]?.GetValue<int>()!=5)throw new InvalidDataException("Choose a version-5 save.");
            // Reuse the strict root/office presence checks before old-state validation.
            try { FromJson(json); }
            catch(InvalidDataException ex) when(ex.Message.StartsWith("Save file version 5 is not supported.",StringComparison.Ordinal)) { }
            node[nameof(Career)]=JsonSerializer.SerializeToNode(new CareerHistory(),JsonOptions);
            node[nameof(Progression)]=JsonSerializer.SerializeToNode(new ProgressionState(),JsonOptions);
            var state=JsonSerializer.Deserialize<GameState>(node.ToJsonString(),JsonOptions)!;
            // Validate the old world and its v5 replay boundary before replacing it.
            state.ValidateSave();state.Version=CurrentVersion;state.InitializeCareer();state.InitializeProgression(true);
            state.World.ReplayCheckpoint=null;state.World.ReplayLogStart=state.CommandLog.Count;
            state.ValidateSave();state.World.ReplayCheckpoint=state.ToJson();return state;
        }
        catch(Exception ex)when(ex is JsonException or InvalidOperationException or ArgumentException or NullReferenceException)
        {throw new InvalidDataException("Could not import this career: "+ex.Message,ex);}
    }
    public static GameState ImportSupported(string json)
    {
        using var doc=JsonDocument.Parse(json);
        if(doc.RootElement.ValueKind!=JsonValueKind.Object||!doc.RootElement.TryGetProperty("Version",out var version)||version.ValueKind!=JsonValueKind.Number||!version.TryGetInt32(out var number))return FromJson(json);
        return number switch
        {3=>ImportStudioV3(json),4=>ImportTimelineV4(json),5=>ImportCareerV5(json),6=>ImportProgressionV6(json),7=>ImportAlphaV7(json),8=>ImportProductionV8(json),9=>ImportConvenienceV9(json),_=>FromJson(json)};
    }
}
