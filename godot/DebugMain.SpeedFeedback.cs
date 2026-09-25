using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private readonly Dictionary<int,Button> _speedButtons=new();
    private Label? _speedFlash;
    private Tween? _speedTween;
    private string SeriesSalesText(Series series)
    {
        var stock=series.Volumes.Sum(v=>_state.Stock(v.Id));
        var reserved=series.Volumes.Sum(v=>_state.ConventionReserved(v.Id));
        var held=series.Volumes.Sum(v=>_state.ConventionReserved(v.Id,true));
        var downloads=series.Volumes.Sum(v=>_state.DoujinDownloadsSold(v.Id));
        return $"Fans {series.Fanbase:N0} · Copies sold {_state.SeriesCopiesSold(series.Id):N0} (including {downloads:N0} downloads)\nPhysical stock {stock:N0} · Available {stock-held:N0}\nConvention reserve {reserved:N0} (includes orders due before the event)";
    }
    private void BuildSpeedFeedback()
    {
        var layer=new CanvasLayer{Layer=30};AddChild(layer);
        _speedFlash=new Label{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore,Visible=false};
        layer.AddChild(_speedFlash);_speedFlash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _speedFlash.AddThemeFontSizeOverride("font_size",160);
        _speedFlash.AddThemeColorOverride("font_color",Colors.White);
        _speedFlash.AddThemeColorOverride("font_shadow_color",new Color(0,0,0,.45f));
        _speedFlash.AddThemeConstantOverride("shadow_offset_x",4);_speedFlash.AddThemeConstantOverride("shadow_offset_y",4);
    }
    private void RefreshSpeedFeedback(bool flash=false)
    {
        var overnight=_overnightTarget is not null;
        if(_overnightSpeedBadge is not null)
        {_overnightSpeedBadge.Visible=overnight;_overnightSpeedBadge.Text=_speed>0?"▶▶ 32× NIGHT":"Ⅱ NIGHT PAUSED";}
        foreach(var (speed,button) in _speedButtons)
        {
            button.SetPressedNoSignal(_speed==speed);
            button.ThemeTypeVariation=_speed==speed?"HeaderActiveButton":"HeaderButton";
            button.Disabled=overnight&&speed>0;
            button.TooltipText=overnight?(speed==0?"Pause / resume the overnight transition at 32×":"Daytime speeds return in the morning. Space pauses / resumes the 32× night transition."):
                "Space: pause / resume · 1: slower · 2: faster";
        }
        if(!flash||_speedFlash is null||_inMenu)return;
        _speedTween?.Kill();_speedFlash.Text=_speed==0?"Ⅱ":_speed==1?"▶ 1×":$"▶▶ {_speed:0}×";
        _speedFlash.Modulate=new Color(1,1,1,.38f);_speedFlash.Show();
        if(_presentation.ReducedUiMotion){_speedTween=CreateTween();_speedTween.TweenInterval(.35);_speedTween.TweenCallback(Callable.From(()=>_speedFlash.Hide()));return;}
        _speedTween=CreateTween();_speedTween.TweenInterval(.2);
        _speedTween.TweenProperty(_speedFlash,"modulate:a",0f,.65);
        _speedTween.TweenCallback(Callable.From(()=>_speedFlash.Hide()));
    }
}
