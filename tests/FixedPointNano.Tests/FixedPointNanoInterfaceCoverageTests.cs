using System;
using System.Numerics;
using Seerstone;

namespace Seerstone.Tests;

/// <summary>
/// Tests for generic math interface implementations that were previously uncovered.
/// Focuses on INumberBase static abstract members accessed through type parameters.
/// </summary>
[TestFixture]
public sealed class FixedPointNanoInterfaceCoverageTests
{
    // Helper methods to access interface members through type parameters
    private static T CallAbsViaInterface<T>(T v) where T : INumberBase<T> => T.Abs(v);
    private static bool CallIsIntegerViaInterface<T>(T v) where T : INumberBase<T> => T.IsInteger(v);
    private static bool CallIsNegativeViaInterface<T>(T v) where T : INumberBase<T> => T.IsNegative(v);
    private static bool CallIsPositiveViaInterface<T>(T v) where T : INumberBase<T> => T.IsPositive(v);
    private static bool CallIsZeroViaInterface<T>(T v) where T : INumberBase<T> => T.IsZero(v);

    // Helper methods for conversion methods accessed through generic constraints
    private static TResult CreateCheckedViaInterface<TResult, TOther>(TOther v)
        where TResult : INumberBase<TResult>
        where TOther : INumberBase<TOther>
        => TResult.CreateChecked(v);

    private static TResult CreateSaturatingViaInterface<TResult, TOther>(TOther v)
        where TResult : INumberBase<TResult>
        where TOther : INumberBase<TOther>
        => TResult.CreateSaturating(v);

    private static TResult CreateTruncatingViaInterface<TResult, TOther>(TOther v)
        where TResult : INumberBase<TResult>
        where TOther : INumberBase<TOther>
        => TResult.CreateTruncating(v);

    // ── INumberBase<FixedPointNano>.Abs via interface ──

    [Test]
    public void AbsViaInterfaceOnPositiveValueReturnsPositiveValue()
    {
        var value = FixedPointNano.One * 5;
        var result = CallAbsViaInterface(value);
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void AbsViaInterfaceOnNegativeValueReturnsPositiveValue()
    {
        var value = FixedPointNano.One * -5;
        var result = CallAbsViaInterface(value);
        Assert.That(result, Is.EqualTo(FixedPointNano.One * 5));
    }

    [Test]
    public void AbsViaInterfaceOnZeroReturnsZero()
    {
        var result = CallAbsViaInterface(FixedPointNano.Zero);
        Assert.That(result, Is.EqualTo(FixedPointNano.Zero));
    }

    [Test]
    public void AbsViaInterfaceOnMinValueThrowsOverflow()
    {
        // MinValue cannot be negated without overflow, so Abs should throw
        var ex = Assert.Throws<OverflowException>(() => CallAbsViaInterface(FixedPointNano.MinValue));
        Assert.That(ex, Is.Not.Null);
    }

    // ── INumberBase<FixedPointNano>.IsInteger via interface ──

    [Test]
    public void IsIntegerViaInterfaceOnIntegerValueReturnsTrue()
    {
        var value = new FixedPointNano(FixedPointNano.Scale * 42);
        Assert.That(CallIsIntegerViaInterface(value), Is.True);
    }

    [Test]
    public void IsIntegerViaInterfaceOnFractionalValueReturnsFalse()
    {
        var value = new FixedPointNano(FixedPointNano.Scale * 42 + 123);
        Assert.That(CallIsIntegerViaInterface(value), Is.False);
    }

    [Test]
    public void IsIntegerViaInterfaceOnZeroReturnsTrue()
    {
        Assert.That(CallIsIntegerViaInterface(FixedPointNano.Zero), Is.True);
    }

    [Test]
    public void IsIntegerViaInterfaceOnNegativeIntegerReturnsTrue()
    {
        var value = new FixedPointNano(FixedPointNano.Scale * -123);
        Assert.That(CallIsIntegerViaInterface(value), Is.True);
    }

    [Test]
    public void IsIntegerViaInterfaceOnNegativeFractionalReturnsFalse()
    {
        var value = new FixedPointNano(FixedPointNano.Scale * -123 - 456);
        Assert.That(CallIsIntegerViaInterface(value), Is.False);
    }

    // ── INumberBase<FixedPointNano>.IsNegative via interface ──

    [Test]
    public void IsNegativeViaInterfaceOnNegativeValueReturnsTrue()
    {
        var value = FixedPointNano.One * -42;
        Assert.That(CallIsNegativeViaInterface(value), Is.True);
    }

    [Test]
    public void IsNegativeViaInterfaceOnPositiveValueReturnsFalse()
    {
        var value = FixedPointNano.One * 42;
        Assert.That(CallIsNegativeViaInterface(value), Is.False);
    }

    [Test]
    public void IsNegativeViaInterfaceOnZeroReturnsFalse()
    {
        Assert.That(CallIsNegativeViaInterface(FixedPointNano.Zero), Is.False);
    }

    [Test]
    public void IsNegativeViaInterfaceOnMinValueReturnsTrue()
    {
        Assert.That(CallIsNegativeViaInterface(FixedPointNano.MinValue), Is.True);
    }

    // ── INumberBase<FixedPointNano>.IsPositive via interface ──

    [Test]
    public void IsPositiveViaInterfaceOnPositiveValueReturnsTrue()
    {
        var value = FixedPointNano.One * 99;
        Assert.That(CallIsPositiveViaInterface(value), Is.True);
    }

    [Test]
    public void IsPositiveViaInterfaceOnNegativeValueReturnsFalse()
    {
        var value = FixedPointNano.One * -99;
        Assert.That(CallIsPositiveViaInterface(value), Is.False);
    }

    [Test]
    public void IsPositiveViaInterfaceOnZeroReturnsFalse()
    {
        Assert.That(CallIsPositiveViaInterface(FixedPointNano.Zero), Is.False);
    }

    [Test]
    public void IsPositiveViaInterfaceOnMaxValueReturnsTrue()
    {
        Assert.That(CallIsPositiveViaInterface(FixedPointNano.MaxValue), Is.True);
    }

    // ── INumberBase<FixedPointNano>.IsZero via interface ──

    [Test]
    public void IsZeroViaInterfaceOnZeroReturnsTrue()
    {
        Assert.That(CallIsZeroViaInterface(FixedPointNano.Zero), Is.True);
    }

    [Test]
    public void IsZeroViaInterfaceOnNonZeroPositiveReturnsFalse()
    {
        Assert.That(CallIsZeroViaInterface(FixedPointNano.One), Is.False);
    }

    [Test]
    public void IsZeroViaInterfaceOnNonZeroNegativeReturnsFalse()
    {
        Assert.That(CallIsZeroViaInterface(FixedPointNano.NegativeOne), Is.False);
    }

    [Test]
    public void IsZeroViaInterfaceOnEpsilonReturnsFalse()
    {
        Assert.That(CallIsZeroViaInterface(FixedPointNano.Epsilon), Is.False);
    }

    // ── Conversion methods with FixedPointNano as both source and target ──

    [Test]
    public void CreateCheckedViaInterfaceWithFixedPointNanoRoundtripsValue()
    {
        var original = (FixedPointNano)42.123456789m;
        var result = CreateCheckedViaInterface<FixedPointNano, FixedPointNano>(original);
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void CreateSaturatingViaInterfaceWithFixedPointNanoRoundtripsValue()
    {
        var original = (FixedPointNano)(-99.999999999m);
        var result = CreateSaturatingViaInterface<FixedPointNano, FixedPointNano>(original);
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void CreateTruncatingViaInterfaceWithFixedPointNanoRoundtripsValue()
    {
        var original = FixedPointNano.MaxValue / 2;
        var result = CreateTruncatingViaInterface<FixedPointNano, FixedPointNano>(original);
        Assert.That(result, Is.EqualTo(original));
    }

    // ── Convert from other types ──

    [Test]
    public void CreateCheckedViaInterfaceFromIntToFixedPointNanoConverts()
    {
        var result = CreateCheckedViaInterface<FixedPointNano, int>(42);
        Assert.That(result, Is.EqualTo(new FixedPointNano(42 * FixedPointNano.Scale)));
    }

    [Test]
    public void CreateSaturatingViaInterfaceFromIntToFixedPointNanoConverts()
    {
        var result = CreateSaturatingViaInterface<FixedPointNano, int>(123);
        Assert.That(result, Is.EqualTo(new FixedPointNano(123 * FixedPointNano.Scale)));
    }

    [Test]
    public void CreateTruncatingViaInterfaceFromIntToFixedPointNanoConverts()
    {
        var result = CreateTruncatingViaInterface<FixedPointNano, int>(99);
        Assert.That(result, Is.EqualTo(new FixedPointNano(99 * FixedPointNano.Scale)));
    }

    // ── Edge case combinations ──

    [Test]
    public void InterfaceMethodChainCheckNegativeZeroIsNotActuallyNegative()
    {
        // Ensure negative zero is treated as zero (which is not negative)
        var negativeZero = FixedPointNano.Zero;
        Assert.That(CallIsNegativeViaInterface(negativeZero), Is.False);
        Assert.That(CallIsZeroViaInterface(negativeZero), Is.True);
    }

    [Test]
    public void InterfaceMethodAbsAndIsNegativeConsistentOnOpposites()
    {
        var positive = FixedPointNano.One * 50;
        var negative = FixedPointNano.One * -50;

        var absNegative = CallAbsViaInterface(negative);
        Assert.That(absNegative, Is.EqualTo(positive));
        Assert.That(CallIsNegativeViaInterface(absNegative), Is.False);
        Assert.That(CallIsPositiveViaInterface(absNegative), Is.True);
    }

    [Test]
    public void InterfaceMethodIsIntegerWithBoundaryValues()
    {
        // Test with maximum integer that fits
        var maxInt = new FixedPointNano(long.MaxValue / FixedPointNano.Scale * FixedPointNano.Scale);
        Assert.That(CallIsIntegerViaInterface(maxInt), Is.True);

        // Test with slightly fractional value
        var almostInt = new FixedPointNano(long.MaxValue / FixedPointNano.Scale * FixedPointNano.Scale - 1);
        Assert.That(CallIsIntegerViaInterface(almostInt), Is.False);
    }
}
