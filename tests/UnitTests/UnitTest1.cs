using System.Text.Json.Nodes;
using McpArcGis.Contracts;
using McpArcGis.Core;
using McpArcGis.Services;
using Microsoft.Extensions.Logging;

namespace UnitTests;

public class JsonRpcServerTests
{
    private static JsonRpcServer CreateServer()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<JsonRpcServer>();

        IToolsService toolsService = new MockToolsService();
        IToolsDispatcher dispatcher = new ToolsDispatcherAdapter(toolsService);

        return new JsonRpcServer(logger, dispatcher);
    }

    [Fact]
    public async Task ToolsList_ReturnsMockToolDefinition()
    {
        // Arrange
        var server = CreateServer();
        using var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}\n");
        using var output = new StringWriter();

        // Act
        await server.RunAsync(input, output, CancellationToken.None);

        // Assert
        var line = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Single();
        var node = JsonNode.Parse(line)!.AsObject();

        Assert.Equal("2.0", (string?)node["jsonrpc"]);
        Assert.Null(node["error"]);

        var result = node["result"]!.AsArray();
        var first = result[0]!.AsObject();
        Assert.Equal("mock-buffer-model", (string?)first["id"]);
        Assert.Equal(SchemaInfo.CurrentVersion, (string?)first["schemaVersion"]);
    }

    [Fact]
    public async Task InvalidJson_ProducesInvalidRequestError()
    {
        // Arrange
        var server = CreateServer();
        using var input = new StringReader("{not valid json}\n");
        using var output = new StringWriter();

        // Act
        await server.RunAsync(input, output, CancellationToken.None);

        // Assert
        var line = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Single();
        var node = JsonNode.Parse(line)!.AsObject();
        var error = node["error"]!.AsObject();

        Assert.Equal(JsonRpcErrorCodes.InvalidRequest, (int?)error["code"]);
    }
}
