using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Minerva.Validate;

/// <summary>
/// Writes the dual viewer: a self-contained HTML page with both arenas side by side, a shared scrubber,
/// and a strip marking the frames where the two engines disagree (dual-viewer phase 3).
/// <para>
/// Both sides are drawn from the same normalised descriptor via <see cref="AoeSample.Contour"/>, so a
/// visual difference is a real difference rather than one engine's renderer against the other's.
/// </para>
/// </summary>
internal static class ViewerWriter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(float v) => v.ToString("f2", Inv);
    private static string F(double v) => v.ToString("f2", Inv);

    public static void Write(string path, string recording, CompareDriver.Result r)
    {
        var json = new StringBuilder();
        json.Append("{\"recording\":").Append(Quote(recording))
            .Append(",\"minerva\":").Append(Quote(r.MinervaModule ?? "?"))
            .Append(",\"bmr\":").Append(Quote(r.BmrModule ?? "?"))
            .Append(",\"frames\":[");

        for (var i = 0; i < r.Captured.Count; ++i)
        {
            var f = r.Captured[i];
            if (i > 0)
                json.Append(',');
            json.Append("{\"i\":").Append(f.Index)
                .Append(",\"t\":").Append(F(f.Seconds))
                .Append(",\"cx\":").Append(F(f.CenterX)).Append(",\"cz\":").Append(F(f.CenterZ))
                .Append(",\"r\":").Append(F(f.Radius))
                .Append(",\"actors\":[")
                .Append(string.Join(",", f.Actors.Select(a =>
                    $"[{F(a.X)},{F(a.Z)},{(a.IsPlayer ? 1 : 0)},{(a.IsPrimary ? 1 : 0)}]")))
                .Append("],\"m\":").Append(Aoes(f.Minerva, f.MinervaMatched))
                .Append(",\"b\":").Append(Aoes(f.Bmr, f.BmrMatched))
                .Append('}');
        }
        json.Append("]}");

        File.WriteAllText(path, Html.Replace("/*DATA*/", json.ToString()), Encoding.UTF8);

        static string Aoes(List<AoeSample> list, bool[] matched)
            => "[" + string.Join(",", list.Select((a, i) =>
            {
                var pts = string.Join(",", a.Contour().Select(p => $"[{F(p.X)},{F(p.Z)}]"));
                return $"{{\"p\":[{pts}],\"d\":{Quote(a.Describe())},\"ok\":{(i < matched.Length && matched[i] ? 1 : 0)}}}";
            })) + "]";

        static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    // Kept deliberately dependency-free: no CDN, no framework. Opens from disk anywhere.
    private const string Html = """
<!doctype html>
<meta charset="utf-8">
<title>Minerva vs BMR — dual viewer</title>
<style>
  :root { color-scheme: dark; --bg:#14161a; --panel:#1c1f26; --line:#2b303a; --text:#e6e8ec; --dim:#8b93a1;
          --mine:#4da3ff; --theirs:#ffa64d; --bad:#ff5470; --ok:#3ecf8e; }
  body { margin:0; background:var(--bg); color:var(--text); font:14px/1.5 ui-sans-serif,system-ui,sans-serif; }
  header { padding:12px 16px; border-bottom:1px solid var(--line); display:flex; gap:24px; align-items:baseline; flex-wrap:wrap; }
  h1 { font-size:15px; margin:0; font-weight:600; }
  .meta { color:var(--dim); font-size:13px; }
  .panels { display:flex; gap:16px; padding:16px; flex-wrap:wrap; }
  .panel { background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:12px; }
  .panel h2 { font-size:13px; margin:0 0 8px; font-weight:600; }
  .panel.mine h2 { color:var(--mine); } .panel.theirs h2 { color:var(--theirs); }
  canvas { display:block; background:#0f1115; border-radius:4px; }
  .controls { padding:0 16px 8px; display:flex; gap:12px; align-items:center; }
  input[type=range] { flex:1; }
  button { background:var(--panel); color:var(--text); border:1px solid var(--line); border-radius:6px; padding:4px 10px; cursor:pointer; }
  button:hover { border-color:var(--dim); }
  #strip { height:26px; margin:0 16px 12px; background:var(--panel); border:1px solid var(--line); border-radius:6px; position:relative; overflow:hidden; cursor:pointer; }
  #strip i { position:absolute; top:0; bottom:0; width:2px; background:var(--bad); }
  #strip .cursor { position:absolute; top:0; bottom:0; width:2px; background:var(--text); }
  #legend { padding:0 16px 20px; color:var(--dim); font-size:13px; }
  #diffs { padding:0 16px 24px; }
  #diffs div { font-family:ui-monospace,monospace; font-size:12px; }
  .only { color:var(--bad); } .agree { color:var(--ok); }
</style>
<header>
  <h1>Minerva vs BossmodReborn</h1>
  <span class="meta" id="hdr"></span>
</header>
<div class="controls">
  <button id="prev">◀ prev diff</button>
  <button id="next">next diff ▶</button>
  <input type="range" id="scrub" min="0" value="0">
  <span class="meta" id="pos"></span>
</div>
<div id="strip"></div>
<div class="panels">
  <div class="panel mine"><h2>Minerva</h2><canvas id="cm" width="420" height="420"></canvas></div>
  <div class="panel theirs"><h2>BossmodReborn</h2><canvas id="cb" width="420" height="420"></canvas></div>
</div>
<div id="diffs"></div>
<div id="legend">
  Shapes outlined in <span class="only">red</span> exist on only one side; <span class="agree">green</span> shapes matched.
  Both sides are drawn from the same normalised descriptor, so a visual difference is a real difference.
  BMR is a second implementation, not ground truth — a disagreement means &ldquo;look here&rdquo;.
</div>
<script>
const DATA = /*DATA*/;
const frames = DATA.frames;
document.getElementById('hdr').textContent =
  `${DATA.recording} · minerva ${DATA.minerva} · bmr ${DATA.bmr} · ${frames.length} frames`;

const scrub = document.getElementById('scrub');
scrub.max = Math.max(0, frames.length - 1);
const diffFrames = frames.map((f, i) => ({ i, bad: f.m.some(a => !a.ok) || f.b.some(a => !a.ok) }))
                         .filter(x => x.bad).map(x => x.i);

const strip = document.getElementById('strip');
function buildStrip() {
  strip.innerHTML = '';
  for (const i of diffFrames) {
    const el = document.createElement('i');
    el.style.left = (frames.length > 1 ? i / (frames.length - 1) * 100 : 0) + '%';
    strip.appendChild(el);
  }
  const cur = document.createElement('div');
  cur.className = 'cursor';
  strip.appendChild(cur);
  return cur;
}
const cursor = buildStrip();

function draw(canvas, frame, aoes) {
  const ctx = canvas.getContext('2d');
  const W = canvas.width, H = canvas.height;
  ctx.clearRect(0, 0, W, H);
  const pad = 14, R = Math.max(frame.r, 1) * 1.15;
  const sx = x => pad + (x - frame.cx + R) / (2 * R) * (W - 2 * pad);
  const sz = z => pad + (z - frame.cz + R) / (2 * R) * (H - 2 * pad);

  ctx.strokeStyle = '#2b303a';
  ctx.beginPath(); ctx.arc(sx(frame.cx), sz(frame.cz), (W - 2 * pad) / 2 / 1.15, 0, Math.PI * 2); ctx.stroke();

  for (const a of aoes) {
    if (!a.p.length) continue;
    ctx.beginPath();
    a.p.forEach((pt, i) => i ? ctx.lineTo(sx(pt[0]), sz(pt[1])) : ctx.moveTo(sx(pt[0]), sz(pt[1])));
    ctx.closePath();
    ctx.fillStyle = a.ok ? 'rgba(62,207,142,.16)' : 'rgba(255,84,112,.22)';
    ctx.fill();
    ctx.strokeStyle = a.ok ? '#3ecf8e' : '#ff5470';
    ctx.lineWidth = a.ok ? 1 : 2;
    ctx.stroke();
  }

  for (const [x, z, isPlayer, isPrimary] of frame.actors) {
    ctx.beginPath();
    ctx.arc(sx(x), sz(z), isPrimary ? 6 : 4, 0, Math.PI * 2);
    ctx.fillStyle = isPrimary ? '#ff6ad5' : (isPlayer ? '#ffb300' : '#8b93a1');
    ctx.fill();
  }
}

function render(i) {
  const f = frames[i];
  if (!f) return;
  draw(document.getElementById('cm'), f, f.m);
  draw(document.getElementById('cb'), f, f.b);
  document.getElementById('pos').textContent = `frame ${f.i} · t+${f.t.toFixed(1)}s`;
  cursor.style.left = (frames.length > 1 ? i / (frames.length - 1) * 100 : 0) + '%';

  const out = [];
  f.m.filter(a => !a.ok).forEach(a => out.push(`<div class="only">minerva-only  ${a.d}</div>`));
  f.b.filter(a => !a.ok).forEach(a => out.push(`<div class="only">bmr-only      ${a.d}</div>`));
  document.getElementById('diffs').innerHTML = out.length ? out.join('') : '<div class="agree">both engines agree on this frame</div>';
}

scrub.addEventListener('input', () => render(+scrub.value));
strip.addEventListener('click', e => {
  const p = (e.clientX - strip.getBoundingClientRect().left) / strip.clientWidth;
  scrub.value = Math.round(p * (frames.length - 1));
  render(+scrub.value);
});
function jump(dir) {
  const cur = +scrub.value;
  const next = dir > 0 ? diffFrames.find(i => i > cur) : [...diffFrames].reverse().find(i => i < cur);
  if (next !== undefined) { scrub.value = next; render(next); }
}
document.getElementById('next').addEventListener('click', () => jump(1));
document.getElementById('prev').addEventListener('click', () => jump(-1));
addEventListener('keydown', e => {
  if (e.key === 'ArrowRight') { scrub.value = Math.min(+scrub.value + 1, frames.length - 1); render(+scrub.value); }
  if (e.key === 'ArrowLeft') { scrub.value = Math.max(+scrub.value - 1, 0); render(+scrub.value); }
});
render(0);
</script>
""";
}
