using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using Xunit;
using Core.Domain.Shared.Interfaces;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using Infrastructure;

    
namespace Tests.EventStore
{
    [Collection("EventStore")]
    public class EventStoreContractAndPatternTests
    {
        private IEventStore _store;

        public EventStoreContractAndPatternTests()
        {
            _store = new InMemoryEventStore();
        }

        [Fact]
        public async Task EventHandler_ShouldResumeFromLastPosition()
        {
            // Arrange
            var studentTag = new EntityTag("Student", "STU-2024-001");
            var handlerTag = new EntityTag("AcademicNotification", "NotificationHandlerId");
            var events = new[]
            {
                Event.CreateEventWithTags("StudentRegistered", new[] { studentTag, handlerTag }, new { Name = "John Doe", Email = "john@example.com" }),
                Event.CreateEventWithTags("StudentProfileUpdated", new[] { studentTag, handlerTag }, new { Name = "John Doe", Phone = "123-456-7890" }),
                Event.CreateEventWithTags("StudentGraduated", new[] { studentTag, handlerTag }, new { GraduationDate = "2024-05-15", GPA = 3.8 })
            };

            // Append events to store
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act - Simulate handler processing up to position 1
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(handlerTag))
                .FromPosition(2)  // Resume from position 2
                .Build();
            var remainingEvents = await _store.QueryEventsAsync(query);

            // Assert
            Assert.Single(remainingEvents);
            Assert.Equal("StudentGraduated", remainingEvents[0].EventType);
        }

        [Fact]
        public async Task Projection_ShouldUpdateFromLastProcessedPosition()
        {
            // Arrange
            var studentTag = new EntityTag("Student", "STU-2024-002");
            var courseTag = new EntityTag("Course", "CS-2024-002");
            var projectionTag = new EntityTag("Projection", "CourseEnrollment");
            var events = new[]
            {
                Event.CreateEventWithTags("CourseEnrolled", new[] { courseTag, studentTag, projectionTag }, new { CourseId = "CS102", StudentId = "STU2", EnrollmentDate = "2024-01-15" }),
                Event.CreateEventWithTags("CourseGradeUpdated", new[] { courseTag, studentTag, projectionTag }, new { CourseId = "CS102", StudentId = "STU2", Grade = "A" }),
                Event.CreateEventWithTags("CourseDropped", new[] { courseTag, studentTag, projectionTag }, new { CourseId = "CS102", StudentId = "STU2", DropDate = "2024-02-01" })
            };

            // Append events to store
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act - Simulate projection update from position 1
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(projectionTag))
                .FromPosition(2)  // Start from position 2
                .Build();
            var newEvents = await _store.QueryEventsAsync(query);

            // Assert
            Assert.Single(newEvents);
            Assert.Equal("CourseDropped", newEvents[0].EventType);
        }

        [Fact]
        public async Task EventSubscriber_ShouldCatchUpOnMissedEvents()
        {
            // Arrange
            var courseTag = new EntityTag("Course", "CS-2024-003");
            var subscriberTag = new EntityTag("Subscriber", "AcademicRecords");
            var events = new[]
            {
                Event.CreateEventWithTags("CourseCreated", new[] { courseTag, subscriberTag }, new { CourseId = "CS103", Title = "Introduction to Programming" }),
                Event.CreateEventWithTags("CourseScheduleUpdated", new[] { courseTag, subscriberTag }, new { CourseId = "CS103", Schedule = "MWF 10:00-11:30" }),
                Event.CreateEventWithTags("CourseCancelled", new[] { courseTag, subscriberTag }, new { CourseId = "CS103", Reason = "Insufficient Enrollment" })
            };

            // Append events to store
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act - Simulate subscriber catching up from position 1
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(subscriberTag))
                .FromPosition(2)  // Catch up from position 2
                .Build();
            var missedEvents = await _store.QueryEventsAsync(query);

            // Assert
            Assert.Single(missedEvents);
            Assert.Equal("CourseCancelled", missedEvents[0].EventType);
        }

        [Fact]
        public async Task Pagination_ShouldHandleLargeEventStreams()
        {
            // Arrange
            var studentTag = new EntityTag("Student", "STU-2024-004");
            var courseTag = new EntityTag("Course", "CS-2024-004");
            var pageSize = 2;
            var events = new List<Event>();
            for (int i = 0; i < 5; i++)
            {
                events.Add(Event.CreateEventWithTags(
                    "CourseModuleCompleted", 
                    new[] { courseTag, studentTag }, 
                    new { 
                        CourseId = "CS104", 
                        StudentId = "STU4", 
                        ModuleNumber = i + 1, 
                        CompletionDate = $"2024-0{i+1}-15" 
                    }
                ));
            }

            // Append events to store
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act - Get first page
            var firstPageQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(courseTag))
                .FromPosition(0)
                .WithPageSize(pageSize)
                .Build();
            var firstPage = await _store.QueryEventsAsync(firstPageQuery);

            // Act - Get second page
            var secondPageQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(courseTag))
                .FromPosition(pageSize)
                .WithPageSize(pageSize)
                .Build();
            var secondPage = await _store.QueryEventsAsync(secondPageQuery);

            // Assert
            Assert.Equal(pageSize, firstPage.Count);
            Assert.Equal("CourseModuleCompleted", firstPage[0].EventType);
            Assert.Equal("CourseModuleCompleted", firstPage[1].EventType);

            Assert.Equal(pageSize, secondPage.Count);
            Assert.Equal("CourseModuleCompleted", secondPage[0].EventType);
            Assert.Equal("CourseModuleCompleted", secondPage[1].EventType);
        }

        [Fact]
        public async Task EventProcessing_ShouldHandleEmptyResultSet()
        {
            // Arrange
            var courseTag = new EntityTag("Course", "CS-2024-005");
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(courseTag))
                .FromPosition(100)  // Position beyond available events
                .Build();

            // Act
            var events = await _store.QueryEventsAsync(query);

            // Assert
            Assert.Empty(events);
        }

        [Fact]
        public async Task EventProcessing_ShouldMaintainOrderWithPositionBasedFiltering()
        {
            // Arrange
            var courseTag = new EntityTag("Course", "CS-2024-006");
            var events = new[]
            {
                Event.CreateEventWithTags("CoursePrerequisitesUpdated", new[] { courseTag }, new { CourseId = "CS106", Prerequisites = new[] { "CS100" } }),
                Event.CreateEventWithTags("CourseCapacityUpdated", new[] { courseTag }, new { CourseId = "CS106", MaxStudents = 30 }),
                Event.CreateEventWithTags("CourseInstructorAssigned", new[] { courseTag }, new { CourseId = "CS106", InstructorId = "PROF123" })
            };

            // Append events to store
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act - Get events from position 1
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(courseTag))
                .FromPosition(1)
                .Build();
            var filteredEvents = await _store.QueryEventsAsync(query);

            // Assert
            Assert.Equal(2, filteredEvents.Count);
            Assert.Equal("CourseCapacityUpdated", filteredEvents[0].EventType);
            Assert.Equal("CourseInstructorAssigned", filteredEvents[1].EventType);
            Assert.True(filteredEvents[0].Position < filteredEvents[1].Position);
        }

        [Fact]
        public async Task AcademicEnrollment_EventStream_ShouldReflectDomainScenario()
        {
            // Arrange: Append events as per the diagram
            var events = new[]
            {
                Event.CreateEventWithTags("CourseDefined", new[] { new EntityTag("course", "c7-1") }, new { CourseId = "c7-1", Title = "Math" }),
                Event.CreateEventWithTags("StudentRegistered", new[] { new EntityTag("student", "s7-1") }, new { StudentId = "s7-1", Name = "Alice" }),
                Event.CreateEventWithTags("StudentRegistered", new[] { new EntityTag("student", "s7-2") }, new { StudentId = "s7-2", Name = "Bob" }),
                Event.CreateEventWithTags("CourseDefined", new[] { new EntityTag("course", "c7-2") }, new { CourseId = "c7-2", Title = "Science" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s7-1"), new EntityTag("course", "c7-1") }, new { StudentId = "s7-1", CourseId = "c7-1" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s7-2"), new EntityTag("course", "c7-1") }, new { StudentId = "s7-2", CourseId = "c7-1" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s7-1"), new EntityTag("course", "c7-2") }, new { StudentId = "s7-1", CourseId = "c7-2" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s7-2"), new EntityTag("course", "c7-2") }, new { StudentId = "s7-2", CourseId = "c7-2" })
            };

            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act: Query for all subscriptions of student s2
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(new EntityTag("student", "s7-2")))
                .Build();
            var s2Events = await _store.QueryEventsAsync(query);

            // Assert: s2 should have registered and subscribed to two courses
            Assert.Equal(3, s2Events.Count); // Registered + 2 subscriptions
            Assert.Contains(s2Events, e => e.EventType == "StudentRegistered");
            Assert.Equal(2, s2Events.Count(e => e.EventType == "StudentSubscribed"));
            // Assert: s7-2 should have subscribed to two courses
            Assert.Equal(1, s2Events.Count(e => e.EventType == "StudentSubscribed" && e.Tags.Any(t => t.Entity == "course" && t.Id == "c7-1")));
            Assert.Equal(1, s2Events.Count(e => e.EventType == "StudentSubscribed" && e.Tags.Any(t => t.Entity == "course" && t.Id == "c7-2")));
        }

        [Fact]
        public async Task Should_Query_StudentAndCourse_ConsistencyBoundary_And_Verify_Subscription_State()
        {
            // Arrange: Use the same event stream as above
            var events = new[]
            {
                Event.CreateEventWithTags("CourseDefined", new[] { new EntityTag("course", "c8-1") }, new { CourseId = "c8-1", Title = "Math" }),
                Event.CreateEventWithTags("StudentRegistered", new[] { new EntityTag("student", "s8-1") }, new { StudentId = "s8-1", Name = "Alice" }),
                Event.CreateEventWithTags("StudentRegistered", new[] { new EntityTag("student", "s8-2") }, new { StudentId = "s8-2", Name = "Bob" }),
                Event.CreateEventWithTags("CourseDefined", new[] { new EntityTag("course", "c8-2") }, new { CourseId = "c8-2", Title = "Science" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s8-1"), new EntityTag("course", "c8-1") }, new { StudentId = "s8-1", CourseId = "c8-1" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s8-2"), new EntityTag("course", "c8-1") }, new { StudentId = "s8-2", CourseId = "c8-1" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s8-1"), new EntityTag("course", "c8-2") }, new { StudentId = "s8-1", CourseId = "c8-2" }),
                Event.CreateEventWithTags("StudentSubscribed", new[] { new EntityTag("student", "s8-2"), new EntityTag("course", "c8-2") }, new { StudentId = "s8-2", CourseId = "c8-2" })
            };
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act: DCB Query for student:s2 and course:c2
            var dcbQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(new EntityTag("student", "s8-2")))
                .WithSpecification(EventFilterSpecification.ByTag(new EntityTag("course", "c8-2")))
                .Build();
            var dcbEvents = await _store.QueryEventsAsync(dcbQuery);

            // In-memory decision model:
            bool studentExists = events.Any(e => e.EventType == "StudentRegistered" && e.Tags.Any(t => t.Entity == "student" && t.Id == "s8-2"));
            bool courseExists = events.Any(e => e.EventType == "CourseDefined" && e.Tags.Any(t => t.Entity == "course" && t.Id == "c8-2"));
            int studentCourseCount = events.Count(e => e.EventType == "StudentSubscribed" && e.Tags.Any(t => t.Entity == "student" && t.Id == "s8-2"));
            int courseStudentCount = events.Count(e => e.EventType == "StudentSubscribed" && e.Tags.Any(t => t.Entity == "course" && t.Id == "c8-2"));

            // Assert
            Assert.True(studentExists);
            Assert.True(courseExists);
            Assert.Equal(2, studentCourseCount); // s8-2 subscribed to 2 courses
            Assert.Equal(2, courseStudentCount); // c8-2 has 2 students
            Assert.Single(dcbEvents); // Only the StudentSubscribed event for s8-2/c8-2
            Assert.Equal("StudentSubscribed", dcbEvents[0].EventType);
        }

        [Fact]
        public async Task QueryEvents_FilteringScenarios_ShouldReturnExpectedResults()
        {
            var s1 = new EntityTag("student", "s1");
            var s2 = new EntityTag("student", "s2");
            var c1 = new EntityTag("class", "c1");
            var c2 = new EntityTag("class", "c2");

            var events = new[]
            {
                Event.CreateEventWithTags("StudentRegistered", new[] { s1 }, new { Name = "Alice" }),
                Event.CreateEventWithTags("StudentRegistered", new[] { s2 }, new { Name = "Bob" }),
                Event.CreateEventWithTags("ClassCreated", new[] { c1 }, new { Title = "Math" }),
                Event.CreateEventWithTags("ClassCreated", new[] { c2 }, new { Title = "Science" }),
                Event.CreateEventWithTags("StudentEnrolled", new[] { s1, c1 }, new { StudentId = "s1", ClassId = "c1" }),
                Event.CreateEventWithTags("StudentEnrolled", new[] { s2, c1 }, new { StudentId = "s2", ClassId = "c1" }),
                Event.CreateEventWithTags("StudentEnrolled", new[] { s1, c2 }, new { StudentId = "s1", ClassId = "c2" }),
            };

            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // 1. Event type only
            var typeQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByEventType("StudentRegistered"))
                .Build();
            var typeResults = await _store.QueryEventsAsync(typeQuery);
            Assert.All(typeResults, e => Assert.Equal("StudentRegistered", e.EventType));

            // 2. Tag only
            var tagQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(s1))
                .Build();
            var tagResults = await _store.QueryEventsAsync(tagQuery);
            Assert.All(tagResults, e => Assert.Contains(e.Tags, t => t.Entity == "student" && t.Id == "s1"));

            // 3. Event type + tag
            var typeTagQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByEventTypeAndTags("StudentEnrolled", new[] { s1 }))
                .Build();
            var typeTagResults = await _store.QueryEventsAsync(typeTagQuery);
            Assert.All(typeTagResults, e => {
                Assert.Equal("StudentEnrolled", e.EventType);
                Assert.Contains(e.Tags, t => t.Entity == "student" && t.Id == "s1");
            });

            // 4. Multiple tags, match all
            var allTagsQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTags(new[] { s1, c1 }))
                .Build();
            var allTagsResults = await _store.QueryEventsAsync(allTagsQuery);
            Assert.All(allTagsResults, e => {
                Assert.Contains(e.Tags, t => t.Entity == "student" && t.Id == "s1");
                Assert.Contains(e.Tags, t => t.Entity == "class" && t.Id == "c1");
            });

            // 5. Multiple tags, match any
            var anyTagsQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTags(new[] { s1, c2 }, matchAny: true))
                .Build();
            var anyTagsResults = await _store.QueryEventsAsync(anyTagsQuery);
            Assert.All(anyTagsResults, e =>
                Assert.True(
                    e.Tags.Any(t => (t.Entity == "student" && t.Id == "s1") || (t.Entity == "class" && t.Id == "c2"))
                )
            );
        }
    }
} 