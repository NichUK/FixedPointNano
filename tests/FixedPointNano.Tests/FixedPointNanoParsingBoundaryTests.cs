using System.Globalization;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoParsingBoundaryTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("invalid")]
    [TestCase("9223372036.854775808")]
    [TestCase("-9223372036.854775809")]
    [TestCase("9223372036.8547758075")]
    [TestCase("-9223372036.8547758086")]
    [TestCase("79228162514264337593543950335")]
    [TestCase("79228162514264337593543950336")]
    public void TryParseShouldReturnFalseAndClearResultForEveryOverload(string? input)
    {
        var result = FixedPointNano.One;
        Assert.That(FixedPointNano.TryParse(input, out result), Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
        result = FixedPointNano.One;
        Assert.That(FixedPointNano.TryParse(input.AsSpan(), out result), Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
        result = FixedPointNano.One;
        Assert.That(FixedPointNano.TryParse(input, null, out result), Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
        result = FixedPointNano.One;
        Assert.That(FixedPointNano.TryParse(input.AsSpan(), null, out result), Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
        result = FixedPointNano.One;
        Assert.That(FixedPointNano.TryParse(input, NumberStyles.Number, null, out result), Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
        result = FixedPointNano.One;
        Assert.That(FixedPointNano.TryParse(input.AsSpan(), NumberStyles.Number, null, out result), Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
    }

    [TestCase("9223372036.854775807", long.MaxValue)]
    [TestCase("-9223372036.854775808", long.MinValue)]
    [TestCase("9223372036.8547758074", long.MaxValue)]
    [TestCase("-9223372036.8547758085", long.MinValue)]
    [TestCase("0.0000000005", 0L)]
    [TestCase("0.0000000015", 2L)]
    [TestCase("-0.0000000015", -2L)]
    public void ParsingShouldPreserveBoundariesAndRoundToEven(string input, long expectedRaw)
    {
        Assert.That(FixedPointNano.Parse(input).RawValue, Is.EqualTo(expectedRaw));
        Assert.That(FixedPointNano.Parse(input.AsSpan()).RawValue, Is.EqualTo(expectedRaw));
        Assert.That(FixedPointNano.Parse(input, NumberStyles.Number).RawValue, Is.EqualTo(expectedRaw));
        Assert.That(FixedPointNano.Parse(input.AsSpan(), NumberStyles.Number).RawValue, Is.EqualTo(expectedRaw));
    }

    [TestCase("9223372036.854775808")]
    [TestCase("-9223372036.854775809")]
    [TestCase("79228162514264337593543950335")]
    [TestCase("79228162514264337593543950336")]
    public void ParseShouldReportOutOfRangeInputAsFormatExceptionConsistently(string input)
    {
        Assert.Throws<FormatException>(() => FixedPointNano.Parse(input));
        Assert.Throws<FormatException>(() => FixedPointNano.Parse(input.AsSpan()));
        Assert.Throws<FormatException>(() => FixedPointNano.Parse(input, NumberStyles.Number));
        Assert.Throws<FormatException>(() => FixedPointNano.Parse(input.AsSpan(), NumberStyles.Number));
    }

    [Test]
    public void ParseNullStringShouldThrowArgumentNullExceptionWithAndWithoutStyles()
    {
        Assert.Throws<ArgumentNullException>(() => FixedPointNano.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => FixedPointNano.Parse(null!, NumberStyles.Number));
    }

    [TestCase(NumberStyles.HexNumber)]
    [TestCase((NumberStyles)(-1))]
    public void InvalidStylesShouldRemainArgumentErrors(NumberStyles style)
    {
        Assert.Throws<ArgumentException>(() => FixedPointNano.Parse("1", style));
        Assert.Throws<ArgumentException>(() => FixedPointNano.Parse("1".AsSpan(), style));
        Assert.Throws<ArgumentException>(() => FixedPointNano.TryParse("1", style, null, out _));
        Assert.Throws<ArgumentException>(() => FixedPointNano.TryParse("1".AsSpan(), style, null, out _));
        Assert.Throws<ArgumentException>(() => FixedPointNano.TryParse((string?)null, style, null, out _));
    }
}
