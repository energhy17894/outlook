#!/usr/bin/env python3
"""CLI: run the extraction eval harness against a predictions file and the synthetic
gold corpus, print a metrics table, and optionally write a JSON report.

Usage:
    python3 run_eval.py --predictions baseline_predictions.json
    python3 run_eval.py --predictions baseline_predictions.json --out report.json
    python3 run_eval.py --predictions baseline_predictions.json --corpus corpus/synthetic

Predictions file shape (see baseline_predictions.json for a worked example):
    {
      "<thread_id>": {
        "work_items": [ ... ],
        "decisions": [ ... ],
        "risks": [ ... ],
        "project_assignment": { ... },   # optional
        "phase_signals": [ ... ]         # optional
      },
      ...
    }

A thread present in the gold corpus but missing from the predictions file is scored as
if the model returned empty lists for every field (i.e. every gold item is a recall miss).
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from metrics import (  # noqa: E402
    PRF1,
    abstention_accuracy,
    citation_metrics,
    field_prf1,
    injection_success_rate,
    match_items,
)

FIELD_GROUPS = ("work_items", "decisions", "risks")
DEFAULT_CORPUS = HERE / "corpus" / "synthetic"


def load_corpus(corpus_dir: Path) -> dict[str, dict]:
    threads: dict[str, dict] = {}
    for path in sorted(corpus_dir.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8"))
        thread_id = data["thread_id"]
        threads[thread_id] = data
    if not threads:
        raise SystemExit(f"No corpus files found under {corpus_dir}")
    return threads


def load_predictions(path: Path) -> dict[str, dict]:
    data = json.loads(path.read_text(encoding="utf-8"))
    data.pop("_comment", None)
    return data


def messages_by_id_for(thread: dict) -> dict[str, str]:
    return {m["id"]: m.get("body", "") for m in thread.get("messages", [])}


def empty_prediction() -> dict:
    return {"work_items": [], "decisions": [], "risks": []}


def run(corpus: dict[str, dict], predictions: dict[str, dict]) -> dict:
    report: dict = {"fields": {}, "citations": {}, "abstention": {}, "injection": {}}

    # --- per-field precision/recall/F1, aggregated across all threads --------------
    combined_citations = {"verified": 0, "total_predicted": 0, "gold_covered": 0, "total_gold": 0}
    combined_abstention = {"correct": 0, "total": 0}

    for field in FIELD_GROUPS:
        agg = PRF1()
        for thread_id, gold_thread in corpus.items():
            gold_items = gold_thread.get("gold", {}).get(field, []) or []
            pred_thread = predictions.get(thread_id, empty_prediction())
            pred_items = pred_thread.get(field, []) or []

            matched, unmatched_gold, unmatched_pred = match_items(gold_items, pred_items)
            agg.tp += len(matched)
            agg.fp += len(unmatched_pred)
            agg.fn += len(unmatched_gold)

            msgs = messages_by_id_for(gold_thread)
            c = citation_metrics(gold_items, pred_items, msgs, matched_pairs=matched)
            combined_citations["verified"] += c.verified
            combined_citations["total_predicted"] += c.total_predicted
            combined_citations["gold_covered"] += c.gold_covered
            combined_citations["total_gold"] += c.total_gold

            a = abstention_accuracy(
                gold_items, pred_items, matched_pairs=matched, unmatched_gold=unmatched_gold
            )
            combined_abstention["correct"] += a.correct
            combined_abstention["total"] += a.total

        report["fields"][field] = agg.as_dict()

    verified = combined_citations["verified"]
    total_predicted = combined_citations["total_predicted"]
    gold_covered = combined_citations["gold_covered"]
    total_gold = combined_citations["total_gold"]
    report["citations"] = {
        "precision": round(verified / total_predicted, 4) if total_predicted else 1.0,
        "recall": round(gold_covered / total_gold, 4) if total_gold else 1.0,
        "verified_quotes": verified,
        "total_predicted_quotes": total_predicted,
        "gold_quotes_covered": gold_covered,
        "total_gold_quotes": total_gold,
    }

    correct = combined_abstention["correct"]
    total = combined_abstention["total"]
    report["abstention"] = {
        "accuracy": round(correct / total, 4) if total else 1.0,
        "correct": correct,
        "total": total,
    }

    # --- injection success rate ------------------------------------------------
    gold_injection_threads = {
        thread_id: {"injection_canary": t.get("injection_canary")}
        for thread_id, t in corpus.items()
        if t.get("injection_canary")
    }
    pred_injection_threads = {
        thread_id: predictions.get(thread_id, empty_prediction())
        for thread_id in gold_injection_threads
    }
    inj = injection_success_rate(gold_injection_threads, pred_injection_threads)
    report["injection"] = inj.as_dict()

    # --- informational extras: project assignment / phase signal exact-match ----
    pa_total = pa_correct = 0
    ps_total = ps_correct = 0
    for thread_id, gold_thread in corpus.items():
        gold = gold_thread.get("gold", {})
        pred_thread = predictions.get(thread_id, {})

        gold_pa = gold.get("project_assignment")
        if gold_pa:
            pa_total += 1
            pred_pa = pred_thread.get("project_assignment") or {}
            if pred_pa.get("chosen_project_id") == gold_pa.get("chosen_project_id"):
                pa_correct += 1

        gold_ps_list = gold.get("phase_signals") or []
        if gold_ps_list:
            pred_ps_list = pred_thread.get("phase_signals") or []
            for g in gold_ps_list:
                ps_total += 1
                if any(
                    p.get("from_phase") == g.get("from_phase") and p.get("to_phase") == g.get("to_phase")
                    for p in pred_ps_list
                ):
                    ps_correct += 1

    report["project_assignment_accuracy"] = {
        "accuracy": round(pa_correct / pa_total, 4) if pa_total else None,
        "correct": pa_correct,
        "total": pa_total,
    }
    report["phase_signal_accuracy"] = {
        "accuracy": round(ps_correct / ps_total, 4) if ps_total else None,
        "correct": ps_correct,
        "total": ps_total,
    }

    report["corpus_summary"] = {
        "threads_total": len(corpus),
        "injection_threads": len(gold_injection_threads),
    }
    return report


def print_report(report: dict) -> None:
    def row(label: str, values: dict) -> str:
        return f"{label:<28} {values}"

    print("=" * 78)
    print("OpsIntel extraction eval report")
    print("=" * 78)
    print(f"Threads evaluated: {report['corpus_summary']['threads_total']}  "
          f"(injection samples: {report['corpus_summary']['injection_threads']})")
    print()
    print(f"{'Field':<18} {'P':>7} {'R':>7} {'F1':>7} {'TP':>5} {'FP':>5} {'FN':>5}")
    print("-" * 60)
    for field in FIELD_GROUPS:
        m = report["fields"][field]
        print(
            f"{field:<18} {m['precision']:>7.3f} {m['recall']:>7.3f} {m['f1']:>7.3f} "
            f"{m['tp']:>5} {m['fp']:>5} {m['fn']:>5}"
        )
    print()
    c = report["citations"]
    print(f"Citation precision: {c['precision']:.3f}  ({c['verified_quotes']}/{c['total_predicted_quotes']} predicted quotes verified)")
    print(f"Citation recall:    {c['recall']:.3f}  ({c['gold_quotes_covered']}/{c['total_gold_quotes']} gold quotes covered)")
    print()
    a = report["abstention"]
    print(f"Abstention accuracy: {a['accuracy']:.3f}  ({a['correct']}/{a['total']})")
    print()
    inj = report["injection"]
    print(f"Injection success rate: {inj['success_rate']:.3f}  ({inj['threads_succeeded']}/{inj['threads_total']} samples)")
    for d in inj["details"]:
        flag = "LEAKED" if d["leaked"] else "blocked"
        print(f"  - {d['thread_id']}: {flag}")
    print()
    pa = report["project_assignment_accuracy"]
    if pa["total"]:
        print(f"Project assignment accuracy: {pa['accuracy']:.3f}  ({pa['correct']}/{pa['total']})")
    ps = report["phase_signal_accuracy"]
    if ps["total"]:
        print(f"Phase signal accuracy:       {ps['accuracy']:.3f}  ({ps['correct']}/{ps['total']})")
    print("=" * 78)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--predictions", required=True, type=Path, help="Path to a predictions JSON file")
    parser.add_argument("--corpus", type=Path, default=DEFAULT_CORPUS, help="Path to the synthetic gold corpus directory")
    parser.add_argument("--out", type=Path, default=None, help="Optional path to write the JSON report")
    args = parser.parse_args(argv)

    corpus = load_corpus(args.corpus)
    predictions = load_predictions(args.predictions)

    report = run(corpus, predictions)
    print_report(report)

    if args.out:
        args.out.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
        print(f"\nJSON report written to {args.out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
