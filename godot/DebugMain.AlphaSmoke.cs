using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private string SmokeOutput => OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--alpha-output=")) is {} path
        ?path["--alpha-output=".Length..]:ProjectSettings.GlobalizePath("res://../TestResults");
    private async void RunAlphaSmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);_careers=new CareerStore(Path.Combine(SmokeOutput,"alpha-careers-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            Check(_guidanceCard.Visible&&_guidanceTitle.Text.Contains("small doujin"),"Fresh optional guidance");
            var before=_state.ToJson();Press("Show me");await SettleUi();
            Check(_page=="New doujin"&&_state.ToJson()==before,"Show me opens creation without taking an action");
            GetNode<LineEdit>("%DoujinTitle").Text="A little light";Press("Create one-shot doujin");await SettleUi();
            Check(_state.Series.Single().StandaloneDoujin,"Standalone creation through actual controls");
            await CaptureSmokeImage("alpha-first-pages");
            var productionStart=_state.Clock.Now;
            for(int day=0;day<180&&_state.Series[0].Volumes.Count==0;day++)_state.Advance(24);
            Check(_state.Series[0].Volumes.Count==1,"Normal work finishes standalone master");
            var completed=_state.Clock.Now;_scanIndex=_state.Events.Count;_popupEvents.Clear();_helperPopup.Hide();_dirty=true;await SettleUi();
            Press("Show me");await SettleUi();Check(_page=="Print doujin"&&_alphaCopies.IsVisibleInTree(),"Guidance opens focused printing");
            var v=_state.Series[0].Volumes[0];_alphaPrinter.Select(0);_alphaCopies.Value=10;
            var money=_state.Money;Press("Order this print run");await SettleUi();
            await CaptureSmokeImage("alpha-printing");
            Check(_state.Money==money-GameState.PrintingCost(PrintTier.CopyShop,v.PrintedPages,10),"Print quote matches actual expense");
            for(int day=0;day<15&&v.CopiesSold==0;day++)_state.Advance(24);
            Check(v.CopiesSold>0&&_state.Series[0].Chapters.Count==1,"Ordinary local sale without extra chapters");
            _scanIndex=_state.Events.Count;_popupEvents.Clear();_helperPopup.Hide();_report.Hide();_dirty=true;await SettleUi();
            Check(_guidanceTitle.Text.Contains("next"),"Existing readers advance guidance to route choice");
            Press("What should I do?");await SettleUi();Press("Enter a contest");Check(_presentation.Guidance.Route=="contest","Contest direction");
            Press("Seek studio employment");Check(_presentation.Guidance.Route=="employment","Employment direction");
            Press("Grow the doujin business");Check(_presentation.Guidance.Route=="doujin","Doujin direction");
            await CaptureSmokeImage("alpha-first-sale");
            var save=SaveCareer("Alpha complete opening");before=_state.ToJson();LoadCareer(save);await SettleUi();
            Check(_state.ToJson()==before&&_presentation.Guidance.Completed.Contains("first-sale"),"Compressed save restores full state and guidance");
            _presentation.Guidance.Visible=false;RefreshGuidance();Check(!_guidanceCard.Visible,"Guidance can be hidden");
            Navigate("Help");Press("Objectives and direction");await SettleUi();Press("Resume guidance");Check(_guidanceCard.Visible,"Guidance resumes from Help");
            GetWindow().Size=new(1280,720);await SettleUi();await CaptureSmokeImage("alpha-guidance-720");
            var bounds=_guidanceCard.GetGlobalRect();Check(bounds.Position.X>=0&&bounds.End.X<=GetViewportRect().Size.X+1&&bounds.End.Y<=GetViewportRect().Size.Y+1,"Card fits 720p");
            ShowMenu();Press("Report a problem");await SettleUi();
            var choices=_menuContent.GetChildren().OfType<CheckBox>().ToArray();Check(choices.Length==2&&choices.All(c=>!c.ButtonPressed),"Report attachments default off");
            _menuContent.GetChildren().OfType<TextEdit>().Single().Text="Alpha smoke: local export only.";
            await CaptureSmokeImage("alpha-report");Press("Export local report");await SettleUi();
            var picker=GetChildren().OfType<FileDialog>().Last();var reportPath=Path.Combine(SmokeOutput,"alpha-report.zip");picker.EmitSignal(FileDialog.SignalName.FileSelected,reportPath);await SettleUi();
            using(var zip=ZipFile.OpenRead(reportPath))Check(zip.Entries.Count==1&&zip.GetEntry("report.json") is not null,"Actual report action writes selected local contents");
            Check(_state.ToJson()==before,"Reporting leaves simulation untouched");
            _menuContent.GetChildren().OfType<Button>().Single(b=>b.Text=="Cancel").EmitSignal(BaseButton.SignalName.Pressed);
            Press("Settings");await SettleUi();Check(_menuContent.FindChildren("*","HSlider",true,false).OfType<HSlider>().Count()>=3,"Volume controls and text scale present");
            _menuContent.GetChildren().OfType<Button>().Single(b=>b.Text=="Back").EmitSignal(BaseButton.SignalName.Pressed);Press("Return to this studio");
            var activity=_audio.ActivityCount;var effects=_audio.EffectCount;
            for(int frame=0;frame<600;frame++){_audio.Update(1d/60,true,true,.35,.6);_audio.Cue();}
            Check(_audio.ActivityCount-activity<=2&&_audio.EffectCount-effects<=40,"Audio budgets use real time and bounded voices");
            using var waveform=OfficeAudio.Wave(.2,1);Check(waveform.MixRate==22050&&waveform.Data.Length==8820,"Original procedural audio generated");
            waveform.SaveToWav(Path.Combine(SmokeOutput,"alpha-pencil.wav"));
            _audio.Update(.1,false,false,0,0);
            File.WriteAllText(Path.Combine(SmokeOutput,"alpha-opening.txt"),$"Build {ProblemReport.Build}\nNormal simulation, 16 pages, seed 0. Production {(completed-productionStart).TotalDays:F1} days; first sale {(_state.Clock.Now-productionStart).TotalDays:F1} days. 1x clock-only {(_state.Clock.Now-productionStart).TotalHours*SecondsPerHourAt1x/60:F1} minutes, 8x {(_state.Clock.Now-productionStart).TotalHours*SecondsPerHourAt1x/480:F1} minutes. UI reading and pause time excluded; this is not a human pacing test.\n{_smokeChecks} checks passed.\n");
            var licenses=new System.Text.StringBuilder(Engine.GetLicenseText());
            licenses.AppendLine("\nThird-party copyright records\n"+Json.Stringify(Engine.GetCopyrightInfo(),"  "));
            foreach(var license in Engine.GetLicenseInfo())licenses.AppendLine("\n"+license.Key+"\n"+license.Value);
            File.WriteAllText(Path.Combine(SmokeOutput,"GODOT-LICENSES.txt"),licenses.ToString());
            GD.Print($"ALPHA SMOKE PASSED: {_smokeChecks} checks.");
            var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("ALPHA SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
