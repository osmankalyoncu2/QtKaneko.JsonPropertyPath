using Json.Path;

namespace QtKaneko.JsonPropertyPath.Extensions;

static class NodeExtensions
{
  public static string GetName(this Node node)
  {
    var location = node.Location!;
    var selector = (NameSelector)location.Segments[^1].Selectors[0];

    return selector.Name;
  }
}