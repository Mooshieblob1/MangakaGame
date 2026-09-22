using MangakaSim;
using Xunit;

namespace MangakaSim.Tests.Rules;

public class QualityRulesTests
{
    private static double Clean(Stage stage, int skill = 80) => QualityRules.Contribution(stage, skill, 0, 100, 0);

    [Fact]
    public void Skill_factor_is_piecewise_linear_with_capped_redo_bonus()
    {
        Assert.Equal(0.2, QualityRules.SkillFactor(0), 9);
        Assert.Equal(0.6, QualityRules.SkillFactor(50), 9);
        Assert.Equal(0.84, QualityRules.SkillFactor(80), 9);
        Assert.Equal(1.0, QualityRules.SkillFactor(100), 9);
        Assert.Equal(0.89, QualityRules.SkillFactor(80, 1), 9);
        Assert.Equal(1.0, QualityRules.SkillFactor(80, 4), 9);
        Assert.Equal(0.2, QualityRules.SkillFactor(-5), 9);
    }

    [Fact]
    public void Aki_solo_clean_chapter_is_84()
    {
        var contributions = StageOrder.All.Select(s => Clean(s));
        Assert.Equal(84, QualityRules.Quality(contributions));
        Assert.Equal(29.4, Clean(Stage.Name), 9);
    }

    [Fact]
    public void Skipped_tones_loses_its_contribution()
    {
        // The spec's example says 74, but its own formula gives 84 - 0.10 x 100 x 0.84 = 75.6 -> 76.
        var contributions = StageOrder.All.Select(s => s == Stage.Tones ? 0 : Clean(s));
        Assert.Equal(76, QualityRules.Quality(contributions));
    }

    [Fact]
    public void Twenty_percent_overtime_on_pencils_removes_ten_percent_of_that_stage()
    {
        Assert.Equal(0.9, QualityRules.RushFactor(20, 100), 9);
        Assert.Equal(0.7, QualityRules.RushFactor(100, 100), 9);
        Assert.Equal(1.0, QualityRules.RushFactor(0, 100), 9);
        var pencils = QualityRules.Contribution(Stage.Pencils, 80, 20, 100, 0);
        Assert.Equal(30 * 0.84 * 0.9, pencils, 9);
        var contributions = StageOrder.All.Select(s => s == Stage.Pencils ? pencils : Clean(s));
        Assert.Equal(81, QualityRules.Quality(contributions)); // 84 - 2.52 = 81.48
    }

    [Fact]
    public void Redo_bonus_applies_to_name_only()
    {
        Assert.Equal(35 * 0.89, QualityRules.Contribution(Stage.Name, 80, 0, 100, 1), 9);
        Assert.Equal(30 * 0.84, QualityRules.Contribution(Stage.Pencils, 80, 0, 100, 1), 9);
        Assert.Equal(84.0, QualityRules.NameQuality(QualityRules.Contribution(Stage.Name, 80, 0, 100, 0)), 9);
    }

    [Fact]
    public void Quality_clamps_and_rounds()
    {
        Assert.Equal(0, QualityRules.Quality(new[] { -3.0 }));
        Assert.Equal(100, QualityRules.Quality(new[] { 120.0 }));
        Assert.Equal(51, QualityRules.Quality(new[] { 50.5 }));
    }
}
