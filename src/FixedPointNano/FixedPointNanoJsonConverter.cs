using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seerstone;

/// <summary>
/// A <see cref="JsonConverter{T}"/> for <see cref="FixedPointNano"/> that serialises values
/// as JSON numbers and deserialises from both JSON numbers and JSON strings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Serialisation</b>: a <see cref="FixedPointNano"/> value is written as a JSON decimal number
/// with up to 9 decimal places (e.g. <c>1234.56789</c>).  Trailing zeros are omitted.
/// </para>
/// <para>
/// <b>Deserialisation</b>: accepts both JSON numbers (<c>1234.56789</c>) and JSON strings
/// (<c>"1234.56789"</c>).  Strings are parsed using <see cref="FixedPointNano.TryParse(string?,out FixedPointNano)"/>
/// with invariant-culture semantics.
/// </para>
/// <para>
/// This converter is registered automatically on the type via
/// <see cref="JsonConverterAttribute"/>. Override it at the property or options level if you
/// need a different representation (for example, serialising as a raw <see langword="long"/>).
/// </para>
/// </remarks>
public sealed class FixedPointNanoJsonConverter : JsonConverter<FixedPointNano>
{
    /// <inheritdoc/>
    public override FixedPointNano Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => FixedPointNano.FromDecimal(reader.GetDecimal()),
            JsonTokenType.String => ParseFromString(ref reader),
            _ => throw new JsonException(
                $"Cannot deserialise FixedPointNano from a JSON token of type {reader.TokenType}."),
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, FixedPointNano value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToDecimal());
    }

    private static FixedPointNano ParseFromString(ref Utf8JsonReader reader)
    {
        var s = reader.GetString();
        if (s is null || !FixedPointNano.TryParse(s, out var result))
        {
            throw new JsonException(
                $"Cannot deserialise FixedPointNano from the JSON string \"{s}\".");
        }

        return result;
    }
}
