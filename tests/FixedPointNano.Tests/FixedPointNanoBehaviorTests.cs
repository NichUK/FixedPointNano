using System;
using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoBehaviorTests
{
    [TestCase(1.5, 2.0, Description = "Positive fractional rounds up")]
    [TestCase(1.0, 1.0, Description = "Exact integer is unchanged")]
    [TestCase(0.1, 1.0, Description = "Small positive fraction rounds up")]
    [TestCase(-1.5, -1.0, Description = "Negative fractional rounds toward zero")]
    [TestCase(-1.0, -1.0, Description = "Negative exact integer is unchanged")]
    [TestCase(-1.9, -1.0, Description = "Negative near-integer rounds toward zero")]
    public void CeilingShouldRoundTowardPositiveInfinity(double input, double expected)
    {
        var value = FixedPointNano.FromDouble(input);
        Assert.That(FixedPointNano.Ceiling(value).ToDouble(), Is.EqualTo(expected));
    }

    [TestCase(1.9, 1.0, Description = "Positive fractional rounds down")]
    [TestCase(1.0, 1.0, Description = "Exact integer is unchanged")]
    [TestCase(0.9, 0.0, Description = "Positive fraction below one floors to zero")]
    [TestCase(-1.1, -2.0, Description = "Negative fractional rounds away from zero")]
    [TestCase(-1.0, -1.0, Description = "Negative exact integer is unchanged")]
    [TestCase(-0.5, -1.0, Description = "Negative fraction floors to minus one")]
    public void FloorShouldRoundTowardNegativeInfinity(double input, double expected)
    {
        var value = FixedPointNano.FromDouble(input);
        Assert.That(FixedPointNano.Floor(value).ToDouble(), Is.EqualTo(expected));
    }

    [TestCase(1.9, 1.0, Description = "Positive fractional truncates toward zero")]
    [TestCase(1.0, 1.0, Description = "Exact integer is unchanged")]
    [TestCase(-1.9, -1.0, Description = "Negative fractional truncates toward zero")]
    [TestCase(-1.0, -1.0, Description = "Negative exact integer is unchanged")]
    public void TruncateShouldRoundTowardZero(double input, double expected)
    {
        var value = FixedPointNano.FromDouble(input);
        Assert.That(FixedPointNano.Truncate(value).ToDouble(), Is.EqualTo(expected));
    }

    [Test]
    public void RoundShouldSupportAllMidpointRoundingModes()
    {
        var midpoint = FixedPointNano.FromDecimal(0.5m);
        var negativeMidpoint = FixedPointNano.FromDecimal(-0.5m);
        var twoPointFive = FixedPointNano.FromDecimal(2.5m);

        Assert.Multiple(() =>
        {
            Assert.That(FixedPointNano.Round(midpoint, 0, MidpointRounding.ToEven).ToDecimal(), Is.EqualTo(0m));
            Assert.That(FixedPointNano.Round(twoPointFive, 0, MidpointRounding.ToEven).ToDecimal(), Is.EqualTo(2m));

            Assert.That(FixedPointNano.Round(midpoint, 0, MidpointRounding.AwayFromZero).ToDecimal(), Is.EqualTo(1m));
            Assert.That(FixedPointNano.Round(negativeMidpoint, 0, MidpointRounding.AwayFromZero).ToDecimal(), Is.EqualTo(-1m));

            Assert.That(FixedPointNano.Round(midpoint, 0, MidpointRounding.ToZero).ToDecimal(), Is.EqualTo(0m));
            Assert.That(FixedPointNano.Round(negativeMidpoint, 0, MidpointRounding.ToZero).ToDecimal(), Is.EqualTo(0m));

            Assert.That(FixedPointNano.Round(midpoint, 0, MidpointRounding.ToPositiveInfinity).ToDecimal(), Is.EqualTo(1m));
            Assert.That(FixedPointNano.Round(negativeMidpoint, 0, MidpointRounding.ToPositiveInfinity).ToDecimal(), Is.EqualTo(0m));

            Assert.That(FixedPointNano.Round(midpoint, 0, MidpointRounding.ToNegativeInfinity).ToDecimal(), Is.EqualTo(0m));
            Assert.That(FixedPointNano.Round(negativeMidpoint, 0, MidpointRounding.ToNegativeInfinity).ToDecimal(), Is.EqualTo(-1m));
        });
    }

    [Test]
    public void RoundShouldSupportBoundaryDecimalCounts()
    {
        var value = FixedPointNano.FromDecimal(1.123456789m);

        Assert.That(FixedPointNano.Round(value, 0).ToDecimal(), Is.EqualTo(1m));
        Assert.That(FixedPointNano.Round(value, 9).ToDecimal(), Is.EqualTo(1.123456789m));
    }

    [Test]
    public void MaxAndMinShouldHandleEqualValues()
    {
        var a = FixedPointNano.FromDecimal(3.14m);
        var b = FixedPointNano.FromDecimal(3.14m);

        Assert.That(FixedPointNano.Max(a, b), Is.EqualTo(a));
        Assert.That(FixedPointNano.Min(a, b), Is.EqualTo(b));
    }

    [Test]
    public void SqrtShouldReturnZeroForZeroInput()
    {
        Assert.That(FixedPointNano.Sqrt(FixedPointNano.Zero), Is.EqualTo(FixedPointNano.Zero));
    }

    [Test]
    public void AbsShouldReturnSameValueForZeroAndPositive()
    {
        Assert.That(FixedPointNano.Abs(FixedPointNano.Zero), Is.EqualTo(FixedPointNano.Zero));
        Assert.That(FixedPointNano.Abs(FixedPointNano.One), Is.EqualTo(FixedPointNano.One));
    }
}
