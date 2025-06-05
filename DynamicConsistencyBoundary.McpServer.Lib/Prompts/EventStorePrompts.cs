using System.ComponentModel;
using DynamicConsistencyBoundary.McpServer.Models;

namespace DynamicConsistencyBoundary.McpServer.Prompts;

[McpServerPromptType]
public static class EventStorePrompts
{
    [McpServerPrompt, Description("""
        Creates a prompt to query events for a specific entity.
        
        Example:
        "Find all events for student s1"
        """)]
    public static ChatMessage QueryEntityEvents(
        [Description("The entity type")] string entityType,
        [Description("The entity ID")] string entityId) =>
        new(ChatRole.User, $"Find all events for {entityType} {entityId}");

    [McpServerPrompt, Description("""
        Creates a prompt to register a new student.
        
        Example:
        "Register student Alice with ID s3"
        """)]
    public static ChatMessage RegisterStudent(
        [Description("The student's name")] string name,
        [Description("The student's ID")] string id) =>
        new(ChatRole.User, $"Register student {name} with ID {id}");

    [McpServerPrompt, Description("""
        Creates a prompt to enroll a student in a class.
        
        Example:
        "Enroll student s1 in class c1"
        """)]
    public static ChatMessage EnrollStudent(
        [Description("The student's ID")] string studentId,
        [Description("The class ID")] string classId) =>
        new(ChatRole.User, $"Enroll student {studentId} in class {classId}");

    [McpServerPrompt, Description("""
        Creates a prompt to create a new class.
        
        Example:
        "Create a new class 'Math' with ID c3"
        """)]
    public static ChatMessage CreateClass(
        [Description("The class title")] string title,
        [Description("The class ID")] string id) =>
        new(ChatRole.User, $"Create a new class '{title}' with ID {id}");
} 