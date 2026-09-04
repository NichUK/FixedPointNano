using BenchmarkDotNet.Attributes;
using Fpn = Seerstone.FixedPointNano;

namespace FixedPointNano.Benchmarks;

[MemoryDiagnoser]
public class FixedPointNanoClampBenchmarks
{
    private const decimal Minimum = 0m;
    private const decimal Maximum = 10000m;
    private readonly Fpn _minimum = Fpn.FromDecimal(Minimum);
    private readonly Fpn _maximum = Fpn.FromDecimal(Maximum);
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
        return Fpn.FromDecimal(Math.Clamp(_decimalValue, Minimum, Maximum));
    }
}
