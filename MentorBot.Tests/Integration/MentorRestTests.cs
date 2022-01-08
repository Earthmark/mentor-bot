using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace MentorBot.Tests.Integration
{
  [Collection("Integration collection")]
  public class MentorRestTests : IClassFixture<SignalWebApplicationFactory>
  {
    private readonly HttpClient _client;

    public MentorRestTests(SignalWebApplicationFactory factory)
    {
      _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTicketCreatesExpectedRecord()
    {
      var response = await _client.GetAsync("/mentor");
      response.EnsureSuccessStatusCode();
      var body = await response.Content.ReadAsStringAsync();
    }
  }
}
