using System;
using Godot;

namespace MangakaGame;

public partial class OfficeActor : Node3D
{
    private Node3D _leftKnee=null!, _rightKnee=null!;
    private Node3D _body = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private OfficeWorldLabel _attention=null!;
    public bool NeedsAttention { get; set; }
    private float _phase;
    public int PersonId { get; set; }
    public bool Ambient { get; set; }
    public float SeatHeight { get; set; }=OfficeArt.DeskSeatHeight;
    public float? SeatedFacing { get; set; }
    private const float StandingBodyOffset=-.14f, PelvisBottom=.38f;
    public string Activity { get; set; } = "Idle";
    public Vector3 Destination { get; set; }
    public System.Collections.Generic.Queue<Vector3> Route { get; } = new();
    public bool Selected { get; set; }
    internal bool PrivacyObscured { get; set; }
    public int TrailCount { get; private set; }
    public bool Moving => Route.Count > 0 || Position.DistanceTo(Destination) > .04f;
    private MeshInstance3D _selection = null!;
    public string ResidentRole { get; private set; } = "";
    private CharacterModel? _characterModel;
    public bool UsingImportedModel=>_characterModel is not null;
    public string ImportedClip=>_characterModel?.CurrentClip??"";
    public void SetDisplayName(string name)
    {
        // Model rebuilds replace actor children; recreate the tag on the next binding.
        var label=GetNodeOrNull<OfficeWorldLabel>("NameTag");
        if(label is null)
        {label=OfficeArt.WorldLabel(this,name,new(0,1.78f,0),20);label.Name="NameTag";}
        else label.Text=name;
    }
    private Node3D? _mealSetting;
    public void SetMealVisible(bool visible)
    {
        if(visible&&_mealSetting is null)
        {
            _mealSetting=new Node3D();AddChild(_mealSetting);
            // The setting sits on the table in front of either inward-facing chair.
            OfficeArt.Sphere(_mealSetting,new(0,.77f,-.46f),new(.23f,.10f,.23f),new("e7e1d5"));
            OfficeArt.Sphere(_mealSetting,new(0,.806f,-.46f),new(.18f,.025f,.18f),new("f4eee0"));
            OfficeArt.Sphere(_mealSetting,new(.035f,.824f,-.45f),new(.045f,.028f,.045f),new("97594e"));
        }
        if(_mealSetting is not null)_mealSetting.Visible=visible;
    }
    public void BuildParent(bool mother)
    {
        var parent=mother?"mom":"dad";
        var visual=CharacterModel.LoadModel($"res://Assets/Parents/{parent}/{parent}-animated.glb");
        if(visual is not null)
        {
            foreach(var child in GetChildren()){RemoveChild(child);child.QueueFree();}
            UsingModularKit=false;_characterModel=visual;_body=visual;AddChild(visual);
            ResidentRole=mother?"Mother":"Father";Name=ResidentRole;
            SetDisplayName(mother?"Mom":"Dad");
            _selection=OfficeArt.Box(this,new(0,.025f,0),new(.6f,.01f,.6f),new(.85f,.70f,.30f,.8f));_selection.Hide();
            _attention=OfficeArt.WorldLabel(this,"!",new(0,2.04f,0),24);_attention.Hide();
            BuildExposure(new("ebba91"),mother?new("e1d4bc"):new("9b986f"),new("715244"));
            visual.Tick(0,0,"Idle",SeatHeight);return;
        }
        BuildLegacy(0,3,mother?4:2,mother?2:0,true,mother?0:2);
        ResidentRole=mother?"Mother":"Father";Name=ResidentRole;
        SetDisplayName(mother?"Mom":"Dad");
        var silver=new Color(.76f,.77f,.74f);
        // Silver temples, eyebrows and crow's feet read as older adults at chibi scale.
        foreach(var x in new[]{-.235f,.235f})
        {
            OfficeArt.Sphere(_body,new(x,1.12f,.025f),new(.09f,.21f,.32f),silver);
            OfficeArt.Box(_body,new(x*.48f,1.11f,-.226f),new(.09f,.025f,.02f),silver);
            OfficeArt.Box(_body,new(x*.83f,.975f,-.218f),new(.045f,.012f,.018f),new(.65f,.49f,.39f));
        }
        if(mother)
        {
            OfficeArt.Sphere(_body,new(0,1.17f,.29f),new(.28f,.25f,.24f),silver);
            OfficeArt.Box(_body,new(0,.56f,-.14f),new(.27f,.30f,.025f),new(.82f,.75f,.65f)); // apron
            OfficeArt.Box(_body,new(0,.52f,-.16f),new(.16f,.08f,.02f),new(.68f,.60f,.52f));
        }
        else
        {
            OfficeArt.Box(_body,new(0,.70f,-.14f),new(.14f,.22f,.025f),new(.83f,.81f,.72f)); // shirt beneath cardigan
            foreach(var y in new[]{.58f,.65f,.72f})OfficeArt.Sphere(_body,new(.045f,y,-.16f),new(.025f,.025f,.015f),new(.27f,.28f,.23f));
        }
    }
    private void BuildLegacy(int skin, int hair, int outfit, int style = 0, bool glasses = false, int build = 0)
    {
        UsingModularKit=false;_characterModel=null;
        foreach(var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        var skins = new[]{new Color(.92f,.73f,.57f),new(.78f,.56f,.40f),new(.59f,.38f,.27f),new(.36f,.23f,.18f)};
        var clothes = new[]{new Color(.30f,.45f,.51f),new(.51f,.32f,.35f),new(.36f,.45f,.32f),new(.64f,.52f,.34f),new(.43f,.36f,.53f),new(.6f,.63f,.61f)};
        var hairColors = new[]{new Color(.12f,.10f,.09f),new(.25f,.16f,.10f),new(.44f,.32f,.22f),new(.48f,.49f,.48f)};
        var skinColor=skins[Math.Abs(skin)%skins.Length]; var cloth=clothes[Math.Abs(outfit)%clothes.Length];
        _body=new Node3D();AddChild(_body);
        // Short, chunky legs and an oversized head, shared by staff and passers-by.
        OfficeArt.Sphere(_body,new(0,1.03f,0),new(.52f,.54f,.48f),skinColor);
        OfficeArt.Sphere(_body,new(0,1.20f,.025f),new(.55f,.21f+style*.015f,.50f),hairColors[Math.Abs(hair)%4]);
        if(style%3==1) OfficeArt.Box(_body,new(0,1.03f,.20f),new(.49f,.38f,.13f),hairColors[Math.Abs(hair)%4]);
        if(style%3==2) OfficeArt.Sphere(_body,new(0,1.05f,.28f),new(.24f,.31f,.25f),hairColors[Math.Abs(hair)%4]);
        foreach(var x in new[]{-.105f,.105f})
        {
            OfficeArt.Sphere(_body,new(x,1.035f,-.225f),new(.05f,.065f,.025f),new(.12f,.11f,.1f));
            if(glasses) OfficeArt.Box(_body,new(x,1.035f,-.245f),new(.17f,.105f,.014f),new(.2f,.24f,.25f,.7f));
        }
        OfficeArt.Box(_body,new(0,.66f,0),new(.36f+build*.025f,.32f,.25f),cloth);
        OfficeArt.Box(_body,new(0,.46f,0),new(.32f,.16f,.25f),new(.20f,.24f,.27f));
        _leftArm=Limb(_body,new(-.23f,.80f,0),cloth,skinColor);
        _rightArm=Limb(_body,new(.23f,.80f,0),cloth,skinColor);
        _leftLeg=Leg(_body,new(-.10f,.46f,0),out _leftKnee); _rightLeg=Leg(_body,new(.10f,.46f,0),out _rightKnee);
        _body.Position=new(0,StandingBodyOffset,0);
        _selection=OfficeArt.Box(this,new(0,.025f,0),new(.6f,.01f,.6f),new(.85f,.70f,.30f,.8f));
        _selection.Visible=false;
        _attention=OfficeArt.WorldLabel(this,"!",new(0,2.04f,0),24,new(.96f,.67f,.26f));_attention.ScreenOffset=new(0,-18);
        BuildExposure(skinColor,cloth,hairColors[Math.Abs(hair)%4]);
    }
    private static Node3D Limb(Node3D parent,Vector3 pivot,Color cloth,Color skin)
    { var n=new Node3D{Position=pivot};parent.AddChild(n);OfficeArt.Box(n,new(0,-.085f,0),new(.14f,.17f,.16f),cloth);OfficeArt.Box(n,new(0,-.23f,-.02f),new(.11f,.14f,.12f),skin);return n; }
    private static Node3D Leg(Node3D parent,Vector3 pivot,out Node3D knee)
    {
        var leg=new Node3D{Position=pivot};parent.AddChild(leg);
        OfficeArt.Box(leg,new(0,-.06f,0),new(.16f,.12f,.16f),new(.20f,.24f,.27f));
        knee=new Node3D{Position=new(0,-.12f,0)};leg.AddChild(knee);
        OfficeArt.Box(knee,new(0,-.065f,0),new(.15f,.13f,.17f),new(.20f,.24f,.27f));
        OfficeArt.Box(knee,new(0,-.145f,-.045f),new(.19f,.11f,.26f),new(.13f,.12f,.11f));return leg;
    }
    public void Animate(double delta,double speed,int effect)
    {
        if(_body is null)return;
        _selection.Visible=Selected&&!PrivacyObscured;
        _attention.Visible=NeedsAttention&&!Ambient&&!PrivacyObscured;
        if(PrivacyObscured){effect=0;ClearTrail();}
        if(speed<=0){ClearTrail();return;}
        _visualTime+=Math.Max(0,delta);
        var moving=Moving;var elapsed=(float)(delta*speed);_phase+=elapsed*9;
        var distance=elapsed*7;var travelledThisFrame=0f;
        while(distance>0 && Moving)
        {
            var target=Route.Count>0?Route.Peek():Destination;var d=Position.DistanceTo(target);
            if(d<.0001f){Position=target;if(Route.Count>0)Route.Dequeue();else break;continue;}
            var step=Math.Min(distance,d);var direction=(target-Position)/d;
            Rotation=new(0,Mathf.Atan2(-direction.X,-direction.Z),0);
            var from=GlobalPosition;Position+=direction*step;
            if(effect>0&&speed>1)RecordMovement(from,GlobalPosition,
                _visualTime-delta+travelledThisFrame/(speed*7),step/(speed*7));
            travelledThisFrame+=step;distance-=step;
            if(step>=d){Position=target;if(Route.Count>0)Route.Dequeue();else break;}
        }
        // The arrival tolerance must not leave a seated actor short of the chair or WC.
        if(!Moving)Position=Destination;
        var seated=!Moving && SeatHeight>0 && (Activity is "Work" or "Promotion" or "Recovery" or "Break" or "Eating" or "Toilet" || Activity=="Idle"&&!Ambient);
        if(seated&&SeatedFacing is {} facing)Rotation=new(0,facing,0);
        if(_characterModel is not null)
        {
            _characterModel.Tick(delta,speed,Moving?"Walk":seated?Activity=="Eating"?"Eat":"Sit":"Idle",SeatHeight);
            UpdateExposure(speed,effect,moving,seated);return;
        }
        // Sit on the cushion's front edge; short shins hang clear rather than stretching to the floor.
        _body.Position=new(0,seated?SeatHeight-PelvisBottom:StandingBodyOffset,seated?-.21f:0);
        _leftLeg.Rotation=new(seated?Mathf.Pi/2:moving?Mathf.Sin(_phase)*.55f:0,0,0);
        _rightLeg.Rotation=new(seated?Mathf.Pi/2:moving?-Mathf.Sin(_phase)*.55f:0,0,0);
        _leftKnee.Rotation=_rightKnee.Rotation=new(seated?-Mathf.Pi/2:0,0,0);
        _leftArm.Rotation=new(seated?1.35f:moving?-Mathf.Sin(_phase)*.45f:0,0,0);
        _rightArm.Rotation=new(seated?1.40f+Mathf.Sin(_phase)*.12f:moving?Mathf.Sin(_phase)*.45f:0,0,0);
        if(UsingModularKit)
        {
            // Shared elbow joints put short chibi hands on the desk, not above it.
            _leftArm.Rotation=new(seated?1.15f:moving?-Mathf.Sin(_phase)*.45f:0,0,0);
            _rightArm.Rotation=new(seated?1.15f:moving?Mathf.Sin(_phase)*.45f:0,0,0);
            _leftForearm!.Rotation=new(seated?-.5f:0,0,0);
            _rightForearm!.Rotation=new(seated?-.5f+Mathf.Sin(_phase)*.08f:0,0,0);
        }
        if(Activity=="Toilet"&&!Moving)
        {
            _leftArm.Rotation=new(.30f,0,0);_rightArm.Rotation=new(.30f,0,0);
            if(UsingModularKit){_leftForearm!.Rotation=new(-.35f,0,0);_rightForearm!.Rotation=new(-.35f,0,0);}
        }
        UpdateExposure(speed,effect,moving,seated);
    }
}
