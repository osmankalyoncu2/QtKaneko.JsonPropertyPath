using System.Text.Json.Serialization;
using Json.Path;

namespace QtKaneko.JsonPropertyPath;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class JsonPropertyPathAttribute : JsonAttribute
{
  public enum MergeModes
  {
    Auto,
    Class,
    Array
  }

  public readonly JsonPath Path;
  public readonly MergeModes MergeMode;

  public JsonPropertyPathAttribute(string path, MergeModes mergeMode = MergeModes.Auto)
  {
    Path = JsonPath.Parse(path.StartsWith("$") ? path :
                          path.StartsWith(".") ? $"${path}" // To handle "..something"
                                               : $"$.{path}");
    MergeMode = mergeMode;
  }
}
