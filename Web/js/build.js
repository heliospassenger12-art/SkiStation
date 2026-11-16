// SkiStation Web — outils de construction (tracé libre + récapitulatif avant validation), sélection, entrées caméra
(function () {
  'use strict';
  const SS = window.SS, T = SS.Terrain, D = SS.Data, R = SS.R, Cam = SS.Cam, Net = SS.Net;
  const B = SS.Build = { tool: null, an: null, hover: null };

  function describe(x, z) {                       // à quoi ce point se raccorde-t-il ?
    const G = SS.G;
    if (Math.hypot(x - SS.HUB.x, z - SS.HUB.z) < SS.HUB_R) return 'Front de neige';
    for (const l of G.lifts) { if (Math.hypot(x - l.b.x, z - l.b.z) < 75) return 'arrivée ' + l.name; if (Math.hypot(x - l.a.x, z - l.a.z) < 75) return 'départ ' + l.name; }
    for (const p of G.pistes) { const c = SS.closestOnPiste(p, x, z); if (c.d < 60) return 'piste ' + p.name; }
    return null;
  }
  B.describe = describe;
  function snap(pt) {                              // aimante vers un raccord proche, sans jamais déplacer ce que le joueur a validé
    const G = SS.G; let best = null, bd = 70;
    const cand = [{ x: SS.HUB.x, z: SS.HUB.z }];
    for (const l of G.lifts) cand.push(l.a, l.b);
    for (const p of G.pistes) cand.push(p.pts[0], p.pts[p.pts.length - 1]);
    for (const c of cand) { const d = Math.hypot(c.x - pt.x, c.z - pt.z); if (d < bd) { bd = d; best = c; } }
    return best ? { x: best.x + (pt.x - best.x) * 0.15, z: best.z + (pt.z - best.z) * 0.15 } : pt;
  }

  B.start = function (kind, type) {
    B.cancel(true);
    B.tool = { kind: kind === 'autopiste' ? 'piste' : kind, auto: kind === 'autopiste', type, ctrl: [], width: B.width || 30, rot: 0 };
    R.showReach = B.tool.kind === 'piste'; R.drawOverlay();
    R.select(null); update(); SS.emit('build');
  };
  B.cancel = function (silent) { B.tool = null; B.an = null; B.autoMsg = null; R.clearPreview(); if (R.showReach) { R.showReach = false; R.drawOverlay(); } if (!silent) SS.emit('build'); };
  B.undo = function () { const t = B.tool; if (!t) return; if (t.kind === 'lift') { if (t.b) t.b = null; else t.a = null; } else if (t.ctrl.length) t.ctrl.pop(); else if (t.kind === 'building') t.pos = null; update(); SS.emit('build'); };
  B.rotate = function () { if (B.tool) { B.tool.rot += Math.PI / 4; update(); } };
  B.setWidth = function (w) { B.width = w; if (B.tool) { B.tool.width = w; update(); SS.emit('build'); } };

  function update(hover) {
    const t = B.tool; if (!t) return;
    B.an = null;
    if (t.kind === 'piste' || t.kind === 'road') {
      const pts = t.ctrl.slice(); if (hover && !R.coarse && pts.length) pts.push(hover);
      if (pts.length >= 2) {
        const an = t.kind === 'piste' ? SS.analyzePiste(pts, t.width) : SS.analyzeRoad(pts);
        if (t.kind === 'piste') R.previewPath(an.pts || pts, an.ok, t.width, t.ctrl); else R.previewPath(pts, an.ok, 9, t.ctrl);
        B.an = t.ctrl.length >= 2 ? (t.kind === 'piste' ? SS.analyzePiste(t.ctrl, t.width) : SS.analyzeRoad(t.ctrl)) : null; B.live = an;
      } else { R.previewPath(pts, true, 0, t.ctrl); B.live = null; }
    } else if (t.kind === 'lift') {
      const b = t.b || (hover && !R.coarse ? hover : null);
      if (t.a && b) { const an = SS.analyzeLift(t.type, t.a, b); R.previewPath(an.path || [t.a, b], an.ok, 0, t.b ? [t.a, t.b] : [t.a]); if (t.b) B.an = an; B.live = an; }
      else { R.previewPath(t.a ? [t.a] : [], true, 0, t.a ? [t.a] : []); B.live = null; }
    } else if (t.kind === 'building') {
      const p = t.pos || (hover && !R.coarse ? hover : null);
      if (p) { const an = SS.analyzeBuilding(t.type, p.x, p.z); R.previewBuilding(t.type, p.x, p.z, t.rot, an.ok); if (t.pos) B.an = an; B.live = an; }
    }
  }
  B.click = function (g) {
    const t = B.tool; if (!t || !g) return;
    if (t.kind === 'piste' && t.auto && !t.ctrl.length) {
      const st = snap(g), r = SS.autoPiste(st, null, t.width);
      if (r.err) { B.autoMsg = r.err; SS.emit('toast', '🧭 ' + r.err, 'warn'); update(); SS.emit('build'); return; }
      B.autoMsg = r.bad ? 'Tracé assisté trouvé, mais il enfreint une règle : ' + (r.an.err || '') : null; t.ctrl = r.ctrl; update(); SS.emit('build'); return;
    }
    if (t.kind === 'piste') t.ctrl.push(snap(g));
    else if (t.kind === 'road') t.ctrl.push({ x: g.x, z: g.z });
    else if (t.kind === 'lift') { const p = { x: g.x, z: g.z }; if (!t.a) t.a = p; else t.b = p; }
    else if (t.kind === 'building') t.pos = { x: g.x, z: g.z };
    else if (t.kind === 'demolish') { const s = pick(g, null); if (s && s.kind !== 'visitor') SS.emit('demolish:ask', s); return; }
    update(); SS.emit('build');
  };
  B.confirm = function () {
    const t = B.tool, an = B.an; if (!t || !an || !an.ok) return;
    if (!SS.canPay(an.cost)) { SS.emit('toast', 'Fonds insuffisants : il manque ' + SS.eur(an.cost - SS.G.money) + '.', 'bad'); return; }
    let made = null;
    if (t.kind === 'piste') made = SS.commitPiste(t.ctrl, an);
    else if (t.kind === 'road') made = SS.commitRoad(t.ctrl, an);
    else if (t.kind === 'lift') made = SS.commitLift(an);
    else if (t.kind === 'building') made = SS.commitBuilding(an, t.rot);
    if (!made) return;
    SS.emit('toast', '✅ ' + (made.name || (t.kind === 'building' ? D.building(t.type).name : 'Route')) + ' construit(e) pour ' + SS.eur(an.cost) + '.', 'good');
    SS.checkCareer(null);
    const again = { kind: t.kind, type: t.type };
    B.cancel(true); B.start(again.kind, again.type);
  };

  // ---------- sélection ----------
  const pp = {};
  function pick(g, screen) {
    const G = SS.G;
    if (screen) {
      let best = null, bd = R.coarse ? 26 : 16;
      for (const v of SS.Sim.visitors) {
        if (v.state === 'gone' || v.state === 'inS' || v.state === 'pending') continue;
        R.project(v.x, v.y + 1.5 * R.vscale, v.z, pp); if (!pp.vis) continue;
        const d = Math.hypot(pp.x - screen.x, pp.y - screen.y); if (d < bd) { bd = d; best = v; }
      }
      if (best) return { kind: 'visitor', obj: best };
    }
    if (!g) return null;
    for (const b of G.buildings) if (Math.hypot(b.x - g.x, b.z - g.z) < D.building(b.type).r + 4) return { kind: 'building', obj: b };
    let bl = null, bd = 22 + Cam.dist * 0.008;
    for (const l of G.lifts) { const d = Math.min(SS.segDist(g.x, g.z, l.a.x, l.a.z, l.b.x, l.b.z).d, Math.hypot(g.x - l.a.x, g.z - l.a.z) - 14, Math.hypot(g.x - l.b.x, g.z - l.b.z) - 14); if (d < bd) { bd = d; bl = l; } }
    if (bl) return { kind: 'lift', obj: bl };
    for (const p of G.pistes) if (SS.closestOnPiste(p, g.x, g.z).d < p.width / 2 + 6) return { kind: 'piste', obj: p };
    for (const r of G.roads) for (let i = 0; i < r.pts.length - 1; i++) if (SS.segDist(g.x, g.z, r.pts[i].x, r.pts[i].z, r.pts[i + 1].x, r.pts[i + 1].z).d < 12) return { kind: 'road', obj: r };
    return null;
  }
  B.highlight = function (s) {
    if (!s) return R.select(null);
    if (s.kind === 'building') R.select(R.circle(s.obj.x, s.obj.z, D.building(s.obj.type).r + 2));
    else if (s.kind === 'lift') R.select(s.obj.path);
    else if (s.kind === 'piste') R.select(s.obj.pts);
    else if (s.kind === 'road') R.select(s.obj.pts);
    else R.select(null);
  };

  // ---------- entrées : souris, tactile, clavier ----------
  B.initInput = function (canvas) {
    const ptrs = new Map(); let moved = 0, downT = 0, pinch = null;
    canvas.addEventListener('contextmenu', e => e.preventDefault());
    canvas.addEventListener('pointerdown', e => {
      canvas.setPointerCapture(e.pointerId); ptrs.set(e.pointerId, { x: e.clientX, y: e.clientY, b: e.button });
      if (ptrs.size === 1) { moved = 0; downT = performance.now(); } pinch = null;
    });
    canvas.addEventListener('pointermove', e => {
      const p = ptrs.get(e.pointerId);
      if (SS.Arcade.active) return;
      if (!p) { if (B.tool && !R.coarse) { B.hover = R.groundAt(e.clientX, e.clientY); update(B.hover); SS.emit('build:live'); } return; }
      const dx = e.clientX - p.x, dy = e.clientY - p.y; p.x = e.clientX; p.y = e.clientY;
      if (ptrs.size === 1) {
        moved += Math.abs(dx) + Math.abs(dy); if (moved < 7) return;
        if (p.b === 2 || p.b === 1 || e.shiftKey) { Cam.yaw -= dx * 0.006; Cam.pitch += dy * 0.005; } else Cam.pan(dx, dy);
      } else if (ptrs.size === 2) {
        const [a, b] = Array.from(ptrs.values()), d = Math.hypot(a.x - b.x, a.y - b.y), ang = Math.atan2(b.y - a.y, b.x - a.x), my = (a.y + b.y) / 2;
        if (pinch) { Cam.dist *= pinch.d / Math.max(20, d); let da = ang - pinch.ang; if (da > 3) da -= 6.283; if (da < -3) da += 6.283; Cam.yaw -= da; Cam.pitch += (my - pinch.my) * 0.004; }
        pinch = { d, ang, my }; moved = 99;
      }
    });
    const up = e => {
      const p = ptrs.get(e.pointerId); ptrs.delete(e.pointerId); pinch = null;
      if (!p || SS.Arcade.active) return;
      if (ptrs.size === 0 && moved < 7 && p.b === 0) {
        const g = R.groundAt(e.clientX, e.clientY);
        if (B.tool) B.click(g);
        else { const s = pick(g, { x: e.clientX, y: e.clientY }); B.highlight(s); SS.emit('select', s); }
      }
    };
    canvas.addEventListener('pointerup', up); canvas.addEventListener('pointercancel', up);
    canvas.addEventListener('wheel', e => { e.preventDefault(); if (!SS.Arcade.active) Cam.dist *= Math.exp(SS.clamp(e.deltaY, -300, 300) * 0.0013); }, { passive: false });
    const keys = B.keys = {};
    window.addEventListener('keydown', e => {
      if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA')) return;
      keys[e.code] = true;
      if (SS.Arcade.active) { if (e.code === 'Escape') SS.Arcade.stop(); if (e.code === 'Space') e.preventDefault(); return; }
      if (!SS.G) return;
      if (e.code === 'Space') { SS.G.paused = !SS.G.paused; e.preventDefault(); SS.emit('speed'); }
      else if (e.code === 'Digit1' || e.code === 'Digit2' || e.code === 'Digit3') { SS.G.speed = { Digit1: 1, Digit2: 3, Digit3: 8 }[e.code]; SS.G.paused = false; SS.emit('speed'); }
      else if (e.code === 'Escape') { if (B.tool) B.cancel(); else { B.highlight(null); SS.emit('select', null); } }
      else if (e.code === 'KeyR') B.rotate();
      else if (e.code === 'Enter') B.confirm();
      else if (e.code === 'Backspace') B.undo();
    });
    window.addEventListener('keyup', e => { keys[e.code] = false; });
    B.frame = function (dt) {
      if (SS.Arcade.active) return;
      const k = 520 * dt; let dx = 0, dy = 0;
      if (keys.KeyA || keys.ArrowLeft) dx += k; if (keys.KeyD || keys.ArrowRight) dx -= k; if (keys.KeyW || keys.ArrowUp) dy += k; if (keys.KeyS || keys.ArrowDown) dy -= k;
      if (dx || dy) Cam.pan(dx, dy);
      if (keys.KeyQ) Cam.yaw += dt * 1.4; if (keys.KeyE) Cam.yaw -= dt * 1.4;
    };
  };
})();
