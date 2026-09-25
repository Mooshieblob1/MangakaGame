using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private const float GenkanFloorHeight=-.14f;
    private bool ViewingFamilyHome=>_state?.Locations.Any(l=>l.Id==_location&&l.IsFamilyHome)==true;
    private float GenkanStepZ=>_plan.Depth*.25f-1.65f;

    private void BuildFamilyHallFloor()
    {
        var w=_plan.Width*.25f;var d=_plan.Depth*.25f;var wood=new Color("b39c75");
        OfficeArt.Box(_room,new(w+1.30f,-.10f,d/2+.25f),new(2.6f,.20f,d+.5f),wood);
        OfficeArt.Box(_room,new(w+3.2f,-.10f,GenkanStepZ/2),new(1.2f,.20f,GenkanStepZ),wood);
        var depth=d+.5f-GenkanStepZ;
        OfficeArt.Box(_room,new(w+3.2f,GenkanFloorHeight-.10f,GenkanStepZ+depth/2),new(1.2f,.20f,depth),new("777971"));
        // Agari-kamachi: the wooden edge marks the change from shoes to indoor floor.
        OfficeArt.Box(_room,new(w+3.2f,-.065f,GenkanStepZ-.035f),new(1.22f,.13f,.09f),new("806143"));
    }

    private void BuildGenkan()
    {
        var w=_plan.Width*.25f;var d=_plan.Depth*.25f;var wood=new Color("917252");
        for(var z=GenkanStepZ+.02f;z<d+.5f;z+=.32f)
            OfficeArt.Box(_room,new(w+3.2f,GenkanFloorHeight+.003f,z),new(1.18f,.006f,.012f),new("b1b0a6"));
        foreach(var x in new[]{w+2.9f,w+3.2f,w+3.5f})
            OfficeArt.Box(_room,new(x,GenkanFloorHeight+.003f,GenkanStepZ+(d+.5f-GenkanStepZ)/2),new(.012f,.006f,d+.5f-GenkanStepZ),new("b1b0a6"));
        // A low shoe cabinet at the end of the tiled area leaves the doorway route clear.
        OfficeArt.Box(_room,new(w+3.16f,.29f,d+.25f),new(.94f,.86f,.36f),wood);
        OfficeArt.Box(_room,new(w+3.16f,.74f,d+.25f),new(1.02f,.06f,.40f),wood.Lightened(.18f));
        foreach(var x in new[]{w+2.93f,w+3.39f})
        {
            OfficeArt.Box(_room,new(x,.29f,d+.061f),new(.43f,.77f,.025f),wood.Lightened(.08f));
            OfficeArt.Box(_room,new(x+.12f,.42f,d+.038f),new(.025f,.14f,.035f),new("51483c"));
        }
        foreach(var x in new[]{w+2.79f,w+2.94f})
            OfficeArt.Sphere(_room,new(x,GenkanFloorHeight+.055f,d-.32f),new(.115f,.11f,.24f),new("51453b"));
        foreach(var x in new[]{w+2.85f,w+3.02f})
            OfficeArt.Sphere(_room,new(x,.04f,GenkanStepZ-.25f),new(.13f,.08f,.25f),new("dad0b5"));
        OfficeArt.Box(_room,new(w+3.16f,.781f,d+.26f),new(.23f,.025f,.15f),new("d0c3a6"));
        OfficeArt.Box(_room,new(w+3.68f,.11f,d-.13f),new(.15f,.5f,.19f),new("586764"));
        foreach(var x in new[]{w+3.65f,w+3.71f})
            OfficeArt.Box(_room,new(x,.46f,d-.13f),new(.025f,.67f,.025f),new("4c5350"));
    }

    private IReadOnlyList<Vector3> WalkingRoute(Vector3 from,Vector3 target)
    {
        if(!ViewingFamilyHome)return FloorWalkingRoute(from,target);
        var w=_plan.Width*.25f;
        bool InGenkan(Vector3 p)=>p.X>=w+2.6f&&p.Z>GenkanStepZ;
        var entering=InGenkan(from);var leaving=InGenkan(target);
        if(entering&&leaving)return new[]{new Vector3(target.X,GenkanFloorHeight,target.Z)};
        var upper=new Vector3(w+3.2f,0,GenkanStepZ-.10f);
        var lower=new Vector3(w+3.2f,GenkanFloorHeight,GenkanStepZ+.10f);
        var route=new List<Vector3>();
        if(entering)
        {route.Add(new(w+3.2f,GenkanFloorHeight,from.Z));route.Add(lower);route.Add(upper);from=upper;}
        route.AddRange(FloorWalkingRoute(from,leaving?upper:target));
        if(leaving)
        {route.Add(lower);route.Add(new(w+3.2f,GenkanFloorHeight,target.Z));route.Add(new(target.X,GenkanFloorHeight,target.Z));}
        return route;
    }
}
