"""Drive the tree to: build green AND validator silent.

Unlanding a States-less module leaves its siblings referencing types that no longer exist, so the two
checks feed each other -- dropping a broken file can orphan another, and dropping an orphan can leave a
module without its States class. Alternate until both are quiet rather than running each once.

Untracked files only. The tracked set is read NUL-separated; splitting on whitespace mangles paths that
contain one and makes committed work look disposable.
"""
import pathlib, re, subprocess

ROOT = pathlib.Path(".").resolve(); L = ROOT / "Modules"
REPLAYS = r"C:\Users\korha\AppData\Roaming\XIVLauncher\pluginConfigs\Minerva\replays"
ERR = re.compile(r"^(.*?\.cs)\((\d+),\d+\): error (CS\d+): ", re.M)

def tracked():
    out = subprocess.run(["git", "ls-files", "-z", "--", "Modules"], capture_output=True, text=True, cwd=ROOT).stdout
    return {p for p in out.split("\0") if p}

T = tracked()
def removable(f):
    return f.is_file() and ("Modules/" + f.relative_to(L).as_posix()) not in T

for it in range(15):
    b = subprocess.run(["dotnet", "build", "-v", "q", "--nologo"], capture_output=True, text=True, cwd=ROOT)
    bad = set()
    for path, _l, _c in ERR.findall(b.stdout + b.stderr):
        q = pathlib.Path(path.replace("\\", "/"))
        if not q.is_absolute():
            q = (ROOT / q).resolve()
        if q.is_relative_to(L) and removable(q):
            bad.add(q)
    if bad:
        for q in bad:
            q.unlink()
        print(f"iter {it+1}: dropped {len(bad)} file(s) that no longer compile", flush=True)
        continue

    errs = len(ERR.findall(b.stdout + b.stderr))
    if errs:
        print(f"iter {it+1}: {errs} error(s) remain in TRACKED files -- stopping, they need a human", flush=True)
        break

    v = subprocess.run(["dotnet", "run", "--project", "Minerva.Validate", "-v", "q", "--", REPLAYS],
                       capture_output=True, text=True, cwd=ROOT)
    names = re.findall(r'WARNING: (\S+) has no matching', v.stdout + v.stderr)
    hit = [f for n in names for f in L.rglob(f"{n}.cs") if removable(f)]
    if not hit:
        print(f"iter {it+1}: build green; {len(names)} validator warning(s), none removable", flush=True)
        print((v.stdout + v.stderr).strip().splitlines()[0], flush=True)
        break
    for f in hit:
        f.unlink()
    print(f"iter {it+1}: build green, unlanded {len(hit)} States-less module(s)", flush=True)

for d in sorted({p for p in L.rglob("*") if p.is_dir()}, key=lambda x: -len(x.parts)):
    try: d.rmdir()
    except OSError: pass
