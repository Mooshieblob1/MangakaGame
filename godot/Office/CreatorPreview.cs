using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>Isolated dressing-room view of the same chibi model used in the office.</summary>
public partial class CreatorPreview : SubViewportContainer
{
    private SubViewport _viewport=null!;
    private OfficeActor _actor=null!;
    private AppearanceRecipe _recipe=new();
    private bool _dragging;
    private float _turn;
    public AppearanceRecipe Recipe
    {
        get=>_recipe;
        set{_recipe=value;if(_actor is not null)Rebuild();}
    }
    public override void _Ready()
    {
        // Like the office: not stretched, rendered at the window's real pixels and shown through a picture, so the preview
        // stays sharp at large interface sizes (display settings, spec 2026-10-04).
        Stretch=false;CustomMinimumSize=new(280,280);SizeFlagsHorizontal=SizeFlags.ExpandFill;
        MouseDefaultCursorShape=CursorShape.Drag;TooltipText="Drag left or right to turn your mangaka.";
        var holder=new Node{Name="ViewportHolder"};AddChild(holder);
        _viewport=new SubViewport{OwnWorld3D=true,Size=new(320,280),Msaa3D=Viewport.Msaa.Msaa2X};holder.AddChild(_viewport);
        var picture=new TextureRect{Texture=_viewport.GetTexture(),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,MouseFilter=MouseFilterEnum.Ignore};
        AddChild(picture);picture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized+=()=>Callable.From(FitViewport).CallDeferred();Callable.From(FitViewport).CallDeferred();
        var world=new Node3D();_viewport.AddChild(world);
        world.AddChild(new WorldEnvironment{Environment=new Godot.Environment
        {
            BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new(.10f,.14f,.16f),
            AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new(.88f,.91f,.95f),AmbientLightEnergy=.8f,
            TonemapMode=Godot.Environment.ToneMapper.Filmic
        }});
        world.AddChild(new DirectionalLight3D{RotationDegrees=new(-45,-30,0),LightEnergy=1,ShadowEnabled=true});
        OfficeArt.Box(world,new(0,-.045f,0),new(.9f,.09f,.9f),new(.22f,.32f,.34f));
        _actor=new OfficeActor{SeatHeight=0};world.AddChild(_actor);Rebuild();
        var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=1.65f,Current=true,Position=new(1.4f,1.25f,-3.5f)};
        world.AddChild(camera);camera.LookAt(new(0,.60f,0));
        VisibilityChanged+=SyncVisibility;SyncVisibility();
    }
    private void FitViewport()
    {
        if(!IsInsideTree())return;var scale=GetWindow().ContentScaleFactor;
        var size=new Vector2I(System.Math.Max(2,(int)System.Math.Round(Size.X*scale)),System.Math.Max(2,(int)System.Math.Round(Size.Y*scale)));
        if(_viewport.Size!=size)_viewport.Size=size;
    }
    private void Rebuild()
    {
        _actor.Build(_recipe);
        _actor.Animate(0,1,0);_actor.Rotation=new(0,_turn,0);
    }
    private void SyncVisibility()
    {
        _dragging=false;
        _viewport.RenderTargetUpdateMode=IsVisibleInTree()?SubViewport.UpdateMode.Always:SubViewport.UpdateMode.Disabled;
    }
    public void Turn(float degrees){_turn+=Mathf.DegToRad(degrees);_actor.Rotation=new(0,_turn,0);}
    public void FaceFront(){_turn=0;_actor.Rotation=Vector3.Zero;}
    public override void _GuiInput(InputEvent input)
    {
        if(input is InputEventMouseButton{ButtonIndex:MouseButton.Left} button){_dragging=button.Pressed;AcceptEvent();}
        if(input is InputEventMouseMotion motion&&_dragging){_turn+=motion.Relative.X*.012f;_actor.Rotation=new(0,_turn,0);AcceptEvent();}
    }
}
