namespace OpsIntel.Normalization;

/// <summary>
/// Minimal message header input to <see cref="ThreadRebuilder"/>. Mirrors the fields AI notes
/// §2 says to persist at ingest: <c>internetMessageId</c>, <c>conversationId</c>,
/// <c>In-Reply-To</c> and <c>References</c>.
/// </summary>
public sealed record MessageHeader(
    string MessageId,
    string? ConversationId,
    string? InReplyTo,
    IReadOnlyList<string>? References,
    string? Subject,
    DateTimeOffset SentUtc);

/// <summary>A rebuilt thread: message ids in chronological order.</summary>
public sealed record RebuiltThread(string ThreadId, IReadOnlyList<string> MessageIdsChronological);

/// <summary>
/// Rebuilds threads by a union-find over <c>conversationId</c> plus RFC 5322
/// <c>In-Reply-To</c>/<c>References</c> links, with a normalized-subject fallback — because
/// Graph's <c>conversationId</c> can itself change mid-conversation (AI notes §2, citing a
/// Microsoft Q&amp;A report), so header links are the primary signal and conversationId is an
/// additional union hint, not the sole key.
/// </summary>
public static class ThreadRebuilder
{
    /// <summary>Groups the given messages into threads and orders each thread chronologically.</summary>
    public static IReadOnlyList<RebuiltThread> Rebuild(IReadOnlyList<MessageHeader> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var indexByMessageId = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < messages.Count; i++)
        {
            // Last write wins on duplicate ids; ids are expected to be unique.
            indexByMessageId[messages[i].MessageId] = i;
        }

        var parent = new int[messages.Count];
        for (var i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        void Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb)
            {
                parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
            }
        }

        // 1) Header links: In-Reply-To and References, when they point at a message we have.
        for (var i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            if (m.InReplyTo is not null && indexByMessageId.TryGetValue(m.InReplyTo, out var replyIdx))
            {
                Union(i, replyIdx);
            }

            if (m.References is not null)
            {
                foreach (var reference in m.References)
                {
                    if (indexByMessageId.TryGetValue(reference, out var refIdx))
                    {
                        Union(i, refIdx);
                    }
                }
            }
        }

        // 2) conversationId hint: union messages sharing a non-null conversationId.
        var byConversation = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < messages.Count; i++)
        {
            var cid = messages[i].ConversationId;
            if (string.IsNullOrEmpty(cid))
            {
                continue;
            }

            if (byConversation.TryGetValue(cid, out var first))
            {
                Union(i, first);
            }
            else
            {
                byConversation[cid] = i;
            }
        }

        // 3) Normalized-subject fallback: only for messages with no header links at all
        // and no conversationId, so we don't merge unrelated threads that happen to reuse a
        // generic subject line once real links are present.
        var bySubject = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            var hasStrongLink = (m.InReplyTo is not null && indexByMessageId.ContainsKey(m.InReplyTo))
                                 || (m.References?.Any(indexByMessageId.ContainsKey) ?? false)
                                 || !string.IsNullOrEmpty(m.ConversationId);
            if (hasStrongLink)
            {
                continue;
            }

            var subject = NormalizeSubject(m.Subject);
            if (subject.Length == 0)
            {
                continue;
            }

            if (bySubject.TryGetValue(subject, out var first))
            {
                Union(i, first);
            }
            else
            {
                bySubject[subject] = i;
            }
        }

        var groups = new Dictionary<int, List<int>>();
        for (var i = 0; i < messages.Count; i++)
        {
            var root = Find(i);
            if (!groups.TryGetValue(root, out var list))
            {
                list = [];
                groups[root] = list;
            }

            list.Add(i);
        }

        var result = new List<RebuiltThread>(groups.Count);
        foreach (var (root, memberIndexes) in groups)
        {
            var ordered = memberIndexes
                .OrderBy(i => messages[i].SentUtc)
                .Select(i => messages[i].MessageId)
                .ToList();
            result.Add(new RebuiltThread($"thread-{messages[root].MessageId}", ordered));
        }

        return result;
    }

    private static readonly string[] ReplyPrefixes = ["re:", "fw:", "fwd:", "ynt:", "yml:"];

    /// <summary>Strips leading Re:/Fwd:/Ynt: (TR)/Yml: (TR forward) prefixes and collapses whitespace, for subject-based grouping.</summary>
    private static string NormalizeSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return string.Empty;
        }

        var s = subject.Trim();
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var prefix in ReplyPrefixes)
            {
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    s = s[prefix.Length..].TrimStart(':', ' ').Trim();
                    changed = true;
                }
            }
        }

        return s.ToLowerInvariant();
    }
}
