using Serilog.Core;
using Serilog.Events;

namespace OpsIntel.Observability;

/// <summary>
/// A Serilog enricher stub that strips known content-bearing property names from every log
/// event before it reaches a sink. ADR-0021 requires content-free logs: mail/document bodies,
/// evidence quotes and similar user content must never be written to disk logs or Event Log.
/// </summary>
/// <remarks>
/// TODO(Faz 0): this only matches a fixed property-name denylist. Before MVP, extend this (or
/// pair it with an analyzer) so any structured logging call that logs a property named/typed
/// like message/document content is caught at review time, not just at runtime.
/// </remarks>
public sealed class RedactionEnricher : ILogEventEnricher
{
    private static readonly string[] DeniedPropertyNames =
    [
        "Body", "UniqueBody", "Content", "Quote", "ExactQuote", "Subject", "AttachmentContent",
    ];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var name in DeniedPropertyNames)
        {
            if (logEvent.Properties.ContainsKey(name))
            {
                logEvent.RemovePropertyIfPresent(name);
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name, "[redacted]"));
            }
        }
    }
}
