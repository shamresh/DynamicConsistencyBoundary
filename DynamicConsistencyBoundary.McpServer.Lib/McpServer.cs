using System.Text.Json;
using Core.Domain.Shared.Interfaces;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using DynamicConsistencyBoundary.McpServer.Models;
using DynamicConsistencyBoundary.McpServer.Tools;
using DynamicConsistencyBoundary.McpServer.Prompts;
using System.Text.Json.Serialization;
using System.Reflection;
using System.ComponentModel;

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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _tools = InitializeTools();
    }

    private List<ToolDefinition> InitializeTools()
    {
        var tools = new List<ToolDefinition>();
        var assembly = Assembly.GetExecutingAssembly();

        // Find all types with McpServerToolType attribute
        var toolTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null);

        foreach (var toolType in toolTypes)
        {
            // Find all methods with McpServerTool attribute
            var toolMethods = toolType.GetMethods()
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

            foreach (var method in toolMethods)
            {
                var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                var descAttr = method.GetCustomAttribute<DescriptionAttribute>();

                var tool = new ToolDefinition
                {
                    Name = !string.IsNullOrEmpty(toolAttr?.Name) ? toolAttr.Name : method.Name,
                    Description = descAttr?.Description ?? string.Empty,
                    InputSchema = CreateInputSchema(method)
                };

                tools.Add(tool);
            }
        }

        return tools;
    }

    private ToolInputSchema CreateInputSchema(MethodInfo method)
    {
        var schema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, ToolParameterProperty>()
        };

        var parameters = method.GetParameters();
        foreach (var param in parameters)
        {
            if (param.ParameterType == typeof(IEventStore)) continue;

            var descAttr = param.GetCustomAttribute<DescriptionAttribute>();
            var property = new ToolParameterProperty
            {
                Type = GetJsonSchemaType(param.ParameterType),
                Description = descAttr?.Description ?? string.Empty
            };

            if (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() == typeof(List<>))
            {
                property.Items = new ToolParameterProperty
                {
                    Type = GetJsonSchemaType(param.ParameterType.GetGenericArguments()[0])
                };
            }

            schema.Properties[param.Name!] = property;

            if (!param.IsOptional)
            {
                schema.Required.Add(param.Name!);
            }
        }

        return schema;
    }

    private string GetJsonSchemaType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(double) || type == typeof(float)) return "number";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "array";
        return "object";
    }

    public async Task RunAsync()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        while (true)
        {
            try
            {
                var line = await Console.In.ReadLineAsync();
                if (line == null) break;

                try
                {
                    var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, jsonOptions);
                    if (request == null) continue;

                    // Validate JSON-RPC 2.0 request
                    if (request.JsonRpc != "2.0")
                    {
                        await SendErrorAsync(request.Id, -32600, "Invalid Request - missing jsonrpc 2.0");
                        continue;
                    }

                    var response = await HandleRequest(request);
                    if (response != null) // Only send response if we got one back
                    {
                        var responseJson = JsonSerializer.Serialize(response, jsonOptions);
                        await Console.Out.WriteLineAsync(responseJson);
                        await Console.Out.FlushAsync();
                    }
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"[ERROR] JSON Parse error: {ex.Message}");
                    await SendErrorAsync(null, -32700, "Parse error");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Internal error: {ex.Message}");
                    await SendErrorAsync(null, -32603, $"Internal error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Fatal error: {ex.Message}");
                break;
            }
        }
    }

    public async Task<Models.JsonRpcResponse?> HandleRequest(JsonRpcRequest request)
    {
        Console.Error.WriteLine($"[DEBUG] Received method: {request.Method} with id: {request.Id}");

        // Handle notifications (requests without id)
        if (request.Id == null)
        {
            switch (request.Method)
            {
                case "notifications/initialized":
                    Console.Error.WriteLine("[DEBUG] Received initialized notification");
                    return null; // Don't send response for notifications
                default:
                    Console.Error.WriteLine($"[DEBUG] Unknown notification method: {request.Method}");
                    return null;
            }
        }

        // Handle regular requests (with id)
        switch (request.Method)
        {
            case "initialize":
                return JsonRpcSuccessResponse.Create(request.Id, new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { }
                    },
                    serverInfo = new
                    {
                        name = "dynamic-consistency-boundary",
                        version = "1.0.0"
                    }
                });

            case "tools/list":
                return JsonRpcSuccessResponse.Create(request.Id, new { tools = _tools });

            case "tools/call":
                if (request.Params == null)
                {
                    return JsonRpcErrorResponse.Create(request.Id, -32602, "Invalid params - tool name required");
                }

                var toolCallParams = JsonSerializer.Deserialize<ToolCallParameters>(request.Params.ToString()!, _jsonOptions);
                if (toolCallParams == null || string.IsNullOrEmpty(toolCallParams.Name))
                {
                    return JsonRpcErrorResponse.Create(request.Id, -32602, "Invalid params - tool name required");
                }

                try
                {
                    Console.Error.WriteLine($"[DEBUG] Calling tool: {toolCallParams.Name} with args: {JsonSerializer.Serialize(toolCallParams.Arguments, _jsonOptions)}");
                    var result = await ExecuteTool(toolCallParams.Name, toolCallParams.Arguments ?? new Dictionary<string, object>());
                    Console.Error.WriteLine($"[DEBUG] Tool result: {JsonSerializer.Serialize(result, _jsonOptions)}");

                    return JsonRpcSuccessResponse.Create(request.Id, new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = result is string str ? str : JsonSerializer.Serialize(result, _jsonOptions)
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Tool execution error: {ex}");
                    return JsonRpcErrorResponse.Create(request.Id, -32603, ex.Message);
                }

            default:
                return JsonRpcErrorResponse.Create(request.Id, -32601, $"Method not found: {request.Method}");
        }
    }

    private async Task<object> ExecuteTool(string name, Dictionary<string, object> arguments)
    {
        switch (name)
        {
            case "QueryEvents":
                return await HandleQueryEvents(arguments);
            case "AppendEvent":
                return await HandleAppendEvent(arguments);
            case "GetCurrentPosition":
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
                Console.Error.WriteLine($"[DEBUG] Using ByEventTypeAndTags with eventType: {eventType}, tags: {JsonSerializer.Serialize(tags, _jsonOptions)}, matchAnyTag: {matchAnyTag}");
                queryBuilder.WithSpecification(EventFilterSpecification.ByEventTypeAndTags(eventType, tags, matchAnyTag));
            }
            else
            {
                Console.Error.WriteLine($"[DEBUG] Using ByEventType with eventType: {eventType}");
                queryBuilder.WithSpecification(EventFilterSpecification.ByEventType(eventType));
            }
        }
        else if (tags != null && tags.Any())
        {
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

        return formattedEvents;
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

        // Extract event properties
        var eventType = eventElement.GetProperty("eventType").GetString();
        var tags = eventElement.GetProperty("tags").EnumerateArray()
            .Select(t => new EntityTag(
                t.GetProperty("entity").GetString()!,
                t.GetProperty("id").GetString()!
            ))
            .ToList();
        var data = eventElement.GetProperty("data").GetRawText();

        // Create event with serialized data
        var @event = new Event(
            Guid.NewGuid().ToString(),
            0, // Position will be set by event store
            eventType!,
            DateTime.UtcNow,
            tags,
            data
        );

        var query = JsonSerializer.Deserialize<EventQuery>(queryElement.GetRawText(), _jsonOptions);
        var lastKnownPosition = positionElement.GetInt64();

        if (query == null)
        {
            throw new ArgumentException("Failed to deserialize query");
        }

        await _eventStore.AppendEventAsync(@event, query, lastKnownPosition);
        return new { success = true };
    }

    private async Task<object> HandleGetCurrentPosition()
    {
        return new { position = await _eventStore.GetCurrentPositionAsync() };
    }

    private async Task SendErrorAsync(object? id, int code, string message)
    {
        var error = JsonRpcErrorResponse.Create(id, code, message);
        Console.Error.WriteLine($"[DEBUG] Sending error response: {JsonSerializer.Serialize(error, _jsonOptions)}");
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(error, _jsonOptions));
        await Console.Out.FlushAsync();
    }
}


