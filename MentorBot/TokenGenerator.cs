using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace MentorBot
{
  public interface ITokenGenerator
  {
    public string CreateToken();
  }

  public class TokenGenerator : ITokenGenerator
  {
    public string CreateToken()
    {
      return Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(80));
    }
  }
}
