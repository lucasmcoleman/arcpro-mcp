# Dev Notes

This file tracks implementation progress and next steps for the MCP ArcGIS Pro server.

## Status (2025-12-01)

### Done

- Created .NET 8 solution `MCP.ArcGIS.Pro.sln`.
- Projects:
  - `src/Contracts` – shared DTOs for tools, model structure, execution results, and error responses.
  - `src/Core` – now contains `IToolsService` and a `MockToolsService` that returns a hard-coded mock "Buffer" model for development.
  - `src/Services` – now contains JSON-RPC 2.0 models (`JsonRpcRequest`, `JsonRpcResponse`, `JsonRpcError`), error codes, a `JsonRpcServer` loop, and a `ToolsDispatcherAdapter` that bridges to `Core.IToolsService`.
  - `src/MCP.Server` – console app that wires logging, `MockToolsService`, `ToolsDispatcherAdapter`, and `JsonRpcServer` together.
  - `tests/UnitTests`, `tests/IntegrationTests` – xUnit test projects (no tests implemented yet).
- Implemented core contracts in `src/Contracts/Class1.cs` (namespace `McpArcGis.Contracts`).
- Wired `MCP.Server` to reference `Contracts`, `Core`, and `Services`.
- Initialized git repo and added a .NET `.gitignore`.
- Added top-level `README.md`.
- Implemented a minimal JSON-RPC loop that reads one JSON request (or batch) per line from stdin and writes JSON-RPC responses to stdout.
- Implemented `tools/list` method that returns the mock tools list from `MockToolsService`.

### In progress / next steps

1. Implement a minimal JSON-RPC 2.0 loop in `src/Services` and wire it from `src/MCP.Server`:
   - Parse single and batch requests from stdin (temporary line-based framing for development).
   - Implement basic validation (`jsonrpc == "2.0"`, `method` present, `id` optional for notifications).
   - Map protocol errors to codes: `2000` (Invalid Request), `2001` (Method Not Found), `2002` (Invalid Params).
   - Ensure all logs go to stderr only.
2. Implement a mock `IToolsService` in `Core` that returns hard-coded `ToolDefinition` objects for `tools/list`.
3. Add `tools/list` handler in the JSON-RPC dispatch layer, calling `IToolsService`.
4. Add unit tests in `tests/UnitTests` for:
   - JSON-RPC request/response models.
   - `tools/list` happy path and error conditions (unknown method, invalid params).
5. After JSON-RPC is stable, design IPC contract and stubs for Named Pipes between MCP.Server and the future ArcGIS Pro add-in.
6. Later phases (not started):
   - Implement real `.tbx` parsing in `Core`.
   - Implement ArcGIS Pro Add-In project and IPC integration.
   - Expand to `tools/get`, `tools/validate`, `tools/call`, and diagnostics endpoints.

## Notes for future contributors/agents

- **Do not commit on behalf of the user** unless they explicitly request it. The repo is initialized but has no commits yet.
- For JSON-RPC framing, we are starting with a simple line-based protocol for development. Proper `Content-Length` framed messages (per MCP best practices) should be implemented before calling this production-ready.
- Keep all console logs on `stderr`. `stdout` must only contain JSON-RPC responses.
- When adding ArcGIS-specific code, keep it isolated from `Contracts` and (ideally) from `Services` to maintain testability.
