using System;
using Godot;

namespace MangakaGame;

public sealed partial class OfficeAmbientTraffic
{
    public enum FamilyPlace { Upstairs, Toilet, Outside, Table }
    public FamilyPlace Residence { get; private set; }=FamilyPlace.Upstairs;
    public FamilyPlace VisitDestination { get; private set; }=FamilyPlace.Upstairs;
    public bool IsOutside { get; private set; }
    public int HouseExits { get; private set; }
    public int HouseEntries { get; private set; }
    public int CompletedToiletVisits { get; private set; }
    public bool ToiletReserved=>_family&&(Residence==FamilyPlace.Toilet||Travelling&&VisitDestination==FamilyPlace.Toilet);
    public bool NeedsNightReturn=>Travelling||_family&&Residence==FamilyPlace.Toilet;
    private Vector3 _toiletDoor,_toiletSeat;
    private OfficeToiletVisit? _toiletVisit;
    public bool UsingToilet=>_toiletVisit is not null;
    private Func<bool>? _toiletAvailable;
    private int _nextFamilyVisit;
    internal sealed record Continuity(bool Outside,int Exits,int Entries,int NextVisit,double Wait);
    internal Continuity CaptureContinuity()=>new(IsOutside,HouseExits,HouseEntries,_nextFamilyVisit,_wait);
    internal void RestoreContinuity(Continuity saved)
    {
        HouseExits=saved.Exits;HouseEntries=saved.Entries;_nextFamilyVisit=saved.NextVisit;
        if(!saved.Outside)return;
        IsOutside=true;Residence=VisitDestination=FamilyPlace.Outside;_wait=saved.Wait;
        Actor.Position=SecondDoor;Actor.Destination=SecondDoor;
    }

    public void ConfigureFamily(Vector3 toiletDoor,Vector3 toiletSeat,Func<bool> toiletAvailable)
    {_toiletDoor=toiletDoor;_toiletSeat=toiletSeat;_toiletAvailable=toiletAvailable;}

    private void ResetFamily()
    {
        Residence=VisitDestination=FamilyPlace.Upstairs;IsOutside=false;
        HouseEntries=HouseExits=0;_nextFamilyVisit=_index;
        Actor.Position=FirstDoor;Actor.Destination=FirstDoor;
    }
    private Vector3 FamilyEndpoint(FamilyPlace place)=>place switch
    {FamilyPlace.Upstairs=>FirstDoor,FamilyPlace.Outside=>SecondDoor,FamilyPlace.Toilet=>_toiletDoor,_=>_mealSeat};

    private bool BeginFamilyWalk(FamilyPlace destination,Func<Vector3,bool>? doorReady)
    {
        var source=FamilyEndpoint(Residence);
        if(doorReady?.Invoke(source)==false)return false;
        // Outside residents can only reappear at the entrance, then return indoors.
        if(IsOutside)
        {
            if(Residence!=FamilyPlace.Outside||destination==FamilyPlace.Outside)return false;
            IsOutside=false;HouseEntries++;
        }
        VisitDestination=destination;Actor.Position=source;Actor.SeatHeight=0;Actor.SeatedFacing=null;
        MealRouteTo(FamilyEndpoint(destination));Actor.ClearTrail();Actor.Visible=true;Travelling=true;
        return true;
    }

    private void UpdateFamily(double delta,double speed,int effect,bool active,Func<Vector3,bool>? doorReady)
    {
        if(speed<=0){Actor.Animate(0,0,effect);return;}
        if(_mealRoute is null)return;
        if(_toiletVisit is not null)
        {
            if(_toiletVisit.Update(delta,speed,!active,doorReady))
            {_toiletVisit=null;BeginFamilyWalk(FamilyPlace.Upstairs,doorReady);}
            return;
        }
        if(!Travelling)
        {
            // A night transition cannot silently turn an outside resident into an upstairs resident.
            if(!active&&Residence!=FamilyPlace.Toilet)return;
            _wait-=delta*speed;
            if(_wait>0&&active)return;
            if(Residence!=FamilyPlace.Upstairs)
            {BeginFamilyWalk(FamilyPlace.Upstairs,doorReady);return;}
            var destination=(_nextFamilyVisit%4) switch
            {0=>FamilyPlace.Toilet,2=>FamilyPlace.Outside,_=>FamilyPlace.Table};
            if(destination==FamilyPlace.Toilet&&_toiletAvailable?.Invoke()!=true)return;
            if(destination==FamilyPlace.Table&&(_mealSeatAvailable?.Invoke()!=true||_canStartMeal?.Invoke()==false))return;
            if(BeginFamilyWalk(destination,doorReady))
            {_nextFamilyVisit++;if(destination==FamilyPlace.Table)_mealPhase=1;}
            return;
        }
        if(_mealPhase is 1 or 2&&(!active||_mealSeatAvailable?.Invoke()!=true))
        {_mealPhase=3;VisitDestination=FamilyPlace.Upstairs;MealRouteTo(FirstDoor);}
        if(_mealPhase==2)
        {
            Actor.Rotation=new(0,_mealAngle,0);Actor.Animate(delta,speed,effect);
            _mealRemaining-=delta*speed;
            if(_mealRemaining<=0)
            {CompletedMeals++;_mealPhase=3;VisitDestination=FamilyPlace.Upstairs;MealRouteTo(FirstDoor);}
            return;
        }
        var endpoint=FamilyEndpoint(VisitDestination);
        // Stop short of a closed destination door, even on a long/fast frame.
        var elapsed=delta;
        if(doorReady?.Invoke(endpoint)==false)
        {
            var length=0f;var previous=Actor.Position;
            foreach(var point in Actor.Route){length+=previous.DistanceTo(point);previous=point;}
            length+=previous.DistanceTo(endpoint);
            elapsed=Math.Min(delta,Math.Max(0,length-.65f)/(speed*7));
        }
        Actor.Animate(elapsed,speed,effect);
        if(Actor.Moving)return;
        if(VisitDestination==FamilyPlace.Table)
        {
            Residence=FamilyPlace.Table;_mealPhase=2;_mealRemaining=24+_index*6;
            Actor.SeatHeight=OfficeArt.BreakSeatHeight;Actor.Activity="Eating";
            Actor.Rotation=new(0,_mealAngle,0);Actor.ClearTrail();Actor.SetMealVisible(true);
            Actor.Animate(.000001,speed,effect);return;
        }
        Residence=VisitDestination;
        if(Residence==FamilyPlace.Outside){IsOutside=true;HouseExits++;}
        if(Residence==FamilyPlace.Toilet)
        {
            CompletedToiletVisits++;CompletedCrossings++;Travelling=false;ClearMeal();
            _toiletVisit=new(Actor,_toiletDoor,_toiletSeat,18+_index*5);return;
        }
        Hide();CompletedCrossings++;
        _wait=Residence switch
        {FamilyPlace.Toilet=>18+_index*5,FamilyPlace.Outside=>70+_index*17,_=>35+_index*17};
    }
}
