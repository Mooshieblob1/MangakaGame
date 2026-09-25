using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class CareerTests
{
    [Fact] public void Conversation_is_persistent_and_does_not_change_the_workforce_or_economy()
    {
        var s=GameState.NewGame(7);var rng=s.Rng.State;var staff=s.StaffRng.State;var money=s.Money;var people=JsonSerializer.Serialize(s.People);var revision=s.OfficeRevision;
        s.Apply(new StoryCommand("beside",0));
        Assert.Equal(rng,s.Rng.State);Assert.Equal(staff,s.StaffRng.State);Assert.Equal(money,s.Money);Assert.Equal(people,JsonSerializer.Serialize(s.People));Assert.Equal(revision,s.OfficeRevision);
        var copy=GameState.FromJson(s.ToJson());Assert.Equal(0,copy.Career.Journal.Single().Answer);Assert.Null(copy.Career.PendingScene);
        Assert.Equal(s.ToJson(),s.ReplayTimeline().ToJson());
    }
    [Fact] public void Invalid_or_repeated_answer_is_atomic()
    {
        var s=GameState.NewGame();var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new StoryCommand("beside",9)));Assert.Equal(before,s.ToJson());
        s.Apply(new StoryCommand("beside",1));before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new StoryCommand("beside",0)));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Deferring_preserves_the_scene_and_skipping_does_not_invent_an_answer()
    {
        var s=GameState.NewGame();s.Apply(new StoryCommand("beside",Defer:true));Assert.Equal("beside",s.Career.PendingScene);Assert.Empty(s.Career.Journal);
        var copy=GameState.FromJson(s.ToJson());Assert.Equal(s.Career.DeferredUntil,copy.Career.DeferredUntil);
        s.Apply(new StoryCommand("beside"));Assert.Equal(-1,s.Career.Journal.Single().Answer);Assert.Contains("finding your own way",HelperStories.Describe(s,"reader").Text);
    }
    [Theory] [InlineData(0,"proud")] [InlineData(1,"needs your story")]
    public void Later_scenes_recall_the_selected_answer(int answer,string expected)
    {
        var s=GameState.NewGame();s.Apply(new StoryCommand("beside",answer));Assert.Contains(expected,HelperStories.Describe(s,"reader").Text);
        Assert.Contains(expected,HelperStories.Describe(s,"still").Text);
    }
    [Fact] public void Optional_scene_schedule_is_independent_of_tick_batching()
    {
        var s=GameState.NewGame(8);s.Apply(new StoryCommand("beside",0));var copy=GameState.FromJson(s.ToJson());
        s.Advance(24*100);for(var i=0;i<24*100;i++)copy.Advance(1);Assert.Equal(s.ToJson(),copy.ToJson());
    }
    [Fact] public void Report_rankings_capture_each_issue_once()
    {
        var s=GameState.NewGame();s.Advance(24*35);Assert.NotEmpty(s.Career.Rankings);
        Assert.Equal(s.Career.Rankings.Count,s.Career.Rankings.Select(r=>(r.At,r.Magazine)).Distinct().Count());
        Assert.Equal(s.ToJson(),GameState.FromJson(s.ToJson()).ToJson());
    }
    [Theory] [InlineData("Career")] [InlineData("Journal")] [InlineData("Rankings")] [InlineData("NarrativeRng")]
    public void Missing_required_career_data_is_rejected(string name)
    {
        var json=JsonNode.Parse(GameState.NewGame().ToJson())!;if(name=="Career")json.AsObject().Remove(name);else json["Career"]!.AsObject().Remove(name);
        Assert.Throws<InvalidDataException>(()=>GameState.FromJson(json.ToJsonString()));
    }
    [Fact] public void V5_import_preserves_gameplay_and_starts_new_report_history_without_backfill()
    {
        var s=GameState.NewGame();s.Advance(24*10);var json=JsonNode.Parse(s.ToJson())!;json["Version"]=5;json.AsObject().Remove("Career");
        var imported=GameState.ImportCareerV5(json.ToJsonString());Assert.Equal(s.Money,imported.Money);Assert.Equal(s.Rng.State,imported.Rng.State);Assert.Equal(s.StaffRng.State,imported.StaffRng.State);
        Assert.Equal(s.Clock.Now,imported.Career.AvailableFrom);Assert.Empty(imported.Career.Rankings);Assert.Equal("beside",imported.Career.PendingScene);
        imported.Apply(new StoryCommand("beside",1));imported.Advance(48);Assert.Equal(imported.ToJson(),imported.ReplayTimeline().ToJson());
    }
    [Fact] public void Named_snapshots_and_autosaves_are_separate_and_failed_writes_keep_old_saves()
    {
        WithStore((store,id)=>
        {
            var state=GameState.NewGame();var view=new CareerPresentation();var manual=store.Save(id,"My career",state,view);
            for(var i=0;i<5;i++)store.Save(id,"Daily",state,view,true);
            Assert.Equal(4,store.List().Count);Assert.Equal(state.ToJson(),store.Load(manual).State.ToJson());
            store.FaultInjector=_=>throw new IOException("disk full");Assert.Throws<IOException>(()=>store.Save(id,"Interrupted",state,view));
            Assert.Equal(4,store.List().Count);Assert.Empty(Directory.GetFiles(store.Root,"*.tmp",SearchOption.AllDirectories));
        });
    }
    [Fact] public void Portable_export_keeps_artwork_and_choices_when_original_files_are_gone()
    {
        WithStore((store,id)=>
        {
            var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
            var hash=store.ImportAsset(id,png);var state=GameState.NewGame();state.Apply(new StoryCommand("beside",1));
            var view=new CareerPresentation{CompactUi=true,ReducedUiMotion=true,OfficeSidebar=true,InboxFilter="All"};view.Artwork["1:0"]=hash;view.Tutorials.Add("Series");
            var saved=store.Save(id,"With pictures",state,view);var package=store.Export(saved);File.Delete(store.AssetPath(id,hash));
            var imported=store.Import(package);var loaded=store.Load(imported);Assert.NotEqual(id,imported.Career);Assert.Equal(state.ToJson(),loaded.State.ToJson());
            Assert.Equal(png,File.ReadAllBytes(store.AssetPath(imported.Career,hash)));Assert.Contains("Series",loaded.View.Tutorials);
            Assert.True(loaded.View.CompactUi);Assert.True(loaded.View.ReducedUiMotion);Assert.True(loaded.View.OfficeSidebar);Assert.Equal("All",loaded.View.InboxFilter);
        });
    }
    [Fact] public void Storage_rejects_paths_and_oversized_image_headers()
    {
        WithStore((store,id)=>
        {
            Assert.Throws<InvalidDataException>(()=>store.Save("../outside","bad",GameState.NewGame(),new()));
            Assert.Throws<InvalidDataException>(()=>store.AssetPath(id,"../other"));
            var header=new byte[24];new byte[]{137,80,78,71,13,10,26,10}.CopyTo(header,0);header[16]=0x7f;header[23]=1;
            Assert.Throws<InvalidDataException>(()=>ImageDimensions.Read(header));Assert.Throws<InvalidDataException>(()=>store.ImportAsset(id,header));
        });
    }
    [Theory] [InlineData("RngSeed")] [InlineData("RecapFiredToday")] [InlineData("LastSalesAt")]
    public void V5_import_rejects_missing_root_fields(string field)
    {
        var json=JsonNode.Parse(GameState.NewGame().ToJson())!;json["Version"]=5;json.AsObject().Remove("Career");json.AsObject().Remove(field);
        Assert.Throws<InvalidDataException>(()=>GameState.ImportCareerV5(json.ToJsonString()));
    }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(-1)]
    public void Full_story_progresses_and_replays_all_answer_paths(int answer)
    {
        var s=GameState.NewGame(14);s.Apply(new CreateSeriesCommand("Paper", "adventure", Cadence.Weekly, 4));
        for(var day=0;day<400&&!s.Career.Journal.Any(e=>e.Scene=="still");day++)
        {
            if(s.Career.PendingScene is {} id)s.Apply(new StoryCommand(id,answer));
            s.Advance(24);
            if(day==75)s=GameState.FromJson(s.ToJson());
        }
        Assert.Equal(HelperStories.Arc,s.Career.Journal.Where(e=>HelperStories.Arc.Contains(e.Scene)).Select(e=>e.Scene));
        Assert.Equal(s.ToJson(),s.ReplayTimeline().ToJson());
    }
    [Fact] public void Choices_and_optional_rng_do_not_change_the_simulation_trajectory()
    {
        var answered=GameState.NewGame(12);var quiet=GameState.NewGame(12);
        answered.Apply(new StoryCommand("beside",1));answered.Advance(24*100);quiet.Advance(24*100);
        Assert.Equal(JsonSerializer.Serialize(quiet.People),JsonSerializer.Serialize(answered.People));
        Assert.Equal(JsonSerializer.Serialize(quiet.Businesses),JsonSerializer.Serialize(answered.Businesses));
        Assert.Equal(quiet.Rng.State,answered.Rng.State);Assert.Equal(quiet.StaffRng.State,answered.StaffRng.State);
        Assert.Equal(quiet.World.HiringRng.State,answered.World.HiringRng.State);Assert.Equal(quiet.World.MarketRng.State,answered.World.MarketRng.State);
    }
    [Fact] public void Physical_report_totals_reconcile_with_settled_volume_sales()
    {
        var state=PublishingTests.Started();PublishingTests.Until(state,()=>state.Series[0].Volumes.Count==1);
        var volume=state.Series[0].Volumes[0];state.Apply(new PauseSeriesCommand(state.Series[0].Id));
        state.Apply(new StudioActionCommand(StudioAction.Print,volume.Id,Amount:100));state.Advance(24*35);
        Assert.True(volume.CopiesSold>0);Assert.Equal(volume.CopiesSold,state.Career.Sales.Where(s=>s.Volume==volume.Id).Sum(s=>s.Physical));
        Assert.Equal(state.ToJson(),GameState.FromJson(state.ToJson()).ToJson());
    }
    private static void WithStore(Action<CareerStore,string> action)
    {
        var root=Path.Combine(Path.GetTempPath(),"mangaka-career-"+Guid.NewGuid().ToString("N"));
        try{action(new CareerStore(root),Guid.NewGuid().ToString("N"));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
