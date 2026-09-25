using Godot;

namespace MangakaGame;

/// <summary>Original reusable geometry. Dimensions and pivots are in metres.</summary>
public static class OfficeArt
{
    // Cushion surfaces, shared with character seated poses.
    public const float DeskSeatHeight=.51f, BreakSeatHeight=.47f;
    public static StandardMaterial3D Material(Color color, bool transparent = false) => new()
    {
        AlbedoColor = color, Roughness = .85f,
        Transparency = transparent ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
    };

    public static OfficeWorldLabel WorldLabel(Node3D parent,string text,Vector3 position,int fontSize=18,Color? color=null)
    {
        var label=new OfficeWorldLabel{Text=text,Position=position,FontSize=fontSize,
            TextColor=color??new Color("f5f0e5")};
        parent.AddChild(label);return label;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 position, Vector3 size, Color color)
    {
        var mesh = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = position,
            MaterialOverride = Material(color, color.A < 1) };
        parent.AddChild(mesh); return mesh;
    }

    public static MeshInstance3D Sphere(Node3D parent, Vector3 position, Vector3 scale, Color color)
    {
        var mesh = new MeshInstance3D { Mesh = new SphereMesh { Radius = .5f, Height = 1, RadialSegments = 12, Rings = 6 },
            Position = position, Scale = scale, MaterialOverride = Material(color, color.A < 1) };
        parent.AddChild(mesh); return mesh;
    }

    public static Node3D Furniture(string kind, int variant = 0, bool frontWriting = false)
    {
        var root = new Node3D();
        var wood = new Color(.57f,.39f,.24f); var paper = new Color(.94f,.91f,.8f);
        var steel = new Color(.33f,.38f,.39f); var dark = new Color(.15f,.20f,.22f);
        if (kind.Contains("desk"))
        {
            Box(root,new(.625f,.74f,.375f),new(1.25f,.08f,.75f),variant == 0 ? wood : paper);
            foreach(var x in new[]{.07f,1.18f}) foreach(var z in new[]{.08f,.67f})
                Box(root,new(x,.35f,z),new(.055f,.7f,.055f),steel);
            Box(root,new(1.02f,.48f,.4f),new(.3f,.45f,.55f),wood.Darkened(.1f));
            for(var i=0;i<3;i++) Box(root,new(1.02f,.35f+i*.14f,.115f),new(.09f,.015f,.018f),dark);
            Box(root,new(.50f,.79f,frontWriting?.14f:.40f),new(.32f,.015f,frontWriting?.20f:.42f),paper);
            for(var i=0;i<4;i++) Box(root,new(.46f,.800f,frontWriting?.08f+i*.035f:.27f+i*.075f),new(.18f,.002f,.007f),dark);
            Box(root,new(.78f,.80f,.54f),new(.23f,.008f,.015f),steel);
            Box(root,new(.14f,.78f,.22f),new(.14f,.025f,.14f),dark);
            Box(root,new(.14f,1.00f,.22f),new(.022f,.42f,.022f),dark);
            Box(root,new(.22f,1.20f,.25f),new(.23f,.06f,.16f),new(.80f,.71f,.43f));
            Box(root,new(.95f,.80f,.52f),new(.16f,.08f,.19f),paper);
        }
        else if(kind.Contains("chair"))
        {
            var fabric=variant==0 ? new Color(.28f,.37f,.39f) : new Color(.29f,.38f,.52f);
            Box(root,new(.375f,DeskSeatHeight-.05f,.375f),new(.46f,.10f,.45f),fabric);
            Box(root,new(.375f,.75f,.63f),new(.46f,variant==0?.4f:.6f,.07f),fabric);
            Box(root,new(.375f,.23f,.375f),new(.06f,.46f,.06f),steel);
            Box(root,new(.375f,.07f,.375f),new(.58f,.05f,.055f),dark);
            Box(root,new(.375f,.07f,.375f),new(.055f,.05f,.58f),dark);
        }
        else if(kind.Contains("break"))
        {
            Box(root,new(.75f,.62f,.75f),new(.75f,.08f,.72f),wood);
            Box(root,new(.75f,.30f,.75f),new(.12f,.6f,.12f),steel);
            foreach(var x in new[]{.17f,1.33f})
            {
                Box(root,new(x,BreakSeatHeight-.05f,.75f),new(.3f,.10f,.45f),variant==0?steel:new Color(.40f,.47f,.33f));
                Box(root,new(x,.2f,.75f),new(.06f,.4f,.06f),steel);
            }
            Sphere(root,new(.7f,.72f,.65f),new(.1f,.1f,.1f),paper);
            Box(root,new(.88f,.68f,.9f),new(.2f,.015f,.2f),paper);
        }
        else if(kind=="shelf")
        {
            Box(root,new(.5f,.85f,.47f),new(1,1.7f,.05f),wood);
            foreach(var x in new[]{.03f,.97f}) Box(root,new(x,.85f,.25f),new(.06f,1.7f,.5f),wood);
            for(var row=0;row<4;row++)
            {
                Box(root,new(.5f,.12f+row*.4f,.25f),new(1,.05f,.5f),wood);
                for(var book=0;book<9;book++) Box(root,new(.12f+book*.09f,.29f+row*.4f,.23f),new(.06f,.28f,.27f),
                    new Color(.35f+book%3*.16f,.30f+row*.10f,.30f+book%2*.20f));
            }
        }
        else if(kind=="plant")
        {
            Box(root,new(.25f,.14f,.25f),new(.26f,.28f,.26f),new(.57f,.31f,.22f));
            Box(root,new(.25f,.43f,.25f),new(.03f,.5f,.03f),wood);
            foreach(var v in new[]{new Vector3(.1f,.55f,.2f),new(.36f,.67f,.26f),new(.25f,.85f,.3f)})
                Sphere(root,v,new(.28f,.19f,.25f),new(.25f,.43f,.28f));
        }
        else if(kind=="print")
        {
            Box(root,new(.5f,1.5f,.025f),new(.8f,.6f,.04f),wood);
            Box(root,new(.5f,1.5f,.052f),new(.72f,.52f,.01f),paper);
            for(var i=0;i<3;i++) Box(root,new(.26f+i*.24f,1.5f,.061f),new(.18f,.3f,.005f),new(.3f+i*.15f,.42f,.45f));
        }
        else
        {
            Box(root,new(.375f,.43f,.25f),new(.75f,.86f,.5f),paper.Darkened(.18f));
            Box(root,new(.375f,.88f,.25f),new(.8f,.04f,.55f),steel);
            Sphere(root,new(.2f,1.02f,.25f),new(.20f,.24f,.20f),paper);
            Box(root,new(.52f,.94f,.24f),new(.12f,.09f,.12f),new(.3f,.46f,.4f));
        }
        return root;
    }
}
