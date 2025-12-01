# MCP Server for ArcGIS Pro ModelBuilder - Enhanced Implementation Directive

## Mission
Design and implement a robust, extensible **Model Context Protocol (MCP) Server** that enables AI assistants to discover, analyze, execute, debug, and edit ArcGIS Pro ModelBuilder workflows using natural language. The system must act as a secure, reliable bridge between AI clients and ArcGIS Pro's internal geoprocessing engine, abstracting complexity while preserving complete fidelity of model behavior.

---

## Core Requirements

### System Components (Modular & Interoperable)
You must develop three tightly integrated, independently deployable components:

1. **Shared Contracts Library**
   - Define common data contracts, interfaces, and error types using C# records and interfaces with strict typing.
   - Include strongly-typed models for:
     - `ToolDefinition` (name, description, inputs, outputs, category, validation rules)
     - `ModelStructure` (nodes, edges, parameters, execution state, dependencies)
     - `ExecutionResult` (success, messages, warnings, logs, output data paths, performance metrics)
     - `ErrorResponse` (code, message, context, suggestion, recovery actions)
   - Use `System.Text.Json` serialization with explicit naming policies and null-safe handling.
   - Version the contracts with a `SchemaVersion` field for backward compatibility.

2. **MCP Server**
   - A standalone console application built on .NET 8.0 with production-grade error handling.
   - Implements **JSON-RPC 2.0** over `stdin/stdout` with complete protocol compliance and validation.
   - Acts as a protocol adapter, routing requests to the ArcGIS Pro Add-In via IPC with request/response correlation.
   - All logging goes exclusively to `stderr` (never `stdout`) with structured logging format.
   - Supports asynchronous method calls, batch requests, and concurrent operations.
   - Gracefully handles malformed, incomplete, or oversized messages with detailed error reporting.

3. **ArcGIS Pro Add-In**
   - A fully functional extension for ArcGIS Pro SDK 3.5+ with complete lifecycle management.
   - Auto-loads on startup and unloads gracefully during shutdown with resource cleanup.
   - Respects all threading constraints (e.g., UI thread access via `Dispatcher`) with deadlock prevention.
   - Communicates with the MCP Server via a reliable IPC mechanism with connection pooling (see below).
   - Exposes geoprocessing capabilities through a well-defined API with comprehensive error handling.

---

## Functional Capabilities (Structured & Prioritized)

### **Model Discovery & Analysis** *(MVP Focus)*
- `tools/list` – Retrieve all available ModelBuilder models from a project or toolbox with metadata enrichment.
  - Return list of `ToolDefinition` objects with metadata: name, path, category, description, last modified, dependencies.
  - Support advanced filtering by project, toolbox, name pattern, category, or modification date.
- `tools/get` – Retrieve complete model structure with comprehensive analysis:
  - List all geoprocessing tools (nodes), their parameters, data types, and validation constraints.
  - Map connections (edges) between tools with data flow direction and transformation rules.
  - Extract input/output relationships, dependencies, and execution order requirements.
  - Return structured data with `ModelStructure` object including performance characteristics.
- `tools/validate` – Perform comprehensive model configuration validation:
  - Check required parameters, data types, value ranges, and business rule constraints.
  - Detect missing data sources, invalid file paths, or permission issues.
  - Return prioritized, actionable feedback with specific remediation steps.

### **Model Execution** *(MVP Focus)*
- `tools/call` – Execute a model with specified parameters and comprehensive monitoring.
  - Accept a `ModelId` and a strongly-typed dictionary of parameter values with validation.
  - Execute synchronously or asynchronously (via callback) with cancellation support.
  - Monitor progress in real-time via `progress` events with granular status (0% → 100%, step-by-step).
  - Capture and relay all geoprocessing messages, warnings, errors, and performance metrics.
  - Support cancellation via `cancel` signal with cleanup guarantee.
  - Return `ExecutionResult` with:
    - Success/failure status with detailed classification
    - Categorized list of messages (info, warning, error, debug)
    - Output file paths with metadata (size, format, spatial reference)
    - Generated data with serialization options (if applicable)
    - Execution statistics (duration, memory usage, tool performance)

### **Diagnostics & Debugging** *(Critical for AI UX)*
- `diagnostics/trace` – Perform comprehensive data flow tracing through the model.
  - Show complete input → process → output lineage for specified datasets.
  - Identify performance bottlenecks, data transformation issues, or validation failures.
- `diagnostics/failure` – Conduct detailed execution failure analysis:
  - Parse error logs and map them to specific tools with context preservation.
  - Suggest prioritized fixes: e.g., "Missing input data for 'Clip' tool — verify path exists: C:\data\input.shp".
  - Provide recovery strategies and alternative approaches.
- `diagnostics/validate-params` – Perform pre-execution parameter validation with detailed feedback:
  - Ensure correct data type, value range, format compliance, and business rules.
  - Flag invalid, out-of-bounds, or potentially problematic values with explanations.
  - Suggest corrections and provide examples of valid inputs.

### **Model Editing** *(Advanced, Optional but Valued)*
- `tools/edit/add` – Add a new geoprocessing tool to the model with validation.
  - Specify tool name, parameters, target location, and connection requirements.
- `tools/edit/update` – Modify existing tool parameters with impact analysis.
- `tools/edit/structure` – Reconnect tools or reorganize layout with dependency validation (if possible).
- (Optional) `tools/suggest` – Provide AI-driven model optimization recommendations:
  - Optimize execution order based on data dependencies and performance characteristics.
  - Suggest iterators for repetitive tasks with efficiency improvements.
  - Recommend optimal data formats, tools, or parameter configurations.

---

## Communication Architecture (Explicit & Secure)

### **AI Client ↔ MCP Server**
- Protocol: **JSON-RPC 2.0** over `stdin/stdout` with strict compliance validation.
- Use `Content-Type: application/json` and `Content-Length` headers with encoding specification.
- Support:
  - `method`, `params`, `id` with type validation
  - `error` objects with `code`, `message`, `data` following standard error codes
  - Batch requests with transaction semantics
- All logs go to `stderr` only with structured format and log levels.
- Handle protocol violations with specific error codes: `2000` (Invalid Request), `2001` (Method Not Found), `2002` (Invalid Params).

### **MCP Server ↔ ArcGIS Pro Add-In**
- Implement **Named Pipes** or **gRPC over TCP** for bidirectional, reliable IPC with authentication.
  - Use `System.IO.Pipes.NamedPipeServerStream` for .NET 8.0 with security settings.
  - Or use gRPC with `Grpc.Net.Client` for cross-platform support with compression.
- Ensure:
  - Concurrent request handling (async/await) with proper resource management
  - Automatic reconnection on disconnection with exponential backoff
  - Timeout handling (configurable, default 30 seconds) with graceful degradation
  - Message buffering, queuing, and priority handling
  - Request correlation and response matching

---

## Critical Technical Constraints (Prioritized & Actionable)

### **ArcGIS Pro SDK Requirements**
- Target **ArcGIS Pro SDK 3.5** or newer with version compatibility checking.
- **Never access UI elements from background threads** - enforce through static analysis.
- Use `Dispatcher.InvokeAsync` for any UI interaction with proper exception handling.
- Auto-load Add-In via manifest configuration with dependency declaration.
- Handle `Application.Quit` event to clean up resources with timeout protection.

### **ModelBuilder Access (Key Integration Challenge)**
- **No public DOM API** exists for direct ModelBuilder manipulation - documented limitation.
- **Solution Strategy:**
  - Parse `.tbx` (toolbox) files as ZIP archives with integrity validation.
  - Extract and parse XML inside `Model` subdirectories (e.g., `Model.xml`) with schema validation.
  - Use `XDocument` and `XElement` to traverse the model structure with error handling.
  - Use **Python script tools** (via `arcpy` or `arcpy.mp`) as fallback for dynamic model creation/editing.
  - Consider embedding a lightweight Python interpreter via `Python.Runtime` with sandboxing if needed.
  - Document all assumptions, limitations, and workarounds with version dependencies clearly.

### **MCP Protocol Compliance**
- Implement complete **JSON-RPC 2.0** specification with strict validation:
  - Correct `error` object format with standard error codes and extensions.
  - Support `id` field for request/response matching with type preservation.
  - Handle `null` values and `optional` parameters with explicit validation.
- Define `ToolSchema` with comprehensive validation:
  - Input/Output types (e.g., `string`, `feature_layer`, `raster_dataset`) with format specifications
  - Validation rules (e.g., `minLength`, `maxLength`, `pattern`, `enum`, `range`) with custom validators
  - Default values with type safety and business rule compliance
- Return standardized `ErrorResponse` with actionable information:
  - `code`: e.g., `1001` (InvalidParameter), `1002` (ModelNotFound), `1003` (ExecutionFailed), `1004` (ResourceUnavailable)
  - `message`: clear, user-friendly description with context
  - `data`: structured contextual details (file path, tool name, parameter name, expected format)
  - `suggestion`: specific, actionable fix with examples (e.g., "Check if the input file exists and is accessible")
  - `recovery`: automated recovery options when available

### **Error Handling (Must Be Actionable)**
- Never expose raw exceptions (e.g., `NullReferenceException`, `COMException`) - always wrap with context.
- Wrap all ArcGIS Pro API calls in try/catch with meaningful error messages and recovery suggestions.
- Distinguish between error categories with specific handling:
  - **User errors**: Invalid input, missing file, permission issues
  - **System errors**: IPC failure, serialization issue, resource exhaustion
  - **ArcGIS Pro errors**: Geoprocessing failure, license issue, version incompatibility
- Include comprehensive context: `tool`, `parameter`, `file path`, `execution time`, `user context`, `system state`.
- Provide error correlation IDs for debugging and support.

---

## Platform & Framework (Modern & Maintainable)

- **Target .NET 8.0** (LTS, cross-platform) with feature utilization.
- Use **async/await** throughout with proper ConfigureAwait usage.
- Apply **SOLID principles** and **clean architecture** with clear separation:
  - Layered design: `Contracts`, `Server`, `AddIn`, `Core`, `Services` with defined interfaces.
  - Dependency injection via `IServiceCollection` with lifetime management.
  - Logging via `Microsoft.Extensions.Logging` with structured logging and correlation.
- Use `ILogger<T>` for structured logging with log levels and filtering.
- Avoid static classes; prefer injectable services with proper scoping and disposal.
- Implement comprehensive unit testing with mocking and integration testing.

---

## Success Criteria (Measurable & Testable)

### Minimum Viable Product (MVP)
The system must demonstrate:
1. List all ModelBuilder models in a toolbox (via `tools/list`) with complete metadata.
2. Retrieve full model structure (via `tools/get`) with validation and dependency analysis.
3. Execute a simple model (e.g., "Buffer" tool) with valid inputs and comprehensive result reporting.
4. Report success/failure with detailed, categorized messages (via `ExecutionResult`).
5. Validate parameters before execution (via `tools/validate`) with specific error reporting and suggestions.

### Quality Standards
- Code coverage ≥ 95% (use `coverlet`) with meaningful test scenarios.
- All public APIs documented with XML comments including examples and usage patterns.
- Error messages include comprehensive context:
  - Tool name and version
  - Parameter name and expected format
  - File path and accessibility status
  - Suggested fix with specific steps
  - Recovery options when available
- System handles robustly:
  - Concurrent requests with proper resource management
  - Disconnections with automatic recovery
  - Invalid JSON with detailed parsing errors
  - Missing data with specific remediation steps
  - Resource exhaustion with graceful degradation
- Add-In does **not** interfere with normal ArcGIS Pro usage and maintains system stability.

### Testing Checkpoints (Test-Driven Development)
Build incrementally with these comprehensive validation points:
1. MCP Server parses valid JSON-RPC 2.0 messages correctly and rejects invalid ones with specific errors.
2. IPC communication works reliably under load (test with 1000+ concurrent round-trips).
3. Add-In can access a sample project and list models with complete metadata extraction.
4. Simple model executes successfully with end-to-end validation and result verification.
5. Complex model (e.g., 10+ tools) executes correctly with complete data flow tracking and performance monitoring.
6. Error scenarios are handled with actionable, specific messages and recovery suggestions.
7. System performance meets requirements under realistic load conditions.

---

## Development Philosophy (Guiding Principles)

### **Incremental Development**
- **Build in small, testable units with clear acceptance criteria**:
  - Phase 1: Contracts + MCP Server (JSON-RPC validation and error handling)
  - Phase 2: IPC layer + Add-In (comprehensive model listing and metadata extraction)
  - Phase 3: Model execution + comprehensive error handling and monitoring
  - Phase 4: Advanced features (editing, diagnostics, optimization suggestions)
- Validate each component in isolation with comprehensive test coverage before integration.
- Use unit tests (`xUnit`), integration tests (`TestHost`), and end-to-end testing with real data.

### **Pragmatic Problem-Solving**
- Research constraints thoroughly *before* implementation:
  - Study `.tbx` file structure (ZIP → XML) with version variations.
  - Test ArcGIS Pro threading behavior under various load conditions.
  - Benchmark IPC performance with realistic data volumes.
- Choose the **most reliable, maintainable solution**:
  - Use XML parsing over complex COM interop for better error handling.
  - Use named pipes over gRPC if simplicity and reliability are preferred.
- Document **trade-offs, assumptions, and limitations** in code comments, `README.md`, and architecture documentation.

### **User-Centric Design (For AI Assistants)**
- Assume the **AI assistant is the primary end user** with specific needs:
  - Requires predictable, structured responses
  - Needs detailed error context for problem-solving
  - Benefits from comprehensive metadata and suggestions
- Prioritize:
  - Clarity and consistency in all responses
  - Actionable, specific error messages with examples
  - Predictable behavior with comprehensive documentation
  - Fast feedback loop with progress indication
- Design for **debugging and troubleshooting**, not just successful execution.

---

## Version Control and Collaboration (GitHub Integration)

### **Repository Structure**
- Use **GitHub** for version control with comprehensive branching strategy:
  - `main` branch for stable, production-ready releases with branch protection
  - `develop` branch for integration of new features with automated testing
  - Feature branches with descriptive names: `feature/model-execution`, `bugfix/ipc-timeout`
  - Release branches for version preparation: `release/v1.0.0`
- Implement **semantic versioning** (e.g., `v1.2.3`) with automated changelog generation
- Tag releases with detailed release notes and binary artifacts

### **GitHub Actions CI/CD Pipeline**
- Configure **automated workflows** for:
  - Build validation on all pull requests with multi-platform testing (.NET 8.0, Windows/Linux)
  - Unit and integration test execution with coverage reporting via Codecov
  - Static code analysis using SonarCloud or CodeQL with security scanning
  - Automated dependency updates via Dependabot with security vulnerability alerts
  - Release artifact generation (NuGet packages, installers) with automated deployment
  - Documentation generation and deployment to GitHub Pages

### **Collaborative Development**
- Use **GitHub Issues** for:
  - Bug tracking with detailed templates and reproduction steps
  - Feature requests with user stories and acceptance criteria
  - Technical debt tracking with priority labels and milestones
- Implement **pull request templates** with:
  - Checklist for code review (tests, documentation, breaking changes)
  - Required reviewers and approval workflows
  - Automated linking to related issues
- Use **GitHub Projects** for:
  - Sprint planning and backlog management with kanban boards
  - Milestone tracking with progress visualization
  - Release planning with feature grouping and dependencies

### **Documentation and Wiki**
- Maintain comprehensive **GitHub Wiki** with:
  - Architecture decisions and design rationale
  - API documentation with interactive examples
  - Deployment guides for different environments
  - Troubleshooting guides with common issues and solutions
  - Contribution guidelines for external developers
- Use **GitHub Discussions** for:
  - Community support and Q&A
  - Feature discussions and RFC (Request for Comments)
  - Integration examples and use case sharing

---

## Project Structure (Clean & Scalable)

```
MCP-ArcGIS-Pro/
│
├── .github/
│   ├── workflows/
│   │   ├── ci.yml               # Build and test automation
│   │   ├── release.yml          # Release artifact generation
│   │   └── security.yml         # Security scanning and updates
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   ├── pull_request_template.md
│   └── dependabot.yml           # Automated dependency updates
│
├── src/
│   ├── Contracts/               # Shared data models
│   ├── MCP.Server/              # JSON-RPC console app
│   ├── ArcGISPro.AddIn/         # ArcGIS Pro extension
│   ├── Core/                    # Shared logic (model parsing, validation)
│   ├── Services/                # IPC, logging, error handling
│   └── Tests/
│       ├── UnitTests/
│       └── IntegrationTests/
│
├── docs/
│   ├── README.md                # Setup, build, run
│   ├── API.md                   # MCP protocol reference
│   ├── Architecture.md          # Component diagram
│   ├── CONTRIBUTING.md          # Development guidelines
│   └── Troubleshooting.md
│
├── configs/
│   ├── mcp-server.json          # Configuration (IPC path, timeout, etc.)
│   └── addin-manifest.xml       # ArcGIS Pro Add-In manifest
│
├── scripts/
│   ├── build.sh                 # Build script (cross-platform)
│   ├── run.sh                   # Run script (start server + add-in)
│   └── setup-dev.sh             # Development environment setup
│
├── .gitignore                   # Git ignore patterns for .NET and ArcGIS
├── LICENSE                      # Open source license
├── CHANGELOG.md                 # Version history and release notes
└── VERSION                      # Current version for automated releases
```

---

## What You Should Figure Out (Research & Decision Tasks)

You must **determine and document** the following with detailed analysis:

1. **IPC Mechanism**: Choose between named pipes and gRPC with performance benchmarking. Justify choice with specific criteria.
2. **Model Reading**: How to parse `.tbx` → XML → `ModelStructure` with error handling. Provide complete sample code with edge cases.
3. **Threading Safety**: How to access ArcGIS Pro objects from background threads with comprehensive safety measures.
4. **Real-Time Logging**: How to capture geoprocessing messages (`arcpy.AddMessage`, etc.) with filtering and formatting.
5. **Serialization**: How to serialize complex ArcGIS Pro objects (e.g., `FeatureLayer`, `RasterDataset`) with type preservation.
6. **Model Editing**: Is it possible to modify `.tbx` XML safely with validation? If not, document fallback approaches.
7. **Deployment**: How to package and deploy the Add-In (via `.esriaddins` or `.dll`) with dependency management.
8. **Client Discovery**: How to allow AI clients to locate the MCP Server (e.g., via port, named pipe, or config file) with service registration.

---

## Resources & References (Curated & Actionable)

- [ArcGIS Pro SDK Documentation](https://developers.arcgis.com/net/latest/windows/)
- [Model Context Protocol (MCP) Specification](https://github.com/modelcontextprotocol/specification)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [.NET 8.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)
- [ArcGIS Pro Geoprocessing API](https://pro.arcgis.com/en/pro-app/latest/arcpy/geoprocessing/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/)
- [Sample: Reading .tbx Files](https://github.com/Esri/arcgis-pro-sdk/wiki/Toolbox-Structure)

---

## Deliverables (Complete & Deployable)

Provide a production-ready system that:
1. Can be built with `dotnet build` and run with `dotnet run` with clear dependency management.
2. Includes all three components with explicit project references and version compatibility.
3. Contains comprehensive `mcp-server.json` and `addin-manifest.xml` for configuration with validation.
4. Includes detailed `README.md` with:
   - Prerequisites (ArcGIS Pro version, .NET 8.0, system requirements)
   - Step-by-step build instructions with troubleshooting
   - Detailed run steps with configuration options
   - Comprehensive AI client usage examples with expected responses
5. Demonstrates core functionality with:
   - Multiple test `.tbx` files with varying complexity
   - Sample models (e.g., "Buffer", "Clip", "Spatial Join") with realistic data
   - Working `test-client.json` script with comprehensive test scenarios
   - Performance benchmarks and load testing results
6. **GitHub repository** with:
   - Complete CI/CD pipeline configuration with automated testing and deployment
   - Issue templates, pull request workflows, and contribution guidelines
   - Comprehensive documentation in Wiki format with architecture decisions
   - Release management with semantic versioning and automated changelog generation
   - Security scanning and dependency management automation

---

> **Final Note**: This system must be **reliable, maintainable, and extensible** for production use. Think like a software architect building enterprise-grade software, not just a prototype. Prioritize clarity, comprehensive error resilience, and exceptional developer experience. Your goal is to **democratize access to ModelBuilder through AI** — make it feel natural, intuitive, powerful, and completely trustworthy for mission-critical geospatial workflows. The GitHub integration ensures collaborative development, continuous quality assurance, and professional project management throughout the software lifecycle.