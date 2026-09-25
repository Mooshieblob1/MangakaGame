using System;
using Godot;

namespace MangakaGame;

/// <summary>Visual-only door-to-door circulation. Waiting happens out of sight.</summary>
public sealed partial class OfficeAmbientTraffic
{
    public OfficeActor Actor { get; }
    public Vector3 FirstDoor { get; }
    public Vector3 SecondDoor { get; }
    public float LaneX { get; }
    public int CompletedCrossings { get; private set; }
    public bool Travelling { get; private set; }
    private readonly int _index;
    private bool _forward;
    private double _wait;
    private readonly bool _family;
    private readonly Func<Vector3,Vector3,System.Collections.Generic.IEnumerable<Vector3>>? _circulationRoute;

    public OfficeAmbientTraffic(OfficeActor actor,Vector3 firstDoor,Vector3 secondDoor,float laneX,int index,bool family=false,
        Func<Vector3,Vector3,System.Collections.Generic.IEnumerable<Vector3>>? circulationRoute=null)
    {
        Actor=actor;FirstDoor=firstDoor;SecondDoor=secondDoor;LaneX=laneX;_index=index;_family=family;_circulationRoute=circulationRoute;Reset();
    }
    public void Reset()
    {
        Hide();_forward=_index%2==0;_wait=_family?12+_index*18:_index*.6;
        if(_family)ResetFamily();
    }
    private void Hide()
    {
        _toiletVisit?.Cancel();_toiletVisit=null;ClearMeal();
        Actor.Visible=false;Actor.Route.Clear();Actor.Destination=Actor.Position;
        Actor.ClearTrail();Travelling=false;
    }
    public void Update(double delta,double speed,int effect,bool active,Func<Vector3,bool>? doorReady=null)
    {
        if(_family){UpdateFamily(delta,speed,effect,active,doorReady);return;}
        // Finish a crossing through its doorway before retiring for the night.
        if(!active&&!Travelling){Reset();return;}
        if(speed<=0){Actor.Animate(0,0,effect);return;}
        if(!Travelling)
        {
            _wait-=delta*speed;if(_wait>0)return;
            var entry=_forward?FirstDoor:SecondDoor;var exit=_forward?SecondDoor:FirstDoor;
            if(doorReady is not null)
            {
                var enter=doorReady(entry);var leave=doorReady(exit);
                if(!enter||!leave)return;
            }
            Actor.Position=entry;Actor.Destination=exit;Actor.Route.Clear();
            if(_circulationRoute is not null)
            {foreach(var point in _circulationRoute(entry,exit))Actor.Route.Enqueue(point);}
            else
            {
                Actor.Route.Enqueue(new(LaneX,0,entry.Z));
                Actor.Route.Enqueue(new(LaneX,0,exit.Z));
                Actor.Route.Enqueue(exit);
            }
            Actor.Rotation=new(0,Mathf.Pi/2,0);Actor.Activity="Idle";
            Actor.ClearTrail();Actor.Visible=true;Travelling=true;
            return; // Present the doorway emergence before advancing into the corridor.
        }
        doorReady?.Invoke(_forward?SecondDoor:FirstDoor);
        Actor.Animate(delta,speed,effect);
        if(!Actor.Moving)
        {
            // Same update as arrival: no visible idle frame or leftover exposure at the exit.
            Hide();CompletedCrossings++;_forward=!_forward;_wait=_family?35+_index*17:3+_index*.55;
        }
    }
}
