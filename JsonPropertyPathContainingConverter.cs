using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using QtKaneko.JsonPropertyPath.Extensions;

namespace QtKaneko.JsonPropertyPath;

public class JsonPropertyPathContainingConverter : JsonConverter<object>
{
  const BindingFlags _binding = BindingFlags.Public | BindingFlags.Instance;

  [DebuggerStepThrough]
  public override bool CanConvert(Type typeToConvert)
  {
    var properties = typeToConvert.GetProperties(_binding);
    var fields = typeToConvert.GetFields(_binding);

    return properties.Any(property => property.IsDefined(typeof(JsonPropertyPathAttribute)))
        || fields.Any(field => field.IsDefined(typeof(JsonPropertyPathAttribute)));
  }

  public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
  {
    var json = (JsonObject)JsonNode.Parse(ref reader)!;
    var result = Activator.CreateInstance(type)!;

    foreach (var property in type.GetProperties(_binding))
    {
      if ((options.IgnoreReadOnlyFields && !property.CanWrite) &&
          !property.IsDefined(typeof(JsonIncludeAttribute))) continue;

      var memberValue = ReadMember(json, property, property.PropertyType, options);
      if (memberValue != null)
      {
        property.SetValue(result, memberValue);
      }
    }
    foreach (var field in type.GetFields(_binding))
    {
      if ((!options.IncludeFields || (options.IgnoreReadOnlyFields && field.IsInitOnly)) &&
          !field.IsDefined(typeof(JsonIncludeAttribute))) continue;

      var memberValue = ReadMember(json, field, field.FieldType, options);
      if (memberValue != null)
      {
        field.SetValue(result, memberValue);
      }
    }

    return result;
  }
  static object? ReadMember(JsonObject json, MemberInfo member, Type memberType, JsonSerializerOptions options)
  {
    var memberStream = new MemoryStream();
    var memberWriter = new Utf8JsonWriter(memberStream);
    if (member.GetCustomAttribute<JsonPropertyPathAttribute>() is {} attribute)
    {
      var matches = attribute.Path.Evaluate(json).Matches!;

      if (matches.Count == 0) return null;
      else if (matches.Count == 1)
      {
        memberWriter.WriteValue(matches[0].Value);
      }
      else // matches.Count > 1
      {
        var mergeMode = attribute.MergeMode;
        if (mergeMode == JsonPropertyPathAttribute.MergeModes.Auto)
        {
          var name = matches[0].GetName();
          mergeMode = matches.All(match => match.GetName() == name)
                    ? JsonPropertyPathAttribute.MergeModes.Array
                    : JsonPropertyPathAttribute.MergeModes.Class;
        }

        if (mergeMode == JsonPropertyPathAttribute.MergeModes.Array)
        {
          memberWriter.WriteStartArray();
          foreach (var match in matches)
          {
            memberWriter.WriteValue(match.Value);
          }
          memberWriter.WriteEndArray();
        }
        else if (mergeMode == JsonPropertyPathAttribute.MergeModes.Class)
        {
          memberWriter.WriteStartObject();
          foreach (var match in matches)
          {
            memberWriter.WritePropertyName(match.GetName());
            memberWriter.WriteValue(match.Value);
          }
          memberWriter.WriteEndObject();
        }
      }
    }
    else if (json.TryGetPropertyValue(options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name,
                                      out var memberJson))
    {
      memberWriter.WriteValue(memberJson);
    }
    else return null;
    memberWriter.Flush();

    var memberSpan = new ReadOnlySpan<byte>(memberStream.GetBuffer(), 0, (int)memberStream.Length);

    #if DEBUG
      var debugMemberJson = System.Text.Encoding.UTF8.GetString(memberSpan);
    #endif

    var memberReader = new Utf8JsonReader(memberSpan);
    memberReader.Read();

    var converter = member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType is {} converterType
                  ? (JsonConverter)Activator.CreateInstance(converterType)!
                  : options.GetConverter(memberType);

    try
    {
      var memberValue = converter?.Read(ref memberReader, memberType, options);

      return memberValue;
    }
    catch (Exception ex)
    {
      throw new JsonException($"Error while parsing '{member.Name}': {ex.Message}", ex);
    }
  }

  public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  {
    throw new NotSupportedException();
  }
}