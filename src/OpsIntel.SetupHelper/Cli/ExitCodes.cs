namespace OpsIntel.SetupHelper.Cli;

/// <summary>
/// Process exit codes. WiX's <c>CustomAction/@Return="check"</c> (see installer/Cert.wxs's
/// <c>CertCreate</c> action) fails the whole install the moment this process returns anything
/// other than 0, so every code here is deliberate and documented.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int UnhandledError = 1;
    public const int InvalidArguments = 2;
    public const int CertificateNotFound = 3;
    public const int CertificateInvalid = 4;
    public const int PortInUse = 10;
    public const int UnsupportedPlatform = 20;
}
