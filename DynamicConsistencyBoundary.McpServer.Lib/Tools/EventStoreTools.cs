using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Shared.Interfaces;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using DynamicConsistencyBoundary.McpServer.Models;

namespace DynamicConsistencyBoundary.McpServer.Tools;

[McpServerToolType]
public static class EventStoreTools
{
    [McpServerTool, Description("""
        Query events from the event store using various filters.
        
        Examples:
        1. Find all events for a student:
           ```json
           {
             "tags": [
               {
                 "entity": "student",
                 "id": "s1"
               }
             ]
           }
           ```
        
        2. Find student enrollments in a class:
           ```json
           {
             "eventType": "StudentEnrolled",
             "tags": [
               {
                 "entity": "class",
                 "id": "c1"
               }
             ]
           }
           ```
        """)]
    public static async Task<object> QueryEvents(
        IEventStore eventStore,
        [Description("Filter events by type")] string? eventType = null,
        [Description("Filter events by tags")] List<EntityTag>? tags = null,
        [Description("Whether to match any tag (true) or all tags (false)")] bool matchAnyTag = false,
        [Description("Start reading from this position")] long? fromPosition = null,
        [Description("Maximum number of events to return")] int? pageSize = null)
    {
        var queryBuilder = EventQuery.Create();

        if (!string.IsNullOrEmpty(eventType))
        {
            if (tags != null && tags.Any())
            {
                queryBuilder.WithSpecification(EventFilterSpecification.ByEventTypeAndTags(eventType, tags, matchAnyTag));
            }
            else
            {
                queryBuilder.WithSpecification(EventFilterSpecification.ByEventType(eventType));
            }
        }
        else if (tags != null && tags.Any())
        {
            queryBuilder.WithSpecification(EventFilterSpecification.ByTags(tags, matchAnyTag));
        }

        if (fromPosition.HasValue)
        {
            queryBuilder.FromPosition(fromPosition.Value);
        }

        if (pageSize.HasValue)
        {
            queryBuilder.WithPageSize(pageSize.Value);
        }

        var query = queryBuilder.Build();
        var events = await eventStore.QueryEventsAsync(query);
        
        return events.Select(e => new
        {
            e.Id,
            e.Position,
            e.EventType,
            e.Timestamp,
            Tags = e.Tags.Select(t => new { entity = t.Entity, id = t.Id }).ToList(),
            Data = e.SerializedData
        });
    }

    [McpServerTool, Description("""
        Append a new event to the event store.
        
        Examples:
        1. Register a new student:
           ```json
           {
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
           ```
        """)]
    public static async Task<object> AppendEvent(
        IEventStore eventStore,
        [Description("The event to append")] Event @event,
        [Description("Query context for the event")] EventQuery query,
        [Description("Last known position for optimistic concurrency")] long lastKnownPosition)
    {
        await eventStore.AppendEventAsync(@event, query, lastKnownPosition);
        return new { success = true };
    }

    [McpServerTool, Description("Get the current position in the event store")]
    public static async Task<object> GetCurrentPosition(IEventStore eventStore)
    {
        return new { position = await eventStore.GetCurrentPositionAsync() };
    }
} 