using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

/// <summary>Shared imported visual only. Routes, employment and time remain owned by the game.</summary>
public partial class CharacterModel : Node3D
{
    private static readonly Dictionary<string,PackedScene> Scenes=new();
    private AnimationPlayer _player=null!;
    private readonly Dictionary<string,string> _clips=new();
    public string CurrentClip { get; private set; }="";
    public string AssetPath { get; private set; }="";
    public double AnimationTime=>CurrentClip.Length==0?0:_player.CurrentAnimationPosition;

    public static CharacterModel? LoadModel(string path,bool facesPositiveZ=false)
    {
        if(!Scenes.TryGetValue(path,out var packed))
        {
            packed=GD.Load<PackedScene>(path);
            if(packed is null){GD.PushWarning($"Character model not imported: {path}");return null;}
            Scenes[path]=packed;
        }
        var visual=new CharacterModel{AssetPath=path};
        var orientation=new Node3D{Rotation=new(0,facesPositiveZ?-Mathf.Pi/2:Mathf.Pi/2,0),Scale=Vector3.One*1.2f};
        visual.AddChild(orientation);
        var instance=packed.Instantiate<Node3D>();orientation.AddChild(instance);
        var player=instance.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().FirstOrDefault();
        if(player is null){visual.Free();GD.PushWarning($"Character rig has no animation player: {path}");return null;}
        visual._player=player;
        // Advance explicitly so pause, editing and fast-forward apply exactly once.
        player.CallbackModeProcess=AnimationMixer.AnimationCallbackModeProcess.Manual;
        player.Autoplay="";
        foreach(var name in player.GetAnimationLibraryList())
        {
            var library=(AnimationLibrary)player.GetAnimationLibrary(name).Duplicate(true);
            player.RemoveAnimationLibrary(name);player.AddAnimationLibrary(name,library);
        }
        foreach(var clip in player.GetAnimationList())
        {
            var shortName=clip.Split('/').Last();
            if(shortName is not ("Idle" or "Walk" or "Sit" or "Write" or "Eat"))continue;
            player.GetAnimation(clip).LoopMode=Animation.LoopModeEnum.Linear;
            visual._clips[shortName]=clip;
        }
        if(new[]{"Idle","Walk","Sit"}.Any(c=>!visual._clips.ContainsKey(c)))
        {visual.Free();GD.PushWarning($"Character rig lacks required clips: {path}");return null;}
        // Packed scenes share meshes and materials. Every actor needs independent opacity.
        foreach(var mesh in instance.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {
            // Include the seated pose and long hair, not just the imported rest bounds.
            mesh.CustomAabb=new Aabb(new(-.8f,-.3f,-.8f),new(1.6f,1.8f,1.6f));
            for(var surface=0;surface<mesh.Mesh.GetSurfaceCount();surface++)
                if(mesh.GetActiveMaterial(surface) is Material material)
                    mesh.SetSurfaceOverrideMaterial(surface,(Material)material.Duplicate());
        }
        return visual;
    }

    public void Tick(double delta,double speed,string clip,float seatHeight)
    {
        if(!IsInsideTree())return;
        if(!_clips.TryGetValue(clip,out var imported)){clip="Sit";imported=_clips[clip];}
        // Authored clips use a .51-high seat. Apply the difference outside the rig.
        Position=new(0,clip is "Sit" or "Write" or "Eat"?seatHeight-OfficeArt.DeskSeatHeight:0,0);
        if(CurrentClip!=clip)
        {
            _player.Play(imported,0);_player.Advance(0);CurrentClip=clip;
        }
        if(speed>0&&delta>0)_player.Advance(delta*speed);
    }
}
