using System;
using Godot;

namespace MangakaGame;

/// <summary>Original procedural room tone, pencil strokes and soft interface cues, timed in real seconds.</summary>
public partial class OfficeAudio : Node
{
    private readonly AudioStreamPlayer _room=new(),_activity=new(),_effect=new();
    private double _elapsed,_nextActivity,_nextEffect;
    private bool _active;
    public int EffectCount { get; private set; }
    public int ActivityCount { get; private set; }
    public override void _Ready()
    {
        AddChild(_room);AddChild(_activity);AddChild(_effect);
        _room.Stream=Wave(12,0,true);_activity.Stream=Wave(.24,1);_effect.Stream=Wave(.13,2);
    }
    public void Update(double delta,bool active,bool working,double ambience,double effects)
    {
        _elapsed+=Math.Min(delta,1);_active=active;
        _room.VolumeDb=Mathf.LinearToDb((float)Math.Max(.00001,ambience));
        _activity.VolumeDb=_room.VolumeDb;_effect.VolumeDb=Mathf.LinearToDb((float)Math.Max(.00001,effects));
        if(active&&ambience>0){if(!_room.Playing)_room.Play();}
        else{_room.Stop();_activity.Stop();}
        if(!active||effects==0)_effect.Stop();
        if(active&&working&&ambience>0&&_elapsed>=_nextActivity)
        {_activity.Play();ActivityCount++;_nextActivity=_elapsed+7.5;}
        if(!active||!working)_nextActivity=_elapsed+3;
    }
    public void Cue()
    {
        if(!_active||_elapsed<_nextEffect)return;
        _effect.Play();EffectCount++;_nextEffect=_elapsed+.25;
    }
    public static AudioStreamWav Wave(double seconds,int kind,bool loop=false)
    {
        const int rate=22050;int count=(int)(rate*seconds);var data=new byte[count*2];uint noise=78193;double low=0;
        for(int i=0;i<count;i++)
        {
            noise=unchecked(noise*1664525+1013904223);double n=(noise/(double)uint.MaxValue)*2-1;
            low=low*.97+n*.03;double t=i/(double)rate;
            double envelope=Math.Min(1,t/.025)*Math.Min(1,(seconds-t)/.04);
            double value=kind switch
            {
                0=>low*.13+Math.Sin(t*Math.PI*2*100)*.0015,
                1=>n*.045*Math.Pow(Math.Sin(Math.PI*t/seconds),2),
                _=>Math.Sin(t*Math.PI*2*660)*.035*Math.Exp(-t*30)
            };
            short sample=(short)(Math.Clamp(value*envelope,-1,1)*short.MaxValue);
            data[i*2]=(byte)sample;data[i*2+1]=(byte)(sample>>8);
        }
        return new AudioStreamWav{Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Data=data,
            LoopMode=loop?AudioStreamWav.LoopModeEnum.Forward:AudioStreamWav.LoopModeEnum.Disabled,LoopEnd=count};
    }
}
