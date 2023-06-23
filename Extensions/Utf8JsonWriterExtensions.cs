using System.Text.Json;
using System.Text.Json.Nodes;

namespace QtKaneko.JsonPropertyPath.Extensions;

static class Utf8JsonWriterExtensions
{
  public static void WriteValue(this Utf8JsonWriter writer, JsonNode? value)
  {
    if (value != null) value.WriteTo(writer);
    else writer.WriteNullValue();
  }
}