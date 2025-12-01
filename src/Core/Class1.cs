using McpArcGis.Contracts;

namespace McpArcGis.Core;

public interface IToolsService
{
    Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(CancellationToken ct);
}

/// <summary>
/// Temporary mock implementation that returns hard-coded tools for development and testing.
/// Replace with real toolbox/model discovery in the ArcGIS Pro integration phase.
/// </summary>
public sealed class MockToolsService : IToolsService
{
    public Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(CancellationToken ct)
    {
        var tools = new List<ToolDefinition>
        {
            new(
                SchemaVersion: SchemaInfo.CurrentVersion,
                Id: "mock-buffer-model",
                Name: "Mock Buffer Model",
                Path: "C:/mock/Buffer.tbx",
                Category: "Mock/Geometry",
                Description: "A mock buffer model used for JSON-RPC plumbing tests.",
                LastModified: DateTimeOffset.UtcNow,
                Dependencies: new List<string> { "Buffer" },
                Inputs: new List<ToolParameter>
                {
                    new(
                        Name: "InputFeatures",
                        DisplayName: "Input Features",
                        DataType: "feature_layer",
                        IsRequired: true,
                        DefaultValue: null,
                        Validation: null)
                },
                Outputs: new List<ToolParameter>
                {
                    new(
                        Name: "OutputFeatures",
                        DisplayName: "Output Features",
                        DataType: "feature_layer",
                        IsRequired: true,
                        DefaultValue: null,
                        Validation: null)
                })
        };

        return Task.FromResult<IReadOnlyList<ToolDefinition>>(tools);
    }
}
