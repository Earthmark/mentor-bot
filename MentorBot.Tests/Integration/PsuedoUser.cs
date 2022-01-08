using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Tests.Integration
{
  public class PsuedoUser : IDisposable
  {
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _stopToken = new();
    private readonly Func<string, Task> _sender;

    public PsuedoUser(WebSocket socket)
    {
      _socket = socket;
      _sender = _socket.RawMessageSender(_stopToken.Token);
    }

    public async Task SendAsync(string message)
    {
      await _sender(message);
    }

    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(string? ticketId = null)
    {
      var cancelToken = new CancellationTokenSource(100);
      var trueCancel = CancellationTokenSource.CreateLinkedTokenSource(cancelToken.Token, _stopToken.Token);

      await foreach (var msg in _socket.ReadRawMessages(_stopToken.Token).WithCancellation(trueCancel.Token))
      {
        var values = UrlEncoder.Decode<Dictionary<string, string>>(msg);
        if (ticketId == null || values["ticket"] == ticketId)
        {
          return values;
        }
      }
      throw new InvalidOperationException("Mentor disconnected before connection was complete");
    }

    public void Dispose()
    {
      _stopToken.Cancel();
      _socket.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None).Wait();
    }
  }

  public static class WebSocketClientExtensions
  {
    public static async Task<PsuedoUser> BindUser(this WebSocketClient client, string route)
    {
      return new PsuedoUser(await client.ConnectAsync(
        new Uri($"http://locahost{route}"), CancellationToken.None));
    }
  }
}
