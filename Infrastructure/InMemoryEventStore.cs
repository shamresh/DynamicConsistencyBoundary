using Core.Domain.Shared.Models;
using Core.Domain.Shared.Interfaces;

namespace Infrastructure;

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
                _currentPosition,  // Use current position for storage
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

            // Apply filters
            if (query.Filters.Any())
            {
                events = events.Where(e => query.Filters.All(filter =>
                {
                    // First check event type if specified
                    if (filter.EventType != null && filter.EventType != e.EventType)
                        return false;

                    // Then check tags if specified
                    if (filter.Tags != null && filter.Tags.Any())
                    {
                        if (filter.MatchAnyTag)
                        {
                            // Match any tag
                            return filter.Tags.Any(tag => e.Tags.Contains(tag));
                        }
                        else
                        {
                            // Match all tags
                            return filter.Tags.All(tag => e.Tags.Contains(tag));
                        }
                    }

                    // If we get here, either:
                    // 1. No event type was specified (filter.EventType == null)
                    // 2. Event type matched (filter.EventType == e.EventType)
                    // 3. No tags were specified (filter.Tags == null || !filter.Tags.Any())
                    return true;
                }));
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