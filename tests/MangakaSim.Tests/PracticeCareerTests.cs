using Xunit;
namespace MangakaSim.Tests;
public class PracticeCareerTests
{
    static readonly string FixturePath=Path.Combine(AppContext.BaseDirectory,"Fixtures","Practice - a struggling series.mangaka");
    static void Store(Action<CareerStore> use)
    {
        var root=Path.Combine(Path.GetTempPath(),"mangaka-practice-"+Guid.NewGuid().ToString("N"));
        try{use(new CareerStore(root));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact]public void Practice_career_keeps_its_identifier_and_name()=>Store(store=>
    {
        var info=store.Import(File.ReadAllBytes(FixturePath));
        Assert.Equal(CareerStore.PracticeCareer,info.Career);
        Assert.Equal("Practice: a struggling series",info.Name);
        var (state,_)=store.Load(info);
        Assert.Equal(CareerDifficulty.Standard,state.Progression.Difficulty);
        Assert.False(state.Progression.EverSandbox);
        Assert.Contains(state.Series,s=>s.BusinessId==state.ControlledBusinessId&&s.Publishing==PublishingStatus.Serialized);
        Assert.DoesNotContain(state.Events,e=>e.Type==EventType.CancellationWarning&&state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==state.ControlledBusinessId));
    });
    [Fact]public void Importing_the_practice_career_twice_gives_two_loadable_saves()=>Store(store=>
    {
        var first=store.Import(File.ReadAllBytes(FixturePath));var second=store.Import(File.ReadAllBytes(FixturePath));
        Assert.NotEqual(first.Snapshot,second.Snapshot);
        Assert.Equal(first.Career,second.Career);
        store.Load(first);store.Load(second);
    });
    [Fact]public void Other_imports_still_get_a_new_identifier()=>Store(store=>
    {
        var state=GameState.NewGame(0);var saved=store.Save(Guid.NewGuid().ToString("N"),"Mine",state,new CareerPresentation());
        var imported=store.Import(store.Export(saved));
        Assert.NotEqual(saved.Career,imported.Career);
        Assert.NotEqual(CareerStore.PracticeCareer,imported.Career);
    });
    [Fact]public void Playing_on_brings_the_cancellation_warning()=>Store(store=>
    {
        var (state,_)=store.Load(store.Import(File.ReadAllBytes(FixturePath)));
        var start=state.Clock.Now;
        bool Warned()=>state.Events.Any(e=>e.Type==EventType.CancellationWarning&&e.Time>=start&&state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==state.ControlledBusinessId));
        for(var d=0;d<45&&!Warned();d++)state.Advance(24);
        // If this fails, report it; do not tune balance to make it pass (no balance change in T1.10).
        Assert.True(Warned(),$"No cancellation warning within 45 days of {start:d MMM yyyy}");
    });
}
