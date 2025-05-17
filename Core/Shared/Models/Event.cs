using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Domain.Shared.ValueObjects;

namespace Core.Domain.Shared.Models;

/// <summary>
/// Represents an event in the system.
/// </summary>
public class Event
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the position of the event in the event stream.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the tags associated with the event.
    /// </summary>
    public IReadOnlyList<EntityTag> Tags { get; }

    /// <summary>
    /// Gets the data associated with the event.
    /// </summary>
    [JsonIgnore]
    public object Data { get; }

    /// <summary>
    /// Gets the serialized data of the event.
    /// </summary>
    [JsonPropertyName("Data")]
    public string SerializedData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Event"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="position">The position of the event in the event stream.</param>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="timestamp">The timestamp when the event was created.</param>
    /// <param name="tags">The tags associated with the event.</param>
    /// <param name="data">The data associated with the event.</param>
    /// <exception cref="ArgumentException">Thrown when id, eventType, or data is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when tags is null.</exception>
    public Event(string id, long position, string eventType, DateTime timestamp, IReadOnlyList<EntityTag> tags, object data)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty", nameof(id));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        if (tags == null)
            throw new ArgumentNullException(nameof(tags));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        Id = id;
        Position = position;
        EventType = eventType;
        Timestamp = timestamp;
        Tags = tags;
        Data = data;
        SerializedData = JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Creates a new event with the specified tags.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="tags">The tags associated with the event.</param>
    /// <param name="data">The data associated with the event.</param>
    /// <returns>A new <see cref="Event"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when eventType is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when tags or data is null.</exception>
    public static Event CreateEventWithTags(string eventType, IReadOnlyList<EntityTag> tags, object data)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        if (tags == null)
            throw new ArgumentNullException(nameof(tags));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        return new Event(
            Guid.NewGuid().ToString(),
            0, // Position will be set by the event store
            eventType,
            DateTime.UtcNow,
            tags,
            data);
    }
} 