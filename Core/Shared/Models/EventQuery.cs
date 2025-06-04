using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Domain.Shared.Models;

/// <summary>
/// Represents a query for retrieving events from the event store.
/// </summary>
public class EventQuery
{
    /// <summary>
    /// Gets the filters to apply to the query.
    /// </summary>
    public IReadOnlyList<EventFilterSpecification> Filters { get; }

    /// <summary>
    /// Gets the position to start reading from.
    /// </summary>
    public long? FromPosition { get; }

    /// <summary>
    /// Gets the maximum number of events to return.
    /// </summary>
    public int? PageSize { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="EventQuery"/> class.
    /// </summary>
    /// <param name="filters">The filters to apply to the query.</param>
    /// <param name="fromPosition">The position to start reading from.</param>
    /// <param name="pageSize">The maximum number of events to return.</param>
    [JsonConstructor]
    internal EventQuery(IReadOnlyList<EventFilterSpecification> filters, long? fromPosition, int? pageSize)
    {
        Filters = filters ?? Array.Empty<EventFilterSpecification>();
        FromPosition = fromPosition;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates a new builder for constructing an event query.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static Builder Create() => new();

    /// <summary>
    /// Builder for constructing event queries.
    /// </summary>
    public class Builder
    {
        private readonly List<EventFilterSpecification> _filters = new();
        private long? _fromPosition;
        private int? _pageSize;

        /// <summary>
        /// Adds a filter to the query.
        /// </summary>
        /// <param name="filter">The filter to add.</param>
        /// <returns>The builder instance.</returns>
        public Builder WithSpecification(EventFilterSpecification filter)
        {
            _filters.Add(filter);
            return this;
        }

        /// <summary>
        /// Sets the position to start reading from.
        /// </summary>
        /// <param name="position">The position to start reading from.</param>
        /// <returns>The builder instance.</returns>
        public Builder FromPosition(long position)
        {
            _fromPosition = position;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of events to return.
        /// </summary>
        /// <param name="pageSize">The maximum number of events to return.</param>
        /// <returns>The builder instance.</returns>
        public Builder WithPageSize(int pageSize)
        {
            if (pageSize <= 0)
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));
            
            _pageSize = pageSize;
            return this;
        }

        /// <summary>
        /// Builds the event query.
        /// </summary>
        /// <returns>A new event query instance.</returns>
        public EventQuery Build() => new(_filters, _fromPosition, _pageSize);
    }
} 