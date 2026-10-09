using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

// Plays six voiced Helper-Chan conversations in real time on a new career and checks the visual novel flow:
// the voice starts, the line types out, the answers wait for her, and the answer reaches the journal.
// Paced for watching, so it doubles as the preview recording:
//   Godot --path godot --write-movie <file>.avi --fixed-fps 30 -- --helper-voice-smoke [--helper-voice=ja]
public partial class DebugMain
{
    private static readonly (string Scene, int Answer)[] VoicePreviewScenes =
        [("beside", 0), ("page", 1), ("same", 1), ("tea", 0), ("migration", 0), ("goal-legend", 0)];

    private async void RunHelperVoiceSmoke()
    {
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "helper-voice-" + Guid.NewGuid().ToString("N")));
            var language = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--helper-voice="))?["--helper-voice=".Length..] ?? "en";
            _audioSettings.HelperVoice = language;
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            ShowOffice(); Pause(); RefreshManagement(); await SettleUi();
            await Wait(3);
            foreach (var (id, answer) in VoicePreviewScenes)
            {
                _popupEvents.Clear(); _state.Career.PendingScene = id; _state.Career.DeferredUntil = null;
                var scene = HelperStories.Describe(_state, id); var journal = _state.Career.Journal.Count;
                ShowStory(id); await SettleUi();
                Check(_storyOpen && _dialogueLine is not null && _helperPopup.Visible, $"{id} opens as a visual novel scene");
                Check(HelperSpeaking && _voice!.Stream.ResourcePath.Contains($"/{language}/"), $"{id} plays her {language} voice (text: {scene.Text.Trim()})");
                Check(_dialogueChoices is { Visible: false }, $"{id} holds the answers back while she speaks");
                await Wait(1);
                Check(_dialogueLine!.VisibleCharacters is > 0 && _dialogueLine.VisibleCharacters < _dialogueLine.Text.Length, $"{id} types the line out");
                while (DialogueTyping || HelperSpeaking) await SettleUi();
                Check(_dialogueChoices is { Visible: true }, $"{id} shows the answers when she finishes");
                await Wait(1.2);
                var choice = ButtonNamed(answer == 0 ? scene.First : scene.Second); choice.GrabFocus();
                await Wait(.7);
                choice.EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
                Check(!_storyOpen && !_helperPopup.Visible && _state.Career.Journal.Count == journal + 1 && _state.Career.Journal[^1].Answer == answer,
                    $"{id} records the answer and closes");
                await Wait(1.5);
            }
            if (!OS.HasFeature("movie")) // the recording (Movie Maker mode) ends on the last answer
            {
                _state.Career.PendingScene = "tea"; ShowStory("tea"); await SettleUi();
                Press("Skip this conversation"); await SettleUi();
                Check(!HelperSpeaking && !_storyOpen, "Skipping a conversation stops her voice");
                _audioSettings.HelperVoice = "off"; _state.Career.PendingScene = "tea"; ShowStory("tea"); await SettleUi();
                Check(_storyOpen && !HelperSpeaking && _dialogueLine is not null, "With the voice off, the scene still opens as text only");
                Press("Skip this conversation"); await SettleUi(); _audioSettings.HelperVoice = language;
            }
            GD.Print($"HELPER VOICE SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"HELPER VOICE SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
}
