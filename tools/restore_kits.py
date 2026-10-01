"""Put BossmodReborn's role-play kits back into Minerva's ported quest modules.

The first ports stripped them (tools/strip_questbattle.py) while Minerva pressed no buttons. Since 2026-09-29 a kit
decides and Daedalus presses (Minerva.QuestBattle.UnmanagedRotation), so each kit class comes back from the BossmodReborn
module, converted by port_bmr_module.port, and its ActivateOnEnter goes back after the line it followed in
BossmodReborn's states. Prints what it did per file; activations it cannot place are reported, not guessed.

Usage: python tools/restore_kits.py <bmr module path relative to BossMod/Modules> ...
"""
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from port_bmr_module import port  # noqa: E402

BMR = pathlib.Path('D:/Dev/Olympus/.cursor/bmr/BossMod/Modules')
MINERVA = pathlib.Path(__file__).resolve().parent.parent / 'Modules'

KIT_DECL = re.compile(r'^(?:public\s+|internal\s+|sealed\s+)*class\s+(\w+)\b[^\n]*:\s*QuestBattle\.(?:UnmanagedRotation|RotationModule<)', re.M)


def block_at(text, start):
    """The class declaration starting at `start`, through its closing brace or its one-line semicolon."""
    line_end = text.index('\n', start)
    first = text[start:line_end]
    if first.rstrip().endswith(';'):
        return text[start:line_end + 1]
    depth = 0
    i = text.index('{', start)
    while True:
        c = text[i]
        if c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                end = text.index('\n', i) + 1 if '\n' in text[i:] else len(text)
                return text[start:end]
        i += 1


def convert(block):
    out, _ = port(block)
    # drop the header port() adds
    return out.split('using Minerva;\n\n', 1)[1]


def restore(rel):
    bmr = (BMR / rel).read_text(encoding='utf-8-sig')
    path = MINERVA / rel
    mine = path.read_text(encoding='utf-8-sig')
    notes = []

    kits = [(m.group(1), m.start()) for m in KIT_DECL.finditer(bmr)]
    added = []
    for name, start in kits:
        if re.search(r'\bclass\s+' + name + r'\b', mine):
            notes.append(f'{name}: already present')
            continue
        added.append(convert(block_at(bmr, start)))
    if added:
        states = re.search(r'^(?:public\s+|internal\s+|sealed\s+)*class\s+\w+States\b', mine, re.M)
        at = states.start() if states else len(mine)
        mine = mine[:at] + '\n'.join(added) + '\n' + mine[at:]
        notes.append('kits: ' + ', '.join(n for n, _ in kits if not re.search(r'already', ' '.join(notes)) or True))

    for using in re.findall(r'^using (?:\w+ = )?BossMod\.[\w.]*;', bmr, re.M):
        u = using.replace('BossMod.', 'Minerva.')
        if u not in mine:
            mine = re.sub(r'^(using Minerva;\n)', r'\1' + u + '\n', mine, count=1, flags=re.M)
            notes.append(u)

    for m in re.finditer(r'\.ActivateOnEnter<(\w+)>\(\)', bmr):
        comp = m.group(1)
        if comp not in [n for n, _ in kits] and not re.search(r'class\s+' + comp + r'\b[^\n]*RotationModule<', bmr):
            continue
        if f'.ActivateOnEnter<{comp}>()' in mine:
            notes.append(f'{comp}: already activated')
            continue
        # the activation BossmodReborn had just before this one, which the stripped port kept
        prev = re.findall(r'\.ActivateOnEnter<(\w+)>\(\)', bmr[:m.start()])
        anchor = f'.ActivateOnEnter<{prev[-1]}>()' if prev else None
        if anchor and mine.count(anchor) == 1:
            i = mine.index(anchor) + len(anchor)
            mine = mine[:i] + f'\n            .ActivateOnEnter<{comp}>()' + mine[i:]
            notes.append(f'{comp}: activated after {prev[-1]}')
        else:
            nxt = re.findall(r'\.ActivateOnEnter<(\w+)>\(\)', bmr[m.end():])
            anchor = f'.ActivateOnEnter<{nxt[0]}>()' if nxt else None
            if anchor and mine.count(anchor) == 1:
                i = mine.index(anchor)
                mine = mine[:i] + f'.ActivateOnEnter<{comp}>()\n            ' + mine[i:]
                notes.append(f'{comp}: activated before {nxt[0]}')
            else:
                notes.append(f'{comp}: NOT ACTIVATED -- place by hand')

    path.write_text(mine, encoding='utf-8')
    print(f'{rel}: ' + '; '.join(notes))


if __name__ == '__main__':
    for rel in sys.argv[1:]:
        restore(rel)
