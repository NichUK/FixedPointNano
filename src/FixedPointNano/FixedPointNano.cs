using System;
using System.Diagnostics;
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
    IConvertible
{
    public const int DecimalPlaces = 9;
    public const long Scale = 1_000_000_000L;

    public static FixedPointNano Zero { get; } = new(0L);
    public static FixedPointNano One { get; } = new(Scale);

    public FixedPointNano(long rawValue)
    {
        RawValue = rawValue;
    }

    public long RawValue { get; }

    public static FixedPointNano Abs(FixedPointNano value)
    {
        return value.RawValue < 0
            ? new FixedPointNano(checked(-value.RawValue))
            : value;
    }

    public static FixedPointNano Ceiling(FixedPointNano value)
    {
        return FromDecimal(decimal.Ceiling(value.ToDecimal()));
    }

    public static FixedPointNano Floor(FixedPointNano value)
    {
        return FromDecimal(decimal.Floor(value.ToDecimal()));
    }

    public static FixedPointNano FromDecimal(decimal value)
    {
        var scaledValue = decimal.Round(value * Scale, 0, MidpointRounding.ToEven);
        return new FixedPointNano(decimal.ToInt64(scaledValue));
    }

    public static FixedPointNano FromDouble(double value)
    {
        ThrowIfInvalidFloatingPoint(value);
        return FromDecimal((decimal)value);
    }

    public static FixedPointNano FromHalf(Half value)
    {
        return FromSingle((float)value);
    }

    public static FixedPointNano FromRaw(long rawValue)
    {
        return new FixedPointNano(rawValue);
    }

    public static FixedPointNano FromSingle(float value)
    {
        ThrowIfInvalidFloatingPoint(value);
        return FromDecimal((decimal)value);
    }

    public static FixedPointNano Max(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue ? left : right;
    }

    public static FixedPointNano Min(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue ? left : right;
    }

    public static FixedPointNano Round(FixedPointNano value, int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        if (decimals is < 0 or > DecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimals must be between 0 and {DecimalPlaces}.");
        }

        return FromDecimal(decimal.Round(value.ToDecimal(), decimals, rounding));
    }

    public static FixedPointNano Truncate(FixedPointNano value)
    {
        return FromDecimal(decimal.Truncate(value.ToDecimal()));
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

    public decimal ToDecimal()
    {
        return RawValue / (decimal)Scale;
    }

    public double ToDouble()
    {
        return RawValue / (double)Scale;
    }

    public float ToSingle()
    {
        return RawValue / (float)Scale;
    }

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

    public override string ToString()
    {
        return ToDecimal().ToString(CultureInfo.CurrentCulture);
    }

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
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(sbyte value)
    {
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(short value)
    {
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(ushort value)
    {
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(int value)
    {
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(uint value)
    {
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(long value)
    {
        return FromDecimal(value);
    }

    public static explicit operator FixedPointNano(ulong value)
    {
        return FromDecimal(value);
    }

    public static implicit operator FixedPointNano(nint value)
    {
        return FromDecimal(value);
    }

    public static explicit operator FixedPointNano(nuint value)
    {
        return FromDecimal(value);
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
        return checked((byte)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator sbyte(FixedPointNano value)
    {
        return checked((sbyte)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator short(FixedPointNano value)
    {
        return checked((short)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator ushort(FixedPointNano value)
    {
        return checked((ushort)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator int(FixedPointNano value)
    {
        return checked((int)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator uint(FixedPointNano value)
    {
        return checked((uint)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator long(FixedPointNano value)
    {
        return checked((long)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator ulong(FixedPointNano value)
    {
        return checked((ulong)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator nint(FixedPointNano value)
    {
        return checked((nint)decimal.Truncate(value.ToDecimal()));
    }

    public static explicit operator nuint(FixedPointNano value)
    {
        return checked((nuint)decimal.Truncate(value.ToDecimal()));
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

    private static void ThrowIfInvalidFloatingPoint(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }
    }

    private static void ThrowIfInvalidFloatingPoint(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }
    }
}
