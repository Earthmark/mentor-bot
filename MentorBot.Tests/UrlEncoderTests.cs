using Xunit;

namespace MentorBot.Tests
{
  public class UrlEncoderTests
  {
    public record SerializeObject
    {
      public string A { get; init; }
      public string B { get; init; }
    }

    [Fact]
    public void EncodeObject()
    {
      var obj = new SerializeObject
      {
        A = "Taco",
        B = "Legume"
      };
      var encoded = UrlEncoder.Encode(obj);

      Assert.Equal("A=Taco&B=Legume", encoded);
    }

    [Fact]
    public void DecodeObject()
    {
      var expected = new SerializeObject
      {
        A = "Taco",
        B = "Legume"
      };
      var obj = UrlEncoder.Decode<SerializeObject>("A=Taco&B=Legume");


      Assert.Equal(expected, obj);
    }
  }
}
