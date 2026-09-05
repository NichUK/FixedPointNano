using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Fpn = Seerstone.FixedPointNano;

namespace FixedPointNano.Benchmarks;

[MemoryDiagnoser]
public class FixedPointNanoMultiplyInliningBenchmarks
{
    private const int BatchSize = 1024;
    private const int PowExponent = 5;
    private Fpn[] _amounts = [];
    private Fpn[] _dependentFactors = [];
    private Fpn[] _left = [];
    private Fpn[] _notionalPrices = [];
    private Fpn[] _notionalQuantities = [];
    private Fpn[] _powValues = [];
    private Fpn[] _right = [];
    private Fpn[] _squareValues = [];
    private Fpn _singleLeft;
    private Fpn _singleRight;

    [GlobalSetup]
    public void Setup()
    {
        _amounts = new Fpn[BatchSize];
        _dependentFactors = new Fpn[BatchSize];
        _left = new Fpn[BatchSize];
        _notionalPrices = new Fpn[BatchSize];
        _notionalQuantities = new Fpn[BatchSize];
        _powValues = new Fpn[BatchSize];
        _right = new Fpn[BatchSize];
        _squareValues = new Fpn[BatchSize];

        for (var index = 0; index < BatchSize; index++)
        {
            var leftSign = (index & 1) == 0 ? 1L : -1L;
            var rightSign = index % 3 == 0 ? -1L : 1L;
            _left[index] = Fpn.FromRaw(leftSign * (750_000_001L + (index * 1_000_003L)));
            _right[index] = Fpn.FromRaw(rightSign * (250_000_003L + (index * 7_919L)));

            _dependentFactors[index] = index % 2 == 0
                ? Fpn.FromRaw(1_000_100_003L)
                : Fpn.FromRaw(999_899_997L);

            _notionalPrices[index] = Fpn.FromRaw(10_000_000_001L + (index * 10_000_019L));
            var quantitySign = index % 5 == 0 ? -1L : 1L;
            _notionalQuantities[index] = Fpn.FromRaw(quantitySign * (1_000_000_007L + (index * 100_003L)));

            _amounts[index] = Fpn.FromRaw((index % 101) * 10_000_000L);
            _squareValues[index] = Fpn.FromRaw(250_000_001L + (index * 10_000_019L));
            _powValues[index] = Fpn.FromRaw(900_000_001L + ((index % 201) * 1_000_003L));
        }

        _left[0] = Fpn.Zero;
        _right[0] = Fpn.FromRaw(123_456_789L);
        _left[1] = Fpn.FromRaw(1L);
        _right[1] = Fpn.FromRaw(499_999_999L);
        _left[2] = Fpn.FromRaw(1L);
        _right[2] = Fpn.FromRaw(500_000_000L);
        _left[3] = Fpn.FromRaw(1L);
        _right[3] = Fpn.FromRaw(500_000_001L);
        _left[4] = Fpn.FromRaw(3L);
        _right[4] = Fpn.FromRaw(500_000_000L);
        _left[5] = Fpn.FromRaw(-3L);
        _right[5] = Fpn.FromRaw(500_000_000L);
        _left[6] = Fpn.FromRaw(2_000_000_000_000L);
        _right[6] = Fpn.FromRaw(3_000_000_000L);
        _amounts[0] = Fpn.Zero;
        _amounts[1] = Fpn.FromRaw(1L);
        _amounts[2] = Fpn.FromRaw(499_999_999L);
        _amounts[3] = Fpn.FromRaw(500_000_000L);
        _amounts[4] = Fpn.FromRaw(500_000_001L);
        _amounts[5] = Fpn.FromRaw(999_999_999L);
        _amounts[6] = Fpn.One;
        _squareValues[0] = Fpn.FromRaw(2_000_000_000_000L);
        _squareValues[1] = Fpn.FromRaw(-2_000_000_000_000L);

        _singleLeft = _left[17];
        _singleRight = _right[17];
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Fpn SingleMultiply()
    {
        return _singleLeft * _singleRight;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long IndependentMultiplyBatch()
    {
        var checksum = 0L;
        for (var index = 0; index < BatchSize; index++)
        {
            checksum = unchecked(checksum + (_left[index] * _right[index]).RawValue);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long DependentMultiplyChain()
    {
        var current = Fpn.One;
        for (var index = 0; index < BatchSize; index++)
        {
            current *= _dependentFactors[index];
        }

        return current.RawValue;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long NotionalBatch()
    {
        var sum = Fpn.Zero;
        for (var index = 0; index < BatchSize; index++)
        {
            sum += _notionalPrices[index] * _notionalQuantities[index];
        }

        return sum.RawValue;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long LerpBatch()
    {
        var checksum = 0L;
        for (var index = 0; index < BatchSize; index++)
        {
            checksum = unchecked(checksum + Fpn.Lerp(_left[index], _right[index], _amounts[index]).RawValue);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long SquareBatch()
    {
        var checksum = 0L;
        for (var index = 0; index < BatchSize; index++)
        {
            checksum = unchecked(checksum + Fpn.Square(_squareValues[index]).RawValue);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long PowBatch()
    {
        var checksum = 0L;
        for (var index = 0; index < BatchSize; index++)
        {
            checksum = unchecked(checksum + Fpn.Pow(_powValues[index], PowExponent).RawValue);
        }

        return checksum;
    }

    public IReadOnlyList<KeyValuePair<string, long>> CaptureChecksums()
    {
        IReadOnlyList<KeyValuePair<string, long>> checksums =
        [
            new(nameof(IndependentMultiplyBatch), IndependentMultiplyBatch()),
            new(nameof(DependentMultiplyChain), DependentMultiplyChain()),
            new(nameof(NotionalBatch), NotionalBatch()),
            new(nameof(LerpBatch), LerpBatch()),
            new(nameof(SquareBatch), SquareBatch()),
            new(nameof(PowBatch), PowBatch()),
        ];

        foreach (var checksum in checksums)
        {
            VerifyChecksum(checksum.Key, checksum.Value, GetExpectedChecksum(checksum.Key));
        }

        return checksums;
    }

    private static long GetExpectedChecksum(string benchmark)
    {
        return benchmark switch
        {
            nameof(IndependentMultiplyBatch) => 6_000_323_985_234L,
            nameof(DependentMultiplyChain) => 999_994_879L,
            nameof(NotionalBatch) => 9_816_311_662_619L,
            nameof(LerpBatch) => 46_212_822_455L,
            nameof(SquareBatch) => 8_038_421_873_095_119L,
            nameof(PowBatch) => 1_050_735_154_786L,
            _ => throw new ArgumentOutOfRangeException(nameof(benchmark), benchmark, "Unknown benchmark."),
        };
    }

    private static void VerifyChecksum(string benchmark, long actual, long expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"The {benchmark} corpus checksum was {actual}, but {expected} was expected.");
        }
    }
}
