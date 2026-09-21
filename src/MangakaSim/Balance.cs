namespace MangakaSim;

/// <summary>Tunable balance constants. Lives inside Settings so it is saved with the game.</summary>
public class Balance
{
    public Dictionary<Stage, double> BaseHoursPerPage { get; set; } = new()
    {
        [Stage.Name] = 1.2,
        [Stage.Pencils] = 1.5,
        [Stage.Inks] = 1.0,
        [Stage.Backgrounds] = 1.0,
        [Stage.Tones] = 0.5,
    };

    public int OvertimeCap { get; set; } = 2;

    public double MultiplierAtSkill0 { get; set; } = 0.5;
    public double MultiplierAtSkill50 { get; set; } = 1.0;
    public double MultiplierAtSkill100 { get; set; } = 2.0;

    public double SkillMultiplier(int skill)
    {
        var s = Math.Clamp(skill, 0, 100);
        if (s <= 50)
        {
            return MultiplierAtSkill0 + (MultiplierAtSkill50 - MultiplierAtSkill0) * (s / 50.0);
        }
        return MultiplierAtSkill50 + (MultiplierAtSkill100 - MultiplierAtSkill50) * ((s - 50) / 50.0);
    }

    public double HoursRequired(Stage stage, int pagesPerChapter) =>
        pagesPerChapter * BaseHoursPerPage[stage];
}
