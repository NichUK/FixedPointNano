using System;
using System.Globalization;
using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoParseTests
{
    // -----------------------------------------------------------------------------------------
    // Parse(string, IFormatProvider?)
    // -----------------------------------------------------------------------------------------

    [Test]
    public void ParseStringShouldReturnCorrectValue()
    {
        var result = FixedPointNano.Parse("1.5", CultureInfo.InvariantCulture);
        Assert.That(result.ToDecimal(), Is.EqualTo(1.5m));
    }

    [Test]
    public void ParseStringShouldHandleNegativeValue()
    {
        var result = FixedPointNano.Parse("-42.000000001", CultureInfo.InvariantCulture);
        Assert.That(result.ToDecimal(), Is.EqualTo(-42.000000001m));
    }

    [Test]
    public void ParseStringShouldHandleZero()
    {
        var result = FixedPointNano.Parse("0", CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo(FixedPointNano.Zero));
    }

    [Test]
    public void ParseStringShouldUseInvariantCultureWhenProviderIsNull()
    {
        // Invariant culture uses '.' as decimal separator
        var result = FixedPointNano.Parse("3.14");
        Assert.That(result.ToDecimal(), Is.EqualTo(3.14m));
    }

    [Test]
    public void ParseStringShouldThrowFormatExceptionOnInvalidInput()
    {
        Assert.That(() => FixedPointNano.Parse("not-a-number", CultureInfo.InvariantCulture),
            Throws.TypeOf<FormatException>());
    }

    [Test]
    public void ParseStringShouldThrowArgumentNullExceptionForNull()
    {
        Assert.That(() => FixedPointNano.Parse(null!, CultureInfo.InvariantCulture),
            Throws.TypeOf<ArgumentNullException>());
    }

    // -----------------------------------------------------------------------------------------
    // Parse(ReadOnlySpan<char>, IFormatProvider?)
    // -----------------------------------------------------------------------------------------

    [Test]
    public void ParseSpanShouldReturnCorrectValue()
    {
        ReadOnlySpan<char> span = "1234.567890123";
        var result = FixedPointNano.Parse(span, CultureInfo.InvariantCulture);
        Assert.That(result.ToDecimal(), Is.EqualTo(1234.567890123m));
    }

    [Test]
    public void ParseSpanShouldThrowFormatExceptionOnInvalidInput()
    {
        Assert.That(() => FixedPointNano.Parse("xyz".AsSpan(), CultureInfo.InvariantCulture),
            Throws.TypeOf<FormatException>());
    }

    // -----------------------------------------------------------------------------------------
    // TryParse(string?, IFormatProvider, out FixedPointNano)
    // -----------------------------------------------------------------------------------------

    [Test]
    public void TryParseStringWithProviderShouldReturnTrueForValidInput()
    {
        var succeeded = FixedPointNano.TryParse("1.25", CultureInfo.InvariantCulture, out var result);
        Assert.That(succeeded, Is.True);
        Assert.That(result.ToDecimal(), Is.EqualTo(1.25m));
    }

    [Test]
    public void TryParseStringWithProviderShouldReturnFalseForNull()
    {
        var succeeded = FixedPointNano.TryParse(null, CultureInfo.InvariantCulture, out var result);
        Assert.That(succeeded, Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
    }

    [Test]
    public void TryParseStringWithProviderShouldReturnFalseForInvalidInput()
    {
        var succeeded = FixedPointNano.TryParse("garbage", CultureInfo.InvariantCulture, out var result);
        Assert.That(succeeded, Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
    }

    [Test]
    public void TryParseStringWithProviderShouldReturnFalseForOutOfRangeValue()
    {
        // Value too large to fit in FixedPointNano (exceeds long.MaxValue / Scale)
        const string tooLarge = "99999999999999999999";
        var succeeded = FixedPointNano.TryParse(tooLarge, CultureInfo.InvariantCulture, out var result);
        Assert.That(succeeded, Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
    }

    // -----------------------------------------------------------------------------------------
    // TryParse(string?, out FixedPointNano) — invariant culture shorthand
    // -----------------------------------------------------------------------------------------

    [Test]
    public void TryParseStringInvariantShouldReturnTrueForValidInput()
    {
        var succeeded = FixedPointNano.TryParse("0.000000001", out var result);
        Assert.That(succeeded, Is.True);
        Assert.That(result, Is.EqualTo(FixedPointNano.Epsilon));
    }

    [Test]
    public void TryParseStringInvariantShouldReturnFalseForNull()
    {
        var succeeded = FixedPointNano.TryParse((string?)null, out var result);
        Assert.That(succeeded, Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
    }

    // -----------------------------------------------------------------------------------------
    // TryParse(ReadOnlySpan<char>, IFormatProvider, out FixedPointNano)
    // -----------------------------------------------------------------------------------------

    [Test]
    public void TryParseSpanShouldReturnTrueForValidInput()
    {
        ReadOnlySpan<char> span = "-1.5";
        var succeeded = FixedPointNano.TryParse(span, CultureInfo.InvariantCulture, out var result);
        Assert.That(succeeded, Is.True);
        Assert.That(result.ToDecimal(), Is.EqualTo(-1.5m));
    }

    [Test]
    public void TryParseSpanShouldReturnFalseForInvalidInput()
    {
        ReadOnlySpan<char> span = "!bad!";
        var succeeded = FixedPointNano.TryParse(span, CultureInfo.InvariantCulture, out var result);
        Assert.That(succeeded, Is.False);
        Assert.That(result, Is.EqualTo(default(FixedPointNano)));
    }

    // -----------------------------------------------------------------------------------------
    // TryParse(ReadOnlySpan<char>, out FixedPointNano) — invariant culture shorthand
    // -----------------------------------------------------------------------------------------

    [Test]
    public void TryParseSpanInvariantShouldReturnTrueForValidInput()
    {
        ReadOnlySpan<char> span = "100.5";
        var succeeded = FixedPointNano.TryParse(span, out var result);
        Assert.That(succeeded, Is.True);
        Assert.That(result.ToDecimal(), Is.EqualTo(100.5m));
    }

    // -----------------------------------------------------------------------------------------
    // Round with invalid MidpointRounding mode
    // -----------------------------------------------------------------------------------------

    [Test]
    public void RoundWithInvalidMidpointRoundingModeShouldThrow()
    {
        var value = FixedPointNano.FromDecimal(1.5m);
        var invalidMode = (MidpointRounding)999;
        Assert.That(() => FixedPointNano.Round(value, 0, invalidMode),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    // -----------------------------------------------------------------------------------------
    // Parse/TryParse round-trip
    // -----------------------------------------------------------------------------------------

    [TestCase("0")]
    [TestCase("0.000000001")]
    [TestCase("-0.000000001")]
    [TestCase("1.234567890")]
    [TestCase("-1234.567890123")]
    [TestCase("1000000.000000001")]
    public void ParseThenToStringShouldRoundTrip(string input)
    {
        var value = FixedPointNano.Parse(input, CultureInfo.InvariantCulture);
        var reparsed = FixedPointNano.Parse(value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        Assert.That(reparsed, Is.EqualTo(value));
    }
}
