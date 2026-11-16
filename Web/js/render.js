// SkiStation Web — rendu 3D : scène, caméra orbitale, objets du domaine, visiteurs instanciés, jour/nuit
(function () {
  'use strict';
  const SS = window.SS, T = SS.Terrain, D = SS.Data, Mo = SS.Models;
  const R = SS.R = { objs: new Map(), labelsOn: true, mapMode: false };
  const Cam = SS.Cam = { tx: 0, tz: 2300, yaw: 0, pitch: 0.75, dist: 1700 };
  let scene, camera, renderer, sun, hemi, winMat, bodyMat, cableMat, ovCanvas, ovCtx, ovTex, OV = 2048;
  let jackets, rests, MAXV = 1700, snowPts, skirt;
  const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), v3 = new THREE.Vector3(), s3 = new THREE.Vector3(), e3 = new THREE.Euler(), c3 = new THREE.Color(), tmp = {};
  const UP = new THREE.Vector3(0, 1, 0);

  R.init = function (canvas) {
    const coarse = window.matchMedia && matchMedia('(pointer:coarse)').matches;
    R.coarse = coarse;
    renderer = R.renderer = new THREE.WebGLRenderer({ canvas, antialias: !coarse, powerPreference: 'high-performance' });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, coarse ? 1.5 : 2));
    OV = (!coarse && renderer.capabilities.maxTextureSize >= 4096) ? 4096 : 2048;
    scene = R.scene = new THREE.Scene();
    scene.background = new THREE.Color(0x7db9ee);
    scene.fog = new THREE.Fog(0x7db9ee, 5000, 24000);
    camera = R.camera = new THREE.PerspectiveCamera(45, 1, 4, 40000);
    hemi = new THREE.HemisphereLight(0xdfeeff, 0x8c97a8, 0.95); scene.add(hemi);
    sun = new THREE.DirectionalLight(0xfff4e0, 1.6); scene.add(sun);
    winMat = new THREE.MeshBasicMaterial({ color: 0x2a3644 });
    bodyMat = new THREE.MeshLambertMaterial({ vertexColors: true });
    cableMat = new THREE.LineBasicMaterial({ color: 0x23272e });
    ovCanvas = document.createElement('canvas'); ovCanvas.width = ovCanvas.height = OV; ovCtx = ovCanvas.getContext('2d');
    ovTex = new THREE.CanvasTexture(ovCanvas); ovTex.flipY = false; ovTex.colorSpace = THREE.SRGBColorSpace; ovTex.anisotropy = Math.min(8, renderer.capabilities.getMaxAnisotropy());
    R.resize(); window.addEventListener('resize', R.resize);
  };
  R.resize = function () {
    const w = window.innerWidth, h = window.innerHeight;
    renderer.setSize(w, h, false); camera.aspect = w / h; camera.updateProjectionMatrix();
  };

  // construit tout le monde visuel pour la partie courante
  R.buildWorld = function () {
    for (const o of scene.children.slice()) if (o !== hemi && o !== sun) scene.remove(o);
    R.objs.clear();
    T.summer = !!SS.G.summer;
    scene.add(T.buildMesh(ovTex));
    scene.add(T.buildTrees());
    skirt = new THREE.Mesh(new THREE.PlaneGeometry(90000, 90000).rotateX(-Math.PI / 2), new THREE.MeshLambertMaterial({ color: 0xe9eef5 })); skirt.position.y = 1632; scene.add(skirt);
    for (const d of SS.DECOR) addStatic(Mo.building(d.k, d.x), d.x, T.getH(d.x, d.z), d.z, (d.x * 0.37) % 6.28);
    const per = Mo.person();
    jackets = new THREE.InstancedMesh(per.jacket, new THREE.MeshLambertMaterial({ vertexColors: false }), MAXV);
    rests = new THREE.InstancedMesh(per.rest, bodyMat, MAXV);
    jackets.frustumCulled = rests.frustumCulled = false; jackets.count = rests.count = 0;
    jackets.instanceMatrix.setUsage(THREE.DynamicDrawUsage); rests.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
    jackets.setColorAt(0, c3.set(0xffffff)); scene.add(jackets, rests);
    R.fleet = new THREE.Group(); scene.add(R.fleet); R.groomMeshes = []; R.rescueMeshes = [];
    R.preview = new THREE.Group(); scene.add(R.preview);
    R.hl = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineBasicMaterial({ color: 0xffd23f, depthTest: false, transparent: true })); R.hl.renderOrder = 10; R.hl.frustumCulled = false; R.hl.visible = false; scene.add(R.hl);
    const sg = new THREE.BufferGeometry(), sp = new Float32Array(1400 * 3); for (let i = 0; i < sp.length; i++) sp[i] = Math.random();
    sg.setAttribute('position', new THREE.BufferAttribute(sp, 3));
    snowPts = new THREE.Points(sg, new THREE.PointsMaterial({ color: 0xffffff, size: 5, sizeAttenuation: true, transparent: true, opacity: 0.85, depthWrite: false })); snowPts.frustumCulled = false; scene.add(snowPts);
    for (const b of SS.G.buildings) addBuilding(b);
    for (const l of SS.G.lifts) addLift(l);
    for (const p of SS.G.pistes) addPiste(p);
    R.drawOverlay();
  };
  function addStatic(geo, x, y, z, ry) {
    const g = new THREE.Group(); g.add(new THREE.Mesh(geo.body, bodyMat)); if (geo.windows) g.add(new THREE.Mesh(geo.windows, winMat));
    g.position.set(x, y, z); g.rotation.y = ry || 0; scene.add(g); return g;
  }
  function addBuilding(b) { const def = D.building(b.type); R.objs.set(b, addStatic(Mo.building(def.look, def.r), b.x, b.y, b.z, b.rot)); }
  function addLift(l) {
    const def = D.lift(l.type), g = new THREE.Group(), st = Mo.liftStatic(l, def), off = Mo.cableOffset(def);
    g.add(new THREE.Mesh(st.body, bodyMat)); if (st.windows) g.add(new THREE.Mesh(st.windows, winMat));
    const dx = l.b.x - l.a.x, dz = l.b.z - l.a.z, hl = Math.hypot(dx, dz), px = -dz / hl * off, pz = dx / hl * off;
    if (def.kind !== 'carpet') for (const sgn of [1, -1]) {
      const pts = l.path.map(p => new THREE.Vector3(p.x + px * sgn, p.y, p.z + pz * sgn));
      g.add(new THREE.Line(new THREE.BufferGeometry().setFromPoints(pts), cableMat));
    }
    const veh = new THREE.InstancedMesh(Mo.vehicle(def), bodyMat, l.n); veh.frustumCulled = false; veh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
    g.add(veh); g.userData = { veh, px, pz, yaw: Math.atan2(dx, dz), def }; g.children.forEach(c => c.frustumCulled = false);
    scene.add(g); R.objs.set(l, g);
  }
  function addPiste(p) {
    const P = [], col = D.DIFF[p.diff].hex, half = p.width / 2;
    for (let s = 20; s < p.len; s += 45) {
      SS.pisteAt(p, s, tmp); const d = Math.hypot(tmp.dx, tmp.dz) || 1, nx = -tmp.dz / d, nz = tmp.dx / d;
      for (const sg of [-1, 1]) {
        const x = tmp.x + nx * half * sg, z = tmp.z + nz * half * sg, y = T.getH(x, z);
        P.push({ geo: new THREE.CylinderGeometry(0.35, 0.35, 3.4, 5), matrix: new THREE.Matrix4().makeTranslation(x, y + 1.7, z), color: sg < 0 ? col : 0xf28a1c });
        P.push({ geo: new THREE.SphereGeometry(0.75, 6, 5), matrix: new THREE.Matrix4().makeTranslation(x, y + 3.5, z), color: col });
      }
    }
    if (p.cannons) for (let s = 40; s < p.len; s += 90) {
      SS.pisteAt(p, s, tmp); const d = Math.hypot(tmp.dx, tmp.dz) || 1, x = tmp.x - tmp.dz / d * (half + 5), z = tmp.z + tmp.dx / d * (half + 5), y = T.getH(x, z);
      const m = new THREE.Matrix4().makeRotationZ(0.25); m.setPosition(x, y + 5, z);
      P.push({ geo: new THREE.CylinderGeometry(0.3, 0.5, 10, 6), matrix: m, color: 0xf2c230 }, { geo: new THREE.BoxGeometry(1.6, 1.6, 1.6), matrix: new THREE.Matrix4().makeTranslation(x - 1.2, y + 10, z), color: 0x8d9299 });
    }
    const lamps = [];
    if (p.lit) for (let s = 30; s < p.len; s += 70) {
      SS.pisteAt(p, s, tmp); const d = Math.hypot(tmp.dx, tmp.dz) || 1, sg = (s / 70 | 0) % 2 ? 1 : -1, x = tmp.x - tmp.dz / d * (half + 3) * sg, z = tmp.z + tmp.dx / d * (half + 3) * sg, y = T.getH(x, z);
      P.push({ geo: new THREE.CylinderGeometry(0.25, 0.35, 11, 5), matrix: new THREE.Matrix4().makeTranslation(x, y + 5.5, z), color: 0x5d636c });
      lamps.push(x, y + 11.5, z);
    }
    const g = new THREE.Group(); if (P.length) g.add(new THREE.Mesh(SS.mergeParts(P), bodyMat));
    if (lamps.length) {
      const lg = new THREE.BufferGeometry(); lg.setAttribute('position', new THREE.Float32BufferAttribute(lamps, 3));
      const halo = new THREE.Points(lg, haloMat()); halo.frustumCulled = false; halo.userData.halo = true; g.add(halo);
      const heads = new THREE.Points(lg, headMat()); heads.frustumCulled = false; heads.userData.halo = true; g.add(heads);
    }
    scene.add(g); R.objs.set(p, g);
  }
  // halos des lampadaires : un dégradé radial partagé, visible la nuit
  let _halo, _head;
  function glowTex() { const c = document.createElement('canvas'); c.width = c.height = 64; const x = c.getContext('2d'), g = x.createRadialGradient(32, 32, 0, 32, 32, 32); g.addColorStop(0, 'rgba(255,226,160,1)'); g.addColorStop(0.35, 'rgba(255,200,120,0.45)'); g.addColorStop(1, 'rgba(255,190,110,0)'); x.fillStyle = g; x.fillRect(0, 0, 64, 64); const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t; }
  function haloMat() { return _halo || (_halo = new THREE.PointsMaterial({ map: glowTex(), size: 70, sizeAttenuation: true, transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending, fog: false })); }
  function headMat() { return _head || (_head = new THREE.PointsMaterial({ color: 0xfff1c8, size: 3, sizeAttenuation: true, transparent: true, opacity: 0, depthWrite: false })); }
  R.updateLamps = function () {
    if (!_halo) return; const G = SS.G, on = SS.researched('night') && G.time > 16.5 * 3600 && G.time < SS.NIGHT_CLOSE + 1200;
    const k = on ? SS.clamp(1 - (R.day || 0) * 1.6, 0, 1) : 0; _halo.opacity = 0.85 * k; _head.opacity = k;
  };
  function remove(o) { const g = R.objs.get(o); if (g) { scene.remove(g); g.traverse(c => c.geometry && c.geometry.dispose()); R.objs.delete(o); } }
  R.refreshPiste = function (p) { remove(p); addPiste(p); R.drawOverlay(); };
  SS.on('building:add', addBuilding); SS.on('lift:add', addLift); SS.on('piste:add', addPiste);
  SS.on('removed', (kind, o) => { remove(o); R.select(null); });
  SS.on('lift:refresh', l => { remove(l); addLift(l); });
  SS.on('world', () => R.drawOverlay());
  SS.on('season', () => {
    if (!T.geo) return; T.summer = !!SS.G.summer; T.updateRegion(0, 0, SS.N - 1, SS.N - 1);
    scene.remove(T.treeMesh); T.treeMesh.geometry.dispose(); scene.add(T.buildTrees()); R.drawOverlay();
  });

  // --- texture de sol : pistes, routes, places (aucun z-fighting : c'est peint sur le relief) ---
  const heat = t => 'hsl(' + Math.round(130 - 130 * SS.clamp(t, 0, 1)) + ',85%,' + Math.round(48 + 8 * t) + '%)';
  R.drawOverlay = function () {
    const G = SS.G, c = ovCtx, k = OV / SS.WORLD, X = x => (x + SS.HALF) * k, Z = z => (z + SS.HALF) * k;
    c.fillStyle = '#ffffff'; c.fillRect(0, 0, OV, OV);
    c.lineCap = 'round'; c.lineJoin = 'round';
    if (R.showReach) {                                 // zones d'où une piste peut rejoindre le front de neige
      if (!R.reachCv || R.reachVer !== SS.Net.version + '|' + SS.G.seed) { const m = SS.reachMask(), cv = R.reachCv || (R.reachCv = document.createElement('canvas')); cv.width = cv.height = SS.N; const x = cv.getContext('2d'), im = x.createImageData(SS.N, SS.N); for (let i = 0; i < m.length; i++) if (m[i]) { im.data[i * 4] = 70; im.data[i * 4 + 1] = 215; im.data[i * 4 + 2] = 120; im.data[i * 4 + 3] = 70; } x.putImageData(im, 0, 0); R.reachVer = SS.Net.version + '|' + SS.G.seed; }
      c.imageSmoothingEnabled = true; c.drawImage(R.reachCv, 0, 0, OV, OV);
    }
    let g = c.createRadialGradient(X(-60), Z(2790), 0, X(-60), Z(2790), 420 * k); g.addColorStop(0, 'rgba(196,202,212,0.9)'); g.addColorStop(1, 'rgba(255,255,255,0)'); c.fillStyle = g; c.fillRect(X(-600), Z(2300), 1200 * k, 1000 * k);
    g = c.createRadialGradient(X(SS.HUB.x), Z(SS.HUB.z), 0, X(SS.HUB.x), Z(SS.HUB.z), SS.HUB_R * k); g.addColorStop(0, 'rgba(190,214,245,1)'); g.addColorStop(0.7, 'rgba(205,222,245,0.8)'); g.addColorStop(1, 'rgba(255,255,255,0)'); c.fillStyle = g; c.fillRect(X(SS.HUB.x - SS.HUB_R), Z(SS.HUB.z - SS.HUB_R), 2 * SS.HUB_R * k, 2 * SS.HUB_R * k);
    c.strokeStyle = 'rgba(255,178,56,0.9)'; c.lineWidth = Math.max(2, 5 * k); c.setLineDash([22 * k, 16 * k]); c.beginPath(); c.arc(X(SS.HUB.x), Z(SS.HUB.z), SS.HUB_R * k, 0, 7); c.stroke(); c.setLineDash([]);
    const path = pts => { c.beginPath(); pts.forEach((p, i) => i ? c.lineTo(X(p.x), Z(p.z)) : c.moveTo(X(p.x), Z(p.z))); c.stroke(); };
    for (const r of G.roads) { c.strokeStyle = '#d9dde3'; c.lineWidth = Math.max(4, 16 * k); path(r.pts); c.strokeStyle = '#596270'; c.lineWidth = Math.max(2, 9 * k); path(r.pts); }
    for (const b of G.buildings) { const def = D.building(b.type); c.fillStyle = 'rgba(190,197,208,0.8)'; c.beginPath(); c.arc(X(b.x), Z(b.z), (def.r + 3) * k, 0, 7); c.fill(); }
    for (const p of G.pistes) {
      const col = D.DIFF[p.diff].color, open = SS.pisteOpen(p);
      if (G.summer && !R.mapMode) {                    // l'été, la piste devient un sentier balisé
        c.strokeStyle = 'rgba(214,226,170,0.55)'; c.lineWidth = p.width * k; path(p.pts);
        c.strokeStyle = '#8a6a45'; c.lineWidth = Math.max(1.5, 7 * k); path(p.pts);
        c.strokeStyle = col; c.setLineDash([10 * k, 22 * k]); c.lineWidth = Math.max(1, 3 * k); path(p.pts); c.setLineDash([]);
        continue;
      }
      c.strokeStyle = col; c.lineWidth = (p.width + 7) * k; path(p.pts);
      let fill = R.mapMode ? col : open ? '#dfe9f7' : '#b9bfc9';
      if (R.mapMode === 'crowd') fill = heat(Math.min(1, p.on / (p.len * p.width / 450)));
      else if (R.mapMode === 'snow') fill = heat(1 - SS.Weather.quality(p, 1));
      c.strokeStyle = fill; c.globalAlpha = R.mapMode === 'map' ? 0.75 : 1; c.lineWidth = Math.max(1, (p.width - 3) * k); path(p.pts); c.globalAlpha = 1;
      if (!R.mapMode && open && p.groomed > 0.5) { c.strokeStyle = 'rgba(255,255,255,0.75)'; c.lineWidth = Math.max(1, p.width * 0.28 * k); path(p.pts); }
    }
    ovTex.needsUpdate = true;
  };

  // --- prévisualisation de construction ---
  R.clearPreview = function () { for (const o of R.preview.children.slice()) { R.preview.remove(o); o.geometry && o.geometry.dispose(); } };
  R.previewPath = function (pts, ok, width, dots) {      // pts: [{x,z,(y)}]
    R.clearPreview(); if (!pts.length) return;
    const col = ok ? 0x35d07f : 0xff4d4d;
    if (width && pts.length > 1) {
      const pos = [], idx = [];
      for (let i = 0; i < pts.length; i++) {
        const a = pts[Math.max(0, i - 1)], b = pts[Math.min(pts.length - 1, i + 1)], d = Math.hypot(b.x - a.x, b.z - a.z) || 1, nx = -(b.z - a.z) / d, nz = (b.x - a.x) / d;
        for (const sgn of [-1, 0, 1]) { const x = pts[i].x + nx * width / 2 * sgn, z = pts[i].z + nz * width / 2 * sgn; pos.push(x, T.getH(x, z) + 2.5, z); }
        if (i) { const o = (i - 1) * 3; idx.push(o, o + 3, o + 1, o + 1, o + 3, o + 4, o + 1, o + 4, o + 2, o + 2, o + 4, o + 5); }
      }
      const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setIndex(idx);
      const m = new THREE.Mesh(g, new THREE.MeshBasicMaterial({ color: col, transparent: true, opacity: 0.45, side: THREE.DoubleSide, depthWrite: false })); m.frustumCulled = false; R.preview.add(m);
    }
    const lp = pts.map(p => new THREE.Vector3(p.x, (p.y !== undefined ? p.y : T.getH(p.x, p.z)) + 3, p.z));
    const line = new THREE.Line(new THREE.BufferGeometry().setFromPoints(lp), new THREE.LineBasicMaterial({ color: col, depthTest: false, transparent: true })); line.renderOrder = 11; line.frustumCulled = false; R.preview.add(line);
    for (const p of (dots || [])) { const s = new THREE.Mesh(new THREE.SphereGeometry(1, 10, 8), new THREE.MeshBasicMaterial({ color: 0xffffff, depthTest: false, transparent: true })); s.position.set(p.x, T.getH(p.x, p.z) + 3, p.z); s.userData.dot = true; s.renderOrder = 12; R.preview.add(s); }
  };
  R.previewBuilding = function (defId, x, z, rot, ok) {
    R.clearPreview(); const def = D.building(defId), geo = Mo.building(def.look, def.r);
    const m = new THREE.Mesh(geo.body, new THREE.MeshBasicMaterial({ color: ok ? 0x35d07f : 0xff4d4d, transparent: true, opacity: 0.6 }));
    m.position.set(x, T.getH(x, z), z); m.rotation.y = rot || 0; R.preview.add(m);
    const ring = new THREE.Mesh(new THREE.RingGeometry(def.r - 1, def.r, 40).rotateX(-Math.PI / 2), new THREE.MeshBasicMaterial({ color: ok ? 0x35d07f : 0xff4d4d, depthTest: false, transparent: true, side: THREE.DoubleSide }));
    ring.position.set(x, T.getH(x, z) + 1.5, z); ring.renderOrder = 11; R.preview.add(ring);
  };

  // --- sélection ---
  R.select = function (pts) {
    if (!pts) { R.hl.visible = false; return; }
    R.hl.geometry.dispose(); R.hl.geometry = new THREE.BufferGeometry().setFromPoints(pts.map(p => new THREE.Vector3(p.x, (p.y !== undefined ? p.y : T.getH(p.x, p.z)) + 4, p.z))); R.hl.visible = true;
  };
  R.circle = (x, z, r) => { const a = []; for (let i = 0; i <= 40; i++) a.push({ x: x + Math.cos(i / 40 * 6.283) * r, z: z + Math.sin(i / 40 * 6.283) * r }); return a; };

  // --- caméra ---
  Cam.apply = function () {
    Cam.tx = SS.clamp(Cam.tx, -SS.HALF, SS.HALF); Cam.tz = SS.clamp(Cam.tz, -SS.HALF, SS.HALF);
    Cam.pitch = SS.clamp(Cam.pitch, 0.18, 1.5); Cam.dist = SS.clamp(Cam.dist, 60, 11000);
    const ty = T.getH(Cam.tx, Cam.tz), cp = Math.cos(Cam.pitch), sp = Math.sin(Cam.pitch);
    let x = Cam.tx + Math.sin(Cam.yaw) * cp * Cam.dist, z = Cam.tz + Math.cos(Cam.yaw) * cp * Cam.dist, y = ty + sp * Cam.dist;
    const gh = T.getH(SS.clamp(x, -SS.HALF, SS.HALF), SS.clamp(z, -SS.HALF, SS.HALF)); if (y < gh + 12) y = gh + 12;
    camera.position.set(x, y, z); camera.lookAt(Cam.tx, ty, Cam.tz);
  };
  Cam.pan = function (dx, dy) {               // déplacement écran -> sol
    Cam.follow = null;
    const k = Cam.dist * 0.0016, sy = Math.sin(Cam.yaw), cy = Math.cos(Cam.yaw);
    Cam.tx -= (cy * dx + sy * dy) * k; Cam.tz -= (-sy * dx + cy * dy) * k;
  };
  Cam.focus = function (x, z, dist) { Cam.follow = null; Cam.tx = x; Cam.tz = z; if (dist) Cam.dist = dist; };
  R.groundAt = function (cx, cy) {
    const rect = renderer.domElement.getBoundingClientRect();
    v3.set((cx - rect.left) / rect.width * 2 - 1, -((cy - rect.top) / rect.height) * 2 + 1, 0.5).unproject(camera).sub(camera.position).normalize();
    return T.raycast(camera.position, v3);
  };
  R.project = function (x, y, z, out) {
    v3.set(x, y, z).project(camera); out.x = (v3.x + 1) / 2 * window.innerWidth; out.y = (1 - v3.y) / 2 * window.innerHeight; out.vis = v3.z < 1 && v3.z > -1; return out;
  };

  // --- lumière, ciel, neige qui tombe ---
  const SKY_N = new THREE.Color(0x0e1830), SUNSET = new THREE.Color(0xf3a56b), sky = new THREE.Color();
  function updateSky() {
    const G = SS.G, h = G.time / 3600, w = D.WEATHER[G.weather.type];
    const day = SS.smooth(6.2, 7.3, h) * (1 - SS.smooth(17.2, 19.2, h));
    const edge = Math.max(0, 1 - Math.abs(h - 17.8) / 1.1) + Math.max(0, 1 - Math.abs(h - 7.4) / 0.6);
    sky.set(w.sky).lerp(SUNSET, Math.min(0.55, edge * 0.55) * (w.light > 0.7 ? 1 : 0.3)); sky.lerp(SKY_N, 1 - day);
    scene.background.copy(sky); scene.fog.color.copy(sky);
    const vis = w.snow > 25 ? 0.35 : w.snow > 0 ? 0.6 : 1; scene.fog.near = 5000 * vis; scene.fog.far = 24000 * vis;
    const ang = (h - 12.5) / 12 * Math.PI;
    sun.position.set(Math.sin(ang) * 0.8 + 0.25, Math.max(0.12, Math.cos(ang) * 0.9), 0.55).normalize();
    sun.intensity = 1.7 * day * w.light; sun.color.setHex(0xfff4e0).lerp(SUNSET, Math.min(0.7, edge * 0.7));
    hemi.intensity = 0.5 + 0.5 * day * (0.6 + 0.4 * w.light); hemi.color.setHex(0xdfeeff).lerp(c3.setHex(0x6f8fd0), 1 - day);
    winMat.color.setHex(0x2a3644).lerp(c3.setHex(0xffd27a), SS.smooth(0.55, 0.15, day));
    skirt.material.color.setHex(SS.G.summer ? 0x4f7a3a : 0xe9eef5);
    R.day = day;
    snowPts.visible = w.snow > 0 || !!w.rain; snowPts.material.color.setHex(w.rain ? 0x9fc4e8 : 0xffffff);
    if (snowPts.visible) {
      const a = snowPts.geometry.attributes.position.array, sz = Math.max(300, Cam.dist * 0.9), t = performance.now() / 1000;
      snowPts.position.set(Cam.tx - sz / 2, T.getH(Cam.tx, Cam.tz) - sz * 0.1, Cam.tz - sz / 2); snowPts.scale.set(sz, sz * 0.8, sz);
      for (let i = 1; i < a.length; i += 3) { a[i] -= 0.004 * (1 + (i % 7) * 0.1); if (a[i] < 0) a[i] += 1; a[i - 1] += Math.sin(t + i) * 0.0006; }
      snowPts.geometry.attributes.position.needsUpdate = true; snowPts.material.size = sz * 0.006;
    }
  }

  // --- visiteurs et véhicules ---
  function put(i, x, y, z, yaw, s, lie) {
    e3.set(lie ? -1.45 : 0, yaw, 0, 'YXZ'); q.setFromEuler(e3); v3.set(x, y, z); s3.set(s, s, s); m4.compose(v3, q, s3);
    jackets.setMatrixAt(i, m4); rests.setMatrixAt(i, m4);
  }
  function updateLiftsVis() {
    const hf = 1;
    for (const l of SS.G.lifts) {
      const g = R.objs.get(l); if (!g) continue;
      const u = g.userData, def = u.def, veh = u.veh, L2 = 2 * l.L, hang = Mo.hang(def), vs = def.kind === 'tow' ? 1 : 1.5;
      for (let i = 0; i < l.n; i++) {
        let s = (l.phase + i * l.D) % L2; const up = s < l.L; if (!up) s = L2 - s;
        SS.liftPos(l, s, tmp); const sg = up ? 1 : -1;
        const x = tmp.x + u.px * sg, z = tmp.z + u.pz * sg;
        e3.set(0, u.yaw + (up ? 0 : Math.PI), 0); q.setFromEuler(e3); v3.set(x, tmp.y, z); s3.set(vs, vs, vs); m4.compose(v3, q, s3); veh.setMatrixAt(i, m4);
        const riders = l.veh[i];
        for (let k = 0; k < riders.length; k++) {
          const v = riders[k], off = (k - (riders.length - 1) / 2) * 0.9;
          v.x = x + Math.cos(u.yaw) * off; v.z = z - Math.sin(u.yaw) * off;
          v.y = def.kind === 'tow' || def.kind === 'carpet' ? T.getH(v.x, v.z) : tmp.y - hang * vs; v.hx = Math.sin(u.yaw); v.hz = Math.cos(u.yaw);
          v.hidden = def.kind === 'gondola' || def.kind === 'cablecar' || def.kind === 'funi';
        }
      }
      veh.instanceMatrix.needsUpdate = true;
      const qn = Math.min(l.queue.length, 80), ux = -Math.sin(u.yaw), uz = -Math.cos(u.yaw), sc = R.vscale;
      for (let k = 0; k < qn; k++) { const v = l.queue[k], row = (k / 4 | 0), colm = k % 4 - 1.5; v.x = l.a.x + ux * (9 + row * 1.6 * sc) + uz * colm * 1.3 * sc; v.z = l.a.z + uz * (9 + row * 1.6 * sc) - ux * colm * 1.3 * sc; v.y = T.getH(v.x, v.z); v.hx = -ux; v.hz = -uz; }
      for (let k = qn; k < l.queue.length; k++) { const v = l.queue[k]; v.x = l.a.x + ux * (9 + 20 * 1.6 * sc); v.z = l.a.z + uz * (9 + 20 * 1.6 * sc); v.y = T.getH(v.x, v.z); }
    }
  }
  function updateVisitorsVis() {
    const vs = SS.Sim.visitors, sc = R.vscale, t = SS.G.time; let n = 0;
    for (let i = 0; i < vs.length && n < MAXV; i++) {
      const v = vs[i], st = v.state;
      if (st === 'gone' || st === 'inS' || st === 'pending') continue;
      if (st === 'ride' && v.hidden) continue;
      let x = v.x, z = v.z, y = v.y, lie = false;
      if (st === 'lesson') { const a = t * 0.004 + v.id * 0.55, rr = 9 + (v.id % 5) * 2.2; x = v.bx + Math.cos(a) * rr; z = v.bz + Math.sin(a * 2) * rr * 0.45; y = T.getH(x, z); v.hx = -Math.sin(a); v.hz = Math.cos(a * 2); v.x = x; v.z = z; v.y = y; }
      else if (st === 'hurt' || st === 'down') lie = true;
      put(n, x, y, z, Math.atan2(v.hx, v.hz), sc * (v.child ? 0.72 : 1), lie);
      jackets.setColorAt(n, c3.setHex(v.color)); n++;
    }
    jackets.count = rests.count = n;
    jackets.instanceMatrix.needsUpdate = rests.instanceMatrix.needsUpdate = true; if (jackets.instanceColor) jackets.instanceColor.needsUpdate = true;
  }
  let groomGeo, patrolGeo, heliGeo;
  function pool(list, n, make) { while (list.length < n) { const m = make(); R.fleet.add(m); list.push(m); } list.forEach((m, i) => m.visible = i < n); }
  function updateFleet() {
    const S = SS.Sim, sc = Math.max(1.3, R.vscale * 0.7);
    groomGeo = groomGeo || Mo.groomer(); patrolGeo = patrolGeo || Mo.patrol(); heliGeo = heliGeo || Mo.heli();
    const gs = S.groomers.filter(g => g.state !== 'parked');
    pool(R.groomMeshes, gs.length, () => new THREE.Mesh(groomGeo, bodyMat));
    gs.forEach((g, i) => { const m = R.groomMeshes[i]; m.position.set(g.x, g.y, g.z); m.rotation.y = Math.atan2(g.hx, g.hz); m.scale.setScalar(sc); });
    pool(R.rescueMeshes, S.rescues.length, () => { const g = new THREE.Group(); g.add(new THREE.Mesh(patrolGeo, bodyMat), new THREE.Mesh(heliGeo.body, bodyMat), new THREE.Mesh(heliGeo.rotor, bodyMat)); return g; });
    S.rescues.forEach((r, i) => { const m = R.rescueMeshes[i], heli = r.kind === 'heli'; m.children[0].visible = !heli; m.children[1].visible = m.children[2].visible = heli; m.position.set(r.x, r.y || T.getH(r.x, r.z), r.z); m.scale.setScalar(heli ? sc * 1.3 : R.vscale * 1.2); if (heli) m.children[2].rotation.y += 0.9; });
  }

  // --- étiquettes HTML lisibles quel que soit le zoom ---
  const labelEls = new Map(), lp = {};
  function updateLabels() {
    const root = document.getElementById('labels'); if (!root) return;
    const show = R.labelsOn && Cam.dist < 6500 && !SS.Arcade.active, seen = new Set();
    if (show) {
      const add = (key, text, color, x, y, z) => {
        R.project(x, y, z, lp); if (!lp.vis || lp.x < -50 || lp.y < 40 || lp.x > innerWidth + 50 || lp.y > innerHeight - 60) return;
        let el = labelEls.get(key); if (!el) { el = document.createElement('div'); el.className = 'lbl'; root.appendChild(el); labelEls.set(key, el); }
        if (el._t !== text) { el.textContent = text; el._t = text; el.style.borderColor = color; }
        el.style.transform = 'translate(' + Math.round(lp.x) + 'px,' + Math.round(lp.y) + 'px) translate(-50%,-100%)'; seen.add(key);
      };
      add('hub', '❄ FRONT DE NEIGE', '#ffb238', SS.HUB.x, T.getH(SS.HUB.x, SS.HUB.z) + 26, SS.HUB.z);
      for (const l of SS.G.lifts) { const w = l.queue.length; add(l, l.name + (G_broken(l) ? ' ⚠' : w > 6 ? ' · ' + w + ' en file' : ''), '#f2c230', l.a.x, l.a.y + 16, l.a.z); }
      for (const p of SS.G.pistes) { SS.pisteAt(p, p.len * 0.45, tmp); add(p, p.name + (SS.pisteOpen(p) ? '' : ' ✕'), D.DIFF[p.diff].color, tmp.x, T.getH(tmp.x, tmp.z) + 8, tmp.z); }
    }
    for (const [k, el] of labelEls) if (!seen.has(k)) { el.remove(); labelEls.delete(k); }
  }
  function G_broken(l) { return SS.G.time < l.brokenUntil || l.windStop || !l.open; }

  R.frame = function () {
    if (!SS.G) return;
    if (Cam.follow) { const f = Cam.follow; if (f.state === 'gone' || SS.Sim.visitors.indexOf(f) < 0) Cam.follow = null; else { Cam.tx += (f.x - Cam.tx) * 0.15; Cam.tz += (f.z - Cam.tz) * 0.15; } }
    if (!SS.Arcade.active) Cam.apply();
    R.vscale = SS.Arcade.active ? 1 : SS.clamp(Cam.dist / 190, 1.3, 9);
    updateSky(); R.updateLamps(); updateLiftsVis(); updateVisitorsVis(); updateFleet(); updateLabels();
    const ds = SS.clamp(Cam.dist / 130, 2, 40); for (const o of R.preview.children) if (o.userData.dot) o.scale.setScalar(ds);
    renderer.render(scene, camera);
  };
})();
