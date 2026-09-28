using Xunit;
namespace MangakaSim.Tests;
// T1.6: careers saved by alpha.11 load in the current build and keep playing.
// alpha11-opening.career was written by the packaged alpha.11 game (its opening check, 15 Apr 1996).
// alpha11-year1/2.mangaka were made by the guided playtest at commit 242240c, whose simulation is identical to alpha.11.
public class OldSaveTests
{
    static string Fixture(string name)=>Path.Combine(AppContext.BaseDirectory,"Fixtures",name);
    static void Store(Action<CareerStore> use)
    {
        var root=Path.Combine(Path.GetTempPath(),"mangaka-oldsave-"+Guid.NewGuid().ToString("N"));
        try{use(new CareerStore(root));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    static void PlaysOn(GameState state,int days)
    {
        var start=state.Clock.Now;
        for(var d=0;d<days;d++)state.Advance(24);
        Assert.Equal(start.AddDays(days),state.Clock.Now);
        var json=state.ToJson();
        Assert.Equal(json,GameState.FromJson(json).ToJson());
    }
    [Theory]
    [InlineData("alpha11-year1.mangaka","1997-04-01 08:00")]
    [InlineData("alpha11-year2.mangaka","1998-04-01 08:00")]
    public void Alpha11_mid_career_imports_and_plays_on(string file,string date)=>Store(store=>
    {
        var info=store.Import(File.ReadAllBytes(Fixture(file)));
        var (state,_)=store.Load(info);
        Assert.Equal(DateTime.Parse(date,System.Globalization.CultureInfo.InvariantCulture),state.Clock.Now);
        Assert.Contains(state.Series,s=>s.BusinessId==state.ControlledBusinessId&&s.Contract is not null);
        Assert.Contains(state.ControlledStaff,p=>p.Id!=state.ProtagonistPersonId);
        PlaysOn(state,90);
    });
    [Fact]public void Alpha11_career_left_on_disk_is_listed_and_loads()=>Store(store=>
    {
        // An alpha.11 player's saves stay in the user folder; the new build must list and open them.
        var career="0cfd131df4f0465caad2d37343080a01";
        Directory.CreateDirectory(Path.Combine(store.Root,career));
        File.Copy(Fixture("alpha11-opening.career"),Path.Combine(store.Root,career,"2ad895013de9400b8193b278e037e415.career"));
        var info=Assert.Single(store.List());
        Assert.Equal("Alpha complete opening",info.Name);
        var (state,view)=store.Load(info);
        Assert.Equal(new DateTime(1996,4,15,8,0,0),state.Clock.Now);
        Assert.NotNull(view.Guidance);
        PlaysOn(state,30);
    });
    [Fact]public void Alpha11_year_two_milestones_are_not_logged_again()=>Store(store=>
    {
        var (state,_)=store.Load(store.Import(File.ReadAllBytes(Fixture("alpha11-year2.mangaka"))));
        var milestones=new JourneyMilestones(state);
        var fresh=new List<string>();
        for(var d=0;d<30;d++){var from=state.Events.Count;state.Advance(24);fresh.AddRange(milestones.Observe(state,state.Events.Skip(from).ToList()));}
        Assert.DoesNotContain("first-doujin-completed",fresh);
        Assert.DoesNotContain("first-hire",fresh);
        Assert.DoesNotContain("serialization-accepted",fresh);
    });
    [Fact]public void Unreadable_saves_are_counted_not_silently_dropped()=>Store(store=>
    {
        var career=Guid.NewGuid().ToString("N");
        store.Save(career,"Readable",GameState.NewGame(0),new CareerPresentation());
        File.WriteAllBytes(Path.Combine(store.Root,career,Guid.NewGuid().ToString("N")+".career"),[1,2,3,4]);
        Assert.Single(store.List());
        Assert.Equal(1,store.Unreadable);
    });
}
