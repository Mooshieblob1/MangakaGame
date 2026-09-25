using System;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private MeshInstance3D? _southDoorLintel;
    private void BuildSouthWalls(float w,float d,bool companion,bool home,Color floor)
    {
        // The office opens onto its interior front walkway at the actual route entrance.
        // The old quarter-width pieces accidentally left openings at both corners too.
        var doorway=(_plan.Entrance.X+.5f)*.25f;
        var start=Math.Clamp(doorway-.55f,0,w);var end=Math.Clamp(doorway+.55f,0,w);
        if(start>0)Wall(new(start/2,1.25f,d),new(start,2.5f,.12f),2);
        if(end<w)Wall(new((end+w)/2,1.25f,d),new(w-end,2.5f,.12f),2);
        _southDoorLintel=OfficeArt.Box(_room,new((start+end)/2,2.36f,d),new(end-start,.28f,.12f),new(.78f,.79f,.71f));

        // Continuous exterior boundary outside the walkway; entry remains in the east wall.
        var left=companion?-3f:0f;
        if(companion)OfficeArt.Box(_room,new(-1.5f,-.10f,d+.35f),new(3,.2f,.7f),floor);
        Wall(new((left+w)/2,1.25f,d+.7f),new(w-left,2.5f,.12f),2);
        Wall(new(left,1.25f,d+.35f),new(.12f,2.5f,.7f),1);
        Wall(new(w,1.25f,d+.6f),new(.12f,2.5f,.2f),3);
        Wall(new(w+1.3f,1.25f,d+.5f),new(2.6f,2.5f,.12f),2);
        var ground=home?GenkanFloorHeight:0;
        Wall(new(w+3.2f,ground+1.25f,d+.5f),new(1.2f,2.5f,.12f),2);
        Wall(new(w+3.8f,ground+1.25f,d+.25f),new(.12f,2.5f,.5f),3);
    }
}
