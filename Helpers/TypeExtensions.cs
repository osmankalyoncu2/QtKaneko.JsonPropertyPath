namespace QtKaneko.JsonPropertyPath.Helpers;

static class TypeExtensions
{
  internal static bool TryGetEnumerableElementType(this Type enumerable, out Type elementType)
  {
    if (enumerable.GetGenericTypeDefinition() == typeof(IEnumerable<>))
    {
      elementType = enumerable.GetGenericArguments()[0];
    }
    else
    {
      elementType = enumerable.GetInterfaces()
                              .Where(@interface => @interface.IsGenericType
                                                && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                              .FirstOrDefault()?.GetGenericArguments()[0];
    }

    return elementType != default;
  }
}
