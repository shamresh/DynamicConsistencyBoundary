using System;
using System.Collections.Generic;
using Xunit;
using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;

namespace Tests;

public class EventTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var eventType = "StudentRegistered";
        var tags = new List<EntityTag> { new("student", "s1") };
        var data = new { CourseId = "c1", RegistrationDate = DateTime.UtcNow };

        // Act
        var @event = Event.CreateEventWithTags(eventType, tags, data);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Equal(tags, @event.Tags);
        Assert.Equal(data, @event.Data);
        Assert.True(@event.Timestamp <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidEventType_ShouldThrowException(string? eventType)
    {
        // Arrange
        var tags = new List<EntityTag> { new("student", "s1") };
        var data = new { CourseId = "c1", RegistrationDate = DateTime.UtcNow };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Event.CreateEventWithTags(eventType!, tags, data));
    }

    [Fact]
    public void Create_WithNullTags_ShouldThrowArgumentNullException()
    {
        // Arrange
        var eventType = "StudentRegistered";
        IReadOnlyList<EntityTag> tags = null!;
        var data = new { CourseId = "c1", RegistrationDate = DateTime.UtcNow };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Event.CreateEventWithTags(eventType, tags, data));
    }

    [Fact]
    public void Create_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var eventType = "StudentRegistered";
        var tags = new List<EntityTag> { new("student", "s1") };
        object data = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Event.CreateEventWithTags(eventType, tags, data));
    }

    [Fact]
    public void Create_WithEmptyTags_ShouldSucceed()
    {
        // Arrange
        var eventType = "CourseDefined";
        var tags = new List<EntityTag>();
        var data = new { CourseId = "c2", StartDate = DateTime.UtcNow };

        // Act
        var @event = Event.CreateEventWithTags(eventType, tags, data);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Empty(@event.Tags);
        Assert.Equal(data, @event.Data);
        Assert.True(@event.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithSingleTag_ShouldSucceed()
    {
        // Arrange
        var eventType = "StudentSubscribed";
        var tag = new EntityTag("course", "c2");
        var data = new { StudentId = "s2", SubscriptionDate = DateTime.UtcNow };

        // Act
        var @event = Event.CreateEventWithTags(eventType, new[] { tag }, data);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Single(@event.Tags);
        Assert.Equal(tag, @event.Tags[0]);
        Assert.Equal(data, @event.Data);
        Assert.True(@event.Timestamp <= DateTime.UtcNow);
    }
} 