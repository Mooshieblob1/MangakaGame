using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class OfficeView
{
    private readonly HashSet<int> _departing=new();
    internal IReadOnlyDictionary<int,OfficeActor> StaffActors=>_actors;
    internal HelperChan? Companion=>_helper;
    internal Vector3 StaffExit=>new(_plan.Width*.25f+3.92f,ViewingFamilyHome?GenkanFloorHeight:0,_plan.Depth*.25f-.85f);
    // Her seat: the alcove desk, or on the studio island the chair behind her island desk (Q59).
    internal Vector3 HelperDesk=>_plan.Island&&OfficeLayoutRules.ChairAt(FloorPlanDefinition.IslandHelperDesk,0) is var c
        ?new((c.X+1.5f)*.25f,0,(c.Z+1.5f)*.25f):new(-1.5f,0,1.24f);
    private bool _helperLeaving;
    private Vector3? _helperTarget;

    private void RemoveStaffActor(int id)
    {_actors[id].QueueFree();_actors.Remove(id);_targets.Remove(id);_recipes.Remove(id);_departing.Remove(id);_arriving.Remove(id);_visualBreaks.Remove(id);_nextVisualBreak.Remove(id);}
    private void HideStaffActor(OfficeActor actor)
    {actor.Position=StaffExit;actor.Visible=false;actor.Route.Clear();actor.Destination=StaffExit;actor.ClearTrail();}

    private IReadOnlyList<Vector3> FloorWalkingRoute(Vector3 from,Vector3 target)
    {
        if(from.DistanceTo(target)<.01f)return [];
        var points=new List<Vector3>();var w=_plan.Width*.25f;var d=_plan.Depth*.25f;
        var occupied=OfficeLayoutRules.Occupied(_layout.Placements,_inventory);
        var reachable=OfficeLayoutRules.Reachable(_plan,occupied);
        OfficeCell Nearest(Vector3 p)=>reachable.OrderBy(c=>Math.Abs(c.X+.5f-p.X*4)+Math.Abs(c.Z+.5f-p.Z*4)).ThenBy(c=>c.X).ThenBy(c=>c.Z).FirstOrDefault(_plan.Entrance);
        bool Inside(Vector3 p)=>p.X>=0&&p.X<w&&p.Z<d;
        void Path(OfficeCell start,OfficeCell end)
        {foreach(var c in OfficeLayoutRules.Path(_plan,_layout.Placements,_inventory,start,end))points.Add(new((c.X+.5f)*.25f,0,(c.Z+.5f)*.25f));}
        var entryX=(_plan.Entrance.X+.5f)*.25f;var apronZ=d+.25f;var laneX=w+3.2f;
        bool BreakArea(Vector3 p)=>p.X>=w&&p.X<w+2.6f&&p.Z<d;
        void FromBreak(Vector3 p)
        {points.Add(new(p.X,0,BreakCentreZ+1.2f));points.Add(new(w+2.3f,0,BreakCentreZ+1.2f));}
        void ToBreak(Vector3 p)
        {points.Add(new(w+2.3f,0,BreakCentreZ+1.2f));points.Add(new(p.X,0,BreakCentreZ+1.2f));}
        if(Inside(from)&&Inside(target))Path(Nearest(from),Nearest(target));
        else if(from.X<0&&target.X<0)points.Add(new(-.45f,0,from.Z));
        else if(BreakArea(from)&&BreakArea(target)){FromBreak(from);ToBreak(target);}
        else if(from.X>=w+2.6f&&target.X>=w+2.6f)
        {points.Add(new(laneX,0,from.Z));points.Add(new(laneX,0,target.Z));}
        else if(BreakArea(from)&&target.X>=w+2.6f)
        {FromBreak(from);points.Add(new(w+2.3f,0,BreakCentreZ));points.Add(new(laneX,0,BreakCentreZ));points.Add(new(laneX,0,target.Z));}
        else if(from.X>=w+2.6f&&BreakArea(target))
        {points.Add(new(laneX,0,from.Z));points.Add(new(laneX,0,BreakCentreZ));points.Add(new(w+2.3f,0,BreakCentreZ));ToBreak(target);}
        else
        {
            // The front walkway connects the office, companion alcove and break room.
            if(Inside(from)){Path(Nearest(from),_plan.Entrance);points.Add(new(entryX,0,apronZ));}
            else if(from.X<0){points.Add(new(-.45f,0,from.Z));points.Add(new(-.45f,0,apronZ));}
            else if(BreakArea(from)){FromBreak(from);points.Add(new(w+2.3f,0,apronZ));}
            else if(from.Z>=d)points.Add(new(from.X,0,apronZ));
            else
            {
                points.Add(new(laneX,0,from.Z));
                if(ViewingFamilyHome)
                {points.Add(new(laneX,0,BreakCentreZ));points.Add(new(w+2.3f,0,BreakCentreZ));points.Add(new(w+2.3f,0,apronZ));}
                else points.Add(new(laneX,0,apronZ));
            }
            if(Inside(target)){points.Add(new(entryX,0,apronZ));Path(_plan.Entrance,Nearest(target));}
            else if(target.X<0){points.Add(new(-.45f,0,apronZ));points.Add(new(-.45f,0,target.Z));}
            else if(BreakArea(target)){points.Add(new(w+2.3f,0,apronZ));ToBreak(target);}
            else
            {
                if(ViewingFamilyHome)
                {points.Add(new(w+2.3f,0,apronZ));points.Add(new(w+2.3f,0,BreakCentreZ));points.Add(new(laneX,0,BreakCentreZ));}
                else points.Add(new(laneX,0,apronZ));
                points.Add(new(laneX,0,target.Z));
            }
        }
        points.Add(target);return points;
    }
    private void SyncCompanion()
    {
        if(_helper is null||_state is null)return;
        var lead=_actors.GetValueOrDefault(_state.ProtagonistPersonId);
        if(lead is null){_helper.Hide();_helperTarget=null;return;}
        _helperLeaving=_departing.Contains(lead.PersonId);
        var toiletVisit=_staffToiletId==lead.PersonId;
        var atDesk=!_helperLeaving&&(toiletVisit||lead.Activity is "Work" or "Idle" or "Promotion" or "Recovery");
        // A WC visit is private: Helper stays at her own desk for the entire visit.
        var target=_helperLeaving?StaffExit:atDesk?HelperDesk:CompanionBeside(lead.Destination);
        _helper.AtDesk=atDesk;
        _helper.SeatHeight=atDesk?OfficeArt.DeskSeatHeight:
            !_helperLeaving&&lead.Activity=="Break"&&target==HelperBreakSeat?OfficeArt.BreakSeatHeight:0;
        if(_discontinuity||Editing)
        {
            _helper.Visible=lead.Visible;_helper.Position=_helperLeaving?StaffExit:target;
            _helper.SetRoute([],target);_helperTarget=target;return;
        }
        if(!_helper.Visible&&!_helperLeaving){if(!lead.Visible||lead.Position.DistanceTo(StaffExit)<.6f)return;_helper.Position=StaffExit;_helper.Show();_helperTarget=null;}
        if(!_helper.Visible)return;
        if(_helperTarget is null||_helperTarget.Value.DistanceTo(target)>.01f)
        {_helper.SetRoute(WalkingRoute(_helper.Position,target),target);_helperTarget=target;}
    }
    private Vector3 CompanionBeside(Vector3 target)
    {
        if(target.X>=_plan.Width*.25f)return HelperBreakSeat;
        var reachable=OfficeLayoutRules.Reachable(_plan,OfficeLayoutRules.Occupied(_layout.Placements,_inventory));
        return reachable.Select(c=>new Vector3((c.X+.5f)*.25f,0,(c.Z+.5f)*.25f))
            .OrderBy(p=>Math.Abs(p.DistanceTo(target)-.65f)).ThenBy(p=>p.X).ThenBy(p=>p.Z).FirstOrDefault(target);
    }
    private void AnimateCompanion(double delta)
    {
        if(_helper is null||!_helper.Visible)return;
        // Keep a short following gap so she never exits ahead of the protagonist.
        var lead=_state is null?null:_actors.GetValueOrDefault(_state.ProtagonistPersonId);
        var elapsed=delta;
        if(_helperLeaving&&lead is {Visible:true}&&Speed>0)
        {
            var path=_helper.Route.Prepend(_helper.Position).ToArray();var length=0f;
            for(var i=1;i<path.Length;i++)length+=path[i-1].DistanceTo(path[i]);
            elapsed=Math.Min(delta,Math.Max(0,length-.7f)/(Speed*7));
        }
        _helper.Animate(elapsed,Editing?0:Speed);
        if(!_helper.Moving&&!_helper.AtDesk)_helper.Rotation=new(0,Mathf.Pi,0);
        if(_helperLeaving&&!_helper.Moving&&lead is not {Visible:true})
        {_helper.Hide();_helper.SetRoute([],StaffExit);}
    }
}
