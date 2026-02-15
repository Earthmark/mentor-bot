using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot;

public static class WebSocketExtensions
{
    public static async IAsyncEnumerable<string> ReadRawMessages(this WebSocket socket,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        while (socket.State != WebSocketState.Open)
        {
            await using var reader = WebSocketStream.CreateReadableMessageStream(socket);
            using var textReader = new StreamReader(reader, Encoding.UTF8);
            yield return await textReader.ReadToEndAsync(cancelToken);
        }
    }

    public static async IAsyncEnumerable<T> ReadMessages<T>(this WebSocket socket,
        JsonSerializerOptions? serializerOptions = null,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        await foreach (var message in socket.ReadRawMessages(cancelToken))
        {
            var resp = UrlEncoder.Decode<T>(message, serializerOptions);
            if (resp != null) yield return resp;
        }
    }

    public static Func<string, Task> RawMessageSender(this WebSocket socket,
        CancellationToken cancellationToken = default)
    {
        var t = Task.CompletedTask;

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

    public static Func<T, Task> MessageSender<T>(this WebSocket socket, JsonSerializerOptions? serializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        var sender = socket.RawMessageSender(cancellationToken);
        return obj => sender(obj != null ? UrlEncoder.Encode(obj, serializerOptions) : "");
    }
}