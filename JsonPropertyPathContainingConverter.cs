// This file is part of QtKaneko.JsonPropertyPath.
// 
// QtKaneko.JsonPropertyPath is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// 
// QtKaneko.JsonPropertyPath is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along with QtKaneko.JsonPropertyPath. If not, see <https://www.gnu.org/licenses/>. 

using Json.Path;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QtKaneko.JsonPropertyPath;

public class JsonPropertyPathContainingConverter : JsonConverter<dynamic>
{
  const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance;

  public override bool CanConvert(Type typeToConvert)
  {
    var properties = typeToConvert.GetProperties(_bindingFlags);
    var fields = typeToConvert.GetFields(_bindingFlags);

    return properties.Any(property => property.IsDefined(typeof(JsonPropertyPathAttribute)))
        || fields.Any(field => field.IsDefined(typeof(JsonPropertyPathAttribute)));
  }

  public override dynamic Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var jsonObj = JsonElement.ParseValue(ref reader);
    var obj = Activator.CreateInstance(typeToConvert);

    foreach (var member in Enumerable.Concat<MemberInfo>(typeToConvert.GetProperties(_bindingFlags),
                                                         typeToConvert.GetFields(_bindingFlags)))
    {
      if (member is FieldInfo && !options.IncludeFields && !member.IsDefined(typeof(JsonIncludeAttribute)))
        continue;

      var memberType = GetType(member);

      if (member.GetCustomAttribute<JsonPropertyPathAttribute>() is { } attribute)
      {
        if (JsonPath.Parse(attribute.Path).Evaluate(jsonObj).Matches is { } matches)
        {
          if (matches.Count == 1)
          {
            SetValue(member, obj, matches.First().Value.Deserialize(memberType));
          }
          else if (matches.Count > 1)
          {
            var arr = memberType.GetInterfaces();

            Type valueType;
            if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
              valueType = memberType.GetGenericArguments()[0];
            }
            else
            {
              valueType = memberType.GetInterfaces()
                                      .Where(@interface => @interface.IsGenericType
                                                        && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                      .FirstOrDefault()
                                     ?.GetGenericArguments()[0];
            }
            if (matchesType == null) throw new Exception($"JsonPath have matched multiple elements, but {member.DeclaringType}.{member.Name} can contain only one.");

            var values = matches.Select(match => match.Value.Deserialize(valueType, options))
                                .ToArray();

            // We need copy matches to type-matching container to avoid "object[] can't be converted to type[]"
            var valueContainer = Array.CreateInstance(valueType, matches.Count);
            values.CopyTo(valueContainer, 0);

            SetValue(member, obj, valueContainer);
          }
        }
      }
      else
      {
        if (jsonObj.TryGetProperty(options.PropertyNamingPolicy.ConvertName(member.Name), out var jsonProperty))
        {
          SetValue(member, obj, jsonProperty.Deserialize(memberType, options));
        }
      }
    }

    return obj;

    void SetValue(MemberInfo member, object? obj, object? value)
    {
      switch (member)
      {
        case PropertyInfo property: property.SetValue(obj, value); break;
        case FieldInfo field: field.SetValue(obj, value); break;
      }
    }
    Type GetType(MemberInfo member)
    {
      return (member) switch
      {
        PropertyInfo property => property.PropertyType,
        FieldInfo field => field.FieldType
      };
    }
  }
  public override void Write(Utf8JsonWriter writer, dynamic value, JsonSerializerOptions options)
  {
    throw new NotImplementedException();
  }
}
