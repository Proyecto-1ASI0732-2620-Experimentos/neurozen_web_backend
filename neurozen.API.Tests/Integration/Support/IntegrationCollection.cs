namespace neurozen.API.Tests.Integration.Support;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    public const string Name = "Integration";
}

/// <summary>xUnit fixture that exposes the shared <see cref="TestEnvironment" />.</summary>
public sealed class IntegrationFixture : IAsyncLifetime
{
    public TestEnvironment Environment { get; private set; } = null!;

    public async Task InitializeAsync() => Environment = await TestEnvironment.GetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
