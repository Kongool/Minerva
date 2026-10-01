"""Port BossmodReborn's quest battle scripts (BossMod/QuestBattle/**) into Modules/QuestBattle/**.

Usage:
    python tools/port_bmr_questbattle.py [<bmr QuestBattle dir>] [<out dir>]

Defaults to the shared BossmodReborn checkout and Modules/QuestBattle. Rewrites only what Minerva names differently
(namespaces, AIHints.InteractWithOID, Dalamud's ConditionFlag, a few AIHints views); everything else ports as written,
because Minerva.QuestBattle keeps BossmodReborn's builder API. Prints the files it wrote and any construct it knows
needs a hand look. Compile afterwards: the report cannot see API drift.
"""
import os
import re
import sys

BMR = 'D:/Dev/Olympus/.cursor/bmr/BossMod/QuestBattle'
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Modules', 'QuestBattle')

HEADER = '''// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

'''

# constructs the port leaves as written but that do something different (or nothing) here
WATCH = [
    (r'ActionsToExecute', 'presses a job action: Minerva publishes only role-play actions, so this is advice'),
    (r'\bWantJump\b', 'wants a jump: nothing presses one'),
    (r'\bWantDismount\b', 'wants a dismount: nothing presses one'),
    (r'\bStatusesToCancel\b', 'cancels a status: nothing does'),
    (r'ForcedMovement', 'forced movement: recorded, not walked'),
    (r'\bService\.', 'reaches a plugin service'),
    (r'\bunsafe\b', 'unsafe code'),
]


def port(text):
    text = text.lstrip('\ufeff')
    text = re.sub(r'namespace\s+BossMod\.', 'namespace Minerva.', text)
    text = re.sub(r'^using BossMod([.;])', r'using Minerva\1', text, flags=re.M)
    text = re.sub(r'^(using\s+\w+\s*=\s*)BossMod\.', r'\1Minerva.', text, flags=re.M)
    text = re.sub(r'^using static BossMod\.', 'using static Minerva.', text, flags=re.M)
    text = re.sub(r'^using Dalamud\.Game\.ClientState\.Conditions;\s*\n', '', text, flags=re.M)
    text = text.replace('Dalamud.Game.ClientState.Conditions.ConditionFlag.', 'ConditionFlag.')

    # boss-module vocabulary, for the few scripts that carry a component
    text = text.replace('(BossModule module)', '(ModuleBase module)')
    text = re.sub(r':\s*BossModule\(', ': ModuleBase(', text)
    text = re.sub(r'\bBossModule\b(?!Info)', 'ModuleBase', text)
    text = re.sub(r'\bBossComponent\b', 'ModuleComponent', text)

    # AIHints: a method BossmodReborn calls InteractWithOID is a field of that name here
    text = re.sub(r'\.InteractWithOID(\s*[(<])', r'.InteractWith\1', text)
    text = text.replace('.PriorityTargetsSpan', '.PriorityTargets()')
    text = text.replace('.PathfindMapCenter', '.Center')
    return text


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else BMR
    out = sys.argv[2] if len(sys.argv) > 2 else OUT
    written = 0
    for root, _, files in os.walk(src):
        for name in sorted(files):
            if not name.endswith('.cs') or name in ('QuestBattle.cs', 'UnmanagedRotation.cs'):
                continue
            path = os.path.join(root, name)
            rel = os.path.relpath(path, src)
            text = open(path, encoding='utf-8-sig').read()
            ported = HEADER + port(text)
            dest = os.path.join(out, rel)
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(dest, 'w', encoding='utf-8', newline='\n') as f:
                f.write(ported)
            written += 1
            notes = [why for pat, why in WATCH if re.search(pat, text)]
            print(f'{rel.replace(os.sep, "/")}' + (f'  -- {"; ".join(notes)}' if notes else ''))
    print(f'{written} script(s) written to {out}', file=sys.stderr)


if __name__ == '__main__':
    main()
