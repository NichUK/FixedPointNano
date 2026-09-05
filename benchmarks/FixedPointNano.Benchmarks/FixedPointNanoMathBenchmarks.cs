using System.Globalization;
using BenchmarkDotNet.Attributes;
using Fpn = Seerstone.FixedPointNano;

namespace FixedPointNano.Benchmarks;

[MemoryDiagnoser]
public class FixedPointNanoMathBenchmarks
{
    private const int Period = 14;
    private const int PowExponent = 5;
    private const string ParseInput = "1234.567890123";
    private readonly decimal _leftDecimal = 1234.567890123m;
    private readonly decimal _rightDecimal = 7.123456789m;
    private readonly Fpn _powBase = Fpn.FromDecimal(1.5m);
    private readonly decimal _powBaseDecimal = 1.5m;
    private readonly double _powBaseDouble = 1.5d;
    private readonly Fpn _lerpAmount = Fpn.FromDecimal(0.5m);
    private readonly decimal _lerpAmountDecimal = 0.5m;
    private readonly double _lerpAmountDouble = 0.5d;
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
    private Fpn _varianceSum;
    private Int128 _varianceSumOfRawSquares;

    [GlobalSetup]
    public void Setup()
    {
        _fixedValues = new Fpn[1024];
        _decimalValues = new decimal[_fixedValues.Length];
        _doubleValues = new double[_fixedValues.Length];
        _varianceSum = Fpn.Zero;
        _varianceSumOfRawSquares = Int128.Zero;

        for (var index = 0; index < _fixedValues.Length; index++)
        {
            var value = 1.200000000m + (index % 31 * 0.000100001m);
            _decimalValues[index] = value;
            _doubleValues[index] = (double)value;
            _fixedValues[index] = Fpn.FromDecimal(value);
            _varianceSum += _fixedValues[index];
            var raw = _fixedValues[index].RawValue;
            _varianceSumOfRawSquares = checked(_varianceSumOfRawSquares + ((Int128)raw * raw));
        }
    }

    [Benchmark]
    public Fpn AddDecimalReference()
    {
        return Fpn.FromDecimal(_leftDecimal + _rightDecimal);
    }

    [Benchmark]
    public double AddDoubleReference()
    {
        return _leftDouble + _rightDouble;
    }

    [Benchmark]
    public Fpn AddRaw()
    {
        return _left + _right;
    }

    [Benchmark]
    public Fpn SubtractDecimalReference()
    {
        return Fpn.FromDecimal(_leftDecimal - _rightDecimal);
    }

    [Benchmark]
    public double SubtractDoubleReference()
    {
        return _leftDouble - _rightDouble;
    }

    [Benchmark]
    public Fpn SubtractRaw()
    {
        return _left - _right;
    }

    [Benchmark]
    public Fpn FromDecimalRaw()
    {
        return Fpn.FromDecimal(_leftDecimal);
    }

    [Benchmark]
    public Fpn PowDecimalReference()
    {
        var result = 1m;
        var current = _powBaseDecimal;
        var remaining = PowExponent;
        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
            {
                result *= current;
            }

            remaining >>= 1;
            if (remaining > 0)
            {
                current *= current;
            }
        }

        return Fpn.FromDecimal(result);
    }

    [Benchmark]
    public double PowDoubleReference()
    {
        return Math.Pow(_powBaseDouble, PowExponent);
    }

    [Benchmark]
    public Fpn PowRaw()
    {
        return Fpn.Pow(_powBase, PowExponent);
    }

    [Benchmark]
    public Fpn LerpDecimalReference()
    {
        return Fpn.FromDecimal(_leftDecimal + ((_rightDecimal - _leftDecimal) * _lerpAmountDecimal));
    }

    [Benchmark]
    public double LerpDoubleReference()
    {
        return _leftDouble + ((_rightDouble - _leftDouble) * _lerpAmountDouble);
    }

    [Benchmark]
    public Fpn LerpRaw()
    {
        return Fpn.Lerp(_left, _right, _lerpAmount);
    }

    [Benchmark]
    public Fpn PopulationVarianceRaw()
    {
        return Fpn.PopulationVariance(_varianceSum, _varianceSumOfRawSquares, _fixedValues.Length);
    }

    [Benchmark]
    public Fpn SampleVarianceRaw()
    {
        return Fpn.SampleVariance(_varianceSum, _varianceSumOfRawSquares, _fixedValues.Length);
    }

    [Benchmark]
    public Fpn ParseRaw()
    {
        return Fpn.Parse(ParseInput, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public Fpn ParseDecimalReference()
    {
        return Fpn.FromDecimal(decimal.Parse(ParseInput, NumberStyles.Number, CultureInfo.InvariantCulture));
    }

    [Benchmark]
    public bool TryParseRaw()
    {
        return Fpn.TryParse(ParseInput, CultureInfo.InvariantCulture, out _);
    }

    [Benchmark]
    public bool TryParseDecimalReference()
    {
        return decimal.TryParse(ParseInput, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
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
    public Fpn FloorDecimalReference()
    {
        return Fpn.FromDecimal(decimal.Floor(_left.ToDecimal()));
    }

    [Benchmark]
    public Fpn FloorRaw()
    {
        return Fpn.Floor(_left);
    }

    [Benchmark]
    public Fpn CeilingDecimalReference()
    {
        return Fpn.FromDecimal(decimal.Ceiling(_left.ToDecimal()));
    }

    [Benchmark]
    public Fpn CeilingRaw()
    {
        return Fpn.Ceiling(_left);
    }

    [Benchmark]
    public Fpn TruncateDecimalReference()
    {
        return Fpn.FromDecimal(decimal.Truncate(_left.ToDecimal()));
    }

    [Benchmark]
    public Fpn TruncateRaw()
    {
        return Fpn.Truncate(_left);
    }

    [Benchmark]
    public Fpn AbsDecimalReference()
    {
        return Fpn.FromDecimal(Math.Abs(_left.ToDecimal()));
    }

    [Benchmark]
    public Fpn AbsRaw()
    {
        return Fpn.Abs(_left);
    }

    [Benchmark]
    public Fpn MultiplyRatioDecimalReference()
    {
        return Fpn.FromDecimal(_left.ToDecimal() * 3m / 7m);
    }

    [Benchmark]
    public Fpn MultiplyRatioRaw()
    {
        return Fpn.MultiplyRatio(_left, 3L, 7L);
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
