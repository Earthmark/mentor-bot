using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace MentorBot.Tests
{
  public class UrlEncoderTests
  {
    private readonly JsonSerializerOptions _opts = new ()
    {
      Converters =
      {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
      },
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public record SerializeObject
    {
      public string A { get; set; }
      public string B { get; set; }
      public ValueThing V { get; set; }
    }

    public enum ValueThing
    {
      ItsA,
      ItsB
    }

    [Fact]
    public void EncodeObject()
    {
      var obj = new SerializeObject
      {
        A = "Taco",
        B = "Legume",
        V = ValueThing.ItsA,
      };
      var encoded = UrlEncoder.Encode(obj, _opts);

      Assert.Equal("a=Taco&b=Legume&v=itsA", encoded);
    }

    [Fact]
    public void DecodeObject()
    {
      var expected = new SerializeObject
      {
        A = "Taco",
        B = "Legume",
        V = ValueThing.ItsB,
      };
      var obj = UrlEncoder.Decode<SerializeObject>("a=Taco&b=Legume&v=itsb", _opts);

      Assert.Equal(expected, obj);
    }
  }
}
