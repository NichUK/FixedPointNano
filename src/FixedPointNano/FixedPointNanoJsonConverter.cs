using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seerstone;

/// <summary>
/// Opt-in converter that writes decimal JSON numbers and reads numbers or invariant numeric strings.
/// </summary>
/// <remarks>
/// Register through <see cref="JsonSerializerOptions.Converters"/> or a property-level
/// <see cref="JsonConverterAttribute"/>. Values are parsed as decimals and rounded to nine
/// places using <see cref="MidpointRounding.ToEven"/>, matching <see cref="FixedPointNano.FromDecimal"/>.
/// String parsing follows <see cref="FixedPointNano.TryParse(string?, out FixedPointNano)"/>.
/// Decimal-aware consumers preserve all stored digits; binary floating-point consumers may lose precision.
/// </remarks>
public sealed class FixedPointNanoJsonConverter : JsonConverter<FixedPointNano>
{
    /// <inheritdoc/>
    public override FixedPointNano Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (FixedPointNano.TryParse(reader.GetString(), out var result))
            {
                return result;
            }

            throw new JsonException("The JSON string is not a valid FixedPointNano value.");
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDecimal(out var value))
        {
            throw new JsonException("Expected a decimal JSON number or an invariant numeric string for FixedPointNano.");
        }

        try
        {
            return FixedPointNano.FromDecimal(value);
        }
        catch (OverflowException exception)
        {
            throw new JsonException("The JSON number is outside the FixedPointNano range.", exception);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, FixedPointNano value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToDecimal());
    }
}
