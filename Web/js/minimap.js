// SkiStation Web — mini-carte : relief ombré, pistes, remontées, visiteurs, champ de la caméra ; un clic recentre la vue
(function () {
  'use strict';
  const SS = window.SS, T = SS.Terrain, D = SS.Data;
  const MM = SS.Minimap = { on: true };
  let cv, ctx, base, S = 176, acc = 0;
  const W2M = v => (v + SS.HALF) / SS.WORLD * S;

  MM.init = function () {
    cv = document.getElementById('minimap'); if (!cv) return;
    const dpr = Math.min(2, window.devicePixelRatio || 1);
    cv.width = cv.height = S * dpr; ctx = cv.getContext('2d'); ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    const go = e => { const r = cv.getBoundingClientRect(), x = (e.clientX - r.left) / r.width * SS.WORLD - SS.HALF, z = (e.clientY - r.top) / r.height * SS.WORLD - SS.HALF; SS.Cam.focus(x, z); e.preventDefault(); };
    let drag = false;
    cv.addEventListener('pointerdown', e => { drag = true; cv.setPointerCapture(e.pointerId); go(e); });
    cv.addEventListener('pointermove', e => { if (drag) go(e); });
    cv.addEventListener('pointerup', () => { drag = false; });
    cv.addEventListener('pointercancel', () => { drag = false; });
    SS.on('game:start', MM.rebuild);
  };
  // relief ombré calculé une fois par partie (altitude + éclairage nord-ouest)
  MM.rebuild = function () {
    const n = 176, img = new ImageData(n, n), d = img.data, step = SS.WORLD / n;
    for (let j = 0; j < n; j++) for (let i = 0; i < n; i++) {
      const x = -SS.HALF + (i + 0.5) * step, z = -SS.HALF + (j + 0.5) * step, h = T.getH(x, z);
      const sh = SS.clamp(0.75 + (T.getH(x - step, z - step) - T.getH(x + step, z + step)) / (step * 2.6), 0.35, 1.15);
      const e = SS.clamp((h - 1640) / 2560, 0, 1), rock = SS.smooth(0.55, 0.9, T.slope(x, z));
      let r = 214 - 40 * e, g = 224 - 30 * e, b = 238 - 10 * e;
      if (h < 2250) { const f = 0.35 * SS.smooth(2250, 1900, h); r -= 70 * f; g -= 40 * f; b -= 70 * f; }
      r = SS.lerp(r, 120, rock); g = SS.lerp(g, 118, rock); b = SS.lerp(b, 116, rock);
      const k = (j * n + i) * 4; d[k] = r * sh; d[k + 1] = g * sh; d[k + 2] = b * sh; d[k + 3] = 255;
    }
    base = document.createElement('canvas'); base.width = base.height = n; base.getContext('2d').putImageData(img, 0, 0);
  };

  MM.frame = function (dt) {
    if (!ctx || !base || !SS.G) return;
    const hide = !MM.on || SS.Arcade.active || !!SS.Build.tool || (innerWidth < 720 && !!SS.UI.panel);
    cv.hidden = hide; if (hide) return;
    acc += dt; if (acc < 0.25) return; acc = 0;
    const G = SS.G;
    ctx.clearRect(0, 0, S, S); ctx.drawImage(base, 0, 0, S, S);
    ctx.lineCap = 'round'; ctx.lineJoin = 'round';
    for (const r of G.roads) { ctx.strokeStyle = '#4b5361'; ctx.lineWidth = 1.4; ctx.beginPath(); r.pts.forEach((p, i) => i ? ctx.lineTo(W2M(p.x), W2M(p.z)) : ctx.moveTo(W2M(p.x), W2M(p.z))); ctx.stroke(); }
    for (const p of G.pistes) {
      ctx.strokeStyle = D.DIFF[p.diff].color; ctx.globalAlpha = SS.pisteOpen(p) ? 1 : 0.35; ctx.lineWidth = 2.2;
      ctx.beginPath(); for (let i = 0; i < p.pts.length; i += 3) { const q = p.pts[i]; i ? ctx.lineTo(W2M(q.x), W2M(q.z)) : ctx.moveTo(W2M(q.x), W2M(q.z)); }
      const e = p.pts[p.pts.length - 1]; ctx.lineTo(W2M(e.x), W2M(e.z)); ctx.stroke();
    }
    ctx.globalAlpha = 1;
    for (const l of G.lifts) {
      const bad = !SS.liftBoarding(l) && G.time > SS.LIFT_OPEN && G.time < SS.LIFT_CLOSE;
      ctx.strokeStyle = bad ? '#ff5d5d' : '#1b1f27'; ctx.lineWidth = 1.6; ctx.setLineDash([3, 2]);
      ctx.beginPath(); ctx.moveTo(W2M(l.a.x), W2M(l.a.z)); ctx.lineTo(W2M(l.b.x), W2M(l.b.z)); ctx.stroke(); ctx.setLineDash([]);
    }
    ctx.fillStyle = '#ffb238';
    for (const b of G.buildings) ctx.fillRect(W2M(b.x) - 1.5, W2M(b.z) - 1.5, 3, 3);
    ctx.fillStyle = 'rgba(255,255,255,0.9)';
    const vs = SS.Sim.visitors;
    for (let i = 0; i < vs.length; i += 2) { const v = vs[i]; if (v.state === 'gone' || v.state === 'pending' || v.state === 'inS') continue; ctx.fillRect(W2M(v.x) - 0.5, W2M(v.z) - 0.5, 1, 1); }
    // champ de la caméra
    const C = SS.Cam, cx = W2M(C.tx), cz = W2M(C.tz), r = SS.clamp(C.dist / SS.WORLD * S * 0.7, 6, 60), a = Math.atan2(-Math.sin(C.yaw), -Math.cos(C.yaw));
    ctx.fillStyle = 'rgba(255,178,56,0.22)'; ctx.strokeStyle = '#ffb238'; ctx.lineWidth = 1.2;
    ctx.beginPath(); ctx.moveTo(cx, cz); ctx.arc(cx, cz, r, a - 0.5, a + 0.5); ctx.closePath(); ctx.fill(); ctx.stroke();
    ctx.fillStyle = '#ffb238'; ctx.beginPath(); ctx.arc(cx, cz, 2.4, 0, 7); ctx.fill();
  };
})();
