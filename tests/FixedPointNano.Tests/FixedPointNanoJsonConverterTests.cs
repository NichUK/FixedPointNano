using System.Text.Json;
using System.Text.Json.Serialization;
using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoJsonConverterTests
{
    // ── Serialisation ────────────────────────────────────────────────────────

    [Test]
    public void SerialisePositiveValueShouldProduceJsonNumber()
    {
        var value = FixedPointNano.FromDecimal(1234.56789m);
        var json = JsonSerializer.Serialize(value);
        Assert.That(json, Is.EqualTo("1234.56789"));
    }

    [Test]
    public void SerialiseNegativeValueShouldProduceJsonNumber()
    {
        var value = FixedPointNano.FromDecimal(-1234.56789m);
        var json = JsonSerializer.Serialize(value);
        Assert.That(json, Is.EqualTo("-1234.56789"));
    }

    [Test]
    public void SerialiseZeroShouldProduceJsonZero()
    {
        var json = JsonSerializer.Serialize(FixedPointNano.Zero);
        Assert.That(json, Is.EqualTo("0"));
    }

    [Test]
    public void SerialiseIntegerValueShouldProduceJsonNumberWithoutDecimalPoint()
    {
        var value = FixedPointNano.FromDecimal(42m);
        var json = JsonSerializer.Serialize(value);
        Assert.That(json, Is.EqualTo("42"));
    }

    [Test]
    public void SerialiseAndDeserialiseRoundTrip()
    {
        var original = FixedPointNano.FromDecimal(987.654321m);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<FixedPointNano>(json);
        Assert.That(restored, Is.EqualTo(original));
    }

    [Test]
    public void SerialiseValueInObjectShouldWork()
    {
        var dto = new PriceDto { Price = FixedPointNano.FromDecimal(99.99m) };
        var json = JsonSerializer.Serialize(dto);
        Assert.That(json, Is.EqualTo("""{"Price":99.99}"""));
    }

    // ── Deserialisation from number ──────────────────────────────────────────

    [Test]
    public void DeserialiseFromJsonNumberShouldSucceed()
    {
        var value = JsonSerializer.Deserialize<FixedPointNano>("1234.56789");
        Assert.That(value, Is.EqualTo(FixedPointNano.FromDecimal(1234.56789m)));
    }

    [Test]
    public void DeserialiseFromJsonNegativeNumberShouldSucceed()
    {
        var value = JsonSerializer.Deserialize<FixedPointNano>("-0.000000001");
        Assert.That(value, Is.EqualTo(FixedPointNano.Epsilon * FixedPointNano.NegativeOne));
    }

    [Test]
    public void DeserialiseFromJsonZeroShouldGiveZero()
    {
        var value = JsonSerializer.Deserialize<FixedPointNano>("0");
        Assert.That(value, Is.EqualTo(FixedPointNano.Zero));
    }

    // ── Deserialisation from string ──────────────────────────────────────────

    [Test]
    public void DeserialiseFromJsonStringShouldSucceed()
    {
        var value = JsonSerializer.Deserialize<FixedPointNano>("""
            "1234.56789"
            """);
        Assert.That(value, Is.EqualTo(FixedPointNano.FromDecimal(1234.56789m)));
    }

    [Test]
    public void DeserialiseFromJsonNegativeStringShouldSucceed()
    {
        var value = JsonSerializer.Deserialize<FixedPointNano>("""
            "-42"
            """);
        Assert.That(value, Is.EqualTo(FixedPointNano.FromDecimal(-42m)));
    }

    // ── Error cases ──────────────────────────────────────────────────────────

    [Test]
    public void DeserialiseFromJsonBoolShouldThrow()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FixedPointNano>("true"));
    }

    [Test]
    public void DeserialiseFromJsonNullShouldThrow()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FixedPointNano>("null"));
    }

    [Test]
    public void DeserialiseFromInvalidStringShouldThrow()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FixedPointNano>("""
            "not-a-number"
            """));
    }

    // ── Converter registration ───────────────────────────────────────────────

    [Test]
    public void ConverterShouldBeRegisteredViaAttribute()
    {
        var converterAttr = (JsonConverterAttribute?)Attribute.GetCustomAttribute(
            typeof(FixedPointNano), typeof(JsonConverterAttribute));

        Assert.That(converterAttr, Is.Not.Null);
        Assert.That(converterAttr!.ConverterType, Is.EqualTo(typeof(FixedPointNanoJsonConverter)));
    }

    [Test]
    public void ConverterShouldHandleValueInsideArray()
    {
        var values = new[]
        {
            FixedPointNano.FromDecimal(1.1m),
            FixedPointNano.FromDecimal(2.2m),
            FixedPointNano.FromDecimal(3.3m),
        };

        var json = JsonSerializer.Serialize(values);
        var restored = JsonSerializer.Deserialize<FixedPointNano[]>(json);

        Assert.That(restored, Is.EqualTo(values));
    }

    // ── Test DTO ─────────────────────────────────────────────────────────────

    private sealed class PriceDto
    {
        public FixedPointNano Price { get; init; }
    }
}
