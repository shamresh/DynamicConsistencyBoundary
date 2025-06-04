using System.Text.Json;
using Core.Domain.Shared.Interfaces;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using DynamicConsistencyBoundary.McpServer;
using DynamicConsistencyBoundary.McpServer.Models;
using Moq;
using Xunit;

namespace DynamicConsistencyBoundary.Tests;

public class McpServerTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly McpServer.McpServer _server;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServerTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
        _server = new McpServer.McpServer(_eventStoreMock.Object);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetCurrentPosition_ReturnsCorrectPosition()
    {
        // Arrange
        var expectedPosition = 42L;
        _eventStoreMock.Setup(x => x.GetCurrentPositionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(expectedPosition));

        var toolCallParams = new ToolCallParameters
        {
            Tool = "get_current_position",
            Parameters = new Dictionary<string, object>()
        };

        var request = new JsonRpcRequest
        {
            Id = "1",
            Method = "mcp/execute",
            Params = JsonSerializer.Serialize(toolCallParams, _jsonOptions)
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("1", response.Id);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.Equal(expectedPosition.ToString(), response.Result.Data.Text);
    }

    [Fact]
    public async Task QueryEvents_WithValidParameters_ReturnsEvents()
    {
        // Arrange
        IReadOnlyList<Event> expectedEvents = Array.Empty<Event>();
        _eventStoreMock.Setup(x => x.QueryEventsAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(expectedEvents));

        var queryParams = new QueryEventsParameters
        {
            EventType = "TestEvent",
            PageSize = 10
        };

        var toolCallParams = new ToolCallParameters
        {
            Tool = "query_events",
            Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(queryParams, _jsonOptions), _jsonOptions)!
        };

        var request = new JsonRpcRequest
        {
            Id = "1",
            Method = "mcp/execute",
            Params = JsonSerializer.Serialize(toolCallParams, _jsonOptions)
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("1", response.Id);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.Equal(JsonSerializer.Serialize(expectedEvents, _jsonOptions), response.Result.Data.Text);
    }

    [Fact]
    public async Task AppendEvent_WithValidParameters_AppendsEvent()
    {
        // Arrange
        var eventId = "test-id";
        var eventType = "TestEvent";
        var eventData = new { TestProperty = "TestValue" };
        var serializedData = JsonSerializer.Serialize(eventData, _jsonOptions);
        var query = EventQuery.Create().Build();
        var lastKnownPosition = 0L; // Start with position 0 for the first event

        var appendParams = new AppendEventParameters
        {
            Event = new Event(eventId, 1, eventType, DateTime.UtcNow, new List<EntityTag>(), serializedData),
            Query = query,
            LastKnownPosition = lastKnownPosition
        };

        var toolCallParams = new ToolCallParameters
        {
            Tool = "append_event",
            Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(appendParams, _jsonOptions), _jsonOptions)!
        };

        var request = new JsonRpcRequest
        {
            Id = "1",
            Method = "mcp/execute",
            Params = JsonSerializer.Serialize(toolCallParams, _jsonOptions)
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("1", response.Id);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.Equal("true", response.Result.Data.Text);

        // Verify the event store was called with the correct event properties
        _eventStoreMock.Verify(x => x.AppendEventAsync(
            It.Is<Event>(e => 
                e.Id == eventId && 
                e.EventType == eventType &&
                e.SerializedData == serializedData),
            It.IsAny<EventQuery>(),
            lastKnownPosition,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_WithInvalidMethod_ReturnsError()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            Id = "1",
            Method = "invalid_method",
            Params = null
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("1", response.Id);
        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error.Code);
        Assert.Contains("Method not found: invalid_method", response.Error.Message);
    }
} 