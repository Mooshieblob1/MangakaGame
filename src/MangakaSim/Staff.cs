namespace MangakaSim;

public enum PersonRole { Mangaka, Assistant }

/// <summary>Hunger, thirst and comfort, 0..100 where 100 is satisfied.</summary>
public class Needs
{
    public double Hunger { get; set; } = 100;
    public double Thirst { get; set; } = 100;
    public double Comfort { get; set; } = 100;

    public double Min => Math.Min(Hunger, Math.Min(Thirst, Comfort));

    public double Get(string need) => need switch
    {
        "hunger" => Hunger,
        "thirst" => Thirst,
        "comfort" => Comfort,
        _ => throw new ArgumentOutOfRangeException(nameof(need), need, null),
    };

    public void Set(string need, double value)
    {
        switch (need)
        {
            case "hunger": Hunger = value; break;
            case "thirst": Thirst = value; break;
            case "comfort": Comfort = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(need), need, null);
        }
    }

    /// <summary>The need with the lowest value; ties go to hunger, then thirst.</summary>
    public string Lowest => Hunger <= Thirst && Hunger <= Comfort ? "hunger" : Thirst <= Comfort ? "thirst" : "comfort";

    public Needs Clone() => new() { Hunger = Hunger, Thirst = Thirst, Comfort = Comfort };
}

public class Candidate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<Stage, int> Skills { get; set; } = new();
    /// <summary>Nominal yen per month.</summary>
    public int AskingSalary { get; set; }
    public DateTime AvailableUntil { get; set; }
    public bool IsScheduled { get; set; }
    /// <summary>A former assistant back on the market; not counted against the pool size.</summary>
    public bool IsReturning { get; set; }
    public string? Note { get; set; }

    public int Skill(Stage stage) => Skills.TryGetValue(stage, out var v) ? v : 0;
}

public class StudioState
{
    public string PremisesId { get; set; } = "garage";
    public List<string> Amenities { get; set; } = new();
    public DateTime MovedInAt { get; set; }
    public int MissedPayrolls { get; set; }
}

public record PersonMood(int PersonId, string Name, double Happiness, double Fatigue, int Breaks, bool IsMoonlighting);

/// <summary>A departed assistant remembered for the returning-candidate rule.</summary>
public class FormerStaffNote
{
    public int PersonId { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<Stage, int> Skills { get; set; } = new();
    public int Salary { get; set; }
    public DateTime LeftAt { get; set; }
    public bool Quit { get; set; }
    public bool Returned { get; set; }
}
