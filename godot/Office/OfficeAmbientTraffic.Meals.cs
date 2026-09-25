using System;
using System.Collections.Generic;
using Godot;

namespace MangakaGame;

public sealed partial class OfficeAmbientTraffic
{
    private Vector3 _mealSeat;
    private float _mealAngle;
    private Func<Vector3,Vector3,IEnumerable<Vector3>>? _mealRoute;
    private Func<bool>? _mealSeatAvailable;
    private Func<bool>? _canStartMeal;
    private int _mealPhase; // 0 crossing, 1 approaching, 2 eating, 3 returning to a door
    private double _mealRemaining;
    public int MealSeatIndex { get; private set; }=-1;
    public bool ReservesMealSeat=>_mealPhase is 1 or 2;
    public bool Eating=>_mealPhase==2;
    public bool MealVisitActive=>_mealPhase!=0;
    public int CompletedMeals { get; private set; }

    public void ConfigureMeals(int seatIndex,Vector3 seat,float angle,
        Func<Vector3,Vector3,IEnumerable<Vector3>> route,Func<bool> available,Func<bool>? canStart=null)
    {MealSeatIndex=seatIndex;_mealSeat=seat;_mealAngle=angle;_mealRoute=route;_mealSeatAvailable=available;_canStartMeal=canStart;}

    private void ClearMeal()
    {_mealPhase=0;_mealRemaining=0;Actor.SetMealVisible(false);Actor.Activity="Idle";}

    private void MealRouteTo(Vector3 target)
    {
        Actor.Route.Clear();
        foreach(var point in _mealRoute!(Actor.Position,target))Actor.Route.Enqueue(point);
        Actor.Destination=target;Actor.Activity="Idle";Actor.SetMealVisible(false);
    }

}

