// SkiStation Web — massif 360°, 1600 → 4200 m, forêts, roches, glaciers
(function () {
  'use strict';
  const SS = window.SS, N = SS.N, CELL = SS.CELL, HALF = SS.HALF;
  const T = SS.Terrain = { POW: 2.4, TER: 0.4, H: new Float32Array(N * N), viewMode: 'normal', trees: [], treeGrid: new Map(), removedTrees: new Set() };
  let noise;

  const PEAKS = [ // x, z (normalisés), dôme, largeur dôme, aiguille, largeur aiguille
    [0.03, -0.20, 1500, 0.56, 1000, 0.17],
    [0.50, -0.46, 1150, 0.36, 650, 0.13],
    [-0.52, -0.40, 1250, 0.38, 700, 0.14],
    [0.55, 0.20, 850, 0.34, 330, 0.12],
    [-0.56, 0.24, 900, 0.34, 380, 0.12],
    [-0.04, -0.74, 950, 0.32, 420, 0.12],
    [0.0, 0.22, 420, 0.42, 0, 1]
  ];

  function rawHeight(x, z) {
    const nx = x / HALF, nz = z / HALF;
    const wx = nx + 0.09 * noise.fbm(nx * 1.7 + 11, nz * 1.7 + 3, 3);
    const wz = nz + 0.09 * noise.fbm(nx * 1.7 - 7, nz * 1.7 + 19, 3);
    let h = 0;
    for (const p of PEAKS) {
      const d2 = (wx - p[0]) * (wx - p[0]) + (wz - p[1]) * (wz - p[1]);
      h += p[2] * Math.exp(-d2 / (p[3] * p[3])) + p[4] * Math.exp(-d2 / (p[5] * p[5]));
    }
    const e = SS.clamp(h / 2400, 0, 1);
    h += (noise.ridged(nx * 2.6 + 40, nz * 2.6 + 8, 4) - 0.45) * 400 * (0.25 + e);
    h += noise.fbm(nx * 9 + 3, nz * 9 + 77, 3) * 70 * (0.4 + e);
    h += (noise.fbm(nx * 1.1 + 90, nz * 1.1 + 60, 2) + 0.2) * 160;        // vallées jamais plates
    h = Math.max(h, 0);
    return 1640 + h;
  }

  T.init = function (seed) {
    noise = SS.makeNoise(seed || 1337);
    T.seed = seed || 1337; T.geo = null; T.snowMask = new Float32Array(N * N); T.removedTrees = new Set(); T.viewMode = 'normal';
    const H = T.H, V = SS.VILLAGE;
    let max = 0;
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const h = rawHeight(-HALF + ix * CELL, -HALF + iz * CELL); H[iz * N + ix] = h; if (h > max) max = h;
    }
    // profil alpin : alpages doux en bas, sommets raides en haut, puis bancs et ressauts (replats skiables / murs)
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const i = iz * N + ix, nx = (-HALF + ix * CELL) / HALF, nz = (-HALF + iz * CELL) / HALF;
      const e = (H[i] - 1640) / (max - 1640);
      let h = 2570 * Math.pow(e, T.POW);
      const P = 300, ph = noise.fbm(nx * 1.4 + 200, nz * 1.4 + 300, 2) * 9;
      h += (P / 6.283) * T.TER * Math.sin(6.283 * h / P + ph) * SS.smooth(40, 300, h);
      H[i] = 1640 + h;
    }
    // plateau glaciaire skiable sous le sommet principal
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const x = -HALF + ix * CELL, z = -HALF + iz * CELL, d = Math.hypot(x - 1000, z + 1500);
      if (d > 1100) continue;
      const target = 3200 - ((x - 1000) * 0.78 - (z + 1500) * 0.62) * 0.17;
      const w = (1 - SS.smooth(450, 1100, d)) * (1 - SS.smooth(350, 800, Math.abs(H[iz * N + ix] - target)));
      H[iz * N + ix] = SS.lerp(H[iz * N + ix], target, w);
    }
    // replat du village et front de neige en pente douce
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const x = -HALF + ix * CELL, z = -HALF + iz * CELL;
      const d = Math.hypot((x - V.x) * 0.8, z - 2760);
      const w = 1 - SS.smooth(380, 760, d);
      if (w > 0) {
        const target = V.h + SS.clamp((V.z - z) * 0.07, -8, 60);
        H[iz * N + ix] = SS.lerp(H[iz * N + ix], target, w);
      }
    }
    T.minH = Infinity; T.maxH = -Infinity;
    for (let i = 0; i < H.length; i++) { if (H[i] < T.minH) T.minH = H[i]; if (H[i] > T.maxH) T.maxH = H[i]; }
  };

  // hauteur exacte sur le maillage (même découpe de triangles que la géométrie)
  T.getH = function (x, z) {
    let gx = (x + HALF) / CELL, gz = (z + HALF) / CELL;
    gx = SS.clamp(gx, 0, N - 1.001); gz = SS.clamp(gz, 0, N - 1.001);
    const ix = gx | 0, iz = gz | 0, fx = gx - ix, fz = gz - iz, H = T.H, i = iz * N + ix;
    const a = H[i], d = H[i + 1], b = H[i + N], c = H[i + N + 1];
    return fx + fz <= 1 ? a + (d - a) * fx + (b - a) * fz : c + (b - c) * (1 - fx) + (d - c) * (1 - fz);
  };
  T.grad = function (x, z, out) {            // gradient (dh/dx, dh/dz) lissé sur une maille
    const e = CELL * 0.6;
    out = out || {};
    out.x = (T.getH(x + e, z) - T.getH(x - e, z)) / (2 * e);
    out.z = (T.getH(x, z + e) - T.getH(x, z - e)) / (2 * e);
    return out;
  };
  const _g = {};
  T.slope = function (x, z) { T.grad(x, z, _g); return Math.hypot(_g.x, _g.z); };
  T.inside = (x, z, m) => Math.abs(x) < HALF - (m || 60) && Math.abs(z) < HALF - (m || 60);

  // --- couleurs de sommets ---
  const col = [0, 0, 0];
  function vertexColor(ix, iz) {
    const H = T.H, i = iz * N + ix, h = H[i];
    const hx = (H[iz * N + Math.min(N - 1, ix + 1)] - H[iz * N + Math.max(0, ix - 1)]) / (2 * CELL);
    const hz = (H[Math.min(N - 1, iz + 1) * N + ix] - H[Math.max(0, iz - 1) * N + ix]) / (2 * CELL);
    const g = Math.hypot(hx, hz);
    if (T.viewMode === 'slope') {
      if (g < 0.04) { col[0] = .82; col[1] = .84; col[2] = .86; }
      else if (g < 0.14) { col[0] = .30; col[1] = .78; col[2] = .38; }
      else if (g < 0.26) { col[0] = .22; col[1] = .50; col[2] = .95; }
      else if (g < 0.42) { col[0] = .92; col[1] = .25; col[2] = .22; }
      else if (g < 0.72) { col[0] = .16; col[1] = .16; col[2] = .19; }
      else { col[0] = .45; col[1] = .28; col[2] = .50; }
      return col;
    }
    const x = -HALF + ix * CELL, z = -HALF + iz * CELL;
    const n = noise.fbm(x * 0.004, z * 0.004, 3);
    let r = 0.93, gg = 0.95, b = 0.99;                               // neige
    const shade = SS.clamp(0.5 + (hz * 0.5 - hx * 0.25), 0, 1);       // versants nord plus bleutés
    r -= 0.07 * shade; gg -= 0.045 * shade;
    if (h > 3150) {                                                   // glacier
      const ice = SS.smooth(3150, 3500, h) * (1 - SS.smooth(0.35, 0.6, g)) * (0.55 + 0.45 * n);
      r = SS.lerp(r, 0.72, ice); gg = SS.lerp(gg, 0.88, ice); b = SS.lerp(b, 0.97, ice);
    }
    const rock = SS.smooth(0.62, 0.95, g + n * 0.12) * (1 - (T.snowMask ? T.snowMask[i] : 0));
    const rc = 0.36 + 0.10 * n;
    r = SS.lerp(r, rc + 0.04, rock); gg = SS.lerp(gg, rc + 0.01, rock); b = SS.lerp(b, rc, rock);
    if (h < 1800) { const v = SS.smooth(1800, 1650, h) * 0.05; r -= v; gg -= v * 0.6; }
    col[0] = r; col[1] = gg; col[2] = b;
    return col;
  }

  T.buildMesh = function (overlayTexture) {
    const geo = new THREE.BufferGeometry();
    const pos = new Float32Array(N * N * 3), uv = new Float32Array(N * N * 2), colr = new Float32Array(N * N * 3), nor = new Float32Array(N * N * 3);
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const i = iz * N + ix;
      pos[i * 3] = -HALF + ix * CELL; pos[i * 3 + 2] = -HALF + iz * CELL;
      uv[i * 2] = ix / (N - 1); uv[i * 2 + 1] = iz / (N - 1);
    }
    const idx = new Uint32Array((N - 1) * (N - 1) * 6); let k = 0;
    for (let iz = 0; iz < N - 1; iz++) for (let ix = 0; ix < N - 1; ix++) {
      const a = iz * N + ix, d = a + 1, b = a + N, c = b + 1;
      idx[k++] = a; idx[k++] = b; idx[k++] = d; idx[k++] = b; idx[k++] = c; idx[k++] = d;
    }
    geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    geo.setAttribute('normal', new THREE.BufferAttribute(nor, 3));
    geo.setAttribute('color', new THREE.BufferAttribute(colr, 3));
    geo.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
    geo.setIndex(new THREE.BufferAttribute(idx, 1));
    T.geo = geo;
    T.updateRegion(0, 0, N - 1, N - 1);
    geo.computeBoundingSphere();
    const mat = new THREE.MeshLambertMaterial({ vertexColors: true, map: overlayTexture });
    T.mesh = new THREE.Mesh(geo, mat);
    T.mesh.frustumCulled = false;
    return T.mesh;
  };

  T.updateRegion = function (x0, z0, x1, z1) {
    x0 = Math.max(0, x0); z0 = Math.max(0, z0); x1 = Math.min(N - 1, x1); z1 = Math.min(N - 1, z1);
    const H = T.H, pos = T.geo.attributes.position.array, nor = T.geo.attributes.normal.array, colr = T.geo.attributes.color.array;
    for (let iz = z0; iz <= z1; iz++) for (let ix = x0; ix <= x1; ix++) {
      const i = iz * N + ix;
      pos[i * 3 + 1] = H[i];
      const hx = (H[iz * N + Math.min(N - 1, ix + 1)] - H[iz * N + Math.max(0, ix - 1)]) / (2 * CELL);
      const hz = (H[Math.min(N - 1, iz + 1) * N + ix] - H[Math.max(0, iz - 1) * N + ix]) / (2 * CELL);
      const l = 1 / Math.hypot(hx, 1, hz);
      nor[i * 3] = -hx * l; nor[i * 3 + 1] = l; nor[i * 3 + 2] = -hz * l;
      const c = vertexColor(ix, iz);
      colr[i * 3] = c[0]; colr[i * 3 + 1] = c[1]; colr[i * 3 + 2] = c[2];
    }
    T.geo.attributes.position.needsUpdate = true; T.geo.attributes.normal.needsUpdate = true; T.geo.attributes.color.needsUpdate = true;
  };
  T.setViewMode = function (m) { T.viewMode = m; T.updateRegion(0, 0, N - 1, N - 1); };

  // Terrassement local : replat sous l'emprise, raccord naturel autour (rejouable pour les sauvegardes)
  T.flatten = function (x, z, r, h, blend) {
    blend = blend || r * 0.9;
    const R = r + blend, H = T.H;
    const x0 = Math.floor((x - R + HALF) / CELL), x1 = Math.ceil((x + R + HALF) / CELL);
    const z0 = Math.floor((z - R + HALF) / CELL), z1 = Math.ceil((z + R + HALF) / CELL);
    for (let iz = Math.max(0, z0); iz <= Math.min(N - 1, z1); iz++) for (let ix = Math.max(0, x0); ix <= Math.min(N - 1, x1); ix++) {
      const d = Math.hypot(-HALF + ix * CELL - x, -HALF + iz * CELL - z);
      const w = 1 - SS.smooth(r, R, d);
      if (w > 0) H[iz * N + ix] = SS.lerp(H[iz * N + ix], h, w);
    }
    if (T.geo) T.updateRegion(x0 - 1, z0 - 1, x1 + 1, z1 + 1);
  };
  // marque une bande enneigée (les pistes recouvrent la roche)
  T.snowAlong = function (pts, halfW) {
    for (const p of pts) {
      const x0 = Math.floor((p.x - halfW + HALF) / CELL), x1 = Math.ceil((p.x + halfW + HALF) / CELL);
      const z0 = Math.floor((p.z - halfW + HALF) / CELL), z1 = Math.ceil((p.z + halfW + HALF) / CELL);
      for (let iz = Math.max(0, z0); iz <= Math.min(N - 1, z1); iz++) for (let ix = Math.max(0, x0); ix <= Math.min(N - 1, x1); ix++) T.snowMask[iz * N + ix] = 1;
    }
  };

  // --- forêts ---
  T.genTrees = function () {
    const r = SS.rng(T.seed + 99), step = 21, trees = T.trees = [];
    T.treeGrid.clear();
    for (let z = -HALF + 30; z < HALF - 30; z += step) for (let x = -HALF + 30; x < HALF - 30; x += step) {
      const px = x + (r() - 0.5) * step, pz = z + (r() - 0.5) * step, rr = r(), rs = r();
      const h = T.getH(px, pz);
      const line = 2260 + noise.fbm(px * 0.0012, pz * 0.0012, 2) * 260;
      if (h > line) continue;
      const dens = noise.fbm(px * 0.0021 + 50, pz * 0.0021 + 9, 3) * 0.5 + 0.5;
      const fade = SS.smooth(line, line - 260, h);
      if (rr > (dens - 0.34) * 1.9 * fade) continue;
      if (T.slope(px, pz) > 0.8) continue;
      if (Math.hypot(px - SS.VILLAGE.x, pz - 2720) < 560) continue;
      if (Math.abs(px - 40) < 45 && pz > 2700) continue;             // route d'accès
      trees.push({ x: px, z: pz, y: h, s: 0.75 + rs * 0.7, alive: true });
    }
    trees.forEach((t, i) => {
      const key = ((t.x + HALF) / 100 | 0) + ((t.z + HALF) / 100 | 0) * 1000;
      let a = T.treeGrid.get(key); if (!a) T.treeGrid.set(key, a = []); a.push(i);
    });
  };
  T.buildTrees = function () {
    const parts = [];
    const c1 = new THREE.ConeGeometry(4.4, 12, 5, 1, true); c1.translate(0, 7, 0);
    const c2 = new THREE.ConeGeometry(3.0, 8, 5, 1, true); c2.translate(0, 12.5, 0);
    parts.push({ geo: c1, color: 0x1f4a36 }, { geo: c2, color: 0xdfeaf2 });
    const geo = SS.mergeParts(parts);
    const mesh = new THREE.InstancedMesh(geo, new THREE.MeshLambertMaterial({ vertexColors: true }), T.trees.length);
    const m = new THREE.Matrix4();
    T.trees.forEach((t, i) => { m.makeScale(t.s, t.s * (0.9 + (i % 7) * 0.05), t.s); m.setPosition(t.x, t.y - 0.5, t.z); mesh.setMatrixAt(i, m); });
    mesh.instanceMatrix.needsUpdate = true; mesh.frustumCulled = false;
    T.treeMesh = mesh;
    for (const i of T.removedTrees) hideTree(i);
    return mesh;
  };
  const zero = new THREE.Matrix4().makeScale(0, 0, 0);
  function hideTree(i) { T.trees[i].alive = false; if (T.treeMesh) { T.treeMesh.setMatrixAt(i, zero); T.treeMesh.instanceMatrix.needsUpdate = true; } }
  // test(x,z) -> bool ; bbox optionnelle {x0,z0,x1,z1}. remove=false : simple comptage (devis)
  T.treesIn = function (test, bb, remove) {
    let n = 0;
    const gx0 = (bb.x0 + HALF) / 100 | 0, gx1 = (bb.x1 + HALF) / 100 | 0, gz0 = (bb.z0 + HALF) / 100 | 0, gz1 = (bb.z1 + HALF) / 100 | 0;
    for (let gz = gz0; gz <= gz1; gz++) for (let gx = gx0; gx <= gx1; gx++) {
      const a = T.treeGrid.get(gx + gz * 1000); if (!a) continue;
      for (const i of a) { const t = T.trees[i]; if (t.alive && test(t.x, t.z)) { n++; if (remove) { hideTree(i); T.removedTrees.add(i); } } }
    }
    return n;
  };
  T.treesNearPath = function (pts, halfW, remove) {
    let x0 = 1e9, z0 = 1e9, x1 = -1e9, z1 = -1e9;
    for (const p of pts) { x0 = Math.min(x0, p.x); x1 = Math.max(x1, p.x); z0 = Math.min(z0, p.z); z1 = Math.max(z1, p.z); }
    return T.treesIn((x, z) => { for (let i = 0; i < pts.length - 1; i++) if (SS.segDist(x, z, pts[i].x, pts[i].z, pts[i + 1].x, pts[i + 1].z).d < halfW) return true; return pts.length === 1 && Math.hypot(x - pts[0].x, z - pts[0].z) < halfW; },
      { x0: x0 - halfW, z0: z0 - halfW, x1: x1 + halfW, z1: z1 + halfW }, remove);
  };

  // intersection rayon / relief par marche puis dichotomie
  T.raycast = function (o, d, maxDist) {
    let t = 0, step = 20, prev = 0;
    maxDist = maxDist || 30000;
    while (t < maxDist) {
      const x = o.x + d.x * t, y = o.y + d.y * t, z = o.z + d.z * t;
      if (Math.abs(x) < HALF && Math.abs(z) < HALF && y < T.getH(x, z)) {
        let a = prev, b = t;
        for (let i = 0; i < 14; i++) { const m = (a + b) / 2; const mx = o.x + d.x * m, mz = o.z + d.z * m; if (o.y + d.y * m < T.getH(mx, mz)) b = m; else a = m; }
        const px = o.x + d.x * b, pz = o.z + d.z * b;
        return { x: px, z: pz, y: T.getH(px, pz) };
      }
      if (y < 1200 && d.y <= 0) return null;
      prev = t; t += step; step = Math.min(60, step * 1.04);
    }
    return null;
  };

  // fusionne des géométries colorées en une seule (couleurs par sommet)
  SS.mergeParts = function (parts) {
    let count = 0; const list = [];
    for (const p of parts) {
      let g = p.geo.index ? p.geo.toNonIndexed() : p.geo;
      if (p.matrix) { g = g.clone(); g.applyMatrix4(p.matrix); }
      if (!g.attributes.normal) g.computeVertexNormals();
      list.push({ g, c: new THREE.Color(p.color) }); count += g.attributes.position.count;
    }
    const pos = new Float32Array(count * 3), nor = new Float32Array(count * 3), colr = new Float32Array(count * 3); let o = 0;
    for (const { g, c } of list) {
      pos.set(g.attributes.position.array, o * 3); nor.set(g.attributes.normal.array, o * 3);
      for (let i = 0; i < g.attributes.position.count; i++) { colr[(o + i) * 3] = c.r; colr[(o + i) * 3 + 1] = c.g; colr[(o + i) * 3 + 2] = c.b; }
      o += g.attributes.position.count;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    geo.setAttribute('normal', new THREE.BufferAttribute(nor, 3));
    geo.setAttribute('color', new THREE.BufferAttribute(colr, 3));
    return geo;
  };
})();
