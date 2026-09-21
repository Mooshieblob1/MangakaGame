namespace MangakaSim;

/// <summary>SplitMix64. State is public so it serializes and a loaded game continues identically.</summary>
public class Rng
{
    public ulong State { get; set; }

    public static Rng FromSeed(int seed) => new() { State = unchecked((ulong)seed * 0x9E3779B97F4A7C15UL) };

    public ulong NextUInt64()
    {
        unchecked
        {
            State += 0x9E3779B97F4A7C15UL;
            var z = State;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        return (int)(NextUInt64() % (ulong)maxExclusive);
    }
}
