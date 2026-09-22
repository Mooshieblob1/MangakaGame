namespace MangakaSim;

public static class AssignmentRules
{
    public const double LeadPencilsBacklogHours = 40;

    /// <summary>Skill multiplier discounted by the person's queue: 20 queued hours halve the score.</summary>
    public static double Score(double skillMultiplier, double queueHours) => skillMultiplier / (1 + queueHours / 20.0);

    /// <summary>The highest score; ties go to the lower id.</summary>
    public static int? Best(IEnumerable<(int PersonId, double Score)> candidates) =>
        candidates.OrderByDescending(c => c.Score).ThenBy(c => c.PersonId).Select(c => (int?)c.PersonId).FirstOrDefault();
}
