using DynamicConsistencyBoundary.McpServer;
using Core.Domain.Shared.Interfaces;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register services
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddSingleton<McpServer>();
    });

var host = builder.Build();

// Get the MCP server instance
var server = host.Services.GetRequiredService<McpServer>();

// Run the server
await server.RunAsync(); 