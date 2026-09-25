using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class AlphaTests
{
    private static void Store(Action<CareerStore,string> action)
    {
        string root=Path.Combine(Path.GetTempPath(),"mangaka-alpha-"+Guid.NewGuid().ToString("N"));
        try{action(new CareerStore(root),Guid.NewGuid().ToString("N"));}
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    private static GameState Book(int seed=0)
    {
        var s=GameState.NewGame(seed);s.Apply(new CreateDoujinCommand("First pages","adventure"));
        for(int day=0;day<180&&s.Series[0].Volumes.Count==0;day++)s.Advance(24);
        Assert.Single(s.Series[0].Volumes);return s;
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(42)]
    public void Standalone_finishes_one_book_and_sells_without_special_bonuses(int seed)
    {
        var s=Book(seed);var title=s.Series[0];var v=title.Volumes.Single();
        Assert.Equal(20,v.PrintedPages);Assert.Single(v.ChapterIds);Assert.Single(title.Chapters);
        var cash=s.Money;var cost=GameState.PrintingCost(PrintTier.CopyShop,v.PrintedPages,10);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10,Value:(int)PrintTier.CopyShop));
        Assert.Equal(cash-cost,s.Money);s.Advance(24*14);
        Assert.True(v.CopiesSold>0);Assert.Single(title.Chapters);Assert.Single(title.Volumes);
        Assert.DoesNotContain(s.ControlledBusiness.Account.Entries,e=>e.Kind==AccountEntryKind.SandboxSubsidy);
        Assert.Equal(s.ToJson(),GameState.FromJson(s.ToJson()).ToJson());
        Assert.Equal(s.ToJson(),s.ReplayTimeline().ToJson());
    }
    [Theory][InlineData(0)][InlineData(7)][InlineData(17)][InlineData(68)]
    public void Invalid_standalone_is_atomic(int pages)
    {var s=GameState.NewGame();var json=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new CreateDoujinCommand("Paper","drama",pages)));Assert.Equal(json,s.ToJson());}
    [Fact]public void Standalone_cannot_be_silently_converted_to_publisher_serialization()
    {var s=Book();var json=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new PitchSeriesCommand(s.Series[0].Id,"unknown")));Assert.Equal(json,s.ToJson());}
    [Fact]public void Guidance_queries_are_read_only_and_ignore_missing_history()
    {
        var s=GameState.NewGame();var p=new GuidancePreferences();var before=s.ToJson();
        Assert.Equal("create",CareerGuidance.Evaluate(s,p).Id);CareerGuidance.Observe(s,p);
        Assert.Equal(before,s.ToJson());Assert.Empty(p.Completed);
        s.Apply(new CreateDoujinCommand("Small","drama"));before=s.ToJson();
        Assert.Equal("produce",CareerGuidance.Evaluate(s,p).Id);CareerGuidance.Observe(s,p);CareerGuidance.Observe(s,p);
        Assert.Single(p.Completed);Assert.Equal(before,s.ToJson());
    }
    [Fact]public void Guidance_tracks_delivery_and_existing_sales_without_repeating_opening()
    {
        var s=Book();var p=new GuidancePreferences();var v=s.Series[0].Volumes.Single();
        Assert.Equal("print",CareerGuidance.Evaluate(s,p).Id);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10,Value:0));
        Assert.Equal("delivery",CareerGuidance.Evaluate(s,p).Id);s.Advance(24);
        Assert.Equal("sell",CareerGuidance.Evaluate(s,p).Id);s.Advance(24*7);
        Assert.Equal("direction",CareerGuidance.Evaluate(s,p).Id);CareerGuidance.Observe(s,p);Assert.Contains("first-sale",p.Completed);
        p.Route="contest";Assert.Equal("contest-create",CareerGuidance.Evaluate(s,p).Id);
        p.Route="doujin";Assert.Equal("grow",CareerGuidance.Evaluate(s,p).Id);
        p.Route="employment";Assert.Equal("employment",CareerGuidance.Evaluate(s,p).Id);
        p.Route="opening";Assert.Equal("direction",CareerGuidance.Evaluate(GameState.NewGame(),p).Id);
    }
    [Fact]public void Guidance_respects_selected_project_and_contest_separation()
    {
        var s=Book();s.Apply(new CreateDoujinCommand("Second","drama"));var p=new GuidancePreferences{Project=s.Series.Last().Id};
        Assert.Equal("produce",CareerGuidance.Evaluate(s,p).Id);
        p.Route="contest";s.Apply(new RecognitionCommand(RecognitionAction.CreateManuscript,Text:"Contest",Category:"story"));
        Assert.Equal("contest-review",CareerGuidance.Evaluate(s,p).Id);Assert.False(s.Series.Last().StandaloneDoujin);
    }
    [Fact]public void Version7_migration_preserves_records_and_replay_checkpoint()
    {
        var s=GameState.NewGame();s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox));s.Advance(48);
        var node=JsonNode.Parse(s.ToJson())!;node["Version"]=7;
        var imported=GameState.ImportSupported(node.ToJsonString());Assert.Equal(s.ToJson(),imported.ToJson());
        Assert.True(imported.Progression.EverSandbox);Assert.False(AchievementDelivery.Eligible(imported));
        var checkpoint=JsonNode.Parse(node.ToJsonString())!;checkpoint["World"]!["ReplayCheckpoint"]=null;checkpoint["World"]!["ReplayLogStart"]=s.CommandLog.Count;
        node["World"]!["ReplayCheckpoint"]=checkpoint.ToJsonString();node["World"]!["ReplayLogStart"]=s.CommandLog.Count;
        imported=GameState.ImportSupported(node.ToJsonString());imported.Advance(24);
        Assert.Equal(imported.ToJson(),imported.ReplayTimeline().ToJson());
    }
    [Fact]public void Compressed_storage_keeps_every_record_and_presentation_preference()
    {
        Store((store,id)=>
        {
            var s=Book();s.Advance(24*60);var json=s.ToJson();var p=new CareerPresentation{Guidance=new(){Route="contest",Visible=false,Completed=new(){"produce"}},AmbienceVolume=0};
            var timer=Stopwatch.StartNew();var info=store.Save(id,"Archive",s,p);var saveMs=timer.Elapsed.TotalMilliseconds;
            var path=Path.Combine(store.Root,id,info.Snapshot+".career");Assert.True(new FileInfo(path).Length<json.Length);
            timer.Restart();Assert.Single(store.List());var listMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();var loaded=store.Load(info);var loadMs=timer.Elapsed.TotalMilliseconds;
            Assert.Equal(json,loaded.State.ToJson());Assert.False(loaded.View.Guidance.Visible);Assert.Equal(0,loaded.View.AmbienceVolume);
            var imported=store.Import(store.Export(info));Assert.Equal(json,store.Load(imported).State.ToJson());
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/alpha-storage.txt",$"JSON chars {json.Length}; archive bytes {new FileInfo(path).Length}; save {saveMs:F2} ms; list {listMs:F2} ms; load {loadMs:F2} ms");
        });
    }
    [Fact]public void Legacy_local_and_portable_envelopes_are_readable()
    {
        Store((store,id)=>
        {
            var s=GameState.NewGame();var old=JsonNode.Parse(s.ToJson())!;old["Version"]=7;
            var snapshot=new CareerSnapshot(1,id,"Old",false,DateTime.UtcNow,old.ToJsonString(),new());
            var dir=Path.Combine(store.Root,id);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,Guid.NewGuid().ToString("N")+".json"),JsonSerializer.Serialize(snapshot));
            Assert.Equal(s.ToJson(),store.Load(store.List().Single()).State.ToJson());
            using var bytes=new MemoryStream();using(var zip=new ZipArchive(bytes,ZipArchiveMode.Create,true))
            {using var entry=zip.CreateEntry("career.json").Open();JsonSerializer.Serialize(entry,snapshot);}
            Assert.Equal(s.ToJson(),store.Load(store.Import(bytes.ToArray())).State.ToJson());
        });
    }
    [Fact]public void Reports_are_allowlisted_opt_in_and_do_not_mutate_the_career()
    {
        Store((store,id)=>
        {
            var s=Book();s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox));var before=s.ToJson();
            s.ControlledBusiness.Name="Private Studio Path C:\\Users\\PrivateName";
            var bytes=ProblemReport.Create("The button was unclear",s);using var zip=new ZipArchive(new MemoryStream(bytes));
            Assert.Single(zip.Entries);using var reader=new StreamReader(zip.GetEntry("report.json")!.Open());var report=reader.ReadToEnd();
            Assert.DoesNotContain("PrivateName",report);Assert.Contains(ProblemReport.Build,report);
            var full=ProblemReport.Create("Save attached",s,career:store.ExportSnapshot(id,s,new()));
            using var attached=new ZipArchive(new MemoryStream(full));using var input=attached.GetEntry("career.mangaka")!.Open();using var buffer=new MemoryStream();input.CopyTo(buffer);
            var imported=store.Load(store.Import(buffer.ToArray())).State;Assert.Equal(s.ToJson(),imported.ToJson());Assert.False(AchievementDelivery.Eligible(imported));
            s.ControlledBusiness.Name=GameState.FromJson(before).ControlledBusiness.Name;Assert.Equal(before,s.ToJson());
        });
    }
    [Fact]public void Corrupt_archives_do_not_replace_previous_snapshots()
    {
        Store((store,id)=>
        {
            var s=GameState.NewGame();var good=store.Save(id,"Good",s,new());var file=Path.Combine(store.Root,id,Guid.NewGuid().ToString("N")+".career");File.WriteAllBytes(file,[1,2,3]);
            Assert.Single(store.List());Assert.Equal(s.ToJson(),store.Load(good).State.ToJson());
            Assert.Throws<InvalidDataException>(()=>store.Import([1,2,3]));Assert.Single(store.List());
        });
    }
    [Fact]public void Transfer_validation_still_rejects_two_legs_in_one_account()
    {
        var s=GameState.NewGame();s.Apply(new ContributeFundsCommand(1000));
        var personal=s.Protagonist.PersonalAccount;var business=s.ControlledBusiness.Account;
        var entry=personal.Entries.Last(e=>e.TransferId is not null);
        personal.Entries.Remove(entry);personal.Balance-=entry.Amount;
        business.Entries.Add(entry);business.Balance+=entry.Amount;
        Assert.Throws<InvalidDataException>(()=>GameState.FromJson(s.ToJson()));
    }
    [Fact]public void Report_rejects_blank_notes()
    {
        Assert.Throws<InvalidDataException>(()=>ProblemReport.Create("",GameState.NewGame()));
        Assert.Throws<InvalidDataException>(()=>ProblemReport.Create(new string('x',12001),GameState.NewGame()));
    }
}
