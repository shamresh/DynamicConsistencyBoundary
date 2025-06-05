# Dynamic Consistency Boundary McpServer

The **McpServer** is a JSON-RPC 2.0 compliant server that provides a standardized interface for interacting with an event store. It is designed to support dynamic tool discovery, flexible event handling, and querying, all within a robust and extensible architecture.

---

## Architecture Overview

### 1. JSON-RPC 2.0 Server
- Implements the JSON-RPC 2.0 protocol for client-server communication.
- Handles request/response cycles with robust error handling.
- Supports both regular requests and notifications.

### 2. Event Store Integration
- Integrates with an `IEventStore` interface for event sourcing.
- Supports appending new events with consistency checks.
- Enables flexible event querying with various filters.

### 3. Dynamic Tool System
- Tools are discovered at runtime using reflection and custom attributes.
- Each tool is defined with a name, description, and input schema.
- Tools can be dynamically added without modifying the core server logic.

### 4. Prompt System
- Prompts are discovered via reflection and custom attributes.
- Provides example usage patterns for tools.
- Helps with tool discovery and usage.

---

## AI-Assisted Tool Usage via JSON Schema

The McpServer is designed so that both programmatic clients and AI assistants can fully understand and use its tools without any special prompt system. This is achieved through the automatic generation of JSON Schema for each tool, which is exposed via the `tools/list` endpoint.

### How It Works

- **Tool Discovery:**  
  Clients (including AI) call `tools/list` to get a list of all available tools, each with a name, description, and a detailed `inputSchema`.

- **Schema Details:**  
  The `inputSchema` describes:
  - The type of each parameter (string, integer, boolean, array, object, etc.)
  - Which parameters are required or optional
  - Descriptions for each parameter (from C# `[Description]` attributes)
  - The structure of complex/nested types

- **Natural Language Guidance:**  
  Tool descriptions often include example requests in natural language and JSON, making it easy for an AI to map user intent to the correct tool and parameters.

### Example

Suppose a user says:  
> "Find all events for student s1"

An AI assistant can:
1. Look up the `QueryEvents` tool in the `tools/list` response.
2. See from the schema and description that it should use the `tags` parameter.
3. Construct the correct JSON-RPC call:
   ```json
   {
     "jsonrpc": "2.0",
     "method": "tools/call",
     "params": {
       "name": "QueryEvents",
       "arguments": {
         "tags": [
           { "entity": "student", "id": "s1" }
         ]
       }
     },
     "id": 1
   }
   ```

### Why Prompts Are Not Needed

- The JSON Schema approach provides all the structure, types, and descriptions needed for both humans and AI to use the API correctly.
- Example usage and parameter descriptions are already included in the tool metadata.
- This makes the system self-documenting and easy to use for both code and AI-driven clients.

---

## Tool Input Schema: Structure and Retrieval

### What the Schema Looks Like

Each tool exposes an `inputSchema` that describes the expected input parameters, their types, and descriptions. This schema is generated from the C# method signatures and `[Description]` attributes.

**Example: QueryEvents Tool Schema**
```json
{
  "name": "QueryEvents",
  "description": "Query events from the event store using various filters.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "eventType": {
        "type": "string",
        "description": "Filter events by type"
      },
      "tags": {
        "type": "array",
        "description": "Filter events by tags",
        "items": {
          "type": "object",
          "properties": {
            "entity": { "type": "string" },
            "id": { "type": "string" }
          }
        }
      },
      "matchAnyTag": {
        "type": "boolean",
        "description": "Whether to match any tag (true) or all tags (false)"
      },
      "fromPosition": {
        "type": "integer",
        "description": "Start reading from this position"
      },
      "pageSize": {
        "type": "integer",
        "description": "Maximum number of events to return"
      }
    }
  }
}
```

### When and How the Schema is Retrieved

- **How:**
  Clients retrieve the schema by calling the `tools/list` JSON-RPC method:
  ```json
  {
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 11
  }
  ```

- **When:**
  Clients typically call `tools/list` after `initialize` and before making any tool calls. This allows them to dynamically discover all available tools and their input schemas.

- **Why:**
  This enables clients (including AI assistants) to:
  - Dynamically generate UI forms
  - Validate user input
  - Map natural language to tool parameters
  - Ensure correct request structure

---

## Available Tools & Examples

### QueryEvents
Queries events from the event store based on:
- Event type filtering
- Tag-based filtering (with support for matching any or all tags)
- Position-based pagination
- Page size control

**Example 1: Find all events for a student**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "QueryEvents",
    "arguments": {
      "tags": [
        {
          "entity": "student",
          "id": "s1"
        }
      ]
    }
  },
  "id": 1
}
```

**Example 2: Find student enrollments in a class**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "QueryEvents",
    "arguments": {
      "eventType": "StudentEnrolled",
      "tags": [
        {
          "entity": "class",
          "id": "c1"
        }
      ]
    }
  },
  "id": 2
}
```

### AppendEvent
Appends a new event to the event store:
- Requires event type, tags, and data
- Supports consistency boundary checks
- Automatically generates event ID and timestamp
- Validates event data format

**Example: Register a new student**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "AppendEvent",
    "arguments": {
      "event": {
        "eventType": "StudentRegistered",
        "tags": [
          {
            "entity": "student",
            "id": "s3"
          }
        ],
        "data": {
          "studentId": "s3",
          "name": "Alice"
        }
      },
      "query": {
        "specification": {
          "type": "ByEventTypeAndTags",
          "eventType": "StudentRegistered",
          "tags": [
            {
              "entity": "student",
              "id": "s3"
            }
          ],
          "matchAnyTag": false
        }
      },
      "lastKnownPosition": 0
    }
  },
  "id": 3
}
```

---

### GetCurrentPosition
Retrieves the current position in the event stream.

**Example JSON-RPC request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "GetCurrentPosition",
    "arguments": {}
  },
  "id": 3
}
```

**Example response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      { "type": "text", "text": "{\"position\":42}" }
    ]
  }
}
```

---

## Event Model

Events in the system have the following structure:
```json
{
  "id": "string",
  "position": "number",
  "eventType": "string",
  "timestamp": "string",
  "tags": [
    { "entity": "string", "id": "string" }
  ],
  "data": "string" // Serialized JSON data
}
```

---

## Query Model

Queries support multiple filter specifications:
```json
{
  "specification": {
    "type": "ByEventTypeAndTags",
    "eventType": "StudentRegistered",
    "tags": [
      { "entity": "student", "id": "s1" }
    ],
    "matchAnyTag": false
  },
  "fromPosition": 0,
  "pageSize": 10
}
```

---

## Protocol Examples

### Initialize
**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "id": 10
}
```
**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": { "tools": {} },
    "serverInfo": { "name": "dynamic-consistency-boundary", "version": "1.0.0" }
  }
}
```

### Tools/List
**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 11
}
```
**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "result": {
    "tools": [
      { "name": "QueryEvents", "description": "...", "inputSchema": { /* ... */ } },
      { "name": "AppendEvent", "description": "...", "inputSchema": { /* ... */ } },
      { "name": "GetCurrentPosition", "description": "...", "inputSchema": { /* ... */ } }
    ]
  }
}
```

### Tools/Call
(See tool-specific examples above.)

---

## Extending the Server

### Adding New Tools

1. Create a new class with the `[McpServerToolType]` attribute.
2. Add methods with the `[McpServerTool]` attribute.
3. Use `[Description]` attribute to provide tool documentation.
4. Implement the tool logic.

**Example:**
```csharp
using Core.Domain.Shared.Interfaces;
using System.ComponentModel;

[McpServerToolType]
public class CustomTools
{
    [McpServerTool]
    [Description("Returns a greeting for the given name.")]
    public async Task<object> Greet(IEventStore eventStore, string name)
    {
        return new { message = $"Hello, {name}!" };
    }
}
```

### Adding New Prompts

1. Create a new class with the `[McpServerPromptType]` attribute.
2. Add methods with the `[McpServerPrompt]` attribute.
3. Use `[Description]` attribute with example usage.

**Example:**
```csharp
[McpServerPromptType]
public class CustomPrompts
{
    [McpServerPrompt]
    [Description("Example: Greet the user by name\nInput: {\"name\":\"Alice\"}")]
    public void GreetPrompt() { }
}
```

### Prompt System

The prompt system is a discovery mechanism that helps users understand how to use the available tools. It works as follows:

1. **Discovery**: When the server starts, it scans for classes marked with `[McpServerPromptType]` and methods marked with `[McpServerPrompt]`.

2. **Documentation**: Each prompt method is decorated with a `[Description]` attribute that contains:
   - An example of how to use the tool
   - The expected input format
   - Any relevant context or notes

3. **Usage**: Prompts are used to:
   - Generate help documentation
   - Provide example usage patterns
   - Guide users in constructing valid requests

4. **Integration**: The prompt system is integrated with the tool system through:
   - Shared attributes (`McpServerToolType` and `McpServerPromptType`)
   - Consistent documentation format
   - Example-driven documentation

Example of a prompt definition:
```csharp
[McpServerPromptType]
public class EventStorePrompts
{
    [McpServerPrompt]
    [Description("""
        Example: Register a new student
        Input: {
          "event": {
            "eventType": "StudentRegistered",
            "tags": [{"entity": "student", "id": "s3"}],
            "data": {"studentId": "s3", "name": "Alice"}
          }
        }
        """)]
    public void StudentRegistrationPrompt() { }
}
```

---

## Best Practices

- Use meaningful event types and relevant tags for efficient querying.
- Keep event data serializable.
- Use appropriate filter specifications and pagination for queries.
- Provide clear tool descriptions and example usage.
- Handle errors gracefully and log for debugging.

---

## Security Considerations

- Input validation for all incoming requests.
- Proper error handling to prevent information leakage.
- Event store access control through the `IEventStore` interface.
- JSON-RPC 2.0 protocol compliance for secure communication.

---

## Deployment & Configuration

- **Release Build:** Ensure to build a release version of the McpServer for production use.
- **Configuration:** Tie the server to the provided JSON configuration:
  ```json
  {
    "mcpServers": {
      "dynamic-consistency-boundary": {
        "command": "/absolute/path/to/MCPServer/publish/DynamicConsistencyBoundary.MCPServer.exe"
      }
    }
  }
  ```
- **Pollution Issue:** Be aware of potential pollution issues when running the server. Ensure proper cleanup and resource management.
- **Windows Configuration:** On Windows, you may need to kill Claude Desktop for configuration changes to take effect. 