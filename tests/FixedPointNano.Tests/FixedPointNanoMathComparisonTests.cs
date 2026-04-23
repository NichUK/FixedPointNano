using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoMathComparisonTests
{
    private static readonly decimal[] DecimalValues =
    [
        0m,
        0.000000001m,
        -0.000000001m,
        0.5m,
        -0.5m,
        1m,
        -1m,
        1.000000001m,
        -1.000000001m,
        2.25m,
        -2.25m,
        12.5m,
        -12.5m,
        1234.567890123m,
        -1234.567890123m,
        1_000_000.000000001m,
        -1_000_000.000000001m,
    ];

    private static readonly decimal[] SqrtValues =
    [
        0m,
        0.000000001m,
        0.000001m,
        0.25m,
        0.5m,
        1m,
        2m,
        10m,
        1234.567890123m,
        1_000_000.000000001m,
    ];

    private static readonly double[] DoubleValues =
    [
        0d,
        0.000000001d,
        -0.000000001d,
        0.5d,
        -0.5d,
        1.0000000005d,
        -1.0000000005d,
        1.0000000015d,
        -1.0000000015d,
        12.5d,
        -12.5d,
        1234.567890123d,
        -1234.567890123d,
        1_000_000.000000001d,
        -1_000_000.000000001d,
    ];

    public static IEnumerable<TestCaseData> MultiplicationCases()
    {
        foreach (var left in DecimalValues)
        {
            foreach (var right in DecimalValues)
            {
                decimal expectedDecimal = left * right;
                if (CanRepresent(expectedDecimal))
                {
                    yield return new TestCaseData(left, right);
                }
            }
        }
    }

    public static IEnumerable<TestCaseData> DivisionCases()
    {
        foreach (var left in DecimalValues)
        {
            foreach (var right in DecimalValues.Where(static value => value != 0m))
            {
                decimal expectedDecimal = left / right;
                if (CanRepresent(expectedDecimal))
                {
                    yield return new TestCaseData(left, right);
                }
            }
        }
    }

    public static IEnumerable<TestCaseData> ModuloCases()
    {
        foreach (var left in DecimalValues)
        {
            foreach (var right in DecimalValues.Where(static value => value != 0m))
            {
                yield return new TestCaseData(left, right);
            }
        }
    }

    public static IEnumerable<TestCaseData> SquareCases()
    {
        foreach (var value in DecimalValues)
        {
            if (CanRepresent(value * value))
            {
                yield return new TestCaseData(value);
            }
        }
    }

    public static IEnumerable<TestCaseData> RoundCases()
    {
        var roundValues = new[]
        {
            0m,
            1.234567891m,
            -1.234567891m,
            1.234500000m,
            -1.234500000m,
            1.235000000m,
            -1.235000000m,
            999.999999999m,
            -999.999999999m,
        };

        var modes = new[]
        {
            MidpointRounding.ToEven,
            MidpointRounding.AwayFromZero,
            MidpointRounding.ToZero,
            MidpointRounding.ToNegativeInfinity,
            MidpointRounding.ToPositiveInfinity,
        };

        foreach (var value in roundValues)
        {
            foreach (var decimals in Enumerable.Range(0, FixedPointNano.DecimalPlaces + 1))
            {
                foreach (var mode in modes)
                {
                    yield return new TestCaseData(value, decimals, mode);
                }
            }
        }
    }

    public static IEnumerable<TestCaseData> SqrtCases()
    {
        foreach (var value in SqrtValues)
        {
            yield return new TestCaseData(value);
        }
    }

    public static IEnumerable<TestCaseData> DoubleCases()
    {
        foreach (var value in DoubleValues)
        {
            yield return new TestCaseData(value);
        }
    }

    [TestCaseSource(nameof(MultiplicationCases))]
    public void MultiplicationMatchesDecimalReference(decimal left, decimal right)
    {
        AssertMatchesDecimalReference(left * right, FixedPointNano.FromDecimal(left) * FixedPointNano.FromDecimal(right));
    }

    [TestCaseSource(nameof(DivisionCases))]
    public void DivisionMatchesDecimalReference(decimal left, decimal right)
    {
        AssertMatchesDecimalReference(left / right, FixedPointNano.FromDecimal(left) / FixedPointNano.FromDecimal(right));
    }

    [TestCaseSource(nameof(ModuloCases))]
    public void ModuloMatchesDecimalReference(decimal left, decimal right)
    {
        AssertMatchesDecimalReference(left % right, FixedPointNano.FromDecimal(left) % FixedPointNano.FromDecimal(right));
    }

    [TestCaseSource(nameof(SquareCases))]
    public void SquareMatchesDecimalReference(decimal valueDecimal)
    {
        var value = FixedPointNano.FromDecimal(valueDecimal);
        AssertMatchesDecimalReference(valueDecimal * valueDecimal, FixedPointNano.Square(value));
    }

    [Test]
    public void PopulationVarianceAndStandardDeviationMatchDecimalReference()
    {
        var values = new[]
        {
            1.200000001m,
            1.200100002m,
            1.199900003m,
            1.200200004m,
            1.199800005m,
        };
        var sum = 0m;
        var sumRaw = 0L;
        var sumSquares = 0m;
        var sumOfRawSquares = Int128.Zero;

        foreach (var value in values)
        {
            var fixedValue = FixedPointNano.FromDecimal(value);
            var decimalValue = fixedValue.ToDecimal();

            sum += decimalValue;
            sumRaw = checked(sumRaw + fixedValue.RawValue);
            sumSquares += decimalValue * decimalValue;
            sumOfRawSquares = checked(sumOfRawSquares + ((Int128)fixedValue.RawValue * fixedValue.RawValue));
        }

        var mean = sum / values.Length;
        var expectedVariance = (sumSquares / values.Length) - (mean * mean);

        Assert.Multiple(() =>
        {
            AssertMatchesDecimalReference(
                expectedVariance,
                FixedPointNano.PopulationVariance(FixedPointNano.FromRaw(sumRaw), sumOfRawSquares, values.Length));
            AssertMatchesDecimalReference(
                DecimalSqrt(expectedVariance),
                FixedPointNano.PopulationStandardDeviation(FixedPointNano.FromRaw(sumRaw), sumOfRawSquares, values.Length));
        });
    }

    [TestCase(-12.5, -7)]
    [TestCase(-12.5, -3)]
    [TestCase(-12.5, 2)]
    [TestCase(-12.5, 14)]
    [TestCase(-0.000000001, 2)]
    [TestCase(0.000000001, 2)]
    [TestCase(12.5, -7)]
    [TestCase(12.5, -3)]
    [TestCase(12.5, 2)]
    [TestCase(12.5, 14)]
    public void DivideByIntegerMatchesDecimalReference(decimal value, long divisor)
    {
        AssertMatchesDecimalReference(value / divisor, FixedPointNano.Divide(FixedPointNano.FromDecimal(value), divisor));
    }

    [TestCase(-12.5, -7, 3)]
    [TestCase(-12.5, 7, 3)]
    [TestCase(-12.5, 7, -3)]
    [TestCase(0.000000001, 1, 2)]
    [TestCase(12.5, -7, 3)]
    [TestCase(12.5, 7, 3)]
    [TestCase(12.5, 7, -3)]
    [TestCase(1234.567890123, 987654321, 123456789)]
    public void MultiplyRatioMatchesDecimalReference(decimal value, long numerator, long denominator)
    {
        AssertMatchesDecimalReference(
            value * numerator / denominator,
            FixedPointNano.MultiplyRatio(FixedPointNano.FromDecimal(value), numerator, denominator));
    }

    [TestCase(-12.000000001)]
    [TestCase(-12)]
    [TestCase(-11.999999999)]
    [TestCase(-0.000000001)]
    [TestCase(0)]
    [TestCase(0.000000001)]
    [TestCase(11.999999999)]
    [TestCase(12)]
    [TestCase(12.000000001)]
    public void IntegerRoundingHelpersMatchDecimalReference(decimal value)
    {
        var fixedPointValue = FixedPointNano.FromDecimal(value);

        Assert.Multiple(() =>
        {
            AssertMatchesDecimalReference(decimal.Ceiling(value), FixedPointNano.Ceiling(fixedPointValue));
            AssertMatchesDecimalReference(decimal.Floor(value), FixedPointNano.Floor(fixedPointValue));
            AssertMatchesDecimalReference(decimal.Truncate(value), FixedPointNano.Truncate(fixedPointValue));
        });
    }

    [TestCaseSource(nameof(RoundCases))]
    public void RoundMatchesDecimalReference(decimal value, int decimals, MidpointRounding rounding)
    {
        AssertMatchesDecimalReference(
            decimal.Round(value, decimals, rounding),
            FixedPointNano.Round(FixedPointNano.FromDecimal(value), decimals, rounding));
    }

    [TestCaseSource(nameof(SqrtCases))]
    public void SqrtMatchesDecimalReference(decimal value)
    {
        AssertMatchesDecimalReference(
            DecimalSqrt(value),
            FixedPointNano.Sqrt(FixedPointNano.FromDecimal(value)));
    }

    [TestCaseSource(nameof(DoubleCases))]
    public void FromDoubleMatchesExplicitDoubleReference(double value)
    {
        var expectedRawValue = RoundDoubleToRaw(value);

        Assert.That(FixedPointNano.FromDouble(value).RawValue, Is.EqualTo(expectedRawValue));
    }

    [Test]
    public void ToDoubleUsesRawScaledDivision()
    {
        var rawValues = new[]
        {
            0L,
            1L,
            -1L,
            FixedPointNano.Scale,
            -FixedPointNano.Scale,
            1_234_567_890_123L,
            -1_234_567_890_123L,
            9_000_000_000_000_000_001L,
        };

        Assert.Multiple(() =>
        {
            foreach (var rawValue in rawValues)
            {
                Assert.That(FixedPointNano.FromRaw(rawValue).ToDouble(), Is.EqualTo(rawValue / (double)FixedPointNano.Scale));
            }
        });
    }

    [Test]
    public void DoubleRoundTripDocumentsExpectedPrecisionLoss()
    {
        var value = FixedPointNano.FromRaw(9_000_000_000_000_000_001L);
        var roundTrippedValue = FixedPointNano.FromDouble(value.ToDouble());

        Assert.Multiple(() =>
        {
            Assert.That(roundTrippedValue.RawValue, Is.Not.EqualTo(value.RawValue));
            Assert.That(Math.Abs(roundTrippedValue.RawValue - value.RawValue), Is.LessThanOrEqualTo(1024L));
        });
    }

    [Test]
    public void NewMathHelpersRejectInvalidInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => FixedPointNano.Divide(FixedPointNano.One, 0), Throws.TypeOf<DivideByZeroException>());
            Assert.That(() => FixedPointNano.MultiplyRatio(FixedPointNano.One, 1, 0), Throws.TypeOf<DivideByZeroException>());
            Assert.That(() => FixedPointNano.Sqrt(FixedPointNano.FromRaw(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FixedPointNano.PopulationVariance(FixedPointNano.Zero, Int128.Zero, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FixedPointNano.PopulationVariance(FixedPointNano.Zero, -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FixedPointNano.PopulationVariance(FixedPointNano.One, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => FixedPointNano.Round(FixedPointNano.One, 2, (MidpointRounding)999),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void FromDoubleRejectsInvalidAndOutOfRangeValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => FixedPointNano.FromDouble(double.NaN), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FixedPointNano.FromDouble(double.PositiveInfinity), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FixedPointNano.FromDouble(double.NegativeInfinity), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FixedPointNano.FromDouble(double.MaxValue), Throws.TypeOf<OverflowException>());
            Assert.That(() => FixedPointNano.FromDouble(10_000_000_000d), Throws.TypeOf<OverflowException>());
            Assert.That(() => FixedPointNano.FromDouble(-10_000_000_000d), Throws.TypeOf<OverflowException>());
        });
    }

    [Test]
    public void CheckedArithmeticThrowsOnOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => _ = FixedPointNano.FromRaw(long.MaxValue) * FixedPointNano.FromRaw(2 * FixedPointNano.Scale),
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = FixedPointNano.FromRaw(long.MaxValue) / FixedPointNano.FromRaw(1),
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = (FixedPointNano)10_000_000_000L,
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = (FixedPointNano)10_000_000_000UL,
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = (FixedPointNano)(UInt128)10_000_000_000UL,
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = (FixedPointNano)new System.Numerics.BigInteger(10_000_000_000L),
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = (FixedPointNano)new System.Numerics.BigInteger(-10_000_000_000L),
                Throws.TypeOf<OverflowException>());
        });
    }

    [Test]
    public void SqrtCorrectsHighDoubleEstimates()
    {
        var method = typeof(FixedPointNano).GetMethod(
            "SquareRootRoundedToNearestEven",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(UInt128)],
            modifiers: null);
        var target = UInt128.Parse("99999999999999999999999999999999");

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.Invoke(null, [target]), Is.EqualTo(UInt128.Parse("10000000000000000")));
    }

    private static bool CanRepresent(decimal value)
    {
        try
        {
            _ = FixedPointNano.FromDecimal(value);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static void AssertMatchesDecimalReference(decimal expectedDecimal, FixedPointNano actual)
    {
        Assert.That(actual.RawValue, Is.EqualTo(FixedPointNano.FromDecimal(expectedDecimal).RawValue));
    }

    private static decimal DecimalSqrt(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Square root input must not be negative.");
        }

        if (value == 0)
        {
            return 0;
        }

        var current = (decimal)Math.Sqrt((double)value);
        if (current == 0)
        {
            current = 1;
        }

        for (var index = 0; index < 40; index++)
        {
            current = (current + (value / current)) / 2;
        }

        return current;
    }

    private static long RoundDoubleToRaw(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Floating-point values must be finite.");
        }

        var roundedValue = Math.Round(value * FixedPointNano.Scale, MidpointRounding.ToEven);
        return checked((long)roundedValue);
    }
}
