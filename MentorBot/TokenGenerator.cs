using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace MentorBot;

public interface ITokenGenerator
{
    public string CreateToken();
}

public class TokenGenerator : ITokenGenerator
{
    public string CreateToken()
    {
        return Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(45));
    }
}