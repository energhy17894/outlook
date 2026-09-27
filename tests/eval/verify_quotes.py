"""Deterministic, Turkish-aware evidence quote verification.

Per ADR-0015: an extracted item's ``evidence[].quote`` is never trusted as-is. It is
checked locally against the source message's cleaned text: normalized exact match first
(NFC, tr-TR casefold, whitespace/quote-mark folding — see normalize.py), producing char
offsets for the ``Evidence``/``TextPositionSelector`` record. An item whose quote cannot be
verified must never be shown as fact (``needs_review`` stays true).

This is the reference implementation used by the eval harness. The production .NET
implementation in OpsIntel.Intelligence must match its normalization behaviour; port the
test vectors in tests/test_verify_quotes.py when that code is written.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Literal

from normalize import normalize_with_map

FailureReason = Literal["empty_quote", "unknown_message_id", "not_found"]


@dataclass(frozen=True)
class QuoteVerification:
    found: bool
    message_id: str
    char_start: int | None = None
    char_end: int | None = None
    failure_reason: FailureReason | None = None

    def as_dict(self) -> dict:
        return {
            "found": self.found,
            "message_id": self.message_id,
            "char_start": self.char_start,
            "char_end": self.char_end,
            "failure_reason": self.failure_reason,
        }


def verify_quote(source_text: str | None, quote: str | None, *, message_id: str = "") -> QuoteVerification:
    """Verify that ``quote`` occurs verbatim (after Turkish-aware normalization) inside
    ``source_text``. Returns char offsets into the ORIGINAL ``source_text`` on success.

    Uses a normalized exact-substring match (not fuzzy) — per ADR-0015 the verification
    ladder's deterministic step is exact-after-normalization; fuzzy/semantic matching is a
    separate, optional step (an NLI/MiniCheck-style check) that this function does not
    perform.
    """
    if not quote or not quote.strip():
        return QuoteVerification(found=False, message_id=message_id, failure_reason="empty_quote")

    norm_source = normalize_with_map(source_text)
    norm_quote = normalize_with_map(quote)

    if not norm_quote.text:
        return QuoteVerification(found=False, message_id=message_id, failure_reason="empty_quote")

    pos = norm_source.text.find(norm_quote.text)
    if pos == -1:
        return QuoteVerification(found=False, message_id=message_id, failure_reason="not_found")

    start, end = norm_source.to_original_span(pos, pos + len(norm_quote.text))
    return QuoteVerification(found=True, message_id=message_id, char_start=start, char_end=end)


def verify_evidence_list(
    evidence: list[dict],
    messages_by_id: dict[str, str],
) -> list[QuoteVerification]:
    """Verify each ``{"message_id": ..., "quote": ...}`` entry against the matching source
    message's cleaned text. Returns one QuoteVerification per evidence entry, in order."""
    results: list[QuoteVerification] = []
    for ev in evidence:
        mid = ev.get("message_id", "")
        quote = ev.get("quote", "")
        if mid not in messages_by_id:
            results.append(
                QuoteVerification(found=False, message_id=mid, failure_reason="unknown_message_id")
            )
            continue
        results.append(verify_quote(messages_by_id[mid], quote, message_id=mid))
    return results
