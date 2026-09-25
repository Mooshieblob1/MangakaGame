using Godot;

namespace MangakaGame;

/// <summary>A single rigid leaf rotates about its hinge; its size never changes.</summary>
public partial class OfficeDoor : Node3D
{
    private Node3D _hinge=null!;
    private double _hold;
    public float Openness { get; private set; }
    public bool ClearToPass=>Openness>=.98f;
    public const float LeafWidth=1.2f;
    public float Width { get; set; }=LeafWidth;
    public override void _Ready()
    {
        _hinge=new Node3D{Position=new(-.025f,0,-Width/2)};AddChild(_hinge);
        OfficeArt.Box(_hinge,new(0,1.03f,Width/2),new(.055f,2.06f,Width),new(.47f,.37f,.27f));
        OfficeArt.Box(_hinge,new(-.06f,1.02f,Width-.14f),new(.13f,.035f,.10f),new("c4c5ad"));
    }
    public void RequestPassage()=>_hold=.4;
    public void Animate(double delta,double speed)
    {
        if(speed<=0)return;
        var elapsed=(float)(delta*speed);Openness=Mathf.MoveToward(Openness,_hold>0?1:0,elapsed*5);
        // Swing into the private room/stairwell so an open leaf cannot block the corridor.
        _hold=System.Math.Max(0,_hold-elapsed);_hinge.Rotation=new(0,Mathf.Pi/2*Openness,0);
    }
}
