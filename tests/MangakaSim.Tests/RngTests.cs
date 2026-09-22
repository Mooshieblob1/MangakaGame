using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class RngTests
{
    [Fact]
    public void NextDouble_stays_in_unit_interval_and_is_deterministic()
    {
        var a = Rng.FromSeed(7);
        var b = Rng.FromSeed(7);
        for (var i = 0; i < 10_000; i++)
        {
            var x = a.NextDouble();
            Assert.InRange(x, 0.0, 0.9999999999999999);
            Assert.Equal(x, b.NextDouble());
        }
    }

    [Fact]
    public void NextDouble_range_stays_inside_bounds()
    {
        var rng = Rng.FromSeed(3);
        for (var i = 0; i < 1000; i++) Assert.InRange(rng.NextDouble(-0.02, 0.02), -0.02, 0.02);
    }

    [Fact]
    public void NextInt_inclusive_range_hits_every_value_and_nothing_else()
    {
        var rng = Rng.FromSeed(11);
        var seen = new HashSet<int>();
        for (var i = 0; i < 500; i++)
        {
            var v = rng.NextInt(1, 3);
            Assert.InRange(v, 1, 3);
            seen.Add(v);
        }
        Assert.Equal(new HashSet<int> { 1, 2, 3 }, seen);
        Assert.Equal(5, rng.NextInt(5, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(4, 3));
    }
}
