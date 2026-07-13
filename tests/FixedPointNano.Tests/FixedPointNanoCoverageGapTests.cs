using System;
using Seerstone;

namespace Seerstone.Tests;

/// <summary>
/// Covers behaviours not exercised by the other test fixtures:
/// <c>Frac</c>, <c>Pow</c> negative-exponent guard, <c>Divide(long)</c> zero guard,
/// and <c>Clamp</c> invalid-range guard.
/// </summary>
[TestFixture]
public sealed class FixedPointNanoCoverageGapTests
{
    // -----------------------------------------------------------------------------------------
    // Frac
    // -----------------------------------------------------------------------------------------

    [TestCase(1.75, 0.75, Description = "Positive fractional part")]
    [TestCase(1.0, 0.0, Description = "Exact integer has zero Frac")]
    [TestCase(-1.25, -0.25, Description = "Negative fractional part keeps sign")]
    [TestCase(0.0, 0.0, Description = "Zero has zero Frac")]
    public void FracShouldReturnFractionalPart(double input, double expected)
    {
        var value = FixedPointNano.FromDouble(input);
        Assert.That(FixedPointNano.Frac(value).ToDouble(), Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void FracShouldMatchFractionalPart()
    {
        var values = new[] { 1.5, -3.25, 0.0, 42.0, -0.999999999 };
        foreach (var d in values)
        {
            var v = FixedPointNano.FromDouble(d);
            Assert.That(FixedPointNano.Frac(v), Is.EqualTo(FixedPointNano.FractionalPart(v)),
                $"Frac and FractionalPart differ for {d}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // Pow – negative exponent guard
    // -----------------------------------------------------------------------------------------

    [Test]
    public void PowShouldThrowForNegativeExponent()
    {
        Assert.That(
            () => FixedPointNano.Pow(FixedPointNano.One, -1),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void PowZeroExponentShouldReturnOne()
    {
        var value = FixedPointNano.FromDecimal(7.5m);
        Assert.That(FixedPointNano.Pow(value, 0), Is.EqualTo(FixedPointNano.One));
    }

    // -----------------------------------------------------------------------------------------
    // Divide(FixedPointNano, long) – zero-divisor guard
    // -----------------------------------------------------------------------------------------

    [Test]
    public void DivideByLongZeroShouldThrowDivideByZeroException()
    {
        Assert.That(
            () => FixedPointNano.Divide(FixedPointNano.One, 0L),
            Throws.TypeOf<DivideByZeroException>());
    }

    // -----------------------------------------------------------------------------------------
    // Clamp – invalid range guard
    // -----------------------------------------------------------------------------------------

    [Test]
    public void ClampShouldThrowWhenMinExceedsMax()
    {
        var min = FixedPointNano.FromDecimal(5m);
        var max = FixedPointNano.FromDecimal(1m);
        Assert.That(
            () => FixedPointNano.Clamp(FixedPointNano.Zero, min, max),
            Throws.TypeOf<ArgumentException>());
    }
}
