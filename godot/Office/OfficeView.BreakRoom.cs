using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private sealed class VisualBreak { public int Seat; public double Remaining=3; }
    private readonly Dictionary<int,VisualBreak> _visualBreaks=new();
    private readonly Dictionary<int,double> _nextVisualBreak=new();
    private double _visualClock;
    private double _parentMealAfter,_creatorBreakAfter;
    private const double FamilyTableGap=8;
    internal bool CreatorOnVisualBreak=>_state is not null&&
        (_staffToiletId==_state.ProtagonistPersonId||_visualBreaks.ContainsKey(_state.ProtagonistPersonId)||
         _actors.TryGetValue(_state.ProtagonistPersonId,out var lead)&&lead.Visible&&lead.Activity=="Break"||
         _helper is {Visible:true,Moving:true});
    private bool ParentMealsMayStart()
    {
        if(CreatorOnVisualBreak||_visualClock<_parentMealAfter)return false;
        if(_state is null||!_actors.TryGetValue(_state.ProtagonistPersonId,out var lead)||!lead.Visible)return true;
        return !_nextVisualBreak.TryGetValue(_state.ProtagonistPersonId,out var due)||due-_visualClock>FamilyTableGap;
    }
    internal int VisualBreakCount=>_visualBreaks.Count;
    private float BreakCentreZ=>Math.Max(1.9f,_plan.Depth*.25f*.48f);
    private Vector3 SharedBreakSeat(int seat)=>new(_plan.Width*.25f+(seat%2==0?.35f:2.05f),0,BreakCentreZ+(seat/2==0?-.4f:.4f));
    private Vector3 HelperBreakSeat=>new(_plan.Width*.25f+1.2f,0,BreakCentreZ+1.0f);
    private bool ParentSeatAvailable(int seat)
    {
        var position=SharedBreakSeat(seat);
        return !CreatorOnVisualBreak&&!_visualBreaks.Values.Any(b=>b.Seat==seat)&&
            !_actors.Values.Any(a=>a.Visible&&(!a.Moving&&a.Position.DistanceTo(position)<.55f||a.Destination.DistanceTo(position)<.55f));
    }

    private void BuildSharedBreakRoom()
    {
        var w=_plan.Width*.25f;var z=BreakCentreZ;
        var d=_plan.Depth*.25f;
        Wall(new(w+2.6f,1.25f,(z-.7f)/2),new(.12f,2.5f,z-.7f),3);
        Wall(new(w+2.6f,1.25f,(z+.7f+d)/2),new(.12f,2.5f,d-z-.7f),3);
        foreach(var side in new[]{-1f,1f})OfficeArt.Box(_room,new(w+2.6f,1.1f,z+side*.72f),new(.18f,2.2f,.10f),new("777c72"));
        OfficeArt.Box(_room,new(w+2.6f,2.2f,z),new(.18f,.12f,1.54f),new("777c72"));
        // Low cutaway plinths keep the corridor boundary legible when tall walls fade.
        OfficeArt.Box(_room,new(w+2.6f,.12f,(z-.7f)/2),new(.12f,.24f,z-.7f),new("8b9289"));
        OfficeArt.Box(_room,new(w+2.6f,.12f,(z+.7f+d)/2),new(.12f,.24f,d-z-.7f),new("8b9289"));
        var cream=new Color("d6d0bf");var wood=new Color("927352");
        // Appliances share a wall; the outer corridor lane remains clear between both doors.
        OfficeArt.Box(_room,new(w+1.05f,.43f,.42f),new(1.8f,.86f,.65f),cream);
        OfficeArt.Box(_room,new(w+1.05f,.89f,.42f),new(1.9f,.08f,.72f),new("89938e"));
        foreach(var x in new[]{w+.48f,w+1.06f,w+1.63f})
        {OfficeArt.Box(_room,new(x,.44f,.756f),new(.54f,.74f,.025f),cream.Lightened(.09f));OfficeArt.Box(_room,new(x,.67f,.79f),new(.22f,.025f,.03f),new("626b69"));}
        OfficeArt.Box(_room,new(w+.53f,.941f,.43f),new(.48f,.025f,.4f),new("465e64"));
        OfficeArt.Box(_room,new(w+.53f,1.07f,.22f),new(.035f,.26f,.04f),new("bcc8c8"));
        OfficeArt.Box(_room,new(w+.53f,1.20f,.31f),new(.035f,.035f,.22f),new("bcc8c8"));
        OfficeArt.Box(_room,new(w+1.52f,1.13f,.4f),new(.53f,.37f,.44f),new("c3c6ba"));
        OfficeArt.Box(_room,new(w+1.48f,1.13f,.63f),new(.33f,.23f,.025f),new("263840"));
        OfficeArt.Sphere(_room,new(w+1.02f,1.065f,.45f),new(.18f,.26f,.18f),new("bfcbbc"));
        OfficeArt.Box(_room,new(w+2.20f,.81f,.4f),new(.63f,1.62f,.67f),new("e0dfd2"));
        OfficeArt.Box(_room,new(w+2.20f,1.15f,.745f),new(.61f,.025f,.02f),new("6c7776"));
        OfficeArt.Box(_room,new(w+1.97f,.83f,.765f),new(.04f,.28f,.045f),new("6c7776"));
        OfficeArt.Box(_room,new(w+1.05f,1.75f,.24f),new(1.9f,.06f,.42f),wood);
        foreach(var x in new[]{w+.45f,w+.73f,w+1.03f})OfficeArt.Sphere(_room,new(x,1.84f,.27f),new(.13f,.14f,.13f),cream);
        OfficeArt.Box(_room,new(w+1.2f,.69f,z),new(1.1f,.09f,1.28f),wood);
        foreach(var x in new[]{w+.78f,w+1.62f})foreach(var dz in new[]{-.5f,.5f})OfficeArt.Box(_room,new(x,.33f,z+dz),new(.065f,.66f,.065f),new("596465"));
        for(var i=0;i<4;i++)BreakChair(SharedBreakSeat(i),i%2==0?-Mathf.Pi/2:Mathf.Pi/2);
        BreakChair(HelperBreakSeat,0);
        foreach(var offset in new[]{-.3f,.3f})OfficeArt.Sphere(_room,new(w+1.2f+offset,.80f,z),new(.12f,.16f,.12f),new("e8e0cd"));
        OfficeArt.Box(_room,new(w+1.2f,.755f,z+.32f),new(.38f,.015f,.29f),new("dcd1ad"));
        OfficeArt.WorldLabel(_room,"BREAK ROOM",new(w+1.25f,2.18f,.10f));
    }
    private void BreakChair(Vector3 position,float angle)
    {
        var chair=new Node3D{Position=position,Rotation=new(0,angle,0)};_room.AddChild(chair);
        OfficeArt.Box(chair,new(0,OfficeArt.BreakSeatHeight-.04f,0),new(.48f,.08f,.48f),new("527777"));
        OfficeArt.Box(chair,new(0,.68f,.24f),new(.48f,.46f,.07f),new("527777"));
        foreach(var x in new[]{-.18f,.18f})foreach(var z in new[]{-.18f,.18f})OfficeArt.Box(chair,new(x,.2f,z),new(.045f,.4f,.045f),new("50595a"));
    }
    private void UpdateVisualBreaks(double delta)
    {
        if(Editing||Speed<=0||_state is null)return;
        _visualClock+=delta*Speed;var changed=false;
        // Include walks back from the table, then leave a short quiet interval.
        if(CreatorOnVisualBreak)_parentMealAfter=_visualClock+FamilyTableGap;
        var parentVisit=_ambientTraffic.Any(t=>t.MealVisitActive);
        if(parentVisit)_creatorBreakAfter=_visualClock+FamilyTableGap;
        foreach(var (id,actor) in _actors)
        {
            if(_visualBreaks.TryGetValue(id,out var visit))
            {
                if(_departing.Contains(id)||!actor.Visible||_state.OfficeActivities.Any(a=>a.PersonId==id&&a.Kind is not (MangakaSim.OfficeActivityKind.Work or MangakaSim.OfficeActivityKind.Idle)))
                    visit.Remaining=0;
                else if(!actor.Moving)visit.Remaining-=delta*Speed;
                if(visit.Remaining<=0){_visualBreaks.Remove(id);_nextVisualBreak[id]=_visualClock+28+id%11;_targets.Remove(id);changed=true;}
                continue;
            }
            if(!_nextVisualBreak.TryGetValue(id,out var due)){_nextVisualBreak[id]=_visualClock+18+id%9;continue;}
            if(_visualClock<due||_visualBreaks.Count>=2||!actor.Visible||actor.Moving||_departing.Contains(id)||actor.Activity is not ("Work" or "Idle"))continue;
            if(id==_state.ProtagonistPersonId&&(parentVisit||_visualClock<_creatorBreakAfter))continue;
            var seat=Enumerable.Range(0,4).FirstOrDefault(i=>_visualBreaks.Values.All(b=>b.Seat!=i)&&
                !_ambientTraffic.Any(t=>t.ReservesMealSeat&&t.MealSeatIndex==i),-1);
            if(seat<0)continue;
            _visualBreaks[id]=new(){Seat=seat};_targets.Remove(id);changed=true;
        }
        if(changed)SyncActors();
    }
}
