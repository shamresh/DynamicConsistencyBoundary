using Core.Domain.Shared.Models;
using Core.Domain.Shared.ValueObjects;
using Xunit;
using Core.Domain.Shared.Interfaces;

    
namespace Tests.EventStore
{
    [Collection("EventStore")]
    public class EventStoreBusinessScenariosTests
    {
        private readonly IEventStore _store;
        private readonly EntityTag _studentTag;
        private readonly EntityTag _courseTag;
        private readonly EntityTag _projectionTag;

        public EventStoreBusinessScenariosTests()
        {
            _store = new InMemoryEventStore();
            _studentTag = new EntityTag("Student", "STU-2024-001");
            _courseTag = new EntityTag("Course", "CS-2024-001");
            _projectionTag = new EntityTag("Projection", "CourseEnrollment");
        }

        [Fact]
        public async Task EventHandler_ShouldResumeFromLastPosition()
        {
            // Arrange
            var handlerTag = new EntityTag("Handler", "NotificationHandler");
            var events = new[]
            {
                Event.CreateWithTags("StudentRegistered", new[] { _studentTag, handlerTag }, new { Name = "John Doe", Email = "john@example.com" }),
                Event.CreateWithTags("StudentProfileUpdated", new[] { _studentTag, handlerTag }, new { Name = "John Doe", Phone = "123-456-7890" }),
                Event.CreateWithTags("StudentGraduated", new[] { _studentTag, handlerTag }, new { GraduationDate = "2024-05-15", GPA = 3.8 })
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
            var events = new[]
            {
                Event.CreateWithTags("CourseEnrolled", new[] { _courseTag, _studentTag, _projectionTag }, new { CourseId = "CS101", StudentId = "STU1", EnrollmentDate = "2024-01-15" }),
                Event.CreateWithTags("CourseGradeUpdated", new[] { _courseTag, _studentTag, _projectionTag }, new { CourseId = "CS101", StudentId = "STU1", Grade = "A" }),
                Event.CreateWithTags("CourseDropped", new[] { _courseTag, _studentTag, _projectionTag }, new { CourseId = "CS101", StudentId = "STU1", DropDate = "2024-02-01" })
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
                .WithSpecification(EventFilterSpecification.ByTag(_projectionTag))
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
            var subscriberTag = new EntityTag("Subscriber", "AcademicRecords");
            var events = new[]
            {
                Event.CreateWithTags("CourseCreated", new[] { _courseTag, subscriberTag }, new { CourseId = "CS101", Title = "Introduction to Programming" }),
                Event.CreateWithTags("CourseScheduleUpdated", new[] { _courseTag, subscriberTag }, new { CourseId = "CS101", Schedule = "MWF 10:00-11:30" }),
                Event.CreateWithTags("CourseCancelled", new[] { _courseTag, subscriberTag }, new { CourseId = "CS101", Reason = "Insufficient Enrollment" })
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
            var pageSize = 2;
            var events = new List<Event>();
            for (int i = 0; i < 5; i++)
            {
                events.Add(Event.CreateWithTags(
                    "CourseModuleCompleted", 
                    new[] { _courseTag, _studentTag }, 
                    new { 
                        CourseId = "CS101", 
                        StudentId = "STU1", 
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
                .WithSpecification(EventFilterSpecification.ByTag(_courseTag))
                .FromPosition(0)
                .WithPageSize(pageSize)
                .Build();
            var firstPage = await _store.QueryEventsAsync(firstPageQuery);

            // Act - Get second page
            var secondPageQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(_courseTag))
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
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(_courseTag))
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
            var events = new[]
            {
                Event.CreateWithTags("CoursePrerequisitesUpdated", new[] { _courseTag }, new { CourseId = "CS101", Prerequisites = new[] { "CS100" } }),
                Event.CreateWithTags("CourseCapacityUpdated", new[] { _courseTag }, new { CourseId = "CS101", MaxStudents = 30 }),
                Event.CreateWithTags("CourseInstructorAssigned", new[] { _courseTag }, new { CourseId = "CS101", InstructorId = "PROF123" })
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
                .WithSpecification(EventFilterSpecification.ByTag(_courseTag))
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
                Event.CreateWithTags("CourseDefined", new[] { new EntityTag("course", "c1") }, new { CourseId = "c1", Title = "Math" }),
                Event.CreateWithTags("StudentRegistered", new[] { new EntityTag("student", "s1") }, new { StudentId = "s1", Name = "Alice" }),
                Event.CreateWithTags("StudentRegistered", new[] { new EntityTag("student", "s2") }, new { StudentId = "s2", Name = "Bob" }),
                Event.CreateWithTags("CourseDefined", new[] { new EntityTag("course", "c2") }, new { CourseId = "c2", Title = "Science" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s1"), new EntityTag("course", "c1") }, new { StudentId = "s1", CourseId = "c1" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s2"), new EntityTag("course", "c1") }, new { StudentId = "s2", CourseId = "c1" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s1"), new EntityTag("course", "c2") }, new { StudentId = "s1", CourseId = "c2" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s2"), new EntityTag("course", "c2") }, new { StudentId = "s2", CourseId = "c2" })
            };

            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act: Query for all subscriptions of student s2
            var query = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(new EntityTag("student", "s2")))
                .Build();
            var s2Events = await _store.QueryEventsAsync(query);

            // Assert: s2 should have registered and subscribed to two courses
            Assert.Equal(3, s2Events.Count); // Registered + 2 subscriptions
            Assert.Contains(s2Events, e => e.EventType == "StudentRegistered");
            Assert.Equal(2, s2Events.Count(e => e.EventType == "StudentSubscribed"));
        }

        [Fact]
        public async Task DCBQuery_And_DecisionModel_ShouldReflectStudentCourseState()
        {
            // Arrange: Use the same event stream as above
            var events = new[]
            {
                Event.CreateWithTags("CourseDefined", new[] { new EntityTag("course", "c1") }, new { CourseId = "c1", Title = "Math" }),
                Event.CreateWithTags("StudentRegistered", new[] { new EntityTag("student", "s1") }, new { StudentId = "s1", Name = "Alice" }),
                Event.CreateWithTags("StudentRegistered", new[] { new EntityTag("student", "s2") }, new { StudentId = "s2", Name = "Bob" }),
                Event.CreateWithTags("CourseDefined", new[] { new EntityTag("course", "c2") }, new { CourseId = "c2", Title = "Science" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s1"), new EntityTag("course", "c1") }, new { StudentId = "s1", CourseId = "c1" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s2"), new EntityTag("course", "c1") }, new { StudentId = "s2", CourseId = "c1" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s1"), new EntityTag("course", "c2") }, new { StudentId = "s1", CourseId = "c2" }),
                Event.CreateWithTags("StudentSubscribed", new[] { new EntityTag("student", "s2"), new EntityTag("course", "c2") }, new { StudentId = "s2", CourseId = "c2" })
            };
            var position = await _store.GetCurrentPositionAsync();
            foreach (var @event in events)
            {
                await _store.AppendEventAsync(@event, EventQuery.Create().Build(), position);
                position = await _store.GetCurrentPositionAsync();
            }

            // Act: DCB Query for student:s2 and course:c2
            var dcbQuery = EventQuery.Create()
                .WithSpecification(EventFilterSpecification.ByTag(new EntityTag("student", "s2")))
                .WithSpecification(EventFilterSpecification.ByTag(new EntityTag("course", "c2")))
                .Build();
            var dcbEvents = await _store.QueryEventsAsync(dcbQuery);

            // In-memory decision model:
            bool studentExists = events.Any(e => e.EventType == "StudentRegistered" && e.Tags.Any(t => t.Entity == "student" && t.Id == "s2"));
            bool courseExists = events.Any(e => e.EventType == "CourseDefined" && e.Tags.Any(t => t.Entity == "course" && t.Id == "c2"));
            int studentCourseCount = events.Count(e => e.EventType == "StudentSubscribed" && e.Tags.Any(t => t.Entity == "student" && t.Id == "s2"));
            int courseStudentCount = events.Count(e => e.EventType == "StudentSubscribed" && e.Tags.Any(t => t.Entity == "course" && t.Id == "c2"));

            // Assert
            Assert.True(studentExists);
            Assert.True(courseExists);
            Assert.Equal(2, studentCourseCount); // s2 subscribed to 2 courses
            Assert.Equal(2, courseStudentCount); // c2 has 2 students
            Assert.Single(dcbEvents); // Only the StudentSubscribed event for s2/c2
            Assert.Equal("StudentSubscribed", dcbEvents[0].EventType);
        }
    }
} 