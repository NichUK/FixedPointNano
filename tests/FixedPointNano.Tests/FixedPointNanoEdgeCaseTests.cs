using System;
using Seerstone;

namespace Seerstone.Tests;

/// <summary>
/// Documents edge-case and boundary behaviors for <see cref="FixedPointNano"/> that complement
/// the core round-trip and comparison tests.
/// </summary>
[TestFixture]
public sealed class FixedPointNanoEdgeCaseTests
{
    // -----------------------------------------------------------------------------------------
    // Abs
    // -----------------------------------------------------------------------------------------

    [Test]
    public void AbsOfMinRawValueThrowsOverflowException()
    {
        // long.MinValue cannot be negated in checked arithmetic (-long.MinValue overflows).
        var minRaw = FixedPointNano.FromRaw(long.MinValue);
        Assert.That(() => FixedPointNano.Abs(minRaw), Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void AbsOfZeroReturnsZero()
    {
        Assert.That(FixedPointNano.Abs(FixedPointNano.Zero).RawValue, Is.EqualTo(0L));
    }

    // -----------------------------------------------------------------------------------------
    // Divide (int overload)
    // -----------------------------------------------------------------------------------------

    [Test]
    public void DivideByIntZeroThrowsDivideByZeroException()
    {
        Assert.That(
            () => FixedPointNano.Divide(FixedPointNano.One, 0),
            Throws.TypeOf<DivideByZeroException>());
    }

    [Test]
    public void DivideByIntProducesCorrectResult()
    {
        var value = FixedPointNano.FromDecimal(10m);
        var result = FixedPointNano.Divide(value, (int)4);
        Assert.That(result.ToDecimal(), Is.EqualTo(2.5m));
    }

    // -----------------------------------------------------------------------------------------
    // Round at boundary decimal places
    // -----------------------------------------------------------------------------------------

    [Test]
    public void RoundAtZeroDecimalPlacesRoundsToInteger()
    {
        var value = FixedPointNano.FromDecimal(1.5m);

        // Banker's rounding: 1.5 → 2 (round half to even)
        Assert.That(FixedPointNano.Round(value, 0).ToDecimal(), Is.EqualTo(2m));

        var value2 = FixedPointNano.FromDecimal(2.5m);

        // Banker's rounding: 2.5 → 2 (round half to even)
        Assert.That(FixedPointNano.Round(value2, 0).ToDecimal(), Is.EqualTo(2m));
    }

    [Test]
    public void RoundAtMaxDecimalPlacesIsIdentity()
    {
        var value = FixedPointNano.FromDecimal(1.123456789m);
        var result = FixedPointNano.Round(value, FixedPointNano.DecimalPlaces);
        Assert.That(result.RawValue, Is.EqualTo(value.RawValue));
    }

    [Test]
    public void RoundNegativeValueAtZeroDecimalPlaces()
    {
        // -1.5 with ToEven → -2 (rounds away from zero, same as positive tie case)
        var value = FixedPointNano.FromDecimal(-1.5m);
        Assert.That(FixedPointNano.Round(value, 0).ToDecimal(), Is.EqualTo(-2m));

        // -2.5 with ToEven → -2 (rounds to even, same magnitude as 2.5 → 2)
        var value2 = FixedPointNano.FromDecimal(-2.5m);
        Assert.That(FixedPointNano.Round(value2, 0).ToDecimal(), Is.EqualTo(-2m));
    }

    // -----------------------------------------------------------------------------------------
    // Ceiling / Floor on exact integers
    // -----------------------------------------------------------------------------------------

    [Test]
    public void CeilingOfExactIntegerIsUnchanged()
    {
        var value = FixedPointNano.FromDecimal(3m);
        Assert.That(FixedPointNano.Ceiling(value).ToDecimal(), Is.EqualTo(3m));
    }

    [Test]
    public void FloorOfExactIntegerIsUnchanged()
    {
        var value = FixedPointNano.FromDecimal(-5m);
        Assert.That(FixedPointNano.Floor(value).ToDecimal(), Is.EqualTo(-5m));
    }

    [Test]
    public void FloorOfNegativeFractionalRoundsAwayFromZero()
    {
        // Floor(-1.1) = -2, not -1
        var value = FixedPointNano.FromDecimal(-1.1m);
        Assert.That(FixedPointNano.Floor(value).ToDecimal(), Is.EqualTo(-2m));
    }

    [Test]
    public void CeilingOfNegativeFractionalRoundsTowardZero()
    {
        // Ceiling(-1.9) = -1, not -2
        var value = FixedPointNano.FromDecimal(-1.9m);
        Assert.That(FixedPointNano.Ceiling(value).ToDecimal(), Is.EqualTo(-1m));
    }

    // -----------------------------------------------------------------------------------------
    // FromDouble near boundary
    // -----------------------------------------------------------------------------------------

    [Test]
    public void FromDoubleLargeValidValueSucceeds()
    {
        // 9_000_000_000.0 * Scale < long.MaxValue, so this should succeed.
        var value = FixedPointNano.FromDouble(9_000_000_000.0);
        Assert.That(value.ToDouble(), Is.EqualTo(9_000_000_000.0).Within(1e-4));
    }

    [Test]
    public void FromDoubleScaledOverflowThrows()
    {
        // double.MaxValue * Scale overflows to infinity, triggering OverflowException.
        Assert.That(() => FixedPointNano.FromDouble(double.MaxValue), Throws.TypeOf<OverflowException>());
    }

    // -----------------------------------------------------------------------------------------
    // Truncate with negative values
    // -----------------------------------------------------------------------------------------

    [Test]
    public void TruncateNegativeFractionalRoundsTowardZero()
    {
        // Truncate(-1.9) = -1 (toward zero, not floor)
        var value = FixedPointNano.FromDecimal(-1.9m);
        Assert.That(FixedPointNano.Truncate(value).ToDecimal(), Is.EqualTo(-1m));
    }

    [Test]
    public void TruncatePositiveFractionalRoundsTowardZero()
    {
        var value = FixedPointNano.FromDecimal(1.9m);
        Assert.That(FixedPointNano.Truncate(value).ToDecimal(), Is.EqualTo(1m));
    }

    // -----------------------------------------------------------------------------------------
    // ToString(string?) uses CurrentCulture
    // -----------------------------------------------------------------------------------------

    [Test]
    public void ToStringWithNullFormatUsesCurrentCulture()
    {
        var value = FixedPointNano.FromDecimal(1.5m);
        // ToString(string? format) uses CultureInfo.CurrentCulture
        var result = value.ToString((string?)null);
        Assert.That(result, Is.EqualTo(value.ToDecimal().ToString((string?)null)));
    }

    // -----------------------------------------------------------------------------------------
    // Operator negation overflow
    // -----------------------------------------------------------------------------------------

    [Test]
    public void UnaryNegationOfMinRawValueThrowsOverflowException()
    {
        var minRaw = FixedPointNano.FromRaw(long.MinValue);
        Assert.That(() => _ = -minRaw, Throws.TypeOf<OverflowException>());
    }

    // -----------------------------------------------------------------------------------------
    // PopulationVariance: inconsistent sum/squares
    // -----------------------------------------------------------------------------------------

    [Test]
    public void PopulationVarianceWithInconsistentDataThrows()
    {
        // sum=10, sumOfRawSquares=0, count=1 → variance cannot be negative
        var sum = FixedPointNano.FromDecimal(10m);
        Assert.That(
            () => FixedPointNano.PopulationVariance(sum, Int128.Zero, 1),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void PopulationVarianceWithCountOneAndMatchingSumReturnsZero()
    {
        // For a single value, population variance = 0
        var value = FixedPointNano.FromDecimal(5m);
        var sumOfRawSquares = (Int128)value.RawValue * value.RawValue;
        var result = FixedPointNano.PopulationVariance(value, sumOfRawSquares, 1);
        Assert.That(result.ToDecimal(), Is.EqualTo(0m));
    }
}
