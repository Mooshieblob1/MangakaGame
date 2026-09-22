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

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        return (int)(minInclusive + (long)(NextUInt64() % (ulong)((long)maxExclusive - minInclusive)));
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public double NextDouble(double min, double max)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min >= max || !double.IsFinite(max - min))
            throw new ArgumentOutOfRangeException(nameof(max));
        return min + (max - min) * NextDouble();
    }
}
