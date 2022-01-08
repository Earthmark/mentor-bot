using System.Text.Json;
using System.Text.Json.Serialization;

namespace MentorBot
{
  public static class JsonSerializerOptionsExtensions
  {
    public static JsonSerializerOptions ConfigureForMentor(this JsonSerializerOptions options)
    {
      options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
      return options;
    }
  }
}
