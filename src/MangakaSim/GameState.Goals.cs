namespace MangakaSim;

public sealed record GoalRecord(string Id, DateTime At, bool Backfilled);

/// <summary>The career goals board's saved state (spec 2026-10-01). Absent from saves written before it (see EnsureGoals).</summary>
public sealed class GoalsState
{
    public int Chapter { get; set; }
    public List<GoalRecord> Completed { get; set; } = new();
    public List<string> Unlocked { get; set; } = new();
    public int FreeConventionTables { get; set; }
    public bool PitchBoost { get; set; }
    public List<string> PendingScenes { get; set; } = new();
}

public partial class GameState
{
    /// <summary>Null only in saves written before the goals board, until EnsureGoals backfills it on load.</summary>
    public GoalsState? Goals { get; set; }

    public bool GoalDone(string id) => Goals?.Completed.Any(r => r.Id == id) == true;

    /// <summary>Checks the current chapter's goals, grants each reward once and opens the next chapter when all are done.
    /// A backfill (older saves) grants unlocks and decorations only, with no events.</summary>
    internal void EvaluateGoals(bool backfill = false)
    {
        if (Goals is not { } goals) return;
        // An older save counts every goal it has already met, in every chapter, so nothing pays out later (final review).
        if (backfill)
            foreach (var goal in GoalCatalog.Goals.Where(g => !goals.Completed.Any(r => r.Id == g.Id) && g.Measure(this).Done))
            {
                goals.Completed.Add(new(goal.Id, Clock.Now, true));
                GrantGoalReward(goal.Reward, true, "Goal reward: " + goal.Title);
            }
        var progressed = true;
        while (progressed && goals.Chapter < GoalCatalog.Chapters.Length)
        {
            progressed = false;
            foreach (var goal in GoalCatalog.In(goals.Chapter))
            {
                if (goals.Completed.Any(r => r.Id == goal.Id) || !goal.Measure(this).Done) continue;
                goals.Completed.Add(new(goal.Id, Clock.Now, backfill));
                GrantGoalReward(goal.Reward, backfill, "Goal reward: " + goal.Title);
                if (!backfill) Emit(EventType.GoalCompleted, $"Goal complete: {goal.Title}. Reward: {goal.Reward.Describe()}.", personId: ProtagonistPersonId);
            }
            if (!GoalCatalog.In(goals.Chapter).All(g => goals.Completed.Any(r => r.Id == g.Id))) break;
            var chapter = GoalCatalog.Chapters[goals.Chapter];
            GrantGoalReward(chapter.Reward, backfill, "Chapter reward: " + chapter.Name);
            if (!backfill)
            {
                var reward = chapter.Reward.Describe();
                Emit(EventType.GoalChapterCompleted, reward.Length > 0 ? $"Chapter complete: {chapter.Name}! Reward: {reward}." : $"Chapter complete: {chapter.Name}!", personId: ProtagonistPersonId);
                if (chapter.Reward.Scene is { } scene) QueueGoalScene(scene);
            }
            goals.Chapter++; progressed = true;
        }
    }

    private void GrantGoalReward(GoalReward reward, bool backfill, string reason)
    {
        var goals = Goals!;
        if (!backfill && reward.Cash > 0) AccountPost(ControlledBusiness.Account, reward.Cash, reason, AccountEntryKind.GoalReward);
        if (!backfill && reward.Fans > 0 && GoalTitles.OrderBy(s => s.Id).LastOrDefault() is { } newest) newest.Fanbase += reward.Fans;
        if (reward.Furniture is { } kind)
            Furniture.Add(new() { Id = NextFurnitureId++, Kind = kind, Owner = FurnitureOwner.Business, BusinessId = ControlledBusinessId, Paid = 0 });
        if (reward.Unlock is { } unlock && !goals.Unlocked.Contains(unlock)) goals.Unlocked.Add(unlock);
        if (!backfill && reward.FreeConventionTable) goals.FreeConventionTables++;
        if (!backfill && reward.PitchBoost) goals.PitchBoost = true;
    }

    /// <summary>Older saves have no goals: goals already met count as done, with their unlocks and decorations but no cash,
    /// fans, opportunities or events (spec 2026-10-01). The replay checkpoint is rebased so replays match the backfilled state.</summary>
    internal void EnsureGoals()
    {
        if (Goals is not null) return;
        Goals = new();
        EvaluateGoals(backfill: true);
        World.ReplayCheckpoint = null; World.ReplayLogStart = CommandLog.Count;
        ValidateSave(); World.ReplayCheckpoint = ToJson();
    }

    private void QueueGoalScene(string scene)
    {
        if (Career.PendingScene is null) Career.PendingScene = scene;
        else Goals!.PendingScenes.Add(scene);
    }

    private void ValidateGoals()
    {
        if (Goals is not { } g) return;
        static void Check(bool ok, string field) { if (!ok) throw new InvalidDataException($"Save file has invalid goals {field}."); }
        Check(g.Chapter >= 0 && g.Chapter <= GoalCatalog.Chapters.Length, "chapter");
        Check(g.Completed is not null && g.Completed.All(r => r is not null && GoalCatalog.Goals.Any(x => x.Id == r.Id)) &&
            g.Completed.Select(r => r.Id).Distinct().Count() == g.Completed.Count, "records");
        Check(g.Unlocked is not null && g.Unlocked.All(k => OfficeCatalog.Furniture.Any(f => f.Id == k)), "unlocks");
        Check(g.FreeConventionTables >= 0 && g.PendingScenes is not null && g.PendingScenes.All(HelperStories.Known), "opportunities");
    }
}
