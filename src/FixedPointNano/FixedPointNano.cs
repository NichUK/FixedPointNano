using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Seerstone;

/// <summary>
/// A <see langword="long"/>-backed fixed-point numeric type with a scale of 1,000,000,000 (10<sup>9</sup>),
/// providing 9 decimal places of precision. Arithmetic is deterministic and allocation-free.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct FixedPointNano :
    IComparable,
    IComparable<FixedPointNano>,
    IEquatable<FixedPointNano>,
    IFormattable,
    ISpanFormattable,
    IParsable<FixedPointNano>,
    ISpanParsable<FixedPointNano>,
    IConvertible
{
    /// <summary>The number of decimal places: 9.</summary>
    public const int DecimalPlaces = 9;
    /// <summary>The scale factor applied to all raw values: 1,000,000,000.</summary>
    public const long Scale = 1_000_000_000L;
    private const double MaxRawValueAsDoubleExclusive = 9_223_372_036_854_775_808d;
    private const double MinRawValueAsDoubleInclusive = -9_223_372_036_854_775_808d;
    private static readonly long[] s_roundingScales =
    [
        1_000_000_000L,
        100_000_000L,
        10_000_000L,
        1_000_000L,
        100_000L,
        10_000L,
        1_000L,
        100L,
        10L,
        1L,
    ];

    /// <summary>The zero value (0).</summary>
    public static FixedPointNano Zero { get; } = new(0L);
    /// <summary>The value one (1).</summary>
    public static FixedPointNano One { get; } = new(Scale);
    /// <summary>The maximum representable value (raw value <see cref="long.MaxValue"/>: approximately ±9.223 billion).</summary>
    public static FixedPointNano MaxValue { get; } = new(long.MaxValue);
    /// <summary>The minimum representable value (raw value <see cref="long.MinValue"/>: approximately −9.223 billion).</summary>
    public static FixedPointNano MinValue { get; } = new(long.MinValue);

    /// <summary>Initializes a new instance from a raw scaled <see langword="long"/> value.</summary>
    /// <param name="rawValue">The pre-scaled integer value (not divided by <see cref="Scale"/>).</param>
    public FixedPointNano(long rawValue)
    {
        RawValue = rawValue;
    }

    /// <summary>The underlying scaled integer. Divide by <see cref="Scale"/> to obtain the decimal value.</summary>
    public long RawValue { get; }

    /// <summary>Returns the absolute value of <paramref name="value"/>.</summary>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> is <see cref="MinValue"/>.</exception>
    public static FixedPointNano Abs(FixedPointNano value)
    {
        return value.RawValue < 0
            ? new FixedPointNano(checked(-value.RawValue))
            : value;
    }

    /// <summary>Returns the smallest integral value that is greater than or equal to <paramref name="value"/>.</summary>
    public static FixedPointNano Ceiling(FixedPointNano value)
    {
        var quotient = value.RawValue / Scale;
        var remainder = value.RawValue % Scale;
        if (remainder > 0)
        {
            quotient = checked(quotient + 1);
        }

        return new FixedPointNano(checked(quotient * Scale));
    }

    /// <summary>Returns the largest integral value that is less than or equal to <paramref name="value"/>.</summary>
    public static FixedPointNano Floor(FixedPointNano value)
    {
        var quotient = value.RawValue / Scale;
        var remainder = value.RawValue % Scale;
        if (remainder < 0)
        {
            quotient = checked(quotient - 1);
        }

        return new FixedPointNano(checked(quotient * Scale));
    }

    /// <summary>Converts a <see cref="decimal"/> to <see cref="FixedPointNano"/> using banker's rounding (ToEven).</summary>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> is outside the representable range.</exception>
    public static FixedPointNano FromDecimal(decimal value)
    {
        var scaledValue = decimal.Round(value * Scale, 0, MidpointRounding.ToEven);
        return new FixedPointNano(decimal.ToInt64(scaledValue));
    }

    /// <summary>Converts a finite <see cref="double"/> to <see cref="FixedPointNano"/> using banker's rounding (ToEven).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> is outside the representable range.</exception>
    public static FixedPointNano FromDouble(double value)
    {
        ThrowIfInvalidFloatingPoint(value);
        var scaledValue = value * Scale;
        if (double.IsInfinity(scaledValue))
        {
            throw new OverflowException("The value is outside the range of FixedPointNano.");
        }

        var roundedValue = Math.Round(scaledValue, MidpointRounding.ToEven);
        if (roundedValue < MinRawValueAsDoubleInclusive || roundedValue >= MaxRawValueAsDoubleExclusive)
        {
            throw new OverflowException("The value is outside the range of FixedPointNano.");
        }

        return new FixedPointNano(checked((long)roundedValue));
    }

    /// <summary>Converts a finite <see cref="Half"/> to <see cref="FixedPointNano"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static FixedPointNano FromHalf(Half value)
    {
        return FromSingle((float)value);
    }

    /// <summary>Creates a <see cref="FixedPointNano"/> directly from a pre-scaled raw value.</summary>
    public static FixedPointNano FromRaw(long rawValue)
    {
        return new FixedPointNano(rawValue);
    }

    /// <summary>Converts a finite <see cref="float"/> to <see cref="FixedPointNano"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> is outside the representable range.</exception>
    public static FixedPointNano FromSingle(float value)
    {
        ThrowIfInvalidFloatingPoint(value);
        return FromDouble(value);
    }

    /// <summary>Returns the larger of two values.</summary>
    public static FixedPointNano Max(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue ? left : right;
    }

    /// <summary>Returns the smaller of two values.</summary>
    public static FixedPointNano Min(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue ? left : right;
    }

    public static FixedPointNano Clamp(FixedPointNano value, FixedPointNano min, FixedPointNano max)
    {
        if (min.RawValue > max.RawValue)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "min must be less than or equal to max.");
        }

        return value.RawValue < min.RawValue ? min : value.RawValue > max.RawValue ? max : value;
    }

    public static int Sign(FixedPointNano value)
    {
        return value.RawValue < 0 ? -1 : value.RawValue > 0 ? 1 : 0;
    }

    public static FixedPointNano Round(FixedPointNano value, int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        if (decimals is < 0 or > DecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimals must be between 0 and {DecimalPlaces}.");
        }

        ValidateRounding(rounding);
        return new FixedPointNano(RoundRaw(value.RawValue, s_roundingScales[decimals], rounding));
    }

    /// <summary>Divides <paramref name="value"/> by an integer <paramref name="divisor"/> with banker's rounding.</summary>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="divisor"/> is zero.</exception>
    public static FixedPointNano Divide(FixedPointNano value, int divisor)
    {
        return Divide(value, (long)divisor);
    }

    /// <summary>Divides <paramref name="value"/> by a <see langword="long"/> <paramref name="divisor"/> with banker's rounding.</summary>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="divisor"/> is zero.</exception>
    public static FixedPointNano Divide(FixedPointNano value, long divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        return FromRawChecked(DivideRoundedToNearestEven(value.RawValue, divisor));
    }

    /// <summary>Computes <c>value * numerator / denominator</c> with banker's rounding, using 128-bit intermediate arithmetic to avoid overflow.</summary>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="denominator"/> is zero.</exception>
    public static FixedPointNano MultiplyRatio(FixedPointNano value, long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException();
        }

        var scaledNumerator = (Int128)value.RawValue * numerator;
        return FromRawChecked(DivideRoundedToNearestEven(scaledNumerator, denominator));
    }

    /// <summary>Returns <c>value²</c>.</summary>
    public static FixedPointNano Square(FixedPointNano value)
    {
        return value * value;
    }

    /// <summary>Computes the population variance from pre-aggregated statistics.</summary>
    /// <param name="sum">The sum of all values.</param>
    /// <param name="sumOfRawSquares">The sum of each element's <see cref="RawValue"/> squared.</param>
    /// <param name="count">The number of values (must be &gt; 0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> ≤ 0 or statistics are inconsistent.</exception>
    public static FixedPointNano PopulationVariance(FixedPointNano sum, Int128 sumOfRawSquares, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        if (sumOfRawSquares < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sumOfRawSquares), "Sum of raw squares must not be negative.");
        }

        var countValue = (Int128)count;
        var numerator = checked((sumOfRawSquares * countValue) - ((Int128)sum.RawValue * sum.RawValue));
        if (numerator < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sumOfRawSquares),
                "Sum of raw squares is inconsistent with the supplied sum and count.");
        }

        var denominator = checked(countValue * countValue * Scale);
        return FromRawChecked(DivideRoundedToNearestEven(numerator, denominator));
    }

    /// <summary>Computes the population standard deviation from pre-aggregated statistics. See <see cref="PopulationVariance"/>.</summary>
    public static FixedPointNano PopulationStandardDeviation(FixedPointNano sum, Int128 sumOfRawSquares, int count)
    {
        return Sqrt(PopulationVariance(sum, sumOfRawSquares, count));
    }

    /// <summary>Returns the square root of <paramref name="value"/>, rounded to nearest (banker's rounding).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public static FixedPointNano Sqrt(FixedPointNano value)
    {
        if (value.RawValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Square root input must not be negative.");
        }

        if (value.RawValue == 0)
        {
            return value;
        }

        var target = checked((UInt128)((Int128)value.RawValue * Scale));
        var rawValue = target <= ulong.MaxValue
            ? SquareRootRoundedToNearestEven((ulong)target)
            : SquareRootRoundedToNearestEven(target);
        return FromRawChecked((Int128)rawValue);
    }

    /// <summary>Returns the integral part of <paramref name="value"/>, discarding the fractional portion (truncation toward zero).</summary>
    public static FixedPointNano Truncate(FixedPointNano value)
    {
        return new FixedPointNano((value.RawValue / Scale) * Scale);
    }

    public static FixedPointNano Parse(string s, IFormatProvider? provider = null)
    {
        return FromDecimal(decimal.Parse(s, provider));
    }

    public static FixedPointNano Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        return FromDecimal(decimal.Parse(s, provider));
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (s is not null && decimal.TryParse(s, provider, out var d))
        {
            try
            {
                result = FromDecimal(d);
                return true;
            }
            catch (OverflowException) { }
        }

        result = Zero;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (decimal.TryParse(s, provider, out var d))
        {
            try
            {
                result = FromDecimal(d);
                return true;
            }
            catch (OverflowException) { }
        }

        result = Zero;
        return false;
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is not FixedPointNano other)
        {
            throw new ArgumentException("Object must be a FixedPointNano.", nameof(obj));
        }

        return CompareTo(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(FixedPointNano other)
    {
        return RawValue.CompareTo(other.RawValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(FixedPointNano other)
    {
        return RawValue == other.RawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is FixedPointNano other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return RawValue.GetHashCode();
    }

    /// <summary>Converts this value to <see cref="decimal"/>. The result is exact to <see cref="DecimalPlaces"/> decimal places.</summary>
    public decimal ToDecimal()
    {
        return RawValue / (decimal)Scale;
    }

    /// <summary>Converts this value to <see cref="double"/>. Precision beyond ~15 significant digits may be lost.</summary>
    public double ToDouble()
    {
        return RawValue / (double)Scale;
    }

    /// <summary>Converts this value to <see cref="float"/>. Significant precision loss is expected.</summary>
    public float ToSingle()
    {
        return RawValue / (float)Scale;
    }

    /// <summary>Converts this value to <see cref="Half"/>. Significant precision loss is expected.</summary>
    public Half ToHalf()
    {
        return (Half)ToSingle();
    }

    public BigInteger ToBigInteger()
    {
        return new BigInteger(RawValue / Scale);
    }

    public Int128 ToInt128()
    {
        return RawValue / Scale;
    }

    public UInt128 ToUInt128()
    {
        var truncatedValue = RawValue / Scale;
        return checked((UInt128)truncatedValue);
    }

    /// <summary>Returns a string representation using the current culture's format.</summary>
    public override string ToString()
    {
        return ToDecimal().ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>Returns a formatted string using the specified numeric format string and current culture.</summary>
    public string ToString(string? format)
    {
        return ToDecimal().ToString(format, CultureInfo.CurrentCulture);
    }

    public string ToString(IFormatProvider? formatProvider)
    {
        return ToDecimal().ToString(formatProvider);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToDecimal().ToString(format, formatProvider);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return ToDecimal().TryFormat(destination, out charsWritten, format, provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator +(FixedPointNano left, FixedPointNano right)
    {
        return new FixedPointNano(checked(left.RawValue + right.RawValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator -(FixedPointNano left, FixedPointNano right)
    {
        return new FixedPointNano(checked(left.RawValue - right.RawValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator -(FixedPointNano value)
    {
        return new FixedPointNano(checked(-value.RawValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator *(FixedPointNano left, FixedPointNano right)
    {
        var product = (Int128)left.RawValue * right.RawValue;
        return FromRawChecked(DivideRoundedToNearestEven(product, Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator /(FixedPointNano left, FixedPointNano right)
    {
        if (right.RawValue == 0)
        {
            throw new DivideByZeroException();
        }

        var numerator = (Int128)left.RawValue * Scale;
        return FromRawChecked(DivideRoundedToNearestEven(numerator, right.RawValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator %(FixedPointNano left, FixedPointNano right)
    {
        if (right.RawValue == 0)
        {
            throw new DivideByZeroException();
        }

        return new FixedPointNano(left.RawValue % right.RawValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FixedPointNano left, FixedPointNano right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FixedPointNano left, FixedPointNano right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue < right.RawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue > right.RawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(byte value)
    {
        return FromInteger((long)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(sbyte value)
    {
        return FromInteger((long)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(short value)
    {
        return FromInteger((long)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(ushort value)
    {
        return FromInteger((long)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(int value)
    {
        return FromInteger((long)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(uint value)
    {
        return FromInteger((ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(long value)
    {
        return FromInteger(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator FixedPointNano(ulong value)
    {
        return FromInteger(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FixedPointNano(nint value)
    {
        return FromInteger((long)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator FixedPointNano(nuint value)
    {
        return FromInteger((ulong)value);
    }

    public static explicit operator FixedPointNano(Half value)
    {
        return FromHalf(value);
    }

    public static explicit operator FixedPointNano(float value)
    {
        return FromSingle(value);
    }

    public static explicit operator FixedPointNano(double value)
    {
        return FromDouble(value);
    }

    public static explicit operator FixedPointNano(decimal value)
    {
        return FromDecimal(value);
    }

    public static explicit operator FixedPointNano(Int128 value)
    {
        return FromInteger(value);
    }

    public static explicit operator FixedPointNano(UInt128 value)
    {
        return FromInteger(value);
    }

    public static explicit operator FixedPointNano(BigInteger value)
    {
        return FromInteger(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator byte(FixedPointNano value)
    {
        return checked((byte)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator sbyte(FixedPointNano value)
    {
        return checked((sbyte)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator short(FixedPointNano value)
    {
        return checked((short)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ushort(FixedPointNano value)
    {
        return checked((ushort)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(FixedPointNano value)
    {
        return checked((int)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator uint(FixedPointNano value)
    {
        return checked((uint)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator long(FixedPointNano value)
    {
        return value.RawValue / Scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ulong(FixedPointNano value)
    {
        return checked((ulong)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator nint(FixedPointNano value)
    {
        return checked((nint)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator nuint(FixedPointNano value)
    {
        return checked((nuint)(value.RawValue / Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Half(FixedPointNano value)
    {
        return value.ToHalf();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator float(FixedPointNano value)
    {
        return value.ToSingle();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double(FixedPointNano value)
    {
        return value.ToDouble();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator decimal(FixedPointNano value)
    {
        return value.ToDecimal();
    }

    public static explicit operator Int128(FixedPointNano value)
    {
        return value.ToInt128();
    }

    public static explicit operator UInt128(FixedPointNano value)
    {
        return value.ToUInt128();
    }

    public static explicit operator BigInteger(FixedPointNano value)
    {
        return value.ToBigInteger();
    }

    TypeCode IConvertible.GetTypeCode()
    {
        return TypeCode.Object;
    }

    bool IConvertible.ToBoolean(IFormatProvider? provider)
    {
        return RawValue != 0;
    }

    byte IConvertible.ToByte(IFormatProvider? provider)
    {
        return checked((byte)this);
    }

    char IConvertible.ToChar(IFormatProvider? provider)
    {
        throw new InvalidCastException("FixedPointNano cannot be converted to Char.");
    }

    DateTime IConvertible.ToDateTime(IFormatProvider? provider)
    {
        throw new InvalidCastException("FixedPointNano cannot be converted to DateTime.");
    }

    decimal IConvertible.ToDecimal(IFormatProvider? provider)
    {
        return ToDecimal();
    }

    double IConvertible.ToDouble(IFormatProvider? provider)
    {
        return ToDouble();
    }

    short IConvertible.ToInt16(IFormatProvider? provider)
    {
        return checked((short)this);
    }

    int IConvertible.ToInt32(IFormatProvider? provider)
    {
        return checked((int)this);
    }

    long IConvertible.ToInt64(IFormatProvider? provider)
    {
        return checked((long)this);
    }

    sbyte IConvertible.ToSByte(IFormatProvider? provider)
    {
        return checked((sbyte)this);
    }

    float IConvertible.ToSingle(IFormatProvider? provider)
    {
        return ToSingle();
    }

    string IConvertible.ToString(IFormatProvider? provider)
    {
        return ToString(provider);
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(conversionType);

        if (conversionType == typeof(FixedPointNano))
        {
            return this;
        }

        if (conversionType == typeof(Half))
        {
            return (Half)this;
        }

        if (conversionType == typeof(Int128))
        {
            return (Int128)this;
        }

        if (conversionType == typeof(UInt128))
        {
            return (UInt128)this;
        }

        if (conversionType == typeof(BigInteger))
        {
            return (BigInteger)this;
        }

        return Convert.ChangeType(ToDecimal(), conversionType, provider)!;
    }

    ushort IConvertible.ToUInt16(IFormatProvider? provider)
    {
        return checked((ushort)this);
    }

    uint IConvertible.ToUInt32(IFormatProvider? provider)
    {
        return checked((uint)this);
    }

    ulong IConvertible.ToUInt64(IFormatProvider? provider)
    {
        return checked((ulong)this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FixedPointNano FromRawChecked(Int128 rawValue)
    {
        return new FixedPointNano(checked((long)rawValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FixedPointNano FromInteger(long value)
    {
        return new FixedPointNano(checked(value * Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FixedPointNano FromInteger(ulong value)
    {
        if (value > long.MaxValue / (ulong)Scale)
        {
            throw new OverflowException("The value is outside the range of FixedPointNano.");
        }

        return new FixedPointNano(checked((long)value * Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FixedPointNano FromInteger(Int128 value)
    {
        return FromRawChecked(checked(value * Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FixedPointNano FromInteger(UInt128 value)
    {
        if (value > (UInt128)(long.MaxValue / Scale))
        {
            throw new OverflowException("The value is outside the range of FixedPointNano.");
        }

        return new FixedPointNano(checked((long)value * Scale));
    }

    private static FixedPointNano FromInteger(BigInteger value)
    {
        var rawValue = value * Scale;
        if (rawValue < long.MinValue || rawValue > long.MaxValue)
        {
            throw new OverflowException("The value is outside the range of FixedPointNano.");
        }

        return new FixedPointNano((long)rawValue);
    }

    private static Int128 DivideRoundedToNearestEven(Int128 numerator, Int128 denominator)
    {
        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        if (numerator < 0)
        {
            return -DivideRoundedToNearestEven(-numerator, denominator);
        }

        var quotient = numerator / denominator;
        var remainder = numerator % denominator;
        var twiceRemainder = remainder * 2;
        if (twiceRemainder < denominator)
        {
            return quotient;
        }

        if (twiceRemainder > denominator)
        {
            return quotient + 1;
        }

        return quotient % 2 == 0 ? quotient : quotient + 1;
    }

    private static long RoundRaw(long rawValue, long quantum, MidpointRounding rounding)
    {
        var quotient = rawValue / quantum;
        var remainder = rawValue % quantum;
        if (remainder == 0)
        {
            return rawValue;
        }

        var sign = rawValue < 0 ? -1L : 1L;
        var absoluteRemainder = remainder < 0 ? -remainder : remainder;
        var twiceRemainder = absoluteRemainder * 2;

        long adjustedQuotient;
        if (rounding == MidpointRounding.ToEven)
        {
            adjustedQuotient = twiceRemainder < quantum
                ? quotient
                : twiceRemainder > quantum
                    ? checked(quotient + sign)
                    : quotient % 2 == 0 ? quotient : checked(quotient + sign);
        }
        else if (rounding == MidpointRounding.AwayFromZero)
        {
            adjustedQuotient = twiceRemainder >= quantum ? checked(quotient + sign) : quotient;
        }
        else if (rounding == MidpointRounding.ToZero)
        {
            adjustedQuotient = quotient;
        }
        else if (rounding == MidpointRounding.ToNegativeInfinity)
        {
            adjustedQuotient = rawValue < 0 ? checked(quotient - 1) : quotient;
        }
        else
        {
            adjustedQuotient = rawValue > 0 ? checked(quotient + 1) : quotient;
        }

        return checked(adjustedQuotient * quantum);
    }

    private static void ValidateRounding(MidpointRounding rounding)
    {
        _ = rounding switch
        {
            MidpointRounding.ToEven => rounding,
            MidpointRounding.AwayFromZero => rounding,
            MidpointRounding.ToZero => rounding,
            MidpointRounding.ToNegativeInfinity => rounding,
            MidpointRounding.ToPositiveInfinity => rounding,
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, "Unsupported midpoint rounding mode."),
        };
    }

    private static UInt128 SquareRootRoundedToNearestEven(ulong value)
    {
        var floor = (ulong)Math.Sqrt(value);
        if (floor > uint.MaxValue)
        {
            floor = uint.MaxValue;
        }

        while (floor * floor > value)
        {
            floor--;
        }

        while (floor < uint.MaxValue)
        {
            var candidate = floor + 1;
            if (candidate * candidate > value)
            {
                break;
            }

            floor = candidate;
        }

        var next = floor + 1;
        var floorSquare = floor * floor;
        var nextSquare = (UInt128)next * next;
        var distanceToFloor = value - floorSquare;
        var distanceToNext = nextSquare - value;
        if (distanceToNext < distanceToFloor)
        {
            return next;
        }

        return floor;
    }

    private static UInt128 SquareRootRoundedToNearestEven(UInt128 value)
    {
        var floor = (UInt128)Math.Sqrt((double)value);
        while (floor * floor > value)
        {
            floor--;
        }

        while (true)
        {
            var candidate = floor + 1;
            if (candidate * candidate > value)
            {
                break;
            }

            floor = candidate;
        }

        var next = floor + 1;
        var floorSquare = floor * floor;
        var nextSquare = next * next;
        var distanceToFloor = value - floorSquare;
        var distanceToNext = nextSquare - value;
        if (distanceToNext < distanceToFloor)
        {
            return next;
        }

        return floor;
    }

    private static void ThrowIfInvalidFloatingPoint<T>(T value) where T : IFloatingPointIeee754<T>
    {
        if (T.IsNaN(value) || T.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }
    }
}
