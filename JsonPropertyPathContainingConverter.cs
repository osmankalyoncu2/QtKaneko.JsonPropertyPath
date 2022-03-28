// This file is part of QtKaneko.JsonPropertyPath.
//
// QtKaneko.JsonPropertyPath is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
// QtKaneko.JsonPropertyPath is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with QtKaneko.JsonPropertyPath. If not, see <https://www.gnu.org/licenses/>. 
// Copyright 2022 Kaneko Qt

using Json.Path;

using QtKaneko.JsonPropertyPath.Helpers;

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QtKaneko.JsonPropertyPath;

public class JsonPropertyPathContainingConverter : JsonConverter<object>
{
  const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance;

  public override bool CanConvert(Type typeToConvert)
  {
    var properties = typeToConvert.GetProperties(_bindingFlags);
    var fields = typeToConvert.GetFields(_bindingFlags);

    return properties.Any(property => property.IsDefined(typeof(JsonPropertyPathAttribute)))
        || fields.Any(field => field.IsDefined(typeof(JsonPropertyPathAttribute)));
  }

  public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
  {
    var json = JsonElement.ParseValue(ref reader);
    var @object = Activator.CreateInstance(type);

    foreach (var member in Enumerable.Concat<MemberInfo>(type.GetProperties(_bindingFlags),
                                                         type.GetFields(_bindingFlags)))
    {
      if (member is FieldInfo && !options.IncludeFields && !member.IsDefined(typeof(JsonIncludeAttribute)))
        continue;

      var memberType = member switch
      {
        PropertyInfo property => property.PropertyType,
        FieldInfo field => field.FieldType
      };

      dynamic memberValue = default;
      if (member.GetCustomAttribute<JsonPropertyPathAttribute>() is { } attribute)
      {
        var matches = JsonPath.Parse(attribute.Path).Evaluate(json).Matches;

        var mergeMode = attribute.MergeMode;
        if (mergeMode == JsonPropertyPathAttribute.MergeModes.Auto)
          mergeMode = matches.All(m => m.Location.Source.EndsWith(matches[0].Location.Source.Split('/').Last()))
                      ? JsonPropertyPathAttribute.MergeModes.Array
                      : JsonPropertyPathAttribute.MergeModes.Class;

        var matchesObjectStream = new MemoryStream();
        var matchesObjectWriter = new Utf8JsonWriter(matchesObjectStream);
        switch (mergeMode)
        {
          case JsonPropertyPathAttribute.MergeModes.Array:
            {
              matchesObjectWriter.WriteStartArray();
              foreach (var match in matches)
              {
                match.Value.WriteTo(matchesObjectWriter);
              }
              matchesObjectWriter.WriteEndArray();
              break;
            }
            case JsonPropertyPathAttribute.MergeModes.Class:
            {
              matchesObjectWriter.WriteStartObject();
              foreach (var match in matches)
              {
                matchesObjectWriter.WritePropertyName(match.Location.Source.Split('/').Last());
                match.Value.WriteTo(matchesObjectWriter);
              }
              matchesObjectWriter.WriteEndObject();
              break;
            }
        }
        matchesObjectWriter.Flush();
        var matchesObjectReader = new Utf8JsonReader(matchesObjectStream.ToArray());
        var matchesObject = JsonElement.ParseValue(ref matchesObjectReader);

        memberValue = Deserialize(matchesObject, memberType, options,
                                  member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType);
      }
      else
      {
        if (!json.TryGetProperty(options.PropertyNamingPolicy.ConvertName(member.Name), out var property))
          continue;

        memberValue = Deserialize(property, memberType, options,
                                  member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType);
      }

      switch (member)
      {
        case PropertyInfo property: property.SetValue(@object, memberValue); break;
        case FieldInfo field: field.SetValue(@object, memberValue); break;
      }
    }

    return @object;
  }
  public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) =>
    throw new NotImplementedException();

  static object Deserialize(JsonElement json, Type returnType, JsonSerializerOptions options,
                            Type converter = default)
  {
    object deserialized;
    if (converter != default)
    {
      var converterInstance = (JsonConverter)Activator.CreateInstance(converter);

      var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json.GetRawText()));
      reader.Read();

      deserialized = converterInstance.Read(ref reader, returnType, options);
    }
    else
    {
      deserialized = json.Deserialize(returnType, options);
    }

    return deserialized;
  }
}
