# MCP ArcGIS Pro Server

Model Context Protocol (MCP) server for ArcGIS Pro ModelBuilder. Provides JSON-RPC 2.0 over stdio for AI assistants, with a backend ArcGIS Pro add-in for model discovery, validation, and execution.

## Build

```bash
dotnet build MCP.ArcGIS.Pro.sln
```

## Run (development)

```bash
dotnet run --project src/MCP.Server/MCP.Server.csproj
```

> NOTE: JSON-RPC and ArcGIS-specific behavior are under active development. See `docs/DEV_NOTES.md` for current status and TODOs.
