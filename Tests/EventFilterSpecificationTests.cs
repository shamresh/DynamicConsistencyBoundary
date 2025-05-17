using System;
using System.Collections.Generic;
using Xunit;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;

namespace Tests;

public class EventFilterSpecificationTests
{
    [Fact]
    public void ByEventType_WithValidEventType_ShouldSucceed()
    {
        // Arrange
        var eventType = "CourseDefined";

        // Act
        var spec = EventFilterSpecification.ByEventType(eventType);

        // Assert
        Assert.Equal(eventType, spec.EventType);
        Assert.Null(spec.Tags);
        Assert.False(spec.MatchAnyTag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ByEventType_WithInvalidEventType_ShouldThrowArgumentException(string? eventType)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByEventType(eventType!));
    }

    [Fact]
    public void ByTag_WithValidTag_ShouldSucceed()
    {
        // Arrange
        var tag = new EntityTag("student", "s2");

        // Act
        var spec = EventFilterSpecification.ByTag(tag);

        // Assert
        Assert.Null(spec.EventType);
        Assert.Single(spec.Tags!);
        Assert.Equal(tag, spec.Tags![0]);
        Assert.False(spec.MatchAnyTag);
    }

    [Fact]
    public void ByTag_WithNullTag_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EventFilterSpecification.ByTag(null!));
    }

    [Fact]
    public void ByTags_WithValidTags_ShouldSucceed()
    {
        // Arrange
        var tags = new List<EntityTag>
        {
            new("student", "s2"),
            new("course", "c2")
        };

        // Act
        var spec = EventFilterSpecification.ByTags(tags);

        // Assert
        Assert.Null(spec.EventType);
        Assert.Equal(tags, spec.Tags);
        Assert.False(spec.MatchAnyTag);
    }

    [Fact]
    public void ByTags_WithValidTagsAndMatchAny_ShouldSucceed()
    {
        // Arrange
        var tags = new List<EntityTag>
        {
            new("student", "s2"),
            new("course", "c2")
        };

        // Act
        var spec = EventFilterSpecification.ByTags(tags, matchAny: true);

        // Assert
        Assert.Null(spec.EventType);
        Assert.Equal(tags, spec.Tags);
        Assert.True(spec.MatchAnyTag);
    }

    [Fact]
    public void ByTags_WithNullTags_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByTags(null!));
    }

    [Fact]
    public void ByTags_WithEmptyTags_ShouldThrowArgumentException()
    {
        // Arrange
        var tags = new List<EntityTag>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByTags(tags));
    }

    [Fact]
    public void ByTags_WithNullTagInList_ShouldThrowArgumentException()
    {
        // Arrange
        var tags = new List<EntityTag>
        {
            new("student", "s2"),
            null!
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByTags(tags));
    }

    [Fact]
    public void ByEventTypeAndTags_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var eventType = "StudentSubscribed";
        var tags = new List<EntityTag>
        {
            new("student", "s2"),
            new("course", "c2")
        };

        // Act
        var spec = EventFilterSpecification.ByEventTypeAndTags(eventType, tags);

        // Assert
        Assert.Equal(eventType, spec.EventType);
        Assert.Equal(tags, spec.Tags);
        Assert.False(spec.MatchAnyTag);
    }

    [Fact]
    public void ByEventTypeAndTags_WithValidParametersAndMatchAny_ShouldSucceed()
    {
        // Arrange
        var eventType = "StudentSubscribed";
        var tags = new List<EntityTag>
        {
            new("student", "s2"),
            new("course", "c2")
        };

        // Act
        var spec = EventFilterSpecification.ByEventTypeAndTags(eventType, tags, matchAny: true);

        // Assert
        Assert.Equal(eventType, spec.EventType);
        Assert.Equal(tags, spec.Tags);
        Assert.True(spec.MatchAnyTag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ByEventTypeAndTags_WithInvalidEventType_ShouldThrowArgumentException(string? eventType)
    {
        // Arrange
        var tags = new List<EntityTag> { new("student", "s2") };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByEventTypeAndTags(eventType!, tags));
    }

    [Fact]
    public void ByEventTypeAndTags_WithNullTags_ShouldThrowArgumentException()
    {
        // Arrange
        var eventType = "StudentSubscribed";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByEventTypeAndTags(eventType, null!));
    }

    [Fact]
    public void ByEventTypeAndTags_WithEmptyTags_ShouldThrowArgumentException()
    {
        // Arrange
        var eventType = "StudentSubscribed";
        var tags = new List<EntityTag>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByEventTypeAndTags(eventType, tags));
    }

    [Fact]
    public void ByEventTypeAndTags_WithNullTagInList_ShouldThrowArgumentException()
    {
        // Arrange
        var eventType = "StudentSubscribed";
        var tags = new List<EntityTag>
        {
            new("student", "s2"),
            null!
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventFilterSpecification.ByEventTypeAndTags(eventType, tags));
    }
} 