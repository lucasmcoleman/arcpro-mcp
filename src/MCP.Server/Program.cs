using McpArcGis.Contracts;
using McpArcGis.Core;
using McpArcGis.Services;
using Microsoft.Extensions.Logging;

// Configure logging to write via ILogger (we'll keep stdout for JSON-RPC only).
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger("Main");
logger.LogInformation("MCP.Server starting (JSON-RPC over stdio, tools/list stub).");

// Wire up services manually for now.
IToolsService toolsService = new MockToolsService();
IToolsDispatcher dispatcher = new ToolsDispatcherAdapter(toolsService);
var rpcLogger = loggerFactory.CreateLogger<JsonRpcServer>();
var server = new JsonRpcServer(rpcLogger, dispatcher);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await server.RunAsync(cts.Token);
