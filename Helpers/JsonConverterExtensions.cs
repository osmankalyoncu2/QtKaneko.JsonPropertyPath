// This file is part of QtKaneko.JsonPropertyPath.
//
// QtKaneko.JsonPropertyPath is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
// QtKaneko.JsonPropertyPath is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with QtKaneko.JsonPropertyPath. If not, see <https://www.gnu.org/licenses/>.
// Copyright 2022 Kaneko Qt

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
