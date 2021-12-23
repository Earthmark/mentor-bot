using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Extern
{
  public interface INeosApiAuthKeeper
  {
    ValueTask<string> GetOrRefreshToken(Func<Task<(string token, DateTime expiry)>> tokenGetter, CancellationToken cancellationToken = default);
  }

  public class NeosApiAuthKeeper : INeosApiAuthKeeper
  {
    private readonly object _refreshLock = new();

    private volatile Task<(string token, DateTime expiry)> _lastToken = Task.FromResult(("", DateTime.MinValue));

    public async ValueTask<string> GetOrRefreshToken(Func<Task<(string token, DateTime expiry)>> tokenGetter, CancellationToken cancellationToken)
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var lastTask = _lastToken;
        var (token, expiry) = await lastTask;

        if (expiry >= DateTime.UtcNow + TimeSpan.FromMinutes(3))
        {
          return token;
        }

        lock (_refreshLock)
        {
          if (_lastToken == lastTask)
          {
            _lastToken = tokenGetter();
          }
        }
      }
      cancellationToken.ThrowIfCancellationRequested();
      throw new InvalidOperationException("This should not be reachable.");
    }
  }
}
