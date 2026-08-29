using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControleDeGastos.SharedKernel.Primitives;

/// <summary>
/// Serializa a competencia como "2026-08" em vez do objeto {year, month}.
/// O front trata competencia como string opaca; isto mantem o contrato estavel.
/// </summary>
public sealed class YearMonthJsonConverter : JsonConverter<YearMonth>
{
    public override YearMonth Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        YearMonth.Parse(reader.GetString() ?? throw new JsonException("Competencia nao pode ser nula."));

    public override void Write(Utf8JsonWriter writer, YearMonth value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
