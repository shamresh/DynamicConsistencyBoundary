using System.Text.Json.Serialization;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicConsistencyBoundary.McpServer.Models;

// JSON-RPC base classes
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object Id { get; set; } = null!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class JsonRpcResponseConverter : JsonConverter<JsonRpcResponse>
{
    public override JsonRpcResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, JsonRpcResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", value.JsonRpc);
        writer.WritePropertyName("id");
        JsonSerializer.Serialize(writer, value.Id, options);
        
        if (value is JsonRpcSuccessResponse successResponse)
        {
            writer.WritePropertyName("result");
            JsonSerializer.Serialize(writer, successResponse.Result, options);
        }
        else if (value is JsonRpcErrorResponse errorResponse)
        {
            writer.WritePropertyName("error");
            JsonSerializer.Serialize(writer, errorResponse.Error, options);
        }
        
        writer.WriteEndObject();
    }
}

[JsonConverter(typeof(JsonRpcResponseConverter))]
public abstract class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }
}

public class JsonRpcSuccessResponse : JsonRpcResponse
{
    [JsonPropertyName("result")]
    public object Result { get; set; } = null!;

    public static JsonRpcSuccessResponse Create(object? id, object result)
    {
        return new JsonRpcSuccessResponse
        {
            Id = id,
            Result = result
        };
    }
}

public class JsonRpcErrorResponse : JsonRpcResponse
{
    [JsonPropertyName("error")]
    public JsonRpcError Error { get; set; } = null!;

    public static JsonRpcErrorResponse Create(object? id, int code, string message)
    {
        return new JsonRpcErrorResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message
            }
        };
    }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// MCP-specific classes
public class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("capabilities")]
    public Capabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = new();
}

public class Capabilities
{
    [JsonPropertyName("tools")]
    public object Tools { get; set; } = new();
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "dynamic-consistency-boundary-mcp-server";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

public class ToolsListResult
{
    [JsonPropertyName("tools")]
    public List<ToolDefinition> Tools { get; set; } = new();
}

// Tool definition classes
public class ToolDefinition
{
    [JsonPropertyName("name")]  // Changed from "tool" to "name"
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]  // Changed from "parameters" to "inputSchema"
    public ToolInputSchema InputSchema { get; set; } = new();
}

public class ToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, ToolParameterProperty> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class ToolParameterProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public ToolParameterProperty? Items { get; set; }
}

// Tool call parameter classes
public class ToolCallParameters
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

// Tool result classes
public class ToolCallResult
{
    [JsonPropertyName("content")]
    public List<ContentItem> Content { get; set; } = new();
}

public class ContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

// Parameter classes for specific tools
public class QueryEventsParameters
{
    [JsonPropertyName("eventType")]
    public string? EventType { get; set; }

    [JsonPropertyName("tags")]
    public List<Tag>? Tags { get; set; }

    [JsonPropertyName("matchAnyTag")]
    public bool MatchAnyTag { get; set; }

    [JsonPropertyName("fromPosition")]
    public long? FromPosition { get; set; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }
}

public class Tag
{
    [JsonPropertyName("entity")]  // Changed from "key" to "entity" to match your domain
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("id")]  // Changed from "value" to "id" to match your domain
    public string Id { get; set; } = string.Empty;
}

public class AppendEventParameters
{
    [JsonPropertyName("event")]
    public Event? Event { get; set; }

    [JsonPropertyName("query")]
    public EventQuery? Query { get; set; }

    [JsonPropertyName("lastKnownPosition")]
    public long LastKnownPosition { get; set; }
}