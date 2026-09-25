using Godot;

namespace MangakaGame;

/// <summary>Keeps reading and action forms bounded on ultrawide monitors.</summary>
public partial class GuiReadingColumn : Container
{
    public float ReadingWidth { get; set; } = 1180;
    public override Vector2 _GetMinimumSize()
    {
        var height=0f;
        foreach(var child in GetChildren())if(child is Control { Visible:true } control)
            height=Mathf.Max(height,control.GetCombinedMinimumSize().Y);
        return new(0,height);
    }
    public override void _Notification(int what)
    {
        if(what!=NotificationSortChildren)return;
        var width=Mathf.Min(Size.X,ReadingWidth);
        foreach(var child in GetChildren())if(child is Control control)
            FitChildInRect(control,new Rect2(new Vector2((Size.X-width)/2,0),new Vector2(width,Size.Y)));
    }
}
