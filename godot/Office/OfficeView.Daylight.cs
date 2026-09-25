using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private DirectionalLight3D _sunlight=null!;
    private Godot.Environment _skyEnvironment=null!;
    private readonly List<StandardMaterial3D> _windowSkies=new(),_lampMaterials=new();
    private readonly List<MeshInstance3D> _windowOrbs=new();
    private readonly List<OmniLight3D> _roomLamps=new();
    public double HourFraction { get; set; }
    public bool ClosedForNight { get; set; }
    public bool EndingDay { get; set; }
    internal bool DeparturesPending=>_departing.Any(id=>_actors.TryGetValue(id,out var a)&&a.Visible)||_helperLeaving&&_helper is {Visible:true}||_ambientTraffic.Any(t=>t.NeedsNightReturn)||_staffToiletId is not null;
    internal float RoomLightLevel { get; private set; }=1;
    internal double LightingHour=>((_state?.Clock.Now.TimeOfDay.TotalHours??8)+Math.Clamp(HourFraction,0,1))%24;

    private void BuildDaylight()
    {
        _sunlight=new DirectionalLight3D{ShadowEnabled=true};_world.AddChild(_sunlight);
        _skyEnvironment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,
            AmbientLightSource=Godot.Environment.AmbientSource.Color,TonemapMode=Godot.Environment.ToneMapper.Filmic};
        _world.AddChild(new WorldEnvironment{Environment=_skyEnvironment});
    }
    private void BuildSkyWindow(float x)
    {
        var glass=OfficeArt.Box(_room,new(x,1.65f,.09f),new(1.25f,.9f,.045f),Colors.White);
        var material=(StandardMaterial3D)glass.MaterialOverride;
        material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;_windowSkies.Add(material);
        var orb=OfficeArt.Sphere(_room,new(x,1.8f,.122f),new(.18f,.18f,.012f),Colors.White);
        ((StandardMaterial3D)orb.MaterialOverride).ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
        orb.SetMeta("window_x",x);_windowOrbs.Add(orb);
    }
    private void BuildRoomLamps(float width,float depth)
    {
        // High wall fittings remain visible in the dollhouse cutaway.
        foreach(var x in new[]{width*.3f,width*.75f})
        {
            OfficeArt.Box(_room,new(x,2.23f,.14f),new(.58f,.14f,.12f),new(.35f,.34f,.30f));
            var diffuser=OfficeArt.Box(_room,new(x,2.22f,.215f),new(.48f,.075f,.04f),Colors.White);
            var material=(StandardMaterial3D)diffuser.MaterialOverride;material.EmissionEnabled=true;_lampMaterials.Add(material);
            var lamp=new OmniLight3D{Position=new(x,2.05f,Math.Min(depth*.45f,1.8f)),LightColor=new(1,.84f,.62f),OmniRange=Math.Max(width,depth)+2,LightEnergy=.45f};
            _room.AddChild(lamp);_roomLamps.Add(lamp);
        }
        UpdateDaylight(0);
    }
    private void UpdateDaylight(double delta)
    {
        var hour=(float)LightingHour;
        float Ramp(float from,float to)=>Mathf.SmoothStep(0,1,Mathf.Clamp((hour-from)/(to-from),0,1));
        var daylight=Ramp(5,8)*(1-Ramp(16.5f,20));
        var warm=Mathf.Max(0,1-Mathf.Abs(hour-6.5f)/1.5f)+Mathf.Max(0,1-Mathf.Abs(hour-18)/2);
        var sky=new Color(.055f,.075f,.17f).Lerp(new(.52f,.75f,.88f),daylight).Lerp(new(.98f,.48f,.26f),warm*.7f);
        _skyEnvironment.BackgroundColor=new Color(.04f,.055f,.10f).Lerp(new(.20f,.27f,.30f),daylight);
        _skyEnvironment.AmbientLightColor=new Color(.39f,.48f,.72f).Lerp(new(.86f,.90f,.94f),daylight);
        _skyEnvironment.AmbientLightEnergy=.22f+daylight*.43f;
        _sunlight.LightColor=new Color(.48f,.60f,.92f).Lerp(Colors.White,daylight).Lerp(new(1,.58f,.30f),warm*.65f);
        _sunlight.LightEnergy=.12f+daylight*.88f;
        _sunlight.RotationDegrees=new(-20-daylight*40,-70+hour*4,0);
        var occupied=_actors.Values.Any(a=>a.Visible)||_helper is {Visible:true};
        var lit=!ClosedForNight&&(occupied||hour is >=7 and <19);
        var target=lit?1f:0f;
        RoomLightLevel=delta<=0?target:Mathf.MoveToward(RoomLightLevel,target,(float)delta*4);
        foreach(var lamp in _roomLamps)lamp.LightEnergy=RoomLightLevel*(.25f+(1-daylight)*.50f);
        foreach(var material in _lampMaterials)
        {
            material.AlbedoColor=new Color(.23f,.24f,.28f).Lerp(new(1,.93f,.76f),RoomLightLevel);
            material.Emission=new Color(1,.83f,.58f)*RoomLightLevel;material.EmissionEnergyMultiplier=RoomLightLevel;
        }
        foreach(var material in _windowSkies)material.AlbedoColor=sky;
        var sun=hour is >=6 and <19;
        var progress=sun?(hour-6)/13:(hour<6?hour+5:hour-19)/11;
        foreach(var orb in _windowOrbs)
        {
            orb.Position=new((float)orb.GetMeta("window_x")-.46f+progress*.92f,1.43f+Mathf.Sin(progress*Mathf.Pi)*.49f,.122f);
            ((StandardMaterial3D)orb.MaterialOverride).AlbedoColor=sun?new(1,.88f,.46f):new(.83f,.90f,1);
            orb.Scale=new(sun?.18f:.13f,sun?.18f:.13f,.012f);
        }
    }
}
