using System.Text.Json;
using Core.Domain.Shared.Interfaces;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using DynamicConsistencyBoundary.McpServer.Models;

namespace DynamicConsistencyBoundary.McpServer;

public class McpServer
{
    private readonly IEventStore _eventStore;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ToolDefinition> _tools;
    private bool _isInitialized;

    public McpServer(IEventStore eventStore)
    {
        _eventStore = eventStore;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _tools = InitializeTools();
        _isInitialized = false;
    }

    private List<ToolDefinition> InitializeTools()
    {
        return new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Name = "query_events",
                Description = "Query events from the event store using various filters",
                Parameters = new ToolParameters
                {
                    Properties = new Dictionary<string, ToolParameterProperty>
                    {
                        ["eventType"] = new ToolParameterProperty
                        {
                            Type = "string",
                            Description = "Filter events by type"
                        },
                        ["tags"] = new ToolParameterProperty
                        {
                            Type = "array",
                            Description = "Filter events by tags",
                            Items = new ToolParameterProperty
                            {
                                Type = "object",
                                Description = "Tag with key and value"
                            }
                        },
                        ["matchAnyTag"] = new ToolParameterProperty
                        {
                            Type = "boolean",
                            Description = "Whether to match any tag (true) or all tags (false)"
                        },
                        ["fromPosition"] = new ToolParameterProperty
                        {
                            Type = "integer",
                            Description = "Start reading from this position"
                        },
                        ["pageSize"] = new ToolParameterProperty
                        {
                            Type = "integer",
                            Description = "Maximum number of events to return"
                        }
                    }
                }
            },
            new ToolDefinition
            {
                Name = "append_event",
                Description = "Append a new event to the event store",
                Parameters = new ToolParameters
                {
                    Properties = new Dictionary<string, ToolParameterProperty>
                    {
                        ["event"] = new ToolParameterProperty
                        {
                            Type = "object",
                            Description = "The event to append"
                        },
                        ["query"] = new ToolParameterProperty
                        {
                            Type = "object",
                            Description = "Query context for the event"
                        },
                        ["lastKnownPosition"] = new ToolParameterProperty
                        {
                            Type = "integer",
                            Description = "Last known position for optimistic concurrency"
                        }
                    },
                    Required = new List<string> { "event", "query", "lastKnownPosition" }
                }
            },
            new ToolDefinition
            {
                Name = "get_current_position",
                Description = "Get the current position in the event store",
                Parameters = new ToolParameters()
            }
        };
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var line = await Console.In.ReadLineAsync();
            if (line == null) break;

            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, _jsonOptions);
                if (request == null) continue;

                // Validate JSON-RPC 2.0 request
                if (request.JsonRpc != "2.0")
                {
                    await SendErrorAsync(request.Id, -32600, "Invalid Request - missing jsonrpc 2.0");
                    continue;
                }

                var response = await HandleRequest(request);
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await Console.Out.WriteLineAsync(responseJson);
                await Console.Out.FlushAsync();
            }
            catch (JsonException)
            {
                await SendErrorAsync(null, -32700, "Parse error");
            }
            catch (Exception ex)
            {
                await SendErrorAsync(null, -32603, $"Internal error: {ex.Message}");
            }
        }
    }

    public async Task<JsonRpcResponse> HandleRequest(JsonRpcRequest request)
    {
        Console.Error.WriteLine($"[DEBUG] Received method: {request.Method} with id: {request.Id}");

        switch (request.Method)
        {
            case "initialize":
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new InitializeResult()
                };

            case "initialized":
            case "notifications/initialized":
                _isInitialized = true;
                Console.Error.WriteLine("[DEBUG] MCP server initialized");
                return new JsonRpcResponse { Id = request.Id };

            case "tools/list":
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new ToolsListResult { Tools = _tools }
                };

            case "tools/call":
                if (request.Params == null)
                {
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = "Invalid params - tool name required"
                        }
                    };
                }

                var toolCallParams = JsonSerializer.Deserialize<ToolCallParameters>(request.Params.ToString()!, _jsonOptions);
                if (toolCallParams == null || string.IsNullOrEmpty(toolCallParams.Name))
                {
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = "Invalid params - tool name required"
                        }
                    };
                }

                try
                {
                    Console.Error.WriteLine($"[DEBUG] Calling tool: {toolCallParams.Name} with args: {request.Params}");
                    var result = await ExecuteTool(toolCallParams.Name, toolCallParams.Arguments ?? new Dictionary<string, object>());
                    Console.Error.WriteLine($"[DEBUG] Tool result: {result}");

                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new ToolCallResult
                        {
                            Content = new List<ContentItem>
                            {
                                new()
                                {
                                    Type = "text",
                                    Text = result is string str ? str : JsonSerializer.Serialize(result, _jsonOptions)
                                }
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[DEBUG] Tool execution error: {ex}");
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32603,
                            Message = ex.Message
                        }
                    };
                }

            default:
                Console.Error.WriteLine($"[DEBUG] Unknown method: {request.Method}");
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32601,
                        Message = $"Method not found: {request.Method}"
                    }
                };
        }
    }

    private async Task<object> ExecuteTool(string name, Dictionary<string, object> arguments)
    {
        switch (name)
        {
            case "query_events":
                return await HandleQueryEvents(arguments);
            case "append_event":
                return await HandleAppendEvent(arguments);
            case "get_current_position":
                return await HandleGetCurrentPosition();
            default:
                throw new InvalidOperationException($"Unknown tool: {name}");
        }
    }

    private async Task<object> HandleQueryEvents(Dictionary<string, object> arguments)
    {
        var queryBuilder = EventQuery.Create();

        // Get event type and tags if specified
        string? eventType = null;
        if (arguments.TryGetValue("eventType", out var eventTypeObj) && eventTypeObj is string eventTypeStr)
        {
            eventType = eventTypeStr;
            queryBuilder.WithSpecification(EventFilterSpecification.ByEventType(eventType));
        }

        List<EntityTag>? tags = null;
        if (arguments.TryGetValue("tags", out var tagsObj) && tagsObj is JsonElement tagsElement)
        {
            tags = tagsElement.EnumerateArray()
                .Select(t => new EntityTag(
                    t.GetProperty("key").GetString()!,
                    t.GetProperty("value").GetString()!
                ))
                .ToList();

            var matchAnyTag = arguments.TryGetValue("matchAnyTag", out var matchAny) && matchAny is bool matchAnyBool && matchAnyBool;
            queryBuilder.WithSpecification(EventFilterSpecification.ByTags(tags, matchAnyTag));
        }

        // Apply position and page size filters
        if (arguments.TryGetValue("fromPosition", out var fromPos) && fromPos is JsonElement fromPosElement)
        {
            queryBuilder.FromPosition(fromPosElement.GetInt64());
        }

        if (arguments.TryGetValue("pageSize", out var pageSize) && pageSize is JsonElement pageSizeElement)
        {
            queryBuilder.WithPageSize(pageSizeElement.GetInt32());
        }

        var query = queryBuilder.Build();
        var events = await _eventStore.QueryEventsAsync(query);
        
        // Convert events to a format that matches the query format
        var formattedEvents = events.Select(e => new
        {
            e.Id,
            e.Position,
            e.EventType,
            e.Timestamp,
            Tags = e.Tags.Select(t => new { key = t.Entity, value = t.Id }).ToList(),
            Data = e.SerializedData
        });

        return JsonSerializer.Serialize(formattedEvents, _jsonOptions);
    }

    private async Task<object> HandleAppendEvent(Dictionary<string, object> arguments)
    {
        // Deserialize the arguments into AppendEventParameters
        var appendParams = JsonSerializer.Deserialize<AppendEventParameters>(
            JsonSerializer.Serialize(arguments, _jsonOptions),
            _jsonOptions
        );

        if (appendParams == null)
        {
            throw new ArgumentException("Invalid parameters");
        }

        await _eventStore.AppendEventAsync(appendParams.Event, appendParams.Query, appendParams.LastKnownPosition);
        return true;
    }

    private class AppendEventParameters
    {
        public Event Event { get; set; } = null!;
        public EventQuery Query { get; set; } = null!;
        public long LastKnownPosition { get; set; }
    }

    private async Task<object> HandleGetCurrentPosition()
    {
        return await _eventStore.GetCurrentPositionAsync();
    }

    private async Task SendErrorAsync(object? id, int code, string message)
    {
        if (id != null)
        {
            var error = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message
                }
            };
            Console.Error.WriteLine($"[DEBUG] Sending error response: {JsonSerializer.Serialize(error, _jsonOptions)}");
            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(error, _jsonOptions));
            await Console.Out.FlushAsync();
        }
        else
        {
            Console.Error.WriteLine($"[ERROR] Cannot send error response without valid id: {{ code: {code}, message: {message} }}");
        }
    }
}

public class ToolCallParameters
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object>? Arguments { get; set; }
} 