using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot
{
  public static class WebSocketExtensions
  {
    public static async IAsyncEnumerable<string> ReadMessages(this WebSocket socket, [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
      var buffer = new byte[1024 * 4];
      WebSocketReceiveResult result;
      do
      {
        using var memStream = new MemoryStream();
        do
        {
          result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
          memStream.Write(buffer, 0, result.Count);
        } while (result.MessageType != WebSocketMessageType.Close && !result.EndOfMessage);
        if (result.MessageType != WebSocketMessageType.Close)
        {
          yield return Encoding.UTF8.GetString(memStream.GetBuffer(), 0, (int)memStream.Length);
        }
      } while (result.MessageType != WebSocketMessageType.Close);
    }

    public static Func<string, Task> MessageSender(this WebSocket socket, CancellationToken cancellationToken = default)
    {
      Task t = Task.CompletedTask;

      async Task NextSequenced(Func<Task> action)
      {
        var tcs = new TaskCompletionSource();
        try
        {
          await Interlocked.Exchange(ref t, tcs.Task);

          await action();
        }
        finally
        {
          tcs.SetResult();
        }
      }

      return msg => NextSequenced(() =>
        socket.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, cancellationToken));
    }
  }
}
