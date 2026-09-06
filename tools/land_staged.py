"""Land what compiles, then unland any module left without its States class.

probe.py lands per FILE. A fight whose XStates.cs still fails to compile then lands its X.cs alone, and the
module registers, claims the boss and draws NOTHING -- strictly worse than not landing, and invisible in a
build that is green. Minerva.Validate already detects exactly this; this drives the fix off that check
rather than off a guess about folder layout. ("One folder = one fight" is false: flat category folders like
Dawntrail/Foray/CriticalEngagement hold ~30 independent fights.)

Only untracked files are ever removed, and the tracked set is read NUL-separated -- splitting `git ls-files`
on whitespace mangles any path containing one and makes committed files look disposable.
"""
import pathlib, re, shutil, subprocess, sys

ROOT = pathlib.Path(".").resolve(); S, L = ROOT / "staged-ports", ROOT / "Modules"
REPLAYS = r"C:\Users\korha\AppData\Roaming\XIVLauncher\pluginConfigs\Minerva\replays"

rej = [s for s in S.rglob("*.cs")
       if not s.relative_to(S).parts[0].startswith("_") and not (L / s.relative_to(S)).exists()]
copied = {}
for s in rej:
    d = L / s.relative_to(S); d.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(s, d); copied[d] = s
print(f"trying {len(copied)} staged files", flush=True)

ERR = re.compile(r"^(.*?\.cs)\((\d+),\d+\): error (CS\d+): (.+?)(?: \[|$)", re.M)
alive = set(copied)
for rnd in range(25):
    r = subprocess.run(["dotnet", "build", "-v", "q", "--nologo"], capture_output=True, text=True, cwd=ROOT)
    first = set()
    for path, _l, _c, _m in ERR.findall(r.stdout + r.stderr):
        q = pathlib.Path(path.replace("\\", "/"))
        if not q.is_absolute():
            q = (ROOT / q).resolve()
        if q in alive:
            first.add(q)
    print(f"  pass {rnd+1}: {len(first)} failed", flush=True)
    if not first:
        break
    for q in first:
        q.unlink(missing_ok=True); alive.discard(q)
print(f"landed {len(alive)}, blocked {len(copied) - len(alive)}", flush=True)

# The States-less sweep does NOT belong here: Minerva.Validate loads bin/Debug/Minerva.dll by path and
# `dotnet run --project Minerva.Validate` does not rebuild the plugin project, so a sweep inside this script
# reads a DLL from before the unlanding and reports stale warnings forever. converge.py alternates a real
# `dotnet build` with the validator, which is the only way the two checks actually see each other.
print("now run converge.py -- landing alone can leave a fight without its States class", flush=True)
