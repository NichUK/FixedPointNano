using System.Globalization;
using BenchmarkDotNet.Attributes;
using Fpn = Seerstone.FixedPointNano;

namespace FixedPointNano.Benchmarks;

/// <summary>
/// Benchmarks for FixedPointNano string formatting operations.
///
/// These establish a baseline for the decimal-round-trip approach used by
/// ToString and TryFormat, and expose the relative cost versus formatting a
/// plain decimal value directly. Results inform any future native-formatting
/// optimisation.
/// </summary>
[MemoryDiagnoser]
public class FixedPointNanoFormatBenchmarks
{
    private readonly Fpn _value = Fpn.FromDecimal(1234.567890123m);
    private readonly decimal _decimalValue = 1234.567890123m;
    private char[] _buffer = [];

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new char[64];
    }

    [Benchmark(Baseline = true)]
    public string? ToStringDefault()
    {
        return _value.ToString(CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public string? DecimalToStringReference()
    {
        return _decimalValue.ToString(CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public string? ToStringFixed2()
    {
        return _value.ToString("F2", CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public string? DecimalToStringFixed2Reference()
    {
        return _decimalValue.ToString("F2", CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public bool TryFormatDefault()
    {
        return _value.TryFormat(_buffer, out _, default, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public bool DecimalTryFormatReference()
    {
        return _decimalValue.TryFormat(_buffer, out _, default, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public bool TryFormatFixed4()
    {
        return _value.TryFormat(_buffer, out _, "F4", CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public bool DecimalTryFormatFixed4Reference()
    {
        return _decimalValue.TryFormat(_buffer, out _, "F4", CultureInfo.InvariantCulture);
    }
}
