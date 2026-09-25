using Xunit;

namespace MangakaSim.Tests;

public class CreatorIdentityTests
{
    [Theory]
    [InlineData(null,"Aki")]
    [InlineData("","Aki")]
    [InlineData("   ","Aki")]
    [InlineData("  Haruka  ","Haruka")]
    [InlineData("秋 山田","秋 山田")]
    public void Opening_identity_and_studio_name_survive_saving(string? chosen,string expected)
    {
        var state=GameState.NewGame(protagonistName:chosen);
        state.Apply(new SetAppearanceCommand(new(2,1,4,2,true)));
        var restored=GameState.FromJson(state.ToJson());
        Assert.Equal(expected,restored.Protagonist.Name);
        Assert.Equal(expected+" Studio",restored.ControlledBusiness.Name);
        Assert.Equal(state.Protagonist.Appearance,restored.Protagonist.Appearance);
        Assert.True(restored.Protagonist.IsProdigy);
        Assert.All(restored.Protagonist.Skills.Values,value=>Assert.Equal(95,value));
        Assert.Equal(restored.ToJson(),restored.ReplayTimeline().ToJson());
    }
    [Fact]
    public void Naming_does_not_change_world_generation_or_initial_gameplay()
    {
        var standard=GameState.NewGame(37);var custom=GameState.NewGame(37,protagonistName:"Ren");
        custom.Protagonist.Name=standard.Protagonist.Name;
        custom.ControlledBusiness.Name=standard.ControlledBusiness.Name;
        Assert.Equal(standard.ToJson(),custom.ToJson());
    }
    [Theory]
    [InlineData("Bad\nName")]
    [InlineData("Bad\0Name")]
    [InlineData("12345678901234567890123456789012345678901")]
    public void Opening_identity_rejects_invalid_names(string name)
    {
        Assert.Throws<ArgumentException>(()=>GameState.NewGame(protagonistName:name));
    }
}
