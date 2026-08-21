"""Rank what actually hit the player, across a folder of Minerva recordings.

The recorder captures the server's own account of every resolved cast (`CST!`), including the list of
actors it landed on. That turns "I took six vulns" into a ranked table of which mechanics are getting
through, aggregated over as many pulls as you care to throw at it -- which is the difference between
chasing one bad pull and seeing which component actually needs work.

Unavoidable hits are separated out rather than dropped: a raidwide landing on you is not a failure, and
mixing the two hides the signal. Auto-attacks are excluded entirely.

    python tools/analyze_hits.py <folder-or-log> [--boss NAME] [--top N]
"""
import argparse
import collections
import pathlib
import re
import sys

# hostile actor types: enemy, and the invisible "helper" actors bosses cast mechanics through
HOSTILE_TYPES = {"0205", "020B"}
PLAYER_TYPE = "0104"
BUDDY_TYPE = "0209"  # duty support NPCs occupy real party slots

ACT_RE = re.compile(r'\S+ ACT\+ (\S+) (\S+) \S+ (".*?"|\S+) \S+ (\S+)')


def load_names():
    """Action id -> name, generated from the game's Action sheet. Optional: without it we print ids."""
    f = pathlib.Path(__file__).with_name("action_names.tsv")
    if not f.exists():
        return {}
    out = {}
    for line in f.read_text(encoding="utf-8", errors="replace").splitlines():
        aid, _, name = line.partition("	")
        if name:
            out[int(aid)] = name
    return out


def parse(path):
    """-> (boss_name, {aid: hit_count}, total_frames_seen)"""
    actors = {}
    me = None
    boss = None
    hits = collections.Counter()
    casts = collections.Counter()

    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        parts = line.split()
        if len(parts) < 3:
            continue

        if parts[1] == "ACT+":
            m = ACT_RE.match(line)
            if not m:
                continue
            aid, oid, name, atype = m.group(1), m.group(2), m.group(3).strip('"'), m.group(4)
            actors[aid] = (oid, name, atype)
            if atype == PLAYER_TYPE and me is None:
                me = aid
            # the boss is the biggest-named targetable enemy; the filename already carries it, so this
            # is only a fallback for logs renamed by hand
            if atype == "0205" and boss is None and name:
                boss = name

        elif parts[1] == "CST!" and len(parts) >= 8:
            caster = actors.get(parts[2])
            if caster is None or caster[2] not in HOSTILE_TYPES:
                continue
            aid = int(parts[3], 16)
            count = int(parts[7])
            casts[aid] += 1
            if count == 0 or me is None:
                continue
            # target ids are spaced 9 tokens apart: the id then its eight effect slots
            if me in parts[8:8 + count * 9:9]:
                hits[aid] += 1

    return boss, hits, casts


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("path")
    ap.add_argument("--boss", help="only recordings whose filename contains this")
    ap.add_argument("--top", type=int, default=25)
    args = ap.parse_args()

    root = pathlib.Path(args.path)
    files = sorted(root.glob("*.log")) if root.is_dir() else [root]
    if args.boss:
        files = [f for f in files if args.boss.lower() in f.name.lower()]
    if not files:
        sys.exit(f"no recordings found under {root}")

    per_boss = collections.defaultdict(lambda: (collections.Counter(), collections.Counter()))
    for f in files:
        boss, hits, casts = parse(f)
        # the filename is the reliable label -- the recorder renames each log once analysis names the boss
        label = f.stem.split("-", 3)[-1] if "-" in f.stem else (boss or f.stem)
        h, c = per_boss[label]
        h.update(hits)
        c.update(casts)

    names = load_names()
    print(f"{len(files)} recording(s)\n")
    for boss, (hits, casts) in sorted(per_boss.items()):
        total = sum(hits.values())
        print(f"=== {boss}   {total} hit(s) taken")
        if not hits:
            print("    (nothing landed)\n")
            continue
        print(f"    {'hits':>5} {'casts':>6} {'rate':>6}   action")
        for aid, n in hits.most_common(args.top):
            fired = casts[aid]
            rate = f"{100.0 * n / fired:.0f}%" if fired else "-"
            print(f"    {n:5} {fired:6} {rate:>6}   {aid} {names.get(aid, '')}")
        print()


if __name__ == "__main__":
    main()
