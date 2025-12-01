using System.Text.Json.Serialization;

namespace McpArcGis.Contracts;

public static class SchemaInfo
{
    public const string CurrentVersion = "1.0.0";
}

public sealed record ToolDefinition(
    string SchemaVersion,
    string Id,
    string Name,
    string Path,
    string Category,
    string Description,
    DateTimeOffset LastModified,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<ToolParameter> Inputs,
    IReadOnlyList<ToolParameter> Outputs
);

public sealed record ToolParameter(
    string Name,
    string DisplayName,
    string DataType,
    bool IsRequired,
    object? DefaultValue,
    ParameterValidation? Validation
);

public sealed record ParameterValidation(
    double? Min,
    double? Max,
    int? MinLength,
    int? MaxLength,
    string? Pattern,
    IReadOnlyList<object>? Enum,
    string? BusinessRuleDescription
);

public sealed record ModelStructure(
    string SchemaVersion,
    string ToolId,
    IReadOnlyList<ModelNode> Nodes,
    IReadOnlyList<ModelEdge> Edges,
    IReadOnlyList<ModelParameterBinding> Parameters,
    ModelPerformanceProfile? Performance
);

public sealed record ModelNode(
    string NodeId,
    string ToolName,
    string Category,
    IReadOnlyList<ToolParameter> Inputs,
    IReadOnlyList<ToolParameter> Outputs
);

public sealed record ModelEdge(
    string FromNodeId,
    string FromOutputName,
    string ToNodeId,
    string ToInputName,
    string? TransformationRule
);

public sealed record ModelParameterBinding(
    string PublicParameterName,
    string NodeId,
    string NodeParameterName
);

public sealed record ModelPerformanceProfile(
    TimeSpan? LastDuration,
    IReadOnlyDictionary<string, TimeSpan>? PerNodeDurations
);

public sealed record ExecutionResult(
    string SchemaVersion,
    string ToolId,
    bool Success,
    string Status,
    IReadOnlyList<ExecutionMessage> Messages,
    IReadOnlyList<ExecutionOutput> Outputs,
    ExecutionStatistics Stats
);

public sealed record ExecutionMessage(
    string Category,
    string Code,
    string Message,
    string? ToolName,
    string? ParameterName,
    string? Path
);

public sealed record ExecutionOutput(
    string Name,
    string DataType,
    string Path,
    long? SizeBytes,
    string? Format,
    string? SpatialReference
);

public sealed record ExecutionStatistics(
    TimeSpan Duration,
    long? PeakWorkingSetBytes,
    IReadOnlyDictionary<string, TimeSpan>? PerNodeDurations
);

public sealed record ErrorResponse(
    int Code,
    string Message,
    ErrorContext? Data,
    string? Suggestion,
    string? Recovery
);

public sealed record ErrorContext(
    string? ToolId,
    string? ToolName,
    string? ParameterName,
    string? ExpectedFormat,
    string? FilePath,
    string? SystemState,
    string? CorrelationId
);
