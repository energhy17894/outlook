using NetArchTest.Rules;
using OpsIntel.Contracts;
using OpsIntel.Intelligence;
using OpsIntel.Platform.Abstractions;
using OpsIntel.Platform.Windows;
using Xunit;

namespace OpsIntel.ArchitectureTests;

/// <summary>
/// Enforces the module boundaries the report's directory structure and ADRs imply (§5): a
/// small set of dependency rules that, if violated, mean the layering has quietly rotted.
/// </summary>
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Intelligence_DoesNotReferenceGraphConnector()
    {
        // OpsIntel.Connectors.Graph does not exist yet in this Faz 0 skeleton, but the rule is
        // written against the namespace (not the assembly) so it starts failing the moment
        // Intelligence takes a dependency on it, whichever project it lands in (ADR-0007/0008:
        // Intelligence must never hold Graph tokens).
        var result = Types.InAssembly(typeof(ExtractionWorker).Assembly)
            .Should()
            .NotHaveDependencyOn("OpsIntel.Connectors.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Contracts_HasNoInternalDependencies()
    {
        // OpsIntel.Contracts is the lowest layer (DTOs only): every other module may depend on
        // it, but it must not depend back on anything else in the solution.
        var result = Types.InAssembly(typeof(WorkItem).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "OpsIntel.Platform",
                "OpsIntel.Host",
                "OpsIntel.Intelligence",
                "OpsIntel.Connectors",
                "OpsIntel.Persistence")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Abstractions_DoesNotDependOnPlatformWindows()
    {
        // OpsIntel.Platform.Abstractions defines the interfaces; only OpsIntel.Platform.Windows
        // (and other future platform implementations) may depend on the abstractions, never
        // the reverse, or the abstraction stops being platform-neutral.
        var result = Types.InAssembly(typeof(ISecretStore).Assembly)
            .Should()
            .NotHaveDependencyOn(typeof(DpapiSecretStore).Namespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    private static string FailureMessage(TestResult result)
        => "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
