using System;
using Godot;

namespace MangakaGame;

public partial class DebugMain
{
    private Control? _floatingUi;
    private PanelContainer _floatingRail=null!,_floatingTop=null!,_floatingBottom=null!;
    private bool _resizingFloating;

    private void BuildFloatingOffice(Control oldMargin,PanelContainer rail,Control oldBody,Control shade)
    {
        // The office owns the whole canvas. Only the floating panel rectangles intercept input.
        _homeOffice.Reparent(this);MoveChild(_homeOffice,_backdrop.GetIndex()+1);
        _homeOffice.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _floatingUi=new Control{MouseFilter=MouseFilterEnum.Ignore};AddChild(_floatingUi);
        MoveChild(_floatingUi,shade.GetIndex());_floatingUi.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _floatingRail=rail;rail.Reparent(_floatingUi);
        _side.Reparent(_floatingUi);_report.Reparent(_floatingUi);
        BuildOfficeDashboard(_floatingUi);
        _floatingBottom=new PanelContainer{ThemeTypeVariation="FloatingPanel"};_floatingUi.AddChild(_floatingBottom);
        var bottom=new VBoxContainer();_floatingBottom.AddChild(bottom);
        _notice.Reparent(bottom);
        oldBody.GetParent().RemoveChild(oldBody);oldBody.QueueFree();
        _floatingTop=new PanelContainer{ThemeTypeVariation="HeaderPanel"};_floatingUi.AddChild(_floatingTop);_shell.Reparent(_floatingTop);
        oldMargin.Hide();oldMargin.QueueFree();
        _shell.Resized+=ResizeFloatingOffice;bottom.Resized+=ResizeFloatingOffice;
        _floatingRail.MinimumSizeChanged+=ResizeFloatingOffice;_floatingTop.MinimumSizeChanged+=ResizeFloatingOffice;
        _side.VisibilityChanged+=ResizeFloatingOffice;_report.VisibilityChanged+=ResizeFloatingOffice;
        _homeOffice.ShowNavigationHint=false;
        // Helper-Chan's phone floats above every other panel.
        _floatingUi.AddChild(_phone);_floatingUi.AddChild(_phoneIcon);
        // Wrapped text measures tall before it has a width, so settle again once the real minimum is known.
        _phone.MinimumSizeChanged+=ResizeFloatingOffice;
    }
    // With large text on a small window a page needs the phone's column, so the phone waits as its icon while a page is open.
    private bool PhoneCrampsPage()
    {
        var s=(float)_presentation.UiScale;
        var width=Math.Max(260,GetViewportRect().Size.X-16-_floatingRail.GetCombinedMinimumSize().X-12-16);
        return width-Math.Min(300*s,width*.6f)-12<560*s;
    }
    private bool PageOpen=>_side.Visible||_report.Visible;
    private void ResizeFloatingOffice()
    {
        if(_floatingUi is null||_resizingFloating)return;
        _resizingFloating=true;
        try
        {
            var window=GetViewportRect().Size;var gap=12f;
            RefreshCompactHeader();
            var railWidth=_floatingRail.GetCombinedMinimumSize().X;
            var left=16+railWidth+gap;var width=Math.Max(260,window.X-left-16);
            void Place(Control panel,float x,float y,float w,float h)
            {panel.Position=new(x,y);panel.Size=new(w,h);}
            Place(_floatingRail,16,16,railWidth,window.Y-32);
            Place(_floatingTop,left,16,width,_floatingTop.GetCombinedMinimumSize().Y);
            var bottomHeight=_floatingBottom.GetCombinedMinimumSize().Y;
            var top=16+_floatingTop.Size.Y+gap;
            // The phone and its icon share the bottom-right corner with the notice strip.
            var s=(float)_presentation.UiScale;
            var phoneWidth=Math.Min(300*s,width*.6f);var phoneHeight=Math.Max(200,Math.Min(440*s,window.Y-16-top));
            var iconSize=new Vector2(56*s,64*s);
            // _phoneSlide runs from 1 (below the window edge) to 0 while the phone slides up.
            // Godot may grow the phone to its content minimum, so the top edge uses the size it actually took.
            Place(_phone,window.X-16-phoneWidth,0,phoneWidth,phoneHeight);
            var phoneTop=window.Y-16-_phone.Size.Y;
            _phone.Position=new(_phone.Position.X,phoneTop+_phoneSlide*(window.Y-phoneTop));
            Place(_phoneIcon,window.X-16-iconSize.X,window.Y-16-iconSize.Y,iconSize.X,iconSize.Y);
            var corner=(_phoneOpen?phoneWidth:_phoneIcon.Visible?iconSize.X:0)+(_phoneOpen||_phoneIcon.Visible?gap:0);
            Place(_floatingBottom,left,window.Y-16-bottomHeight,Math.Max(200,width-corner),bottomHeight);
            var available=Math.Max(160,_floatingBottom.Position.Y-gap-top);
            var sidebarWidth=Math.Min(460,width*.5f);
            // An open phone keeps its own column when the page stays readable beside it; otherwise the player chose to open it over the page.
            var phoneColumn=_phoneOpen&&!PhoneCrampsPage()?phoneWidth+gap:0;
            var pageHeight=available;
            var pageWidth=_officeSidebar?Math.Min(sidebarWidth,width-phoneColumn):width-phoneColumn;
            Place(_side,window.X-16-phoneColumn-pageWidth,top,pageWidth,pageHeight);
            Place(_report,left,top,width-phoneColumn,pageHeight);
            var dashboardWidth=window.X<1500?320f:380f;
            _officeDashboard.Visible=_page=="Office"&&!_side.Visible&&!_report.Visible;
            Place(_officeDashboard,window.X-16-dashboardWidth,top,dashboardWidth,available);
            var obstruction=_officeDashboard.Visible?dashboardWidth+gap:_side.Visible&&_officeSidebar?pageWidth+gap:0;
            _homeOffice.PresentationArea=new Rect2(left,top,Math.Max(200,width-obstruction),available);
            PositionOvernightPanel();
        }
        finally{_resizingFloating=false;}
    }
}
