using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Seerstone;

/// <summary>
/// A fixed-point numeric type backed by a <see langword="long"/>, with a scale of
/// <see cref="Scale"/> (10⁹) giving 9 decimal places of precision.
/// </summary>
/// <remarks>
/// All arithmetic uses checked integer operations and banker's rounding
/// (<see cref="MidpointRounding.ToEven"/>) by default.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct FixedPointNano :
    IComparable,
    IComparable<FixedPointNano>,
    IEquatable<FixedPointNano>,
    IFormattable,
    ISpanFormattable,
    IConvertible,
    IAdditiveIdentity<FixedPointNano, FixedPointNano>,
    IMultiplicativeIdentity<FixedPointNano, FixedPointNano>,
    IAdditionOperators<FixedPointNano, FixedPointNano, FixedPointNano>,
    ISubtractionOperators<FixedPointNano, FixedPointNano, FixedPointNano>,
    IMultiplyOperators<FixedPointNano, FixedPointNano, FixedPointNano>,
    IDivisionOperators<FixedPointNano, FixedPointNano, FixedPointNano>,
    IModulusOperators<FixedPointNano, FixedPointNano, FixedPointNano>,
    IUnaryNegationOperators<FixedPointNano, FixedPointNano>,
    IUnaryPlusOperators<FixedPointNano, FixedPointNano>,
    IIncrementOperators<FixedPointNano>,
    IDecrementOperators<FixedPointNano>,
    IComparisonOperators<FixedPointNano, FixedPointNano, bool>,
    IParsable<FixedPointNano>,
    ISpanParsable<FixedPointNano>,
    IMinMaxValue<FixedPointNano>,
    INumber<FixedPointNano>,
    ISignedNumber<FixedPointNano>
{
    /// <summary>The number of decimal places supported by <see cref="FixedPointNano"/>.</summary>
    public const int DecimalPlaces = 9;

    /// <summary>
    /// The scale factor used to store values as raw integers.
    /// Equals 10⁹ (1,000,000,000).
    /// </summary>
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

    public static FixedPointNano MaxValue { get; } = new(long.MaxValue);
    public static FixedPointNano MinValue { get; } = new(long.MinValue);
    public static FixedPointNano Zero { get; } = new(0L);
    public static FixedPointNano One { get; } = new(Scale);
    public static FixedPointNano Epsilon { get; } = new(1L);
    public static FixedPointNano NegativeOne { get; } = new(-Scale);

    static FixedPointNano IAdditiveIdentity<FixedPointNano, FixedPointNano>.AdditiveIdentity => Zero;
    static FixedPointNano IMultiplicativeIdentity<FixedPointNano, FixedPointNano>.MultiplicativeIdentity => One;

    public FixedPointNano(long rawValue)
    {
        RawValue = rawValue;
    }

    /// <summary>
    /// Gets the raw scaled integer value.
    /// Divide by <see cref="Scale"/> to obtain the represented real number.
    /// </summary>
    public long RawValue { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Abs(FixedPointNano value)
    {
        return value.RawValue < 0
            ? new FixedPointNano(checked(-value.RawValue))
            : value;
    }

    /// <summary>
    /// Returns the smallest integral value that is greater than or equal to <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to ceiling.</param>
    /// <returns>The ceiling of <paramref name="value"/>.</returns>
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

    /// <summary>
    /// Returns the largest integral value that is less than or equal to <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to floor.</param>
    /// <returns>The floor of <paramref name="value"/>.</returns>
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

    public static FixedPointNano FractionalPart(FixedPointNano value)
    {
        return new FixedPointNano(value.RawValue % Scale);
    }

    public static bool IsInteger(FixedPointNano value)
    {
        return value.RawValue % Scale == 0;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="value"/> is strictly greater than zero.</summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPositive(FixedPointNano value)
    {
        return value.RawValue > 0;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="value"/> is strictly less than zero.</summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNegative(FixedPointNano value)
    {
        return value.RawValue < 0;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="value"/> is exactly zero.</summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(FixedPointNano value)
    {
        return value.RawValue == 0;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="value"/> is an even integer.</summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEvenInteger(FixedPointNano value)
    {
        return IsInteger(value) && (value.RawValue / Scale) % 2L == 0L;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="value"/> is an odd integer.</summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOddInteger(FixedPointNano value)
    {
        return IsInteger(value) && (value.RawValue / Scale) % 2L != 0L;
    }

    public static FixedPointNano FromDecimal(decimal value)
    {
        var scaledValue = decimal.Round(value * Scale, 0, MidpointRounding.ToEven);
        return new FixedPointNano(decimal.ToInt64(scaledValue));
    }

    /// <summary>
    /// Creates a <see cref="FixedPointNano"/> from a <see cref="double"/> value
    /// using banker's rounding (<see cref="MidpointRounding.ToEven"/>).
    /// </summary>
    /// <param name="value">The double value to convert. Must be finite.</param>
    /// <returns>A <see cref="FixedPointNano"/> representing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is <see cref="double.NaN"/> or infinite.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the scaled value overflows <see cref="long"/> range.
    /// </exception>
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

    /// <summary>
    /// Creates a <see cref="FixedPointNano"/> from a <see cref="Half"/> value.
    /// </summary>
    /// <param name="value">The half-precision float to convert. Must be finite.</param>
    /// <returns>A <see cref="FixedPointNano"/> representing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is <see cref="Half.NaN"/> or infinite.
    /// </exception>
    public static FixedPointNano FromHalf(Half value)
    {
        return FromSingle((float)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano FromRaw(long rawValue)
    {
        return new FixedPointNano(rawValue);
    }

    /// <summary>
    /// Creates a <see cref="FixedPointNano"/> from a <see cref="float"/> value.
    /// </summary>
    /// <param name="value">The single-precision float to convert. Must be finite.</param>
    /// <returns>A <see cref="FixedPointNano"/> representing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is <see cref="float.NaN"/> or infinite.
    /// </exception>
    public static FixedPointNano FromSingle(float value)
    {
        ThrowIfInvalidFloatingPoint(value);
        return FromDouble(value);
    }

    public static FixedPointNano CopySign(FixedPointNano value, FixedPointNano sign)
    {
        if (sign.RawValue >= 0)
        {
            return value.RawValue < 0 ? new FixedPointNano(checked(-value.RawValue)) : value;
        }

        return value.RawValue > 0 ? new FixedPointNano(-value.RawValue) : value;
    }

    public static FixedPointNano Lerp(FixedPointNano start, FixedPointNano end, FixedPointNano amount)
    {
        return start + (end - start) * amount;
    }

    public static FixedPointNano Max(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue ? left : right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Min(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue ? left : right;
    }

    public static FixedPointNano Clamp(FixedPointNano value, FixedPointNano min, FixedPointNano max)
    {
        if (min.RawValue > max.RawValue)
        {
            throw new ArgumentException($"'{nameof(min)}' cannot be greater than '{nameof(max)}'.");
        }

        if (value.RawValue < min.RawValue)
        {
            return min;
        }

        if (value.RawValue > max.RawValue)
        {
            return max;
        }

        return value;
    }

    public static int Sign(FixedPointNano value)
    {
        return value.RawValue switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0,
        };
    }

    public static FixedPointNano Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    public static FixedPointNano Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException($"The input string '{s.ToString()}' was not in a correct format for FixedPointNano.");
        }

        return result;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out FixedPointNano result)
        => TryParse(s, NumberStyles.Number, provider, out result);

    public static bool TryParse(string? s, out FixedPointNano result)
        => TryParse(s, CultureInfo.InvariantCulture, out result);

    public static bool TryParse(ReadOnlySpan<char> s, out FixedPointNano result)
        => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <summary>Parses <paramref name="s"/> using the specified <see cref="NumberStyles"/>.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="style">The number styles to allow.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> cannot be parsed.</exception>
    public static FixedPointNano Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider = null)
    {
        if (!TryParse(s, style, provider, out var result))
        {
            throw new FormatException($"The input '{s}' is not a valid {nameof(FixedPointNano)}.");
        }

        return result;
    }

    /// <summary>Parses <paramref name="s"/> using the specified <see cref="NumberStyles"/>.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="style">The number styles to allow.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> cannot be parsed.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is null.</exception>
    public static FixedPointNano Parse(string s, NumberStyles style, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), style, provider);
    }

    /// <summary>Attempts to parse <paramref name="s"/> using the specified <see cref="NumberStyles"/>.</summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="style">The number styles to allow.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <param name="result">The parsed value on success; otherwise the default value.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out FixedPointNano result)
    {
        if (!decimal.TryParse(s, style, provider ?? CultureInfo.InvariantCulture, out var d))
        {
            result = default;
            return false;
        }

        try
        {
            result = FromDecimal(d);
            return true;
        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>Attempts to parse <paramref name="s"/> using the specified <see cref="NumberStyles"/>.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="style">The number styles to allow.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <param name="result">The parsed value on success; otherwise the default value.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out FixedPointNano result)
        => TryParse(s.AsSpan(), style, provider, out result);

    public static FixedPointNano Round(FixedPointNano value, int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        if (decimals is < 0 or > DecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimals must be between 0 and {DecimalPlaces}.");
        }

        ValidateRounding(rounding);
        return new FixedPointNano(RoundRaw(value.RawValue, s_roundingScales[decimals], rounding));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Divide(FixedPointNano value, int divisor)
    {
        return Divide(value, (long)divisor);
    }

    /// <summary>
    /// Divides <paramref name="value"/> by a <see cref="long"/> <paramref name="divisor"/>
    /// using banker's rounding (<see cref="MidpointRounding.ToEven"/>).
    /// </summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The long divisor. Must not be zero.</param>
    /// <returns>The quotient rounded to nearest even.</returns>
    /// <exception cref="DivideByZeroException">
    /// Thrown when <paramref name="divisor"/> is zero.
    /// </exception>
    public static FixedPointNano Divide(FixedPointNano value, long divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        return new FixedPointNano(DivideRoundedToNearestEven(value.RawValue, divisor));
    }

    /// <summary>
    /// Multiplies <paramref name="value"/> by the ratio
    /// <paramref name="numerator"/> / <paramref name="denominator"/>
    /// using banker's rounding (<see cref="MidpointRounding.ToEven"/>).
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="numerator">The ratio numerator.</param>
    /// <param name="denominator">The ratio denominator. Must not be zero.</param>
    /// <returns>The scaled result rounded to nearest even.</returns>
    /// <exception cref="DivideByZeroException">
    /// Thrown when <paramref name="denominator"/> is zero.
    /// </exception>
    public static FixedPointNano MultiplyRatio(FixedPointNano value, long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException();
        }

        var scaledNumerator = (Int128)value.RawValue * numerator;
        return FromRawChecked(DivideRoundedToNearestEven(scaledNumerator, denominator));
    }

    /// <summary>Returns the square of <paramref name="value"/>.</summary>
    /// <param name="value">The value to square.</param>
    /// <returns><paramref name="value"/> multiplied by itself.</returns>
    public static FixedPointNano Square(FixedPointNano value)
    {
        return value * value;
    }

    public static FixedPointNano Pow(FixedPointNano value, int exponent)
    {
        if (exponent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), "Exponent must not be negative.");
        }

        if (exponent == 0)
        {
            return One;
        }

        var result = One;
        var current = value;
        var remaining = exponent;
        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
            {
                result = result * current;
            }

            remaining >>= 1;
            if (remaining > 0)
            {
                current = current * current;
            }
        }

        return result;
    }

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

    /// <summary>
    /// Computes the population standard deviation from pre-aggregated raw statistics.
    /// </summary>
    /// <param name="sum">The sum of all observations.</param>
    /// <param name="sumOfRawSquares">
    /// The sum of <c>RawValue * RawValue</c> for all observations.
    /// </param>
    /// <param name="count">The number of observations. Must be greater than zero.</param>
    /// <returns>The population standard deviation as a <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the statistics arguments are invalid (see <see cref="PopulationVariance"/>).
    /// </exception>
    public static FixedPointNano PopulationStandardDeviation(FixedPointNano sum, Int128 sumOfRawSquares, int count)
    {
        return Sqrt(PopulationVariance(sum, sumOfRawSquares, count));
    }

    public static FixedPointNano SampleVariance(FixedPointNano sum, Int128 sumOfRawSquares, int count)
    {
        if (count < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 2.");
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

        var denominator = checked(countValue * (countValue - 1) * Scale);
        return FromRawChecked(DivideRoundedToNearestEven(numerator, denominator));
    }

    public static FixedPointNano SampleStandardDeviation(FixedPointNano sum, Int128 sumOfRawSquares, int count)
    {
        return Sqrt(SampleVariance(sum, sumOfRawSquares, count));
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Truncate(FixedPointNano value)
    {
        return new FixedPointNano((value.RawValue / Scale) * Scale);
    }

    public static FixedPointNano Frac(FixedPointNano value)
    {
        return new FixedPointNano(value.RawValue % Scale);
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

    public void Deconstruct(out long integerPart, out FixedPointNano fractionalPart)
    {
        integerPart = RawValue / Scale;
        fractionalPart = new FixedPointNano(RawValue % Scale);
    }

    public decimal ToDecimal()
    {
        return RawValue / (decimal)Scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToDouble()
    {
        return RawValue / (double)Scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ToSingle()
    {
        return RawValue / (float)Scale;
    }

    /// <summary>Converts this value to a <see cref="Half"/>.</summary>
    /// <returns>The value as a <see cref="Half"/>.</returns>
    public Half ToHalf()
    {
        return (Half)ToSingle();
    }

    /// <summary>
    /// Converts the integral part of this value to a <see cref="BigInteger"/>,
    /// discarding any fractional component.
    /// </summary>
    /// <returns>The truncated value as a <see cref="BigInteger"/>.</returns>
    public BigInteger ToBigInteger()
    {
        return new BigInteger(RawValue / Scale);
    }

    /// <summary>
    /// Converts the integral part of this value to an <see cref="Int128"/>,
    /// discarding any fractional component.
    /// </summary>
    /// <returns>The truncated value as an <see cref="Int128"/>.</returns>
    public Int128 ToInt128()
    {
        return RawValue / Scale;
    }

    /// <summary>
    /// Converts the integral part of this value to a <see cref="UInt128"/>,
    /// discarding any fractional component.
    /// </summary>
    /// <returns>The truncated value as a <see cref="UInt128"/>.</returns>
    /// <exception cref="OverflowException">Thrown when the value is negative.</exception>
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

    /// <summary>
    /// Formats this value using the specified numeric <paramref name="format"/> string.
    /// </summary>
    /// <param name="format">A standard or custom numeric format string, or <see langword="null"/>.</param>
    /// <returns>The formatted string.</returns>
    public string ToString(string? format)
    {
        return ToDecimal().ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Formats this value using the specified <paramref name="formatProvider"/>.
    /// </summary>
    /// <param name="formatProvider">
    /// An object that provides culture-specific formatting information,
    /// or <see langword="null"/> to use the current culture.
    /// </param>
    /// <returns>The formatted string.</returns>
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

    public static FixedPointNano operator +(FixedPointNano value)
    {
        return value;
    }

    public static FixedPointNano operator ++(FixedPointNano value)
    {
        return new FixedPointNano(checked(value.RawValue + Scale));
    }

    public static FixedPointNano operator --(FixedPointNano value)
    {
        return new FixedPointNano(checked(value.RawValue - Scale));
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

    /// <summary>Implicitly converts a <see cref="byte"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FixedPointNano(byte value)
    {
        return FromInteger((long)value);
    }

    /// <summary>Implicitly converts an <see cref="sbyte"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FixedPointNano(sbyte value)
    {
        return FromInteger((long)value);
    }

    /// <summary>Implicitly converts a <see cref="short"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FixedPointNano(short value)
    {
        return FromInteger((long)value);
    }

    /// <summary>Implicitly converts a <see cref="ushort"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FixedPointNano(ushort value)
    {
        return FromInteger((long)value);
    }

    /// <summary>Implicitly converts an <see cref="int"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FixedPointNano(int value)
    {
        return FromInteger((long)value);
    }

    /// <summary>Implicitly converts a <see cref="uint"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FixedPointNano(uint value)
    {
        return FromInteger((ulong)value);
    }

    /// <summary>Implicitly converts a <see cref="long"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown on arithmetic overflow.</exception>
    public static implicit operator FixedPointNano(long value)
    {
        return FromInteger(value);
    }

    /// <summary>Explicitly converts a <see cref="ulong"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value exceeds <see cref="FixedPointNano"/> range.</exception>
    public static explicit operator FixedPointNano(ulong value)
    {
        return FromInteger(value);
    }

    /// <summary>Implicitly converts an <see cref="nint"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown on arithmetic overflow.</exception>
    public static implicit operator FixedPointNano(nint value)
    {
        return FromInteger((long)value);
    }

    /// <summary>Explicitly converts a <see cref="nuint"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value exceeds <see cref="FixedPointNano"/> range.</exception>
    public static explicit operator FixedPointNano(nuint value)
    {
        return FromInteger((ulong)value);
    }

    /// <summary>Explicitly converts a <see cref="Half"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static explicit operator FixedPointNano(Half value)
    {
        return FromHalf(value);
    }

    /// <summary>Explicitly converts a <see cref="float"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static explicit operator FixedPointNano(float value)
    {
        return FromSingle(value);
    }

    /// <summary>Explicitly converts a <see cref="double"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is NaN or infinite.</exception>
    public static explicit operator FixedPointNano(double value)
    {
        return FromDouble(value);
    }

    /// <summary>Explicitly converts a <see cref="decimal"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator FixedPointNano(decimal value)
    {
        return FromDecimal(value);
    }

    /// <summary>Explicitly converts an <see cref="Int128"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown on arithmetic overflow.</exception>
    public static explicit operator FixedPointNano(Int128 value)
    {
        return FromInteger(value);
    }

    /// <summary>Explicitly converts a <see cref="UInt128"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value exceeds <see cref="FixedPointNano"/> range.</exception>
    public static explicit operator FixedPointNano(UInt128 value)
    {
        return FromInteger(value);
    }

    /// <summary>Explicitly converts a <see cref="BigInteger"/> to <see cref="FixedPointNano"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value exceeds <see cref="FixedPointNano"/> range.</exception>
    public static explicit operator FixedPointNano(BigInteger value)
    {
        return FromInteger(value);
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="byte"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="byte"/> range.</exception>
    public static explicit operator byte(FixedPointNano value)
    {
        return checked((byte)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="sbyte"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="sbyte"/> range.</exception>
    public static explicit operator sbyte(FixedPointNano value)
    {
        return checked((sbyte)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="short"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="short"/> range.</exception>
    public static explicit operator short(FixedPointNano value)
    {
        return checked((short)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="ushort"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="ushort"/> range.</exception>
    public static explicit operator ushort(FixedPointNano value)
    {
        return checked((ushort)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="int"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="int"/> range.</exception>
    public static explicit operator int(FixedPointNano value)
    {
        return checked((int)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="uint"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="uint"/> range.</exception>
    public static explicit operator uint(FixedPointNano value)
    {
        return checked((uint)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="long"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator long(FixedPointNano value)
    {
        return value.RawValue / Scale;
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="ulong"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="ulong"/> range.</exception>
    public static explicit operator ulong(FixedPointNano value)
    {
        return checked((ulong)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="nint"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="nint"/> range.</exception>
    public static explicit operator nint(FixedPointNano value)
    {
        return checked((nint)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="nuint"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is outside <see cref="nuint"/> range.</exception>
    public static explicit operator nuint(FixedPointNano value)
    {
        return checked((nuint)(value.RawValue / Scale));
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="Half"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator Half(FixedPointNano value)
    {
        return value.ToHalf();
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="float"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator float(FixedPointNano value)
    {
        return value.ToSingle();
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="double"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator double(FixedPointNano value)
    {
        return value.ToDouble();
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="decimal"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator decimal(FixedPointNano value)
    {
        return value.ToDecimal();
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="Int128"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator Int128(FixedPointNano value)
    {
        return value.ToInt128();
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="UInt128"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when the value is negative.</exception>
    public static explicit operator UInt128(FixedPointNano value)
    {
        return value.ToUInt128();
    }

    /// <summary>Explicitly converts a <see cref="FixedPointNano"/> to <see cref="BigInteger"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
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

    // Fast long-only overload for Divide(value, int/long) — avoids Int128 overhead on the common path.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long DivideRoundedToNearestEven(long numerator, long denominator)
    {
        // Fall back to Int128 for long.MinValue to avoid negation overflow.
        if (numerator == long.MinValue || denominator == long.MinValue)
        {
            return (long)DivideRoundedToNearestEven((Int128)numerator, (Int128)denominator);
        }

        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        if (numerator < 0)
        {
            return -DivideRoundedToNearestEvenNonNeg(-numerator, denominator);
        }

        return DivideRoundedToNearestEvenNonNeg(numerator, denominator);
    }

    // Both numerator and denominator are non-negative; denominator > 0.
    // Uses (denominator - remainder) instead of 2*remainder to avoid overflow.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long DivideRoundedToNearestEvenNonNeg(long numerator, long denominator)
    {
        var quotient = numerator / denominator;
        var remainder = numerator % denominator;
        var halfFromAbove = denominator - remainder;
        if (remainder < halfFromAbove)
        {
            return quotient;
        }

        if (remainder > halfFromAbove)
        {
            return quotient + 1;
        }

        return (quotient & 1L) == 0 ? quotient : quotient + 1;
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
        if (!T.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }
    }

    private static void ThrowIfInvalidFloatingPoint(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }
    }

    // INumberBase<FixedPointNano>
    static int INumberBase<FixedPointNano>.Radix => 10;

    static FixedPointNano INumberBase<FixedPointNano>.Abs(FixedPointNano value) => Abs(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsCanonical(FixedPointNano value) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsComplexNumber(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsEvenInteger(FixedPointNano value) => IsEvenInteger(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsFinite(FixedPointNano value) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsImaginaryNumber(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsInfinity(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsInteger(FixedPointNano value) => IsInteger(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsNaN(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsNegative(FixedPointNano value) => IsNegative(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsNegativeInfinity(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsNormal(FixedPointNano value) => !IsZero(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsOddInteger(FixedPointNano value) => IsOddInteger(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsPositive(FixedPointNano value) => IsPositive(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsPositiveInfinity(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsRealNumber(FixedPointNano value) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsSubnormal(FixedPointNano value) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<FixedPointNano>.IsZero(FixedPointNano value) => IsZero(value);

    static FixedPointNano INumberBase<FixedPointNano>.MaxMagnitude(FixedPointNano x, FixedPointNano y)
        => MaxMagnitudeCore(x, y);

    static FixedPointNano INumberBase<FixedPointNano>.MaxMagnitudeNumber(FixedPointNano x, FixedPointNano y)
        => MaxMagnitudeCore(x, y);

    static FixedPointNano INumberBase<FixedPointNano>.MinMagnitude(FixedPointNano x, FixedPointNano y)
        => MinMagnitudeCore(x, y);

    static FixedPointNano INumberBase<FixedPointNano>.MinMagnitudeNumber(FixedPointNano x, FixedPointNano y)
        => MinMagnitudeCore(x, y);

    static FixedPointNano INumberBase<FixedPointNano>.CreateChecked<TOther>(TOther value)
    {
        if (typeof(TOther) == typeof(FixedPointNano))
        {
            return (FixedPointNano)(object)value!;
        }

        if (!TryConvertFromCheckedCore(value, out var result))
        {
            throw new NotSupportedException($"Cannot convert from {typeof(TOther).Name} to {nameof(FixedPointNano)}.");
        }

        return result;
    }

    static FixedPointNano INumberBase<FixedPointNano>.CreateSaturating<TOther>(TOther value)
    {
        if (typeof(TOther) == typeof(FixedPointNano))
        {
            return (FixedPointNano)(object)value!;
        }

        if (!TryConvertFromSaturatingCore(value, out var result))
        {
            throw new NotSupportedException($"Cannot convert from {typeof(TOther).Name} to {nameof(FixedPointNano)}.");
        }

        return result;
    }

    static FixedPointNano INumberBase<FixedPointNano>.CreateTruncating<TOther>(TOther value)
    {
        if (typeof(TOther) == typeof(FixedPointNano))
        {
            return (FixedPointNano)(object)value!;
        }

        if (!TryConvertFromTruncatingCore(value, out var result))
        {
            throw new NotSupportedException($"Cannot convert from {typeof(TOther).Name} to {nameof(FixedPointNano)}.");
        }

        return result;
    }

    static bool INumberBase<FixedPointNano>.TryConvertFromChecked<TOther>(TOther value, out FixedPointNano result)
        => TryConvertFromCheckedCore(value, out result);

    static bool INumberBase<FixedPointNano>.TryConvertFromSaturating<TOther>(TOther value, out FixedPointNano result)
        => TryConvertFromSaturatingCore(value, out result);

    static bool INumberBase<FixedPointNano>.TryConvertFromTruncating<TOther>(TOther value, out FixedPointNano result)
        => TryConvertFromTruncatingCore(value, out result);

    static bool INumberBase<FixedPointNano>.TryConvertToChecked<TOther>(FixedPointNano value, out TOther result)
        => TryConvertToCheckedCore(value, out result);

    static bool INumberBase<FixedPointNano>.TryConvertToSaturating<TOther>(FixedPointNano value, out TOther result)
        => TryConvertToSaturatingCore(value, out result);

    static bool INumberBase<FixedPointNano>.TryConvertToTruncating<TOther>(FixedPointNano value, out TOther result)
        => TryConvertToTruncatingCore(value, out result);

    // INumber<FixedPointNano>
    static FixedPointNano INumber<FixedPointNano>.MaxNumber(FixedPointNano x, FixedPointNano y) => Max(x, y);

    static FixedPointNano INumber<FixedPointNano>.MinNumber(FixedPointNano x, FixedPointNano y) => Min(x, y);

    private static FixedPointNano MaxMagnitudeCore(FixedPointNano x, FixedPointNano y)
    {
        var absX = Abs(x);
        var absY = Abs(y);
        if (absX > absY)
        {
            return x;
        }

        if (absX < absY)
        {
            return y;
        }

        return IsNegative(x) ? y : x;
    }

    private static FixedPointNano MinMagnitudeCore(FixedPointNano x, FixedPointNano y)
    {
        var absX = Abs(x);
        var absY = Abs(y);
        if (absX < absY)
        {
            return x;
        }

        if (absX > absY)
        {
            return y;
        }

        return IsNegative(x) ? x : y;
    }

    private static bool TryConvertFromCheckedCore<TOther>(TOther value, out FixedPointNano result) where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(byte)) { result = (FixedPointNano)(byte)(object)value!; return true; }
        if (typeof(TOther) == typeof(sbyte)) { result = (FixedPointNano)(sbyte)(object)value!; return true; }
        if (typeof(TOther) == typeof(short)) { result = (FixedPointNano)(short)(object)value!; return true; }
        if (typeof(TOther) == typeof(ushort)) { result = (FixedPointNano)(ushort)(object)value!; return true; }
        if (typeof(TOther) == typeof(int)) { result = (FixedPointNano)(int)(object)value!; return true; }
        if (typeof(TOther) == typeof(uint)) { result = (FixedPointNano)(uint)(object)value!; return true; }
        if (typeof(TOther) == typeof(long)) { result = (FixedPointNano)(long)(object)value!; return true; }
        if (typeof(TOther) == typeof(ulong)) { result = (FixedPointNano)(ulong)(object)value!; return true; }
        if (typeof(TOther) == typeof(nint)) { result = (FixedPointNano)(nint)(object)value!; return true; }
        if (typeof(TOther) == typeof(nuint)) { result = (FixedPointNano)(nuint)(object)value!; return true; }
        if (typeof(TOther) == typeof(Int128)) { result = (FixedPointNano)(Int128)(object)value!; return true; }
        if (typeof(TOther) == typeof(UInt128)) { result = (FixedPointNano)(UInt128)(object)value!; return true; }
        if (typeof(TOther) == typeof(Half)) { result = (FixedPointNano)(Half)(object)value!; return true; }
        if (typeof(TOther) == typeof(float)) { result = (FixedPointNano)(float)(object)value!; return true; }
        if (typeof(TOther) == typeof(double)) { result = (FixedPointNano)(double)(object)value!; return true; }
        if (typeof(TOther) == typeof(decimal)) { result = (FixedPointNano)(decimal)(object)value!; return true; }
        if (typeof(TOther) == typeof(BigInteger)) { result = (FixedPointNano)(BigInteger)(object)value!; return true; }
        result = default;
        return false;
    }

    private static bool TryConvertFromSaturatingCore<TOther>(TOther value, out FixedPointNano result) where TOther : INumberBase<TOther>
    {
        // Small integer types always fit in FixedPointNano range — no saturation needed.
        if (typeof(TOther) == typeof(byte)) { result = (FixedPointNano)(byte)(object)value!; return true; }
        if (typeof(TOther) == typeof(sbyte)) { result = (FixedPointNano)(sbyte)(object)value!; return true; }
        if (typeof(TOther) == typeof(short)) { result = (FixedPointNano)(short)(object)value!; return true; }
        if (typeof(TOther) == typeof(ushort)) { result = (FixedPointNano)(ushort)(object)value!; return true; }
        if (typeof(TOther) == typeof(int)) { result = (FixedPointNano)(int)(object)value!; return true; }
        if (typeof(TOther) == typeof(uint)) { result = (FixedPointNano)(uint)(object)value!; return true; }

        // Larger integer types can exceed FixedPointNano range — clamp.
        const long maxSafeInteger = long.MaxValue / Scale;
        const long minSafeInteger = long.MinValue / Scale;

        if (typeof(TOther) == typeof(long))
        {
            var v = (long)(object)value!;
            if (v > maxSafeInteger) { result = MaxValue; return true; }
            if (v < minSafeInteger) { result = MinValue; return true; }
            result = new FixedPointNano(v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(ulong))
        {
            var v = (ulong)(object)value!;
            if (v > (ulong)maxSafeInteger) { result = MaxValue; return true; }
            result = new FixedPointNano((long)v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(nint))
        {
            var v = (long)(nint)(object)value!;
            if (v > maxSafeInteger) { result = MaxValue; return true; }
            if (v < minSafeInteger) { result = MinValue; return true; }
            result = new FixedPointNano(v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(nuint))
        {
            var v = (ulong)(nuint)(object)value!;
            if (v > (ulong)maxSafeInteger) { result = MaxValue; return true; }
            result = new FixedPointNano((long)v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(Int128))
        {
            var v = (Int128)(object)value!;
            if (v > maxSafeInteger) { result = MaxValue; return true; }
            if (v < minSafeInteger) { result = MinValue; return true; }
            result = new FixedPointNano((long)v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(UInt128))
        {
            var v = (UInt128)(object)value!;
            if (v > (ulong)maxSafeInteger) { result = MaxValue; return true; }
            result = new FixedPointNano((long)v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(BigInteger))
        {
            var v = (BigInteger)(object)value!;
            if (v > maxSafeInteger) { result = MaxValue; return true; }
            if (v < minSafeInteger) { result = MinValue; return true; }
            result = new FixedPointNano((long)v * Scale);
            return true;
        }

        if (typeof(TOther) == typeof(Half))
        {
            var v = (Half)(object)value!;
            if (Half.IsNaN(v)) { result = Zero; return true; }
            if (Half.IsPositiveInfinity(v) || (double)v * Scale >= MaxRawValueAsDoubleExclusive) { result = MaxValue; return true; }
            if (Half.IsNegativeInfinity(v) || (double)v * Scale < MinRawValueAsDoubleInclusive) { result = MinValue; return true; }
            result = FromDouble((double)v);
            return true;
        }

        if (typeof(TOther) == typeof(float))
        {
            var v = (float)(object)value!;
            if (float.IsNaN(v)) { result = Zero; return true; }
            if (float.IsPositiveInfinity(v) || (double)v * Scale >= MaxRawValueAsDoubleExclusive) { result = MaxValue; return true; }
            if (float.IsNegativeInfinity(v) || (double)v * Scale < MinRawValueAsDoubleInclusive) { result = MinValue; return true; }
            result = FromDouble(v);
            return true;
        }

        if (typeof(TOther) == typeof(double))
        {
            var v = (double)(object)value!;
            if (double.IsNaN(v)) { result = Zero; return true; }
            if (double.IsPositiveInfinity(v) || v * Scale >= MaxRawValueAsDoubleExclusive) { result = MaxValue; return true; }
            if (double.IsNegativeInfinity(v) || v * Scale < MinRawValueAsDoubleInclusive) { result = MinValue; return true; }
            result = FromDouble(v);
            return true;
        }

        if (typeof(TOther) == typeof(decimal))
        {
            var v = (decimal)(object)value!;
            try
            {
                result = FromDecimal(v);
            }
            catch (OverflowException)
            {
                result = v > decimal.Zero ? MaxValue : MinValue;
            }

            return true;
        }

        result = default;
        return false;
    }

    private static bool TryConvertFromTruncatingCore<TOther>(TOther value, out FixedPointNano result) where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(byte)) { result = (FixedPointNano)(byte)(object)value!; return true; }
        if (typeof(TOther) == typeof(sbyte)) { result = (FixedPointNano)(sbyte)(object)value!; return true; }
        if (typeof(TOther) == typeof(short)) { result = (FixedPointNano)(short)(object)value!; return true; }
        if (typeof(TOther) == typeof(ushort)) { result = (FixedPointNano)(ushort)(object)value!; return true; }
        if (typeof(TOther) == typeof(int)) { result = (FixedPointNano)(int)(object)value!; return true; }
        if (typeof(TOther) == typeof(uint)) { result = (FixedPointNano)(uint)(object)value!; return true; }

        // Large integer truncating: use unchecked arithmetic to wrap on overflow.
        if (typeof(TOther) == typeof(long)) { result = new FixedPointNano(unchecked((long)(object)value! * Scale)); return true; }
        if (typeof(TOther) == typeof(ulong)) { result = new FixedPointNano(unchecked((long)(ulong)(object)value! * Scale)); return true; }
        if (typeof(TOther) == typeof(nint)) { result = new FixedPointNano(unchecked((long)(nint)(object)value! * Scale)); return true; }
        if (typeof(TOther) == typeof(nuint)) { result = new FixedPointNano(unchecked((long)(nuint)(object)value! * Scale)); return true; }
        if (typeof(TOther) == typeof(Int128)) { result = new FixedPointNano(unchecked((long)(Int128)(object)value! * Scale)); return true; }
        if (typeof(TOther) == typeof(UInt128)) { result = new FixedPointNano(unchecked((long)(UInt128)(object)value! * Scale)); return true; }
        if (typeof(TOther) == typeof(BigInteger))
        {
            var v = (BigInteger)(object)value!;
            result = new FixedPointNano(unchecked((long)(v * Scale)));
            return true;
        }

        // Floating-point truncating: clamp NaN/infinity, convert otherwise.
        if (typeof(TOther) == typeof(Half))
        {
            var v = (Half)(object)value!;
            if (Half.IsNaN(v)) { result = Zero; return true; }
            if (Half.IsPositiveInfinity(v) || (double)v * Scale >= MaxRawValueAsDoubleExclusive) { result = MaxValue; return true; }
            if (Half.IsNegativeInfinity(v) || (double)v * Scale < MinRawValueAsDoubleInclusive) { result = MinValue; return true; }
            result = FromDouble((double)v);
            return true;
        }

        if (typeof(TOther) == typeof(float))
        {
            var v = (float)(object)value!;
            if (float.IsNaN(v)) { result = Zero; return true; }
            if (float.IsPositiveInfinity(v) || (double)v * Scale >= MaxRawValueAsDoubleExclusive) { result = MaxValue; return true; }
            if (float.IsNegativeInfinity(v) || (double)v * Scale < MinRawValueAsDoubleInclusive) { result = MinValue; return true; }
            result = FromDouble(v);
            return true;
        }

        if (typeof(TOther) == typeof(double))
        {
            var v = (double)(object)value!;
            if (double.IsNaN(v)) { result = Zero; return true; }
            if (double.IsPositiveInfinity(v) || v * Scale >= MaxRawValueAsDoubleExclusive) { result = MaxValue; return true; }
            if (double.IsNegativeInfinity(v) || v * Scale < MinRawValueAsDoubleInclusive) { result = MinValue; return true; }
            result = FromDouble(v);
            return true;
        }

        if (typeof(TOther) == typeof(decimal))
        {
            var v = (decimal)(object)value!;
            try
            {
                result = FromDecimal(v);
            }
            catch (OverflowException)
            {
                result = v > decimal.Zero ? MaxValue : MinValue;
            }

            return true;
        }

        result = default;
        return false;
    }

    private static bool TryConvertToCheckedCore<TOther>(FixedPointNano value, out TOther result) where TOther : INumberBase<TOther>
    {
        var intPart = value.RawValue / Scale;
        if (typeof(TOther) == typeof(byte)) { result = (TOther)(object)checked((byte)intPart); return true; }
        if (typeof(TOther) == typeof(sbyte)) { result = (TOther)(object)checked((sbyte)intPart); return true; }
        if (typeof(TOther) == typeof(short)) { result = (TOther)(object)checked((short)intPart); return true; }
        if (typeof(TOther) == typeof(ushort)) { result = (TOther)(object)checked((ushort)intPart); return true; }
        if (typeof(TOther) == typeof(int)) { result = (TOther)(object)checked((int)intPart); return true; }
        if (typeof(TOther) == typeof(uint)) { result = (TOther)(object)checked((uint)intPart); return true; }
        if (typeof(TOther) == typeof(long)) { result = (TOther)(object)intPart; return true; }
        if (typeof(TOther) == typeof(ulong)) { result = (TOther)(object)checked((ulong)intPart); return true; }
        if (typeof(TOther) == typeof(nint)) { result = (TOther)(object)checked((nint)intPart); return true; }
        if (typeof(TOther) == typeof(nuint)) { result = (TOther)(object)checked((nuint)intPart); return true; }
        if (typeof(TOther) == typeof(Int128)) { result = (TOther)(object)(Int128)intPart; return true; }
        if (typeof(TOther) == typeof(UInt128)) { result = (TOther)(object)checked((UInt128)intPart); return true; }
        if (typeof(TOther) == typeof(BigInteger)) { result = (TOther)(object)(BigInteger)intPart; return true; }
        if (typeof(TOther) == typeof(Half)) { result = (TOther)(object)(Half)value; return true; }
        if (typeof(TOther) == typeof(float)) { result = (TOther)(object)(float)value; return true; }
        if (typeof(TOther) == typeof(double)) { result = (TOther)(object)(double)value; return true; }
        if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
        if (typeof(TOther) == typeof(FixedPointNano)) { result = (TOther)(object)value; return true; }
        result = default!;
        return false;
    }

    private static bool TryConvertToSaturatingCore<TOther>(FixedPointNano value, out TOther result) where TOther : INumberBase<TOther>
    {
        var intPart = value.RawValue / Scale;
        if (typeof(TOther) == typeof(byte))
        {
            result = (TOther)(object)(byte)Math.Clamp(intPart, byte.MinValue, byte.MaxValue);
            return true;
        }

        if (typeof(TOther) == typeof(sbyte))
        {
            result = (TOther)(object)(sbyte)Math.Clamp(intPart, sbyte.MinValue, sbyte.MaxValue);
            return true;
        }

        if (typeof(TOther) == typeof(short))
        {
            result = (TOther)(object)(short)Math.Clamp(intPart, short.MinValue, short.MaxValue);
            return true;
        }

        if (typeof(TOther) == typeof(ushort))
        {
            result = (TOther)(object)(ushort)Math.Clamp(intPart, ushort.MinValue, ushort.MaxValue);
            return true;
        }

        if (typeof(TOther) == typeof(int))
        {
            result = (TOther)(object)(int)Math.Clamp(intPart, int.MinValue, int.MaxValue);
            return true;
        }

        if (typeof(TOther) == typeof(uint))
        {
            result = (TOther)(object)(uint)Math.Clamp(intPart, uint.MinValue, uint.MaxValue);
            return true;
        }

        if (typeof(TOther) == typeof(long)) { result = (TOther)(object)intPart; return true; }
        if (typeof(TOther) == typeof(ulong)) { result = (TOther)(object)(ulong)Math.Max(intPart, 0L); return true; }
        if (typeof(TOther) == typeof(nint)) { result = (TOther)(object)(nint)Math.Clamp(intPart, nint.MinValue, nint.MaxValue); return true; }
        if (typeof(TOther) == typeof(nuint)) { result = (TOther)(object)(nuint)Math.Max(intPart, 0L); return true; }
        if (typeof(TOther) == typeof(Int128)) { result = (TOther)(object)(Int128)intPart; return true; }
        if (typeof(TOther) == typeof(UInt128)) { result = (TOther)(object)(UInt128)Math.Max(intPart, 0L); return true; }
        if (typeof(TOther) == typeof(BigInteger)) { result = (TOther)(object)(BigInteger)intPart; return true; }
        if (typeof(TOther) == typeof(Half)) { result = (TOther)(object)(Half)value; return true; }
        if (typeof(TOther) == typeof(float)) { result = (TOther)(object)(float)value; return true; }
        if (typeof(TOther) == typeof(double)) { result = (TOther)(object)(double)value; return true; }
        if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
        if (typeof(TOther) == typeof(FixedPointNano)) { result = (TOther)(object)value; return true; }
        result = default!;
        return false;
    }

    private static bool TryConvertToTruncatingCore<TOther>(FixedPointNano value, out TOther result) where TOther : INumberBase<TOther>
    {
        var intPart = value.RawValue / Scale;
        if (typeof(TOther) == typeof(byte)) { result = (TOther)(object)unchecked((byte)intPart); return true; }
        if (typeof(TOther) == typeof(sbyte)) { result = (TOther)(object)unchecked((sbyte)intPart); return true; }
        if (typeof(TOther) == typeof(short)) { result = (TOther)(object)unchecked((short)intPart); return true; }
        if (typeof(TOther) == typeof(ushort)) { result = (TOther)(object)unchecked((ushort)intPart); return true; }
        if (typeof(TOther) == typeof(int)) { result = (TOther)(object)unchecked((int)intPart); return true; }
        if (typeof(TOther) == typeof(uint)) { result = (TOther)(object)unchecked((uint)intPart); return true; }
        if (typeof(TOther) == typeof(long)) { result = (TOther)(object)intPart; return true; }
        if (typeof(TOther) == typeof(ulong)) { result = (TOther)(object)unchecked((ulong)intPart); return true; }
        if (typeof(TOther) == typeof(nint)) { result = (TOther)(object)unchecked((nint)intPart); return true; }
        if (typeof(TOther) == typeof(nuint)) { result = (TOther)(object)unchecked((nuint)intPart); return true; }
        if (typeof(TOther) == typeof(Int128)) { result = (TOther)(object)(Int128)intPart; return true; }
        if (typeof(TOther) == typeof(UInt128)) { result = (TOther)(object)unchecked((UInt128)intPart); return true; }
        if (typeof(TOther) == typeof(BigInteger)) { result = (TOther)(object)(BigInteger)intPart; return true; }
        if (typeof(TOther) == typeof(Half)) { result = (TOther)(object)(Half)value; return true; }
        if (typeof(TOther) == typeof(float)) { result = (TOther)(object)(float)value; return true; }
        if (typeof(TOther) == typeof(double)) { result = (TOther)(object)(double)value; return true; }
        if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
        if (typeof(TOther) == typeof(FixedPointNano)) { result = (TOther)(object)value; return true; }
        result = default!;
        return false;
    }
}
