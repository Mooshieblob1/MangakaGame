using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private readonly List<OfficeDoor> _doors=new();
    private readonly HashSet<int> _arriving=new();
    internal IReadOnlyList<OfficeDoor> Doors=>_doors;
    private bool DoorReady(Vector3 threshold)
    {
        var door=_doors.FirstOrDefault(d=>d.Position.DistanceTo(threshold)<.5f);
        if(door is null)return true;
        door.RequestPassage();return door.ClearToPass;
    }
    private void UpdateDoors(double delta)
    {
        var speed=Editing?0:Speed;
        if(_helperLeaving&&_helper is {Visible:true}||_arriving.Count>0||_departing.Any(id=>_actors.TryGetValue(id,out var a)&&a.Visible))DoorReady(StaffExit);
        foreach(var door in _doors)
        {
            if(ToiletInUse&&door.Position.DistanceTo(ToiletDoor)<.5f){door.Animate(delta,speed);continue;}
            if(_actors.Values.Concat(_ambient).Any(a=>a.Visible&&a.Position.DistanceTo(door.Position)<1.25f)||
                _helper is {Visible:true}&&_helper.Position.DistanceTo(door.Position)<1.25f)door.RequestPassage();
            door.Animate(delta,speed);
        }
        if(speed<=0)return;
        foreach(var id in _arriving.ToArray())
        {
            if(!_actors.TryGetValue(id,out var actor)||_departing.Contains(id)){_arriving.Remove(id);continue;}
            if(DoorReady(StaffExit)){actor.Show();_arriving.Remove(id);SyncCompanion();}
        }
        if(_helper is {Visible:false}&&!_helperLeaving&&_state is not null&&
            _actors.TryGetValue(_state.ProtagonistPersonId,out var lead)&&lead.Visible&&lead.Position.DistanceTo(StaffExit)>.6f)SyncCompanion();
    }
}
