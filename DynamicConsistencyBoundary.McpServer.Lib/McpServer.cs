using System.Text.Json;
using Core.Domain.Shared.Interfaces;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using DynamicConsistencyBoundary.McpServer.Models;
using System.Text.Json.Serialization;

namespace DynamicConsistencyBoundary.McpServer;

public class McpServer
{
    private readonly IEventStore _eventStore;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ToolDefinition> _tools;

    public McpServer(IEventStore eventStore)
    {
        _eventStore = eventStore;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _tools = InitializeTools();
    }

    private List<ToolDefinition> InitializeTools()
    {
        return new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Tool = "query_events",
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
                Tool = "append_event",
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
                Tool = "get_current_position",
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
                    Result = new ToolCallResult
                    {
                        Status = "success",
                        Data = new ContentItem
                        {
                            Type = "text",
                            Text = JsonSerializer.Serialize(new InitializeResult(), _jsonOptions)
                        }
                    }
                };

            case "initialized":
            case "notifications/initialized":
                Console.Error.WriteLine("[DEBUG] MCP server initialized");
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new ToolCallResult
                    {
                        Status = "success",
                        Data = new ContentItem
                        {
                            Type = "text",
                            Text = "{}"
                        }
                    }
                };

            case "tools/list":
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new ToolCallResult
                    {
                        Status = "success",
                        Data = new ContentItem
                        {
                            Type = "text",
                            Text = JsonSerializer.Serialize(new ToolsListResult { Tools = _tools }, _jsonOptions)
                        }
                    }
                };

            case "mcp/execute":
                if (request.Params == null)
                {
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = "Invalid params - tool name required",
                            Data = new ErrorData
                            {
                                Timestamp = DateTime.UtcNow,
                                Server = "MCP Stdio Server"
                            }
                        }
                    };
                }

                var toolCallParams = JsonSerializer.Deserialize<ToolCallParameters>(request.Params.ToString()!, _jsonOptions);
                if (toolCallParams == null || string.IsNullOrEmpty(toolCallParams.Tool))
                {
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = "Invalid params - tool name required",
                            Data = new ErrorData
                            {
                                Timestamp = DateTime.UtcNow,
                                Server = "MCP Stdio Server"
                            }
                        }
                    };
                }

                try
                {
                    Console.Error.WriteLine($"[DEBUG] Calling tool: {toolCallParams.Tool} with args: {request.Params}");
                    var result = await ExecuteTool(toolCallParams.Tool, toolCallParams.Parameters ?? new Dictionary<string, object>());
                    Console.Error.WriteLine($"[DEBUG] Tool result: {result}");

                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new ToolCallResult
                        {
                            Status = "success",
                            Data = new ContentItem
                            {
                                Type = "text",
                                Text = result is string str ? str : JsonSerializer.Serialize(result, _jsonOptions)
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
                            Message = ex.Message,
                            Data = new ErrorData
                            {
                                Timestamp = DateTime.UtcNow,
                                Server = "MCP Stdio Server"
                            }
                        }
                    };
                }

            default:
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32601,
                        Message = $"Method not found: {request.Method}",
                        Data = new ErrorData
                        {
                            Timestamp = DateTime.UtcNow,
                            Server = "MCP Stdio Server"
                        }
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

        string? eventType = null;
        if (arguments.TryGetValue("eventType", out var eventTypeObj))
        {
            if (eventTypeObj is string eventTypeStr)
            {
                eventType = eventTypeStr;
            }
            else if (eventTypeObj is JsonElement eventTypeElement && eventTypeElement.ValueKind == JsonValueKind.String)
            {
                eventType = eventTypeElement.GetString();
            }
        }

        List<EntityTag>? tags = null;
        bool matchAnyTag = false;
        if (arguments.TryGetValue("tags", out var tagsObj) && tagsObj is JsonElement tagsElement)
        {
            tags = tagsElement.EnumerateArray()
                .Select(t => new EntityTag(
                    t.GetProperty("entity").GetString()!,
                    t.GetProperty("id").GetString()!
                ))
                .ToList();

            matchAnyTag = arguments.TryGetValue("matchAnyTag", out var matchAny) && matchAny is bool matchAnyBool && matchAnyBool;
        }

        // Add filters based on what's provided
        Console.Error.WriteLine($"[DEBUG] eventType: {eventType}, tags: {JsonSerializer.Serialize(tags, _jsonOptions)}, matchAnyTag: {matchAnyTag}");
        if (!string.IsNullOrEmpty(eventType))
        {
            if (tags != null && tags.Any())
            {
                // Both event type and tags are provided
                Console.Error.WriteLine($"[DEBUG] Using ByEventTypeAndTags with eventType: {eventType}, tags: {JsonSerializer.Serialize(tags, _jsonOptions)}, matchAnyTag: {matchAnyTag}");
                queryBuilder.WithSpecification(EventFilterSpecification.ByEventTypeAndTags(eventType, tags, matchAnyTag));
            }
            else
            {
                // Only event type is provided
                Console.Error.WriteLine($"[DEBUG] Using ByEventType with eventType: {eventType}");
                queryBuilder.WithSpecification(EventFilterSpecification.ByEventType(eventType));
            }
        }
        else if (tags != null && tags.Any())
        {
            // Only tags are provided
            Console.Error.WriteLine($"[DEBUG] Using ByTags with tags: {JsonSerializer.Serialize(tags, _jsonOptions)}, matchAnyTag: {matchAnyTag}");
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
        Console.Error.WriteLine($"[DEBUG] Query: {JsonSerializer.Serialize(query, _jsonOptions)}");
        var events = await _eventStore.QueryEventsAsync(query);
        Console.Error.WriteLine($"[DEBUG] Results: {JsonSerializer.Serialize(events, _jsonOptions)}");
        
        // Convert events to a format that matches the query format
        var formattedEvents = events.Select(e => new
        {
            e.Id,
            e.Position,
            e.EventType,
            e.Timestamp,
            Tags = e.Tags.Select(t => new { entity = t.Entity, id = t.Id }).ToList(),
            Data = e.SerializedData
        });

        return JsonSerializer.Serialize(formattedEvents, _jsonOptions);
    }

    private async Task<object> HandleAppendEvent(Dictionary<string, object> arguments)
    {
        if (!arguments.TryGetValue("event", out var eventObj) || eventObj is not JsonElement eventElement)
        {
            throw new ArgumentException("Invalid event parameter");
        }

        if (!arguments.TryGetValue("query", out var queryObj) || queryObj is not JsonElement queryElement)
        {
            throw new ArgumentException("Invalid query parameter");
        }

        if (!arguments.TryGetValue("lastKnownPosition", out var positionObj) || positionObj is not JsonElement positionElement)
        {
            throw new ArgumentException("Invalid lastKnownPosition parameter");
        }

        // Debug logging
        Console.Error.WriteLine($"[DEBUG] Event JSON: {eventElement.GetRawText()}");

        var @event = JsonSerializer.Deserialize<Event>(eventElement.GetRawText(), _jsonOptions);
        var query = JsonSerializer.Deserialize<EventQuery>(queryElement.GetRawText(), _jsonOptions);
        var lastKnownPosition = positionElement.GetInt64();

        if (@event == null || query == null)
        {
            throw new ArgumentException("Failed to deserialize event or query");
        }

        await _eventStore.AppendEventAsync(@event, query, lastKnownPosition);
        return true;
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
                    Message = message,
                    Data = new ErrorData
                    {
                        Timestamp = DateTime.UtcNow,
                        Server = "MCP Stdio Server"
                    }
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