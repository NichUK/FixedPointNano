using BenchmarkDotNet.Attributes;
using Fpn = Seerstone.FixedPointNano;

namespace FixedPointNano.Benchmarks;

[MemoryDiagnoser]
public class FixedPointNanoClampBenchmarks
{
    private readonly Fpn _minimum = Fpn.Zero;
    private readonly Fpn _maximum = Fpn.FromDecimal(10000m);
    private Fpn _value;
    private decimal _decimalValue;

    [Params(-1, 5000, 10001)]
    public int Input { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _decimalValue = Input;
        _value = Fpn.FromDecimal(_decimalValue);
    }

    [Benchmark]
    public Fpn ClampRaw()
    {
        return Fpn.Clamp(_value, _minimum, _maximum);
    }

    [Benchmark]
    public Fpn ClampDecimalReference()
    {
        return Fpn.FromDecimal(Math.Clamp(_decimalValue, 0m, 10000m));
    }
}
