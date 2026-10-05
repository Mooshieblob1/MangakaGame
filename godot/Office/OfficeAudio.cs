using System;
using Godot;

namespace MangakaGame;

/// <summary>Room tone, pencil strokes, page turns, interface clicks and the phone buzz, timed in real seconds.
/// The sounds are CC0 recordings in Assets/Sfx (Q67, see its README); the procedural waves stand in if a file is missing.</summary>
public partial class OfficeAudio : Node
{
    private const string Folder="res://Assets/Sfx/";
    private readonly AudioStreamPlayer _room=new(),_activity=new(),_effect=new(),_buzz=new();
    private AudioStream[] _strokes=[],_pages=[];
    // Presentation only: picks which stroke plays next and never touches the simulation's random numbers.
    private readonly Random _pick=new(1996);
    private AudioStream? _lastActivity;
    private double _elapsed,_nextActivity,_nextEffect;
    private bool _active,_effectsOn=true;
    public int EffectCount { get; private set; }
    public int ActivityCount { get; private set; }
    public int BuzzCount { get; private set; }
    public bool EffectPlaying => _effect.Playing;
    /// <summary>True when every sound comes from the Assets/Sfx recordings and the room tone loops, for the smoke check.</summary>
    public bool UsingRecordings => _room.Stream is AudioStreamOggVorbis { Loop: true } && _effect.Stream is AudioStreamOggVorbis
        && _buzz.Stream is AudioStreamOggVorbis && _strokes.Length == 5 && _pages.Length == 2;
    public override void _Ready()
    {
        AddChild(_room);AddChild(_activity);AddChild(_effect);AddChild(_buzz);
        var room=Sound("room-tone");if(room is AudioStreamOggVorbis ogg)ogg.Loop=true;
        _room.Stream=room??Wave(12,0,true);_effect.Stream=Sound("click")??Wave(.13,2);_buzz.Stream=Sound("phone-buzz")??Wave(.42,3);
        _strokes=Sounds("pencil-0",5);_pages=Sounds("page-0",2);
        if(_strokes.Length==0)_strokes=[Wave(.24,1)];
        _activity.Stream=_strokes[0];
        foreach (var player in new[] { _room, _activity, _effect, _buzz }) player.Bus = "Effects";
    }
    public void Update(double delta,bool active,bool working,double ambience,double effects)
    {
        _elapsed+=Math.Min(delta,1);_active=active;_previewLeft-=delta;
        _room.VolumeDb=Mathf.LinearToDb((float)Math.Max(.00001,ambience));
        _activity.VolumeDb=_room.VolumeDb;_effect.VolumeDb=Mathf.LinearToDb((float)Math.Max(.00001,effects));_buzz.VolumeDb=_effect.VolumeDb;
        if(active&&ambience>0){if(!_room.Playing)_room.Play();}
        else{_room.Stop();_activity.Stop();}
        // A slider preview may play in menus, where everything else here is paused (final review).
        if((!active||effects==0)&&_previewLeft<=0){_effect.Stop();_buzz.Stop();}
        _effectsOn=effects>0;
        if(active&&working&&ambience>0&&_elapsed>=_nextActivity)
        {_activity.Stream=NextActivity();_activity.Play();ActivityCount++;_nextActivity=_elapsed+7.5;}
        if(!active||!working)_nextActivity=_elapsed+3;
    }
    /// <summary>A pencil stroke most of the time and now and then a page turn, never the same sound twice running.</summary>
    private AudioStream NextActivity()
    {
        var pool=_pages.Length>0&&_pick.Next(5)==0?_pages:_strokes;
        var next=pool[_pick.Next(pool.Length)];
        if(next==_lastActivity&&pool.Length>1)next=pool[(Array.IndexOf(pool,next)+1)%pool.Length];
        return _lastActivity=next;
    }
    private static AudioStream? Sound(string name)=>ResourceLoader.Exists(Folder+name+".ogg")?ResourceLoader.Load<AudioStream>(Folder+name+".ogg"):null;
    private static AudioStream[] Sounds(string prefix,int count)
    {
        var found=new System.Collections.Generic.List<AudioStream>();
        for(int i=1;i<=count;i++)if(Sound(prefix+i) is { } sound)found.Add(sound);
        return found.ToArray();
    }
    /// <summary>The interface click at the current level, for the Sound effects slider.</summary>
    public void Preview() { if (!_effect.Playing) _effect.Play(); _previewLeft = .3; }
    private double _previewLeft;
    public void Cue()
    {
        if(!_active||_elapsed<_nextEffect)return;
        _effect.Play();EffectCount++;_nextEffect=_elapsed+.25;
    }
    /// <summary>A soft two-pulse vibration for a new message from Helper-Chan. Counted even when muted so checks can see it.</summary>
    public void Buzz()
    {
        BuzzCount++;
        if(_active&&_effectsOn)_buzz.Play();
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
                3=>(Math.Sin(t*Math.PI*2*180)*.7+Math.Sin(t*Math.PI*2*360)*.2)*.06*Math.Max(0,Math.Sin(Math.PI*2*t/.21)),
                _=>Math.Sin(t*Math.PI*2*660)*.035*Math.Exp(-t*30)
            };
            short sample=(short)(Math.Clamp(value*envelope,-1,1)*short.MaxValue);
            data[i*2]=(byte)sample;data[i*2+1]=(byte)(sample>>8);
        }
        return new AudioStreamWav{Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Data=data,
            LoopMode=loop?AudioStreamWav.LoopModeEnum.Forward:AudioStreamWav.LoopModeEnum.Disabled,LoopEnd=count};
    }
}
