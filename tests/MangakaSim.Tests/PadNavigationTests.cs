using Xunit;
namespace MangakaSim.Tests;

public class PadNavigationTests
{
    [Fact] public void Stick_needs_a_firm_push_and_reports_each_direction_once()
    {
        var stick = new PadStick();
        Assert.Equal(PadDirection.None, stick.Update(0, .4));
        Assert.Equal(PadDirection.Down, stick.Update(0, .7));
        Assert.Equal(PadDirection.None, stick.Update(0, .9));
        Assert.Equal(PadDirection.None, stick.Update(.1, .4)); // still held: above the release point
        Assert.Equal(PadDirection.Down, stick.Direction);
        Assert.Equal(PadDirection.None, stick.Update(0, .1));
        Assert.Equal(PadDirection.None, stick.Direction);
        Assert.Equal(PadDirection.Left, stick.Update(-.8, .2));
    }

    [Fact] public void Stick_swinging_round_changes_direction_without_letting_go()
    {
        var stick = new PadStick();
        Assert.Equal(PadDirection.Up, stick.Update(0, -.9));
        Assert.Equal(PadDirection.Right, stick.Update(.9, -.2));
        Assert.Equal(PadDirection.None, stick.Update(.8, -.3));
    }

    [Fact] public void Held_direction_repeats_after_a_pause_then_steadily()
    {
        var repeat = new PadRepeat();
        repeat.Press(PadDirection.Down);
        Assert.Equal(0, repeat.Update(.3));
        Assert.Equal(1, repeat.Update(.1));
        Assert.Equal(1, repeat.Update(PadRepeat.Interval));
        Assert.Equal(3, repeat.Update(2)); // a long hitch never floods the list
        repeat.Release(PadDirection.Up);
        Assert.Equal(PadDirection.Down, repeat.Held); // letting go of another direction changes nothing
        repeat.Release(PadDirection.Down);
        Assert.Equal(0, repeat.Update(1));
    }

    [Fact] public void Trigger_presses_once_until_released()
    {
        var trigger = new PadTrigger();
        Assert.False(trigger.Update(.5));
        Assert.True(trigger.Update(.8));
        Assert.False(trigger.Update(1));
        Assert.False(trigger.Update(.4));
        Assert.False(trigger.Update(.2));
        Assert.True(trigger.Update(.7));
    }

    [Fact] public void Game_prompts_name_back_only_when_there_is_somewhere_to_go_back_to()
    {
        var office = PadPrompts.For(new PadContext(PadScreen.Game));
        Assert.DoesNotContain(office, p => p.Glyph == PadGlyph.B);
        Assert.Contains(office, p => p.Glyph == PadGlyph.RightStick && p.Label == "Look");
        var page = PadPrompts.For(new PadContext(PadScreen.Game, PageOpen: true));
        Assert.Contains(page, p => p.Glyph == PadGlyph.B && p.Label == "Back");
        Assert.Contains(page, p => p.Glyph == PadGlyph.RightStick && p.Label == "Scroll");
        Assert.Contains(PadPrompts.For(new PadContext(PadScreen.Game, PhoneOpen: true)), p => p.Glyph == PadGlyph.B && p.Label == "Put phone away");
    }

    [Fact] public void Story_offers_no_back_button_and_text_boxes_say_what_A_does()
    {
        Assert.DoesNotContain(PadPrompts.For(new PadContext(PadScreen.Story)), p => p.Glyph == PadGlyph.B);
        Assert.Contains(PadPrompts.For(new PadContext(PadScreen.MenuPage, TextField: true, Keyboard: true)), p => p.Glyph == PadGlyph.A && p.Label == "Type");
        Assert.Contains(PadPrompts.For(new PadContext(PadScreen.MenuPage, TextField: true)), p => p.Glyph == PadGlyph.A && p.Label == "Edit");
    }

    [Fact] public void Paired_buttons_share_one_label()
    {
        var prompts = PadPrompts.For(new PadContext(PadScreen.Game)).ToList();
        var lb = prompts.FindIndex(p => p.Glyph == PadGlyph.LB);
        Assert.Equal("", prompts[lb].Label);
        Assert.Equal((PadGlyph.RB, "Sections"), prompts[lb + 1]);
    }
}
