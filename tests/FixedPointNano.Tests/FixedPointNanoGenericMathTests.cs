using System;
using System.Globalization;
using System.Numerics;
using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoGenericMathTests
{
    // ── helpers to call static abstract interface members through type parameters ──

    private static bool IsEvenInt<T>(T v) where T : INumberBase<T> => T.IsEvenInteger(v);
    private static bool IsOddInt<T>(T v) where T : INumberBase<T> => T.IsOddInteger(v);
    private static bool IsCanonical<T>(T v) where T : INumberBase<T> => T.IsCanonical(v);
    private static bool IsComplex<T>(T v) where T : INumberBase<T> => T.IsComplexNumber(v);
    private static bool IsFiniteV<T>(T v) where T : INumberBase<T> => T.IsFinite(v);
    private static bool IsImaginary<T>(T v) where T : INumberBase<T> => T.IsImaginaryNumber(v);
    private static bool IsInfinityV<T>(T v) where T : INumberBase<T> => T.IsInfinity(v);
    private static bool IsNaNV<T>(T v) where T : INumberBase<T> => T.IsNaN(v);
    private static bool IsNegInf<T>(T v) where T : INumberBase<T> => T.IsNegativeInfinity(v);
    private static bool IsNormalV<T>(T v) where T : INumberBase<T> => T.IsNormal(v);
    private static bool IsPosInf<T>(T v) where T : INumberBase<T> => T.IsPositiveInfinity(v);
    private static bool IsRealNum<T>(T v) where T : INumberBase<T> => T.IsRealNumber(v);
    private static bool IsSubnormal<T>(T v) where T : INumberBase<T> => T.IsSubnormal(v);
    private static int GetRadix<T>() where T : INumberBase<T> => T.Radix;
    private static T MaxMag<T>(T x, T y) where T : INumberBase<T> => T.MaxMagnitude(x, y);
    private static T MinMag<T>(T x, T y) where T : INumberBase<T> => T.MinMagnitude(x, y);
    private static T MaxMagN<T>(T x, T y) where T : INumberBase<T> => T.MaxMagnitudeNumber(x, y);
    private static T MinMagN<T>(T x, T y) where T : INumberBase<T> => T.MinMagnitudeNumber(x, y);
    private static TResult CreateChecked<TResult, TOther>(TOther v) where TResult : INumberBase<TResult> where TOther : INumberBase<TOther> => TResult.CreateChecked(v);
    private static TResult CreateSaturating<TResult, TOther>(TOther v) where TResult : INumberBase<TResult> where TOther : INumberBase<TOther> => TResult.CreateSaturating(v);
    private static TResult CreateTruncating<TResult, TOther>(TOther v) where TResult : INumberBase<TResult> where TOther : INumberBase<TOther> => TResult.CreateTruncating(v);
    private static T MaxNumber<T>(T x, T y) where T : INumber<T> => T.MaxNumber(x, y);
    private static T MinNumber<T>(T x, T y) where T : INumber<T> => T.MinNumber(x, y);
    private static T NegOne<T>() where T : ISignedNumber<T> => T.NegativeOne;

    // ── IsEvenInteger / IsOddInteger (public + INumberBase) ──────────────────────

    [TestCase(0L, true)]
    [TestCase(2L, true)]
    [TestCase(-4L, true)]
    [TestCase(1L, false)]
    [TestCase(-3L, false)]
    public void IsEvenIntegerShouldClassifyCorrectly(long intValue, bool expected)
    {
        var value = (FixedPointNano)intValue;
        Assert.That(FixedPointNano.IsEvenInteger(value), Is.EqualTo(expected));
    }

    [Test]
    public void IsEvenIntegerShouldReturnFalseForFractional()
    {
        Assert.That(FixedPointNano.IsEvenInteger(FixedPointNano.FromDecimal(2.5m)), Is.False);
    }

    [TestCase(1L, true)]
    [TestCase(-3L, true)]
    [TestCase(0L, false)]
    [TestCase(2L, false)]
    [TestCase(-4L, false)]
    public void IsOddIntegerShouldClassifyCorrectly(long intValue, bool expected)
    {
        var value = (FixedPointNano)intValue;
        Assert.That(FixedPointNano.IsOddInteger(value), Is.EqualTo(expected));
    }

    [Test]
    public void IsOddIntegerShouldReturnFalseForFractional()
    {
        Assert.That(FixedPointNano.IsOddInteger(FixedPointNano.FromDecimal(3.5m)), Is.False);
    }

    [Test]
    public void INumberBaseIsEvenAndOddDelegateToPublicMethods()
    {
        var two = (FixedPointNano)2L;
        var three = (FixedPointNano)3L;
        Assert.Multiple(() =>
        {
            Assert.That(IsEvenInt(two), Is.True);
            Assert.That(IsEvenInt(three), Is.False);
            Assert.That(IsOddInt(three), Is.True);
            Assert.That(IsOddInt(two), Is.False);
        });
    }

    // ── INumberBase constant predicates ──────────────────────────────────────────

    [Test]
    public void INumberBaseAlwaysTruePredicatesShouldReturnTrue()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IsCanonical(FixedPointNano.One), Is.True);
            Assert.That(IsFiniteV(FixedPointNano.One), Is.True);
            Assert.That(IsRealNum(FixedPointNano.One), Is.True);
            Assert.That(IsNormalV(FixedPointNano.One), Is.True);
        });
    }

    [Test]
    public void INumberBaseAlwaysFalsePredicatesShouldReturnFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IsComplex(FixedPointNano.One), Is.False);
            Assert.That(IsImaginary(FixedPointNano.One), Is.False);
            Assert.That(IsInfinityV(FixedPointNano.One), Is.False);
            Assert.That(IsNaNV(FixedPointNano.One), Is.False);
            Assert.That(IsNegInf(FixedPointNano.One), Is.False);
            Assert.That(IsPosInf(FixedPointNano.One), Is.False);
            Assert.That(IsSubnormal(FixedPointNano.One), Is.False);
        });
    }

    [Test]
    public void IsNormalShouldBeFalseForZero()
    {
        Assert.That(IsNormalV(FixedPointNano.Zero), Is.False);
    }

    [Test]
    public void RadixShouldBeTen()
    {
        Assert.That(GetRadix<FixedPointNano>(), Is.EqualTo(10));
    }

    // ── MaxMagnitude / MinMagnitude ───────────────────────────────────────────────

    [Test]
    public void MaxMagnitudeShouldReturnLargerAbsoluteValue()
    {
        var three = (FixedPointNano)3L;
        var negFive = (FixedPointNano)(-5L);
        Assert.That(MaxMag(three, negFive), Is.EqualTo(negFive));
        Assert.That(MaxMag(negFive, three), Is.EqualTo(negFive));
    }

    [Test]
    public void MaxMagnitudeWithEqualAbsValueShouldReturnPositive()
    {
        var pos = (FixedPointNano)4L;
        var neg = (FixedPointNano)(-4L);
        Assert.That(MaxMag(neg, pos), Is.EqualTo(pos));
        Assert.That(MaxMag(pos, neg), Is.EqualTo(pos));
    }

    [Test]
    public void MaxMagnitudeNumberMatchesMaxMagnitude()
    {
        var a = (FixedPointNano)7L;
        var b = (FixedPointNano)(-10L);
        Assert.That(MaxMagN(a, b), Is.EqualTo(MaxMag(a, b)));
    }

    [Test]
    public void MinMagnitudeShouldReturnSmallerAbsoluteValue()
    {
        var three = (FixedPointNano)3L;
        var negFive = (FixedPointNano)(-5L);
        Assert.That(MinMag(three, negFive), Is.EqualTo(three));
        Assert.That(MinMag(negFive, three), Is.EqualTo(three));
    }

    [Test]
    public void MinMagnitudeWithEqualAbsValueShouldReturnNegative()
    {
        var pos = (FixedPointNano)4L;
        var neg = (FixedPointNano)(-4L);
        Assert.That(MinMag(neg, pos), Is.EqualTo(neg));
        Assert.That(MinMag(pos, neg), Is.EqualTo(neg));
    }

    [Test]
    public void MinMagnitudeNumberMatchesMinMagnitude()
    {
        var a = (FixedPointNano)7L;
        var b = (FixedPointNano)(-10L);
        Assert.That(MinMagN(a, b), Is.EqualTo(MinMag(a, b)));
    }

    // ── INumber.MaxNumber / MinNumber ─────────────────────────────────────────────

    [Test]
    public void MaxNumberAndMinNumberDelegateToMaxAndMin()
    {
        var a = (FixedPointNano)5L;
        var b = (FixedPointNano)3L;
        Assert.That(MaxNumber(a, b), Is.EqualTo(a));
        Assert.That(MinNumber(a, b), Is.EqualTo(b));
    }

    // ── ISignedNumber ─────────────────────────────────────────────────────────────

    [Test]
    public void ISignedNumberNegativeOneIsNegativeOne()
    {
        Assert.That(NegOne<FixedPointNano>(), Is.EqualTo(FixedPointNano.NegativeOne));
    }

    // ── CreateChecked ─────────────────────────────────────────────────────────────

    [Test]
    public void CreateCheckedFromIntShouldRoundtrip()
    {
        var result = CreateChecked<FixedPointNano, int>(42);
        Assert.That(result, Is.EqualTo((FixedPointNano)42L));
    }

    [Test]
    public void CreateCheckedFromLongInRangeShouldRoundtrip()
    {
        var result = CreateChecked<FixedPointNano, long>(100L);
        Assert.That(result, Is.EqualTo((FixedPointNano)100L));
    }

    [Test]
    public void CreateCheckedFromDoubleShouldConvert()
    {
        var result = CreateChecked<FixedPointNano, double>(1.5);
        Assert.That(result, Is.EqualTo(FixedPointNano.FromDecimal(1.5m)));
    }

    [Test]
    public void CreateCheckedFromFixedPointNanoReturnsSameValue()
    {
        var input = FixedPointNano.FromDecimal(3.14m);
        var result = CreateChecked<FixedPointNano, FixedPointNano>(input);
        Assert.That(result, Is.EqualTo(input));
    }

    [Test]
    public void CreateCheckedFromLongMaxValueShouldThrowOverflow()
    {
        Assert.That(
            () => CreateChecked<FixedPointNano, long>(long.MaxValue),
            Throws.TypeOf<OverflowException>());
    }

    // ── CreateSaturating ──────────────────────────────────────────────────────────

    [Test]
    public void CreateSaturatingFromLongMaxValueShouldClampToMaxValue()
    {
        var result = CreateSaturating<FixedPointNano, long>(long.MaxValue);
        Assert.That(result, Is.EqualTo(FixedPointNano.MaxValue));
    }

    [Test]
    public void CreateSaturatingFromLongMinValueShouldClampToMinValue()
    {
        var result = CreateSaturating<FixedPointNano, long>(long.MinValue);
        Assert.That(result, Is.EqualTo(FixedPointNano.MinValue));
    }

    [Test]
    public void CreateSaturatingFromULongMaxValueShouldClampToMaxValue()
    {
        var result = CreateSaturating<FixedPointNano, ulong>(ulong.MaxValue);
        Assert.That(result, Is.EqualTo(FixedPointNano.MaxValue));
    }

    [Test]
    public void CreateSaturatingFromDoubleNaNShouldReturnZero()
    {
        var result = CreateSaturating<FixedPointNano, double>(double.NaN);
        Assert.That(result, Is.EqualTo(FixedPointNano.Zero));
    }

    [Test]
    public void CreateSaturatingFromPositiveInfinityShouldClampToMaxValue()
    {
        var result = CreateSaturating<FixedPointNano, double>(double.PositiveInfinity);
        Assert.That(result, Is.EqualTo(FixedPointNano.MaxValue));
    }

    [Test]
    public void CreateSaturatingFromNegativeInfinityShouldClampToMinValue()
    {
        var result = CreateSaturating<FixedPointNano, double>(double.NegativeInfinity);
        Assert.That(result, Is.EqualTo(FixedPointNano.MinValue));
    }

    [Test]
    public void CreateSaturatingFromNormalDoubleShouldConvert()
    {
        var result = CreateSaturating<FixedPointNano, double>(2.5);
        Assert.That(result, Is.EqualTo(FixedPointNano.FromDecimal(2.5m)));
    }

    [Test]
    public void CreateSaturatingFromInt128MaxValueShouldClampToMaxValue()
    {
        var result = CreateSaturating<FixedPointNano, Int128>(Int128.MaxValue);
        Assert.That(result, Is.EqualTo(FixedPointNano.MaxValue));
    }

    [Test]
    public void CreateSaturatingFromInt128MinValueShouldClampToMinValue()
    {
        var result = CreateSaturating<FixedPointNano, Int128>(Int128.MinValue);
        Assert.That(result, Is.EqualTo(FixedPointNano.MinValue));
    }

    // ── CreateTruncating ──────────────────────────────────────────────────────────

    [Test]
    public void CreateTruncatingFromIntShouldRoundtrip()
    {
        var result = CreateTruncating<FixedPointNano, int>(99);
        Assert.That(result, Is.EqualTo((FixedPointNano)99L));
    }

    [Test]
    public void CreateTruncatingFromDoubleNaNShouldReturnZero()
    {
        var result = CreateTruncating<FixedPointNano, double>(double.NaN);
        Assert.That(result, Is.EqualTo(FixedPointNano.Zero));
    }

    [Test]
    public void CreateTruncatingFromPositiveInfinityShouldClampToMaxValue()
    {
        var result = CreateTruncating<FixedPointNano, double>(double.PositiveInfinity);
        Assert.That(result, Is.EqualTo(FixedPointNano.MaxValue));
    }

    // ── TryConvertTo direction (tested via CreateChecked on target type) ──────────

    [Test]
    public void CreateCheckedIntFromFixedPointNanoShouldReturnIntegerPart()
    {
        var fp = (FixedPointNano)42L;
        var result = CreateChecked<int, FixedPointNano>(fp);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void CreateCheckedDoubleFromFixedPointNanoPreservesValue()
    {
        var fp = FixedPointNano.FromDecimal(1.5m);
        var result = CreateChecked<double, FixedPointNano>(fp);
        Assert.That(result, Is.EqualTo(1.5).Within(1e-9));
    }

    [Test]
    public void CreateCheckedByteFromLargeFixedPointNanoShouldThrowOverflow()
    {
        var fp = (FixedPointNano)1_000_000L;
        Assert.That(
            () => CreateChecked<byte, FixedPointNano>(fp),
            Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void CreateSaturatingByteFromLargeFixedPointNanoShouldClampToByteMax()
    {
        var fp = (FixedPointNano)1_000_000L;
        var result = CreateSaturating<byte, FixedPointNano>(fp);
        Assert.That(result, Is.EqualTo(byte.MaxValue));
    }

    [Test]
    public void CreateSaturatingULongFromNegativeFixedPointNanoShouldClampToZero()
    {
        var fp = (FixedPointNano)(-10L);
        var result = CreateSaturating<ulong, FixedPointNano>(fp);
        Assert.That(result, Is.EqualTo(0UL));
    }

    [Test]
    public void CreateTruncatingByteFromOverflowFixedPointNanoWraps()
    {
        var fp = (FixedPointNano)256L;
        var result = CreateTruncating<byte, FixedPointNano>(fp);
        Assert.That(result, Is.EqualTo(unchecked((byte)256)));
    }

    // ── NumberStyles Parse / TryParse ─────────────────────────────────────────────

    [Test]
    public void ParseSpanWithNumberStylesShouldSucceed()
    {
        var result = FixedPointNano.Parse("1234.5".AsSpan(), NumberStyles.Number, CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo(FixedPointNano.FromDecimal(1234.5m)));
    }

    [Test]
    public void ParseStringWithNumberStylesShouldSucceed()
    {
        var result = FixedPointNano.Parse("99.9", NumberStyles.Number, CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo(FixedPointNano.FromDecimal(99.9m)));
    }

    [Test]
    public void ParseWithNumberStylesShouldThrowOnInvalidInput()
    {
        Assert.That(
            () => FixedPointNano.Parse("not-a-number".AsSpan(), NumberStyles.Number, CultureInfo.InvariantCulture),
            Throws.TypeOf<FormatException>());
    }

    [Test]
    public void TryParseSpanWithNumberStylesShouldReturnTrueOnSuccess()
    {
        var ok = FixedPointNano.TryParse("55.5".AsSpan(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result);
        Assert.That(ok, Is.True);
        Assert.That(result, Is.EqualTo(FixedPointNano.FromDecimal(55.5m)));
    }

    [Test]
    public void TryParseStringWithNumberStylesShouldReturnFalseOnInvalidInput()
    {
        var ok = FixedPointNano.TryParse("abc", NumberStyles.Number, CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryParseNullStringWithNumberStylesShouldReturnFalse()
    {
        var ok = FixedPointNano.TryParse((string?)null, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    // ── Generic math algorithm test ───────────────────────────────────────────────

    [Test]
    public void GenericSumViaINumberShouldProduceCorrectResult()
    {
        var values = new[] { (FixedPointNano)1L, (FixedPointNano)2L, (FixedPointNano)3L };
        var sum = SumGeneric(values);
        Assert.That(sum, Is.EqualTo((FixedPointNano)6L));
    }

    private static T SumGeneric<T>(T[] values) where T : INumber<T>
    {
        var total = T.Zero;
        foreach (var v in values)
        {
            total += v;
        }

        return total;
    }
}
