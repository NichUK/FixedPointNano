using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoJsonConverterTests
{
    [TestCase(0L, "0")]
    [TestCase(1L, "0.000000001")]
    [TestCase(-1L, "-0.000000001")]
    [TestCase(42_000_000_000L, "42")]
    [TestCase(long.MaxValue, "9223372036.854775807")]
    [TestCase(long.MinValue, "-9223372036.854775808")]
    public void OptionsRegistrationShouldRoundTripExactValues(long rawValue, string expectedJson)
    {
        var options = CreateOptions();
        var value = FixedPointNano.FromRaw(rawValue);

        Assert.That(JsonSerializer.Serialize(value, options), Is.EqualTo(expectedJson));
        Assert.That(JsonSerializer.Deserialize<FixedPointNano>(expectedJson, options), Is.EqualTo(value));
        Assert.That(JsonSerializer.Deserialize<FixedPointNano>(JsonSerializer.Serialize(expectedJson), options), Is.EqualTo(value));
    }

    [TestCase("1.0000000005", 1_000_000_000L)]
    [TestCase("1.0000000015", 1_000_000_002L)]
    [TestCase("-1.0000000005", -1_000_000_000L)]
    [TestCase("-1.0000000015", -1_000_000_002L)]
    [TestCase("9223372036.8547758074", long.MaxValue)]
    [TestCase("-9223372036.8547758085", long.MinValue)]
    public void NumberAndStringShouldUseBankersRounding(string text, long expectedRawValue)
    {
        var options = CreateOptions();

        Assert.That(JsonSerializer.Deserialize<FixedPointNano>(text, options).RawValue, Is.EqualTo(expectedRawValue));
        Assert.That(JsonSerializer.Deserialize<FixedPointNano>(JsonSerializer.Serialize(text), options).RawValue, Is.EqualTo(expectedRawValue));
    }

    [TestCase("10000000000")]
    [TestCase("-10000000000")]
    [TestCase("9223372036.8547758075")]
    [TestCase("-9223372036.8547758086")]
    [TestCase("79228162514264337593543950335")]
    [TestCase("79228162514264337593543950336")]
    public void OutOfRangeNumberAndStringShouldThrowJsonException(string text)
    {
        var options = CreateOptions();

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FixedPointNano>(text, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FixedPointNano>(JsonSerializer.Serialize(text), options));
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("\"invalid\"")]
    [TestCase("\"\"")]
    [TestCase("\"NaN\"")]
    [TestCase("1e1000")]
    [TestCase("1.")]
    public void InvalidInputShouldThrowJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FixedPointNano>(json, CreateOptions()));
    }

    [Test]
    public void NumberShouldAcceptExponentNotation()
    {
        Assert.That(JsonSerializer.Deserialize<FixedPointNano>("1e-9", CreateOptions()), Is.EqualTo(FixedPointNano.Epsilon));
    }

    [Test]
    public void ConverterShouldUseInvariantCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var options = CreateOptions();
            var value = FixedPointNano.FromDecimal(1234.5m);

            Assert.That(JsonSerializer.Serialize(value, options), Is.EqualTo("1234.5"));
            Assert.That(JsonSerializer.Deserialize<FixedPointNano>("\"1,234.5\"", options), Is.EqualTo(value));
            Assert.That(JsonSerializer.Deserialize<FixedPointNano>("\"1234.5\"", options), Is.EqualTo(value));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void DefaultSerializationShouldKeepObjectRepresentation()
    {
        var value = FixedPointNano.FromRaw(123L);

        Assert.That(JsonSerializer.Serialize(value), Is.EqualTo("{\"RawValue\":123}"));
        Assert.That(Attribute.IsDefined(typeof(FixedPointNano), typeof(JsonConverterAttribute)), Is.False);
    }

    [Test]
    public void PropertyRegistrationShouldOnlyConvertSelectedProperty()
    {
        var value = new PriceDto { Price = FixedPointNano.Epsilon, Unconverted = FixedPointNano.One };
        var json = JsonSerializer.Serialize(value);

        Assert.That(json, Is.EqualTo("{\"Price\":0.000000001,\"Unconverted\":{\"RawValue\":1000000000}}"));
        Assert.That(JsonSerializer.Deserialize<PriceDto>(json)!.Price, Is.EqualTo(value.Price));
    }

    [Test]
    public void NullableAndArrayValuesShouldFollowSerializerSemantics()
    {
        var options = CreateOptions();
        FixedPointNano?[] values = [FixedPointNano.MinValue, null, FixedPointNano.Epsilon, FixedPointNano.MaxValue];
        var json = JsonSerializer.Serialize(values, options);

        Assert.That(JsonSerializer.Deserialize<FixedPointNano?[]>(json, options), Is.EqualTo(values));
        Assert.That(JsonSerializer.Serialize<FixedPointNano?>(null, options), Is.EqualTo("null"));
        Assert.That(JsonSerializer.Deserialize<FixedPointNano?>("null", options), Is.Null);
    }

    [Test]
    public void InvalidPropertyShouldReportJsonPath()
    {
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PriceDto>("{\"Price\":10000000000}"));

        Assert.That(exception!.Path, Is.EqualTo("$.Price"));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new FixedPointNanoJsonConverter());
        return options;
    }

    private sealed class PriceDto
    {
        [JsonConverter(typeof(FixedPointNanoJsonConverter))]
        public FixedPointNano Price { get; init; }

        public FixedPointNano Unconverted { get; init; }
    }
}
