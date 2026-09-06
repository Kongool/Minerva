#!/usr/bin/env python3
"""Port a BossmodReborn boss module (.cs) to Minerva's API.

BMR is BSD-3 (see THIRD-PARTY-NOTICES.txt). Minerva's module API mirrors BMR's closely, so the port is
mostly mechanical: this script does the ~95% that is, and prints a report of the parts that need a human
(arena bounds, missing components, non-CFC groups). Missing components are checked against the classes
actually declared in Minerva.Core/Components, so the report tracks the codebase instead of a list in this file.

Usage:  python tools/port_bmr_module.py <bmr_module.cs> [output.cs]
        (with no output path, prints the ported source to stdout; report always goes to stderr)
"""
import re, sys, pathlib

# Minerva's real component surface, read from Minerva.Core/Components at run time rather than kept in a
# hand-maintained list here (which drifted in both directions: it named components that were never written,
# and went stale as new ones landed). Only top-level classes count — nested helper types (Exaflare.Line,
# GenericBaitAway.Bait, ...) are indented and are not addressable as Components.X.
COMPONENTS_DIR = pathlib.Path(__file__).resolve().parent.parent / "Minerva.Core" / "Components"
MODULE_INFO = pathlib.Path(__file__).resolve().parent.parent / "Minerva.Core" / "Modules" / "ModuleInfoAttribute.cs"


def known_groups():
    """ModuleGroup members Minerva actually declares, read from source so this cannot go stale."""
    if not MODULE_INFO.is_file():
        return set()
    src = MODULE_INFO.read_text(encoding="utf-8")
    body = re.search(r"public enum ModuleGroup\s*\{(.*?)\n\}", src, re.S)
    return set(re.findall(r"^\s*(\w+),", body.group(1), re.M)) if body else set()


KNOWN_GROUPS = known_groups()
TOP_LEVEL_CLASS = re.compile(r'^(?:public\s+|internal\s+|sealed\s+|abstract\s+|partial\s+|static\s+)*(?:class|record)\s+(\w+)', re.M)


def known_components():
    """Component names Minerva.Core actually defines. Returns None if the directory can't be read, so the
    caller can say 'unverified' instead of silently reporting every component as present."""
    if not COMPONENTS_DIR.is_dir():
        return None
    names = set()
    for f in COMPONENTS_DIR.glob("*.cs"):
        names.update(TOP_LEVEL_CLASS.findall(f.read_text(encoding="utf-8-sig")))
    return names or None


def port(text):
    report = []

    # 1. namespace BossMod.* -> Minerva.*
    if not re.search(r'namespace\s+BossMod\.', text):
        report.append("WARN: no 'namespace BossMod.*' found — is this a BMR module?")
    text = re.sub(r'namespace\s+BossMod\.', 'namespace Minerva.', text)

    # 2. base class BossModule -> ModuleBase
    text = text.replace('(BossModule module)', '(ModuleBase module)')
    text = re.sub(r':\s*BossModule\(', ': ModuleBase(', text)
    # remaining BossModule references (method params, Func<BossModule,...>, etc.) but NOT BossModuleInfo,
    # which the ModuleInfo remap below rewrites separately
    text = re.sub(r'\bBossModule\b(?!Info)', 'ModuleBase', text)
    # components that derive from BossComponent directly -> ModuleComponent
    text = re.sub(r'\bBossComponent\b', 'ModuleComponent', text)

    # 3. [ModuleInfo(...)] attribute: GroupID->CFCID, BossModuleInfo.Maturity.X->ModuleMaturity, keep NameID/Contributors
    def remap_info(m):
        body = m.group(1)
        # Carry the group across rather than merely reporting it. This used to append a MANUAL note and
        # emit nothing, so 133 ported modules silently lost their group and fell into the generic CFC pile
        # -- every critical engagement, hunt and quest battle mixed in with ordinary duties in the browser.
        # A note nobody actions is the same as no note.
        gtype = re.search(r'GroupType\s*=\s*BossModuleInfo\.GroupType\.(\w+)', body)
        group = gtype.group(1) if gtype else None
        if group in (None, 'CFC', 'None'):
            group = None
        elif group not in KNOWN_GROUPS:
            report.append(f"MANUAL: GroupType={group} has no ModuleGroup member — add one or the module lands in CFC")
            group = None
        gid = re.search(r'GroupID\s*=\s*(\d+)', body)
        nid = re.search(r'NameID\s*=\s*(\d+)', body)
        contrib = re.search(r'Contributors\s*=\s*("(?:[^"\\]|\\.)*")', body)
        # Minerva activates a module by matching a live actor against PrimaryActorOID; without it the
        # module registers but never fires (Minerva only infers it from an OID member named `Boss`,
        # which BMR's enums rarely have).
        poid = re.search(r'PrimaryActorOID\s*=\s*([^,]+?)\s*,', body)
        if not poid:
            report.append("MANUAL: no PrimaryActorOID in BMR's ModuleInfo — set it by hand or the module never activates")
        parts = []
        if group:
            parts.append(f"Group = ModuleGroup.{group}")
        parts += [
            f"CFCID = {gid.group(1) if gid else 0}u",
            f"NameID = {nid.group(1) if nid else 0}u",
        ]
        if poid:
            parts.append(f"PrimaryActorOID = {poid.group(1).strip()}")
        parts += [
            "PrimaryActorDeathEndsEncounter = true",
            "Maturity = ModuleMaturity.WIP",  # a fresh port is unvalidated in Minerva
        ]
        if contrib:
            parts.append(f'Contributors = {contrib.group(1)[:-1]} (ported from BMR)"')
        else:
            parts.append('Contributors = "ported from BossmodReborn"')
        return "[ModuleInfo(" + ", ".join(parts) + ")]"

    text, n = re.subn(r'\[ModuleInfo\((.*?)\)\]', remap_info, text, flags=re.S)
    if n == 0:
        report.append("WARN: no [ModuleInfo(...)] found")

    # 3b. small body-level idioms BMR uses that Minerva doesn't
    text = re.sub(r'\[\s*with\([^)]*\)\s*\]', '[]', text)   # BMR pre-sized collection-expression -> empty list
    # and the form that pre-sizes AND initialises: `[with(12), .. walls]` -> `[.. walls]`
    text = re.sub(r'\[\s*with\([^)]*\)\s*,\s*', '[', text)
    # BMR nests AOEInstance inside GenericAOEs; Minerva declares it at namespace level, and a nested type
    # cannot be aliased into a class, so the qualified spelling has to be rewritten rather than shimmed
    text = re.sub(r'(?:Components\.)?GenericAOEs\.AOEInstance', 'AOEInstance', text)
    # BMR's BossComponent exposes 'WorldState'/'Raid'; Minerva's ModuleComponent exposes 'World'/party via World
    text = re.sub(r'\bWorldState\.', 'World.', text)       # property access only (type usages have no trailing dot)
    # BMR's Arena.Bounds/Center IS the module's live geometry; Minerva splits them — Module.Bounds/Center
    # is the truth that feeds AIHints (the pathfinder), and Arena.* is a per-frame render copy the radar
    # overwrites from the module every frame. Writing Arena.* would be a silent no-op in-game and an NRE
    # headless (the validator has no renderer), so retarget both reads and writes at the module.
    # The (?<![.\w]) matters: a module may hold a static bounds table with a member named Arena, and
    # 'Holder.Arena.Center' has to keep it. Without the lookbehind this rule eats that too, and the
    # result still compiles wherever the holder exposes a Center of its own -- a wrong arena centre
    # with no error to find it by.
    text = re.sub(r'(?<![.\w])Arena\.(Bounds|Center)\b', r'Module.\1', text)
    # Same reasoning for the three bounds QUERIES, which compute purely from Bounds+Center. The other
    # Arena.*Bounds members (ActorsInBounds, ActorInsideBounds) genuinely draw, so they stay on the
    # renderer. CheckPull/AddHints/AddAIHints run on the game tick, where Arena is null.
    text = re.sub(r'(?<![.\w])(?:Mini)?Arena\.(InBounds|IntersectRayBounds|ClampToBounds)\b',
                  r'Module.\1', text)

    # A `using static BossMod.X` is NOT one of the lines to drop. It imports a type's MEMBERS, so
    # removing it does not lose a namespace, it loses every constant the file names unqualified -- and
    # the failure then reads as a missing framework member rather than a missing import.
    text = re.sub(r'using static BossMod\.', 'using static Minerva.', text)

    # BossmodReborn's own using lines: the namespace does not exist here, and leaving them turns every
    # component name in the file into an unresolved type. Second-largest blocker in the batch port.
    text = re.sub(r'^using BossMod([.;])', r'using Minerva\1', text, flags=re.M)
    # type aliases: `using RPID = BossMod.Roleplay.AID;` -- the rule above only matches a line that
    # STARTS with `using BossMod`, so the right-hand side of an alias survived untouched
    text = re.sub(r'^(using\s+\w+\s*=\s*)BossMod\.', r'\1Minerva.', text, flags=re.M)

    # strip predicted-damage args Minerva's components don't take (it has no damage-based AI)
    text = re.sub(r',\s*damageType:\s*AIHints\.PredictedDamageType\.\w+', '', text)
    text = re.sub(r',\s*AIHints\.PredictedDamageType\.\w+', '', text)

    # 4. arena / overrides that don't map 1:1
    # ArenaBoundsCustom + the Shape operands (Square/Polygon/DonutV/PolygonCustom/...) are supported now;
    # only flag DefaultBounds, which has no Minerva equivalent yet.
    if 'DefaultBounds' in text:
        report.append("MANUAL: uses DefaultBounds — pick an explicit Minerva ArenaBounds (Circle/Square/Rect/Donut/Custom)")
    if re.search(r'protected override void Draw', text):
        report.append("MANUAL: main class overrides a Draw* method — review or drop (Minerva default draws the primary)")

    # 5. components Minerva may not have
    known = known_components()
    used = sorted(set(re.findall(r'Components\.(\w+)', text)))
    if known is None:
        report.append(f"WARN: could not read {COMPONENTS_DIR} — components NOT verified "
                      f"({len(used)} referenced: {', '.join(used) if used else 'none'})")
    else:
        for c in used:
            if c not in known:
                report.append(f"MISSING COMPONENT: Components.{c} — add to Minerva.Core/Components or map it")

    header = ("// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;\n"
              "// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).\n"
              "using System;\n"
              "using System.Collections.Generic;\n"
              "using System.Linq;\n"
              "using System.Runtime.CompilerServices;\n"
              "using System.Runtime.InteropServices;\n"
              "using Minerva;\n\n")
    return header + text, report


def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")  # BMR files/messages are UTF-8; Windows console defaults to cp1252
        except Exception:
            pass
    if len(sys.argv) < 2:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    src = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8-sig")  # strip BOM
    out, report = port(src)
    if len(sys.argv) >= 3:
        pathlib.Path(sys.argv[2]).write_text(out, encoding="utf-8")
        print(f"wrote {sys.argv[2]}", file=sys.stderr)
    else:
        sys.stdout.write(out)
    print("\n=== port report ===", file=sys.stderr)
    for r in report:
        print("  " + r, file=sys.stderr)
    if not report:
        print("  (clean — no manual items)", file=sys.stderr)


if __name__ == "__main__":
    main()
