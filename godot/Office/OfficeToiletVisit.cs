using System;
using Godot;

namespace MangakaGame;

/// <summary>A visible, cosmetic trip from the doorway to the WC seat and back.</summary>
internal sealed class OfficeToiletVisit
{
    private enum Phase { Entering, Seated, Leaving }
    private Phase _phase;
    private readonly Vector3 _door;
    private double _remaining;
    public OfficeActor Actor { get; }
    public OfficeToiletVisit(OfficeActor actor,Vector3 door,Vector3 seat,double duration)
    {
        Actor=actor;_door=door;_remaining=duration;
        actor.PrivacyObscured=true;actor.Show();actor.ClearTrail();actor.SetMealVisible(false);
        actor.Activity="Toilet";actor.SeatHeight=.56f;actor.SeatedFacing=Mathf.Pi/2;
        actor.Route.Clear();actor.Destination=seat;
    }
    public bool Update(double delta,double speed,bool closing,Func<Vector3,bool>? doorReady)
    {
        if(speed<=0){Actor.Animate(0,0,0);return false;}
        if(_phase==Phase.Seated)
        {
            _remaining-=delta*speed;
            if(_remaining<=0||closing)
            {
                _phase=Phase.Leaving;Actor.SeatHeight=0;Actor.SeatedFacing=null;
                Actor.Route.Clear();Actor.Destination=_door;
            }
        }
        var elapsed=delta;
        if(_phase==Phase.Leaving&&doorReady?.Invoke(_door)==false)
            elapsed=Math.Min(delta,Math.Max(0,Actor.Position.DistanceTo(_door)-.45f)/(speed*7));
        Actor.Animate(elapsed,speed,0);
        if(Actor.Moving)return false;
        if(_phase==Phase.Entering){_phase=Phase.Seated;return false;}
        if(_phase!=Phase.Leaving)return false;
        Cancel();return true;
    }
    public void Cancel()
    {
        Actor.PrivacyObscured=false;Actor.SeatHeight=0;Actor.SeatedFacing=null;
        Actor.Activity="Idle";Actor.Route.Clear();Actor.Destination=Actor.Position;Actor.ClearTrail();
    }
}
