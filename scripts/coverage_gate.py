#!/usr/bin/env python3
"""Coverage gate for a pull request: absolute floors, and the ratchet against main.

    coverage_gate.py PR_DIR BASE_DIR TOLERANCE [--min-line N] [--min-branch N] [--comment FILE]

It fails when line or branch coverage is below its floor, or when line coverage
dropped more than TOLERANCE points below main's baseline.

The figure it reads is ReportGenerator's merged report (report/Cobertura.xml).
Each test project writes its own Cobertura file, and the crawler tests also
instrument the API assembly they reference, so no single raw file is the product
figure. Summing the files would count the same lines twice. Older baselines have
no merged report, so for those the best single raw file is used, as before.

There are three answers, not two: a missing baseline is "we could not look",
not a failure, so the first pull request of a repository can go green.
"""
import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET


def read(folder: str):
    """(line %, branch %) for a coverage folder, or None when there is nothing to read."""
    merged = glob.glob(os.path.join(folder, "**", "report", "Cobertura.xml"), recursive=True)
    if merged:
        root = ET.parse(merged[0]).getroot()
        return 100.0 * float(root.get("line-rate", "0")), 100.0 * float(root.get("branch-rate", "0"))

    best = None
    for path in glob.glob(os.path.join(folder, "**", "coverage.cobertura.xml"), recursive=True):
        root = ET.parse(path).getroot()
        if int(root.get("lines-valid", "0")) == 0:
            continue
        figure = (100.0 * float(root.get("line-rate", "0")), 100.0 * float(root.get("branch-rate", "0")))
        if best is None or figure[0] > best[0]:
            best = figure
    return best


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("pr_dir")
    parser.add_argument("base_dir")
    parser.add_argument("tolerance", type=float)
    parser.add_argument("--min-line", type=float, default=0.0)
    parser.add_argument("--min-branch", type=float, default=0.0)
    parser.add_argument("--comment", help="write a Markdown summary for the pull request here")
    args = parser.parse_args()

    pr = read(args.pr_dir)
    base = read(args.base_dir)
    lines = ["<!-- coverage-gate -->", "## Coverage", ""]
    failures = []

    if pr is None:
        print("coverage-gate: nothing to measure on this PR - passing.")
        lines.append("Nothing to measure on this pull request.")
    else:
        line, branch = pr
        print(f"coverage-gate: PR line {line:.2f}%, branch {branch:.2f}%")
        if line < args.min_line:
            failures.append(f"line coverage {line:.2f}% is below the {args.min_line:g}% floor")
        if branch < args.min_branch:
            failures.append(f"branch coverage {branch:.2f}% is below the {args.min_branch:g}% floor")

        delta = "no baseline from main yet"
        if base is None:
            print("coverage-gate: no baseline from main yet - not compared.")
        else:
            print(f"coverage-gate: baseline line {base[0]:.2f}%, tolerance {args.tolerance}pp")
            delta = f"{line - base[0]:+.2f}pp vs main ({base[0]:.2f}%)"
            if line + args.tolerance < base[0]:
                failures.append(f"line coverage dropped {base[0] - line:.2f}pp below main")

        lines += [
            "| Metric | This PR | Floor | Change |",
            "|---|---|---|---|",
            f"| Line | **{line:.2f}%** | {args.min_line:g}% | {delta} |",
            f"| Branch | **{branch:.2f}%** | {args.min_branch:g}% | |",
            "",
        ]

    for failure in failures:
        print(f"coverage-gate: FAIL - {failure}")
    lines.append("**Result: FAIL**" if failures else "**Result: OK**")
    lines += [f"- {failure}" for failure in failures]

    if args.comment:
        with open(args.comment, "w", encoding="utf-8") as handle:
            handle.write("\n".join(lines) + "\n")

    if failures:
        return 1
    print("coverage-gate: OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
