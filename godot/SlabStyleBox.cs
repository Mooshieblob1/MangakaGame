using Godot;

namespace MangakaGame;

/// <summary>A button face in the logo's style (spec 2026-09-29): a coloured face on a darker extruded base inside a
/// brown outline, drawn as three rounded boxes. Content margins keep the label on the face; a pressed slab (less
/// depth) moves its face down onto the base.</summary>
public partial class SlabStyleBox : StyleBox
{
    public Color Face, Base, Outline;
    public float Border = 2, Depth = 4, Radius = 8;
    private readonly StyleBoxFlat _box = new();

    public static SlabStyleBox Create(Color face, Color @base, Color outline, float depth, float padX, float padY)
    {
        var slab = new SlabStyleBox { Face = face, Base = @base, Outline = outline, Depth = depth };
        slab.ContentMarginLeft = slab.ContentMarginRight = padX + slab.Border;
        slab.ContentMarginTop = padY + slab.Border + (4 - depth);
        slab.ContentMarginBottom = padY + slab.Border + depth;
        return slab;
    }

    private void Fill(Rid canvas, Rect2 rect, Color colour, float radius)
    {
        _box.BgColor = colour; _box.SetCornerRadiusAll((int)radius); _box.Draw(canvas, rect);
    }

    public override void _Draw(Rid toCanvasItem, Rect2 rect)
    {
        Fill(toCanvasItem, rect, Outline, Radius + Border);
        var inner = rect.Grow(-Border);
        Fill(toCanvasItem, inner, Base, Radius);
        var sink = 4 - Depth;
        Fill(toCanvasItem, new Rect2(inner.Position.X, inner.Position.Y + sink, inner.Size.X, inner.Size.Y - Depth - sink), Face, Radius);
    }
}
