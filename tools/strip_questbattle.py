"""Strip BossmodReborn's QuestBattle autorotation out of ported quest-duty modules.

QuestBattle is not boss-module infrastructure, despite the name. `UnmanagedRotation` is a hand-written
rotation for the temporary jobs solo quest duties hand you — it picks a target and pushes actions onto
ActionsToExecute — and `RotationModule<R>` is the component that runs one each frame. Minerva presses no
buttons, so both belong in the same bucket as StrategyValues: stripped, not ported.

What is kept: several RotationModule subclasses override AddAIHints to do real work before delegating —
marking an invincible add, deciding which enemy is on you, saying where one should be dragged. That is
knowledge about the fight, and Minerva records all of it. Those classes are re-based onto ModuleComponent
with the delegating `base.AddAIHints` call dropped; the rest are deleted outright.
"""
import pathlib
import re

ROT = re.compile(r"(?:^|\n)((?:[ \t]*(?://[^\n]*\n)?)*[ \t]*(?:public |internal |sealed |abstract )*class (\w+)\s*\([^)]*\)\s*:\s*QuestBattle\.UnmanagedRotation\s*\([^)]*\)\s*)\{")
BARE = re.compile(r"(?:^|\n)[ \t]*(?:public |internal |sealed )*class (\w+)\s*\([^)]*\)\s*:\s*QuestBattle\.RotationModule<\w+>\s*\([^)]*\)\s*;[ \t]*")
BODIED = re.compile(r"(class \w+\s*\([^)]*\)\s*:\s*)QuestBattle\.RotationModule<\w+>(\s*\([^)]*\))")
BASECALL = re.compile(r"[ \t]*base\.AddAIHints\([^;]*\);[ \t]*\r?\n")


def block_end(text, brace_index):
    """Index just past the closing brace of the block opening at brace_index."""
    depth = 0
    i = brace_index
    while i < len(text):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return i + 1
        i += 1
    raise ValueError("unbalanced braces")


def strip(path):
    t = path.read_text(encoding="utf-8")
    original = t
    removed = []

    # 1. whole UnmanagedRotation classes
    while (m := ROT.search(t)) is not None:
        start = m.start(1)
        end = block_end(t, m.end() - 1)
        removed.append(m.group(2))
        t = t[:start] + t[end:]

    # 2. bare RotationModule one-liners
    while (m := BARE.search(t)) is not None:
        removed.append(m.group(1))
        t = t[:m.start()] + t[m.end():]

    # 3. RotationModule subclasses that carry real hint work: re-base and drop the delegating call
    kept = BODIED.findall(t)
    if kept:
        t = BODIED.sub(r"\1ModuleComponent\2", t)
        t = BASECALL.sub("", t)

    # 4. drop state-machine activations of anything deleted
    for name in removed:
        t = re.sub(rf"[ \t]*\.?(?:De)?[Aa]ctivateOnEnter<{name}>\(\)\r?\n", "\n", t)
        t = re.sub(rf"\.(?:De)?[Aa]ctivateOnEnter<{name}>\(\)", "", t)

    t = re.sub(r"\n{3,}", "\n\n", t)
    if t != original:
        path.write_text(t, encoding="utf-8")
    return removed, len(kept)


total_removed = total_kept = touched = 0
for f in sorted(pathlib.Path("staged-ports").rglob("*.cs")):
    if "QuestBattle." not in f.read_text(encoding="utf-8", errors="replace"):
        continue
    removed, kept = strip(f)
    if removed or kept:
        touched += 1
        total_removed += len(removed)
        total_kept += kept
        print(f"  {str(f.relative_to('staged-ports')):<62} -{len(removed)} +{kept} kept")

print(f"\n{touched} files: deleted {total_removed} rotation classes, re-based {total_kept} onto ModuleComponent")
leftover = [str(f.relative_to('staged-ports')) for f in pathlib.Path("staged-ports").rglob("*.cs")
            if "QuestBattle" in f.read_text(encoding="utf-8", errors="replace")]
print(f"still mentioning QuestBattle: {len(leftover)}")
for x in leftover:
    print("   ", x)
