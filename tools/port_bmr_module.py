#!/usr/bin/env python3
"""Port a BossmodReborn boss module (.cs) to Minerva's API.

BMR is BSD-3 (see THIRD-PARTY-NOTICES.txt). Minerva's module API mirrors BMR's closely, so the port is
mostly mechanical: this script does the ~95% that is, and prints a report of the parts that need a human
(arena bounds, missing components, non-CFC groups).

Usage:  python tools/port_bmr_module.py <bmr_module.cs> [output.cs]
        (with no output path, prints the ported source to stdout; report always goes to stderr)
"""
import re, sys, pathlib

# components Minerva already has — anything else is flagged to add/map
KNOWN = {
    "GenericAOEs", "SimpleAOEs", "RaidwideCast", "RaidwideCasts", "CastHint", "SingleTargetCast",
    "Voidzone", "TetherAOEs", "BaitAwayTethers", "Gaze", "LineStack", "SimpleKnockbacks",
    "SpreadFromCastTargets", "SpreadFromIcon", "StackFromIcon", "StackWithCastTargets",
    "ConcentricAOEs", "Exaflare", "SimpleExaflare", "ArenaChange",
}


def port(text):
    report = []

    # 1. namespace BossMod.* -> Minerva.*
    if not re.search(r'namespace\s+BossMod\.', text):
        report.append("WARN: no 'namespace BossMod.*' found — is this a BMR module?")
    text = re.sub(r'namespace\s+BossMod\.', 'namespace Minerva.', text)

    # 2. base class BossModule -> ModuleBase
    text = text.replace('(BossModule module)', '(ModuleBase module)')
    text = re.sub(r':\s*BossModule\(', ': ModuleBase(', text)

    # 3. [ModuleInfo(...)] attribute: GroupID->CFCID, BossModuleInfo.Maturity.X->ModuleMaturity, keep NameID/Contributors
    def remap_info(m):
        body = m.group(1)
        gtype = re.search(r'GroupType\s*=\s*BossModuleInfo\.GroupType\.(\w+)', body)
        if gtype and gtype.group(1) != 'CFC':
            report.append(f"MANUAL: GroupType={gtype.group(1)} (not CFC) — set CFCID / id space by hand")
        gid = re.search(r'GroupID\s*=\s*(\d+)', body)
        nid = re.search(r'NameID\s*=\s*(\d+)', body)
        contrib = re.search(r'Contributors\s*=\s*("(?:[^"\\]|\\.)*")', body)
        parts = [
            f"CFCID = {gid.group(1) if gid else 0}u",
            f"NameID = {nid.group(1) if nid else 0}u",
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

    # 4. arena / overrides that don't map 1:1
    if 'ArenaBoundsCustom' in text or 'DefaultBounds' in text:
        report.append("MANUAL: uses ArenaBoundsCustom/DefaultBounds — approximate with ArenaBoundsCircle/Square/Rect/Donut/Polygon")
    if re.search(r'protected override void Draw', text):
        report.append("MANUAL: main class overrides a Draw* method — review or drop (Minerva default draws the primary)")

    # 5. components Minerva may not have
    for c in sorted(set(re.findall(r'Components\.(\w+)', text))):
        if c not in KNOWN:
            report.append(f"MISSING COMPONENT: Components.{c} — add to Minerva.Core/Components or map it")

    header = "// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;\n// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).\nusing System;\nusing Minerva;\n\n"
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
