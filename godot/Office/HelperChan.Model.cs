using System;
using Godot;

namespace MangakaGame;

public partial class HelperChan
{
    private CharacterModel? _characterModel;
    public bool UsingImportedModel=>_characterModel is not null;
    public string ImportedClip=>_characterModel?.CurrentClip??"";
    public double ImportedAnimationTime=>_characterModel?.AnimationTime??0;

    private bool BuildImportedModel()
    {
        // The companion's existing routes and desk use +Z forward.
        _characterModel=CharacterModel.LoadModel("res://Assets/Helper/Model/helper-chan-animated.glb",facesPositiveZ:true);
        if(_characterModel is null)return false;
        AddChild(_characterModel);Destination=Position;
        _characterModel.Tick(0,0,AtDesk?"Write":"Idle",SeatHeight);return true;
    }

    private void AnimateImportedModel(double delta,double speed)
    {
        // Keep the existing seven-unit route speed, doorway handling and follower gap.
        if(speed>0)
        {
            _phase+=(float)(delta*speed);
            var distance=(float)(delta*speed*7);
            while(distance>0&&Moving)
            {
                var next=Route.Count>0?Route.Peek():Destination;var gap=Position.DistanceTo(next);
                if(gap<.0001f){Position=next;if(Route.Count>0)Route.Dequeue();else break;continue;}
                var direction=(next-Position)/gap;var step=Math.Min(distance,gap);
                Rotation=new(0,Mathf.Atan2(direction.X,direction.Z),0);Position+=direction*step;distance-=step;
                if(step>=gap){Position=next;if(Route.Count>0)Route.Dequeue();else break;}
            }
        }
        var moving=Moving;var seated=!moving&&SeatHeight>0;
        var writing=seated&&AtDesk&&_phase%20<14;
        DeskActivity=moving?"Following":!AtDesk?"Taking a break":writing?"Writing notes":"Checking clipboard";
        if(!moving&&AtDesk)Rotation=Vector3.Zero;
        _characterModel!.Tick(delta,Math.Max(0,speed),moving?"Walk":writing?"Write":seated?"Sit":"Idle",SeatHeight);
    }
}
