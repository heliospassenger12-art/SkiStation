// SkiStation Web — massif 360°, 1600 → 4200 m, forêts, roches, glaciers
(function () {
  'use strict';
  const SS = window.SS;
  let N = SS.N, CELL = SS.CELL, HALF = SS.HALF;           // relus à chaque T.init (carte classique ou grande carte)
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
      const calm = SS.smooth(0.62, 0.28, nz);              // près du village le terrain reste doux : pistes vertes et bleues pour les débutants
      f[iz * N + ix] = e * 2250 + (rid * 560 * (0.25 + Math.min(1, e)) + rid2 * 470 * Math.min(1, 0.25 + e * 1.1) + noise.fbm(nx * 8 + 3, nz * 8 + 77, 3) * 26 * (0.3 + Math.min(1, e)) + noise.fbm(nx * 3.4 + 90, nz * 3.4 + 12, 3) * 140) * apron * calm;
    }
    return f;
  }
  function erode(h, cap) {                             // érosion thermique : aucune pente ne dépasse le talus de son altitude (cap : les sommets au-dessus restent intacts)
    const d = new Float32Array(N * N), nb = [[1, 0, 1], [-1, 0, 1], [0, 1, 1], [0, -1, 1], [1, 1, 1.414], [-1, 1, 1.414], [1, -1, 1.414], [-1, -1, 1.414]];
    const tal = a => { const t = (a - 1640) / 500; const i = SS.clamp(Math.floor(t), 0, 3), u = SS.clamp(t - i, 0, 1); return T.TALUS[i] + (T.TALUS[i + 1] - T.TALUS[i]) * u; };
    for (let it = 0; it < T.ITER; it++) {
      d.fill(0);
      for (let z = 1; z < N - 1; z++) for (let x = 1; x < N - 1; x++) {
        const i = z * N + x, hi = h[i], lim = tal(hi);
        if (cap && hi > cap) continue;
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

  // massif « classique » (8 × 8 km, village au sud) : carrière, scénarios, bac à sable
  const CV = { x: 0, z: 2750, h: 1650 };
  function classic(seed, H) {
    noise = SS.makeNoise(seed);
    const V = CV, f = domeField(seed);
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
  }


  // ================= grande carte (Alpine Empires) : quatre montagnes différentes reliées par un plateau de cols =================
  // Chaque montagne est bâtie autour d'un sommet principal d'où rayonnent plusieurs arêtes (4 à 8). Entre deux arêtes se creuse une
  // combe ; chaque arête offre deux versants d'orientation différente, rythmés par un sous-sommet et un col. Puis des replats sont posés
  // à différentes altitudes (épaulements sur les arêtes, fonds de cirque, alpages à flanc) : petits, légèrement inclinés (≈ 6 %) pour
  // garder de la vitesse, entourés de pentes plus raides, et de chacun on repart vers plusieurs versants. Ils pourront accueillir gares,
  // restaurants d'altitude, bars ou activités selon leur taille (liste dans T.plateaus).
  // style : nombre et hauteur des reliefs, raideur (γ : > 1 = sommet raide et pied doux), part de sommets plats, replats de flanc
  T.BIG_STYLES = {
    player: { name: 'Massif aux mille versants', top: 4200, body: 2350, subs: [2, 3], sides: [2, 3], backs: [1, 2], fronts: [1, 2], g: [1.0, 1.3], flat: 0.45, scale: 1, plateaus: 9, pr: [240, 460], rough: 1, spikes: 3, spikeH: 480 },
    vast: { name: 'Grand domaine aux larges alpages', top: 3550, body: 2400, subs: [2, 3], sides: [3, 3], backs: [1, 2], fronts: [2, 2], g: [0.85, 1.1], flat: 0.75, scale: 1.12, plateaus: 10, pr: [300, 560], rough: 0.6, spikes: 1, spikeH: 240 },
    alpine: { name: 'Haute montagne et glaciers', top: 4400, body: 2450, subs: [3, 4], sides: [1, 2], backs: [1, 2], fronts: [1, 1], g: [1.2, 1.6], flat: 0.3, scale: 0.95, plateaus: 7, pr: [220, 420], rough: 1.3, spikes: 5, spikeH: 650 },
    hills: { name: 'Moyenne montagne boisée', top: 3350, body: 2250, subs: [3, 4], sides: [3, 4], backs: [2, 2], fronts: [2, 2], g: [0.9, 1.2], flat: 0.5, scale: 0.9, plateaus: 10, pr: [220, 400], rough: 0.8, spikes: 2, spikeH: 180 }
  };
  // reliefs sans village, entre les stations : de quoi relier les domaines et bâtir de grandes stations
  T.BIG_STYLES.central = { name: 'Massif central', top: 3650, body: 2600, g: [0.85, 1.1], flat: 0.5, scale: 1, plateaus: 12, pr: [260, 520], rough: 1, spikes: 3, spikeH: 420 };
  T.BIG_STYLES.col = { name: 'Montagne de liaison', top: 3050, body: 2300, g: [0.8, 1.05], flat: 0.7, scale: 1, plateaus: 5, pr: [240, 440], rough: 0.8, spikes: 1, spikeH: 200 };
  T.BIG_STYLES.butte = { name: 'Col vallonné', top: 2700, body: 2300, g: [0.9, 1.2], flat: 0.3, scale: 1, plateaus: 1, pr: [200, 320], rough: 0.7, spikes: 0, spikeH: 0 };
  const FLOOR = 1600;
  // cols entre le massif central et les montagnes de liaison : jamais plats, et chacun différent
  function makeButte(m, st, r) {
    const R = (a, b) => a + (b - a) * r(), b0 = m.b0;
    m.st = st; m.styleName = { bosses: 'Col des bosses', crete: 'Col de la petite crête', mamelons: 'Col des mamelons', combe: 'Col de la combe' }[m.kind]; m.peaks = []; m.p0 = { x: 0, z: 0 };
    const add = (x, z, dh, rad, g, rt, ang, asp) => m.peaks.push({ x, z, h: b0 + dh, fb: b0 - 120, R: rad, g, rt: rt || 0, c: Math.cos(ang), s: Math.sin(ang), asp, tilt: 0.06 });
    if (m.kind === 'bosses') for (let i = 0, n = 7 + Math.floor(r() * 4); i < n; i++) { const a = r() * 6.28, d = R(0, 1200); add(Math.sin(a) * d, Math.cos(a) * d, R(90, 210), R(260, 460), R(0.9, 1.2), 0, r() * 3, R(1, 1.5)); }
    else if (m.kind === 'crete') { const a = r() * 3.14; add(0, 0, R(300, 400), R(850, 1050), 1.1, 0, a, R(2.6, 3.2)); for (const sg of [-1, 1]) add(Math.sin(a) * 900 * sg + R(-200, 200), Math.cos(a) * 900 * sg, R(160, 260), R(400, 600), 1.0, 0, a + 1.4, 1.3); }
    else if (m.kind === 'mamelons') for (let i = 0, n = 2 + Math.floor(r() * 2); i < n; i++) { const a = i * 6.28 / n + r(), d = R(500, 900); add(Math.sin(a) * d, Math.cos(a) * d, R(260, 420), R(700, 1000), R(1.0, 1.3), i === 0 ? R(180, 260) : 0, r() * 3, R(1, 1.4)); }
    else for (let i = 0; i < 5; i++) { const a = i * 1.257 + R(-0.25, 0.25), d = R(550, 750); add(Math.sin(a) * d, Math.cos(a) * d, R(150, 300), R(450, 620), R(1.0, 1.3), 0, a, R(1.3, 1.8)); }
  }
  function makeLink(m, st, r) {                     // massif central (arêtes vers les quatre stations) ou montagne de liaison (à cheval sur une frontière)
    const R = (a, b) => a + (b - a) * r(), top = st.top + 80, fr = m.fr;
    m.st = st; m.styleName = st.name; m.peaks = [];
    const add = (x, z, h, rad, g, flat, ang, asp, fb) => m.peaks.push({ x, z, h, fb: fb || FLOOR, R: rad, g, rt: flat ? R(280, 480) : 0, c: Math.cos(ang), s: Math.sin(ang), asp, tilt: R(0.05, 0.08) });
    m.p0 = { x: R(-300, 300), z: R(-300, 300) };
    add(0, 0, st.body, fr * 0.95, 0.85, false, m.axis || 0, m.axis !== undefined ? 1.5 : 1.1);
    if (m.axis === undefined) {
      add(m.p0.x, m.p0.z, top, R(3000, 3500), R(st.g[0], st.g[1]), false, 0, 1, st.body - 200);
      for (let i = 0; i < 4; i++) { const a = Math.PI / 4 + i * Math.PI / 2 + R(-0.25, 0.25), d = R(1700, 2500); add(Math.sin(a) * d, Math.cos(a) * d, top * R(0.76, 0.86), R(2000, 2600), R(st.g[0], st.g[1]), r() < 0.3, a, R(1.4, 1.9), st.body - 250); }
      for (let i = 0; i < 3; i++) { const a = r() * 6.28, d = R(1200, 2600); add(Math.sin(a) * d, Math.cos(a) * d, R(2700, 3050), R(1500, 2000), R(st.g[0], st.g[1]), r() < st.flat, R(-1, 1), R(1.1, 1.5), st.body - 300); }
    } else {
      const ux = Math.sin(m.axis), uz = Math.cos(m.axis);
      add(m.p0.x, m.p0.z, top, R(2200, 2700), R(st.g[0], st.g[1]), r() < 0.3, m.axis, 1.6, st.body - 200);
      for (const sg of [-1, 1]) { const d = R(1300, 1900); add(ux * d * sg + R(-300, 300), uz * d * sg + R(-300, 300), R(2650, 2950), R(1500, 1900), R(st.g[0], st.g[1]), r() < st.flat, m.axis, R(1.1, 1.5), st.body - 250); }
    }
  }
  // Une montagne = plusieurs reliefs : le sommet principal et ses sous-sommets allongés (des arêtes, chacune avec ses versants),
  // des montagnes moyennes devant (on monte jusque vers 2 500 m, sommet plat = plateau, puis ça redescend vers un col),
  // sur les côtés et à l'arrière. Entre eux se creusent cols et combes.
  function makeMassif(m, st, r) {
    const R = (a, b) => a + (b - a) * r(), I = (a) => Math.round(R(a[0], a[1] + 0.99) - 0.49), VZ = SS.BIG_VZ, top = st.top + 80, sc = st.scale;
    m.st = st; m.styleName = st.name; m.peaks = [];
    const add = (x, z, h, rad, g, flat, ang, asp, fb) => m.peaks.push({ x, z, h, fb: fb || FLOOR, R: rad * sc, g, rt: flat ? R(280, 520) * sc : 0, c: Math.cos(ang), s: Math.sin(ang), asp, tilt: R(0.05, 0.08) });
    const main = { x: R(-700, 700), z: R(-2300, -1500) };
    m.p0 = main;
    add(R(-400, 400), R(-1300, -700), st.body, 4300, 0.85, false, R(-0.4, 0.4), 1.25);    // socle de la montagne : les cols entre reliefs restent en altitude
    add(main.x, main.z, top, R(3600, 4300), R(st.g[0], st.g[1]), false, 0, 1, st.body - 200);
    for (let i = 0, n = I(st.subs), a0 = r() * 6.28; i < n; i++) {               // sous-sommets : arêtes rayonnantes autour du sommet
      const a = a0 + (i + R(-0.25, 0.25)) * 6.28 / n, d = R(1400, 2400);
      add(main.x + Math.sin(a) * d, Math.min(VZ - 2400, main.z + Math.cos(a) * d), top * R(0.72, 0.86), R(2400, 3100), R(st.g[0], st.g[1]), r() < st.flat * 0.5, a, R(1.4, 1.9), st.body - 250);
    }
    for (let i = 0, n = I(st.fronts); i < n; i++) {                                  // montagnes de devant : montée vers un plateau autour de 2 500 m
      const side = n === 1 ? (r() < 0.5 ? -1 : 1) : (i ? 1 : -1);
      add(side * R(700, 2300), R(200, 1100), R(2400, 2750), R(2300, 2900), R(0.95, 1.25), r() < 0.85, R(-0.6, 0.6), R(1.0, 1.4), FLOOR + 120);
    }
    for (let i = 0, n = I(st.sides); i < n; i++) {                                   // montagnes latérales
      const side = i % 2 ? 1 : -1;
      add(side * R(2500, 3500), R(-2800, 600), R(2600, 3150) * (st.top > 3800 ? 1.04 : 1), R(2300, 3000), R(st.g[0], st.g[1]), r() < st.flat, R(-1, 1), R(1.1, 1.6), FLOOR + 200);
    }
    for (let i = 0, n = I(st.backs); i < n; i++)                                     // arrière : vers le plateau de cols commun
      add(R(-2500, 2500), R(-3700, -3100), R(2700, 3250), R(2100, 2700), R(st.g[0], st.g[1]), r() < st.flat, R(-1, 1), R(1.2, 1.7), FLOOR + 250);
  }
  function peakAt(p, x, z) {
    const dx = x - p.x, dz = z - p.z, a = dx * p.c - dz * p.s, b = dx * p.s + dz * p.c;   // b : le long du grand axe
    const d = Math.hypot(a, b / p.asp), u = Math.max(0, d - p.rt) / Math.max(1, p.R - p.rt);
    if (u >= 1) return FLOOR + (p.fb - FLOOR) * (1 - SS.smooth(1, 1.35, u));       // jupe : le pied du relief rejoint la vallée
    return p.fb + (p.h - p.fb) * Math.pow(1 - u, p.g) - (p.rt ? p.tilt * Math.min(d, p.rt) * (dz >= 0 ? 1 : -0.4) * (1 - u) : 0);
  }
  function massifH(m, k, x, z) {                     // relief brut d'une montagne (repère local)
    const wx = x + 520 * noise.fbm(x * 0.0003 + k * 13, z * 0.0003 - 4, 3) + 120 * noise.fbm(x * 0.0012, z * 0.0012 + k, 2), wz = z + 520 * noise.fbm(x * 0.0003 - 9, z * 0.0003 + k * 7, 3) + 120 * noise.fbm(x * 0.0012 + 5, z * 0.0012, 2);
    let h = FLOOR, sum = 0;
    for (const p of m.peaks) { const v = peakAt(p, wx, wz) - FLOOR; if (v > 0) { h = Math.max(h, FLOOR + v); sum += Math.exp(v / 70); } }
    if (sum > 0) h = FLOOR + 70 * Math.log(sum);                                     // raccords doux entre reliefs : cols arrondis
    // enveloppe : la montagne retombe vers le socle avant la limite de son quart ; embouchure ouverte devant le village
    const VZ = SS.BIG_VZ, FR = SS.QUAD - 150;
    const env = m.fr ? 1 - SS.smooth(0.45, 1.0, (m.axis === undefined ? Math.hypot(x, z) : Math.hypot(x * Math.cos(m.axis) - z * Math.sin(m.axis), (x * Math.sin(m.axis) + z * Math.cos(m.axis)) / 1.45)) / m.fr + 0.12 * noise.fbm(x * 0.0005 + k, z * 0.0005, 2)) : (1 - SS.smooth(0.66, 1.0, Math.hypot(x / FR, Math.min(0, z) / FR) + 0.06 * noise.fbm(x * 0.0005 + k, z * 0.0005, 2)))
      * SS.smooth(450, 3300, Math.hypot(x * 0.75, z - VZ) + 150 * noise.fbm(x * 0.001, z * 0.001 + k, 2));      // cuvette ouverte autour du village
    h = FLOOR + (h - FLOOR) * env;
    const hi = SS.smooth(1800, 2700, h);
    h += m.st.rough * ((noise.ridged(wx * 0.0011 + k * 5, wz * 0.0011, 3) - 0.5) * 120 * hi + (noise.ridged(wx * 0.0026 - k, wz * 0.0026 + 3, 3) - 0.5) * 24 * hi + noise.fbm(wx * 0.004, wz * 0.004, 2) * 22);
    return h;
  }

  function bigWorld(seed) {
    const Q = SS.QUAD, VZ = SS.BIG_VZ, FR = Q - 150, vr = SS.rng(seed * 17 + 3);
    noise = SS.makeNoise(seed * 3 + 1);
    const M = T.MASSIFS = [{ cx: -Q, cz: Q, dir: 1 }, { cx: Q, cz: Q, dir: 1 }, { cx: Q, cz: -Q, dir: -1 }, { cx: -Q, cz: -Q, dir: -1 }];
    const styles = ['player', 'vast', 'alpine', 'hills'];              // vous ; Valfroide ; Pic-Royal ; Les Rochettes
    M.forEach((m, k) => { m.th = k ? (vr() - 0.5) * 0.3 : 0; m.c = Math.cos(m.th); m.s = Math.sin(m.th); m.style = styles[k]; makeMassif(m, T.BIG_STYLES[styles[k]], SS.rng(seed * 31 + k * 977 + 5)); });
    // entre les stations : un massif central et quatre montagnes de liaison à cheval sur les frontières
    const AX = Q * 1.25, links = [{ cx: 0, cz: 0, fr: 4300 }, { cx: 0, cz: AX, fr: 3000, axis: Math.PI / 2, name: 'sud' }, { cx: AX, cz: 0, fr: 3000, axis: 0, name: 'est' }, { cx: 0, cz: -AX, fr: 3000, axis: Math.PI / 2, name: 'nord' }, { cx: -AX, cz: 0, fr: 3000, axis: 0, name: 'ouest' }];
    links.forEach((m, j) => { m.dir = 1; m.th = 0; m.c = 1; m.s = 0; m.link = true; m.style = m.axis === undefined ? 'central' : 'col'; makeLink(m, T.BIG_STYLES[m.style], SS.rng(seed * 53 + j * 389 + 9)); if (m.name) m.styleName += ' ' + m.name; M.push(m); });
    const br = SS.rng(seed * 61 + 13), kinds = ['bosses', 'crete', 'mamelons', 'combe'].sort(() => br() - 0.5);
    [[0, 1], [1, 0], [0, -1], [-1, 0]].forEach((d, j) => {
      const rr = Q * 0.74 + (br() - 0.5) * 500, cx = d[0] * rr + d[1] * (br() - 0.5) * 600, cz = d[1] * rr + d[0] * (br() - 0.5) * 600;
      const m = { cx, cz, fr: 1900, dir: 1, th: 0, c: 1, s: 0, link: true, style: 'butte', kind: kinds[j], b0: 1600 + 650 * SS.smooth(HALF * 0.95, HALF * 0.22, Math.hypot(cx, cz)) };
      makeButte(m, T.BIG_STYLES.butte, SS.rng(seed * 71 + j * 211 + 3)); M.push(m);
    });
    const toLocal = (m, x, z, o) => { const px = (x - m.cx) * m.dir, pz = (z - m.cz) * m.dir; o.x = px * m.c - pz * m.s; o.z = px * m.s + pz * m.c; return o; };
    const toWorld = (m, lx, lz) => { const px = lx * m.c + lz * m.s, pz = -lx * m.s + lz * m.c; return { x: m.cx + px * m.dir, z: m.cz + pz * m.dir }; };
    const lift = (d, k) => d >= k ? d : d <= -k ? 0 : (d + k) * (d + k) / (4 * k);
    const H = T.H, own = new Int8Array(N * N).fill(-1), lo = {};
    // 1. socle continu (plateau de cols au centre, pente douce vers les bords) + montagnes posées dessus
    for (let iz = 0; iz < N; iz++) for (let ix = 0; ix < N; ix++) {
      const x = -HALF + ix * CELL, z = -HALF + iz * CELL, r = Math.hypot(x, z), i = iz * N + ix;
      const base = 1600 + 650 * SS.smooth(HALF * 0.95, HALF * 0.22, r) + noise.fbm(x * 0.0004 + 3, z * 0.0004 - 8, 3) * 160 * SS.smooth(HALF, HALF * 0.45, r) + noise.fbm(x * 0.0012, z * 0.0012, 2) * 25 + (noise.ridged(x * 0.0016 + 21, z * 0.0016 - 5, 3) - 0.45) * 70 * SS.smooth(HALF * 0.75, HALF * 0.3, r);      // ondulations : le centre n'est jamais plat
      let up = 0;
      for (let k = 0; k < M.length; k++) {
        const m = M[k]; toLocal(m, x, z, lo); const e = Math.max(Math.abs(lo.x), Math.abs(lo.z)) / (m.fr || FR); if (e >= 1) continue;
        const u = (1 - SS.smooth(0.88, 1, e)) * lift(massifH(m, k, lo.x, lo.z) - base, 60); if (u > up) { up = u; own[i] = k; }
      }
      H[i] = base + up;
    }
    // 2. érosion : les pentes ne dépassent pas le talus de leur altitude
    const it = T.ITER; T.ITER = 100; erode(H); T.ITER = it; smoothOnce(H, 0.2);
    // 3. replats : épaulements sur les arêtes, puis replats à flanc et fonds de cirque, à des altitudes variées
    const hAt = (x, z) => { const gx = SS.clamp((x + HALF) / CELL, 0, N - 1.001), gz = SS.clamp((z + HALF) / CELL, 0, N - 1.001), ix = gx | 0, iz = gz | 0, fx = gx - ix, fz = gz - iz, i = iz * N + ix; return (H[i] * (1 - fx) + H[i + 1] * fx) * (1 - fz) + (H[i + N] * (1 - fx) + H[i + N + 1] * fx) * fz; };
    const ownAt = (x, z) => own[SS.clamp(Math.round((z + HALF) / CELL), 0, N - 1) * N + SS.clamp(Math.round((x + HALF) / CELL), 0, N - 1)];
    T.villages = M.slice(0, 4).map(m => { const v = toWorld(m, 0, VZ); return { x: v.x, z: v.z, ux: m.dir * m.s, uz: m.dir * m.c }; });
    T.plateaus = [];
    const pv = SS.rng(seed * 7 + 41);
    M.forEach((m, k) => {
      const st = m.st, bands = [2050, 2250, 2450, 2550, 2650, 2850, 3000, 3150, 3300].filter(b => b < st.top - 420), vil = T.villages[k];
      for (let j = 0; j < st.plateaus; j++) {
        const band = bands[(j * 3 + k) % bands.length], rad = st.pr[0] + (st.pr[1] - st.pr[0]) * pv();
        let best = null;
        for (let tries = 0; tries < 160 && !best; tries++) {
          let lx, lz;
          if (j < m.peaks.length - 1 && tries < 60) {                     // épaulement entre deux reliefs : on en repart vers plusieurs versants
            const p1 = m.peaks[0], p2 = m.peaks[j + 1], t = 0.35 + 0.4 * pv(); lx = p1.x + (p2.x - p1.x) * t; lz = p1.z + (p2.z - p1.z) * t;
          } else { const a = pv() * Math.PI * 2, d = 500 + pv() * Math.min(3800, (m.fr || FR) * 0.75); lx = m.p0.x + Math.sin(a) * d; lz = m.p0.z + Math.cos(a) * d; }
          if (Math.max(Math.abs(lx), Math.abs(lz)) > (m.fr || FR) * 0.86) continue;
          const w = toWorld(m, lx, lz); if (!T.inside(w.x, w.z, 600) || ownAt(w.x, w.z) !== k) continue;
          if (T.villages.some(v => Math.hypot(w.x - v.x, w.z - v.z) < 1300)) continue;
          const h = hAt(w.x, w.z); if (Math.abs(h - band) > 170) continue;
          const gx = (hAt(w.x + 40, w.z) - hAt(w.x - 40, w.z)) / 80, gz = (hAt(w.x, w.z + 40) - hAt(w.x, w.z - 40)) / 80, g = Math.hypot(gx, gz);
          if (g < 0.1 || g > 0.85) continue;
          if (T.plateaus.some(p => Math.hypot(p.x - w.x, p.z - w.z) < p.r + rad + 450)) continue;
          best = { x: w.x, z: w.z, h, gx: -gx / g, gz: -gz / g };
        }
        if (!best) continue;
        const { x: px, z: pz, h: hc, gx: dx, gz: dz } = best, Rb = rad * 3.2;
        const x0 = Math.max(0, Math.floor((px - Rb + HALF) / CELL)), x1 = Math.min(N - 1, Math.ceil((px + Rb + HALF) / CELL));
        const z0 = Math.max(0, Math.floor((pz - Rb + HALF) / CELL)), z1 = Math.min(N - 1, Math.ceil((pz + Rb + HALF) / CELL));
        for (let iz = z0; iz <= z1; iz++) for (let ix = x0; ix <= x1; ix++) {
          const x = -HALF + ix * CELL, z = -HALF + iz * CELL, ax = (x - px) * dx + (z - pz) * dz, cx2 = (x - px) * dz - (z - pz) * dx;      // banquette allongée le long des courbes de niveau
          const e = Math.hypot(ax / 0.7, cx2 / 1.35) / (rad * (1 + 0.32 * noise.fbm(x * 0.003 + j * 3.1, z * 0.003 - k, 2)));
          const w = 1 - SS.smooth(0.55, 1.8, e); if (w <= 0) continue;
          const tgt = hc - 0.065 * ((x - px) * dx + (z - pz) * dz);         // légère inclinaison vers l'aval : on garde de la vitesse
          H[iz * N + ix] = SS.lerp(H[iz * N + ix], tgt, w);
        }
        T.plateaus.push({ x: Math.round(px), z: Math.round(pz), r: Math.round(rad), h: Math.round(hc), massif: k });
      }
    });
    // 4. reprise d'érosion : les ressauts sous les replats restent raides mais skiables, les replats ne bougent pas
    T.ITER = 32; erode(H); T.ITER = it;
    // 5. aiguilles au sommet principal et sur les sous-sommets ; chaque montagne atteint au moins son altitude visée (≥ 3 300 m)
    M.forEach((m, k) => {
      const st = m.st, sp = [{ x: m.p0.x, z: m.p0.z, a: st.spikeH, r: 230 }];
      for (const p of m.peaks.slice(1).filter(p => !p.rt).sort((a, b) => b.h - a.h).slice(0, st.spikes - 1)) sp.push({ x: p.x, z: p.z, a: st.spikeH * 0.6, r: 170 });
      for (const s of sp) {
        const w = toWorld(m, s.x, s.z), Rb = s.r * 3;
        for (let iz = Math.max(0, Math.floor((w.z - Rb + HALF) / CELL)); iz <= Math.min(N - 1, Math.ceil((w.z + Rb + HALF) / CELL)); iz++)
          for (let ix = Math.max(0, Math.floor((w.x - Rb + HALF) / CELL)); ix <= Math.min(N - 1, Math.ceil((w.x + Rb + HALF) / CELL)); ix++) {
            const x = -HALF + ix * CELL, z = -HALF + iz * CELL, q = ((x - w.x) ** 2 + (z - w.z) ** 2) / (s.r * s.r);
            const rug = SS.clamp(0.55 + 0.9 * (noise.ridged(x * 0.0065, z * 0.0065, 3) - 0.35), 0.25, 1.2);
            H[iz * N + ix] += s.a * Math.exp(-q) * rug;
          }
      }
      let top = 0; for (let i = 0; i < H.length; i++) if (own[i] === k && H[i] > top) top = H[i];
      if (m.style !== 'butte' && (top < st.top || top > st.top + 250)) { const f = (Math.min(Math.max(top, st.top), st.top + 250) - 2400) / Math.max(1, top - 2400); for (let i = 0; i < H.length; i++) if (own[i] === k && H[i] > 2400) H[i] = 2400 + (H[i] - 2400) * f; }
      const pk = toWorld(m, m.p0.x, m.p0.z); m.peak = { x: pk.x, z: pk.z };
      m.top = 0; for (let i = 0; i < H.length; i++) if (own[i] === k && H[i] > m.top) m.top = H[i]; m.top = Math.round(m.top);
    });
    M.forEach((m, k) => { for (const p of m.peaks) if (p.rt) { const w = toWorld(m, p.x, p.z); T.plateaus.push({ x: Math.round(w.x), z: Math.round(w.z), r: Math.round(p.rt), h: 0, massif: k, top: true }); } });
    for (const p of T.plateaus) p.h = Math.round(hAt(p.x, p.z));
    // 6. replats des villages (le joueur au sud-ouest, les rivaux ailleurs)
    for (const v of T.villages) {                          // rampe douce du village vers la montagne (≈ 6 %) : les pistes redescendent jusqu'au front de neige
      const R3 = 3700, x0 = Math.max(0, Math.floor((v.x - R3 + HALF) / CELL)), x1 = Math.min(N - 1, Math.ceil((v.x + R3 + HALF) / CELL)), z0 = Math.max(0, Math.floor((v.z - R3 + HALF) / CELL)), z1 = Math.min(N - 1, Math.ceil((v.z + R3 + HALF) / CELL));
      for (let iz = z0; iz <= z1; iz++) for (let ix = x0; ix <= x1; ix++) {
        const x = -HALF + ix * CELL, z = -HALF + iz * CELL, dx = x - v.x, dz = z - v.z, up = -(dx * v.ux + dz * v.uz), side = Math.abs(dx * v.uz - dz * v.ux);
        if (up < 0) continue;
        const ramp = CV.h + Math.min(60, up * 0.07) + Math.max(0, up - 900) * 0.058, i = iz * N + ix;
        const w = (1 - SS.smooth(1300, 2600, side)) * (1 - SS.smooth(3000, 3700, up));
        if (H[i] < ramp) H[i] += (ramp - H[i]) * w;
      }
    }
    for (const v of T.villages) {
      const R2 = 950, x0 = Math.max(0, Math.floor((v.x - R2 + HALF) / CELL)), x1 = Math.min(N - 1, Math.ceil((v.x + R2 + HALF) / CELL)), z0 = Math.max(0, Math.floor((v.z - R2 + HALF) / CELL)), z1 = Math.min(N - 1, Math.ceil((v.z + R2 + HALF) / CELL));
      for (let iz = z0; iz <= z1; iz++) for (let ix = x0; ix <= x1; ix++) {
        const x = -HALF + ix * CELL, z = -HALF + iz * CELL, dx = x - v.x, dz = z - v.z, along = dx * v.ux + dz * v.uz, side = dx * v.uz - dz * v.ux, d = Math.hypot(side * 0.8, along - 10);
        const w = 1 - SS.smooth(330, 900, d); if (w <= 0) continue;
        H[iz * N + ix] = SS.lerp(H[iz * N + ix], CV.h + SS.clamp(-along * 0.07, -8, 60), w);
      }
    }
    smoothOnce(H, 0.12);
  }

  T.init = function (seed) {
    seed = seed || 1337;
    N = SS.N; CELL = SS.CELL; HALF = SS.HALF;
    if (T.H.length !== N * N) T.H = new Float32Array(N * N);
    T.seed = seed; T.geo = null; T.snowMask = new Float32Array(N * N); T.removedTrees = new Set(); T.viewMode = 'normal'; T.summer = false;
    T.plateaus = []; T.MASSIFS = null;
    if (SS.BIG) bigWorld(seed); else { T.villages = [{ x: CV.x, z: CV.z, ux: 0, uz: 1 }]; classic(seed, T.H); noise = SS.makeNoise(seed); }
    const H = T.H;
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
    const r = SS.rng(T.seed + 99), step = SS.BIG ? 27 : 21, trees = T.trees = [];
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
      if (T.villages.some(v => Math.hypot(px - v.x + v.ux * 30, pz - v.z + v.uz * 30) < 560)) continue;
      if (Math.abs(px - SS.VILLAGE.x - 40) < 45 && pz > SS.VILLAGE.z - 50) continue;             // route d'accès
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
