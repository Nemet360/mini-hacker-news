#!/usr/bin/env python3
"""Fail a PR whose line coverage dropped more than `tolerance` points below main.

Three answers, not two: a missing baseline, or nothing to measure, is NOT a
failure - it is "we could not look", and the first PR of the repository has to
be able to go green.
"""
import sys
import glob
import xml.etree.ElementTree as ET


def line_rate(folder: str):
    files = glob.glob(f"{folder}/**/coverage.cobertura.xml", recursive=True)
    if not files:
        return None
    best = None
    for path in files:
        root = ET.parse(path).getroot()
        covered = int(root.get("lines-covered", "0"))
        valid = int(root.get("lines-valid", "0"))
        if valid == 0:
            continue
        rate = 100.0 * covered / valid
        best = rate if best is None else max(best, rate)
    return best


def main() -> int:
    pr_dir, base_dir, tol = sys.argv[1], sys.argv[2], float(sys.argv[3])
    pr = line_rate(pr_dir)
    base = line_rate(base_dir)

    if pr is None:
        print("coverage-gate: nothing to measure on this PR - passing.")
        return 0
    print(f"coverage-gate: PR line coverage {pr:.2f}%")
    if base is None:
        print("coverage-gate: no baseline from main yet - passing and recording.")
        return 0
    print(f"coverage-gate: baseline {base:.2f}%, tolerance {tol}pp")
    if pr + tol < base:
        print(f"coverage-gate: FAIL - dropped {base - pr:.2f}pp")
        return 1
    print("coverage-gate: OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
