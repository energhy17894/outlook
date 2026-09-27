namespace OpsIntel.SetupHelper.Tests;

/// <summary>A deterministic, manually-advanced <see cref="TimeProvider"/> for lease/backoff tests.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset start) => _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
