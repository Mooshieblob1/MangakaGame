using System;
using System.Linq;
using Godot;
using MangakaSim;
using Steamworks;

namespace MangakaGame;

public partial class DebugMain
{
    // Steam achievements (Q68): a round trip through Valve's test app 480 on a PC with Steam running and signed in.
    // Spacewar's own ACH_WIN_ONE_GAME stands in for ours, and its earlier state is put back afterwards.
    private async void RunSteamSmoke()
    {
        SetProcess(false);
        try
        {
            await SettleUi();
            Check(_steam.Connected, $"Steam connects ({_steam.Status})");
            Check(_steam.AppId == SteamAchievements.TestAppId, $"Development runs use Valve's test app 480 (app {_steam.AppId})");
            var started = Time.GetTicksMsec();
            while (!_steam.StatsReady && Time.GetTicksMsec() - started < 15000) { SteamAPI.RunCallbacks(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            Check(_steam.StatsReady, $"Steam loads the player's achievements ({SteamUserStats.GetNumAchievements()} in app 480)");

            const string stand = "ACH_WIN_ONE_GAME";
            Check(SteamUserStats.GetAchievement(stand, out var before), "Spacewar's first achievement is readable");
            try
            {
                if (before) { SteamUserStats.ClearAchievement(stand); SteamUserStats.StoreStats(); }
                var session = new AchievementSession(_steam);
                Check(session.Unlock(stand) && SteamUserStats.GetAchievement(stand, out var after) && after, "An unlock through the game's achievement session reaches Steam");
                for (var i = 0; i < 10; i++) { _steam.Update(1); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
                Check(session.Unlock(stand), "A repeated unlock is accepted without a second request");
                Check(_steam.Unlock(ProgressionCatalog.Achievements[0].ApiName), "Our own names, unknown to app 480, are skipped instead of retried forever");
            }
            finally
            {
                // Leave the player's Spacewar achievement as it was before the check.
                if (before) SteamUserStats.SetAchievement(stand); else SteamUserStats.ClearAchievement(stand);
                SteamUserStats.StoreStats(); SteamAPI.RunCallbacks();
            }

            // Every recorded achievement on an eligible save is offered to the platform (Sandbox is covered by ProgressionTests).
            var recorder = new StubSink(); var s = GameState.NewGame();
            s.Progression.Milestones.Add(new("first_publication", s.ProtagonistPersonId, s.Clock.Now, "Published"));
            s.Progression.Achievements.Add(new("first_publication", s.Clock.Now));
            AchievementDelivery.Deliver(s, recorder);
            Check(recorder.Keys.SequenceEqual(["MKG_FIRST_PUBLICATION"]), "An eligible save offers its achievements");

            GD.Print($"STEAM SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"STEAM SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }
    private sealed class StubSink : IAchievementSink
    { public System.Collections.Generic.List<string> Keys { get; } = new(); public bool Unlock(string key) { Keys.Add(key); return true; } }
}
