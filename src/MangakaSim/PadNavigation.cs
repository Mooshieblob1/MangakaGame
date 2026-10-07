namespace MangakaSim;

// Controller support (2026-10-06): the timing and button prompts behind gamepad navigation, kept engine-free so they
// can be tested. The Godot side reads the controller and moves the focus; nothing here touches the simulation.

public enum PadDirection { None, Up, Down, Left, Right }

/// <summary>Turns the left stick into one of four directions. A push must pass <see cref="PressAt"/> to count and
/// fall back under <see cref="ReleaseAt"/> to end, so a stick resting near the edge does not flicker.</summary>
public sealed class PadStick
{
    public const double PressAt = .55, ReleaseAt = .3;
    public PadDirection Direction { get; private set; }

    /// <summary>The direction the stick has just entered, or None when nothing new happened.</summary>
    public PadDirection Update(double x, double y)
    {
        var length = Math.Sqrt(x * x + y * y);
        if (Direction != PadDirection.None && length < ReleaseAt) { Direction = PadDirection.None; return PadDirection.None; }
        if (length < PressAt) return PadDirection.None;
        var now = Dominant(x, y);
        if (now == Direction) return PadDirection.None;
        Direction = now;
        return now;
    }

    public void Reset() => Direction = PadDirection.None;

    private static PadDirection Dominant(double x, double y) =>
        Math.Abs(x) > Math.Abs(y) ? (x < 0 ? PadDirection.Left : PadDirection.Right) : (y < 0 ? PadDirection.Up : PadDirection.Down);
}

/// <summary>A held direction repeats after a short pause, so long lists can be run through without tapping.</summary>
public sealed class PadRepeat
{
    public const double FirstDelay = .38, Interval = .085;
    private double _wait;
    public PadDirection Held { get; private set; }

    public void Press(PadDirection direction) { Held = direction; _wait = FirstDelay; }
    public void Release(PadDirection direction) { if (Held == direction) Held = PadDirection.None; }
    public void Clear() => Held = PadDirection.None;

    /// <summary>How many repeat steps fall due in this frame; a slow frame never fires more than three.</summary>
    public int Update(double delta)
    {
        if (Held == PadDirection.None) return 0;
        _wait -= delta;
        var steps = 0;
        while (_wait <= 0 && steps < 3) { steps++; _wait += Interval; }
        if (_wait <= 0) _wait = Interval;
        return steps;
    }
}

/// <summary>An analogue trigger used as a button: pressed past <see cref="PressAt"/>, released under <see cref="ReleaseAt"/>.</summary>
public sealed class PadTrigger
{
    public const double PressAt = .6, ReleaseAt = .3;
    public bool Down { get; private set; }

    /// <summary>True only on the update where the trigger is newly pressed.</summary>
    public bool Update(double value)
    {
        if (Down) { if (value < ReleaseAt) Down = false; return false; }
        if (value < PressAt) return false;
        Down = true;
        return true;
    }
}

/// <summary>Controller buttons as the prompts show them. Xbox lettering, which Valve accepts for Steam Deck.</summary>
public enum PadGlyph { A, B, X, Y, LB, RB, LT, RT, Menu, View, LeftStick, RightStick, DPad }

public enum PadScreen { Startup, Title, TitlePage, PauseMenu, MenuPage, Popup, Story, Game }

/// <summary>What the prompt bar needs to know about the moment.</summary>
public sealed record PadContext(PadScreen Screen, bool PageOpen = false, bool PhoneOpen = false, bool Slider = false,
    bool TextField = false, bool Keyboard = false, bool FurnitureEditing = false);

public static class PadPrompts
{
    /// <summary>The prompts for the moment, most useful first. An empty label joins a button to the next one (LB RB Sections). The bar wraps, so a narrow window keeps every one.</summary>
    public static IReadOnlyList<(PadGlyph Glyph, string Label)> For(PadContext context)
    {
        var prompts = new List<(PadGlyph, string)>();
        if (context.Screen == PadScreen.Startup) return [(PadGlyph.DPad, "Move"), (PadGlyph.A, "Select")];
        prompts.Add((PadGlyph.DPad, "Move"));
        if (context.Slider) prompts.Add((PadGlyph.DPad, "◀ ▶ Adjust"));
        prompts.Add((PadGlyph.A, context.TextField ? context.Keyboard ? "Type" : "Edit" : "Select"));
        switch (context.Screen)
        {
            case PadScreen.Title: break;
            case PadScreen.TitlePage: prompts.Add((PadGlyph.B, "Back")); break;
            case PadScreen.PauseMenu: prompts.Add((PadGlyph.B, "Resume")); break;
            case PadScreen.MenuPage: prompts.Add((PadGlyph.B, "Back")); prompts.Add((PadGlyph.Menu, "Resume")); break;
            case PadScreen.Popup: prompts.Add((PadGlyph.B, "Close")); break;
            case PadScreen.Story: break; // an answer, Read later or Skip closes a conversation
            case PadScreen.Game:
                if (context.FurnitureEditing) { prompts.Add((PadGlyph.RightStick, "Look")); break; }
                if (context.PageOpen) prompts.Add((PadGlyph.B, "Back"));
                else if (context.PhoneOpen) prompts.Add((PadGlyph.B, "Put phone away"));
                prompts.Add((PadGlyph.LB, "")); prompts.Add((PadGlyph.RB, "Sections"));
                prompts.Add((PadGlyph.Y, "Pause"));
                prompts.Add((PadGlyph.LT, "")); prompts.Add((PadGlyph.RT, "Slower, faster"));
                if (!context.PhoneOpen) prompts.Add((PadGlyph.X, "Phone")); // B already says how to put it away
                prompts.Add((PadGlyph.View, "Inbox"));
                prompts.Add((PadGlyph.RightStick, context.PageOpen ? "Scroll" : "Look"));
                prompts.Add((PadGlyph.Menu, "Menu"));
                break;
        }
        return prompts;
    }
}
