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
        _guidanceCard.Reparent(bottom);_notice.Reparent(bottom);
        oldBody.GetParent().RemoveChild(oldBody);oldBody.QueueFree();
        _floatingTop=new PanelContainer{ThemeTypeVariation="HeaderPanel"};_floatingUi.AddChild(_floatingTop);_shell.Reparent(_floatingTop);
        oldMargin.Hide();oldMargin.QueueFree();
        _shell.Resized+=ResizeFloatingOffice;bottom.Resized+=ResizeFloatingOffice;
        _floatingRail.MinimumSizeChanged+=ResizeFloatingOffice;
        _guidanceCard.VisibilityChanged+=ResizeFloatingOffice;
        _side.VisibilityChanged+=ResizeFloatingOffice;_report.VisibilityChanged+=ResizeFloatingOffice;
        _homeOffice.ShowNavigationHint=false;
    }
    private void ResizeFloatingOffice()
    {
        if(_floatingUi is null||_resizingFloating)return;
        _resizingFloating=true;
        try
        {
            var window=GetViewportRect().Size;var gap=12f;
            // At larger text scales on short windows, keep the page usable while
            // retaining the next-step action and the full guide in Help.
            var compactGuidance=window.Y/Math.Max(1,_presentation.UiScale)<600;
            RefreshCompactHeader();
            _guidanceText.Visible=!compactGuidance;_guidanceRoutes.Visible=!compactGuidance;
            _guidancePortrait.CustomMinimumSize=compactGuidance?new(48,54):new(96,108);
            var railWidth=_floatingRail.GetCombinedMinimumSize().X;
            var left=16+railWidth+gap;var width=Math.Max(260,window.X-left-16);
            void Place(Control panel,float x,float y,float w,float h)
            {panel.Position=new(x,y);panel.Size=new(w,h);}
            Place(_floatingRail,16,16,railWidth,window.Y-32);
            Place(_floatingTop,left,16,width,_floatingTop.GetCombinedMinimumSize().Y);
            var bottomHeight=_floatingBottom.GetCombinedMinimumSize().Y;
            Place(_floatingBottom,left,window.Y-16-bottomHeight,width,bottomHeight);
            var top=16+_floatingTop.Size.Y+gap;
            var available=Math.Max(160,_floatingBottom.Position.Y-gap-top);
            var sidebarWidth=Math.Min(460,width*.5f);
            var pageWidth=_officeSidebar?sidebarWidth:width;
            Place(_side,window.X-16-pageWidth,top,pageWidth,available);
            Place(_report,left,top,width,available);
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
