using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class OfficeActor
{
    private static PackedScene? _modularKit;
    private Node3D? _leftForearm,_rightForearm;
    public bool UsingModularKit { get; private set; }
    internal AppearanceRecipe? DisplayedAppearance { get; private set; }
    private static readonly string[] HairModules={"HairShort","HairBob","HairTied"};
    private static readonly string[] ClothingModules={"Cardigan","CollaredShirt","Sweater"};
    internal static readonly string[] SkinPalette={"ebba91","c78f66","966145","5c3b2e"};
    internal static readonly string[] HairPalette={"252332","40291a","705238","92959d"};
    internal static readonly string[] ClothesPalette={"4c7382","825259","5c7352","a38557","6e5c87","99a19c"};

    public void Build(int skin,int hair,int outfit,int style=0,bool glasses=false,int build=0,int wardrobe=0)
        =>Build(new AppearanceRecipe(skin,hair,outfit,style,glasses,build,wardrobe));

    public void Build(AppearanceRecipe recipe)
    {
        if(!recipe.Valid)throw new ArgumentException("Invalid employee appearance.",nameof(recipe));
        _modularKit??=GD.Load<PackedScene>("res://Assets/People/modular-person.glb");
        if(_modularKit is null)
        {
            GD.PushWarning("Modular employee asset is unavailable; using the legacy actor until assets are imported.");
            BuildLegacy(recipe.Skin,recipe.Hair,recipe.Outfit,recipe.Style,recipe.Glasses,recipe.Build);return;
        }
        foreach(var child in GetChildren()){RemoveChild(child);child.QueueFree();}
        ResidentRole="";UsingModularKit=true;_characterModel=null;DisplayedAppearance=recipe;
        var instance=_modularKit.Instantiate<Node3D>();AddChild(instance);
        Node3D Joint(string name)=>instance.Name==name?instance:instance.FindChild(name,true,false) as Node3D
            ??throw new InvalidOperationException("Modular kit is missing joint: "+name);
        _body=Joint("Body");_leftArm=Joint("LeftArm");_rightArm=Joint("RightArm");
        _leftForearm=Joint("LeftForearm");_rightForearm=Joint("RightForearm");
        _leftLeg=Joint("LeftLeg");_rightLeg=Joint("RightLeg");
        _leftKnee=Joint("LeftKnee");_rightKnee=Joint("RightKnee");
        for(var i=0;i<HairModules.Length;i++)Joint(HairModules[i]).Visible=i==recipe.Style;
        for(var i=0;i<ClothingModules.Length;i++)Joint(ClothingModules[i]).Visible=i==recipe.Wardrobe;
        Joint("Glasses").Visible=recipe.Glasses;
        Joint("SleeveLeft").Visible=Joint("SleeveRight").Visible=recipe.Wardrobe!=1;
        // Body breadth affects clothes and shoulder sockets, never the head or leg length.
        var width=1+recipe.Build*.07f;Joint("Torso").Scale=new(width,1,1);
        _leftArm.Position=new(-.23f*width,.80f,0);_rightArm.Position=new(.23f*width,.80f,0);
        var skin=new Color(SkinPalette[recipe.Skin]);var hair=new Color(HairPalette[recipe.Hair]);
        var cloth=new Color(ClothesPalette[recipe.Outfit]);
        foreach(var mesh in _body.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {
            var source=mesh.GetActiveMaterial(0) as StandardMaterial3D
                ??throw new InvalidOperationException("Modular kit requires standard materials.");
            // Meshes are shared; material instances are per actor so recolouring or
            // hyperlapse fading one employee cannot change everyone else's skin/outfit.
            var material=(StandardMaterial3D)source.Duplicate();
            material.AlbedoColor=source.ResourceName switch{"Skin"=>skin,"Hair"=>hair,"HairHighlight"=>hair.Lightened(.15f),"Cloth"=>cloth,_=>source.AlbedoColor};
            mesh.MaterialOverride=material;
        }
        _body.Position=new(0,StandingBodyOffset,0);
        _selection=OfficeArt.Box(this,new(0,.025f,0),new(.6f,.01f,.6f),new(.85f,.70f,.30f,.8f));_selection.Visible=false;
        _attention=OfficeArt.WorldLabel(this,"!",new(0,2.04f,0),24,new(.96f,.67f,.26f));_attention.ScreenOffset=new(0,-18);
        BuildExposure(skin,cloth,hair);
    }
}
