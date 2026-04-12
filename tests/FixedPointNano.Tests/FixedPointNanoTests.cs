using System;
using System.Globalization;
using System.Numerics;
using Moq;

namespace FixedPointNano.Tests;

[TestFixture]
public sealed class FixedPointNanoTests
{
    [Test]
    public void ConstantsAndRawFactoriesShouldWork()
    {
        Assert.That(FixedPointNano.Zero.RawValue, Is.EqualTo(0L));
        Assert.That(FixedPointNano.One.RawValue, Is.EqualTo(FixedPointNano.Scale));
        Assert.That(FixedPointNano.MaxValue.RawValue, Is.EqualTo(long.MaxValue));
        Assert.That(FixedPointNano.MinValue.RawValue, Is.EqualTo(long.MinValue));

        var fromRaw = FixedPointNano.FromRaw(123456789L);
        var fromLong = (FixedPointNano)42L;

        Assert.That(fromRaw.RawValue, Is.EqualTo(123456789L));
        Assert.That(fromLong.RawValue, Is.EqualTo(42L * FixedPointNano.Scale));
    }

    [Test]
    public void FromDecimalShouldRoundToNineDecimalPlacesUsingToEven()
    {
        var lowerTie = FixedPointNano.FromDecimal(1.0000000005m);
        var upperTie = FixedPointNano.FromDecimal(1.0000000015m);
        var negativeTie = FixedPointNano.FromDecimal(-1.0000000015m);

        Assert.That(lowerTie.RawValue, Is.EqualTo(1_000_000_000L));
        Assert.That(upperTie.RawValue, Is.EqualTo(1_000_000_002L));
        Assert.That(negativeTie.RawValue, Is.EqualTo(-1_000_000_002L));
    }

    [Test]
    public void FloatingPointFactoriesShouldRejectInvalidValues()
    {
        Assert.That(() => FixedPointNano.FromDouble(double.NaN), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FixedPointNano.FromDouble(double.PositiveInfinity), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FixedPointNano.FromSingle(float.NaN), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FixedPointNano.FromSingle(float.NegativeInfinity), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ArithmeticOperatorsShouldWork()
    {
        var left = FixedPointNano.FromDecimal(12.5m);
        var right = FixedPointNano.FromDecimal(2.25m);

        Assert.That((left + right).ToDecimal(), Is.EqualTo(14.75m));
        Assert.That((left - right).ToDecimal(), Is.EqualTo(10.25m));
        Assert.That((-right).ToDecimal(), Is.EqualTo(-2.25m));
        Assert.That((left * right).ToDecimal(), Is.EqualTo(28.125m));
        Assert.That((left / right).ToDecimal(), Is.EqualTo(5.555555556m));
        Assert.That((left % right).ToDecimal(), Is.EqualTo(1.25m));
    }

    [Test]
    public void DivisionAndModuloByZeroShouldThrow()
    {
        var value = FixedPointNano.FromDecimal(1.25m);

        Assert.That(() => _ = value / FixedPointNano.Zero, Throws.TypeOf<DivideByZeroException>());
        Assert.That(() => _ = value % FixedPointNano.Zero, Throws.TypeOf<DivideByZeroException>());
    }

    [Test]
    public void ComparisonAndEqualityShouldWork()
    {
        var lower = FixedPointNano.FromDecimal(1.25m);
        var equal = FixedPointNano.FromDecimal(1.25m);
        var higher = FixedPointNano.FromDecimal(2.50m);

        Assert.Multiple(() =>
        {
            Assert.That(lower == equal, Is.True);
            Assert.That(lower != higher, Is.True);
            Assert.That(lower < higher, Is.True);
            Assert.That(lower <= equal, Is.True);
            Assert.That(higher > lower, Is.True);
            Assert.That(higher >= equal, Is.True);
            Assert.That(lower.CompareTo(equal), Is.Zero);
            Assert.That(((IComparable)lower).CompareTo(null), Is.EqualTo(1));
            Assert.That(((IComparable)lower).CompareTo(equal), Is.Zero);
            Assert.That(() => ((IComparable)lower).CompareTo("wrong"), Throws.TypeOf<ArgumentException>());
            Assert.That(lower.Equals((object)equal), Is.True);
            Assert.That(lower.Equals((object?)null), Is.False);
            Assert.That(lower.Equals("wrong"), Is.False);
            Assert.That(lower.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        });
    }

    [Test]
    public void HelperMethodsShouldWork()
    {
        var negative = FixedPointNano.FromDecimal(-1.75m);
        var positive = FixedPointNano.FromDecimal(1.25m);
        var fractional = FixedPointNano.FromDecimal(1.234567891m);

        Assert.Multiple(() =>
        {
            Assert.That(FixedPointNano.Abs(negative).ToDecimal(), Is.EqualTo(1.75m));
            Assert.That(FixedPointNano.Abs(positive).ToDecimal(), Is.EqualTo(1.25m));
            Assert.That(FixedPointNano.Ceiling(FixedPointNano.FromDecimal(1.1m)).ToDecimal(), Is.EqualTo(2m));
            Assert.That(FixedPointNano.Floor(FixedPointNano.FromDecimal(1.9m)).ToDecimal(), Is.EqualTo(1m));
            Assert.That(FixedPointNano.Truncate(negative).ToDecimal(), Is.EqualTo(-1m));
            Assert.That(FixedPointNano.Round(fractional, 4).ToDecimal(), Is.EqualTo(1.2346m));
            Assert.That(FixedPointNano.Max(positive, negative), Is.EqualTo(positive));
            Assert.That(FixedPointNano.Max(negative, positive), Is.EqualTo(positive));
            Assert.That(FixedPointNano.Min(positive, negative), Is.EqualTo(negative));
            Assert.That(FixedPointNano.Min(negative, positive), Is.EqualTo(negative));
        });

        Assert.That(() => FixedPointNano.Round(fractional, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FixedPointNano.Round(fractional, 10), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FixedPointNano.Abs(FixedPointNano.MinValue), Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void ParseAndTryParseShouldWork()
    {
        var invariant = CultureInfo.InvariantCulture;

        Assert.That(FixedPointNano.Parse("1.5", invariant).ToDecimal(), Is.EqualTo(1.5m));
        Assert.That(FixedPointNano.Parse("1.5".AsSpan(), invariant).ToDecimal(), Is.EqualTo(1.5m));
        Assert.That(FixedPointNano.Parse("-123.456789123", invariant).ToDecimal(), Is.EqualTo(-123.456789123m));

        Assert.That(FixedPointNano.TryParse("2.75", invariant, out var result1), Is.True);
        Assert.That(result1.ToDecimal(), Is.EqualTo(2.75m));

        Assert.That(FixedPointNano.TryParse("2.75".AsSpan(), invariant, out var result2), Is.True);
        Assert.That(result2.ToDecimal(), Is.EqualTo(2.75m));

        Assert.That(FixedPointNano.TryParse("not-a-number", invariant, out var result3), Is.False);
        Assert.That(result3, Is.EqualTo(FixedPointNano.Zero));

        Assert.That(FixedPointNano.TryParse((string?)null, invariant, out var result4), Is.False);
        Assert.That(result4, Is.EqualTo(FixedPointNano.Zero));

        Assert.That(() => FixedPointNano.Parse("not-a-number", invariant), Throws.TypeOf<FormatException>());
    }

    [Test]
    [NonParallelizable]
    public void ToStringShouldRespectCurrentCulture()
    {
        using var cultureScope = new CultureScope(new CultureInfo("fr-FR"));
        var value = FixedPointNano.FromDecimal(1234.5m);

        Assert.That(value.ToString(), Is.EqualTo("1234,5"));
        Assert.That(value.ToString("F2"), Is.EqualTo("1234,50"));
    }

    [Test]
    public void ToStringAndTryFormatShouldUseProvidedFormatProvider()
    {
        var numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        numberFormat.NumberDecimalSeparator = "_";
        numberFormat.NumberGroupSeparator = " ";

        var provider = new Mock<IFormatProvider>(MockBehavior.Strict);
        provider.Setup(x => x.GetFormat(typeof(NumberFormatInfo))).Returns(numberFormat);

        var value = FixedPointNano.FromDecimal(1234.5m);
        Span<char> buffer = stackalloc char[32];

        Assert.That(value.ToString(provider.Object), Is.EqualTo("1234_5"));
        Assert.That(value.ToString("N2", provider.Object), Is.EqualTo("1 234_50"));
        Assert.That(value.TryFormat(buffer, out var charsWritten, "F3", provider.Object), Is.True);
        Assert.That(new string(buffer[..charsWritten]), Is.EqualTo("1234_500"));

        provider.Verify(x => x.GetFormat(typeof(NumberFormatInfo)), Times.AtLeastOnce);
    }

    [Test]
    public void TryFormatShouldReturnFalseWhenDestinationIsTooSmall()
    {
        var value = FixedPointNano.FromDecimal(1234.5m);
        Span<char> buffer = stackalloc char[2];

        Assert.That(value.TryFormat(buffer, out var charsWritten, "F2", CultureInfo.InvariantCulture), Is.False);
        Assert.That(charsWritten, Is.EqualTo(0));
    }

    [Test]
    public void ConversionsToFixedPointNanoShouldSupportAllTargetNumericInputs()
    {
        var fromByte = (FixedPointNano)(byte)1;
        var fromSByte = (FixedPointNano)(sbyte)-2;
        var fromShort = (FixedPointNano)(short)3;
        var fromUShort = (FixedPointNano)(ushort)4;
        var fromInt = (FixedPointNano)5;
        var fromUInt = (FixedPointNano)6U;
        var fromLong = (FixedPointNano)7L;
        var fromULong = (FixedPointNano)8UL;
        var fromNInt = (FixedPointNano)(nint)9;
        var fromNUInt = (FixedPointNano)(nuint)10;
        var fromHalf = (FixedPointNano)(Half)11.25f;
        var fromSingle = (FixedPointNano)12.5f;
        var fromDouble = (FixedPointNano)13.75d;
        var fromDecimal = (FixedPointNano)14.125m;
        var fromInt128 = (FixedPointNano)(Int128)15;
        var fromUInt128 = (FixedPointNano)(UInt128)16;
        var fromBigInteger = (FixedPointNano)new BigInteger(17);

        Assert.Multiple(() =>
        {
            Assert.That(fromByte.ToDecimal(), Is.EqualTo(1m));
            Assert.That(fromSByte.ToDecimal(), Is.EqualTo(-2m));
            Assert.That(fromShort.ToDecimal(), Is.EqualTo(3m));
            Assert.That(fromUShort.ToDecimal(), Is.EqualTo(4m));
            Assert.That(fromInt.ToDecimal(), Is.EqualTo(5m));
            Assert.That(fromUInt.ToDecimal(), Is.EqualTo(6m));
            Assert.That(fromLong.ToDecimal(), Is.EqualTo(7m));
            Assert.That(fromULong.ToDecimal(), Is.EqualTo(8m));
            Assert.That(fromNInt.ToDecimal(), Is.EqualTo(9m));
            Assert.That(fromNUInt.ToDecimal(), Is.EqualTo(10m));
            Assert.That(fromHalf.ToDecimal(), Is.EqualTo(11.25m));
            Assert.That(fromSingle.ToDecimal(), Is.EqualTo(12.5m));
            Assert.That(fromDouble.ToDecimal(), Is.EqualTo(13.75m));
            Assert.That(fromDecimal.ToDecimal(), Is.EqualTo(14.125m));
            Assert.That(fromInt128.ToDecimal(), Is.EqualTo(15m));
            Assert.That(fromUInt128.ToDecimal(), Is.EqualTo(16m));
            Assert.That(fromBigInteger.ToDecimal(), Is.EqualTo(17m));
        });
    }

    [Test]
    public void ConversionsFromFixedPointNanoShouldSupportAllTargetNumericOutputs()
    {
        var integerValue = FixedPointNano.FromDecimal(42m);
        var fractionalValue = FixedPointNano.FromDecimal(42.875m);

        Assert.Multiple(() =>
        {
            Assert.That((byte)integerValue, Is.EqualTo((byte)42));
            Assert.That((sbyte)integerValue, Is.EqualTo((sbyte)42));
            Assert.That((short)integerValue, Is.EqualTo((short)42));
            Assert.That((ushort)integerValue, Is.EqualTo((ushort)42));
            Assert.That((int)integerValue, Is.EqualTo(42));
            Assert.That((uint)integerValue, Is.EqualTo(42U));
            Assert.That((long)integerValue, Is.EqualTo(42L));
            Assert.That((ulong)integerValue, Is.EqualTo(42UL));
            Assert.That((nint)integerValue, Is.EqualTo((nint)42));
            Assert.That((nuint)integerValue, Is.EqualTo((nuint)42));
            Assert.That((Half)fractionalValue, Is.EqualTo((Half)42.875f));
            Assert.That((float)fractionalValue, Is.EqualTo(42.875f));
            Assert.That((double)fractionalValue, Is.EqualTo(42.875d));
            Assert.That((decimal)fractionalValue, Is.EqualTo(42.875m));
            Assert.That((Int128)fractionalValue, Is.EqualTo((Int128)42));
            Assert.That((UInt128)integerValue, Is.EqualTo((UInt128)42));
            Assert.That((BigInteger)fractionalValue, Is.EqualTo(new BigInteger(42)));
            Assert.That(fractionalValue.ToDouble(), Is.EqualTo(42.875d));
            Assert.That(fractionalValue.ToSingle(), Is.EqualTo(42.875f));
            Assert.That(fractionalValue.ToHalf(), Is.EqualTo((Half)42.875f));
            Assert.That(fractionalValue.ToBigInteger(), Is.EqualTo(new BigInteger(42)));
            Assert.That(fractionalValue.ToInt128(), Is.EqualTo((Int128)42));
            Assert.That(integerValue.ToUInt128(), Is.EqualTo((UInt128)42));
        });

        var negativeValue = FixedPointNano.FromDecimal(-1m);

        Assert.Multiple(() =>
        {
            Assert.That(() => _ = (ulong)negativeValue, Throws.TypeOf<OverflowException>());
            Assert.That(() => _ = (nuint)negativeValue, Throws.TypeOf<OverflowException>());
            Assert.That(() => _ = (UInt128)negativeValue, Throws.TypeOf<OverflowException>());
            Assert.That(() => _ = negativeValue.ToUInt128(), Throws.TypeOf<OverflowException>());
        });
    }

    [Test]
    public void IConvertibleShouldSupportDocumentedConversions()
    {
        var value = FixedPointNano.FromDecimal(42.875m);
        var convertible = (IConvertible)value;
        var zeroConvertible = (IConvertible)FixedPointNano.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(convertible.GetTypeCode(), Is.EqualTo(TypeCode.Object));
            Assert.That(convertible.ToBoolean(null), Is.True);
            Assert.That(zeroConvertible.ToBoolean(null), Is.False);
            Assert.That(convertible.ToByte(null), Is.EqualTo((byte)42));
            Assert.That(convertible.ToDecimal(null), Is.EqualTo(42.875m));
            Assert.That(convertible.ToDouble(null), Is.EqualTo(42.875d));
            Assert.That(convertible.ToInt16(null), Is.EqualTo((short)42));
            Assert.That(convertible.ToInt32(null), Is.EqualTo(42));
            Assert.That(convertible.ToInt64(null), Is.EqualTo(42L));
            Assert.That(convertible.ToSByte(null), Is.EqualTo((sbyte)42));
            Assert.That(convertible.ToSingle(null), Is.EqualTo(42.875f));
            Assert.That(convertible.ToString(CultureInfo.InvariantCulture), Is.EqualTo("42.875"));
            Assert.That(convertible.ToUInt16(null), Is.EqualTo((ushort)42));
            Assert.That(convertible.ToUInt32(null), Is.EqualTo(42U));
            Assert.That(convertible.ToUInt64(null), Is.EqualTo(42UL));
            Assert.That(convertible.ToType(typeof(FixedPointNano), null), Is.EqualTo(value));
            Assert.That(convertible.ToType(typeof(Half), null), Is.EqualTo((Half)42.875f));
            Assert.That(convertible.ToType(typeof(Int128), null), Is.EqualTo((Int128)42));
            Assert.That(convertible.ToType(typeof(UInt128), null), Is.EqualTo((UInt128)42));
            Assert.That(convertible.ToType(typeof(BigInteger), null), Is.EqualTo(new BigInteger(42)));
            Assert.That(convertible.ToType(typeof(decimal), null), Is.EqualTo(42.875m));
        });

        Assert.Multiple(() =>
        {
            Assert.That(() => convertible.ToChar(null), Throws.TypeOf<InvalidCastException>());
            Assert.That(() => convertible.ToDateTime(null), Throws.TypeOf<InvalidCastException>());
            Assert.That(() => convertible.ToType(typeof(Guid), null), Throws.TypeOf<InvalidCastException>());
            Assert.That(() => convertible.ToType(null!, null), Throws.TypeOf<ArgumentNullException>());
        });
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUiCulture;

        public CultureScope(CultureInfo culture)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
