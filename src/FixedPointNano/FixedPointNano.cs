using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace FixedPointNano;

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
    /// <summary>The number of decimal places used by this type (9).</summary>
    public const int DecimalPlaces = 9;

    /// <summary>The scaling factor applied to the raw <see cref="long"/> storage value (10^9).</summary>
    public const long Scale = 1_000_000_000L;

    /// <summary>Represents the value zero (0).</summary>
    public static FixedPointNano Zero { get; } = new(0L);

    /// <summary>Represents the value one (1).</summary>
    public static FixedPointNano One { get; } = new(Scale);

    /// <summary>Represents the largest possible value (~9.223372036 billion).</summary>
    public static FixedPointNano MaxValue { get; } = new(long.MaxValue);

    /// <summary>Represents the smallest possible value (~-9.223372036 billion).</summary>
    public static FixedPointNano MinValue { get; } = new(long.MinValue);

    /// <summary>Initialises a new instance from a raw scaled <see cref="long"/> value.</summary>
    /// <param name="rawValue">The raw value, already multiplied by <see cref="Scale"/>.</param>
    public FixedPointNano(long rawValue)
    {
        RawValue = rawValue;
    }

    /// <summary>Gets the underlying raw scaled <see cref="long"/> value.</summary>
    public long RawValue { get; }

    /// <summary>Returns the absolute value of <paramref name="value"/>.</summary>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> equals <see cref="MinValue"/>.</exception>
    public static FixedPointNano Abs(FixedPointNano value)
    {
        return value.RawValue < 0
            ? new FixedPointNano(checked(-value.RawValue))
            : value;
    }

    /// <summary>Returns the smallest integral value greater than or equal to <paramref name="value"/>.</summary>
    public static FixedPointNano Ceiling(FixedPointNano value)
    {
        return FromDecimal(decimal.Ceiling(value.ToDecimal()));
    }

    /// <summary>Returns the largest integral value less than or equal to <paramref name="value"/>.</summary>
    public static FixedPointNano Floor(FixedPointNano value)
    {
        return FromDecimal(decimal.Floor(value.ToDecimal()));
    }

    /// <summary>
    /// Creates a <see cref="FixedPointNano"/> from a <see cref="decimal"/> value,
    /// rounding to <see cref="DecimalPlaces"/> decimal places using <see cref="MidpointRounding.ToEven"/>.
    /// </summary>
    public static FixedPointNano FromDecimal(decimal value)
    {
        var scaledValue = decimal.Round(value * Scale, 0, MidpointRounding.ToEven);
        return new FixedPointNano(decimal.ToInt64(scaledValue));
    }

    /// <summary>Creates a <see cref="FixedPointNano"/> from a <see cref="double"/> value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static FixedPointNano FromDouble(double value)
    {
        ThrowIfInvalidFloatingPoint(value);
        return FromDecimal((decimal)value);
    }

    /// <summary>Creates a <see cref="FixedPointNano"/> from a <see cref="Half"/> value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static FixedPointNano FromHalf(Half value)
    {
        return FromSingle((float)value);
    }

    /// <summary>Creates a <see cref="FixedPointNano"/> directly from a raw scaled <see cref="long"/> value without scaling.</summary>
    public static FixedPointNano FromRaw(long rawValue)
    {
        return new FixedPointNano(rawValue);
    }

    /// <summary>Creates a <see cref="FixedPointNano"/> from a <see cref="float"/> value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static FixedPointNano FromSingle(float value)
    {
        ThrowIfInvalidFloatingPoint(value);
        return FromDecimal((decimal)value);
    }

    /// <summary>Returns the greater of two <see cref="FixedPointNano"/> values.</summary>
    public static FixedPointNano Max(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue ? left : right;
    }

    /// <summary>Returns the lesser of two <see cref="FixedPointNano"/> values.</summary>
    public static FixedPointNano Min(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue ? left : right;
    }

    /// <summary>
    /// Rounds <paramref name="value"/> to a specified number of decimal places.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="decimals">The number of decimal places (0 to <see cref="DecimalPlaces"/>).</param>
    /// <param name="rounding">The midpoint rounding strategy. Defaults to <see cref="MidpointRounding.ToEven"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="decimals"/> is less than 0 or greater than <see cref="DecimalPlaces"/>.
    /// </exception>
    public static FixedPointNano Round(FixedPointNano value, int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        if (decimals is < 0 or > DecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimals must be between 0 and {DecimalPlaces}.");
        }

        return FromDecimal(decimal.Round(value.ToDecimal(), decimals, rounding));
    }

    /// <summary>Returns the integral part of <paramref name="value"/>, discarding any fractional digits toward zero.</summary>
    public static FixedPointNano Truncate(FixedPointNano value)
    {
        return new FixedPointNano(value.RawValue / Scale * Scale);
    }

    /// <summary>Converts the string representation of a number to its <see cref="FixedPointNano"/> equivalent.</summary>
    /// <param name="s">A string containing the number to convert.</param>
    /// <param name="provider">An optional format provider. Defaults to the current culture.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid number.</exception>
    /// <exception cref="OverflowException">Thrown when the parsed value is outside the representable range.</exception>
    public static FixedPointNano Parse(string s, IFormatProvider? provider = null)
    {
        return FromDecimal(decimal.Parse(s, NumberStyles.Number, provider));
    }

    /// <summary>Converts the span representation of a number to its <see cref="FixedPointNano"/> equivalent.</summary>
    /// <param name="s">A read-only span containing the number to convert.</param>
    /// <param name="provider">An optional format provider. Defaults to the current culture.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid number.</exception>
    /// <exception cref="OverflowException">Thrown when the parsed value is outside the representable range.</exception>
    public static FixedPointNano Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        return FromDecimal(decimal.Parse(s, NumberStyles.Number, provider));
    }

    /// <summary>
    /// Tries to convert the string representation of a number to its <see cref="FixedPointNano"/> equivalent.
    /// Returns <see langword="false"/> if the conversion fails.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (decimal.TryParse(s, NumberStyles.Number, provider, out var d))
        {
            result = FromDecimal(d);
            return true;
        }

        result = Zero;
        return false;
    }

    /// <summary>
    /// Tries to convert the span representation of a number to its <see cref="FixedPointNano"/> equivalent.
    /// Returns <see langword="false"/> if the conversion fails.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (decimal.TryParse(s, NumberStyles.Number, provider, out var d))
        {
            result = FromDecimal(d);
            return true;
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

    public int CompareTo(FixedPointNano other)
    {
        return RawValue.CompareTo(other.RawValue);
    }

    public bool Equals(FixedPointNano other)
    {
        return RawValue == other.RawValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is FixedPointNano other && Equals(other);
    }

    public override int GetHashCode()
    {
        return RawValue.GetHashCode();
    }

    /// <summary>Converts this value to a <see cref="decimal"/>.</summary>
    public decimal ToDecimal()
    {
        return RawValue / (decimal)Scale;
    }

    /// <summary>Converts this value to a <see cref="double"/>. May lose precision.</summary>
    public double ToDouble()
    {
        return RawValue / (double)Scale;
    }

    /// <summary>Converts this value to a <see cref="float"/>. May lose precision.</summary>
    public float ToSingle()
    {
        return RawValue / (float)Scale;
    }

    /// <summary>Converts this value to a <see cref="Half"/>. May lose precision.</summary>
    public Half ToHalf()
    {
        return (Half)ToSingle();
    }

    /// <summary>Converts this value to a <see cref="BigInteger"/>, truncating any fractional part toward zero.</summary>
    public BigInteger ToBigInteger()
    {
        return new BigInteger(RawValue / Scale);
    }

    /// <summary>Converts this value to an <see cref="Int128"/>, truncating any fractional part toward zero.</summary>
    public Int128 ToInt128()
    {
        return RawValue / Scale;
    }

    /// <summary>Converts this value to a <see cref="UInt128"/>, truncating any fractional part toward zero.</summary>
    /// <exception cref="OverflowException">Thrown when this value is negative.</exception>
    public UInt128 ToUInt128()
    {
        var truncatedValue = RawValue / Scale;
        return checked((UInt128)truncatedValue);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToDecimal().ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>Converts this value to its string representation using the specified format and the current culture.</summary>
    public string ToString(string? format)
    {
        return ToDecimal().ToString(format, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc/>
    public string ToString(IFormatProvider? formatProvider)
    {
        return ToDecimal().ToString(formatProvider);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToDecimal().ToString(format, formatProvider);
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return ToDecimal().TryFormat(destination, out charsWritten, format, provider);
    }

    public static FixedPointNano operator +(FixedPointNano left, FixedPointNano right)
    {
        return new FixedPointNano(checked(left.RawValue + right.RawValue));
    }

    public static FixedPointNano operator -(FixedPointNano left, FixedPointNano right)
    {
        return new FixedPointNano(checked(left.RawValue - right.RawValue));
    }

    public static FixedPointNano operator -(FixedPointNano value)
    {
        return new FixedPointNano(checked(-value.RawValue));
    }

    public static FixedPointNano operator *(FixedPointNano left, FixedPointNano right)
    {
        return FromDecimal(left.ToDecimal() * right.ToDecimal());
    }

    public static FixedPointNano operator /(FixedPointNano left, FixedPointNano right)
    {
        if (right.RawValue == 0)
        {
            throw new DivideByZeroException();
        }

        return FromDecimal(left.ToDecimal() / right.ToDecimal());
    }

    public static FixedPointNano operator %(FixedPointNano left, FixedPointNano right)
    {
        if (right.RawValue == 0)
        {
            throw new DivideByZeroException();
        }

        return FromDecimal(left.ToDecimal() % right.ToDecimal());
    }

    public static bool operator ==(FixedPointNano left, FixedPointNano right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FixedPointNano left, FixedPointNano right)
    {
        return !left.Equals(right);
    }

    public static bool operator <(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue < right.RawValue;
    }

    public static bool operator <=(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue;
    }

    public static bool operator >(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue > right.RawValue;
    }

    public static bool operator >=(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue;
    }

    public static implicit operator FixedPointNano(byte value)
    {
        return new FixedPointNano(value * Scale);
    }

    public static implicit operator FixedPointNano(sbyte value)
    {
        return new FixedPointNano(value * Scale);
    }

    public static implicit operator FixedPointNano(short value)
    {
        return new FixedPointNano(value * Scale);
    }

    public static implicit operator FixedPointNano(ushort value)
    {
        return new FixedPointNano(value * Scale);
    }

    public static implicit operator FixedPointNano(int value)
    {
        return new FixedPointNano((long)value * Scale);
    }

    public static implicit operator FixedPointNano(uint value)
    {
        return new FixedPointNano((long)value * Scale);
    }

    public static implicit operator FixedPointNano(long value)
    {
        return new FixedPointNano(checked(value * Scale));
    }

    public static explicit operator FixedPointNano(ulong value)
    {
        return new FixedPointNano(checked((long)value * Scale));
    }

    public static implicit operator FixedPointNano(nint value)
    {
        return new FixedPointNano(checked((long)value * Scale));
    }

    public static explicit operator FixedPointNano(nuint value)
    {
        return new FixedPointNano(checked((long)value * Scale));
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
        return FromDecimal(decimal.CreateChecked(value));
    }

    public static explicit operator FixedPointNano(UInt128 value)
    {
        return FromDecimal(decimal.CreateChecked(value));
    }

    public static explicit operator FixedPointNano(BigInteger value)
    {
        return FromDecimal((decimal)value);
    }

    public static explicit operator byte(FixedPointNano value)
    {
        return checked((byte)(value.RawValue / Scale));
    }

    public static explicit operator sbyte(FixedPointNano value)
    {
        return checked((sbyte)(value.RawValue / Scale));
    }

    public static explicit operator short(FixedPointNano value)
    {
        return checked((short)(value.RawValue / Scale));
    }

    public static explicit operator ushort(FixedPointNano value)
    {
        return checked((ushort)(value.RawValue / Scale));
    }

    public static explicit operator int(FixedPointNano value)
    {
        return checked((int)(value.RawValue / Scale));
    }

    public static explicit operator uint(FixedPointNano value)
    {
        return checked((uint)(value.RawValue / Scale));
    }

    public static explicit operator long(FixedPointNano value)
    {
        return value.RawValue / Scale;
    }

    public static explicit operator ulong(FixedPointNano value)
    {
        return checked((ulong)(value.RawValue / Scale));
    }

    public static explicit operator nint(FixedPointNano value)
    {
        return checked((nint)(value.RawValue / Scale));
    }

    public static explicit operator nuint(FixedPointNano value)
    {
        return checked((nuint)(value.RawValue / Scale));
    }

    public static explicit operator Half(FixedPointNano value)
    {
        return value.ToHalf();
    }

    public static explicit operator float(FixedPointNano value)
    {
        return value.ToSingle();
    }

    public static explicit operator double(FixedPointNano value)
    {
        return value.ToDouble();
    }

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

    private static void ThrowIfInvalidFloatingPoint<T>(T value) where T : IFloatingPointIeee754<T>
    {
        if (!T.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }
    }
}
