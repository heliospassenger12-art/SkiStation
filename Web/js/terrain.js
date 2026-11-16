// SkiStation Web — massif 360°, 1600 → 4200 m, forêts, roches, glaciers
(function () {
  'use strict';
  const SS = window.SS, N = SS.N, CELL = SS.CELL, HALF = SS.HALF;
  const T = SS.Terrain = { POW: 2.4, TER: 0.4, H: new Float32Array(N * N), viewMode: 'normal', trees: [], treeGrid: new Map(), removedTrees: new Set() };
  let noise;

  // --- relief : massifs larges, vallées et crêtes, pentes plafonnées par érosion (talus), aiguilles rocheuses étroites au sommet ---
  // x, z normalisés ; amplitude ; largeur du dôme. La graine décale légèrement chaque massif.
  const DOMES = [
    [0.06, -0.22, 1.00, 0.60], [0.56, -0.36, 0.72, 0.44], [-0.56, -0.30, 0.76, 0.46], [0.02, -0.80, 0.66, 0.42],
    [0.74, 0.02, 0.42, 0.36], [-0.74, 0.06, 0.46, 0.38], [0.30, -0.52, 0.55, 0.30], [-0.30, -0.56, 0.58, 0.30], [0.66, -0.80, 0.5, 0.34], [-0.66, -0.82, 0.52, 0.36]
  ];
  const SPIKES = [[0.10, -0.24, 850, 0.085], [0.58, -0.38, 560, 0.075], [-0.55, -0.32, 640, 0.075], [0.0, -0.80, 520, 0.075]];
  const PLATEAUS = [                                  // replats étagés : alpages, cirques et glaciers (x, z, altitude, rayon), à relier par des pentes de ski
    [-0.42, 0.30, 1980, 0.17], [0.02, 0.26, 2030, 0.16], [0.44, 0.30, 1990, 0.17],
    [-0.58, 0.02, 2420, 0.15], [-0.18, -0.02, 2480, 0.15], [0.24, 0.0, 2450, 0.15], [0.62, 0.04, 2400, 0.15],
    [-0.46, -0.30, 2850, 0.15], [0.02, -0.32, 2900, 0.14], [0.44, -0.30, 2880, 0.15],
    [-0.40, -0.58, 3150, 0.15], [0.06, -0.60, 3200, 0.15], [0.52, -0.56, 3120, 0.15], [0.34, -0.40, 3250, 0.14], [-0.32, -0.46, 3050, 0.14], [0.0, -0.84, 2950, 0.15]
  ];
  T.TALUS = [0.40, 0.48, 0.58, 0.88, 1.1];        // pente maximale selon l'altitude (bandes de 2 000, 2 500, 3 000, 3 500, 4 000 m)
  T.ITER = 140; T.BODY = 3300;

  function domeField(seed) {
    const r = SS.rng(seed * 31 + 7), f = new Float32Array(N * N), dj = DOMES.map(d => [d[0] + (r() - 0.5) * 0.14, d[1] + (r() - 0.5) * 0.12, d[2] * (0.85 + r() * 0.3), d[3] * 1.18 * (0.9 + r() * 0.2)]);
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const nx = (ix / (N - 1)) * 2 - 1, nz = (iz / (N - 1)) * 2 - 1;
      const wx = nx + 0.16 * noise.fbm(nx * 1.7 + 11, nz * 1.7 + 3, 3), wz = nz + 0.16 * noise.fbm(nx * 1.7 - 7, nz * 1.7 + 19, 3);
      let e = 0; for (const d of dj) { const q = (wx - d[0]) * (wx - d[0]) + (wz - d[1]) * (wz - d[1]); e += d[2] * Math.exp(-q / (d[3] * d[3])); }
      const apron = SS.smooth(0.66, 0.02, nz);            // le massif ne démarre pas au village : la vallée d'accès monte progressivement
      e *= apron;
      const rid = noise.ridged(nx * 2.3 + 40, nz * 2.3 + 8, 4) - 0.5;                       // crêtes et combes (grande échelle)
      const rid2 = noise.ridged((nx + 0.15 * noise.fbm(nx * 3 + 5, nz * 3, 2)) * 4.8 + 13, nz * 4.8 + 57, 4) - 0.45;    // éperons, combes et replats (1 à 2 km)
      f[iz * N + ix] = e * 2250 + rid * 560 * (0.25 + Math.min(1, e)) * apron + rid2 * 470 * Math.min(1, 0.25 + e * 1.1) * apron + noise.fbm(nx * 8 + 3, nz * 8 + 77, 3) * 26 * (0.3 + Math.min(1, e)) * apron + noise.fbm(nx * 3.4 + 90, nz * 3.4 + 12, 3) * 140 * apron;
    }
    return f;
  }
  function erode(h) {                                  // érosion thermique : aucune pente ne dépasse le talus de son altitude
    const d = new Float32Array(N * N), nb = [[1, 0, 1], [-1, 0, 1], [0, 1, 1], [0, -1, 1], [1, 1, 1.414], [-1, 1, 1.414], [1, -1, 1.414], [-1, -1, 1.414]];
    const tal = a => { const t = (a - 1640) / 500; const i = SS.clamp(Math.floor(t), 0, 3), u = SS.clamp(t - i, 0, 1); return T.TALUS[i] + (T.TALUS[i + 1] - T.TALUS[i]) * u; };
    for (let it = 0; it < T.ITER; it++) {
      d.fill(0);
      for (let z = 1; z < N - 1; z++) for (let x = 1; x < N - 1; x++) {
        const i = z * N + x, hi = h[i], lim = tal(hi);
        for (let k = 0; k < 8; k++) {
          const j = i + nb[k][1] * N + nb[k][0], ex = hi - h[j] - lim * CELL * nb[k][2];
          if (ex > 0) { const m = ex * 0.2; d[i] -= m; d[j] += m; }
        }
      }
      for (let i = 0; i < h.length; i++) h[i] += d[i];
    }
  }
  function smoothOnce(h, k) {                          // adoucit les arêtes laissées par l'érosion
    const o = new Float32Array(h);
    for (let z = 1; z < N - 1; z++) for (let x = 1; x < N - 1; x++) { const i = z * N + x; o[i] = h[i] * (1 - k) + k * 0.25 * (h[i - 1] + h[i + 1] + h[i - N] + h[i + N]); }
    h.set(o);
  }

  T.init = function (seed) {
    seed = seed || 1337; noise = SS.makeNoise(seed);
    T.seed = seed; T.geo = null; T.snowMask = new Float32Array(N * N); T.removedTrees = new Set(); T.viewMode = 'normal'; T.summer = false;
    const H = T.H, V = SS.VILLAGE, f = domeField(seed);
    let mx = 0; for (let i = 0; i < f.length; i++) { f[i] = Math.max(0, f[i]); if (f[i] > mx) mx = f[i]; }
    const body = T.BODY;                                 // altitude visée du massif avant les aiguilles
    for (let i = 0; i < H.length; i++) H[i] = f[i] * (body - 1640) / mx;
    const pr = SS.rng(seed * 13 + 5);
    for (const pl of PLATEAUS) {                         // les plateaux sont creusés avant l'érosion : leurs accès deviennent des rampes douces
      const cx = (pl[0] + (pr() - 0.5) * 0.12 + 1) / 2 * (N - 1), cz = (pl[1] + (pr() - 0.5) * 0.12 + 1) / 2 * (N - 1), rad = pl[3] * (N - 1) / 2, tgt = pl[2] - 1640;
      for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) { const q = Math.hypot(ix - cx, iz - cz) / rad; if (q > 2.2) continue; const w = 1 - SS.smooth(0.55, 2.2, q); H[iz * N + ix] = SS.lerp(H[iz * N + ix], tgt + noise.fbm(ix * 0.06, iz * 0.06, 2) * 40, w); }
    }
    erode(H); smoothOnce(H, 0.3); smoothOnce(H, 0.2);
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const i = iz * N + ix, nx = (ix / (N - 1)) * 2 - 1, nz = (iz / (N - 1)) * 2 - 1;
      const zz = -HALF + iz * CELL;
      let h = 1640 + H[i] + SS.clamp((2760 - zz) * 0.07, 0, 130) * (1 - SS.smooth(1700, 2400, H[i] + 1640));      // vallée d'accès : monte doucement depuis le village
      for (const sp of SPIKES) { const q = (nx - sp[0]) * (nx - sp[0]) + (nz - sp[1]) * (nz - sp[1]); const rug = 0.55 + 0.9 * (noise.ridged(nx * 26 + sp[0] * 9, nz * 26, 3) - 0.35); h += sp[2] * Math.exp(-q / (sp[3] * sp[3])) * SS.clamp(rug, 0.25, 1.2); }
      h += noise.fbm(nx * 18 + 5, nz * 18 + 9, 3) * 6 * (0.4 + SS.smooth(1700, 2800, h));      // bosses et ondulations locales
      H[i] = Math.max(1640, h);
    }
    // l'altitude maximale est recalée à 4 200 m par les aiguilles
    let top = 0; for (let i = 0; i < H.length; i++) if (H[i] > top) top = H[i];
    for (let i = 0; i < H.length; i++) if (H[i] > 3350) H[i] = 3350 + (H[i] - 3350) * (4200 - 3350) / Math.max(1, top - 3350);      // point culminant : 4 200 m
    // replat du village et front de neige en pente douce
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const x = -HALF + ix * CELL, z = -HALF + iz * CELL, d = Math.hypot((x - V.x) * 0.8, z - 2760), w = 1 - SS.smooth(330, 900, d);
      if (w > 0) H[iz * N + ix] = SS.lerp(H[iz * N + ix], V.h + SS.clamp((V.z - z) * 0.07, -8, 60), w);
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
    if (T.summer) {                                                  // été : alpages verts, éboulis, neiges éternelles
      const snowS = SS.smooth(2950, 3250, h + n * 150) * (1 - SS.smooth(0.7, 1.0, g));
      const alp = SS.smooth(2100, 2800, h + n * 200);
      let sr = SS.lerp(0.30, 0.52, alp) + n * 0.05, sg = SS.lerp(0.50, 0.50, alp) + n * 0.06, sb = SS.lerp(0.20, 0.36, alp);
      const rockS = Math.max(SS.smooth(0.7, 1.1, g + n * 0.1), SS.smooth(3000, 3400, h) * 0.7), rcS = 0.40 + 0.10 * n;
      sr = SS.lerp(sr, rcS + 0.03, rockS); sg = SS.lerp(sg, rcS + 0.01, rockS); sb = SS.lerp(sb, rcS, rockS);
      col[0] = SS.lerp(sr, 0.93, snowS); col[1] = SS.lerp(sg, 0.95, snowS); col[2] = SS.lerp(sb, 0.99, snowS);
      return col;
    }
    let r = 0.93, gg = 0.95, b = 0.99;                               // neige
    const shade = SS.clamp(0.5 + (hz * 0.5 - hx * 0.25), 0, 1);       // versants nord plus bleutés
    r -= 0.07 * shade; gg -= 0.045 * shade;
    if (h > 3150) {                                                   // glacier
      const ice = SS.smooth(3150, 3500, h) * (1 - SS.smooth(0.35, 0.6, g)) * (0.55 + 0.45 * n);
      r = SS.lerp(r, 0.72, ice); gg = SS.lerp(gg, 0.88, ice); b = SS.lerp(b, 0.97, ice);
    }
    const rock = SS.smooth(0.8, 1.15, g + n * 0.1) * (1 - (T.snowMask ? T.snowMask[i] : 0));
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
    parts.push({ geo: c1, color: 0x1f4a36 }, { geo: c2, color: T.summer ? 0x2c6146 : 0xdfeaf2 });
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
