namespace MangakaSim.Rules;

public static class QualityRules
{
    public static double Weight(Stage stage) => stage switch
    {
        Stage.Name => .35, Stage.Pencils => .30, Stage.Inks => .15,
        Stage.Backgrounds => .10, Stage.Tones => .10, _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };
    public static double SkillFactor(int skill) => .2 + .008 * Math.Clamp(skill, 0, 100);
    public static double RushFactor(double overtime, double required) => Math.Max(.7, 1 - .5 * overtime / required);
    public static double Contribution(Stage stage, int skill, double overtime, double required, int redos) =>
        Weight(stage) * 100 * Math.Min(1, SkillFactor(skill) + (stage == Stage.Name ? .05 * redos : 0)) * RushFactor(overtime, required);
    public static int Total(IEnumerable<double> contributions) =>
        Math.Clamp((int)Math.Round(contributions.Sum(), MidpointRounding.AwayFromZero), 0, 100);
}
