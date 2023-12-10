using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QtKaneko.JsonPropertyPath.Extensions;

static class JsonConverterExtensions
{
  static Dictionary<Type, ReadDelegate> _readDelegateCache = new(20);

  delegate object? ReadDelegate(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);

  public static object? Read(this JsonConverter converter,
                             ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
  {
    ref var readDelegate = ref CollectionsMarshal.GetValueRefOrAddDefault(_readDelegateCache, converter.GetType(), out var exists)!;
    if (!exists)
    {
      var instance = Expression.Constant(converter);
      var method = converter.GetType().GetMethod("Read", BindingFlags.Public | BindingFlags.Instance)!;
      var parameters = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

      var call = Expression.Call(instance, method, parameters);
      var cast = Expression.TypeAs(call, typeof(object));

      readDelegate = Expression.Lambda<ReadDelegate>(cast, parameters).Compile();
    }

    return readDelegate.Invoke(ref reader, type, options);
  }
}
