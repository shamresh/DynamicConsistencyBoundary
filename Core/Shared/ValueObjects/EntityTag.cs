namespace Core.Domain.Shared.ValueObjects;

/// <summary>
/// Represents a tag that can be associated with an entity.
/// </summary>
public class EntityTag
{
    /// <summary>
    /// Gets the entity type of the tag.
    /// </summary>
    public string Entity { get; }

    /// <summary>
    /// Gets the identifier of the tag.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityTag"/> class.
    /// </summary>
    /// <param name="entity">The entity type.</param>
    /// <param name="id">The identifier.</param>
    /// <exception cref="ArgumentException">Thrown when entity or id is null or empty.</exception>
    public EntityTag(string entity, string id)
    {
        if (string.IsNullOrWhiteSpace(entity))
            throw new ArgumentException("Entity cannot be empty", nameof(entity));

        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty", nameof(id));

        Entity = entity;
        Id = id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is EntityTag other)
        {
            return Entity == other.Entity && Id == other.Id;
        }
        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Entity, Id);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Entity}:{Id}";
    }
} 