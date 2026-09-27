"""Unit tests for verify_quotes.py, with a focus on the Turkish casing cases the harness
description calls out explicitly: İ/ı/I/i, NFC normalization, and whitespace collapse."""

import pytest

from normalize import normalize_for_compare, normalize_with_map
from verify_quotes import verify_evidence_list, verify_quote


def test_exact_match_ascii():
    source = "We will ship the report by Friday."
    result = verify_quote(source, "ship the report by Friday")
    assert result.found
    assert source[result.char_start:result.char_end] == "ship the report by Friday"


def test_not_found():
    result = verify_quote("Hello world", "goodbye world")
    assert not result.found
    assert result.failure_reason == "not_found"


def test_empty_quote_rejected():
    result = verify_quote("Some text here", "   ")
    assert not result.found
    assert result.failure_reason == "empty_quote"


def test_turkish_dotted_capital_i_folds_to_dotted_lowercase_i():
    # 'İ' (U+0130) must fold to plain 'i', not to 'i' + combining dot above.
    source = "İYİ ÇALIŞMALAR, teklif İptal edildi."
    quote = "teklif iptal edildi"
    result = verify_quote(source, quote)
    assert result.found
    assert source[result.char_start:result.char_end] == "teklif İptal edildi"


def test_turkish_undotted_capital_i_folds_to_dotless_i():
    # 'I' (U+0049) must fold to 'ı' (U+0131) under Turkish rules, not to plain 'i'.
    source = "PROJE ISIK durumu güncellendi."
    # 'IŞIK' folded the Turkish way is 'ışık'; matching against that must succeed.
    quote = "proje ışık durumu"
    result = verify_quote(source, quote)
    assert result.found


def test_turkish_naive_casefold_would_have_failed():
    # Demonstrates the exact bug this module exists to avoid: str.casefold() turns 'İ'
    # into 'i' + COMBINING DOT ABOVE (2 chars), which would never equal a plain 'i'.
    assert "İ".casefold() != "i"
    assert normalize_for_compare("İ") == "i"


def test_lowercase_i_and_dotless_i_are_distinct_after_normalization():
    # normalization must not collapse the İ/ı vs I/i distinction the other way either.
    assert normalize_for_compare("ışık") != normalize_for_compare("isik")


def test_whitespace_collapse_and_newlines():
    source = "Merhaba,\n\n   Bütçe   revizyonunu   yakın zamanda   tamamlayacağım.\n\nElif"
    quote = "Bütçe revizyonunu yakın zamanda tamamlayacağım."
    result = verify_quote(source, quote)
    assert result.found
    assert source[result.char_start:result.char_end] == "Bütçe   revizyonunu   yakın zamanda   tamamlayacağım."


def test_curly_quote_and_dash_folding():
    source = "Toplantı 10–14 Kasım tarihleri arasında; katılımcı sayısı artıyor. O ‘tamam’ dedi."
    quote = "O 'tamam' dedi."
    result = verify_quote(source, quote)
    assert result.found


def test_nfc_normalization_of_combining_characters():
    # 'g' + combining breve (decomposed) must normalize the same as precomposed 'ğ'.
    decomposed = "buğün"  # bu-g-breve-ün -> 'buğün' after NFC
    precomposed = "buğün"  # 'buğün'
    assert normalize_for_compare(decomposed) == normalize_for_compare(precomposed)


def test_offsets_map_back_into_original_not_normalized_text():
    # Normalized text is shorter than the original due to whitespace collapse; offsets
    # must still index correctly into the ORIGINAL string.
    source = "Kayıt:   İYİ    çalışmalar,   teşekkürler."
    quote = "iyi çalışmalar"
    result = verify_quote(source, quote)
    assert result.found
    matched_original_substring = source[result.char_start:result.char_end]
    # Re-normalizing the recovered original substring must reproduce the query.
    assert normalize_for_compare(matched_original_substring) == normalize_for_compare(quote)


def test_verify_evidence_list_unknown_message_id():
    evidence = [{"message_id": "does-not-exist", "quote": "anything"}]
    results = verify_evidence_list(evidence, {"m1": "some source text"})
    assert len(results) == 1
    assert not results[0].found
    assert results[0].failure_reason == "unknown_message_id"


def test_verify_evidence_list_mixed_results():
    messages = {"m1": "Karar: Proje ertelendi çünkü İzin alınamadı."}
    evidence = [
        {"message_id": "m1", "quote": "Proje ertelendi çünkü izin alınamadı"},
        {"message_id": "m1", "quote": "bu cümle kaynakta yok"},
    ]
    results = verify_evidence_list(evidence, messages)
    assert results[0].found
    assert not results[1].found


@pytest.mark.parametrize(
    "quote",
    [
        "PROJE ERTELENDİ",
        "proje ertelendi",
        "Proje Ertelendi",
    ],
)
def test_case_insensitive_variants_all_match(quote):
    source = "Karar: proje ertelendi, yeni tarih belirlenecek."
    result = verify_quote(source, quote)
    assert result.found


def test_normalize_with_map_empty_text():
    nt = normalize_with_map("")
    assert nt.text == ""
    assert nt.index_map == ()

    nt_none = normalize_with_map(None)
    assert nt_none.text == ""
