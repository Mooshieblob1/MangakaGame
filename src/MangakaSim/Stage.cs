namespace MangakaSim;

public enum Stage
{
    Name = 0,
    Pencils = 1,
    Inks = 2,
    Backgrounds = 3,
    Tones = 4,
}

public static class StageOrder
{
    public static readonly Stage[] All =
        { Stage.Name, Stage.Pencils, Stage.Inks, Stage.Backgrounds, Stage.Tones };

    public static Stage? Next(Stage stage)
    {
        var index = Array.IndexOf(All, stage);
        return index < All.Length - 1 ? All[index + 1] : null;
    }

    /// <summary>Stages that must be Complete or Skipped before this one can start: Name -> Pencils -> {Inks, Backgrounds} -> Tones.</summary>
    public static IReadOnlyList<Stage> Prerequisites(Stage stage) => stage switch
    {
        Stage.Name => Array.Empty<Stage>(),
        Stage.Pencils => new[] { Stage.Name },
        Stage.Inks => new[] { Stage.Pencils },
        Stage.Backgrounds => new[] { Stage.Pencils },
        Stage.Tones => new[] { Stage.Inks, Stage.Backgrounds },
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null),
    };
}
