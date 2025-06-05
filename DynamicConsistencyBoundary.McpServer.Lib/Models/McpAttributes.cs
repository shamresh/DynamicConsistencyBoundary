using System;

namespace DynamicConsistencyBoundary.McpServer.Models;

[AttributeUsage(AttributeTargets.Class)]
public class McpServerToolTypeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class McpServerToolAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
}

[AttributeUsage(AttributeTargets.Class)]
public class McpServerPromptTypeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class McpServerPromptAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
} 