// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace StreamJsonRpc
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using Microsoft;
    using StreamJsonRpc.Protocol;

    public class Utf8JsonMessageFormatter : IJsonRpcMessageTextFormatter
    {
        /// <summary>
        /// UTF-8 encoding without a preamble.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonMessageFormatter"/> class
        /// that uses <see cref="Encoding.UTF8"/> (without the preamble) for its text encoding.
        /// </summary>
        public Utf8JsonMessageFormatter()
            : this(DefaultEncoding)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonMessageFormatter"/> class.
        /// </summary>
        /// <param name="encoding">The encoding to use for the JSON text.</param>
        public Utf8JsonMessageFormatter(Encoding encoding)
        {
            Requires.NotNull(encoding, nameof(encoding));
            this.Encoding = encoding;
        }

        /// <summary>
        /// Gets or sets the encoding to use for transmitted messages.
        /// </summary>
        public Encoding Encoding
        {
            get => DefaultEncoding;
            set => ThrowIfNotUtf8Encoding(value);
        }

        /// <inheritdoc/>
        public JsonRpcMessage Deserialize(ReadOnlySequence<byte> contentBuffer, Encoding encoding)
        {
            ThrowIfNotUtf8Encoding(encoding);

            var reader = new Utf8JsonReader(contentBuffer);
            return JsonSerializer.Deserialize<JsonRpcMessage>(ref reader);
        }

        /// <inheritdoc/>
        public JsonRpcMessage Deserialize(ReadOnlySequence<byte> contentBuffer) => this.Deserialize(contentBuffer, this.Encoding);

        /// <inheritdoc/>
        public void Serialize(IBufferWriter<byte> bufferWriter, JsonRpcMessage message)
        {
            var writer = new Utf8JsonWriter(bufferWriter);
            JsonSerializer.Serialize(writer, message);
        }

        /// <inheritdoc/>
        public object GetJsonText(JsonRpcMessage message) => JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });

        private static void ThrowIfNotUtf8Encoding(Encoding value)
        {
            if (value?.EncodingName != DefaultEncoding.EncodingName)
            {
                throw new NotSupportedException();
            }
        }
    }
}
