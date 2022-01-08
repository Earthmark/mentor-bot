using Xunit;

namespace MentorBot.Tests.Integration
{
  [CollectionDefinition("Integration collection")]
  public class IntegrationFixtureCollection : ICollectionFixture<SignalWebApplicationFactory>
  {
  }
}
