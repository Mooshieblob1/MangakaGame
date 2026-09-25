using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private const int FamilyStepCount=10;
    private const float FamilyStepRise=.19f,FamilyStepRun=.22f;
    private Vector3 FamilyStairFoot=>new(_plan.Width*.25f+3.2f,0,.28f);
    internal Vector3 FamilyStairTop=>new(FamilyStairFoot.X,FamilyStepCount*FamilyStepRise,-2.63f);
    private Vector3 FamilyStairThreshold=>new(FamilyStairFoot.X,0,.55f);
    private Vector3 StairTread(int i)=>new(FamilyStairFoot.X,(i+1)*FamilyStepRise,.17f-(i+.5f)*FamilyStepRun);

    private void BuildFamilyStairs()
    {
        var wood=new Color("967653");var cream=new Color("d1c9b6");var x=FamilyStairFoot.X;
        OfficeArt.Box(_room,new(x,-.115f,-1.13f),new(1.35f,.20f,3.5f),wood.Darkened(.22f));
        for(var i=0;i<FamilyStepCount;i++)
        {
            var top=StairTread(i);
            OfficeArt.Box(_room,new(x,top.Y/2,top.Z),new(1.10f,top.Y,FamilyStepRun),wood);
            OfficeArt.Box(_room,new(x,top.Y+.008f,top.Z),new(1.14f,.016f,FamilyStepRun),wood.Lightened(.16f));
            if(i%2==0)foreach(var side in new[]{-1f,1f})
                OfficeArt.Box(_room,new(x+side*.57f,top.Y+.38f,top.Z),new(.05f,.76f,.05f),wood.Darkened(.30f));
        }
        // A covered upper landing hides the endpoint; the visible route uses every tread.
        var height=FamilyStairTop.Y;
        OfficeArt.Box(_room,new(x,height-.07f,-2.53f),new(1.25f,.14f,1.02f),wood);
        foreach(var side in new[]{-1f,1f})
        {
            OfficeArt.Box(_room,new(x+side*.64f,height+.68f,-2.54f),new(.12f,1.5f,1.10f),cream);
            var rail=OfficeArt.Box(_room,new(x+side*.57f,1.75f,-.93f),new(.065f,.065f,2.92f),wood.Darkened(.3f));
            rail.Rotation=new(Mathf.Atan(FamilyStepRise/FamilyStepRun),0,0);
        }
        OfficeArt.Box(_room,new(x,height+.70f,-3.08f),new(1.40f,1.55f,.12f),cream);
        OfficeArt.Box(_room,new(x,height+1.5f,-2.52f),new(1.46f,.14f,1.3f),wood.Darkened(.45f));
        OfficeArt.WorldLabel(_room,"UPSTAIRS",new(x,.7f,.5f));
    }

    private IEnumerable<Vector3> FamilyWalkingRoute(Vector3 from,Vector3 target)
    {
        var points=new List<Vector3>();
        if(from.Y>.1f)
        {
            // Meal redirects may happen mid-descent at closing time.
            points.AddRange(Enumerable.Range(0,FamilyStepCount).Reverse().Select(StairTread)
                .Where(p=>p.Y<=from.Y+.001f));
            points.Add(FamilyStairFoot);points.Add(FamilyStairThreshold);from=FamilyStairThreshold;
        }
        if(target.Y>.1f)
        {
            points.AddRange(WalkingRoute(from,FamilyStairThreshold));points.Add(FamilyStairFoot);
            points.AddRange(Enumerable.Range(0,FamilyStepCount).Select(StairTread));points.Add(FamilyStairTop);
        }
        else points.AddRange(WalkingRoute(from,target));
        return points;
    }

}

