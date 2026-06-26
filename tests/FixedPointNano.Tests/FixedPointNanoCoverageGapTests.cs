using System;
using Seerstone;

namespace Seerstone.Tests;

/// <summary>
/// Tests for public API members and code paths not exercised by other test fixtures.
/// </summary>
[TestFixture]
public sealed class FixedPointNanoCoverageGapTests
{
    // -----------------------------------------------------------------------------------------
    // Frac — public alias for FractionalPart; same implementation, needs independent coverage
    // -----------------------------------------------------------------------------------------

    [Test]
    public void FracShouldReturnDecimalComponent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixedPointNano.Frac(FixedPointNano.Zero).RawValue, Is.EqualTo(0L));
            Assert.That(FixedPointNano.Frac(FixedPointNano.One).RawValue, Is.EqualTo(0L));
            Assert.That(FixedPointNano.Frac(FixedPointNano.FromDecimal(1.25m)).ToDecimal(), Is.EqualTo(0.25m));
            Assert.That(FixedPointNano.Frac(FixedPointNano.FromDecimal(3.75m)).ToDecimal(), Is.EqualTo(0.75m));
            Assert.That(FixedPointNano.Frac(FixedPointNano.FromDecimal(-1.25m)).ToDecimal(), Is.EqualTo(-0.25m));
            Assert.That(FixedPointNano.Frac(FixedPointNano.FromDecimal(-3.75m)).ToDecimal(), Is.EqualTo(-0.75m));
        });
    }

    [Test]
    public void FracShouldReturnSameResultAsFractionalPart()
    {
        var values = new[]
        {
            FixedPointNano.Zero,
            FixedPointNano.One,
            FixedPointNano.NegativeOne,
            FixedPointNano.Epsilon,
            FixedPointNano.FromDecimal(1.123456789m),
            FixedPointNano.FromDecimal(-1.123456789m),
        };

        foreach (var v in values)
        {
            Assert.That(FixedPointNano.Frac(v).RawValue, Is.EqualTo(FixedPointNano.FractionalPart(v).RawValue),
                $"Frac and FractionalPart should agree for {v}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // Pow — negative exponent rejection
    // -----------------------------------------------------------------------------------------

    [Test]
    public void PowShouldThrowForNegativeExponent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => FixedPointNano.Pow(FixedPointNano.One, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => FixedPointNano.Pow(FixedPointNano.FromDecimal(2m), -5),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    // -----------------------------------------------------------------------------------------
    // ++ / -- overflow at boundary raw values
    // -----------------------------------------------------------------------------------------

    [Test]
    public void IncrementShouldThrowWhenMaxValueWouldOverflow()
    {
        // The largest safely-incrementable raw value is long.MaxValue - Scale.
        var safeToIncrement = FixedPointNano.FromRaw(long.MaxValue - FixedPointNano.Scale);
        var incremented = ++safeToIncrement;
        Assert.That(incremented.RawValue, Is.EqualTo(long.MaxValue));

        // MaxValue itself cannot be incremented.
        Assert.That(
            () =>
            {
                var v = FixedPointNano.MaxValue;
                v++;
                return v;
            },
            Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void DecrementShouldThrowWhenMinValueWouldOverflow()
    {
        // MinValue.RawValue - Scale overflows long.
        Assert.That(
            () =>
            {
                var v = FixedPointNano.MinValue;
                v--;
                return v;
            },
            Throws.TypeOf<OverflowException>());
    }
}
