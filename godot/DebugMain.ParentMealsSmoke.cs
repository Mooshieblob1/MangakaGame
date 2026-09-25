using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunFamilyHomeSmoke()
    {
        SetProcess(false);_homeOffice.SetProcess(false);_officeView.SetProcess(false);
        try
        {
            System.IO.Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(System.IO.Path.Combine(SmokeOutput,"family-"+Guid.NewGuid().ToString("N")));
            GetWindow().Size=new(1920,1080);
            await CheckParentMeals();GD.Print($"FAMILY HOME SMOKE PASSED: {_smokeChecks} checks.");GetTree().Quit();
        }
        catch(Exception ex){GD.PrintErr("FAMILY HOME SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }

    private async Task CheckParentMeals()
    {
        // The overnight fixture ends before the morning arrivals; start an occupied work hour.
        _state=GameState.NewGame(2);_state.Apply(new CreateDoujinCommand("Family home","drama",64));_state.Advance(1);
        ResetManagementSession();ShowOffice();_helperPopup.Hide();_popupEvents.Clear();
        await SettleUi();_homeOffice.SetProcess(false);
        var before=_state.ToJson();
        SetSpeed(1);_homeOffice.Speed=1;
        var seen=new HashSet<string>();var climbing=new HashSet<string>();
        var startingMeals=_homeOffice.AmbientTraffic.ToDictionary(t=>t,t=>t.CompletedMeals);
        Check(_homeOffice.Doors.Count==2,"Family home has separate working WC and entry doors, with rear stairs");
        var previousPlaces=_homeOffice.AmbientTraffic.ToDictionary(t=>t,t=>(t.IsOutside,t.Actor.PrivacyObscured));
        var returned=new HashSet<string>();var wcReturns=new HashSet<string>();var seatedVisitors=new HashSet<string>();
        var sawCreatorInWc=false;
        var startingWc=_homeOffice.CompletedStaffToiletVisits;
        var creatorBreaks=0;var wasBreaking=false;
        for(var i=0;i<54000;i++)
        {
            _homeOffice._Process(1d/60);
            var breaking=_homeOffice.CreatorOnVisualBreak;
            if(breaking&&!wasBreaking)creatorBreaks++;
            wasBreaking=breaking;
            foreach(var traffic in _homeOffice.AmbientTraffic)
            {
                var actor=traffic.Actor;
                var prior=previousPlaces[traffic];
                if(traffic.HouseExits-traffic.HouseEntries!=(traffic.IsOutside?1:0))
                    throw new InvalidOperationException("Parent entered or exited twice without the opposite crossing.");
                if(prior.IsOutside&&!traffic.IsOutside)
                {
                    Check(actor.Visible&&actor.Position.DistanceTo(_homeOffice.StaffExit)<.001f,
                        "An outside parent re-enters only at the entrance");returned.Add(actor.ResidentRole);
                }
                if(prior.PrivacyObscured&&!actor.PrivacyObscured)
                {
                    Check(actor.Visible&&actor.Position.DistanceTo(_homeOffice.ToiletDoor)<.001f,"WC occupant walks back through the same WC door");
                    wcReturns.Add(actor.ResidentRole);
                }
                if(traffic.UsingToilet&&!actor.Moving&&seatedVisitors.Add(actor.ResidentRole))
                {
                    Check(actor.Visible&&actor.PrivacyObscured&&actor.ImportedClip=="Sit"&&
                        actor.Position.DistanceTo(_homeOffice.ToiletSeat)<.001f&&_homeOffice.ToiletPrivacyVisible,
                        $"Parent remains visibly seated inside the WC under the privacy blur: visible={actor.Visible}, obscured={actor.PrivacyObscured}, clip={actor.ImportedClip}, seat distance={actor.Position.DistanceTo(_homeOffice.ToiletSeat)}, blur={_homeOffice.ToiletPrivacyVisible}");
                    var position=actor.Position;traffic.Update(100,0,0,true);
                    Check(traffic.UsingToilet&&actor.Position==position&&actor.PrivacyObscured,"Pause freezes the occupied WC visit");
                }
                if(traffic.IsOutside)
                {
                    var exits=traffic.HouseExits;var entries=traffic.HouseEntries;
                    traffic.Update(100,1,0,false);
                    if(!traffic.IsOutside||traffic.Actor.Visible||exits!=traffic.HouseExits||entries!=traffic.HouseEntries)
                        throw new InvalidOperationException("Off-hours reset an outside parent's location.");
                }
                previousPlaces[traffic]=(traffic.IsOutside,actor.PrivacyObscured);
                if(actor.Visible&&actor.Position.Y>.2f&&actor.Position.Y<_homeOffice.FamilyStairTop.Y-.2f)
                    climbing.Add(actor.ResidentRole);
                if(traffic.Eating)
                {
                    if(breaking)throw new InvalidOperationException("A parent's meal overlaps the creator and Helper's break.");
                    if(seen.Add(actor.ResidentRole))
                    {
                        Check(actor.ImportedClip=="Eat"&&traffic.ReservesMealSeat,"Parent sits and eats at a reserved chair");
                        var position=actor.Position;var meals=traffic.CompletedMeals;
                        traffic.Update(100,0,0,true);
                        Check(traffic.Eating&&actor.Position==position&&traffic.CompletedMeals==meals,"Pause freezes the meal");
                        await CaptureSmokeImage("parent-meal-"+actor.ResidentRole.ToLowerInvariant());
                        await CaptureFamilyScene("family-meal-"+actor.ResidentRole.ToLowerInvariant(),7,new(3,0,0));
                    }
                }
            }
            if(_homeOffice.ToiletReservations>1)
                throw new InvalidOperationException("Staff and parents double-booked the WC.");
            var creator=_homeOffice.StaffActors[_state.ProtagonistPersonId];
            if(creator.PrivacyObscured)
            {
                if(_homeOffice.Companion is not {Visible:true,AtDesk:true} helper||helper.Destination.X>=0)
                    throw new InvalidOperationException("Helper followed the creator on a private WC visit.");
                if(!creator.Moving&&!sawCreatorInWc)
                {
                    sawCreatorInWc=true;
                    Check(creator.Visible&&creator.Activity=="Toilet"&&creator.SeatHeight>0&&
                        creator.Position.DistanceTo(_homeOffice.ToiletSeat)<.001f&&_homeOffice.ToiletPrivacyVisible,
                        "The creator sits inside the blurred WC while Helper stays at her own desk");
                    _homeOffice.Bind(_state,_state.Protagonist.Employment!.LocationId);
                    Check(creator.Activity=="Toilet"&&creator.PrivacyObscured,"A simulation refresh preserves the private WC pose");
                    await CaptureFamilyScene("family-wc-in-use",6,new(4.8f,0,-.5f));
                }
            }
            if(seen.Count==2&&returned.Count==2&&wcReturns.Count==2&&creatorBreaks>=2&&
                _homeOffice.CompletedStaffToiletVisits>startingWc&&
                _homeOffice.AmbientTraffic.All(t=>t.CompletedMeals>startingMeals[t]&&!t.Travelling&&!t.UsingToilet))break;
        }
        Check(seen.Count==2&&climbing.Count==2,"Both parents use the actual staircase and get an occasional meal");
        Check(creatorBreaks>=2&&_homeOffice.AmbientTraffic.All(t=>t.CompletedMeals>startingMeals[t]),
            $"Staggering preserves both creator breaks and parent meals: breaks={creatorBreaks}, meals="+string.Join(",",_homeOffice.AmbientTraffic.Select(t=>$"{t.Actor.ResidentRole}:{t.CompletedMeals-startingMeals[t]}")));
        foreach(var traffic in _homeOffice.AmbientTraffic)
            Check(!traffic.Travelling&&!traffic.UsingToilet&&!traffic.Actor.Visible,
                "Parent retires inside their actual destination, without lingering at the threshold");
        Check(returned.Count==2&&wcReturns.Count==2,"Both parents complete outings and WC visits");
        Check(seatedVisitors.Count==2&&sawCreatorInWc,"Both parents and the creator use the visible WC seat and privacy blur");
        Check(_homeOffice.CompletedStaffToiletVisits>startingWc,"Staff also make actual WC visits");
        for(var i=0;i<54000&&!_homeOffice.AmbientTraffic.Any(t=>t.Eating);i++)_homeOffice._Process(1d/60);
        Check(_homeOffice.AmbientTraffic.Any(t=>t.Eating),"A later meal remains possible");
        _homeOffice.EndingDay=true;
        for(var i=0;i<1200&&_homeOffice.AmbientTraffic.Any(t=>t.NeedsNightReturn);i++)_homeOffice._Process(1d/60);
        Check(_homeOffice.AmbientTraffic.All(t=>!t.NeedsNightReturn&&!t.Actor.Visible&&t.Actor.TrailCount==0),"Closing time clears meals and WC visits while preserving outside residents");
        _homeOffice.EndingDay=false;
        Check(_state.ToJson()==before,"Staggered meals and stairs leave simulation time, money, work and RNG unchanged");
        await CaptureFamilyScene("family-home-overview",15,new(-.2f,0,-1.8f));
        await CaptureFamilyScene("family-rear-stairs",8,new(3.7f,0,-2.2f));
        await CaptureFamilyScene("family-genkan",7,new(3.7f,0,1));
        await CaptureFamilyScene("family-wc",6,new(4.8f,0,-.5f));
        await CaptureFamilyScene("family-neighborhood",36,Vector3.Zero);
        CheckGestureTimeScale();
    }

    private void CheckGestureTimeScale()
    {
        var sitter=new OfficeActor{Activity="Work",SeatHeight=OfficeArt.DeskSeatHeight};AddChild(sitter);sitter.BuildParent(false);
        foreach(var facing in new[]{0f,-Mathf.Pi/2,-Mathf.Pi,-Mathf.Pi*1.5f})
        {
            sitter.Position=Vector3.Zero;sitter.Destination=new(.1f,0,0);sitter.Route.Enqueue(sitter.Destination);
            sitter.SeatedFacing=facing;sitter.Animate(.1,8,0);
            Check(!sitter.Moving&&Math.Abs(Mathf.AngleDifference(sitter.Rotation.Y,facing))<.001f&&sitter.ImportedClip=="Sit",
                "Desk arrival applies the chair-facing pose immediately, without a later scene refresh");
        }
        sitter.QueueFree();
        foreach(var (path,clip) in new[]{("res://Assets/Parents/mom/mom-animated.glb","Eat"),
            ("res://Assets/Helper/Model/helper-chan-animated.glb","Write"),
            ("res://Assets/Parents/dad/dad-animated.glb","Sit")})
        {
            var model=CharacterModel.LoadModel(path)!;AddChild(model);
            foreach(var speed in new[]{1d,2d,4d,8d,32d})
            {
                model.Tick(0,1,clip,OfficeArt.DeskSeatHeight);model.Tick(.01,speed,clip,OfficeArt.DeskSeatHeight);
                Check(Math.Abs(model.AnimationTime-.01*speed)<.001,$"{clip} follows the selected {speed}x speed");
                var time=model.AnimationTime;model.Tick(.5,0,clip,OfficeArt.DeskSeatHeight);
                Check(Math.Abs(model.AnimationTime-time)<.001,$"Pause freezes {clip}");
                model.Tick(0,1,"Walk",0);model.Tick(.01,speed,"Walk",0);
                Check(Math.Abs(model.AnimationTime-.01*speed)<.001,"Walk animation retains the selected time scale");
            }
            model.QueueFree();
        }
    }

    private async Task CaptureFamilyScene(string name,float zoom,Vector3 pan)
    {
        if(!OS.GetCmdlineUserArgs().Contains("--capture"))return;
        await SettleUi();
        var saved=_homeOffice.CapturePreferences();var shot=_homeOffice.CapturePreferences();
        shot.Cameras[shot.Location]=new[]{.7f,zoom,pan.X,pan.Z,0f};
        var uiVisible=_floatingUi!.Visible;
        try
        {
            _floatingUi.Hide();_homeOffice.RestorePreferences(shot);
            await CaptureSmokeImage(name);
        }
        finally{_homeOffice.RestorePreferences(saved);_floatingUi.Visible=uiVisible;}
    }
}
