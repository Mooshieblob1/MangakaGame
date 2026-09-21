using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class StageTests
{
    [Fact]
    public void Stages_are_in_pipeline_order()
    {
        Assert.Equal(
            new[] { Stage.Name, Stage.Pencils, Stage.Inks, Stage.Backgrounds, Stage.Tones },
            StageOrder.All);
    }

    [Fact]
    public void Next_returns_following_stage_and_null_after_last()
    {
        Assert.Equal(Stage.Pencils, StageOrder.Next(Stage.Name));
        Assert.Equal(Stage.Tones, StageOrder.Next(Stage.Backgrounds));
        Assert.Null(StageOrder.Next(Stage.Tones));
    }
}
