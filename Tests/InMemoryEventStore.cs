using Core.Domain.Shared.Models;
using Core.Domain.Shared.Interfaces;

namespace Tests;

/// <summary>
/// An in-memory implementation of <see cref="IEventStore"/> for testing purposes.
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly List<Event> _events = new();
    private long _currentPosition;
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task AppendEventAsync(
        Event @event,
        EventQuery query,
        long lastKnownPosition,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (lastKnownPosition != _currentPosition)
                throw new InvalidOperationException("Concurrent modification detected");

            // Create a new event with the correct position
            var eventWithPosition = new Event(
                @event.Id,
                _currentPosition,
                @event.EventType,
                @event.Timestamp,
                @event.Tags,
                @event.Data);

            _events.Add(eventWithPosition);
            _currentPosition++;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Event>> QueryEventsAsync(
        EventQuery query,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Start with all events ordered by position
            var events = _events.OrderBy(e => e.Position).AsEnumerable();

            // Apply tag and event type filters first
            if (query.Filters.Any())
            {
                events = events.Where(e =>
                    query.Filters.All(f =>
                        (f.EventType == null || f.EventType == e.EventType) &&
                        (f.Tags == null || f.Tags.All(tag => e.Tags.Contains(tag)))));
            }

            // Then apply position filter
            if (query.FromPosition.HasValue)
                events = events.Where(e => e.Position >= query.FromPosition.Value);

            // Finally apply pagination
            if (query.PageSize.HasValue)
                events = events.Take(query.PageSize.Value);

            return Task.FromResult<IReadOnlyList<Event>>(events.ToList());
        }
    }

    /// <inheritdoc />
    public Task<long> GetCurrentPositionAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_currentPosition);
        }
    }

    public void Flush()
    {
        // No-op for in-memory implementation
    }

    public void Dispose()
    {
        _events.Clear();
        _currentPosition = 0;
    }

} 