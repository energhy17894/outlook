using System.Net;
using System.Net.Sockets;
using OpsIntel.SetupHelper.Cli;

namespace OpsIntel.SetupHelper.Commands;

/// <summary>
/// <c>port check --port 6500</c>: the launch-condition pre-check Package.wxs's TODO comment
/// describes ("PORT_IN_USE launch condition"). Cross-platform (plain <see cref="TcpListener"/>
/// bind probes), because the port-in-use check itself needs nothing Windows-specific — only the
/// certificate/registry commands do.
/// </summary>
public static class PortCommands
{
    /// <summary>
    /// Attempts to bind loopback IPv4 and IPv6 on <paramref name="port"/>, exactly like Kestrel
    /// does in OpsIntel.Host's Program.cs (<c>IPAddress.Loopback</c> + <c>IPAddress.IPv6Loopback</c>).
    /// Returns <see cref="ExitCodes.Success"/> if both are free, <see cref="ExitCodes.PortInUse"/>
    /// otherwise.
    /// </summary>
    public static int Check(int port, TextWriter output)
    {
        if (port is < 1 or > 65535)
        {
            output.WriteLine($"Port {port} is out of range.");
            return ExitCodes.InvalidArguments;
        }

        var ipv4Free = TryBind(IPAddress.Loopback, port);
        // Some hosts (containers/CI runners) have no IPv6 stack at all; that is not the same
        // thing as "port in use" and must not fail the check.
        var ipv6Free = !Socket.OSSupportsIPv6 || TryBind(IPAddress.IPv6Loopback, port);

        if (ipv4Free && ipv6Free)
        {
            output.WriteLine($"Port {port} is free.");
            return ExitCodes.Success;
        }

        output.WriteLine($"Port {port} is already in use.");
        return ExitCodes.PortInUse;
    }

    private static bool TryBind(IPAddress address, int port)
    {
        try
        {
            using var listener = new TcpListener(address, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
