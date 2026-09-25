using System;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeActor
{
    private const int ExposureSamples=96;
    private const float SampleSpacing=.075f, MaximumStreak=4.5f;
    private readonly record struct ExposureSample(Vector3 Position,float Angle,double Time,float Distance);
    private readonly ExposureSample[] _exposureHistory=new ExposureSample[ExposureSamples];
    private int _exposureHead,_exposureCount;
    private float _distanceToSample=SampleSpacing,_pathDistance,_bodyBlur=-1;
    private double _visualTime;
    private MultiMeshInstance3D _exposure=null!,_handExposure=null!;
    private (MeshInstance3D Mesh,StandardMaterial3D Material,float Alpha)[] _bodyMaterials=Array.Empty<(MeshInstance3D,StandardMaterial3D,float)>();
    private static Mesh? _exposureMesh;
    private static Shader? _exposureShader;
    public float TrailSpan { get; private set; }
    public float BodyBlur=>Math.Max(0,_bodyBlur);

    private static Mesh ExposureMesh()
    {
        if(_exposureMesh is not null)return _exposureMesh;
        var builder=new SurfaceTool();builder.Begin(Mesh.PrimitiveType.Triangles);
        // Rounded volumes, not sharp boxes: overlapping silhouettes form soft exposure.
        void Part(Vector3 centre,Vector3 size)=>builder.AppendFrom(
            new SphereMesh{Radius=.5f,Height=1,RadialSegments=8,Rings=4},0,
            new Transform3D(Basis.FromScale(size),centre));
        Part(new(0,1.03f+StandingBodyOffset,0),new(.54f,.55f,.50f));
        Part(new(0,.65f+StandingBodyOffset,0),new(.43f,.36f,.29f));
        foreach(var x in new[]{-.24f,.24f})Part(new(x,.66f+StandingBodyOffset,0),new(.17f,.32f,.17f));
        foreach(var x in new[]{-.10f,.10f})Part(new(x,.16f,0),new(.19f,.32f,.23f));
        _exposureMesh=builder.Commit();return _exposureMesh;
    }
    private void BuildExposure(Color skin,Color cloth,Color hair)
    {
        _exposureShader??=GD.Load<Shader>("res://Office/Hyperlapse.gdshader");
        MultiMeshInstance3D Create(Mesh mesh,int count,bool hand)
        {
            var material=new ShaderMaterial{Shader=_exposureShader};
            material.SetShaderParameter("skin_color",skin);material.SetShaderParameter("cloth_color",cloth);
            material.SetShaderParameter("hair_color",hair);material.SetShaderParameter("hand_only",hand);
            var node=new MultiMeshInstance3D{
                Multimesh=new MultiMesh{TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,UseColors=true,Mesh=mesh,InstanceCount=count,VisibleInstanceCount=0},
                MaterialOverride=material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            AddChild(node);node.TopLevel=true;node.GlobalTransform=Transform3D.Identity;return node;
        }
        _exposure=Create(ExposureMesh(),ExposureSamples,false);
        _handExposure=Create(new SphereMesh{Radius=.065f,Height=.16f,RadialSegments=8,Rings=4},20,true);
        _bodyMaterials=_body.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
            .SelectMany(p=>Enumerable.Range(0,p.Mesh.GetSurfaceCount())
                .Select(surface=>(Mesh:p,Material:p.GetActiveMaterial(surface) as StandardMaterial3D)))
            .Where(p=>p.Material is not null)
            .Select(p=>(p.Mesh,p.Material!,p.Material!.AlbedoColor.A)).ToArray();
        _bodyBlur=-1;ClearTrail();
    }
    private void SetBodyBlur(float value)
    {
        if(Math.Abs(_bodyBlur-value)<.001f)return;
        foreach(var (mesh,material,alpha) in _bodyMaterials)
        {
            var color=material.AlbedoColor;color.A=alpha*(1-value);material.AlbedoColor=color;
            material.Transparency=color.A<1?BaseMaterial3D.TransparencyEnum.Alpha:BaseMaterial3D.TransparencyEnum.Disabled;
            mesh.CastShadow=value>0?GeometryInstance3D.ShadowCastingSetting.Off:GeometryInstance3D.ShadowCastingSetting.On;
        }
        _bodyBlur=value;
    }
    public void ClearTrail()
    {
        if(_exposure is not null)_exposure.Multimesh.VisibleInstanceCount=0;
        if(_handExposure is not null)_handExposure.Multimesh.VisibleInstanceCount=0;
        _exposureHead=_exposureCount=0;_pathDistance=0;_distanceToSample=SampleSpacing;
        TrailCount=0;TrailSpan=0;SetBodyBlur(0);
    }
    private void RecordMovement(Vector3 from,Vector3 to,double start,double duration)
    {
        var length=from.DistanceTo(to);if(length<.0001f)return;
        var direction=(to-from)/length;var angle=Mathf.Atan2(-direction.X,-direction.Z);
        // Sample distance along each real route segment, including within a slow frame.
        // This neither bridges corners nor depends on one snapshot per rendered frame.
        for(var offset=_distanceToSample;offset<=length;offset+=SampleSpacing)
        {
            _exposureHistory[_exposureHead]=new(from+direction*offset,angle,start+duration*offset/length,_pathDistance+offset);
            _exposureHead=(_exposureHead+1)%ExposureSamples;_exposureCount=Math.Min(ExposureSamples,_exposureCount+1);
        }
        _distanceToSample=(_distanceToSample-length)%SampleSpacing;
        if(_distanceToSample<=.00001f)_distanceToSample+=SampleSpacing;
        _pathDistance+=length;
    }
    private void UpdateExposure(double speed,int effect,bool moving,bool seated)
    {
        if(effect==0||speed<=1){ClearTrail();return;}
        var shutter=speed>=8?.18:speed>=4?.14:.10;
        var count=0;TrailSpan=0;
        for(var i=0;i<_exposureCount;i++)
        {
            var sample=_exposureHistory[(_exposureHead-_exposureCount+i+ExposureSamples)%ExposureSamples];
            var age=(_visualTime-sample.Time)/shutter;var distance=_pathDistance-sample.Distance;
            if(age<0||age>=1||distance>MaximumStreak||effect==1&&i%3!=0)continue;
            var fade=(float)Math.Pow(1-Math.Max(age,distance/MaximumStreak),1.2);
            var transform=new Transform3D(Basis.FromEuler(new(0,sample.Angle,0)),sample.Position);
            _exposure.Multimesh.SetInstanceTransform(count,transform);
            _exposure.Multimesh.SetInstanceColor(count,new Color(1,1,1,fade*(effect==1?.17f:.13f)));
            count++;TrailSpan=Math.Max(TrailSpan,distance);
        }
        _exposure.Multimesh.VisibleInstanceCount=count;
        SetBodyBlur(moving?(effect==1?.28f:speed>=8?.65f:speed>=4?.45f:.22f)*(Selected?.6f:1):0);
        var hands=_characterModel is null&&seated&&Activity is "Work" or "Promotion" or "Recovery"?(effect==1?6:20):0;
        for(var i=0;i<hands;i++)
        {
            var pastPhase=_phase-(float)(shutter*(i+1)/hands*speed*9);
            var arm=new Transform3D(Basis.FromEuler(new(1.40f+Mathf.Sin(pastPhase)*.12f,0,0)),_rightArm.Position);
            var pose=_body.GlobalTransform*arm*new Transform3D(Basis.Identity,new(0,-.23f,-.02f));
            _handExposure.Multimesh.SetInstanceTransform(i,pose);
            _handExposure.Multimesh.SetInstanceColor(i,new(1,1,1,.09f*(1-(float)i/hands)));
        }
        _handExposure.Multimesh.VisibleInstanceCount=hands;TrailCount=count+hands;
    }
}
