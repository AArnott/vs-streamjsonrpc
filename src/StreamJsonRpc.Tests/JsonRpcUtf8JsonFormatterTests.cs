// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using StreamJsonRpc;
using Xunit.Abstractions;

public class JsonRpcUtf8JsonFormatterTests : JsonRpcTests
{
    public JsonRpcUtf8JsonFormatterTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    protected override void InitializeFormattersAndHandlers()
    {
        this.serverMessageFormatter = new Utf8JsonMessageFormatter();
        this.clientMessageFormatter = new Utf8JsonMessageFormatter();

        this.serverMessageHandler = new LengthHeaderMessageHandler(this.serverStream, this.serverStream, this.serverMessageFormatter);
        this.clientMessageHandler = new LengthHeaderMessageHandler(this.clientStream, this.clientStream, this.clientMessageFormatter);
    }
}
