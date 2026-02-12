#!/usr/bin/env python3
"""
KoH2 AI Overhaul - Performance Analysis Script

Reads CSV logs from BepInEx/AI_Analytics/ and produces:
  1. Detailed text report (stdout) with per-session breakdowns, time-series
     comparisons from aggregate data, per-kingdom deep dives
  2. PNG graph of kingdom survival over game time

Dependencies: Python 3.6+ stdlib + matplotlib
"""

import argparse
import csv
import os
import re
import sys
from collections import defaultdict

W = 96  # report width

# ---------------------------------------------------------------------------
# Path resolution
# ---------------------------------------------------------------------------

def find_data_dir(override=None):
    if override and os.path.isdir(override):
        return override
    script_dir = os.path.dirname(os.path.abspath(__file__))
    candidate = os.path.normpath(os.path.join(script_dir, "..", "..", "BepInEx", "AI_Analytics"))
    if os.path.isdir(candidate):
        return candidate
    candidate2 = os.path.normpath(os.path.join(os.getcwd(), "BepInEx", "AI_Analytics"))
    if os.path.isdir(candidate2):
        return candidate2
    return None

# ---------------------------------------------------------------------------
# CSV parsing helpers
# ---------------------------------------------------------------------------

def read_csv(path):
    if not os.path.isfile(path):
        return [], []
    with open(path, "r", encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        headers = None
        rows = []
        for line in reader:
            if not line:
                continue
            if headers is None:
                headers = [h.strip() for h in line]
                continue
            row = {}
            for i, h in enumerate(headers):
                row[h] = line[i].strip() if i < len(line) else ""
            rows.append(row)
        return headers or [], rows


def safe_float(val, default=0.0):
    if not val or val == "":
        return default
    try:
        return float(val)
    except (ValueError, TypeError):
        return default


def safe_int(val, default=0):
    return int(safe_float(val, default))


def col(row, name, default=0.0):
    return safe_float(row.get(name, ""), default)


def parse_game_time_minutes(val):
    if not val:
        return 0.0
    val = val.strip().strip('"')
    m = re.match(r"(\d+)h\s*(\d+)m", val)
    if m:
        return int(m.group(1)) * 60 + int(m.group(2))
    try:
        return float(val) * 60.0
    except ValueError:
        return 0.0


def fmt_minutes(mins):
    h = int(mins) // 60
    m = int(mins) % 60
    return f"{h}h {m:02d}m" if h > 0 else f"{m}m"


def ratio_str(enhanced, baseline):
    if baseline > 0:
        r = enhanced / baseline
        arrow = "+" if r > 1.05 else ("-" if r < 0.95 else "=")
        return f"{r:.2f}x {arrow}"
    return "  N/A  "


def pct_str(val):
    return f"{val:.0%}"


def banner(text):
    print()
    print("=" * W)
    print(f"  {text}")
    print("=" * W)


def section(text):
    print()
    print("-" * W)
    print(f"  {text}")
    print("-" * W)

# ---------------------------------------------------------------------------
# Session detection
# ---------------------------------------------------------------------------

def detect_sessions(rows):
    sessions = []
    current = []
    prev_enhanced = None
    for row in rows:
        ec = safe_int(row.get("EnhancedCount", 0))
        if prev_enhanced is not None and ec > 0 and prev_enhanced > 0:
            if ec > prev_enhanced + 10:
                sessions.append(current)
                current = []
        current.append(row)
        prev_enhanced = ec
    if current:
        sessions.append(current)
    return sessions

# ---------------------------------------------------------------------------
# Report sections
# ---------------------------------------------------------------------------

def print_overview(agg_rows, perf_rows, baseline_rows, sessions):
    banner("KoH2 AI Overhaul - Performance Analysis Report")
    print()
    print(f"  {'Sessions detected:':<28} {len(sessions)}")
    print(f"  {'Aggregate data points:':<28} {len(agg_rows):,}")
    print(f"  {'Per-kingdom records:':<28} {len(perf_rows):,}")
    print(f"  {'Baselines recorded:':<28} {len(baseline_rows):,}")

    if agg_rows:
        timestamps = [r.get("Timestamp", "") for r in agg_rows if r.get("Timestamp")]
        if timestamps:
            print(f"  {'Date range:':<28} {timestamps[0]}  to  {timestamps[-1]}")

    # File sizes
    print()


def print_session_summaries(sessions):
    """Per-session summary using aggregate data - the heart of the report."""
    section("Session-by-Session Results (from Aggregate Data)")

    for si, session in enumerate(sessions):
        if not session:
            continue

        first = session[0]
        last = session[-1]
        has_baseline = any(safe_int(r.get("BaselineCount", 0)) > 0 for r in session)
        is_ab = has_baseline

        t_first = parse_game_time_minutes(first.get("GameYear", "0"))
        t_last = parse_game_time_minutes(last.get("GameYear", "0"))
        duration = t_last - t_first

        ec_start = safe_int(first.get("EnhancedCount", 0))
        bc_start = safe_int(first.get("BaselineCount", 0))
        ec_end = safe_int(last.get("EnhancedCount", 0))
        bc_end = safe_int(last.get("BaselineCount", 0))

        mode = "A/B Test" if is_ab else "Enhanced Only"
        ts_start = first.get("Timestamp", "?")[:10]
        ts_end = last.get("Timestamp", "?")[:10]
        date_str = ts_start if ts_start == ts_end else f"{ts_start} to {ts_end}"

        print(f"\n  *** Session {si+1} [{mode}] - {date_str} ***")
        print(f"      Duration: {fmt_minutes(duration)} game time  |  {len(session)} data points")
        print(f"      Kingdoms: Enhanced {ec_start} -> {ec_end}  |  Baseline {bc_start} -> {bc_end}")

        if not is_ab:
            # Show Enhanced-only progression
            print(f"\n      Enhanced progression:")
            _print_time_snapshots(session, enhanced_only=True)
            continue

        # A/B comparison at time intervals
        print(f"\n      {'Time':<10} {'Metric':<14} {'Enhanced':>10} {'Baseline':>10} {'Ratio':>10}")
        print(f"      {'-'*10} {'-'*14} {'-'*10} {'-'*10} {'-'*10}")

        # Pick snapshots at ~25%, 50%, 75%, 100% of session
        snapshots = _pick_snapshots(session, [0.0, 0.25, 0.50, 0.75, 1.0])

        metrics = [
            ("Kingdoms",   "EnhancedCount",      "BaselineCount",      "d"),
            ("Avg Realms", "EnhancedAvgRealms",   "BaselineAvgRealms",  "f1"),
            ("Avg Str",    "EnhancedAvgStrength",  "BaselineAvgStrength", "f0"),
            ("Avg Gold",   "EnhancedAvgGold",      "BaselineAvgGold",    "f0"),
            ("Avg Books",  "EnhancedAvgBooks",     "BaselineAvgBooks",   "f1"),
            ("Survival",   "EnhancedSurvivalRate", "BaselineSurvivalRate", "pct"),
        ]

        # Add new resource metrics if present
        if "EnhancedAvgGoldIncome" in (session[0] if session else {}):
            metrics.extend([
                ("Avg GoldInc", "EnhancedAvgGoldIncome", "BaselineAvgGoldIncome", "f0"),
                ("Avg Levy",    "EnhancedAvgLevy",       "BaselineAvgLevy",       "f0"),
                ("Avg Piety",   "EnhancedAvgPiety",      "BaselineAvgPiety",      "f0"),
            ])

        for pct_label, row in snapshots:
            time_str = row.get("GameYear", "?")
            first_metric = True
            for mname, ecol, bcol, fmt in metrics:
                ev = col(row, ecol)
                bv = col(row, bcol)
                time_cell = time_str if first_metric else ""
                first_metric = False

                if fmt == "pct":
                    e_str = pct_str(ev)
                    b_str = pct_str(bv)
                elif fmt == "f0":
                    e_str = f"{ev:.0f}"
                    b_str = f"{bv:.0f}"
                elif fmt == "f1":
                    e_str = f"{ev:.1f}"
                    b_str = f"{bv:.1f}"
                else:
                    e_str = f"{ev:.0f}"
                    b_str = f"{bv:.0f}"

                rs = ratio_str(ev, bv) if fmt != "pct" else ""
                print(f"      {time_cell:<10} {mname:<14} {e_str:>10} {b_str:>10} {rs:>10}")

            if pct_label != snapshots[-1][0]:
                print(f"      {'':10} {'':14} {'':10} {'':10} {'':10}")

        # Session verdict
        e_str_final = col(last, "EnhancedAvgStrength")
        b_str_final = col(last, "BaselineAvgStrength")
        e_realms_final = col(last, "EnhancedAvgRealms")
        b_realms_final = col(last, "BaselineAvgRealms")
        e_surv = col(last, "EnhancedSurvivalRate")
        b_surv = col(last, "BaselineSurvivalRate")

        wins, losses = 0, 0
        if e_str_final > b_str_final: wins += 1
        elif b_str_final > e_str_final: losses += 1
        if e_realms_final > b_realms_final: wins += 1
        elif b_realms_final > e_realms_final: losses += 1
        if e_surv > b_surv: wins += 1
        elif b_surv > e_surv: losses += 1

        if wins > losses:
            verdict = "ENHANCED WINS"
        elif losses > wins:
            verdict = "BASELINE WINS"
        else:
            verdict = "DRAW"

        print(f"\n      Verdict: {verdict} (Str {ratio_str(e_str_final, b_str_final).strip()}, "
              f"Realms {ratio_str(e_realms_final, b_realms_final).strip()}, "
              f"Survival {pct_str(e_surv)} vs {pct_str(b_surv)})")


def _pick_snapshots(session, fractions):
    """Pick rows at given fractions through the session."""
    n = len(session)
    if n == 0:
        return []
    result = []
    for frac in fractions:
        idx = min(int(frac * (n - 1)), n - 1)
        label = f"{frac:.0%}" if frac > 0 else "Start"
        result.append((label, session[idx]))
    return result


def _print_time_snapshots(session, enhanced_only=False):
    """Print progression for Enhanced-only sessions."""
    snapshots = _pick_snapshots(session, [0.0, 0.5, 1.0])
    print(f"      {'Time':<10} {'Kingdoms':>10} {'Avg Realms':>12} {'Avg Str':>10} {'Avg Gold':>10} {'Survival':>10}")
    print(f"      {'-'*10} {'-'*10} {'-'*12} {'-'*10} {'-'*10} {'-'*10}")
    for _, row in snapshots:
        time_str = row.get("GameYear", "?")
        print(f"      {time_str:<10} {col(row, 'EnhancedCount'):>10.0f} "
              f"{col(row, 'EnhancedAvgRealms'):>12.1f} "
              f"{col(row, 'EnhancedAvgStrength'):>10.0f} "
              f"{col(row, 'EnhancedAvgGold'):>10.0f} "
              f"{pct_str(col(row, 'EnhancedSurvivalRate')):>10}")


def print_aggregate_trends(sessions):
    """Show how the Enhanced/Baseline ratio evolves over time across all A/B sessions."""
    ab_sessions = [s for s in sessions if any(safe_int(r.get("BaselineCount", 0)) > 0 for r in s)]
    if not ab_sessions:
        return

    section("Aggregate Trends Over Time (all A/B sessions combined)")

    # Bucket by time (10-minute intervals)
    buckets = defaultdict(lambda: {"e_str": [], "b_str": [], "e_realms": [], "b_realms": [],
                                     "e_gold": [], "b_gold": [], "e_surv": [], "b_surv": [],
                                     "e_count": [], "b_count": []})

    for session in ab_sessions:
        for row in session:
            if safe_int(row.get("BaselineCount", 0)) == 0:
                continue
            t = parse_game_time_minutes(row.get("GameYear", "0"))
            bucket = int(t / 10) * 10  # 10-min buckets
            b = buckets[bucket]
            b["e_str"].append(col(row, "EnhancedAvgStrength"))
            b["b_str"].append(col(row, "BaselineAvgStrength"))
            b["e_realms"].append(col(row, "EnhancedAvgRealms"))
            b["b_realms"].append(col(row, "BaselineAvgRealms"))
            b["e_gold"].append(col(row, "EnhancedAvgGold"))
            b["b_gold"].append(col(row, "BaselineAvgGold"))
            b["e_surv"].append(col(row, "EnhancedSurvivalRate"))
            b["b_surv"].append(col(row, "BaselineSurvivalRate"))
            b["e_count"].append(safe_int(row.get("EnhancedCount", 0)))
            b["b_count"].append(safe_int(row.get("BaselineCount", 0)))

    sorted_times = sorted(buckets.keys())
    if not sorted_times:
        return

    def avg(lst):
        return sum(lst) / len(lst) if lst else 0

    print(f"\n  {'Time':<8} {'E.Count':>8} {'B.Count':>8} {'StrRatio':>10} {'RlmRatio':>10} "
          f"{'GoldRatio':>10} {'E.Surv':>8} {'B.Surv':>8}")
    print(f"  {'-'*8} {'-'*8} {'-'*8} {'-'*10} {'-'*10} {'-'*10} {'-'*8} {'-'*8}")

    for t in sorted_times:
        b = buckets[t]
        e_s, b_s = avg(b["e_str"]), avg(b["b_str"])
        e_r, b_r = avg(b["e_realms"]), avg(b["b_realms"])
        e_g, b_g = avg(b["e_gold"]), avg(b["b_gold"])
        e_sv, b_sv = avg(b["e_surv"]), avg(b["b_surv"])
        ec, bc = avg(b["e_count"]), avg(b["b_count"])

        str_r = f"{e_s/b_s:.2f}x" if b_s > 0 else "N/A"
        rlm_r = f"{e_r/b_r:.2f}x" if b_r > 0 else "N/A"
        gld_r = f"{e_g/b_g:.2f}x" if b_g > 0 else "N/A"

        print(f"  {fmt_minutes(t):<8} {ec:>8.0f} {bc:>8.0f} {str_r:>10} {rlm_r:>10} "
              f"{gld_r:>10} {pct_str(e_sv):>8} {pct_str(b_sv):>8}")


def print_per_kingdom_comparison(perf_rows):
    """Per-kingdom resource comparison using the detailed performance CSV."""
    alive = [r for r in perf_rows if r.get("IsDefeated", "").lower() != "true"]
    if not alive:
        return

    section("Per-Kingdom Resource Comparison (latest snapshot per kingdom)")

    # Check if new resource columns exist
    has_new_cols = any(col(r, "GoldIncome") != 0 for r in alive[:1000])

    # Get latest snapshot per kingdom
    latest = {}
    for r in alive:
        name = r.get("KingdomName", "?")
        ye = col(r, "YearsElapsed")
        if name not in latest or ye > col(latest[name], "YearsElapsed"):
            latest[name] = r

    snapshots = list(latest.values())
    enhanced = [r for r in snapshots if r.get("AI_Type") == "Enhanced"]
    baseline = [r for r in snapshots if r.get("AI_Type") == "Baseline"]

    if not enhanced and not baseline:
        print("\n  No data available.")
        return

    metrics = [
        ("Realms",     "RealmsCount"),
        ("Gold",       "Gold"),
        ("Strength",   "TotalStrength"),
        ("Traditions", "TraditionsCount"),
        ("Books",      "BooksCount"),
        ("Armies",     "ArmiesCount"),
        ("Wars",       "WarsCount"),
        ("Vassals",    "VassalsCount"),
        ("Allies",     "AlliesCount"),
    ]

    if has_new_cols:
        metrics.extend([
            ("Gold Income", "GoldIncome"),
            ("Food",        "FoodAmount"),
            ("Food Income", "FoodIncome"),
            ("Books Income","BooksIncome"),
            ("Books Max",   "BooksMax"),
            ("Levy",        "LevyCurrent"),
            ("Levy Income", "LevyIncome"),
            ("Levy Max",    "LevyMax"),
            ("Piety",       "PietyCurrent"),
            ("Piety Income","PietyIncome"),
            ("Piety Max",   "PietyMax"),
        ])

    e_label = f"Enhanced (n={len(enhanced)})"
    b_label = f"Baseline (n={len(baseline)})"
    print(f"\n  {'Metric':<16} {e_label:>18} {b_label:>18} {'Ratio':>10}")
    print(f"  {'-'*16} {'-'*18} {'-'*18} {'-'*10}")

    for display_name, csv_col in metrics:
        e_vals = [col(r, csv_col) for r in enhanced]
        b_vals = [col(r, csv_col) for r in baseline]
        e_avg = sum(e_vals) / len(e_vals) if e_vals else 0
        b_avg = sum(b_vals) / len(b_vals) if b_vals else 0

        # Skip rows where both are 0 (column doesn't exist in data)
        if e_avg == 0 and b_avg == 0 and csv_col not in ("WarsCount", "VassalsCount"):
            continue

        rs = ratio_str(e_avg, b_avg)
        print(f"  {display_name:<16} {e_avg:>18.1f} {b_avg:>18.1f} {rs:>10}")


def print_survival_analysis(perf_rows, sessions):
    """Detailed survival analysis using both per-kingdom and aggregate data."""
    section("Survival Analysis")

    defeats = [r for r in perf_rows if r.get("IsDefeated", "").lower() == "true"]
    enhanced_defeats = [r for r in defeats if r.get("AI_Type") == "Enhanced"]
    baseline_defeats = [r for r in defeats if r.get("AI_Type") == "Baseline"]

    print(f"\n  Total defeats:   Enhanced {len(enhanced_defeats):>4}   |   Baseline {len(baseline_defeats):>4}")

    if enhanced_defeats:
        years = [col(r, "SurvivalYears") for r in enhanced_defeats]
        non_zero = [y for y in years if y > 0]
        if non_zero:
            print(f"  Enhanced survival:  avg {sum(non_zero)/len(non_zero):.2f} years  "
                  f"(min {min(non_zero):.2f}, max {max(non_zero):.2f}, median {sorted(non_zero)[len(non_zero)//2]:.2f})")
        else:
            print(f"  Enhanced survival:  all at year 0 (baseline capture at game start)")

    if baseline_defeats:
        years = [col(r, "SurvivalYears") for r in baseline_defeats]
        non_zero = [y for y in years if y > 0]
        if non_zero:
            print(f"  Baseline survival:  avg {sum(non_zero)/len(non_zero):.2f} years  "
                  f"(min {min(non_zero):.2f}, max {max(non_zero):.2f}, median {sorted(non_zero)[len(non_zero)//2]:.2f})")
        else:
            print(f"  Baseline survival:  all at year 0 (baseline capture at game start)")

    # Survival rates from aggregate data (final snapshot per A/B session)
    ab_sessions = [s for s in sessions if any(safe_int(r.get("BaselineCount", 0)) > 0 for r in s)]
    if ab_sessions:
        print(f"\n  Final survival rates per A/B session:")
        for i, session in enumerate(ab_sessions):
            last = session[-1]
            e_surv = col(last, "EnhancedSurvivalRate")
            b_surv = col(last, "BaselineSurvivalRate")
            ec = safe_int(last.get("EnhancedCount", 0))
            bc = safe_int(last.get("BaselineCount", 0))
            time_str = last.get("GameYear", "?")
            winner = "Enhanced" if e_surv > b_surv else ("Baseline" if b_surv > e_surv else "Tie")
            print(f"    Session {i+1} @ {time_str}: Enhanced {pct_str(e_surv)} ({ec} alive) vs "
                  f"Baseline {pct_str(b_surv)} ({bc} alive)  -> {winner}")

    # First to fall
    if defeats:
        # Filter to non-zero survival (0.0 is usually game-start artifacts)
        meaningful = [r for r in defeats if col(r, "SurvivalYears") > 0]
        show_list = meaningful if meaningful else defeats

        print(f"\n  First kingdoms to fall (by survival time):")
        sorted_defeats = sorted(show_list, key=lambda r: col(r, "SurvivalYears"))
        for r in sorted_defeats[:10]:
            print(f"    {r.get('KingdomName', '?'):<28} {r.get('AI_Type', '?'):<10} "
                  f"survived {col(r, 'SurvivalYears'):.2f} years")

    # Most resilient
    if defeats:
        print(f"\n  Most resilient defeated kingdoms:")
        sorted_resilient = sorted(defeats, key=lambda r: col(r, "SurvivalYears"), reverse=True)
        for r in sorted_resilient[:10]:
            sv = col(r, "SurvivalYears")
            if sv <= 0:
                continue
            print(f"    {r.get('KingdomName', '?'):<28} {r.get('AI_Type', '?'):<10} "
                  f"survived {sv:.2f} years")


def print_top_performers(perf_rows):
    """Top and worst performers by multiple metrics."""
    section("Top & Worst Performers (latest snapshot per kingdom)")

    alive = [r for r in perf_rows if r.get("IsDefeated", "").lower() != "true"]
    if not alive:
        print("\n  No data.")
        return

    latest = {}
    for r in alive:
        name = r.get("KingdomName", "?")
        ye = col(r, "YearsElapsed")
        if name not in latest or ye > col(latest[name], "YearsElapsed"):
            latest[name] = r

    snapshots = list(latest.values())

    categories = [
        ("Realm Count",        "RealmsCount",     True),
        ("Military Strength",  "TotalStrength",    True),
        ("Gold Treasury",      "Gold",             True),
        ("Traditions",         "TraditionsCount",  True),
        ("Books",              "BooksCount",       True),
        ("Realm Growth Rate",  "RealmsGrowthRate", True),
        ("Gold Growth Rate",   "GoldGrowthRate",   True),
    ]

    # Add new cols if present
    has_income = any(col(r, "GoldIncome") != 0 for r in snapshots[:100])
    if has_income:
        categories.extend([
            ("Gold Income",   "GoldIncome",    True),
            ("Levy",          "LevyCurrent",   True),
            ("Piety",         "PietyCurrent",  True),
        ])

    for cat_name, csv_col, reverse in categories:
        sorted_list = sorted(snapshots, key=lambda r: col(r, csv_col), reverse=reverse)
        # Skip if all zeros
        if col(sorted_list[0], csv_col) == 0:
            continue

        print(f"\n  Top 5 by {cat_name}:")
        for r in sorted_list[:5]:
            val = col(r, csv_col)
            ai = r.get("AI_Type", "?")
            marker = "*" if ai == "Enhanced" else " "
            print(f"   {marker} {r.get('KingdomName', '?'):<28} {ai:<10} {val:>10.1f}")

    # Biggest growers (realm expansion)
    growers = [r for r in snapshots if col(r, "RealmsCount") > col(r, "RealmsRatio") * 1.5]
    if growers:
        growers.sort(key=lambda r: col(r, "RealmsRatio"), reverse=True)
        print(f"\n  Biggest realm growers (realms ratio vs start):")
        for r in growers[:5]:
            ratio = col(r, "RealmsRatio")
            realms = col(r, "RealmsCount")
            print(f"    {r.get('KingdomName', '?'):<28} {r.get('AI_Type', '?'):<10} "
                  f"{ratio:.1f}x growth ({realms:.0f} realms)")


def print_king_class_distribution(perf_rows):
    """King class breakdown with performance correlation."""
    section("King Class Distribution & Performance")

    alive = [r for r in perf_rows if r.get("IsDefeated", "").lower() != "true"]
    if not alive:
        print("\n  No data.")
        return

    latest = {}
    for r in alive:
        name = r.get("KingdomName", "?")
        ye = col(r, "YearsElapsed")
        if name not in latest or ye > col(latest[name], "YearsElapsed"):
            latest[name] = r

    for ai_type in ["Enhanced", "Baseline"]:
        type_rows = [r for r in latest.values() if r.get("AI_Type") == ai_type]
        if not type_rows:
            continue

        counts = defaultdict(list)
        for r in type_rows:
            cls = r.get("KingClass", "Unknown")
            counts[cls].append(r)

        total = len(type_rows)
        print(f"\n  {ai_type} ({total} kingdoms):")
        print(f"    {'Class':<16} {'Count':>6} {'%':>6} {'Avg Str':>10} {'Avg Gold':>10} {'Avg Realms':>10}")
        print(f"    {'-'*16} {'-'*6} {'-'*6} {'-'*10} {'-'*10} {'-'*10}")

        for cls, members in sorted(counts.items(), key=lambda x: -len(x[1])):
            cnt = len(members)
            pct = cnt / total * 100
            avg_str = sum(col(r, "TotalStrength") for r in members) / cnt
            avg_gold = sum(col(r, "Gold") for r in members) / cnt
            avg_realms = sum(col(r, "RealmsCount") for r in members) / cnt
            print(f"    {cls:<16} {cnt:>6} {pct:>5.1f}% {avg_str:>10.0f} {avg_gold:>10.0f} {avg_realms:>10.1f}")


def print_baseline_starting_conditions(baseline_rows):
    """Analyze starting conditions from baseline CSV."""
    if not baseline_rows:
        return

    section("Starting Conditions Analysis (from Baseline CSV)")

    enhanced = [r for r in baseline_rows if r.get("AI_Type") == "Enhanced"]
    baseline = [r for r in baseline_rows if r.get("AI_Type") == "Baseline"]

    if not enhanced and not baseline:
        return

    metrics = [
        ("Initial Realms",    "InitialRealms"),
        ("Initial Gold",      "InitialGold"),
        ("Initial Strength",  "InitialTotalStrength"),
        ("Initial Armies",    "InitialArmies"),
        ("Neighbors",         "NeighborCount"),
        ("Neighbor Avg Str",  "NeighborAvgStrength"),
    ]

    # Add new baseline cols if present
    if any(r.get("InitialGoldIncome") for r in baseline_rows[:10]):
        metrics.extend([
            ("Initial Gold Inc", "InitialGoldIncome"),
            ("Initial Food",     "InitialFood"),
            ("Initial Levy",     "InitialLevy"),
            ("Initial Piety",    "InitialPiety"),
        ])

    print(f"\n  Starting conditions fairness check (should be ~1.00x for a fair test):")
    e_label = f"Enhanced (n={len(enhanced)})"
    b_label = f"Baseline (n={len(baseline)})"
    print(f"  {'Metric':<20} {e_label:>18} {b_label:>18} {'Ratio':>10}")
    print(f"  {'-'*20} {'-'*18} {'-'*18} {'-'*10}")

    for display_name, csv_col in metrics:
        e_vals = [col(r, csv_col) for r in enhanced]
        b_vals = [col(r, csv_col) for r in baseline]
        e_avg = sum(e_vals) / len(e_vals) if e_vals else 0
        b_avg = sum(b_vals) / len(b_vals) if b_vals else 0
        rs = ratio_str(e_avg, b_avg)
        print(f"  {display_name:<20} {e_avg:>18.1f} {b_avg:>18.1f} {rs:>10}")

    # Religion distribution
    for ai_type, rows in [("Enhanced", enhanced), ("Baseline", baseline)]:
        if not rows:
            continue
        religions = defaultdict(int)
        for r in rows:
            rel = r.get("Religion", "Unknown")
            religions[rel] += 1
        print(f"\n  {ai_type} religion distribution:")
        for rel, cnt in sorted(religions.items(), key=lambda x: -x[1])[:5]:
            print(f"    {rel:<24} {cnt:>4} ({cnt/len(rows)*100:5.1f}%)")


def print_anomalies(sessions):
    """Identify sessions and time windows where Baseline outperformed Enhanced."""
    section("Anomaly Detection")

    found = False
    for i, session in enumerate(sessions):
        if not session:
            continue
        has_baseline = any(safe_int(r.get("BaselineCount", 0)) > 0 for r in session)
        if not has_baseline:
            continue

        last = session[-1]
        e_str = col(last, "EnhancedAvgStrength")
        b_str = col(last, "BaselineAvgStrength")
        e_realms = col(last, "EnhancedAvgRealms")
        b_realms = col(last, "BaselineAvgRealms")
        e_surv = col(last, "EnhancedSurvivalRate")
        b_surv = col(last, "BaselineSurvivalRate")

        issues = []
        if b_str > e_str > 0:
            issues.append(f"Strength: Baseline {b_str:.0f} > Enhanced {e_str:.0f}")
        if b_realms > e_realms > 0:
            issues.append(f"Realms: Baseline {b_realms:.1f} > Enhanced {e_realms:.1f}")
        if b_surv > e_surv and e_surv < 1.0:
            issues.append(f"Survival: Baseline {pct_str(b_surv)} > Enhanced {pct_str(e_surv)}")

        if issues:
            found = True
            time_str = last.get("GameYear", "?")
            print(f"\n  Session {i+1} (final state at {time_str}):")
            for issue in issues:
                print(f"    - {issue}")

        # Also check for temporary reversals during the session
        reversal_windows = []
        for row in session:
            if safe_int(row.get("BaselineCount", 0)) == 0:
                continue
            es = col(row, "EnhancedAvgStrength")
            bs = col(row, "BaselineAvgStrength")
            if bs > es > 0 and bs / es > 1.1:
                reversal_windows.append(row)

        if reversal_windows:
            pct_reversed = len(reversal_windows) / len([r for r in session if safe_int(r.get("BaselineCount", 0)) > 0]) * 100
            first_rev = reversal_windows[0].get("GameYear", "?")
            last_rev = reversal_windows[-1].get("GameYear", "?")
            print(f"    Strength reversal windows: {pct_reversed:.0f}% of data points "
                  f"({first_rev} - {last_rev})")

    if not found:
        print("\n  No anomalies found. Enhanced outperformed Baseline in all A/B sessions.")


def print_executive_summary(sessions, perf_rows):
    """Overall verdict across all sessions."""
    banner("Executive Summary")

    ab_sessions = [s for s in sessions if any(safe_int(r.get("BaselineCount", 0)) > 0 for r in s)]

    if not ab_sessions:
        print("\n  No A/B sessions available for comparison.")
        return

    # Aggregate final-state metrics across all A/B sessions
    all_e_str, all_b_str = [], []
    all_e_realms, all_b_realms = [], []
    all_e_surv, all_b_surv = [], []
    enhanced_wins = 0

    for session in ab_sessions:
        last = session[-1]
        es = col(last, "EnhancedAvgStrength")
        bs = col(last, "BaselineAvgStrength")
        er = col(last, "EnhancedAvgRealms")
        br = col(last, "BaselineAvgRealms")
        esv = col(last, "EnhancedSurvivalRate")
        bsv = col(last, "BaselineSurvivalRate")

        all_e_str.append(es); all_b_str.append(bs)
        all_e_realms.append(er); all_b_realms.append(br)
        all_e_surv.append(esv); all_b_surv.append(bsv)

        score = 0
        if es > bs: score += 1
        elif bs > es: score -= 1
        if er > br: score += 1
        elif br > er: score -= 1
        if esv > bsv: score += 1
        elif bsv > esv: score -= 1
        if score > 0: enhanced_wins += 1

    def avg(lst):
        return sum(lst) / len(lst) if lst else 0

    n = len(ab_sessions)
    print(f"\n  A/B Sessions analyzed: {n}")
    print(f"  Enhanced win rate:     {enhanced_wins}/{n} sessions ({enhanced_wins/n*100:.0f}%)")
    print()
    print(f"  Cross-session averages (final state):")
    print(f"    {'Metric':<20} {'Enhanced':>12} {'Baseline':>12} {'Ratio':>10}")
    print(f"    {'-'*20} {'-'*12} {'-'*12} {'-'*10}")
    print(f"    {'Avg Strength':<20} {avg(all_e_str):>12.0f} {avg(all_b_str):>12.0f} {ratio_str(avg(all_e_str), avg(all_b_str)):>10}")
    print(f"    {'Avg Realms':<20} {avg(all_e_realms):>12.1f} {avg(all_b_realms):>12.1f} {ratio_str(avg(all_e_realms), avg(all_b_realms)):>10}")
    print(f"    {'Avg Survival':<20} {pct_str(avg(all_e_surv)):>12} {pct_str(avg(all_b_surv)):>12} {'':>10}")

    # Defeat counts from per-kingdom data
    defeats = [r for r in perf_rows if r.get("IsDefeated", "").lower() == "true"]
    e_def = len([r for r in defeats if r.get("AI_Type") == "Enhanced"])
    b_def = len([r for r in defeats if r.get("AI_Type") == "Baseline"])
    print(f"    {'Total Defeats':<20} {e_def:>12} {b_def:>12}")

# ---------------------------------------------------------------------------
# Graph
# ---------------------------------------------------------------------------

def plot_survival(agg_rows, sessions, output_path):
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        print("\n  [WARN] matplotlib not available - skipping graph.")
        print("         Install with: pip install matplotlib")
        return False

    ab_sessions = [s for s in sessions
                   if any(safe_int(r.get("BaselineCount", 0)) > 0 for r in s) and len(s) >= 4]
    if not ab_sessions:
        print("\n  [WARN] No A/B sessions found for graph.")
        return False

    fig, ax = plt.subplots(figsize=(12, 6))

    all_enhanced_by_time = defaultdict(list)
    all_baseline_by_time = defaultdict(list)

    for session in ab_sessions:
        e_times, e_counts, b_times, b_counts = [], [], [], []
        for row in session:
            t = parse_game_time_minutes(row.get("GameYear", "0"))
            ec = safe_int(row.get("EnhancedCount", 0))
            bc = safe_int(row.get("BaselineCount", 0))
            e_times.append(t); e_counts.append(ec)
            b_times.append(t); b_counts.append(bc)
            t_rounded = round(t / 5) * 5
            all_enhanced_by_time[t_rounded].append(ec)
            all_baseline_by_time[t_rounded].append(bc)

        ax.plot(e_times, e_counts, color="blue", alpha=0.15, linewidth=0.8)
        ax.plot(b_times, b_counts, color="orange", alpha=0.15, linewidth=0.8)

    if all_enhanced_by_time:
        sorted_times = sorted(all_enhanced_by_time.keys())
        avg_e = [sum(all_enhanced_by_time[t]) / len(all_enhanced_by_time[t]) for t in sorted_times]
        ax.plot(sorted_times, avg_e, color="blue", linewidth=2.5, label="Enhanced (avg)")

    if all_baseline_by_time:
        sorted_times = sorted(all_baseline_by_time.keys())
        avg_b = [sum(all_baseline_by_time[t]) / len(all_baseline_by_time[t]) for t in sorted_times]
        ax.plot(sorted_times, avg_b, color="orange", linewidth=2.5, label="Baseline (avg)")

    ax.set_xlabel("Game Time (minutes)")
    ax.set_ylabel("Kingdom Count (alive)")
    ax.set_title("Kingdom Survival: Enhanced AI vs Baseline AI")
    ax.legend(loc="upper right")
    ax.grid(True, alpha=0.3)
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    print(f"\n  Graph saved to: {output_path}")

    try:
        plt.show(block=False)
    except Exception:
        pass
    plt.close(fig)
    return True

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="KoH2 AI Overhaul Performance Analyzer")
    parser.add_argument("--data-dir", help="Path to BepInEx/AI_Analytics/ directory")
    parser.add_argument("--no-graph", action="store_true", help="Skip graph generation")
    parser.add_argument("--output", default=None, help="Output path for PNG graph")
    args = parser.parse_args()

    data_dir = find_data_dir(args.data_dir)
    if not data_dir:
        print("ERROR: Could not find BepInEx/AI_Analytics/ directory.")
        print("       Run from the AIOverhaul directory, or use --data-dir <path>")
        sys.exit(1)

    print(f"  Data directory: {data_dir}")

    agg_path = os.path.join(data_dir, "AI_Aggregate_Stats.csv")
    perf_path = os.path.join(data_dir, "AI_Performance_Enhanced.csv")
    baseline_path = os.path.join(data_dir, "AI_Baseline_Initial.csv")

    _, agg_rows = read_csv(agg_path)
    _, perf_rows = read_csv(perf_path)
    _, baseline_rows = read_csv(baseline_path)

    if not agg_rows and not perf_rows:
        print("ERROR: No data found. Run the game first to generate CSV logs.")
        sys.exit(1)

    sessions = detect_sessions(agg_rows)

    # Report
    print_overview(agg_rows, perf_rows, baseline_rows, sessions)
    print_session_summaries(sessions)
    print_aggregate_trends(sessions)
    print_per_kingdom_comparison(perf_rows)
    print_survival_analysis(perf_rows, sessions)
    print_top_performers(perf_rows)
    print_king_class_distribution(perf_rows)
    print_baseline_starting_conditions(baseline_rows)
    print_anomalies(sessions)
    print_executive_summary(sessions, perf_rows)

    # Graph
    if not args.no_graph:
        output_png = args.output or os.path.join(data_dir, "AI_Survival_Graph.png")
        plot_survival(agg_rows, sessions, output_png)

    print()
    print("=" * W)
    print("  Analysis complete.")
    print("=" * W)


if __name__ == "__main__":
    main()
