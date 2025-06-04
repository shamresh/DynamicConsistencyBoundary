using System;
using System.Collections.Generic;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;

namespace Infrastructure;
/// <summary>
/// A builder class for creating test data for the in-memory event store.
/// </summary>
public class InMemoryEventStoreBuilder
{
    private readonly List<Event> _events = new();
    private long _currentPosition;

    /// <summary>
    /// Adds an event to the builder.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="data">The event data.</param>
    /// <param name="tags">The tags to associate with the event.</param>
    /// <returns>The builder instance.</returns>
    public InMemoryEventStoreBuilder WithEvent(string eventType, object data, params EntityTag[] tags)
    {
        var @event = new Event(
            Guid.NewGuid().ToString(),
            _currentPosition,
            eventType,
            DateTime.UtcNow,
            tags,
            data);

        _events.Add(@event);
        _currentPosition++;

        return this;
    }

    /// <summary>
    /// Builds an in-memory event store with the configured events.
    /// </summary>
    /// <returns>A new <see cref="InMemoryEventStore"/> instance.</returns>
    public InMemoryEventStore Build()
    {
        var store = new InMemoryEventStore();
        foreach (var @event in _events)
        {
            store.AppendEventAsync(@event, EventQuery.Create().Build(), @event.Position).Wait();
        }
        return store;
    }
} 