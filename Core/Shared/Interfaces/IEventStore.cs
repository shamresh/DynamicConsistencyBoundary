using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;

namespace Core.Domain.Shared.Interfaces
{
    public interface IEventStore
    {
        /// <summary>
        /// Appends an event to the event store
        /// </summary>
        /// <param name="event">The event to append</param>
        /// <param name="query">The query context for the event</param>
        /// <param name="lastKnownPosition">The last known position for optimistic concurrency</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AppendEventAsync(Event @event, EventQuery query, long lastKnownPosition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries events from the event store based on the provided query
        /// </summary>
        /// <param name="query">The query to filter events</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of events matching the query</returns>
        Task<IReadOnlyList<Event>> QueryEventsAsync(EventQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current position in the event store
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The current position</returns>
        Task<long> GetCurrentPositionAsync(CancellationToken cancellationToken = default);
    }
}
