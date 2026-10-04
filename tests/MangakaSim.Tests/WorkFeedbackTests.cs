using Xunit;
namespace MangakaSim.Tests;

// Work feedback (2026-10-03): sparkles from workers into the progress bar and "stage done" bubbles. Presentation only.
public class WorkFeedbackTests
{
    private static int Run(WorkFeedbackPlan plan, double seconds, double speed, params int[] workers)
    {
        var total = 0;
        for (var t = 0.0; t < seconds - 1e-9; t += .1) total += plan.Sparkles(.1, speed, workers).Count;
        return total;
    }

    [Fact] public void No_sparkles_while_paused_or_without_workers()
    {
        var plan = new WorkFeedbackPlan();
        Assert.Equal(0, Run(plan, 10, 0, 1, 2));
        Assert.Equal(0, Run(plan, 10, 8));
    }

    [Fact] public void Each_worker_sparkles_at_the_8x_rate()
    {
        var plan = new WorkFeedbackPlan();
        var sparkles = plan.Sparkles(10, 8, [1, 2]);
        Assert.Equal(new[] { 1, 1, 1, 2, 2, 2 }, sparkles.OrderBy(i => i)); // a long frame never bursts more than 3 per worker
        var steady = Run(new WorkFeedbackPlan(), 10, 8, 1);
        Assert.InRange(steady, 40, 42); // 3 per second over a whole day, so about 4.1 per seated second (73% at the desk)
    }

    // Sparkles flow only while seated at the desk (2026-10-03). People spend about 27% of work time on visual toilet and
    // break-room trips, so the seated rate is raised to keep the same number of sparkles across a working day.
    [Fact] public void Seated_rate_makes_up_for_the_time_away_from_the_desk()
    {
        Assert.Equal(.73, WorkFeedbackPlan.DeskShare);
        Assert.Equal(WorkFeedbackPlan.SparklesPerSecondAt8x / WorkFeedbackPlan.DeskShare, WorkFeedbackPlan.SparkleRate(8), 10);
        Assert.Equal(WorkFeedbackPlan.SparklesPerSecondAt8x, WorkFeedbackPlan.SparkleRate(8) * WorkFeedbackPlan.DeskShare, 10);
    }

    [Fact] public void Faster_speeds_sparkle_more_but_stop_growing_at_32x()
    {
        Assert.True(WorkFeedbackPlan.SparkleRate(32) > WorkFeedbackPlan.SparkleRate(8));
        Assert.True(WorkFeedbackPlan.SparkleRate(8) > WorkFeedbackPlan.SparkleRate(1));
        Assert.Equal(WorkFeedbackPlan.SparkleRate(32), WorkFeedbackPlan.SparkleRate(64));
    }

    [Fact] public void A_stage_finishing_queues_one_bubble()
    {
        var plan = new WorkFeedbackPlan();
        plan.Observe(1, [(10, Stage.Name, true), (10, Stage.Pencils, false)]);
        Assert.Null(plan.NextBubble(1));
        plan.Observe(1, [(10, Stage.Name, true), (10, Stage.Pencils, true)]);
        Assert.Equal(Stage.Pencils, plan.NextBubble(1));
        Assert.Null(plan.NextBubble(1));
    }

    [Fact] public void Loading_a_series_or_a_new_chapter_never_claims_finished_work()
    {
        var plan = new WorkFeedbackPlan();
        plan.Observe(1, [(10, Stage.Name, true), (10, Stage.Pencils, true)]);
        plan.Observe(2, [(20, Stage.Name, true)]);                       // switched series
        plan.Observe(2, [(20, Stage.Name, true), (21, Stage.Name, true)]); // a chapter that arrived already drawn
        Assert.Null(plan.NextBubble(5));
        plan.Reset();
        plan.Observe(2, [(20, Stage.Name, true)]);
        Assert.Null(plan.NextBubble(5));
    }

    [Fact] public void Bubbles_wait_a_second_apart_and_old_ones_are_dropped()
    {
        var plan = new WorkFeedbackPlan();
        plan.Observe(1, [(10, Stage.Inks, false), (10, Stage.Backgrounds, false), (10, Stage.Tones, false)]);
        Assert.Null(plan.NextBubble(1));
        plan.Observe(1, [(10, Stage.Inks, true), (10, Stage.Backgrounds, true), (10, Stage.Tones, true)]);
        Assert.Equal(Stage.Inks, plan.NextBubble(.1));
        Assert.Null(plan.NextBubble(.5));
        Assert.Equal(Stage.Backgrounds, plan.NextBubble(.5));
        Assert.Null(plan.NextBubble(5)); // Tones waited longer than 3 seconds, so it is stale
    }
}
