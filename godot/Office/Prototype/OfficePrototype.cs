using System;
using Godot;

namespace MangakaGame;

public partial class OfficePrototype : Node3D
{
    private OfficeActor _actor=null!;
    private double _elapsed;
    private int _captures;
    public override void _Ready()
    {
        OfficeArt.Box(this,new(4,-.1f,3),new(9,.2f,7),new(.73f,.69f,.59f));
        OfficeArt.Box(this,new(4,1.1f,3),new(.2f,2.2f,3),new(.72f,.76f,.70f));
        var desk=OfficeArt.Furniture("desk");desk.Position=new(1,0,1);AddChild(desk);
        _actor=new OfficeActor{Position=new(1,0,4),Destination=new(7,0,4)};AddChild(_actor);_actor.Build(0,0,0);_actor.ClearTrail();
        var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=11,Position=new(11,10,12),Current=true};AddChild(camera);camera.LookAt(new(4,0,3));
        var light=new DirectionalLight3D{RotationDegrees=new(-55,-25,0),ShadowEnabled=true};AddChild(light);
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new(.17f,.21f,.23f),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.6f}});
    }
    public override void _Process(double delta)
    {
        _elapsed+=delta;var speed=_elapsed<3?1:8;
        if(!_actor.Moving)_actor.Destination=_actor.Position.X>4?new(1,0,4):new(7,0,4);
        _actor.Animate(delta,speed,2);
        if(DisplayServer.GetName()!="headless" && ((_captures==0 && _elapsed>1.5)||(_captures==1 && _elapsed>4.25)))
        {
            System.IO.Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://../TestResults"));
            using var captured=GetViewport().GetTexture().GetImage();
            ScreenshotCapture.SaveAvif(captured,ProjectSettings.GlobalizePath($"res://../TestResults/office-prototype-{_captures++}.avif"));
        }
        if(_elapsed>6){GD.Print("Office motion prototype: accelerated animation and pooled trails ran.");GetTree().Quit();}
    }
}
