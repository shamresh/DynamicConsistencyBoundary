using System.Text.Json.Serialization;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;

namespace DynamicConsistencyBoundary.McpServer.Models;

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

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object Id { get; set; } = null!;

    [JsonPropertyName("result")]
    public ToolCallResult? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public ErrorData? Data { get; set; }
}

public class ErrorData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("server")]
    public string Server { get; set; } = "MCP Stdio Server";
}

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

public class ToolCallResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("data")]
    public ContentItem Data { get; set; } = new();
}

public class ContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

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
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
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

public class ToolCallParameters
{
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
} 