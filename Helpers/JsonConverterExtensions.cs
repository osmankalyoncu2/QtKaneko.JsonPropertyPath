using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QtKaneko.JsonPropertyPath.Helpers;

static class JsonConverterExtensions
{
  delegate object ReadDelegate(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);

  internal static object Read(this JsonConverter converter,
                              ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
  {
    var instance = Expression.Constant(converter);
    var method = converter.GetType().GetMethod("Read", BindingFlags.Public | BindingFlags.Instance);
    var parameters = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

    var call = Expression.Call(instance, method, parameters);
    var cast = Expression.TypeAs(call, typeof(object));

    var @delegate = Expression.Lambda<ReadDelegate>(cast, parameters);
    
    return @delegate.Compile()(ref reader, type, options);
  }
}
