using System.Net;
using System.Net.Sockets;
using OpsIntel.SetupHelper.Cli;
using OpsIntel.SetupHelper.Commands;
using Xunit;

namespace OpsIntel.SetupHelper.Tests;

public sealed class PortCommandsTests
{
    [Fact]
    public void Check_ReturnsSuccess_WhenPortIsFree()
    {
        var port = GetEphemeralFreePort();
        using var writer = new StringWriter();

        var exitCode = PortCommands.Check(port, writer);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("free", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ReturnsPortInUse_WhenSomethingIsAlreadyListening()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var writer = new StringWriter();
        var exitCode = PortCommands.Check(port, writer);

        Assert.Equal(ExitCodes.PortInUse, exitCode);
        Assert.Contains("in use", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    [InlineData(-1)]
    public void Check_ReturnsInvalidArguments_ForOutOfRangePorts(int port)
    {
        using var writer = new StringWriter();
        var exitCode = PortCommands.Check(port, writer);

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    private static int GetEphemeralFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
