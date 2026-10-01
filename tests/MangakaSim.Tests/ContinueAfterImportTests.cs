using Xunit;
namespace MangakaSim.Tests;
// Fresh-player finding B5: importing a career stamped it as the newest save, so Continue opened it instead of the career just played.
public class ContinueAfterImportTests
{
    [Fact]public void An_imported_career_keeps_its_own_save_time_and_does_not_jump_ahead_of_the_career_you_played()
    {
        var root = Path.Combine(Path.GetTempPath(), "mangaka-import-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareerStore(root);
            var practice = store.Save(Guid.NewGuid().ToString("N"), "Practice", GameState.NewGame(1), new CareerPresentation());
            var package = store.Export(practice);
            Thread.Sleep(30);
            var played = store.Save(Guid.NewGuid().ToString("N"), "My career", GameState.NewGame(2), new CareerPresentation());
            Thread.Sleep(30);
            var imported = store.Import(package);
            Assert.Equal(practice.SavedAt, imported.SavedAt);
            Assert.Equal(played.Career, store.List().First().Career);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
