## How to use:
1. Add JsonPropertyPath attribute to properties or fields you want to map with JSONPath:
```cs
using QtKaneko.JsonPropertyPath;

// Example JS:
// {
//   "obj1": {
//     "obj2": {
//       "testInt": 1
//     }
//   }
// }
class Test
{
  [JsonPropertyPath("$..testInt")]
  public int TestInt { get; set; } // This property will be mapped to testInt from JSON
}
```

2. Add JsonPropertyPathContainingConverter to Converters in your JsonSerializerOptions:
```cs
using QtKaneko.JsonPropertyPath;

var options = new JsonSerializerOptions()
{
  Converters = {
    new JsonPropertyPathContainingConverter()
  }
}
```

## Features:
- You can map multiple JSON properties to .NET one:
```cs
// Example JS:
// {
//   "obj1": {
//     "obj2": {
//       "testInt": 1
//     },
//     "obj3": {
//       "testInt": 2
//     }
//   }
// }
class Test
{
  [JsonPropertyPath("$..testInt")]
  public int[] TestInt { get; set; } // This property will be mapped to all testInt's from JSON
}
```
