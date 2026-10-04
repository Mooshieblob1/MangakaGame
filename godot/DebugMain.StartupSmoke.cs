using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Start-up disclaimer and first-launch volume (spec 2026-09-28). Uses a temporary settings file, never the player's.
    private async void RunStartupSmoke()
    {
        SetProcess(false);
        try
        {
            await SettleUi();
            int Bus(string name) => AudioServer.GetBusIndex(name);
            Check(Bus("Music") > 0 && Bus("Effects") > 0 && AudioServer.GetBusSend(Bus("Music")) == "Master" && AudioServer.GetBusSend(Bus("Effects")) == "Master",
                "Music and Sound effects feed the Master bus");
            _audioSettings = new() { Master = .5, Music = 0, Effects = 1, SetupDone = true }; ApplyAudioSettings();
            Check(AudioServer.IsBusMute(Bus("Music")) && !AudioServer.IsBusMute(Bus("Effects")) && AudioServer.GetBusVolumeDb(0) < -5,
                "Each slider drives its own bus, and Master caps both");
            Check(_audioSettingsPath is null, "Smoke runs never read or write the player's audio settings file");
            // A first launch: disclaimer in silence, then the volume screen.
            var path = Path.Combine(SmokeOutput, "audio-settings-" + Guid.NewGuid().ToString("N") + ".json");
            _audioSettings = new(); _audioSettingsPath = path;
            BeginStartup(!_audioSettings.SetupDone); await SettleUi();
            Check(_disclaimer!.Visible && _disclaimer.Text == DisclaimerText && AudioServer.IsBusMute(0), "The disclaimer shows in silence");
            // Final review: the first screen uses the game's theme, and music waits for the disclaimer to end.
            Check(_startupRoot!.Theme is not null && _startupRoot.Theme == Theme, "The start-up screens use the game's theme");
            _managementReady = true; UpdateMusic(6);
            Check(_music.Current is null, "Music does not start underneath the disclaimer");
            var key = new InputEventKey { Pressed = true, Keycode = Key.Space };
            _startupRoot!.EmitSignal(Control.SignalName.GuiInput, key); await SettleUi();
            _startupRoot.EmitSignal(Control.SignalName.GuiInput, key); await SettleUi();
            Check(!_disclaimer.Visible && _volumeSetup is { Visible: true }, "A key skips the disclaimer, once, to the volume screen on a first launch");
            ApplyAudioSettings();
            Check(AudioServer.IsBusMute(0), "Master starts at 0, so nothing is heard");
            var sliders = _volumeSetup!.FindChildren("*", "HSlider", true, false).OfType<HSlider>().ToArray();
            // Quick start (Q65): only Master here; Music and Sound effects keep their defaults and live in Settings.
            Check(sliders.Length == 1 && sliders[0].Value == 0 && Math.Abs(_audioSettings.Music - .5) < 1e-9 && Math.Abs(_audioSettings.Effects - .6) < 1e-9,
                "Only Master, at 0; Music 50% and Sound effects 60% wait in Settings");
            Check(_volumeSetup.FindChildren("InterfaceSize", "OptionButton", true, false).Any() && !_volumeSetup.FindChildren("DisplayMode", "OptionButton", true, false).Any(),
                "The first-launch screen offers the interface size, not the window options");
            // Final review: keyboard and controller players start on Master and can move through the screen.
            Check(GetViewport().GuiGetFocusOwner() == sliders[0], "The volume screen starts with keyboard focus on Master");
            // Final review: the Sound effects preview is heard even though menus pause the office ambience.
            _audio.Preview(); _audio.Update(.02, false, false, AmbienceBalance, 1);
            Check(_audio.EffectPlaying, "The Sound effects preview keeps playing while the start-up screens are open");
            sliders[0].Value = .8; ApplyAudioSettings();
            Check(!AudioServer.IsBusMute(0) && AudioServer.GetBusVolumeDb(0) > -3, "Raising Master makes sound audible");
            // Press the volume screen's own Continue: the title screen may also show one when the player has saves.
            _volumeSetup!.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Continue").EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
            Check(_startup is null && _audioSettings.SetupDone && AudioSettings.Load(path) is { SetupDone: true, Master: > .79 },
                "Continue saves the levels and ends first-launch setup");
            // A later launch: only the disclaimer.
            BeginStartup(!_audioSettings.SetupDone); await SettleUi();
            _startupRoot!.EmitSignal(Control.SignalName.GuiInput, key); await SettleUi();
            Check(_startup is null && !AudioServer.IsBusMute(0), "Later launches show only the disclaimer, then sound returns");
            _audioSettings.PlayWhileUnfocused = false; Check(AudioFocused == GetWindow().HasFocus(), "Sound follows window focus by default");
            _audioSettings.PlayWhileUnfocused = true; Check(AudioFocused, "The checkbox keeps sound playing while unfocused");
            _audioSettingsPath = null;
            GD.Print($"STARTUP SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"STARTUP SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }
}
