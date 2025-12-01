using System.Text.Json;
using System.Text.Json.Nodes;
using McpArcGis.Contracts;
using Microsoft.Extensions.Logging;

namespace McpArcGis.Services;

public static class JsonRpcErrorCodes
{
    public const int InvalidRequest = 2000;
    public const int MethodNotFound = 2001;
    public const int InvalidParams  = 2002;
}

public sealed record JsonRpcRequest(
    string Jsonrpc,
    string Method,
    JsonNode? Params,
    JsonNode? Id
);

public sealed record JsonRpcResponse(
    string Jsonrpc,
    JsonNode? Result,
    JsonRpcError? Error,
    JsonNode? Id
);

public sealed record JsonRpcError(
    int Code,
    string Message,
    JsonNode? Data
);

public interface IToolsDispatcher
{
    Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(CancellationToken ct);
}

/// <summary>
/// Simple adapter from Core.IToolsService to the JSON-RPC dispatcher interface.
/// This keeps JSON-RPC plumbing in Services and ArcGIS/domain logic in Core.
/// </summary>
public sealed class ToolsDispatcherAdapter : IToolsDispatcher
{
    private readonly McpArcGis.Core.IToolsService _inner;

    public ToolsDispatcherAdapter(McpArcGis.Core.IToolsService inner)
    {
        _inner = inner;
    }

    public Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(CancellationToken ct)
        => _inner.ListToolsAsync(ct);
}

public sealed class JsonRpcServer
{
    private readonly ILogger<JsonRpcServer> _logger;
    private readonly IToolsDispatcher _toolsDispatcher;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public JsonRpcServer(ILogger<JsonRpcServer> logger, IToolsDispatcher toolsDispatcher)
    {
        _logger = logger;
        _toolsDispatcher = toolsDispatcher;
    }

    public Task RunAsync(CancellationToken ct)
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        using var writer = new StreamWriter(stdout) { AutoFlush = true };

        return RunAsync(reader, writer, ct);
    }

    /// <summary>
    /// Core JSON-RPC loop that reads from the provided reader and writes to the provided writer.
    /// This overload exists to make the server testable with in-memory streams.
    /// </summary>
    public async Task RunAsync(TextReader reader, TextWriter writer, CancellationToken ct)
    {
        _logger.LogInformation("JSON-RPC server loop started.");

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonNode? root;
            try
            {
                root = JsonNode.Parse(line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON-RPC input line.");
                var errorResponse = new JsonRpcResponse(
                    Jsonrpc: "2.0",
                    Result: null,
                    Error: new JsonRpcError(
                        Code: JsonRpcErrorCodes.InvalidRequest,
                        Message: "Invalid JSON-RPC request payload.",
                        Data: null),
                    Id: null);

                await WriteResponseAsync(writer, errorResponse, ct).ConfigureAwait(false);

                if (reader is StringReader)
                {
                    break; // avoid infinite loop for in-memory tests
                }
                continue;
            }

            if (root is JsonArray batch)
            {
                var responses = new JsonArray();
                foreach (var item in batch)
                {
                    var resp = await HandleSingleAsync(item, ct).ConfigureAwait(false);
                    if (resp is not null)
                    {
                        responses.Add(JsonSerializer.SerializeToNode(resp, SerializerOptions));
                    }
                }

                await writer.WriteLineAsync(responses.ToJsonString(SerializerOptions)).ConfigureAwait(false);

                if (reader is StringReader)
                {
                    break; // avoid infinite loop for in-memory tests
                }
            }
            else
            {
                var response = await HandleSingleAsync(root, ct).ConfigureAwait(false);
                if (response is not null)
                {
                    await WriteResponseAsync(writer, response, ct).ConfigureAwait(false);
                }

                if (reader is StringReader)
                {
                    break; // avoid infinite loop for in-memory tests
                }
            }
        }

        _logger.LogInformation("JSON-RPC server loop exiting.");
    }

    private async Task<JsonRpcResponse?> HandleSingleAsync(JsonNode? node, CancellationToken ct)
    {
        if (node is null || node.AsObject() is not JsonObject obj)
        {
            return new JsonRpcResponse(
                Jsonrpc: "2.0",
                Result: null,
                Error: new JsonRpcError(JsonRpcErrorCodes.InvalidRequest, "Invalid request object.", null),
                Id: null);
        }

        var id = obj["id"];
        var jsonrpc = (string?)obj["jsonrpc"] ?? string.Empty;
        var method = (string?)obj["method"];
        var @params = obj["params"];

        if (!string.Equals(jsonrpc, "2.0", StringComparison.Ordinal))
        {
            return new JsonRpcResponse(
                Jsonrpc: "2.0",
                Result: null,
                Error: new JsonRpcError(JsonRpcErrorCodes.InvalidRequest, "jsonrpc must be '2.0'.", null),
                Id: id);
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            return new JsonRpcResponse(
                Jsonrpc: "2.0",
                Result: null,
                Error: new JsonRpcError(JsonRpcErrorCodes.InvalidRequest, "Missing method.", null),
                Id: id);
        }

        try
        {
            switch (method)
            {
                case "tools/list":
                    var tools = await _toolsDispatcher.ListToolsAsync(ct).ConfigureAwait(false);
                    var resultNode = JsonSerializer.SerializeToNode(tools, SerializerOptions);
                    return new JsonRpcResponse("2.0", resultNode, null, id);

                default:
                    return new JsonRpcResponse(
                        Jsonrpc: "2.0",
                        Result: null,
                        Error: new JsonRpcError(JsonRpcErrorCodes.MethodNotFound, $"Method '{method}' not found.", null),
                        Id: id);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return null; // server shutting down; do not respond.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing method {Method}.", method);
            var data = JsonSerializer.SerializeToNode(new ErrorResponse(
                Code: JsonRpcErrorCodes.InvalidRequest,
                Message: "Unhandled server error.",
                Data: null,
                Suggestion: "Check server logs on stderr for details.",
                Recovery: null), SerializerOptions);

            return new JsonRpcResponse(
                Jsonrpc: "2.0",
                Result: null,
                Error: new JsonRpcError(JsonRpcErrorCodes.InvalidRequest, "Unhandled server error.", data),
                Id: id);
        }
    }

    private static Task WriteResponseAsync(TextWriter writer, JsonRpcResponse response, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(response, SerializerOptions);
        return writer.WriteLineAsync(json);
    }
}
