using BenchmarkDotNet.Attributes;
using Fpn = Seerstone.FixedPointNano;

namespace FixedPointNano.Benchmarks;

[MemoryDiagnoser]
public class FixedPointNanoMathBenchmarks
{
    private const int Period = 14;
    private readonly Fpn _left = Fpn.FromDecimal(1234.567890123m);
    private readonly Fpn _right = Fpn.FromDecimal(7.123456789m);
    private readonly Fpn _sqrtInput = Fpn.FromDecimal(1234.567890123m);
    private readonly double _leftDouble = 1234.567890123d;
    private readonly double _rightDouble = 7.123456789d;
    private readonly double _sqrtInputDouble = 1234.567890123d;
    private readonly double _doubleValue = 1234.567890123d;
    private Fpn[] _fixedValues = [];
    private decimal[] _decimalValues = [];
    private double[] _doubleValues = [];

    [GlobalSetup]
    public void Setup()
    {
        _fixedValues = new Fpn[1024];
        _decimalValues = new decimal[_fixedValues.Length];
        _doubleValues = new double[_fixedValues.Length];

        for (var index = 0; index < _fixedValues.Length; index++)
        {
            var value = 1.200000000m + (index % 31 * 0.000100001m);
            _decimalValues[index] = value;
            _doubleValues[index] = (double)value;
            _fixedValues[index] = Fpn.FromDecimal(value);
        }
    }

    [Benchmark]
    public Fpn MultiplyDecimalReference()
    {
        return Fpn.FromDecimal(_left.ToDecimal() * _right.ToDecimal());
    }

    [Benchmark]
    public double MultiplyDoubleReference()
    {
        return _leftDouble * _rightDouble;
    }

    [Benchmark]
    public Fpn MultiplyRaw()
    {
        return _left * _right;
    }

    [Benchmark]
    public Fpn DivideDecimalReference()
    {
        return Fpn.FromDecimal(_left.ToDecimal() / _right.ToDecimal());
    }

    [Benchmark]
    public double DivideDoubleReference()
    {
        return _leftDouble / _rightDouble;
    }

    [Benchmark]
    public Fpn DivideRaw()
    {
        return _left / _right;
    }

    [Benchmark]
    public Fpn DivideByIntegerDecimalReference()
    {
        return Fpn.FromDecimal(_left.ToDecimal() / Period);
    }

    [Benchmark]
    public double DivideByIntegerDoubleReference()
    {
        return _leftDouble / Period;
    }

    [Benchmark]
    public Fpn DivideByIntegerRaw()
    {
        return Fpn.Divide(_left, Period);
    }

    [Benchmark]
    public Fpn SquareDecimalReference()
    {
        return Fpn.FromDecimal(_right.ToDecimal() * _right.ToDecimal());
    }

    [Benchmark]
    public double SquareDoubleReference()
    {
        return _rightDouble * _rightDouble;
    }

    [Benchmark]
    public Fpn SquareRaw()
    {
        return Fpn.Square(_right);
    }

    [Benchmark]
    public Fpn SqrtDecimalReference()
    {
        return Fpn.FromDecimal((decimal)Math.Sqrt((double)_sqrtInput.ToDecimal()));
    }

    [Benchmark]
    public double SqrtDoubleReference()
    {
        return Math.Sqrt(_sqrtInputDouble);
    }

    [Benchmark]
    public Fpn SqrtRaw()
    {
        return Fpn.Sqrt(_sqrtInput);
    }

    [Benchmark]
    public Fpn RoundDecimalReference()
    {
        return Fpn.FromDecimal(decimal.Round(_left.ToDecimal(), 4, MidpointRounding.ToEven));
    }

    [Benchmark]
    public double RoundDoubleReference()
    {
        return Math.Round(_leftDouble, 4, MidpointRounding.ToEven);
    }

    [Benchmark]
    public Fpn RoundRaw()
    {
        return Fpn.Round(_left, 4);
    }

    [Benchmark]
    public Fpn FromDoubleDecimalReference()
    {
        return Fpn.FromDecimal((decimal)_doubleValue);
    }

    [Benchmark]
    public Fpn FromDoubleRaw()
    {
        return Fpn.FromDouble(_doubleValue);
    }

    [Benchmark]
    public double ToDoubleDecimalReference()
    {
        return (double)_left.ToDecimal();
    }

    [Benchmark]
    public double ToDoubleRaw()
    {
        return _left.ToDouble();
    }

    [Benchmark]
    public double SmaLoopDoubleReference()
    {
        var sum = 0d;
        var current = 0d;
        for (var index = 0; index < _doubleValues.Length; index++)
        {
            sum += _doubleValues[index];
            if (index >= Period)
            {
                sum -= _doubleValues[index - Period];
            }

            if (index >= Period - 1)
            {
                current = sum / Period;
            }
        }

        return current;
    }

    [Benchmark]
    public decimal SmaLoopDecimalReference()
    {
        var sum = 0m;
        var current = 0m;
        for (var index = 0; index < _decimalValues.Length; index++)
        {
            sum += _decimalValues[index];
            if (index >= Period)
            {
                sum -= _decimalValues[index - Period];
            }

            if (index >= Period - 1)
            {
                current = sum / Period;
            }
        }

        return current;
    }

    [Benchmark]
    public Fpn SmaLoopRaw()
    {
        var sum = Fpn.Zero;
        var current = Fpn.Zero;
        for (var index = 0; index < _fixedValues.Length; index++)
        {
            sum += _fixedValues[index];
            if (index >= Period)
            {
                sum -= _fixedValues[index - Period];
            }

            if (index >= Period - 1)
            {
                current = Fpn.Divide(sum, Period);
            }
        }

        return current;
    }

    [Benchmark]
    public double BollingerLoopDoubleReference()
    {
        var sum = 0d;
        var sumSquares = 0d;
        var current = 0d;
        for (var index = 0; index < _doubleValues.Length; index++)
        {
            var value = _doubleValues[index];
            sum += value;
            sumSquares += value * value;
            if (index >= Period)
            {
                var outgoing = _doubleValues[index - Period];
                sum -= outgoing;
                sumSquares -= outgoing * outgoing;
            }

            if (index >= Period - 1)
            {
                var mean = sum / Period;
                var variance = (sumSquares / Period) - (mean * mean);
                current = Math.Sqrt(Math.Max(variance, 0d));
            }
        }

        return current;
    }

    [Benchmark]
    public decimal BollingerLoopDecimalReference()
    {
        var sum = 0m;
        var sumSquares = 0m;
        var current = 0m;
        for (var index = 0; index < _decimalValues.Length; index++)
        {
            var value = _decimalValues[index];
            sum += value;
            sumSquares += value * value;
            if (index >= Period)
            {
                var outgoing = _decimalValues[index - Period];
                sum -= outgoing;
                sumSquares -= outgoing * outgoing;
            }

            if (index >= Period - 1)
            {
                var mean = sum / Period;
                var variance = (sumSquares / Period) - (mean * mean);
                current = (decimal)Math.Sqrt((double)Math.Max(variance, 0m));
            }
        }

        return current;
    }

    [Benchmark]
    public Fpn BollingerLoopRaw()
    {
        var sumRaw = 0L;
        var sumOfRawSquares = Int128.Zero;
        var current = Fpn.Zero;
        for (var index = 0; index < _fixedValues.Length; index++)
        {
            var value = _fixedValues[index];
            var valueRaw = value.RawValue;
            sumRaw = checked(sumRaw + valueRaw);
            sumOfRawSquares = checked(sumOfRawSquares + ((Int128)valueRaw * valueRaw));

            if (index >= Period)
            {
                var outgoing = _fixedValues[index - Period];
                var outgoingRaw = outgoing.RawValue;
                sumRaw = checked(sumRaw - outgoingRaw);
                sumOfRawSquares = checked(sumOfRawSquares - ((Int128)outgoingRaw * outgoingRaw));
            }

            if (index >= Period - 1)
            {
                current = Fpn.PopulationStandardDeviation(Fpn.FromRaw(sumRaw), sumOfRawSquares, Period);
            }
        }

        return current;
    }
}
