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

    [Fact]
    public void Create_WithMultipleTags_ShouldSucceed()
    {
        // Arrange
        var eventType = "CourseEnrollment";
        var tags = new List<EntityTag>
        {
            new("student", "s1"),
            new("course", "c1"),
            new("semester", "2024-1")
        };
        var data = new { EnrollmentDate = DateTime.UtcNow, Grade = "A" };

        // Act
        var @event = Event.CreateEventWithTags(eventType, tags, data);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Equal(3, @event.Tags.Count);
        Assert.Contains(new EntityTag("student", "s1"), @event.Tags);
        Assert.Contains(new EntityTag("course", "c1"), @event.Tags);
        Assert.Contains(new EntityTag("semester", "2024-1"), @event.Tags);
        Assert.Equal(data, @event.Data);
    }

    [Fact]
    public void Create_WithDuplicateTags_ShouldSucceed()
    {
        // Arrange
        var eventType = "GradeUpdated";
        var tags = new List<EntityTag>
        {
            new("student", "s1"),
            new("student", "s1"), // Duplicate tag
            new("course", "c1")
        };
        var data = new { OldGrade = "B", NewGrade = "A" };

        // Act
        var @event = Event.CreateEventWithTags(eventType, tags, data);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Equal(3, @event.Tags.Count); // Should still include duplicates
        Assert.Equal(2, @event.Tags.Count(t => t.Entity == "student" && t.Id == "s1")); // Should have 2 student tags
        Assert.Equal(data, @event.Data);
    }

    [Fact]
    public void Create_WithComplexEventType_ShouldSucceed()
    {
        // Arrange
        var eventType = "Student.Course.Enrollment.Completed";
        var tags = new List<EntityTag>
        {
            new("student", "s1"),
            new("course", "c1"),
            new("department", "CS"),
            new("semester", "2024-1")
        };
        var data = new 
        { 
            EnrollmentDate = DateTime.UtcNow,
            Grade = "A",
            Credits = 3,
            IsFullTime = true
        };

        // Act
        var @event = Event.CreateEventWithTags(eventType, tags, data);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Equal(4, @event.Tags.Count);
        Assert.Contains(new EntityTag("student", "s1"), @event.Tags);
        Assert.Contains(new EntityTag("course", "c1"), @event.Tags);
        Assert.Contains(new EntityTag("department", "CS"), @event.Tags);
        Assert.Contains(new EntityTag("semester", "2024-1"), @event.Tags);
        Assert.Equal(data, @event.Data);
    }

    [Fact]
    public void Create_WithEmptyTagId_ShouldThrowArgumentException()
    {
        // Arrange
        var eventType = "SystemEvent";
        var tags = new List<EntityTag>
        {
            new("system", "system1"), // Valid tag
            new("component", "auth")
        };
        var data = new { Message = "System maintenance" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EntityTag("system", "")); // Should throw for empty ID
        var @event = Event.CreateEventWithTags(eventType, tags, data); // This should succeed with valid tags

        // Verify the event was created correctly with valid tags
        Assert.False(string.IsNullOrWhiteSpace(@event.Id));
        Assert.Equal(eventType, @event.EventType);
        Assert.Equal(2, @event.Tags.Count);
        Assert.Contains(new EntityTag("system", "system1"), @event.Tags);
        Assert.Contains(new EntityTag("component", "auth"), @event.Tags);
        Assert.Equal(data, @event.Data);
    }
} 