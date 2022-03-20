// This file is part of QtKaneko.JsonPropertyPath.
//
// QtKaneko.JsonPropertyPath is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
// QtKaneko.JsonPropertyPath is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with QtKaneko.JsonPropertyPath. If not, see <https://www.gnu.org/licenses/>. 

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

        if (matches.Count == 1)
        {
          memberValue = Deserialize(matches[0].Value, memberType, options,
                                    member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType);
        }
        else if (matches.Count > 1)
        {
          if (!memberType.TryGetEnumerableElementType(out var elementType))
            throw new ArgumentOutOfRangeException(member.Name,
                                                  $"JsonPath have matched multiple elements, " +
                                                  $"but {member.DeclaringType}.{member.Name} can contain only one.");

          memberValue = Array.CreateInstance(elementType, matches.Count);
          for (int matchIndex = 0; matchIndex < matches.Count; ++matchIndex)
          {
            var element = Deserialize(matches[matchIndex].Value, memberType, options,
                                      member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType);

            memberValue.SetValue(element, matchIndex);
          }
        }
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
