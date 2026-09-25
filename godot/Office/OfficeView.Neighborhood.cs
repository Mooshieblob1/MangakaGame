using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private Node3D? _neighborhood;
    private string _neighborhoodKey="";
    private const float NeighborhoodRadius=108,GroundY=-.32f,EdgeMargin=8;
    private Vector3 NeighborhoodCentre=>new((_plan.Width+12)*.125f,0,_plan.Depth*.125f);

    private void BuildNeighborhood(bool home,float width,float depth)
    {
        var key=$"{home}:{width}:{depth}";
        if(_neighborhoodKey==key)return;
        _neighborhoodKey=key;
        if(_neighborhood is not null){_world.RemoveChild(_neighborhood);_neighborhood.QueueFree();}
        _neighborhood=new Node3D{Name="ResidentialNeighborhood"};_world.AddChild(_neighborhood);
        var n=_neighborhood;var centre=NeighborhoodCentre;
        OfficeArt.Box(n,centre+new Vector3(0,GroundY-.12f,0),new(NeighborhoodRadius*2,.24f,NeighborhoodRadius*2),new("697367"));
        // Quiet residential lanes, not a skyline beneath a high-rise apartment.
        var plotWidth=width+12;var plotDepth=depth+9;
        var laneX=width+8;var laneZ=depth+5;
        OfficeArt.Box(n,new(laneX,GroundY+.01f,centre.Z),new(4,.02f,NeighborhoodRadius*2),new("626967"));
        OfficeArt.Box(n,new(centre.X,GroundY+.012f,laneZ),new(NeighborhoodRadius*2,.02f,3.6f),new("626967"));
        OfficeArt.Box(n,new(width/2,GroundY+.025f,depth/2),new(plotWidth,.05f,plotDepth),new("aaa99a"));
        OfficeArt.Box(n,new((width+.8f)/2,-.30f,depth/2),new(width+7,.20f,depth+.8f),new("98978b"));
        // The entrance is at ground level, with a short approach and a recessed step.
        OfficeArt.Box(n,new(width+5.2f,-.20f,depth-.85f),new(2.8f,.24f,1.6f),new("aaa89a"));
        OfficeArt.Box(n,new(width+6.65f,-.28f,depth-.85f),new(.6f,.08f,1.6f),new("bab5a1"));
        if(home)
        {
            // Leave the sky-window side open; the rear stairs already establish the house's upper floor.
            var wood=new Color("65513e");
            for(var x=-3f;x<=width+3.8f;x+=1.8f)
                OfficeArt.Box(n,new(x,.20f,-.13f),new(.1f,.5f,.14f),wood);
            OfficeArt.Box(n,new(width+4.12f,1.08f,depth-1.9f),new(.14f,.24f,.34f),new("59665e"));
            for(var i=0;i<5;i++)Shrub(n,new(-4.4f,GroundY,1+i*1.1f),.65f);
            OfficeArt.Box(n,new(-4.6f,.09f,depth+1.9f),new(.18f,.82f,depth+3),new("939789"));
            OfficeArt.Box(n,new(width*.4f,.09f,depth+2.5f),new(width+8,.82f,.18f),new("939789"));
            for(var i=0;i<4;i++)Shrub(n,new(-2+i*1.4f,GroundY,depth+2.1f),.7f);
        }
        BuildCompactResidentialBlocks(n,home,width,depth,laneX,laneZ);
        BatchNeighborhoodBoxes(n);
    }

    private void BuildCompactResidentialBlocks(Node3D parent,bool home,float width,float depth,float laneX,float laneZ)
    {
        // Compact ~8.2 x 8.7 lots, with facing pairs of rows on narrow connected lanes.
        // Stylized ward housing, not a cadastral recreation or city-wide density claim.
        var columns=new List<float>();
        for(var i=0;i<13;i++){columns.Add(-8.5f-i*8.2f);columns.Add(laneX+6.4f+i*8.2f);}
        for(var x=-.3f;x+3.9f<laneX-2;x+=8.2f)columns.Add(x);
        for(var block=-6;block<=6;block++)
        {
            var street=laneZ+block*17.4f;
            OfficeArt.Box(parent,new(NeighborhoodCentre.X,GroundY+.016f,street),new(NeighborhoodRadius*2,.025f,3.2f),new("626967"));
            for(var col=0;col<columns.Count;col++)foreach(var side in new[]{-1f,1f})
            {
                var x=columns[col];var z=street+side*4.65f;
                if(Math.Abs(x-NeighborhoodCentre.X)>NeighborhoodRadius-5||Math.Abs(z-NeighborhoodCentre.Z)>NeighborhoodRadius-5)continue;
                // Keep the player plot, open window setback and rear stairwell clear.
                if(x+4.0f>-4.5f&&x-4.0f<width+5.6f&&z+4>-6.2f&&z-4<depth+3)continue;
                var variant=Math.Abs(col*13+block*7+(int)side)%4;
                var near=Math.Abs(x-NeighborhoodCentre.X)<30&&Math.Abs(z-NeighborhoodCentre.Z)<30;
                var lot=new Node3D{Position=new(x,GroundY,z),Rotation=new(0,side>0?Mathf.Pi:0,0)};parent.AddChild(lot);
                OfficeArt.Box(lot,new(0,.025f,0),new(8.05f,.05f,8.5f),new("a9a899"));
                House(lot,Vector3.Zero,6.6f+variant*.12f,5.6f,home?5.0f+variant*.22f:5.5f+variant*.2f,variant,near);
                if(!near)continue;
                // Short plot walls, a small front apron and restrained greenery replace lawns.
                foreach(var edge in new[]{-1f,1f})OfficeArt.Box(lot,new(edge*3.98f,.38f,-.25f),new(.10f,.76f,7.8f),new("94948b"));
                OfficeArt.Box(lot,new(0,.30f,-4.2f),new(8,.6f,.10f),new("94948b"));
                OfficeArt.Box(lot,new(2.5f,.035f,3.45f),new(2.5f,.07f,1.2f),new("c1beb1"));
                OfficeArt.Box(lot,new(-2.9f,.38f,3.55f),new(.17f,.76f,.16f),new("777c79"));
                OfficeArt.Box(lot,new(-2.9f,.78f,3.55f),new(.32f,.22f,.20f),new("626961"));
                Shrub(lot,new(-3.1f,0,2.9f),.5f);
                if(col%3==0)
                {
                    OfficeArt.Box(lot,new(3.75f,2.8f,3.6f),new(.12f,5.6f,.12f),new("66665b"));
                    OfficeArt.Box(lot,new(3.75f,5.15f,3.6f),new(1.1f,.08f,.09f),new("56584f"));
                }
            }
        }
    }

    private static void BatchNeighborhoodBoxes(Node3D parent)
    {
        // Dense housing shares a few instanced box batches instead of thousands of draws.
        var boxes=parent.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
            .Where(m=>m.Mesh is BoxMesh&&m.MaterialOverride is StandardMaterial3D).ToArray();
        var inverse=parent.GlobalTransform.AffineInverse();
        foreach(var group in boxes.GroupBy(m=>((StandardMaterial3D)m.MaterialOverride).AlbedoColor))
        {
            var entries=group.ToArray();var multi=new MultiMesh{TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,Mesh=new BoxMesh{Size=Vector3.One},InstanceCount=entries.Length};
            for(var i=0;i<entries.Length;i++)
            {
                var mesh=entries[i];var transform=inverse*mesh.GlobalTransform;
                transform.Basis*=Basis.FromScale(((BoxMesh)mesh.Mesh).Size);multi.SetInstanceTransform(i,transform);
                mesh.GetParent().RemoveChild(mesh);mesh.QueueFree();
            }
            parent.AddChild(new MultiMeshInstance3D{Multimesh=multi,MaterialOverride=OfficeArt.Material(group.Key)});
        }
    }
    private static void Shrub(Node3D parent,Vector3 at,float size)
    {
        OfficeArt.Box(parent,at+new Vector3(0,.24f,0),new(.14f,.48f,.14f),new("625241"));
        OfficeArt.Sphere(parent,at+new Vector3(0,size*.7f,0),new(size,size,size),new("4e704e"));
    }
    private static void House(Node3D parent,Vector3 at,float w,float d,float h,int variant,bool detail)
    {
        var root=new Node3D{Position=at};parent.AddChild(root);
        var walls=new Color[]{new("d1c9b4"),new("b8bcb2"),new("c6bca9"),new("b9b4a6")};
        OfficeArt.Box(root,new(0,h/2,0),new(w,h,d),walls[variant%4]);
        var roof=new Color(variant%2==0?"4b555c":"686057");
        var rise=d*.27f;var slope=Mathf.Atan2(rise,d*.5f+.3f);
        var length=Mathf.Sqrt(Mathf.Pow(d*.5f+.3f,2)+rise*rise);
        foreach(var sign in new[]{-1,1})
        {
            var panel=OfficeArt.Box(root,new(0,h+rise/2,sign*(d*.25f+.15f)),new(w+.8f,.16f,length),roof);
            panel.Rotation=new(sign*slope,0,0);
        }
        OfficeArt.Box(root,new(0,h+rise,0),new(w+.85f,.18f,.20f),roof.Lightened(.08f));
        if(!detail)return;
        foreach(var x in new[]{-w*.28f,w*.28f})
        {
            OfficeArt.Box(root,new(x,h*.6f,d/2+.025f),new(1.4f,1.05f,.07f),new("596a70"));
            OfficeArt.Box(root,new(x,h*.6f,d/2+.07f),new(.055f,1.06f,.04f),new("e1dccb"));
            OfficeArt.Box(root,new(x,h*.6f,d/2+.075f),new(1.4f,.045f,.04f),new("e1dccb"));
        }
        OfficeArt.Box(root,new(0,.95f,d/2+.04f),new(.9f,1.9f,.12f),new("665b4c"));
        OfficeArt.Box(root,new(w/2+.2f,.4f,0),new(.45f,.65f,.9f),new("bbbeb6"));
    }

    // Bound the full orthographic footprint, including the part beneath floating UI.
    // Ground projection is affine, so its four corners also bound every screen pixel.
    private void ConstrainNeighborhoodCamera()
    {
        var direction=-_camera.Basis.Z;
        Vector3 GroundVector(Vector3 v)=>v-direction*(v.Y/direction.Y);
        _pan=GroundVector(_pan);
        var aspect=Math.Max(1,Size.X)/Math.Max(1,Size.Y);
        var right=GroundVector(_camera.Basis.X);var up=GroundVector(_camera.Basis.Y);
        var extentX=(Math.Abs(right.X)*aspect+Math.Abs(up.X))*_zoom*.5f;
        var extentZ=(Math.Abs(right.Z)*aspect+Math.Abs(up.Z))*_zoom*.5f;
        var available=NeighborhoodRadius-EdgeMargin;
        var fit=Math.Min(1,available/Math.Max(extentX,extentZ));
        _zoom*=fit;extentX*=fit;extentZ*=fit;
        var limitX=Math.Max(0,Math.Min(24,available-extentX));var limitZ=Math.Max(0,Math.Min(24,available-extentZ));
        _pan=new(Math.Clamp(_pan.X,-limitX,limitX),0,Math.Clamp(_pan.Z,-limitZ,limitZ));
        var centre=NeighborhoodCentre+_pan;
        _camera.Size=_zoom;_camera.Position=centre+new Vector3(Mathf.Cos(_angle)*20,22,Mathf.Sin(_angle)*20);_camera.LookAt(centre);
    }
    internal bool NeighborhoodEdgesHidden()
    {
        var viewportSize=_viewport.Size;var centre=NeighborhoodCentre;
        foreach(var p in new[]{Vector2.Zero,new Vector2(viewportSize.X,0),new Vector2(0,viewportSize.Y),new Vector2(viewportSize.X,viewportSize.Y)})
        {
            var ray=_camera.ProjectRayOrigin(p);var direction=_camera.ProjectRayNormal(p);
            var ground=ray+direction*((GroundY-ray.Y)/direction.Y);
            if(Math.Abs(ground.X-centre.X)>=NeighborhoodRadius-1||Math.Abs(ground.Z-centre.Z)>=NeighborhoodRadius-1)return false;
        }
        return true;
    }
}
