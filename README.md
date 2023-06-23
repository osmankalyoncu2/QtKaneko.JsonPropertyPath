## How to use:
1. Install [NuGet package](https://www.nuget.org/packages/QtKaneko.JsonPropertyPath/).
2. Add JsonPropertyPath attribute to properties or fields you want to map with JSONPath:
```cs
using QtKaneko.JsonPropertyPath;

// Example JSON:
// {
//   "firstName": "John",
//   "lastName": "doe",
//   "age": 26,
//   "address": {
//     "streetAddress": "naist street",
//     "city": "Nara",
//     "postalCode": "630-0192"
//   },
//   "phoneNumbers": [
//     {
//       "type": "iPhone",
//       "number": "0123-4567-8888"
//     },
//     {
//       "type": "home",
//       "number": "0123-4567-8910"
//     }
//   ]
// }
class Person
{
  [JsonPropertyPath("$..streetAddress")]
  public string StreetAddress { get; set; } // This property will be mapped to streetAddress from JSON
}
```

3. Add JsonPropertyPathContainingConverter to Converters in your JsonSerializerOptions:
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

If JSON property names are the same, they will be merged into array before deserialization:
```cs
// Example JSON:
// {
//   "firstName": "John",
//   "lastName": "doe",
//   "age": 26,
//   "address": {
//     "streetAddress": "naist street",
//     "city": "Nara",
//     "postalCode": "630-0192"
//   },
//   "phoneNumbers": [
//     {
//       "type": "iPhone",
//       "number": "0123-4567-8888"
//     },
//     {
//       "type": "home",
//       "number": "0123-4567-8910"
//     }
//   ]
// }
class Person
{
  [JsonPropertyPath("$..number")]
  public string[] PhoneNumbers { get; set; } // This property will be mapped to all number's from JSON
}
```

If JSON property names are not the same, they will be merged into object before deserialization:
```cs
// Example JSON:
// {
//   "glossary": {
//     "title": "example glossary",
//     "GlossDiv": {
//       "title": "S",
//       "GlossList": {
//         "GlossEntry": {
//           "ID": "SGML",
//           "SortAs": "SGML",
//           "GlossTerm": "Standard Generalized Markup Language",
//           "Acronym": "SGML",
//           "Abbrev": "ISO 8879:1986",
//           "GlossDef": {
//             "para": "A meta-markup language, used to create markup languages such as DocBook.",
//             "GlossSeeAlso": [
//               "GML",
//               "XML"
//             ]
//           },
//           "GlossSee": "markup"
//         }
//       }
//     }
//   }
// }
class GlossEntry
{
  public string ID { get; set; }
  public string GlossTerm { get; set; }
}
class Glossary
{
  [JsonPropertyPath("$..[ID,GlossTerm]")]
  public GlossEntry GlossEntry { get; set; } // This property will be mapped to object created from ID and GlossTerm from JSON
}
```

This behavior can be changed manually with `mergeMode` in `JsonPropertyPathAttribute` (`JsonPropertyPath` argument) constructor.

> You can experiment with JSONPath at https://json-everything.net/json-path/

- And all of this can be used for nested objects!