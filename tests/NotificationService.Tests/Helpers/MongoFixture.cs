using MongoDB.Driver;

using Testcontainers.MongoDb;

namespace NotificationService.Tests;

/// <summary>
/// Spins up an ephemeral MongoDB container via Testcontainers.
/// Shared across tests in a class via IClassFixture.
/// Implements IAsyncLifetime for async start/stop.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    public IMongoDatabase Database { get; private set; } = null!;
    public MongoClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Client = new MongoClient(_container.GetConnectionString());
        Database = Client.GetDatabase("test_notification");
    }

    /// <summary>
    /// Drops and recreates the database for a clean state.
    /// </summary>
    public void Reset()
    {
        Client.DropDatabase("test_notification");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
