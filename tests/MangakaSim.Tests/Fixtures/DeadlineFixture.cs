using MangakaSim;
namespace MangakaSim.Tests;
internal static class DeadlineFixture
{
    // Isolate production deadline arithmetic from the magazine issue calendar.
    // Full contract lifecycle and save validation use SimulationFixture.Serialized.
    internal static void Attach(GameState state)
    {
        var series=state.Series[0];
        series.Contract=new Contract(state.AllocateId(),"hoshigaku-flowers",1000,state.Clock.Now,series.Chapters[0].DueDate);
        foreach(var chapter in series.Chapters)chapter.DoujinEligible=false;
    }
}
