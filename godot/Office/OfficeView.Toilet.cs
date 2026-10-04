using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    internal Vector3 ToiletDoor=>new(_plan.Width*.25f+3.92f,0,
        ViewingFamilyHome?Math.Min(_plan.Depth*.25f*.36f,GenkanStepZ-.65f):_plan.Depth*.125f);
    private int? _staffToiletId;
    private bool _toiletLeaveRequested;
    private OfficeToiletVisit? _staffToiletVisit;
    private bool _toiletInside=>_staffToiletVisit is not null;
    private float ToiletFit=>ViewingFamilyHome?1f:Math.Min(1f,(_plan.Depth*.125f-1.5f)/.84f);
    internal Vector3 ToiletSeat=>ToiletDoor+new Vector3(1.16f,0,.28f*ToiletFit);
    private readonly Dictionary<int,double> _nextToiletVisit=new();
    private OfficeWorldLabel? _toiletStatus;
    internal int CompletedStaffToiletVisits { get; private set; }
    internal int ToiletReservations=>(_staffToiletId is null?0:1)+_ambientTraffic.Count(t=>t.ToiletReserved);
    internal bool ToiletInUse=>_toiletInside||_ambientTraffic.Any(t=>t.UsingToilet);
    private bool ToiletAvailable(OfficeAmbientTraffic? parent=null)=>_staffToiletId is null&&
        !_ambientTraffic.Any(t=>t!=parent&&t.ToiletReserved);

    private void ResetToiletVisits()
    {_staffToiletVisit?.Cancel();_staffToiletVisit=null;_staffToiletId=null;_toiletLeaveRequested=false;_nextToiletVisit.Clear();}

    private void BuildToiletRoom()
    {
        var at=ToiletDoor;var x=at.X-.12f;var z=at.Z;
        var tile=new Color("c2c8bc");var porcelain=new Color("eeeade");
        var fit=ToiletFit;
        // Roofless side room with cutaway walls and visible fittings.
        OfficeArt.Box(_room,new(x+.9f,-.10f,z),new(1.8f,.20f,1.68f*fit),tile);
        foreach(var side in new[]{-1f,1f})
            Wall(new(x+.9f,1.15f,z+side*.84f*fit),new(1.8f,2.3f,.10f),side<0?0:2);
        Wall(new(x+1.8f,1.15f,z),new(.10f,2.3f,1.78f*fit),3);
        for(var i=1;i<6;i++)OfficeArt.Box(_room,new(x+i*.3f,.003f,z),new(.012f,.006f,1.65f*fit),tile.Darkened(.18f));
        for(var i=-2;i<=2;i++)OfficeArt.Box(_room,new(x+.9f,.004f,z+i*.3f*fit),new(1.78f,.006f,.012f),tile.Darkened(.18f));
        // Toilet tank, pedestal, bowl and oval seat face the corridor door.
        OfficeArt.Box(_room,new(x+1.52f,.62f,z+.28f*fit),new(.27f,.65f,.52f*fit),porcelain);
        OfficeArt.Sphere(_room,new(x+1.16f,.23f,z+.28f*fit),new(.42f,.46f,.40f*fit),porcelain);
        OfficeArt.Sphere(_room,new(x+1.14f,.46f,z+.28f*fit),new(.65f,.20f,.50f*fit),porcelain);
        OfficeArt.Sphere(_room,new(x+1.09f,.557f,z+.28f*fit),new(.39f,.018f,.29f*fit),new("727d79"));
        OfficeArt.Box(_room,new(x+1.37f,.97f,z+.28f*fit),new(.08f,.10f,.12f),new("afb7af"));
        // Hand basin, mirror, towel and paper holder.
        OfficeArt.Box(_room,new(x+.76f,.43f,z-.61f*fit),new(.55f,.86f,.32f*fit),new("a69b80"));
        OfficeArt.Box(_room,new(x+.76f,.89f,z-.61f*fit),new(.61f,.08f,.38f*fit),porcelain);
        OfficeArt.Sphere(_room,new(x+.76f,.94f,z-.59f*fit),new(.36f,.025f,.24f*fit),new("788f91"));
        OfficeArt.Box(_room,new(x+.76f,1.04f,z-.74f*fit),new(.04f,.20f,.04f),new("b5c1bc"));
        OfficeArt.Box(_room,new(x+.76f,1.55f,z-.775f*fit),new(.54f,.55f,.03f),new("a5c4cd"));
        OfficeArt.Box(_room,new(x+1.36f,1.15f,z-.765f*fit),new(.26f,.38f,.03f),new("d7c2a2"));
        OfficeArt.Sphere(_room,new(x+.87f,.72f,z+.75f*fit),new(.20f,.18f,.15f),porcelain);
        AmbientDoor(new(x,0,z),"WC",.72f,false);
        _toiletStatus=OfficeArt.WorldLabel(_room,"",new(x-.15f,1.65f,z),16,new("e9bd7b"));
    }

    private void UpdateToiletVisits(double delta)
    {
        if(_toiletStatus is not null)_toiletStatus.Text=ToiletInUse?"IN USE":"";
        if(Editing||Speed<=0||_state is null)return;
        if(_staffToiletId is {} id)
        {
            if(!_actors.TryGetValue(id,out var actor)){ResetToiletVisits();return;}
            _toiletLeaveRequested|=EndingDay||ClosedForNight;
            if(_staffToiletVisit is null)return;
            if(!_staffToiletVisit.Update(delta,Speed,_toiletLeaveRequested,DoorReady))return;
            // The visible resident crossed the same doorway on the way out.
            _staffToiletVisit=null;_staffToiletId=null;_toiletLeaveRequested=false;CompletedStaffToiletVisits++;
            _nextToiletVisit[id]=_visualClock+110+id%23;_targets.Remove(id);SyncActors();return;
        }
        if(EndingDay||ClosedForNight||!ToiletAvailable())return;
        foreach(var (personId,actor) in _actors)
        {
            if(!_nextToiletVisit.TryGetValue(personId,out var due))
            {_nextToiletVisit[personId]=_visualClock+65+personId%19;continue;}
            if(_visualClock<due||!actor.Visible||actor.Moving||_departing.Contains(personId)||
                _visualBreaks.ContainsKey(personId)||actor.Activity is not ("Work" or "Idle"))continue;
            if(personId==_state.ProtagonistPersonId&&_ambientTraffic.Any(t=>t.MealVisitActive))continue;
            _staffToiletId=personId;_toiletLeaveRequested=false;
            _targets.Remove(personId);SyncActors();break;
        }
    }

    // Smoke checks send someone on a trip now instead of waiting for the schedule.
    internal void SendToToilet(int personId){if(!_actors.ContainsKey(personId)||!ToiletAvailable())return;_staffToiletId=personId;_toiletLeaveRequested=false;_targets.Remove(personId);SyncActors();}
    internal void SendToBreakRoom(int personId){if(!_actors.ContainsKey(personId))return;_visualBreaks[personId]=new(){Seat=0};_targets.Remove(personId);SyncActors();}

    private void FinishToiletApproach(OfficeActor actor)
    {
        if(_staffToiletId!=actor.PersonId||_toiletInside||actor.Moving)return;
        _staffToiletVisit=new(actor,ToiletDoor,ToiletSeat,18);
    }
}
