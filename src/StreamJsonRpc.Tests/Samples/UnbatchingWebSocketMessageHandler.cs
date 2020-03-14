#nullable enable

#if NETCOREAPP

namespace StreamJsonRpc.Sample.Client
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft;
    using Nerdbank.Streams;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using StreamJsonRpc.Protocol;
    using StreamJsonRpc.Reflection;

    /// <summary>
    /// This is a specialized web socket message handler that supports *receiving* JSON-RPC messages batched in an array.
    /// </summary>
    public class UnbatchingWebSocketMessageHandler : WebSocketMessageHandler
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        private static readonly byte JsonArrayFirstByte = Encoding.GetBytes("[").Single();
        private readonly Queue<JsonRpcMessage> pendingMessages = new Queue<JsonRpcMessage>();
        private readonly int sizeHint;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnbatchingWebSocketMessageHandler"/> class
        /// that uses the <see cref="JsonMessageFormatter"/> to serialize messages as textual JSON.
        /// </summary>
        /// <inheritdoc cref="WebSocketMessageHandler(WebSocket)"/>
        public UnbatchingWebSocketMessageHandler(WebSocket webSocket)
            : base(webSocket, new JsonMessageFormatter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnbatchingWebSocketMessageHandler"/> class.
        /// </summary>
        /// <inheritdoc cref="WebSocketMessageHandler(WebSocket, IJsonRpcMessageFormatter, int)"/>
        public UnbatchingWebSocketMessageHandler(WebSocket webSocket, IJsonRpcMessageTextFormatter formatter, int sizeHint = 4096)
            : base(webSocket, formatter, sizeHint)
        {
            Requires.Argument(formatter.Encoding is UTF8Encoding, nameof(formatter), "Only UTF-8 encoding is supported.");
            this.sizeHint = sizeHint;
        }

        /// <inheritdoc />
        protected override async ValueTask<JsonRpcMessage?> ReadCoreAsync(CancellationToken cancellationToken)
        {
            if (this.pendingMessages.Count > 0)
            {
                return this.pendingMessages.Dequeue();
            }

            using (var contentSequenceBuilder = new Sequence<byte>())
            {
                ValueWebSocketReceiveResult result;
                do
                {
                    Memory<byte> memory = contentSequenceBuilder.GetMemory(this.sizeHint);
                    result = await this.WebSocket.ReceiveAsync(memory, cancellationToken).ConfigureAwait(false);
                    contentSequenceBuilder.Advance(result.Count);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await this.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed as requested.", CancellationToken.None).ConfigureAwait(false);
                        return null;
                    }
                }
                while (!result.EndOfMessage);

                if (contentSequenceBuilder.AsReadOnlySequence.Length == 0)
                {
                    return null;
                }

                var sequence = contentSequenceBuilder.AsReadOnlySequence;
                if (sequence.First.Span[0] == JsonArrayFirstByte)
                {
                    // This is a JSON array of JSON-RPC messages.
                    // The formatter doesn't support this, so break them up first.
                    using (var reader = new JsonTextReader(new SequenceTextReader(sequence, Encoding)))
                    {
                        var array = JArray.Load(reader);
                        if (array.Count == 0)
                        {
                            // An empty array? I hope the server never sends that.
                            throw new NotSupportedException("An empty JSON array was received.");
                        }

                        foreach (JToken messageJson in array.Children())
                        {
                            byte[] encodedMessage = Encoding.GetBytes(messageJson.ToString());
                            this.pendingMessages.Enqueue(this.Formatter.Deserialize(new ReadOnlySequence<byte>(encodedMessage)));
                        }
                    }

                    return this.pendingMessages.Dequeue();
                }

                return this.Formatter.Deserialize(sequence);
            }
        }
    }
}

#endif
