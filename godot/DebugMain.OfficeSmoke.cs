using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async Task RunOfficeSmoke()
    {
        CheckHyperlapseExposure();
        CheckAmbientDoorTraffic();
        _state=GameState.NewGame(7);_scanIndex=0;_log.Clear();_recapDialog.Hide();Pause();_dirty=true;_mainTabs.CurrentTab=5;
        await SettleUi();
        Check(_officeView.ActorCount==1&&_officeView.AmbientCount==2,"Family office contains creator and ambient residents");
        Check(_state.Locations[0].MonthlyRent==0&&_state.UsableWorkspaces(_state.Locations[0].Id)==2,"Family workstations and free rent");
        var unchanged=_state.ToJson();_officeView.RotateCamera(90);_officeView.ResetCamera();Check(_state.ToJson()==unchanged,"Camera does not mutate simulation");
        await CaptureSmokeImage("office-family");
        SetSpeed(2);await SettleUi();Press("Furnish");Check(OfficeEditing&&_speed==0,"Furnishing pauses the clock");
        _officeItem=_officeDraft!.Placements.First().ItemId;MoveOfficeFurniture(new(0,99));Check(_officeFeedback.Text.Contains("Cannot apply"),"Invalid placement has visible explanation");
        Check(_state.ToJson()==unchanged,"Invalid draft has no cash or state side effects");Press("Discard changes");Check(!OfficeEditing&&_speed==2,"Discard restores previous running speed");
        Pause();Press("Furnish");_officeCatalog.Select(Array.FindIndex(OfficeCatalog.Furniture,f=>f.Id=="plant"));Press("Add / replace selected chair");Check(_officeDraft!.Purchases.Count==1,"Furniture catalog adds a quoted purchase");
        Check(_state.Money==300000,"Furniture preview does not debit business");await CaptureSmokeImage("office-editor");
        Press("Apply layout and cost");Check(!OfficeEditing&&_state.Money==299000&&_speed==0,"Apply charges once and preserves prior pause");
        _officeProperty.Select(0);Press("Preview studio move");Check(OfficeEditing&&_state.Locations.Count(l=>l.BusinessId==_state.ControlledBusinessId)==1,"Move preview does not create live lease");
        Press("Fill all desks (purchase missing)");_officeCatalog.Select(Array.FindIndex(OfficeCatalog.Furniture,f=>f.Id=="break"));Press("Add / replace selected chair");
        Press("Apply layout and cost");Check(!OfficeEditing&&_state.Locations.Count(l=>l.BusinessId==_state.ControlledBusinessId)==2,"Combined relocation applies from editor");
        var moved=_state.Locations.Single(l=>!l.Closed&&l.BusinessId==_state.ControlledBusinessId);Check(_state.UsableWorkspaces(moved.Id)==4&&moved.BreakSeats==4,"Move equips desks and extra break furniture");
        Check(_state.Furniture.Count(i=>i.Owner==FurnitureOwner.Family)==4,"Family furniture was not duplicated or sold");
        _officeView.Effect=1;_officeEffect.Select(1);_officeView.RotateCamera(90);var camera=_officeView.CapturePreferences();
        Save();var saved=_state.ToJson();_state.Advance(1);Load();await SettleUi();Check(_state.ToJson()==saved,"Office inventory/layout round-trip through controls");
        Check(_officeView.Effect==1&&_officeView.CapturePreferences().Cameras[camera.Location].SequenceEqual(camera.Cameras[camera.Location]),"Camera and effect preferences persist outside simulation");
        _officeView.Effect=2;_officeEffect.Select(0);_officeView.ResetCamera();
        await CaptureSmokeImage("office-small-studio");
        _state.Apply(new StudioActionCommand(StudioAction.CareerHome));_state.Advance(24);_dirty=true;await SettleUi();
        Check(_officeView.AmbientCount==2&&_state.Furniture.Count(i=>i.Owner==FurnitureOwner.Family)==4,"Career return restores one inhabited family home");
        Check(_state.Offices.Where(o=>o.LocationId!=_state.Protagonist.Employment!.LocationId).All(o=>!o.Assignments.Any(a=>a.PersonId==_state.ProtagonistPersonId)),"Career removes old desk reservation");

        // A funded large-office fixture isolates rendering from years of economic progression.
        _state=GameState.NewGame(11);_state.ControlledBusiness.Account.OpeningBalance+=10000000;_state.Money+=10000000;
        _state.Apply(new StudioActionCommand(StudioAction.Move,16));_state.Advance(24);
        var location=_state.Protagonist.Employment!.LocationId;var arrangement=_state.ArrangeOffice(location,true,true);
        _state.Apply(new ApplyOfficeLayoutCommand(location,_state.OfficeRevision,arrangement.Placements,arrangement.Purchases,[]));
        for(var i=1;i<32;i++)
        {
            var person=new Person{Id=_state.AllocateId(),Name=$"Assistant {i}",ExpectedSalary=StudioRules.MinimumMonthlySalary,Skills=StageOrder.All.ToDictionary(s=>s,_=>60)};
            person.EmploymentHistory.Add(new(){BusinessId=_state.ControlledBusinessId,LocationId=location,StartsAt=_state.Clock.Now,MonthlySalary=StudioRules.MinimumMonthlySalary});_state.People.Add(person);
        }
        _state.Apply(new StudioActionCommand(StudioAction.SetFounderSalary));
        _state.OfficeActivities=_state.ControlledStaff.Select(p=>new OfficeActivity(p.Id,location,OfficeActivityKind.Work,_state.Clock.Now,Stage.Inks)).ToList();
        _scanIndex=_state.Events.Count;_recapDialog.Hide();_dirty=true;SetSpeed(8);await SettleUi();
        Check(_officeView.ActorCount==32&&_officeView.AmbientCount==6,"Large office renders staff and shared-building occupants");
        var beforeVisual=_state.ToJson();
        foreach(var speed in new[]{1d,2d,4d,8d})
        {
            _officeView.Speed=speed;
            var peakTrails=0;
            for(var i=0;i<120;i++){_officeView._Process(.016);peakTrails=Math.Max(peakTrails,_officeView.VisibleTrails);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            Check(_state.ToJson()==beforeVisual,$"{speed}x rendering cannot advance or mutate simulation");
            if(speed==8)Check(peakTrails>0,"Timelapse generates bounded character trails");
        }
        _officeView.Speed=0;_officeView._Process(.1);Check(_officeView.VisibleTrails==0,"Pause clears timelapse ghosts");
        _officeView.Speed=8;
        if(DisplayServer.GetName()!="headless")
        {
            var timings=new System.Collections.Generic.List<double>();var last=Time.GetTicksUsec();
            for(var i=0;i<180;i++)
            {
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);var now=Time.GetTicksUsec();if(i>=30)timings.Add((now-last)/1000d);last=now;
                if(i is 60 or 65 or 70)await CaptureSmokeImage($"office-timelapse-{i}");
            }
            var sorted=timings.OrderBy(x=>x).ToArray();GD.Print($"OFFICE PERFORMANCE: {RenderingServer.GetVideoAdapterName()}, 32 staff + 6 ambient, 8x Full; median {sorted[sorted.Length/2]:F2}ms, p95 {sorted[(int)(sorted.Length*.95)]:F2}ms; capture frames included.");
            _officeView.Effect=1;_officeEffect.Select(1);timings.Clear();last=Time.GetTicksUsec();
            for(var i=0;i<150;i++){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);var now=Time.GetTicksUsec();if(i>=30)timings.Add((now-last)/1000d);last=now;}
            sorted=timings.OrderBy(x=>x).ToArray();GD.Print($"OFFICE PERFORMANCE: 32 staff + 6 ambient, 8x Reduced; median {sorted[sorted.Length/2]:F2}ms, p95 {sorted[(int)(sorted.Length*.95)]:F2}ms.");
            _officeView.Effect=2;_officeEffect.Select(0);
            await CaptureSmokeImage("office-large");
            if(OS.GetCmdlineUserArgs().Contains("--capture"))
            {
                _officeView.SetProcess(false);
                for(var frame=0;frame<45;frame++){_officeView._Process(1d/30);await CaptureSmokeImage($"office-motion-{frame:D3}");}
                _officeView.SetProcess(true);
            }
            DisplayServer.WindowSetSize(new(1280,720));await SettleUi();await CaptureSmokeImage("office-1280");DisplayServer.WindowSetSize(new(1600,900));
        }
        // The same next hour produces identical state with the office shown, hidden or processing disabled.
        var snapshot=_state.ToJson();string? expected=null;
        foreach(var presentation in new[]{0,1,2})
        {
            _state=GameState.FromJson(snapshot);_mainTabs.CurrentTab=presentation==1?0:5;_officeView.SetProcess(presentation!=2);
            _state.Advance(1);_dirty=true;await SettleUi();
            expected??=_state.ToJson();Check(_state.ToJson()==expected,"Office visibility and processing preserve identical hour outcomes");
        }
        _officeView.SetProcess(true);
        Pause();_officeView.Speed=0;_officeView.ClearMotion();
        Check(GameState.FromJson(_state.ToJson()).ControlledStaff.Count()==32,"Large office save validates");
    }
    private void CheckHyperlapseExposure()
    {
        var root=new Node3D();AddChild(root);
        OfficeActor Make()
        {
            var actor=new OfficeActor{Ambient=true,Destination=new(100,0,0)};root.AddChild(actor);actor.Build(1,0,2);return actor;
        }
        var slow=Make();var fast=Make();
        for(var i=0;i<15;i++)slow.Animate(1d/30,8,2);
        for(var i=0;i<72;i++)fast.Animate(1d/144,8,2);
        Check(slow.GlobalPosition.DistanceTo(fast.GlobalPosition)<.01f&&Math.Abs(slow.TrailSpan-fast.TrailSpan)<.1f,"Exposure path is consistent at 30 and 144 FPS");
        Check(slow.TrailCount>40&&slow.TrailCount<=96&&slow.TrailSpan>4&&slow.BodyBlur>.5f,"8x uses continuous bounded exposure with softened current body");
        var previous=0f;
        foreach(var speed in new[]{1d,2d,4d,8d})
        {
            var actor=Make();for(var i=0;i<15;i++)actor.Animate(1d/30,speed,2);
            Check(speed==1?actor.TrailCount==0&&actor.BodyBlur==0:actor.TrailSpan>previous,"Exposure strengthens across 1x/2x/4x/8x");previous=actor.TrailSpan;
        }
        var reduced=Make();for(var i=0;i<15;i++)reduced.Animate(1d/30,8,1);
        Check(reduced.TrailCount>0&&reduced.TrailCount<slow.TrailCount&&reduced.BodyBlur<slow.BodyBlur,"Reduced retains a lighter exposure");
        slow.Animate(.01,8,0);Check(slow.TrailCount==0&&slow.BodyBlur==0,"Off restores sharp bodies without residual history");
        fast.Destination=fast.Position;fast.Animate(.25,8,2);
        Check(fast.TrailCount==0&&fast.BodyBlur==0,"Stationary exposure fades out by elapsed time");
        reduced.Animate(.01,0,2);Check(reduced.TrailCount==0&&reduced.BodyBlur==0,"Pause immediately clears the exposure");
        root.QueueFree();
    }
    private void CheckAmbientDoorTraffic()
    {
        var root=new Node3D();AddChild(root);var actor=new OfficeActor{Ambient=true};root.AddChild(actor);actor.Build(1,0,2);
        var plans=TokyoProperties.All.Select(p=>OfficeCatalog.Plan(new StudioLocation{Seats=p.Seats,PropertyOfferId=p.Id,IsFamilyHome=false}))
            .Prepend(OfficeCatalog.Plan(new StudioLocation{IsFamilyHome=true,Seats=2}));
        foreach(var plan in plans)
        {
            var w=plan.Width*.25f;var d=plan.Depth*.25f;
            foreach(var speed in new[]{1d,2d,4d,8d})
            {
                var traffic=new OfficeAmbientTraffic(actor,new(w+3.92f,0,.85f),new(w+3.92f,0,d-.85f),w+2.7f,0);
                Check(!actor.Visible,"Residents start out of sight behind a door");
                traffic.Update(.1,0,2,true);Check(!actor.Visible,"Paused traffic cannot appear");
                traffic.Update(.016,speed,2,true);
                Check(actor.Visible&&actor.Position==traffic.FirstDoor,"Resident appears at the entry threshold");
                var traversed=0;
                while(traffic.CompletedCrossings==0&&traversed++<1000)
                {
                    traffic.Update(1d/60,speed,2,true);
                    if(actor.Visible&&(actor.Position.X<w+2.69f||actor.Position.X>w+3.93f||
                        actor.Position.Z<.84f||actor.Position.Z>d-.84f||
                        actor.Position.X>w+3.79f&&Math.Abs(actor.Position.Z-.85f)>.01&&Math.Abs(actor.Position.Z-(d-.85f))>.01))
                        throw new InvalidOperationException("Ambient route crossed a wall outside its doorway.");
                }
                Check(traffic.CompletedCrossings==1&&!actor.Visible&&!traffic.Travelling&&actor.TrailCount==0&&actor.Position.DistanceTo(traffic.SecondDoor)<.001f,
                    "Door arrival immediately hides the resident and exposure");
                traffic.Update(.1/speed,speed,2,true);Check(!actor.Visible,"Destination wait remains behind the door");
                traffic.Update(4/speed,speed,2,true);Check(actor.Visible&&actor.Position==traffic.SecondDoor,"Next trip emerges from the other doorway");
                var paused=actor.Position;traffic.Update(.2,0,2,true);Check(actor.Position==paused&&actor.Visible,"Pause freezes a doorway trip without teleporting");
                traffic.Update(10,speed,2,true);Check(!actor.Visible&&traffic.CompletedCrossings==2&&actor.TrailCount==0,"A slow frame still exits cleanly at the doorway");
                traffic.Update(4/speed,speed,2,true);traffic.Update(.01,speed,2,false);
                Check(actor.Visible&&traffic.Travelling,"Off-hours let an existing resident finish walking to the door");
                traffic.Update(10,speed,2,false);Check(!actor.Visible&&!traffic.Travelling,"Off-hours finish at the exit doorway");
            }
        }
        root.QueueFree();
    }
}
