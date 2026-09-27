using System.Collections.Concurrent;

namespace OpsIntel.Connectors.Graph.Http;

/// <summary>
/// Caps concurrent in-flight requests per mailbox at Outlook's documented limit of 4
/// (ADR-0009: "Outlook … four concurrent requests"). One <see cref="SemaphoreSlim"/> per
/// mailbox ID, created lazily and shared by every <see cref="MailboxConcurrencyHandler"/> call
/// for that mailbox for the process's lifetime.
/// </summary>
public sealed class MailboxConcurrencyLimiter
{
    /// <summary>The mailbox ID used when a request doesn't set one explicitly (single-mailbox spike/MVP).</summary>
    public const string DefaultMailboxId = "default";

    private readonly int _maxConcurrentPerMailbox;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public MailboxConcurrencyLimiter(int maxConcurrentPerMailbox = 4)
    {
        if (maxConcurrentPerMailbox < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrentPerMailbox), maxConcurrentPerMailbox, "Must be at least 1.");
        }

        _maxConcurrentPerMailbox = maxConcurrentPerMailbox;
    }

    /// <summary>The current number of requests allowed to run at once for this mailbox.</summary>
    public int MaxConcurrentPerMailbox => _maxConcurrentPerMailbox;

    public SemaphoreSlim GetSemaphore(string mailboxId) =>
        _semaphores.GetOrAdd(mailboxId, static (_, max) => new SemaphoreSlim(max, max), _maxConcurrentPerMailbox);
}
