using Core.Domain.Shared.ValueObjects;
using System.Text.Json.Serialization;

namespace Core.Domain.Shared.Models;

/// <summary>
/// Represents the criteria for querying events in the event store.
/// This class defines the specification for filtering events based on event type and entity tags.
/// </summary>
public class EventFilterSpecification
{
    /// <summary>
    /// Gets the event type to filter by.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string? EventType { get; }

    /// <summary>
    /// Gets the tags to filter by.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<EntityTag>? Tags { get; }

    /// <summary>
    /// Gets whether to match any of the tags (true) or all of them (false).
    /// </summary>
    [JsonPropertyName("matchAnyTag")]
    public bool MatchAnyTag { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventFilterSpecification"/> class.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="tags">The tags to filter by.</param>
    /// <param name="matchAnyTag">Whether to match any of the tags (true) or all of them (false).</param>
    [JsonConstructor]
    public EventFilterSpecification(string? eventType, IReadOnlyList<EntityTag>? tags, bool matchAnyTag = false)
    {
        EventType = eventType;
        Tags = tags;
        MatchAnyTag = matchAnyTag;
    }

    /// <summary>
    /// Creates a criteria for filtering by event type.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <returns>A new <see cref="EventFilterSpecification"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when event type is null or empty.</exception>
    public static EventFilterSpecification ByEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        return new EventFilterSpecification(eventType, null);
    }

    /// <summary>
    /// Creates a criteria for filtering by a single tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>A new <see cref="EventFilterSpecification"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tag is null.</exception>
    public static EventFilterSpecification ByTag(EntityTag tag)
    {
        if (tag == null)
            throw new ArgumentNullException(nameof(tag));

        return new EventFilterSpecification(null, new[] { tag });
    }

    /// <summary>
    /// Creates a criteria for filtering by multiple tags.
    /// </summary>
    /// <param name="tags">The tags to filter by.</param>
    /// <param name="matchAny">Whether to match any of the tags (true) or all of them (false).</param>
    /// <returns>A new <see cref="EventFilterSpecification"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when tags is null or empty.</exception>
    public static EventFilterSpecification ByTags(IReadOnlyList<EntityTag> tags, bool matchAny = false)
    {
        if (tags == null || tags.Count == 0)
            throw new ArgumentException("Tags cannot be empty", nameof(tags));

        if (tags.Any(t => t == null))
            throw new ArgumentException("Tags cannot contain null values", nameof(tags));

        return new EventFilterSpecification(null, tags, matchAny);
    }

    /// <summary>
    /// Creates a criteria that combines event type and tags filtering.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="tags">The tags to filter by.</param>
    /// <param name="matchAny">Whether to match any of the tags (true) or all of them (false).</param>
    /// <returns>A new <see cref="EventFilterSpecification"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when event type is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when tags is null or empty.</exception>
    public static EventFilterSpecification ByEventTypeAndTags(string eventType, IReadOnlyList<EntityTag> tags, bool matchAny = false)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        if (tags == null || tags.Count == 0)
            throw new ArgumentException("Tags cannot be empty", nameof(tags));

        if (tags.Any(t => t == null))
            throw new ArgumentException("Tags cannot contain null values", nameof(tags));

        return new EventFilterSpecification(eventType, tags, matchAny);
    }
} 