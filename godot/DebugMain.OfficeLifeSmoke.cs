using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunOfficeLifeSmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);_careers=new CareerStore(Path.Combine(SmokeOutput,"office-life-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();_side.Hide();_report.Hide();
            _state=GameState.NewGame(2);_state.Apply(new CreateDoujinCommand("Office life","drama",64));_state.Advance(1);
            ResetManagementSession();_side.Hide();_homeOffice.Show();_homeOffice.SetProcess(false);SetSpeed(1);_homeOffice.Speed=1;
            var lead=_homeOffice.StaffActors[_state.ProtagonistPersonId];
            var helper=_homeOffice.Companion!;Check(lead.Visible&&helper.Visible,"Creator and Helper-Chan begin on site");
            Check(lead.UsingModularKit&&helper.UsingImportedModel,"Approved employee kit and rigged Helper are installed");
            var stateBefore=_state.ToJson();var gestures=new HashSet<string>();
            for(var i=0;i<200;i++){helper.Animate(.1,1);gestures.Add(helper.DeskActivity);}
            Check(gestures.IsSupersetOf(new[]{"Writing notes","Checking clipboard"}),"Imported desk cycle writes and checks the clipboard");
            Check(helper.ImportedClip is "Write" or "Sit","Companion is driven by the imported seated clips");
            var animationTime=helper.ImportedAnimationTime;helper.Animate(.5,0);
            Check(Math.Abs(helper.ImportedAnimationTime-animationTime)<.00001,"Pause freezes imported animation playback");
            Check(helper.AtDesk&&!helper.Moving,"Desk gestures keep Helper-Chan at her own station");
            await CaptureSmokeImage("office-life-kitchen");
            _homeOffice.FocusCompanion();await CaptureSmokeImage("office-life-chibi-seated");_homeOffice.ResetCamera();
            var visited=false;var accompanied=false;var returned=false;
            for(var i=0;i<3000;i++)
            {
                _homeOffice._Process(1d/60);
                if(_homeOffice.VisualBreakCount>0)
                {
                    if(!visited)Check(lead.Activity=="Break","Decorative visit uses a break pose");visited=true;
                    if(!lead.Moving&&!helper.Moving&&!helper.AtDesk)
                    {if(!accompanied)await CaptureSmokeImage("office-life-shared-break");accompanied=true;}
                }
                else if(visited&&!lead.Moving&&!helper.Moving&&helper.AtDesk){returned=true;break;}
            }
            Check(visited&&accompanied&&returned,$"Visual break takes both characters to the table and returns them to their desks (visited={visited}, accompanied={accompanied}, returned={returned}, speed={_homeOffice.Speed}, lead={lead.Activity}, moving={lead.Moving}, helperMoving={helper.Moving})");
            Check(_state.ToJson()==stateBefore,"Visual breaks and gestures do not change production, time, money or needs");
            foreach(var speed in new[]{1d,8d})
            {
                _state=GameState.NewGame(2);_state.Apply(new CreateDoujinCommand("Departure","drama",64));_state.Apply(new SetOutsideJobCommand(OutsideJob.Afternoons));_state.Advance(3);
                _homeOffice.Bind(_state,_state.Protagonist.Employment!.LocationId);_homeOffice.Speed=speed;
                lead=_homeOffice.StaffActors[_state.ProtagonistPersonId];helper=_homeOffice.Companion!;var origin=lead.Position;
                _state.Advance(2);_homeOffice.Bind(_state,_state.Protagonist.Employment!.LocationId);
                Check(lead.Visible&&lead.Moving&&lead.Position==origin,"Outside work starts a walk from the desk, with no teleport");
                Check(helper.Visible&&helper.Moving&&!helper.AtDesk,"Helper-Chan leaves her desk to accompany outside work");
                _homeOffice.Speed=0;_homeOffice._Process(.5);Check(lead.Position==origin,"Pausing freezes departure in place");_homeOffice.Speed=speed;
                var exit=_homeOffice.StaffExit;var walked=false;
                for(var i=0;i<1500&&(lead.Visible||helper.Visible);i++)
                {
                    _homeOffice._Process(1d/120);walked|=lead.Visible&&lead.Position.DistanceTo(origin)>.1f;
                    if(!lead.Visible)Check(lead.Position.DistanceTo(exit)<.001f&&lead.TrailCount==0,"Departure hides only at the entry door and clears exposure immediately");
                    if(lead.Visible&&lead.Position.DistanceTo(exit)<.1f)Check(_homeOffice.Doors.Last().ClearToPass,"Entry door is open as the creator crosses");
                }
                Check(walked&&!lead.Visible&&!helper.Visible,"Creator and companion both exit cleanly at running and fast speeds");
                _state.Advance(4);_homeOffice.Bind(_state,_state.Protagonist.Employment!.LocationId);
                for(var i=0;i<2000&&(!lead.Visible||!helper.Visible||lead.Moving||helper.Moving);i++)_homeOffice._Process(1d/120);
                Check(lead.Visible&&helper.Visible&&!lead.Moving&&!helper.Moving&&helper.AtDesk,"Both characters return through the entrance to their stations");
            }
            var door=new OfficeDoor();AddChild(door);door.RequestPassage();door.Animate(.25,1);
            Check(door.ClearToPass,"Door leaf opens through a hinge rotation");var leaf=door.GetChild(0).GetChild<MeshInstance3D>(0);var dimensions=leaf.Mesh.GetAabb().Size;
            door.Animate(1,1);door.Animate(1,1);Check(door.Openness==0&&leaf.Mesh.GetAabb().Size==dimensions,"Door closes without stretching or changing leaf dimensions");door.QueueFree();
            _homeOffice.Speed=1;await CaptureSmokeImage("office-life-returned");
            CheckAmbientDoorTraffic();
            GD.Print($"OFFICE LIFE SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("OFFICE LIFE SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
