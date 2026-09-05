using System.Globalization;
using BenchmarkDotNet.Running;

namespace FixedPointNano.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 1 && string.Equals(args[0], "--verify-inlining-corpus", StringComparison.Ordinal))
        {
            var benchmarks = new FixedPointNanoMultiplyInliningBenchmarks();
            benchmarks.Setup();
            foreach (var checksum in benchmarks.CaptureChecksums())
            {
                Console.WriteLine($"{checksum.Key}={checksum.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
