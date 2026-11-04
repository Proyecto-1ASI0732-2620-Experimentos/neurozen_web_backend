using neurozen.API.Tests.Integration.Support;
using Reqnroll;

namespace neurozen.API.Tests.BDD.Support;

[Binding]
public sealed class BddHooks
{
    /// <summary>
    ///     Starts (only the first time) the same API + MySQL environment used by the integration tests.
    ///     A scenario hook, not [BeforeTestRun]: with xUnit, a failing test-run hook becomes an assembly fixture
    ///     failure and would also fail the unit tests, which must never depend on Docker or MySQL.
    /// </summary>
    [BeforeScenario(Order = 0)]
    public static async Task EnsureApiAsync() => await TestEnvironment.GetAsync();
}

/// <summary>Per-scenario state shared by the step classes (injected by Reqnroll).</summary>
public sealed class ScenarioState
{
    public TestAccount? Worker { get; set; }
    public TestAccount? OtherWorker { get; set; }
    public TestPsychologist? Psychologist { get; set; }
    public long AppointmentId { get; set; }
    public string? ResetToken { get; set; }
    public HttpResponseMessage? LastResponse { get; set; }
}
