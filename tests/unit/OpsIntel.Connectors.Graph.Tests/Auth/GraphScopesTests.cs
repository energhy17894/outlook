using OpsIntel.Connectors.Graph.Auth;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Auth;

public class GraphScopesTests
{
    [Fact]
    public void Delegated_NeverContainsMailSend()
    {
        Assert.DoesNotContain(GraphScopes.Delegated, scope =>
            string.Equals(scope, "Mail.Send", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Delegated_NeverContainsAnyScopeWithSendInIt()
    {
        // Guards against a future "Mail.Send.Shared" or similarly-named scope slipping in too.
        Assert.DoesNotContain(GraphScopes.Delegated, scope =>
            scope.Contains("Send", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Delegated_ContainsExpectedMvpScopes()
    {
        Assert.Equal(
            ["User.Read", "Mail.ReadWrite", "Files.Read.All", "Sites.Read.All", "Calendars.Read", "offline_access"],
            GraphScopes.Delegated);
    }

    [Fact]
    public void ForbiddenMailSendScope_MatchesTheActualGraphScopeName()
    {
        Assert.Equal("Mail.Send", GraphScopes.ForbiddenMailSendScope);
    }
}
