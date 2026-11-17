// SkiStation Web — noyau : utilitaires, bruit, bus d'événements
(function () {
  'use strict';
  const SS = window.SS = { VERSION: 'Web 1.8' };

  SS.WORLD = 8000;            // côté de la carte en mètres (360°)
  SS.HALF = SS.WORLD / 2;
  SS.N = 257;                 // sommets par côté
  SS.CELL = SS.WORLD / (SS.N - 1);
  SS.VILLAGE = { x: 0, z: 2750, h: 1650 };
  SS.HUB = { x: 0, z: 2480 };            // front de neige
  SS.ARRIVAL = { x: 60, z: 2990 };
  SS.HUB_R = 320;                        // rayon du front de neige : toute gare ou fin de piste dans ce cercle s'y raccorde       // route d'accès / parking de départ

  // Une seule horloge : tout est exprimé en secondes de jeu.
  SS.CLOCK_RATE = 30;         // secondes de jeu par seconde réelle à x1
  SS.MOTION = 0.2;            // vitesse simulée = vitesse réelle x MOTION (par seconde de jeu)
  SS.DAY_START = 7.5 * 3600;
  SS.DAY_END = 24 * 3600 - 60;
  SS.LIFT_OPEN = 9 * 3600;
  SS.LIFT_CLOSE = 16.75 * 3600;
  SS.NIGHT_OPEN = 18.5 * 3600;     // ski nocturne : pistes éclairées et remontées désignées
  SS.NIGHT_CLOSE = 21 * 3600;
  SS.SUMMER_DAYS = 30;             // saison d'été jouable (VTT, randonnée) après la recherche dédiée

  // maisons décoratives du vieux village (non interactives)
  SS.DECOR = [
    { x: -150, z: 2800, r: 16, k: 'church' }, { x: -90, z: 2870, r: 11, k: 'house' }, { x: -210, z: 2870, r: 11, k: 'house' }, { x: -250, z: 2790, r: 11, k: 'house' },
    { x: -40, z: 2790, r: 11, k: 'house' }, { x: -130, z: 2930, r: 11, k: 'house' }, { x: 230, z: 2830, r: 11, k: 'house' }, { x: 290, z: 2890, r: 11, k: 'house' }, { x: -310, z: 2880, r: 11, k: 'house' }
  ];

  SS.clamp = (v, a, b) => v < a ? a : v > b ? b : v;
  SS.lerp = (a, b, t) => a + (b - a) * t;
  SS.smooth = (a, b, v) => { const t = SS.clamp((v - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); };
  SS.dist2 = (ax, az, bx, bz) => Math.hypot(ax - bx, az - bz);

  SS.rng = function (seed) {
    let a = seed >>> 0;
    return function () {
      a |= 0; a = a + 0x6D2B79F5 | 0;
      let t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  };
  SS.rand = Math.random;
  SS.pick = (arr, r) => arr[Math.floor((r || Math.random)() * arr.length)];
  SS.range = (a, b) => a + Math.random() * (b - a);

  // --- bruit de valeur 2D, déterministe par graine ---
  SS.makeNoise = function (seed) {
    const P = new Uint16Array(512), G = new Float32Array(256), r = SS.rng(seed);
    const p = []; for (let i = 0; i < 256; i++) { p.push(i); G[i] = r() * 2 - 1; }
    for (let i = 255; i > 0; i--) { const j = Math.floor(r() * (i + 1)); const t = p[i]; p[i] = p[j]; p[j] = t; }
    for (let i = 0; i < 512; i++) P[i] = p[i & 255];
    const g = (x, y) => G[P[(x & 255) + P[y & 255]]];
    function noise(x, y) {
      const xi = Math.floor(x), yi = Math.floor(y), xf = x - xi, yf = y - yi;
      const u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
      const a = g(xi, yi), b = g(xi + 1, yi), c = g(xi, yi + 1), d = g(xi + 1, yi + 1);
      return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
    }
    function fbm(x, y, oct) {
      let s = 0, a = 0.5, f = 1, n = 0;
      for (let i = 0; i < (oct || 4); i++) { s += a * noise(x * f, y * f); n += a; a *= 0.5; f *= 2.03; }
      return s / n;
    }
    function ridged(x, y, oct) {
      let s = 0, a = 0.5, f = 1, n = 0;
      for (let i = 0; i < (oct || 4); i++) { const v = 1 - Math.abs(noise(x * f, y * f)); s += a * v * v; n += a; a *= 0.5; f *= 2.1; }
      return s / n;
    }
    return { noise, fbm, ridged };
  };

  // --- formatage ---
  const nf = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 0 });
  SS.eur = v => nf.format(Math.round(v)) + ' €';
  SS.num = v => nf.format(Math.round(v));
  SS.hhmm = t => { const h = Math.floor(t / 3600), m = Math.floor(t % 3600 / 60); return (h < 10 ? '0' : '') + h + ':' + (m < 10 ? '0' : '') + m; };
  SS.esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  // --- bus d'événements ---
  const handlers = {};
  SS.on = (ev, fn) => { (handlers[ev] = handlers[ev] || []).push(fn); };
  SS.emit = (ev, a, b) => { const h = handlers[ev]; if (h) for (let i = 0; i < h.length; i++) h[i](a, b); };

  // --- géométrie 2D utile ---
  SS.segDist = function (px, pz, ax, az, bx, bz) {       // distance point-segment, renvoie {d,t}
    const dx = bx - ax, dz = bz - az, l2 = dx * dx + dz * dz;
    let t = l2 > 0 ? ((px - ax) * dx + (pz - az) * dz) / l2 : 0;
    t = SS.clamp(t, 0, 1);
    return { d: Math.hypot(px - ax - dx * t, pz - az - dz * t), t };
  };
  SS.segCross = function (a, b, c, d) {                    // intersection stricte de deux segments 2D
    const o = (p, q, r) => (q.x - p.x) * (r.z - p.z) - (q.z - p.z) * (r.x - p.x);
    const o1 = o(a, b, c), o2 = o(a, b, d), o3 = o(c, d, a), o4 = o(c, d, b);
    return (o1 > 0) !== (o2 > 0) && (o3 > 0) !== (o4 > 0);
  };
})();
