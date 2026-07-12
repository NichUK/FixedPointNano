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
    IComparisonOperators<FixedPointNano, FixedPointNano, bool>
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

    /// <summary>The largest representable <see cref="FixedPointNano"/> value.</summary>
    public static FixedPointNano MaxValue { get; } = new(long.MaxValue);

    /// <summary>The smallest (most negative) representable <see cref="FixedPointNano"/> value.</summary>
    public static FixedPointNano MinValue { get; } = new(long.MinValue);

    /// <summary>The value that represents zero.</summary>
    public static FixedPointNano Zero { get; } = new(0L);

    /// <summary>The value that represents one.</summary>
    public static FixedPointNano One { get; } = new(Scale);

    /// <summary>The smallest positive representable value (raw value 1, i.e. 10⁻⁹).</summary>
    public static FixedPointNano Epsilon { get; } = new(1L);

    /// <summary>The value that represents negative one.</summary>
    public static FixedPointNano NegativeOne { get; } = new(-Scale);

    static FixedPointNano IAdditiveIdentity<FixedPointNano, FixedPointNano>.AdditiveIdentity => Zero;
    static FixedPointNano IMultiplicativeIdentity<FixedPointNano, FixedPointNano>.MultiplicativeIdentity => One;

    /// <summary>Initialises a new <see cref="FixedPointNano"/> directly from a raw scaled value.</summary>
    /// <param name="rawValue">The raw integer value. Divide by <see cref="Scale"/> to obtain the represented number.</param>
    public FixedPointNano(long rawValue)
    {
        RawValue = rawValue;
    }

    /// <summary>
    /// Gets the raw scaled integer value.
    /// Divide by <see cref="Scale"/> to obtain the represented real number.
    /// </summary>
    public long RawValue { get; }

    /// <summary>Returns the absolute value of <paramref name="value"/>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The absolute value of <paramref name="value"/>.</returns>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> is <see cref="MinValue"/>.</exception>
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>Returns the fractional part of <paramref name="value"/> (i.e. <c>value - Truncate(value)</c>).</summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="FixedPointNano"/> whose raw value equals <c>value.RawValue % Scale</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano FractionalPart(FixedPointNano value)
    {
        return new FixedPointNano(value.RawValue % Scale);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="value"/> has no fractional component.</summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInteger(FixedPointNano value)
    {
        return value.RawValue % Scale == 0;
    }

    /// <summary>
    /// Creates a <see cref="FixedPointNano"/> from a <see cref="decimal"/> value
    /// using banker's rounding (<see cref="MidpointRounding.ToEven"/>).
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <returns>A <see cref="FixedPointNano"/> representing <paramref name="value"/>.</returns>
    /// <exception cref="OverflowException">Thrown when the scaled value overflows <see cref="long"/> range.</exception>
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

    /// <summary>Creates a <see cref="FixedPointNano"/> directly from a raw scaled <see cref="long"/> value.</summary>
    /// <param name="rawValue">The raw integer. Divide by <see cref="Scale"/> to obtain the represented number.</param>
    /// <returns>A <see cref="FixedPointNano"/> wrapping <paramref name="rawValue"/>.</returns>
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

    /// <summary>
    /// Returns a value with the magnitude of <paramref name="value"/> and the sign of <paramref name="sign"/>.
    /// </summary>
    /// <param name="value">The value supplying the magnitude.</param>
    /// <param name="sign">The value supplying the sign.</param>
    /// <returns>A <see cref="FixedPointNano"/> with the same absolute value as <paramref name="value"/> and the same sign as <paramref name="sign"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano CopySign(FixedPointNano value, FixedPointNano sign)
    {
        if (sign.RawValue >= 0)
        {
            return value.RawValue < 0 ? new FixedPointNano(checked(-value.RawValue)) : value;
        }

        return value.RawValue > 0 ? new FixedPointNano(-value.RawValue) : value;
    }

    /// <summary>
    /// Linearly interpolates between <paramref name="start"/> and <paramref name="end"/>
    /// by the factor <paramref name="amount"/>.
    /// </summary>
    /// <param name="start">The start value (returned when <paramref name="amount"/> is zero).</param>
    /// <param name="end">The end value (returned when <paramref name="amount"/> is one).</param>
    /// <param name="amount">The interpolation factor. Values outside [0, 1] extrapolate.</param>
    /// <returns><c>start + (end - start) * amount</c>.</returns>
    public static FixedPointNano Lerp(FixedPointNano start, FixedPointNano end, FixedPointNano amount)
    {
        return start + (end - start) * amount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Max(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue >= right.RawValue ? left : right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Min(FixedPointNano left, FixedPointNano right)
    {
        return left.RawValue <= right.RawValue ? left : right;
    }

    /// <summary>
    /// Clamps <paramref name="value"/> to the inclusive range [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound. Must be greater than or equal to <paramref name="min"/>.</param>
    /// <returns>
    /// <paramref name="min"/> when <paramref name="value"/> &lt; <paramref name="min"/>;
    /// <paramref name="max"/> when <paramref name="value"/> &gt; <paramref name="max"/>;
    /// otherwise <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
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

    /// <summary>Returns the sign of <paramref name="value"/>: +1, -1, or 0.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>1 if positive, -1 if negative, 0 if zero.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sign(FixedPointNano value)
    {
        return value.RawValue switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0,
        };
    }

    /// <summary>
    /// Parses a string representation of a fixed-point number using the invariant culture.
    /// </summary>
    /// <param name="s">The string to parse. Must not be <see langword="null"/>.</param>
    /// <param name="provider">An optional format provider; defaults to <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <returns>The parsed <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid number.</exception>
    public static FixedPointNano Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a span representation of a fixed-point number.
    /// </summary>
    /// <param name="s">The character span to parse.</param>
    /// <param name="provider">An optional format provider; defaults to <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <returns>The parsed <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid number.</exception>
    public static FixedPointNano Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException($"The input string '{s.ToString()}' was not in a correct format for FixedPointNano.");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a string representation of a fixed-point number.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider; defaults to <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <param name="result">
    /// When this method returns <see langword="true"/>, the parsed value; otherwise <c>default</c>.
    /// </param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Tries to parse a span representation of a fixed-point number.
    /// </summary>
    /// <param name="s">The character span to parse.</param>
    /// <param name="provider">An optional format provider; defaults to <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <param name="result">
    /// When this method returns <see langword="true"/>, the parsed value; otherwise <c>default</c>.
    /// </param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out FixedPointNano result)
    {
        if (!decimal.TryParse(s, NumberStyles.Number, provider ?? CultureInfo.InvariantCulture, out var d))
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

    /// <summary>Parses a string using the invariant culture. Equivalent to <c>TryParse(s, CultureInfo.InvariantCulture, out result)</c>.</summary>
    public static bool TryParse(string? s, out FixedPointNano result)
        => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <summary>Parses a span using the invariant culture. Equivalent to <c>TryParse(s, CultureInfo.InvariantCulture, out result)</c>.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, out FixedPointNano result)
        => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Rounds <paramref name="value"/> to <paramref name="decimals"/> decimal places using the specified <paramref name="rounding"/> mode.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="decimals">The number of decimal places (0–<see cref="DecimalPlaces"/>).</param>
    /// <param name="rounding">The midpoint rounding convention. Defaults to <see cref="MidpointRounding.ToEven"/>.</param>
    /// <returns>The rounded <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="decimals"/> is outside [0, <see cref="DecimalPlaces"/>].</exception>
    public static FixedPointNano Round(FixedPointNano value, int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        if (decimals is < 0 or > DecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimals must be between 0 and {DecimalPlaces}.");
        }

        ValidateRounding(rounding);
        return new FixedPointNano(RoundRaw(value.RawValue, s_roundingScales[decimals], rounding));
    }

    /// <summary>
    /// Divides <paramref name="value"/> by an <see cref="int"/> <paramref name="divisor"/>
    /// using banker's rounding (<see cref="MidpointRounding.ToEven"/>).
    /// </summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The integer divisor. Must not be zero.</param>
    /// <returns>The quotient rounded to nearest even.</returns>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="divisor"/> is zero.</exception>
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

    /// <summary>
    /// Raises <paramref name="value"/> to the power of a non-negative integer <paramref name="exponent"/>
    /// using binary exponentiation.
    /// </summary>
    /// <param name="value">The base.</param>
    /// <param name="exponent">The non-negative integer exponent.</param>
    /// <returns><paramref name="value"/> raised to <paramref name="exponent"/>; returns <see cref="One"/> when <paramref name="exponent"/> is zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="exponent"/> is negative.</exception>
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

    /// <summary>
    /// Computes the population variance from pre-aggregated raw statistics.
    /// </summary>
    /// <param name="sum">The sum of all observations.</param>
    /// <param name="sumOfRawSquares">
    /// The sum of <c>RawValue * RawValue</c> for all observations.
    /// </param>
    /// <param name="count">The number of observations. Must be greater than zero.</param>
    /// <returns>The population variance as a <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is zero or negative, <paramref name="sumOfRawSquares"/> is negative,
    /// or the statistics are mutually inconsistent.
    /// </exception>
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

    /// <summary>
    /// Computes the sample variance from pre-aggregated raw statistics.
    /// </summary>
    /// <param name="sum">The sum of all observations.</param>
    /// <param name="sumOfRawSquares">
    /// The sum of <c>RawValue * RawValue</c> for all observations.
    /// </param>
    /// <param name="count">The number of observations. Must be at least 2.</param>
    /// <returns>The sample variance as a <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is less than 2, <paramref name="sumOfRawSquares"/> is negative,
    /// or the statistics are mutually inconsistent.
    /// </exception>
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

    /// <summary>
    /// Computes the sample standard deviation from pre-aggregated raw statistics.
    /// </summary>
    /// <param name="sum">The sum of all observations.</param>
    /// <param name="sumOfRawSquares">
    /// The sum of <c>RawValue * RawValue</c> for all observations.
    /// </param>
    /// <param name="count">The number of observations. Must be at least 2.</param>
    /// <returns>The sample standard deviation as a <see cref="FixedPointNano"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the statistics arguments are invalid (see <see cref="SampleVariance"/>).
    /// </exception>
    public static FixedPointNano SampleStandardDeviation(FixedPointNano sum, Int128 sumOfRawSquares, int count)
    {
        return Sqrt(SampleVariance(sum, sumOfRawSquares, count));
    }

    /// <summary>
    /// Computes the square root of <paramref name="value"/> using integer Newton–Raphson,
    /// rounded to nearest even.
    /// </summary>
    /// <param name="value">The value. Must not be negative.</param>
    /// <returns>The square root as a <see cref="FixedPointNano"/>.</returns>
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

    /// <summary>Truncates <paramref name="value"/> toward zero, discarding the fractional part.</summary>
    /// <param name="value">The value to truncate.</param>
    /// <returns>The nearest integral <see cref="FixedPointNano"/> whose magnitude is less than or equal to <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano Truncate(FixedPointNano value)
    {
        return new FixedPointNano((value.RawValue / Scale) * Scale);
    }

    /// <summary>
    /// Returns the fractional part of <paramref name="value"/> with the same sign as <paramref name="value"/>
    /// (i.e. <c>value - Truncate(value)</c>).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="FixedPointNano"/> whose raw value equals <c>value.RawValue % Scale</c>.</returns>
    public static FixedPointNano Frac(FixedPointNano value)
    {
        return new FixedPointNano(value.RawValue % Scale);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(FixedPointNano other)
    {
        return RawValue.CompareTo(other.RawValue);
    }

    /// <inheritdoc/>
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

    /// <summary>Deconstructs this value into its integer and fractional parts.</summary>
    /// <param name="integerPart">The integer part (truncated toward zero).</param>
    /// <param name="fractionalPart">The fractional part with the same sign as this value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out long integerPart, out FixedPointNano fractionalPart)
    {
        integerPart = RawValue / Scale;
        fractionalPart = new FixedPointNano(RawValue % Scale);
    }

    /// <summary>Converts this value to <see cref="decimal"/>.</summary>
    /// <returns>The value as a <see cref="decimal"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal ToDecimal()
    {
        return RawValue / (decimal)Scale;
    }

    /// <summary>Converts this value to <see cref="double"/>.</summary>
    /// <returns>The value as a <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToDouble()
    {
        return RawValue / (double)Scale;
    }

    /// <summary>Converts this value to <see cref="float"/>.</summary>
    /// <returns>The value as a <see cref="float"/>.</returns>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator +(FixedPointNano value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator ++(FixedPointNano value)
    {
        return new FixedPointNano(checked(value.RawValue + Scale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedPointNano operator --(FixedPointNano value)
    {
        return new FixedPointNano(checked(value.RawValue - Scale));
    }

    /// <summary>Multiplies two <see cref="FixedPointNano"/> values using banker's rounding on the product.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The product, rounded to <see cref="DecimalPlaces"/> decimal places.</returns>
    /// <exception cref="OverflowException">Thrown when the result overflows <see cref="long"/> range.</exception>
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
}
