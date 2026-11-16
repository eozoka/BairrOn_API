using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BairrOn.Api.Validation;

// confere se as datas recebidas informam o fuso horário
public class DataComFusoConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Informe uma data com fuso horário.");

        var texto = reader.GetString()!;
        // aceita uma data terminada em z ou em um fuso como -03:00
        bool temFuso = Regex.IsMatch(texto, @"T.*(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant);
        bool dataValida = DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data);
        if (!temFuso || !dataValida)
            throw new JsonException("Use uma data como 2026-11-15T09:00:00-03:00.");
        return data;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
