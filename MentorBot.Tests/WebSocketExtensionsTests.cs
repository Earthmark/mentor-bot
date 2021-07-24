using Moq;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MentorBot.Tests
{
  public class WebSocketExtensionsTests
  {
    public WebSocket CreateDataSocket(params string[] messages)
    {
      var socket = new Mock<WebSocket>();

      void MockReentrant(string[] remaining)
      {
        if (remaining.Length != 0)
        {
          var data = Encoding.UTF8.GetBytes(remaining[0]);
          var next = remaining[1..];

          socket.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
           .Callback<ArraySegment<byte>, CancellationToken>((b, c) =>
           {
             c.ThrowIfCancellationRequested();
             data.AsMemory().CopyTo(b.AsMemory());
             MockReentrant(next);
           })
           .Returns(() => Task.FromResult(new WebSocketReceiveResult(data.Length, WebSocketMessageType.Text, true)));
        }
        else
        {
          socket.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
           .Returns(() => Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)));
        }
      }

      MockReentrant(messages);

      return socket.Object;
    }

    [Fact(Timeout = 1)]
    public async Task ReportsMessages()
    {
      var expected = new []{ "a", "b", "c"};
      var socket = CreateDataSocket(expected);

      var items = new List<string>();
      await foreach(var item in socket.ReadMessages())
      {
        items.Add(item);
      }

      Assert.Equal(expected, items);
    }
  }
}
