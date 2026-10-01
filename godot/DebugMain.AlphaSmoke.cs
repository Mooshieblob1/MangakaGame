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
    private bool PhoneSays(string text)=>_phone.FindChildren("*","Label",true,false).OfType<Label>().Any(l=>l.Text.Contains(text));
    private async void RunAlphaSmoke()
    {
        SetProcess(false);
        try
        {
            // Headless runs start with a tiny window, which would fold the phone like a cramped screen, so use the project size.
            GetWindow().Size=new(1600,900);
            Directory.CreateDirectory(SmokeOutput);_careers=new CareerStore(Path.Combine(SmokeOutput,"alpha-careers-"+Guid.NewGuid().ToString("N")));
            var buzzes=_audio.BuzzCount;NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            RefreshGuidance();await SettleUi();
            Check(_phone.Visible&&!_phone.Modern&&PhoneSays("16-page one-shot")&&_audio.BuzzCount>buzzes,"Fresh career: PHS buzzes with the first text");
            Check(_presentation.Guidance.Thread.SelectMany(m=>m.Texts).All(t=>t.Length<=CareerGuidance.TextLimit),"Texts fit the 140 character limit");
            await CaptureSmokeImage("alpha-phone-phs");
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
            buzzes=_audio.BuzzCount;
            for(int day=0;day<15&&v.CopiesSold==0;day++)_state.Advance(24);
            Check(v.CopiesSold>0&&_state.Series[0].Chapters.Count==1,"Ordinary local sale without extra chapters");
            _scanIndex=_state.Events.Count;_popupEvents.Clear();_helperPopup.Hide();_report.Hide();_dirty=true;await SettleUi();
            RefreshGuidance();await SettleUi();
            Check(CareerGuidance.Evaluate(_state,_presentation.Guidance).Id=="continue-series"&&_phone.Visible&&PhoneSays("Readers bought")&&_audio.BuzzCount>buzzes,"First sale texts the continue-series step");
            Press("Later");await SettleUi();Check(!_phone.Visible&&_phoneIcon.Visible&&_phoneIcon.Unread==0,"Later puts the phone away as an icon");
            CareerGuidance.Say(_presentation.Guidance,_state.Clock.Now,"Smoke check message.");ClosePhone();
            Check(_phoneIcon.Unread==1,"Icon counts unread messages");
            _phoneIcon.EmitSignal(BaseButton.SignalName.Pressed);await SettleUi();Check(_phone.Visible&&_phoneIcon.Unread==0,"Icon reopens the thread");
            Press("Show me");await SettleUi();Check(_page=="Series details"&&!_phone.Visible,"Show me opens Series details for continuing");
            Navigate("Help");Press("Objectives and direction");await SettleUi();
            Press("Enter a contest");Check(_presentation.Guidance.Route=="contest","Contest direction");
            Press("Seek studio employment");Check(_presentation.Guidance.Route=="employment","Employment direction");
            Press("Follow the career path");Check(_presentation.Guidance.Route=="career","Career direction");
            await CaptureSmokeImage("alpha-first-sale");
            var save=SaveCareer("Alpha complete opening");before=_state.ToJson();LoadCareer(save);await SettleUi();
            Check(_state.ToJson()==before&&_presentation.Guidance.Completed.Contains("first-sale"),"Compressed save restores full state and guidance");
            SetGuidanceVisible(false);CareerGuidance.Say(_presentation.Guidance,_state.Clock.Now,"Hidden smoke message.");RefreshGuidance();await SettleUi();
            Check(!_phone.Visible&&_phoneIcon.Visible&&_phoneIcon.Unread==1,"Hidden guidance stops pop-ups but keeps the icon");
            Navigate("Help");Press("Objectives and direction");await SettleUi();Press("Resume guidance");await SettleUi();
            Check(_presentation.Guidance.Visible&&_phone.Visible,"Guidance resumes from Help and shows unread texts");
            foreach(var (size,name) in new(Vector2I,string)[]{(new(1920,1080),"1080"),(new(2560,1080),"2560x1080"),(new(3440,1440),"3440x1440"),(new(1280,720),"720-150")})
            {
                if(name=="720-150"){_presentation.UiScale=1.5;ApplyTextScale();}
                GetWindow().Size=size;RefreshGuidance();await SettleUi();await SettleUi();
                foreach(var modern in new[]{false,true})
                {
                    _phoneTween?.Kill();_phoneSlide=0;_phoneKey="";BuildPhone(modern,(float)_presentation.UiScale);ResizeFloatingOffice();await SettleUi();
                    await CaptureSmokeImage($"alpha-phone-{(modern?"smartphone":"phs")}-{name}");
                    var bounds=_phone.GetGlobalRect();var reply=_showGuidance.GetGlobalRect();var view=GetViewportRect().Size;
                    Check(_phone.Modern==modern&&bounds.Position.X>=0&&bounds.Position.Y>=_floatingTop.GetGlobalRect().End.Y&&bounds.End.X<=view.X+1&&bounds.End.Y<=view.Y+1&&reply.End.Y<=bounds.End.Y+1,$"Phone fits {name} ({(modern?"smartphone":"PHS")})");
                }
            }
            _presentation.UiScale=1;ApplyTextScale();_phoneKey="";RefreshGuidance();
            ShowMenu();Press("Report a problem");await SettleUi();
            var choices=_menuContent.GetChildren().OfType<CheckBox>().ToArray();Check(choices.Length==3&&!choices[0].ButtonPressed&&!choices[1].ButtonPressed&&choices[2].ButtonPressed,"Report attachments: screen and career off, timeline on");
            _menuContent.GetChildren().OfType<TextEdit>().Single().Text="Alpha smoke: local export only.";
            await CaptureSmokeImage("alpha-report");Press("Export local report");await SettleUi();
            var picker=GetChildren().OfType<FileDialog>().Last();var reportPath=Path.Combine(SmokeOutput,"alpha-report.zip");picker.EmitSignal(FileDialog.SignalName.FileSelected,reportPath);await SettleUi();
            using(var zip=ZipFile.OpenRead(reportPath))Check(zip.Entries.Any(e=>e.FullName=="report.json")&&zip.GetEntry("timeline.log") is {} log&&new StreamReader(log.Open()).ReadToEnd().Contains("session start"),"Report holds report.json and the session timeline");
            Check(_state.ToJson()==before,"Reporting leaves simulation untouched");
            _menuContent.GetChildren().OfType<Button>().Single(b=>b.Text=="Cancel").EmitSignal(BaseButton.SignalName.Pressed);
            Press("Settings");await SettleUi();Check(_menuContent.FindChildren("*","HSlider",true,false).OfType<HSlider>().Count()>=3,"Volume controls and text scale present");
            _menuContent.GetChildren().OfType<Button>().Single(b=>b.Text=="Back").EmitSignal(BaseButton.SignalName.Pressed);Press("Resume");
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
            // Tier 1 fix 2: runway before hiring, short-runway confirmation, missed-payday text and one-tap cover.
            _state=GameState.NewGame(0);ResetManagementSession();_scanIndex=_state.Events.Count;_dirty=true;
            OpenWorkspace("Recruitment");await SettleUi();
            var candidate=_state.Candidates.First(c=>c.Id==_candidateOption.GetSelectedId());_salaryOffer.Value=candidate.ExpectedSalary;RefreshRunway();RefreshDeskWarning();await SettleUi();
            var runway=_state.HiringRunway(candidate.ExpectedSalary);
            Check(_runwayLabel.IsVisibleInTree()&&_runwayLabel.Text.Contains($"¥{runway.MonthlyCosts:N0}")&&_runwayLabel.Text.Contains("Runway:")&&!runway.Safe,"Recruitment shows the hiring runway before a hire");
            Check(_deskWarning.Visible==(_state.WorkplaceWithFreeDesk is null),"Desk warning matches free desks");
            foreach(var (size,name,scale) in new(Vector2I,string,double)[]{(new(1280,720),"720-150",1.5),(new(1920,1080),"1080",1)})
            {
                _presentation.UiScale=scale;ApplyTextScale();GetWindow().Size=size;RefreshRunway();await SettleUi();await SettleUi();
                // Open the page with the phone already out, as a player following a text would.
                if(!_phoneOpen)OpenPhone(false);_phoneTween?.Kill();_phoneSlide=0;OpenWorkspace("Recruitment");RefreshRunway();await SettleUi();await SettleUi();
                ScrollContainer? runwayScroll=null;
                for(Node n=_runwayLabel;n is not null;n=n.GetParent())if(n is ScrollContainer scroll){runwayScroll=scroll;break;}
                // Scroll the line's first row to the top, as a player reading it would; at large text it can be taller than the view.
                if(runwayScroll is not null)runwayScroll.ScrollVertical+=(int)(_runwayLabel.GetGlobalPosition().Y-runwayScroll.GetGlobalPosition().Y);await SettleUi();
                var runwayShown=runwayScroll is not null&&runwayScroll.Size.Y>=80&&runwayScroll.GetGlobalRect().HasPoint(_runwayLabel.GetGlobalPosition()+new Vector2(2,2));
                Check(runwayShown,$"Recruitment page has room to show the runway line at {name} (scroll {runwayScroll?.GetGlobalRect()}, line {_runwayLabel.GetGlobalRect()}, page {_side.Visible}:{_side.GetGlobalRect()} report {_report.Visible}:{_report.GetGlobalRect()}, phone {_phoneOpen})");
                if(PhoneCrampsPage())
                {
                    Check(!_phoneOpen&&_phoneIcon.Visible,$"Phone folds to its icon when a page opens at {name}");
                    RefreshGuidance();await SettleUi();
                    Check(!_phoneOpen,$"New texts wait on the icon instead of covering the page at {name}");
                }
                else Check(_phoneOpen&&_phone.Visible&&!_runwayLabel.GetGlobalRect().Intersects(_phone.GetGlobalRect()),$"Open phone leaves the runway line uncovered at {name}");
                await CaptureSmokeImage($"alpha-hiring-runway-{name}");
            }
            before=_state.ToJson();Press("Hire at selected workplace");await SettleUi();
            var warning=GetChildren().OfType<ConfirmationDialog>().LastOrDefault(d=>d.Title=="Hire with a short runway");
            Check(warning is not null&&warning.DialogText.Contains("months")&&_state.ToJson()==before,"Short runway asks before hiring");
            await CaptureSmokeImage("alpha-hiring-runway-warning");
            warning!.EmitSignal(AcceptDialog.SignalName.Confirmed);await SettleUi();
            Check(_state.ControlledStaff.Any(p=>p.Id==candidate.Id),"Hire anyway goes ahead");
            _state.Advance(_state.Clock.HoursUntil(new DateTime(1996,7,1)));_state.Protagonist.PersonalAccount.Balance=1_000_000;
            ScanEvents();_dirty=true;RefreshMoneyHeader();RefreshGuidance();await SettleUi();
            var cover=_phone.FindChild("GuidanceCoverArrears",true,false) as Button;
            Check(_state.WageArrears>0&&_phone.Visible&&PhoneSays("Payday missed")&&cover is not null,"Missed payday texts with a cover offer");
            await CaptureSmokeImage("alpha-wage-arrears");
            var savings=_state.PersonalMoney;var owed=_state.WageArrears;cover!.EmitSignal(BaseButton.SignalName.Pressed);_state.Advance(1);await SettleUi();
            Check(_state.WageArrears==0&&_state.PersonalMoney==savings-owed,"Cover from savings pays the wages only on the tap");
            _state.Recruitment=null;_state.LastRecruitmentAt=_state.Clock.Now.AddDays(-3);RefreshStaff();
            Check(_recruitButton.Disabled&&_recruitButton.Text.Contains($"next search from {_state.Clock.Now.AddDays(11):d MMM yyyy}"),"Recruitment cooldown shows the next search date");
            GD.Print($"ALPHA SMOKE PASSED: {_smokeChecks} checks.");
            var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("ALPHA SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
