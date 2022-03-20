// This file is part of QtKaneko.JsonPropertyPath.
//
// QtKaneko.JsonPropertyPath is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
// QtKaneko.JsonPropertyPath is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with QtKaneko.JsonPropertyPath. If not, see <https://www.gnu.org/licenses/>.

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
