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
}
