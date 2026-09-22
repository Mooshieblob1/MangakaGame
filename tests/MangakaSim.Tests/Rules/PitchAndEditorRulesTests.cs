using MangakaSim;
using Xunit;

namespace MangakaSim.Tests.Rules;

public class PitchAndEditorRulesTests
{
    [Fact]
    public void Pitch_chance_multiplies_factors_and_clamps()
    {
        Assert.Equal(1.248, PitchRules.QualityFactor(84), 9);
        Assert.Equal(0.85, PitchRules.ReputationFactor(25), 9);
        Assert.Equal(0.15 * 1.248 * 0.85 * 1.2 * 1.3, PitchRules.Chance(1, 84, 25, 1.2, 1.3), 9);
        Assert.Equal(0.248227, PitchRules.Chance(1, 84, 25, 1.2, 1.3), 6);
        Assert.Equal(0.95, PitchRules.Chance(3, 100, 100, 1.25, 1.8), 9);
        Assert.Equal(0.28, PitchRules.QualityFactor(40), 9);
        Assert.Equal(0.02, PitchRules.Chance(1, 40, 0, 0.8, 0.25), 9);
    }

    [Fact]
    public void Weakest_factor_names_the_smallest_term()
    {
        Assert.Equal("reputation", PitchRules.WeakestFactor(84, 25, 1.2, 1.3));
        Assert.Equal("fit", PitchRules.WeakestFactor(84, 90, 0.8, 1.3));
        Assert.Equal("trend", PitchRules.WeakestFactor(84, 90, 1.2, 0.7));
        Assert.Equal("quality", PitchRules.WeakestFactor(40, 90, 1.2, 1.3));
    }

    [Fact]
    public void Fee_interpolates_the_magazine_range_and_rounds_to_hundreds()
    {
        var jump = PublisherCatalog.LoadDefault().Require("tokiwa-jump");
        Assert.Equal(14_500, PitchRules.FeePerPage(jump, 50, 1.00));
        Assert.Equal(17_100, PitchRules.FeePerPage(jump, 50, 1.18));
        Assert.Equal(9_000, PitchRules.FeePerPage(jump, 0, 1.00));
        Assert.Equal(20_000, PitchRules.FeePerPage(jump, 100, 1.00));
    }

    [Fact]
    public void Editor_threshold_and_chance()
    {
        Assert.Equal(65, EditorRules.Threshold(1, 0), 9);
        Assert.Equal(50, EditorRules.Threshold(1, 100), 9);
        Assert.Equal(47.5, EditorRules.Threshold(2, 50), 9);
        Assert.Equal(45, EditorRules.Threshold(3, 0), 9);
        Assert.Equal(0.5, EditorRules.ApproveChance(65, 65), 9);
        Assert.Equal(0.95, EditorRules.ApproveChance(85, 65), 9);
        Assert.Equal(0.05, EditorRules.ApproveChance(45, 65), 9);
        Assert.Equal(0.95, EditorRules.ApproveChance(100, 30), 9);
    }

    [Fact]
    public void Review_hours_by_tier()
    {
        Assert.Equal(48, EditorRules.ReviewHours(1));
        Assert.Equal(36, EditorRules.ReviewHours(2));
        Assert.Equal(24, EditorRules.ReviewHours(3));
    }
}
