using System;
using Godot;

namespace MangakaGame;

public partial class DebugMain
{
    private DateTime? _overnightTarget;
    private bool _overnightDeparting;
    private double _overnightHold;
    private Label? _overnightCaption;
    private Label? _overnightSpeedBadge;
    private ProgressBar? _overnightProgress;
    private DateTime _overnightStart;
    private const double OvernightSpeed=32;
    // Overnight is a brief presentation, independent of the longer playable day.
    private const double OvernightSecondsPerHourAt1x=2.5;
    private double OfficePlaybackSpeed=>_speed<=0?0:_overnightDeparting?8:_speed;

    private void BeginOvernight()
    {
        var hours=_state.HoursUntilNextWork();
        if(hours<=0){SetSpeed(_resumeSpeed);return;}
        var previous=_resumeSpeed;
        SetSpeed(OvernightSpeed);
        _resumeSpeed=previous; // Temporary playback must never become the player's chosen speed.
        _overnightTarget=_state.Clock.Now.AddHours(hours);
        _overnightStart=_state.Clock.Now;
        _overnightDeparting=true;_overnightHold=.2;
        _homeOffice.EndingDay=_officeView.EndingDay=true;
        ShowOffice();Refresh();
        if(_overnightCaption is null)
        {
            var panel=new PanelContainer{MouseFilter=MouseFilterEnum.Ignore};_homeOffice.AddChild(panel);
            panel.AddThemeStyleboxOverride("panel",Surface(new Color("203d46"),14));
            var content=new VBoxContainer{MouseFilter=MouseFilterEnum.Ignore};panel.AddChild(content);
            _overnightCaption=new Label{HorizontalAlignment=HorizontalAlignment.Center,CustomMinimumSize=new(350,0),MouseFilter=MouseFilterEnum.Ignore};content.AddChild(_overnightCaption);
            _overnightCaption.AddThemeFontSizeOverride("font_size",22);
            _overnightCaption.AddThemeColorOverride("font_color",new Color("b2f4e6"));
            _overnightProgress=new ProgressBar{ShowPercentage=false,CustomMinimumSize=new(0,8),MouseFilter=MouseFilterEnum.Ignore};content.AddChild(_overnightProgress);
        }
        OvernightPanel!.Show();_overnightProgress!.Value=0;
        RefreshOvernightCaption();
    }
    private Control? OvernightPanel=>_overnightCaption?.GetParent().GetParent<Control>();
    private void RefreshOvernightCaption()
    {
        if(_overnightCaption is null||_overnightTarget is not {} target)return;
        var morning=_state.Clock.Hour is >=5 and <12;
        _overnightCaption.Text=_speed<=0?"Ⅱ OVERNIGHT PAUSED\nPress Space or Ⅱ to resume at 32×":
            _overnightDeparting?"Closing up for the night":
            $"▶▶ {(morning?"DAWN":"OVERNIGHT")} · 32×\n{_state.Clock.Now:HH:mm} → {target:ddd HH:mm}";
        PositionOvernightPanel();
    }
    private void PositionOvernightPanel()
    {
        if(OvernightPanel is not {} panel)return;
        var area=_homeOffice.PresentationArea;
        if(area.Size==Vector2.Zero)area=new Rect2(Vector2.Zero,_homeOffice.Size);
        panel.Size=panel.GetCombinedMinimumSize();
        panel.Position=new(area.Position.X+(area.Size.X-panel.Size.X)/2,area.End.Y-panel.Size.Y-12);
    }
    private void CancelOvernight()
    {
        _overnightTarget=null;_overnightDeparting=false;_overnightHold=0;
        OvernightPanel?.Hide();_overnightSpeedBadge?.Hide();
        if(_homeOffice is not null)_homeOffice.ClosedForNight=_homeOffice.EndingDay=false;
        if(_officeView is not null)_officeView.ClosedForNight=_officeView.EndingDay=false;
    }
    private void TickOvernight(double delta)
    {
        if(_overnightTarget is not {} target||_speed<=0||_inMenu||OfficeEditing)return;
        if(_overnightDeparting)
        {
            // Finish the existing doorway routes before any new day's arrivals can be bound.
            if(_homeOffice.DeparturesPending)return;
            _homeOffice.ClosedForNight=_officeView.ClosedForNight=true;
            _overnightDeparting=false;_overnightCaption!.Text="▶▶ OVERNIGHT · 32×\nLights out";
            RefreshSpeedFeedback(true);
        }
        if(_overnightHold>0){_overnightHold-=delta;return;}
        _accumulator+=delta*OvernightSpeed/OvernightSecondsPerHourAt1x;
        while(_accumulator>=1&&_overnightTarget is not null)
        {
            _accumulator-=1;
            if(AdvanceAndScan(1))return;
            if(_state.Clock.Now>=target)
            {var previous=_resumeSpeed;CancelOvernight();SetSpeed(previous);return;}
            // Keep one readable beat of darkness, including when a slow frame spans midnight.
            if(_state.Clock.Hour==0){_accumulator=0;_overnightHold=.2;break;}
        }
        if(_overnightTarget is not null)
        {
            _overnightProgress!.Value=100*((_state.Clock.Now-_overnightStart).TotalHours+_accumulator)/(target-_overnightStart).TotalHours;
            RefreshOvernightCaption();
        }
    }
}
