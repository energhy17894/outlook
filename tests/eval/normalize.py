"""Turkish-aware text normalization shared by quote verification and metrics.

Why a hand-rolled normalizer instead of ``str.casefold()``: Python's ``casefold`` is not
locale aware. In particular it maps ``İ`` (U+0130, LATIN CAPITAL LETTER I WITH DOT ABOVE)
to the two-character sequence ``i`` + COMBINING DOT ABOVE (U+0307), not to the plain ``i``
a Turkish reader would expect, and it maps ``I`` (U+0049) to ``i`` rather than to ``ı``
(U+0131, LATIN SMALL LETTER DOTLESS I) as Turkish casing rules require. Both are exactly the
letters most likely to appear in Turkish business email (İyi, İş, Istanbul, ...), so a
naive fold silently breaks quote verification on Turkish content — see
docs/research/notes/ai_agent_mimarisi.md §2 ("Turkish text normalization").

This module builds a normalized string *and* a per-character offset map back into the
original text, so callers (verify_quotes.py) can turn a match in normalized space back into
a ``(char_start, char_end)`` span in the original cleaned message text, as required by the
``Evidence``/``TextPositionSelector`` data model (research report §3).
"""

from __future__ import annotations

import unicodedata
from dataclasses import dataclass

# Turkish-specific casing exceptions that plain str.casefold()/str.lower() get wrong.
# Uppercase dotted I -> lowercase dotted i (not i + combining dot above).
# Uppercase dotless I -> lowercase dotless ı (str.lower() would give plain 'i').
_TURKISH_UPPER_TO_LOWER = {
    "İ": "i",
    "I": "ı",
}

# Normalize "smart" quote/apostrophe variants to plain ASCII equivalents so a quote typed
# with a straight apostrophe matches a source that has a curly one (or vice versa), and
# dash variants that commonly appear in pasted/HTML-derived email bodies.
_PUNCT_FOLD = {
    "’": "'",  # RIGHT SINGLE QUOTATION MARK
    "‘": "'",  # LEFT SINGLE QUOTATION MARK
    "“": '"',  # LEFT DOUBLE QUOTATION MARK
    "”": '"',  # RIGHT DOUBLE QUOTATION MARK
    "–": "-",  # EN DASH
    "—": "-",  # EM DASH
    " ": " ",  # NO-BREAK SPACE
}


def _fold_char(ch: str) -> str:
    """Fold a single character the Turkish-aware way. May expand to >1 char (rare, e.g.
    German sharp s under casefold); collapse to <1 char never happens here."""
    ch = unicodedata.normalize("NFC", ch)
    if ch in _TURKISH_UPPER_TO_LOWER:
        return _TURKISH_UPPER_TO_LOWER[ch]
    if ch in _PUNCT_FOLD:
        return _PUNCT_FOLD[ch]
    return ch.casefold()


@dataclass(frozen=True)
class NormalizedText:
    """Normalized text plus a map from each normalized-char index back to the original
    string's char index it was derived from (for building evidence offsets)."""

    text: str
    index_map: tuple[int, ...]  # len(index_map) == len(text)

    def to_original_span(self, norm_start: int, norm_end: int) -> tuple[int, int]:
        """norm_end is exclusive; returns an exclusive (start, end) span in the original
        text that fully covers the matched normalized range."""
        if norm_end <= norm_start:
            raise ValueError("norm_end must be greater than norm_start")
        start = self.index_map[norm_start]
        end = self.index_map[norm_end - 1] + 1
        return start, end


def normalize_with_map(text: str | None) -> NormalizedText:
    """Fold case (Turkish-aware) and collapse whitespace runs to a single space, tracking
    provenance so a match can be mapped back to the original string's char offsets."""
    if not text:
        return NormalizedText(text="", index_map=())

    folded_chars: list[str] = []
    folded_origin: list[int] = []
    for i, ch in enumerate(text):
        for fc in _fold_char(ch):
            folded_chars.append(fc)
            folded_origin.append(i)

    # Collapse consecutive whitespace to a single ASCII space, and trim leading/trailing
    # whitespace, while keeping the origin map in lock-step.
    out_chars: list[str] = []
    out_origin: list[int] = []
    prev_was_space = True  # true at the start so leading whitespace is dropped
    for ch, origin in zip(folded_chars, folded_origin):
        if ch.isspace():
            if prev_was_space:
                continue
            out_chars.append(" ")
            out_origin.append(origin)
            prev_was_space = True
        else:
            out_chars.append(ch)
            out_origin.append(origin)
            prev_was_space = False

    # Trim a single trailing space produced by collapsing.
    while out_chars and out_chars[-1] == " ":
        out_chars.pop()
        out_origin.pop()

    return NormalizedText(text="".join(out_chars), index_map=tuple(out_origin))


def normalize_for_compare(text: str | None) -> str:
    """Convenience wrapper for callers (metrics.py) that only need the normalized string,
    not the offset map."""
    return normalize_with_map(text).text
