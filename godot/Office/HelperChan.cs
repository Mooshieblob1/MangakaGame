using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

/// <summary>Dedicated chibi companion retaining the reference's hair, glasses and outfit. No simulation employee or inventory item.</summary>
public partial class HelperChan : Node3D
{
    private readonly List<Node3D> _tails=new();
    private Node3D _upper=null!,_head=null!,_leftArm=null!,_rightArm=null!;
    private readonly List<Node3D> _walkLegs=new();
    public Queue<Vector3> Route { get; }=new();
    public Vector3 Destination { get; private set; }
    public bool Moving=>Route.Count>0||Position.DistanceTo(Destination)>.01f;
    public bool AtDesk { get; set; }=true;
    public float SeatHeight { get; set; }=OfficeArt.DeskSeatHeight;
    public string DeskActivity { get; private set; }="Writing notes";
    public void SetRoute(IEnumerable<Vector3> points,Vector3 destination){Route.Clear();foreach(var point in points)Route.Enqueue(point);Destination=destination;}
    private Node3D _body=null!,_standingLegs=null!,_sittingLegs=null!;
    private float _phase;
    public double Speed { get; set; }
    private static readonly Color Hair=new("efbd68"), Highlight=new("ffe3a0"), Skin=new("f9d1bd"), Shirt=new("e4f0ef"), Skirt=new("343d51"), Black=new("242a36");
    public override void _Ready()
    {
        if(BuildImportedModel())return;
        _body=new Node3D();AddChild(_body);_upper=new Node3D();_body.AddChild(_upper);
        _standingLegs=new Node3D();AddChild(_standingLegs);_sittingLegs=new Node3D();AddChild(_sittingLegs);
        // Oval ring surfaces retain the fitted waist, blouse and pencil-skirt silhouette.
        Rings(_body,new[]{(.48f,.19f,.115f),(.55f,.22f,.125f),(.73f,.24f,.145f),(.88f,.21f,.13f),(.97f,.165f,.115f)},Skirt);
        Rings(_upper,new[]{(.94f,.163f,.117f),(1.00f,.172f,.125f),(1.08f,.20f,.145f),(1.19f,.245f,.165f),(1.30f,.22f,.14f),(1.36f,.18f,.105f)},Shirt);
        foreach(var x in new[]{-.105f,.105f})
        {
            var leg=new Node3D{Position=new(x,0,0)};_standingLegs.AddChild(leg);
            var beforeLeg=_standingLegs.GetChildren().ToArray();
            Rings(leg,new[]{(.07f,.044f,.044f),(.17f,.041f,.043f),(.31f,.06f,.056f),(.46f,.074f,.069f),(.58f,.085f,.084f)},new("796958"));
            Ellipse(_standingLegs,new(x,.065f,.07f),new(.12f,.11f,.24f),Black);
            OfficeArt.Box(_standingLegs,new(x,.04f,-.065f),new(.085f,.08f,.08f),Black);
            Tube(_standingLegs,new[]{new Vector3(x-.04f,.098f,.09f),new(x,.12f,.095f),new(x+.04f,.098f,.09f)},.006f,new("747d84"));
            var pivot=new Node3D{Position=new(x,.5f,0)};_standingLegs.AddChild(pivot);
            leg.Reparent(pivot);foreach(var part in _standingLegs.GetChildren().Where(n=>n!=pivot&&!beforeLeg.Contains(n)).ToArray())part.Reparent(pivot);
            _walkLegs.Add(pivot);
            // Seated legs are authored at chibi size, relative to the hip, not adult floor-reaching legs.
            Tube(_sittingLegs,new[]{new Vector3(x,0,0),new(x,0,.14f),new(x,-.12f,.14f)},.055f,new("796958"));
            Ellipse(_sittingLegs,new(x,-.16f,.18f),new(.14f,.10f,.20f),Black);
        }
        Ellipse(_upper,new(0,1.405f,0),new(.105f,.15f,.105f),Skin);
        Ellipse(_upper,new(0,1.60f,0),new(.345f,.40f,.305f),Skin);
        // Back cap and crown leave the face open toward +Z.
        Ellipse(_upper,new(0,1.66f,-.045f),new(.38f,.39f,.27f),Hair);
        foreach(var sign in new[]{-1f,1f})
        {
            Ellipse(_upper,new(sign*.168f,1.60f,-.005f),new(.05f,.085f,.035f),Skin);
            Ellipse(_upper,new(sign*.080f,1.605f,.137f),new(.092f,.053f,.026f),new("fcfffc"));
            Ellipse(_upper,new(sign*.080f,1.605f,.151f),new(.036f,.044f,.012f),new("467acf"));
            Ellipse(_upper,new(sign*.080f,1.605f,.157f),new(.016f,.031f,.007f),new("153153"));
            Ellipse(_upper,new(sign*.075f,1.615f,.162f),new(.012f,.012f,.006f),Colors.White);
            Tube(_upper,new[]{new Vector3(sign*.035f,1.633f,.148f),new(sign*.08f,1.639f,.152f),new(sign*.127f,1.627f,.140f)},.004f,Black);
            Tube(_upper,new[]{new Vector3(sign*.04f,1.668f,.135f),new(sign*.08f,1.675f,.140f),new(sign*.122f,1.668f,.126f)},.003f,new("aa753c"));
            var glasses=new List<Vector3>();for(var i=0;i<=40;i++){var t=Mathf.Tau*i/40;glasses.Add(new(sign*.082f+Mathf.Cos(t)*.063f,1.608f+Mathf.Sin(t)*.044f,.172f));}Tube(_upper,glasses,.006f,Black);
            Tube(_upper,new[]{new Vector3(sign*.145f,1.625f,.167f),new(sign*.17f,1.627f,.015f)},.005f,Black);
            // Separate arm pivots support writing and hair-adjusting gestures.
            var beforeArm=_upper.GetChildren().ToArray();
            // Rolled sleeves and bare forearms, with one hand supporting the clipboard.
            Ellipse(_upper,new(sign*.23f,1.23f,0),new(.135f,.235f,.15f),Shirt);
            var elbow=new Vector3(sign*.29f,1.075f,.015f);var wrist=sign>0?new Vector3(.34f,1.40f,.20f):new Vector3(-.28f,.78f,.075f);
            Tube(_upper,new[]{new Vector3(sign*.25f,1.17f,0),elbow,wrist},.045f,Skin);
            Ellipse(_upper,wrist,new(.08f,.12f,.04f),Skin);
            Tube(_upper,new[]{new Vector3(sign*.265f,1.13f,-.07f),new(sign*.30f,1.13f,0),new(sign*.265f,1.13f,.07f)},.016f,Shirt);
            var arm=new Node3D{Position=new(sign*.23f,1.23f,0)};_upper.AddChild(arm);
            foreach(var part in _upper.GetChildren().Where(n=>n!=arm&&!beforeArm.Contains(n)).ToArray())part.Reparent(arm);
            if(sign<0)_leftArm=arm;else _rightArm=arm;
            // Exaggerated twin tails flow down into large spiral curls wider than the body.
            var tail=new Node3D{Position=new(sign*.22f,1.76f,-.06f)};_upper.AddChild(tail);_tails.Add(tail);
            var points=new List<Vector3>();
            for(var i=0;i<=90;i++)
            {
                var t=i/90f;float x,z;
                if(t<.46f){x=sign*.32f*Mathf.Sin(t/.46f*Mathf.Pi/2);z=-.05f;}
                else{var u=(t-.46f)/.54f;var radius=.30f*(1-.60f*u);x=sign*(.32f+Mathf.Sin(u*Mathf.Tau*2.3f)*radius);z=-.05f+(Mathf.Cos(u*Mathf.Tau*2.3f)-1)*radius;}
                points.Add(new(x,-1.66f*t,z));
            }
            Tube(tail,points,.135f,Hair,true);
            for(var strand=0;strand<5;strand++)
            {var line=new List<Vector3>();for(var i=0;i<points.Count;i++)line.Add(points[i]+new Vector3((strand-2)*.037f,0,.098f));Tube(tail,line,.012f,strand%2==0?Highlight:new("dba555"),true);}
            Ellipse(_upper,new(sign*.22f,1.74f,-.05f),new(.12f,.10f,.10f),Black);
            Ellipse(_upper,new(sign*.21f,1.795f,-.04f),new(.20f,.13f,.19f),Hair);
        }
        Tube(_upper,new[]{new Vector3(-.018f,1.615f,.173f),new(0,1.624f,.175f),new(.018f,1.615f,.173f)},.005f,Black);
        Ellipse(_upper,new(0,1.565f,.153f),new(.025f,.035f,.025f),Skin.Lightened(.05f));
        Tube(_upper,new[]{new Vector3(-.027f,1.526f,.138f),new(0,1.521f,.149f),new(.027f,1.526f,.138f)},.003f,new("b77f75"));
        for(var i=-3;i<=3;i++)
        {
            var x=i*.047f;
            Tube(_upper,new[]{new Vector3(x*.5f,1.81f,.02f),new(x,1.75f,.13f),new(x*.92f,1.68f+Math.Abs(i)*.009f,.139f)},.027f,Hair,true);
            Tube(_upper,new[]{new Vector3(x*.5f,1.815f,.04f),new(x,1.755f,.151f),new(x*.92f,1.695f+Math.Abs(i)*.009f,.16f)},.003f,Highlight,true);
        }
        Triangle(_upper,new(-.06f,1.38f,.07f),new(-.15f,1.30f,.13f),new(-.055f,1.24f,.153f),Colors.White);
        Triangle(_upper,new(.06f,1.38f,.07f),new(.15f,1.30f,.13f),new(.055f,1.24f,.153f),Colors.White);
        Tube(_upper,new[]{new Vector3(0,1.28f,.162f),new(0,1.13f,.17f),new(0,.96f,.12f)},.003f,new("abc1c4"));
        for(var y=.98f;y<1.31f;y+=.085f)Ellipse(_upper,new(0,y,.17f),new(.013f,.013f,.007f),Black);
        Tube(_upper,new[]{new Vector3(-.06f,1.36f,.10f),new(0,1.18f,.20f),new(.06f,1.36f,.10f)},.008f,new("af5152"));
        OfficeArt.Box(_upper,new(0,1.13f,.208f),new(.075f,.105f,.012f),Black);OfficeArt.Box(_upper,new(0,1.13f,.219f),new(.06f,.086f,.004f),Colors.White);
        Ellipse(_upper,new(0,1.145f,.224f),new(.025f,.03f,.003f),new("b2bcc5"));
        var clipboard=new Node3D{Position=new(.29f,1.37f,.25f),RotationDegrees=new(-22,0,-15)};_upper.AddChild(clipboard);
        OfficeArt.Box(clipboard,Vector3.Zero,new(.23f,.32f,.025f),Black);OfficeArt.Box(clipboard,new(0,0,.015f),new(.20f,.28f,.008f),Colors.White);
        OfficeArt.Box(clipboard,new(0,.14f,.026f),new(.08f,.03f,.014f),new("9fadb5"));
        for(var y=-.10f;y<.11f;y+=.035f)OfficeArt.Box(clipboard,new(0,y,.023f),new(.16f,.002f,.002f),new("9ba9ad"));
        // Keep face/glasses together while shortening the torso and limbs. Enlarging
        // the head independently gives the same chibi scale as the ordinary cast.
        _head=new Node3D{Scale=new(1.55f,1.30f,1.55f)};AddChild(_head);
        foreach(var mesh in _upper.GetChildren().OfType<MeshInstance3D>().ToArray())
            if((mesh.Transform*mesh.Mesh.GetAabb()).Position.Y>=1.39f)mesh.Reparent(_head,false);
        _body.Scale=new(.85f,.60f,.85f);_standingLegs.Scale=new(.85f,.46f,.85f);
        foreach(var tail in _tails){var sign=Math.Sign(tail.Position.X);tail.Reparent(this,false);tail.Position=new(sign*.29f,1.23f,-.06f);}
        Destination=Position;
        var pen=new Node3D{Position=new(-.05f,-.44f,.11f)};_leftArm.AddChild(pen);
        OfficeArt.Box(pen,Vector3.Zero,new(.025f,.025f,.20f),new("244963"));
        OfficeArt.Box(pen,new(0,0,.105f),new(.012f,.012f,.035f),Black);
        Pose(true);
    }
    private void Pose(bool seated)
    {
        // The skirt hem rests above the cushion. Forward offset clears the backrest
        // and puts the short lower legs beyond the cushion's front edge.
        var offset=seated?SeatHeight+.015f-.48f*.60f:-.06f;
        var forward=seated?.18f:0;
        _body.Position=new(0,offset,forward);_head.Position=new(0,.94f-1.60f*1.30f+offset,forward);
        _sittingLegs.Position=new(0,SeatHeight+.06f,forward);
        _standingLegs.Visible=!seated;_sittingLegs.Visible=seated;
        foreach(var tail in _tails){tail.Scale=new(.75f,seated?.70f:.61f,.75f);tail.Position=new(tail.Position.X,1.14f+offset,-.06f+forward);}
    }
    public void Animate(double delta,double speed)
    {
        if(_characterModel is not null){Speed=speed;AnimateImportedModel(delta,speed);return;}
        Speed=speed;Pose(!Moving&&SeatHeight>0);if(speed<=0)return;_phase+=(float)(delta*speed);
        var moving=Moving;var distance=(float)(delta*speed*7);
        while(distance>0&&Moving)
        {
            var next=Route.Count>0?Route.Peek():Destination;var gap=Position.DistanceTo(next);
            if(gap<.0001f){Position=next;if(Route.Count>0)Route.Dequeue();else break;continue;}
            var direction=(next-Position)/gap;var step=Math.Min(distance,gap);
            Rotation=new(0,Mathf.Atan2(direction.X,direction.Z),0);Position+=direction*step;distance-=step;
            if(step>=gap){Position=next;if(Route.Count>0)Route.Dequeue();else break;}
        }
        var seated=!Moving&&SeatHeight>0;Pose(seated);
        var beat=_phase%20;
        DeskActivity=moving?"Following":!AtDesk?"Taking a break":beat<10?"Writing notes":beat<14?"Adjusting hair":"Checking clipboard";
        if(!moving&&AtDesk)Rotation=Vector3.Zero;
        _leftArm.Rotation=moving?new(Mathf.Sin(_phase*7)*.3f,0,0):!AtDesk?new(-.6f,0,.3f):beat<10?
            new(-1.2f+Mathf.Sin(_phase*9)*.09f,0,.3f+Mathf.Sin(_phase*7)*.045f):beat<14?
            new(.2f,0,-2.4f+Mathf.Sin(_phase*3)*.08f):new(-.55f,0,.25f);
        _rightArm.Rotation=new(0,0,moving?Mathf.Sin(_phase*6)*.04f:Mathf.Sin(_phase*2)*.025f);
        for(var i=0;i<_walkLegs.Count;i++)_walkLegs[i].Rotation=new(moving?Mathf.Sin(_phase*9+i*Mathf.Pi)*.45f:0,0,0);
        for(var i=0;i<_tails.Count;i++)_tails[i].Rotation=new(0,Mathf.Sin(_phase*.7f+i)*.025f,Mathf.Sin(_phase*(moving?5:.9f)+i)*(moving?.04f:.016f));
        _upper.Rotation=new(!moving&&AtDesk&&beat<10?.035f:0,Mathf.Sin(_phase*.25f)*.025f,0);
    }
    private static void Ellipse(Node3D root,Vector3 pos,Vector3 scale,Color color)
    {root.AddChild(new MeshInstance3D{Position=pos,Scale=scale,Mesh=new SphereMesh{Radius=.5f,Height=1,RadialSegments=32,Rings=16},MaterialOverride=OfficeArt.Material(color)});}
    private static void Triangle(Node3D root,Vector3 a,Vector3 b,Vector3 c,Color color)
    {var st=new SurfaceTool();st.Begin(Mesh.PrimitiveType.Triangles);st.AddVertex(a);st.AddVertex(b);st.AddVertex(c);st.GenerateNormals();var mat=OfficeArt.Material(color);mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;root.AddChild(new MeshInstance3D{Mesh=st.Commit(),MaterialOverride=mat});}
    private static void Rings(Node3D root,(float Y,float X,float Z)[] rings,Color color)
    {
        var st=new SurfaceTool();st.Begin(Mesh.PrimitiveType.Triangles);const int count=40;
        Vector3 V(int r,int i){var t=Mathf.Tau*i/count;return new(Mathf.Cos(t)*rings[r].X,rings[r].Y,Mathf.Sin(t)*rings[r].Z);}
        for(var r=0;r<rings.Length-1;r++)for(var i=0;i<count;i++)foreach(var v in new[]{V(r,i),V(r+1,i),V(r+1,i+1),V(r,i),V(r+1,i+1),V(r,i+1)})st.AddVertex(v);
        st.GenerateNormals();var mat=OfficeArt.Material(color);mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;root.AddChild(new MeshInstance3D{Mesh=st.Commit(),MaterialOverride=mat});
    }
    private static void Tube(Node3D root,IReadOnlyList<Vector3> points,float radius,Color color,bool taper=false)
    {
        if(points.Count<2)return;const int sides=12;var st=new SurfaceTool();st.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 V(int i,int side)
        {
            var tangent=(points[Math.Min(points.Count-1,i+1)]-points[Math.Max(0,i-1)]).Normalized();
            var axis=Math.Abs(tangent.Dot(Vector3.Forward))>.98f?Vector3.Right:Vector3.Forward;
            var a=tangent.Cross(axis).Normalized();var b=tangent.Cross(a).Normalized();
            var r=radius*(taper?Mathf.Lerp(1,.05f,Mathf.Pow(i/(float)(points.Count-1),5)):1);
            return points[i]+(a*Mathf.Cos(Mathf.Tau*side/sides)+b*Mathf.Sin(Mathf.Tau*side/sides))*r;
        }
        for(var i=0;i<points.Count-1;i++)for(var j=0;j<sides;j++)foreach(var v in new[]{V(i,j),V(i+1,j),V(i+1,j+1),V(i,j),V(i+1,j+1),V(i,j+1)})st.AddVertex(v);
        st.GenerateNormals();var mat=OfficeArt.Material(color);mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;root.AddChild(new MeshInstance3D{Mesh=st.Commit(),MaterialOverride=mat});
    }
}
