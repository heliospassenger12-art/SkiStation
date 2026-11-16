// SkiStation Web — état de partie, règles de construction (devis avant validation), réseau GPS, finances, sauvegarde
(function () {
  'use strict';
  const SS = window.SS, T = SS.Terrain, D = SS.Data;
  let G = null;
  const SAVE_KEY = 'skistation_web_save_v1';

  SS.newGame = function (opts) {
    G = SS.G = {
      v: 1, mode: opts.mode || 'career', unlimited: !!opts.unlimited, allUnlocked: !!opts.allUnlocked, casual: !!opts.casual,
      name: opts.name || 'Mont-Hélios', seed: opts.seed || 1337,
      day: 0, year: 1, time: SS.DAY_START, speed: 1, paused: true,
      money: opts.mode === 'sandbox' ? 2500000 : [900000, 650000, 450000][opts.level === undefined ? 1 : opts.level], loan: 0, level: opts.level === undefined ? 1 : opts.level, ach: {},
      ledger: { inc: {}, exp: {} }, history: [],
      pistes: [], lifts: [], buildings: [], roads: [], nextId: 1, flats: [],
      research: { done: [], cur: null }, tier: 0,
      passPrice: 26, nightPrice: 14, summerPass: 12, summer: false, maint: 1, marketing: { social: 0, tv: 0 },
      reputation: 50, prestige: 0, satToday: [], sat: 60,
      weather: null, forecast: [], snowBase: 55,
      career: { stage: 0, done: false }, event: null, eventsHosted: {}, bestDay: 0,
      medals: {}, incidentsToday: 0, visitorsToday: 0, totalVisitors: 0, log: [], hints: {}
    };
    // équipements de départ : route d'accès, parking communal, billetterie
    G.roads.push({ id: G.nextId++, fixed: true, pts: [{ x: 40, z: 3990 }, { x: 40, z: 3400 }, { x: 60, z: 3050 }, { x: 30, z: 2900 }] });
    placeStarter('ticket', -40, 2620); placeStarter('parking', 120, 2960);
    SS.Mgmt.migrate(G);
    SS.Weather.init();
    SS.Net.rebuild();
    SS.Sim.startDay();
    return G;
  };
  function placeStarter(type, x, z) {
    const def = D.building(type), y = T.getH(x, z);
    T.flatten(x, z, def.r, y); G.flats.push([x, z, def.r, y]);
    G.buildings.push(makeBuilding(def, x, z, 0, true));
  }
  function makeBuilding(def, x, z, rot, starter) {
    return SS.Mgmt.initBuilding({ id: G.nextId++, type: def.id, x, z, y: T.getH(x, z), rot: rot || 0, level: 1, price: def.price || 0, open: true, occ: 0, queue: [], visits: 0, revenue: 0, node: -1, starter: !!starter, staff: def.type === 'school' ? 3 : 0 });
  }

  // ---------- finances ----------
  SS.canPay = c => G.unlimited || G.money >= c;
  SS.spend = function (c, cat) {
    if (c <= 0) return true;
    if (!G.unlimited) { if (G.money < c) return false; G.money -= c; }
    G.ledger.exp[cat] = (G.ledger.exp[cat] || 0) + c; return true;
  };
  SS.charge = function (c, cat) { if (!G.unlimited) G.money -= c; G.ledger.exp[cat] = (G.ledger.exp[cat] || 0) + c; };  // dépense obligatoire (peut passer en négatif)
  SS.earn = function (c, cat) { G.money += c; G.ledger.inc[cat] = (G.ledger.inc[cat] || 0) + c; };
  SS.log = function (msg, kind) { G.log.unshift({ t: G.time, d: G.day, msg, kind: kind || 'info' }); if (G.log.length > 60) G.log.pop(); SS.emit('toast', msg, kind || 'info'); };
  SS.researched = id => G.allUnlocked || G.research.done.indexOf(id) >= 0;
  SS.liftUnlocked = def => G.allUnlocked || def.tier <= G.tier;
  SS.buildingUnlocked = def => !def.research || SS.researched(def.research);
  SS.maxLen = def => Math.round(def.maxLen * (1 + 0.08 * Math.max(0, G.tier - def.tier)));

  // ---------- calendrier ----------
  const WD = ['Mer.', 'Jeu.', 'Ven.', 'Sam.', 'Dim.', 'Lun.', 'Mar.'], MO = [['déc.', 31], ['janv.', 31], ['févr.', 28], ['mars', 31], ['avr.', 30]];
  SS.SEASON_DAYS = 140;
  SS.dateStr = function (day, summer) {
    if (summer === undefined ? G.summer : summer) return WD[day % 7] + ' ' + (day < 21 ? (day + 11) + ' juil.' : (day - 20) + ' août');
    let d = day, m = 0; while (m < MO.length - 1 && d >= MO[m][1]) { d -= MO[m][1]; m++; }
    return WD[day % 7] + ' ' + (d + 1) + ' ' + MO[m][0];
  };
  SS.isWeekend = day => day % 7 === 3 || day % 7 === 4;
  SS.isHoliday = day => (day >= 20 && day <= 35) || (day >= 68 && day <= 96);   // Noël/Nouvel An et vacances d'hiver

  // ---------- pistes ----------
  function spline(ctrl) {            // Catmull-Rom centripète simplifiée, rééchantillonnée tous les ~10 m
    const out = [], n = ctrl.length;
    const P = i => ctrl[SS.clamp(i, 0, n - 1)];
    for (let i = 0; i < n - 1; i++) {
      const p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
      const seg = Math.max(2, Math.ceil(Math.hypot(p2.x - p1.x, p2.z - p1.z) / 10));
      for (let k = 0; k < seg; k++) {
        const t = k / seg, t2 = t * t, t3 = t2 * t;
        out.push({
          x: 0.5 * (2 * p1.x + (-p0.x + p2.x) * t + (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * t2 + (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * t3),
          z: 0.5 * (2 * p1.z + (-p0.z + p2.z) * t + (2 * p0.z - 5 * p1.z + 4 * p2.z - p3.z) * t2 + (-p0.z + 3 * p1.z - 3 * p2.z + p3.z) * t3)
        });
      }
    }
    out.push({ x: ctrl[n - 1].x, z: ctrl[n - 1].z });
    let s = 0;
    for (let i = 0; i < out.length; i++) {
      out[i].y = T.getH(out[i].x, out[i].z);
      if (i) s += Math.hypot(out[i].x - out[i - 1].x, out[i].z - out[i - 1].z, out[i].y - out[i - 1].y);
      out[i].s = s;
    }
    return out;
  }
  SS.analyzePiste = function (ctrl, width) {
    const r = { ok: false, width: width || 30 };
    if (ctrl.length < 2) { r.err = 'Placez au moins deux points : le départ en haut, l\'arrivée en bas.'; return r; }
    const pts = r.pts = spline(ctrl);
    r.len = pts[pts.length - 1].s; r.drop = pts[0].y - pts[pts.length - 1].y; r.topAlt = pts[0].y;
    const slopes = []; let minY = pts[0].y, rise = 0, g = {};
    for (let i = 1; i < pts.length; i++) {
      const dh = Math.hypot(pts[i].x - pts[i - 1].x, pts[i].z - pts[i - 1].z) || 1;
      const along = (pts[i - 1].y - pts[i].y) / dh; T.grad(pts[i].x, pts[i].z, g);
      slopes.push(Math.max(Math.abs(along), 0.26 * Math.hypot(g.x, g.z)));   // le terrassement de la piste atténue le dévers
      if (pts[i].y > minY + 5) rise = Math.max(rise, pts[i].y - minY); minY = Math.min(minY, pts[i].y);
      if (!T.inside(pts[i].x, pts[i].z)) { r.err = 'La piste sort des limites du domaine.'; return r; }
    }
    const sorted = slopes.slice().sort((a, b) => a - b);
    r.avg = slopes.reduce((a, b) => a + b, 0) / slopes.length; r.p90 = sorted[Math.floor(sorted.length * 0.9)]; r.max = sorted[sorted.length - 1];
    const m = 0.55 * r.p90 + 0.45 * r.avg;
    r.diff = m < 0.14 ? 0 : m < 0.26 ? 1 : m < 0.42 ? 2 : 3;
    r.trees = T.treesNearPath(pts, r.width / 2 + 5, false);
    r.cost = Math.round((4000 + r.len * 28 * (r.width / 30) * (1 + r.diff * 0.25) + r.trees * 120) / 100) * 100;
    if (r.len < 80) { r.err = 'Piste trop courte (minimum 80 m).'; return r; }
    if (r.drop < 8) { r.err = 'L\'arrivée doit être plus basse que le départ : une piste se descend.'; return r; }
    if (rise > 0) { r.err = 'La piste remonte de ' + Math.round(rise) + ' m : les skieurs ne remontent que par les remontées.'; return r; }
    if (r.p90 > 1.15) { r.err = 'Pente trop forte (' + Math.round(r.p90 * 100) + ' %) : impraticable.'; return r; }
    for (const l of G.lifts) if (D.lift(l.type).kind === 'tow' && crossesLine(pts, l, 45)) { r.err = 'Traversée du ' + l.name + ' interdite : une piste ne coupe pas un téléski.'; return r; }
    if (!SS.canPay(r.cost)) r.warn = 'Fonds insuffisants.';
    r.ok = true; return r;
  };
  function crossesLine(pts, l, margin) {
    const dx = l.b.x - l.a.x, dz = l.b.z - l.a.z, len = Math.hypot(dx, dz), ux = dx / len, uz = dz / len;
    const a = { x: l.a.x + ux * margin, z: l.a.z + uz * margin }, b = { x: l.b.x - ux * margin, z: l.b.z - uz * margin };
    if (len < margin * 2.2) return false;
    for (let i = 0; i < pts.length - 1; i++) if (SS.segCross(pts[i], pts[i + 1], a, b)) return true;
    return false;
  }
  SS.commitPiste = function (ctrl, an, loaded) {
    if (!loaded && !SS.spend(an.cost, 'Construction')) return null;
    const used = G.pistes.map(p => p.name);
    const p = loaded || {
      id: G.nextId++, name: D.PISTE_NAMES.find(n => used.indexOf(n) < 0) || 'Piste ' + G.nextId, ctrl: ctrl.map(c => ({ x: Math.round(c.x), z: Math.round(c.z) })),
      width: an.width, open: true, cannons: false, depth: G.snowBase + (an.topAlt - 1650) * 0.03, groomed: 0.8, ice: 0, slush: 0, powder: 0, passes: 0, prio: 1
    };
    p.pts = an.pts; p.len = an.len; p.drop = an.drop; p.avg = an.avg; p.p90 = an.p90; p.diff = an.diff; p.topAlt = an.topAlt; p.on = 0; p.edges = [];
    T.treesNearPath(p.pts, p.width / 2 + 5, true);
    T.snowAlong(p.pts, p.width / 2 + SS.CELL);
    if (!loaded) { G.pistes.push(p); SS.Net.rebuild(); SS.emit('piste:add', p); SS.emit('world'); }
    return p;
  };
  SS.pisteAt = function (p, s, out) {          // position sur la piste à l'abscisse s
    const pts = p.pts; let lo = 0, hi = pts.length - 1;
    while (hi - lo > 1) { const m = (lo + hi) >> 1; if (pts[m].s <= s) lo = m; else hi = m; }
    const a = pts[lo], b = pts[hi], t = SS.clamp((s - a.s) / ((b.s - a.s) || 1), 0, 1);
    out.x = a.x + (b.x - a.x) * t; out.z = a.z + (b.z - a.z) * t; out.dx = b.x - a.x; out.dz = b.z - a.z; out.slope = (a.y - b.y) / ((b.s - a.s) || 1);
    return out;
  };
  function closestS(p, x, z) {
    let best = 1e9, bs = 0;
    for (let i = 0; i < p.pts.length - 1; i++) {
      const a = p.pts[i], b = p.pts[i + 1], r = SS.segDist(x, z, a.x, a.z, b.x, b.z);
      if (r.d < best) { best = r.d; bs = a.s + (b.s - a.s) * r.t; }
    }
    return { d: best, s: bs };
  }
  SS.closestOnPiste = closestS;

  // ---------- remontées ----------
  SS.analyzeLift = function (defId, a, b, force) {
    const def = D.lift(defId), r = { ok: false, def };
    const ay = T.getH(a.x, a.z), by = T.getH(b.x, b.z), hl = Math.hypot(b.x - a.x, b.z - a.z);
    r.vert = by - ay; r.hlen = hl; r.len = Math.hypot(hl, r.vert); r.maxLen = SS.maxLen(def);
    r.cost = Math.round(def.cost * (0.45 + 0.55 * Math.min(1.3, r.len / def.maxLen)) / 1000) * 1000;
    const speed = def.speed * SS.MOTION * (SS.researched('handsfree') ? 1.12 : 1);
    const n = def.kind === 'cablecar' || def.kind === 'funi' ? 2 : Math.max(2, Math.round(2 * r.len / def.spacing));
    r.vehicles = n; r.capacity = Math.round(def.seats * 3600 / ((2 * r.len / n) / speed)); r.rideMin = r.len / speed / 60;
    r.maint = Math.round(r.cost * 0.0022 + 220);
    if (!T.inside(a.x, a.z) || !T.inside(b.x, b.z)) { r.err = 'Station hors des limites du domaine.'; if (!force) return r; }
    if (r.len < 60) { r.err = 'Ligne trop courte (minimum 60 m).'; if (!force) return r; }
    if (r.len > r.maxLen) { r.err = 'Portée dépassée : ' + Math.round(r.len) + ' m pour ' + r.maxLen + ' m maximum. Rapprochez la gare ou choisissez un autre modèle.'; if (!force) return r; }
    if (r.vert < (def.kind === 'carpet' ? 2 : 10)) { r.err = 'La gare d\'arrivée doit être plus haute que la gare de départ.'; if (!force) return r; }
    if (T.slope(a.x, a.z) > 0.9 || T.slope(b.x, b.z) > 0.9) { r.err = 'Terrain trop raide pour une gare.'; if (!force) return r; }
    // profil du terrain sous la ligne
    const steps = Math.max(4, Math.ceil(hl / 20)), prof = [];
    for (let i = 0; i <= steps; i++) { const t = i / steps, x = SS.lerp(a.x, b.x, t), z = SS.lerp(a.z, b.z, t); prof.push({ x, z, y: T.getH(x, z), t }); }
    if (def.kind === 'tow' || def.kind === 'carpet' || def.kind === 'funi') {
      const lim = def.kind === 'tow' ? 0.6 : def.kind === 'funi' ? 0.75 : 0.24; let hi = prof[0].y;
      for (let i = 1; i < prof.length; i++) {
        const sl = (prof[i].y - prof[i - 1].y) / (hl / steps);
        if (sl > lim) { r.err = 'Pente trop forte sous la ligne (' + Math.round(sl * 100) + ' %, max ' + Math.round(lim * 100) + ' %).'; if (!force) return r; }
        if (prof[i].y < hi - 8) { r.err = 'Le tracé redescend : un ' + (def.kind === 'tow' ? 'téléski' : def.kind === 'funi' ? 'funiculaire' : 'tapis') + ' doit monter en continu.'; if (!force) return r; }
        hi = Math.max(hi, prof[i].y);
      }
      if (def.kind === 'tow') for (const p of G.pistes) if (crossesLine(p.pts, { a, b }, 45)) { r.err = 'Ce téléski couperait la piste ' + p.name + ' : traversée interdite.'; if (!force) return r; }
    }
    // câble : gares + pylônes, relevés si le relief gêne
    const pyl = def.kind === 'funi' ? Math.max(1, Math.round(hl / 30)) : def.kind === 'carpet' ? 0 : def.kind === 'cablecar' ? Math.max(1, Math.floor(hl / 1400)) : Math.max(1, Math.round(hl / (def.big ? 420 : def.kind === 'gondola' ? 125 : def.kind === 'tow' ? 70 : 95)) - 1);
    const sh = def.kind === 'funi' ? 1.2 : def.kind === 'carpet' ? 0.6 : def.kind === 'tow' ? 5 : 6;
    const path = [{ x: a.x, z: a.z, y: ay + sh, g: ay }];
    for (let i = 1; i <= pyl; i++) { const t = i / (pyl + 1), x = SS.lerp(a.x, b.x, t), z = SS.lerp(a.z, b.z, t), g = T.getH(x, z); path.push(def.kind === 'funi' ? { x, z, y: g + 1.2, g } : { x, z, y: g + def.pylonH, g, pylon: true }); }
    path.push({ x: b.x, z: b.z, y: by + sh, g: by });
    const need = def.kind === 'tow' ? 2.5 : def.kind === 'carpet' || def.kind === 'funi' ? -50 : 7;
    for (let it = 0; it < 9; it++) {
      let bad = false;
      for (let i = 0; i < path.length - 1; i++) {
        const p = path[i], q = path[i + 1], d = Math.hypot(q.x - p.x, q.z - p.z), n2 = Math.max(2, Math.ceil(d / 20)); let def2 = 0;
        for (let k = 1; k < n2; k++) { const t = k / n2; def2 = Math.max(def2, T.getH(SS.lerp(p.x, q.x, t), SS.lerp(p.z, q.z, t)) + need - SS.lerp(p.y, q.y, t)); }
        if (def2 > 0.3) { bad = true; if (p.pylon) p.y += def2; if (q.pylon) q.y += def2; if (!p.pylon && !q.pylon) { r.err = 'Le câble toucherait le relief.'; if (!force) return r; } }
      }
      if (!bad) break;
      if (it === 8) { r.err = 'Le relief coupe la ligne : le câble ne passe pas. Déplacez une gare.'; if (!force) return r; }
    }
    for (const p of path) if (p.pylon && p.y - p.g > (def.kind === 'cablecar' || def.big ? 110 : def.kind === 'gondola' ? 70 : 45)) { r.err = 'Pylône trop haut nécessaire (' + Math.round(p.y - p.g) + ' m). Contournez la crête ou choisissez un modèle à plus grande portée.'; if (!force) return r; }
    let s = 0; path[0].s = 0;
    for (let i = 1; i < path.length; i++) { s += Math.hypot(path[i].x - path[i - 1].x, path[i].y - path[i - 1].y, path[i].z - path[i - 1].z); path[i].s = s; }
    r.path = path; r.L = s; r.a = { x: a.x, z: a.z, y: ay }; r.b = { x: b.x, z: b.z, y: by };
    r.trees = T.treesNearPath([a, b], 9, false); r.cost += r.trees * 120;
    if (!SS.canPay(r.cost)) r.warn = 'Fonds insuffisants.';
    r.ok = !r.err; return r;
  };
  SS.commitLift = function (an, loaded) {
    if (!loaded && !SS.spend(an.cost, 'Construction')) return null;
    const used = G.lifts.map(l => l.name), def = an.def;
    const short = def.kind === 'tow' ? 'TK' : def.kind === 'chair' ? 'TS' : def.kind === 'gondola' ? 'TC' : def.kind === 'cablecar' ? 'TPH' : def.kind === 'funi' ? 'Funi' : 'Tapis';
    const l = loaded || { id: G.nextId++, type: def.id, name: short + ' ' + (D.LIFT_NAMES.find(n => !used.some(u => u.endsWith(' ' + n))) || G.nextId), open: true, cond: 1, built: G.day, cost: an.cost };
    l.a = an.a; l.b = an.b; l.path = an.path; l.L = an.L; l.len = an.len; l.vert = an.vert; l.capacity = an.capacity; l.maint = an.maint;
    l.n = an.vehicles; l.D = 2 * l.L / l.n; l.phase = 0; l.veh = []; for (let i = 0; i < l.n; i++) l.veh.push([]);
    l.queue = []; l.brokenUntil = 0; l.windStop = false; l.rides = 0;
    T.treesNearPath([l.a, l.b], 9, true);
    for (const st of [l.a, l.b]) { T.flatten(st.x, st.z, 12, st.y, 14); if (!loaded) G.flats.push([st.x, st.z, 12, st.y, 14]); }
    if (!loaded) { G.lifts.push(l); SS.Net.rebuild(); SS.emit('lift:add', l); SS.emit('world'); }
    return l;
  };
  // modernisation : remplace le modèle sur la même ligne (gares inchangées), remontée vide uniquement
  SS.modernize = function (l, type, dry) {
    const others = G.lifts; G.lifts = others.filter(x => x !== l);
    const an = SS.analyzeLift(type, l.a, l.b); G.lifts = others;
    if (!an.ok) return { err: an.err };
    const cost = Math.max(Math.round(an.cost * 0.1), an.cost - Math.round(l.cost * 0.3));
    if (dry) return { cost, an };
    if (l.queue.length || l.veh.some(v => v.length)) return { err: 'Des visiteurs utilisent encore cette remontée : fermez-la et attendez qu\'elle se vide.' };
    if (!SS.spend(cost, 'Construction')) return { err: 'Fonds insuffisants.' };
    const old = l.name.split(' ')[0]; l.type = type; l.cost = an.cost; l.cond = 1;
    SS.commitLift(an, l);
    const k = an.def.kind, short = k === 'tow' ? 'TK' : k === 'chair' ? 'TS' : k === 'gondola' ? 'TC' : k === 'cablecar' ? 'TPH' : k === 'funi' ? 'Funi' : 'Tapis';
    if (['TK', 'TS', 'TC', 'TPH', 'Funi', 'Tapis'].indexOf(old) >= 0) l.name = short + l.name.slice(old.length);
    Net.rebuild(); SS.emit('lift:refresh', l); SS.emit('world'); return { cost };
  };
  SS.liftPos = function (l, s, out) {           // position sur le câble (s de 0 à L)
    const p = l.path; let i = 0; while (i < p.length - 2 && p[i + 1].s < s) i++;
    const a = p[i], b = p[i + 1], t = SS.clamp((s - a.s) / (b.s - a.s), 0, 1);
    out.x = a.x + (b.x - a.x) * t; out.y = a.y + (b.y - a.y) * t; out.z = a.z + (b.z - a.z) * t; return out;
  };
  SS.liftSeason = l => !G.summer || (D.lift(l.type).kind !== 'tow' && D.lift(l.type).kind !== 'carpet');   // l'été, téléskis et tapis sont à l'arrêt
  SS.passNow = () => G.summer ? G.summerPass : G.passPrice;
  SS.nightOn = () => !G.summer && SS.researched('night') && G.pistes.some(p => p.lit) && G.lifts.some(l => l.night);
  SS.nightActive = () => G.time >= SS.NIGHT_OPEN && G.time < SS.NIGHT_CLOSE && SS.researched('night');
  const nightLift = l => l.night && SS.researched('night');
  SS.liftRunning = l => l.open && SS.liftSeason(l) && !l.windStop && G.time >= l.brokenUntil && ((G.time >= SS.LIFT_OPEN - 900 && G.time < SS.LIFT_CLOSE + 2400) || (nightLift(l) && G.time >= SS.NIGHT_OPEN - 900 && G.time < SS.NIGHT_CLOSE + 1800));
  SS.liftBoarding = l => l.open && SS.liftSeason(l) && !l.windStop && G.time >= l.brokenUntil && ((G.time >= SS.LIFT_OPEN && G.time < SS.LIFT_CLOSE) || (nightLift(l) && SS.nightActive()));
  // après la fermeture du jour, seules les pistes éclairées restent ouvertes
  SS.pisteOpen = p => p.open && !p.snowClosed && !(p.avaUntil > G.time) && (G.time < SS.LIFT_CLOSE + 2700 || G.time >= SS.NIGHT_CLOSE + 600 || !!p.lit);

  // ---------- aides au tracé : zones desservables et tracé assisté ----------
  // Zones d'où une piste peut redescendre jusqu'au front de neige (descente continue, pente de piste admissible).
  SS.reachMask = function (lim) {
    lim = lim || 0.42; const N = SS.N, C = SS.CELL, H = T.H, seen = new Uint8Array(N * N), q = [];
    const hx = Math.round((SS.HUB.x + SS.HALF) / C), hz = Math.round((SS.HUB.z + SS.HALF) / C), R = Math.ceil(SS.HUB_R / C);
    for (let dz = -R; dz <= R; dz++) for (let dx = -R; dx <= R; dx++) if (Math.hypot(dx, dz) * C < SS.HUB_R) { const i = (hz + dz) * N + hx + dx; seen[i] = 1; q.push(i); }
    const nb = [[1, 0, 1], [-1, 0, 1], [0, 1, 1], [0, -1, 1], [1, 1, 1.414], [-1, 1, 1.414], [1, -1, 1.414], [-1, -1, 1.414]];
    for (let k = 0; k < q.length; k++) {
      const i = q[k], x = i % N, z = (i / N) | 0;
      for (const [dx, dz, w] of nb) {
        const nx = x + dx, nz = z + dz; if (nx < 2 || nz < 2 || nx > N - 3 || nz > N - 3) continue;
        const j = nz * N + nx; if (seen[j]) continue; const s = (H[j] - H[i]) / (C * w);
        if (s > -0.02 && s <= lim) { seen[j] = 1; q.push(j); }
      }
    }
    return seen;
  };
  // A* : meilleure descente d'un point de départ vers le front de neige (ou un point d'arrivée donné).
  // Descente continue, pentes douces, détours autour des forêts denses. Renvoie des points de contrôle ou une erreur.
  SS.autoPiste = function (start, goal, width) {
    const N = SS.N, C = SS.CELL, H = T.H, S = 2, M = Math.ceil(N / S);
    const to = i => ({ x: -SS.HALF + (i % M) * S * C, z: -SS.HALF + ((i / M) | 0) * S * C });
    const gx0 = goal || SS.HUB, ci = (x, z) => Math.round((z + SS.HALF) / C / S) * M + Math.round((x + SS.HALF) / C / S);
    const hAt = i => { const p = to(i); return T.getH(p.x, p.z); };
    const si = ci(start.x, start.z), gi = ci(gx0.x, gx0.z), stepL = S * C, g = {};
    if (hAt(si) < T.getH(gx0.x, gx0.z) + 8) return { err: 'Le départ doit être nettement plus haut que l\'arrivée.' };
    const open = [[0, si]], cost = new Map([[si, 0]]), prev = new Map(), done = new Set();
    const nbs = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [-1, 1], [1, -1], [-1, -1]];
    const hg = to(gi); let iter = 0, reached = -1;
    const push = (f, i) => { open.push([f, i]); let k = open.length - 1; while (k > 0) { const p = (k - 1) >> 1; if (open[p][0] <= open[k][0]) break; [open[p], open[k]] = [open[k], open[p]]; k = p; } };
    const pop = () => { const t = open[0], l = open.pop(); if (open.length) { open[0] = l; let k = 0; for (; ;) { let a = k * 2 + 1, b = a + 1, m = k; if (a < open.length && open[a][0] < open[m][0]) m = a; if (b < open.length && open[b][0] < open[m][0]) m = b; if (m === k) break; [open[m], open[k]] = [open[k], open[m]]; k = m; } } return t; };
    while (open.length && iter++ < 60000) {
      const [, i] = pop(); if (done.has(i)) continue; done.add(i);
      const p = to(i);
      if (Math.hypot(p.x - hg.x, p.z - hg.z) < (goal ? 60 : 110)) { reached = i; break; }
      const hi = hAt(i);
      for (const [dx, dz] of nbs) {
        const nx = (i % M) + dx, nz = ((i / M) | 0) + dz; if (nx < 1 || nz < 1 || nx >= M - 1 || nz >= M - 1) continue;
        const j = nz * M + nx; if (done.has(j)) continue;
        const q = to(j); if (!T.inside(q.x, q.z, 80)) continue;
        const hj = T.getH(q.x, q.z), len = stepL * (dx && dz ? 1.414 : 1), along = (hi - hj) / len;
        if (along < -0.003) continue;                                    // jamais de remontée
        T.grad(q.x, q.z, g); const cross = Math.hypot(g.x, g.z), eff = Math.max(along, 0.26 * cross);
        let pen = 1 + 14 * Math.max(0, eff - 0.17) + 3 * Math.max(0, along < 0.03 ? 0.03 - along : 0) * 10 + 4 * Math.max(0, cross - 0.5);
        if (T.slope(q.x, q.z) > 1.0) pen += 30;
        const nc = cost.get(i) + len * pen; if (nc >= (cost.has(j) ? cost.get(j) : 1e18)) continue;
        cost.set(j, nc); prev.set(j, i); push(nc + Math.hypot(q.x - hg.x, q.z - hg.z), j);
      }
    }
    if (reached < 0) return { err: goal ? 'Aucune descente continue ne mène à ce point depuis le départ.' : 'Aucune descente continue ne mène au front de neige depuis ce point. Choisissez un départ plus bas, ou activez la carte des zones desservables pour repérer les emplacements possibles.' };
    const pts = []; for (let i = reached; i !== undefined; i = prev.get(i)) pts.unshift(to(i));
    pts[0] = { x: start.x, z: start.z }; if (goal) pts.push({ x: gx0.x, z: gx0.z });
    { let m = T.getH(start.x, start.z); const out = [pts[0]];           // le départ réel peut être plus bas que la maille voisine : on écarte les points qui feraient remonter
      for (let i = 1; i < pts.length; i++) { const h = T.getH(pts[i].x, pts[i].z); if (h <= m + 0.4) { out.push(pts[i]); m = Math.min(m, h); } }
      if (out.length < 2) return { err: 'Départ trop plat pour tracer une descente.' }; pts.length = 0; pts.push(...out); }
    // simplification (Douglas-Peucker) puis vérification par le vrai calcul de piste ; on affine si besoin
    const dp = (a, tol) => { if (a.length < 3) return a; let md = 0, mi = 0; for (let i = 1; i < a.length - 1; i++) { const d = SS.segDist(a[i].x, a[i].z, a[0].x, a[0].z, a[a.length - 1].x, a[a.length - 1].z).d; if (d > md) { md = d; mi = i; } } if (md < tol) return [a[0], a[a.length - 1]]; return dp(a.slice(0, mi + 1), tol).slice(0, -1).concat(dp(a.slice(mi), tol)); };
    let best = null;
    for (const tol of [90, 60, 40, 28, 18, 10]) {
      const ctrl = dp(pts, tol); if (ctrl.length > 26) continue;
      const an = SS.analyzePiste(ctrl, width || 30); if (an.ok) { best = { ctrl, an }; break; } if (!best) best = { ctrl, an, bad: true };
    }
    if (!best) return { err: 'Tracé trop complexe : choisissez un départ plus proche.' };
    return best;
  };

  // ---------- bâtiments ----------
  SS.inVillage = (x, z) => Math.hypot(x - SS.VILLAGE.x, z - 2720) < 620;
  SS.nearRoad = function (x, z, d) {
    for (const r of G.roads) for (let i = 0; i < r.pts.length - 1; i++) if (SS.segDist(x, z, r.pts[i].x, r.pts[i].z, r.pts[i + 1].x, r.pts[i + 1].z).d < d) return true;
    return false;
  };
  SS.analyzeBuilding = function (defId, x, z) {
    const def = D.building(defId), r = { ok: false, def, x, z, y: T.getH(x, z) };
    r.slope = T.slope(x, z); r.trees = T.treesIn((tx, tz) => Math.hypot(tx - x, tz - z) < def.r + 4, { x0: x - def.r - 4, z0: z - def.r - 4, x1: x + def.r + 4, z1: z + def.r + 4 }, false);
    r.earth = Math.round(r.slope * def.r * def.r * 9 / 100) * 100;
    r.cost = def.cost + r.earth + r.trees * 120;
    if (!T.inside(x, z, 100)) { r.err = 'Hors des limites du domaine.'; return r; }
    if (r.slope > (def.r > 25 ? 0.4 : 0.65)) { r.err = 'Terrain trop raide ici (' + Math.round(r.slope * 100) + ' %).'; return r; }
    for (const d of SS.DECOR) if (Math.hypot(d.x - x, d.z - z) < d.r + def.r) { r.err = 'Emplacement occupé par une maison du village.'; return r; }
    for (const b of G.buildings) if (Math.hypot(b.x - x, b.z - z) < D.building(b.type).r + def.r) { r.err = 'Emplacement occupé par : ' + D.building(b.type).name + '.'; return r; }
    for (const l of G.lifts) if (SS.segDist(x, z, l.a.x, l.a.z, l.b.x, l.b.z).d < def.r + 8) { r.err = 'Trop près de la ligne du ' + l.name + '.'; return r; }
    for (const p of G.pistes) if (def.look !== 'snowpark' && def.look !== 'stadium' && closestS(p, x, z).d < def.r + p.width / 2 - 4) { r.err = 'Sur la piste ' + p.name + ' : décalez le bâtiment sur le bord.'; return r; }
    if (def.road && !SS.inVillage(x, z) && !SS.nearRoad(x, z, 130)) { r.err = 'Doit être desservi par une route (à moins de 130 m) ou placé au village.'; return r; }
    const nd = SS.Net.nearestNode(x, z);
    if (!def.road && def.type !== 'garage' && def.type !== 'heli' && !(nd && nd.d < 240) && !SS.inVillage(x, z)) r.warn = 'Non relié au domaine : placez-le près d\'une gare ou d\'une extrémité de piste pour que les visiteurs y accèdent.';
    if (!SS.canPay(r.cost)) r.warn = 'Fonds insuffisants.';
    r.ok = true; return r;
  };
  SS.commitBuilding = function (an, rot) {
    if (!SS.spend(an.cost, 'Construction')) return null;
    const def = an.def;
    T.treesIn((tx, tz) => Math.hypot(tx - an.x, tz - an.z) < def.r + 4, { x0: an.x - def.r - 4, z0: an.z - def.r - 4, x1: an.x + def.r + 4, z1: an.z + def.r + 4 }, true);
    T.flatten(an.x, an.z, def.r, an.y); G.flats.push([an.x, an.z, def.r, an.y]);
    const b = makeBuilding(def, an.x, an.z, rot);
    G.buildings.push(b); SS.Net.rebuild(); SS.emit('building:add', b); SS.emit('world');
    return b;
  };
  SS.bcap = function (b) {
    const def = D.building(b.type);
    if (def.type === 'garage') return b.machines || def.cap;
    if (def.type === 'rescue') return b.patrol || def.cap;
    return Math.round(def.cap * (1 + 0.5 * (b.level - 1)) * SS.Mgmt.capMul(b));
  };
  SS.upgradeCost = b => Math.round(D.building(b.type).cost * 0.6 * b.level);

  // ---------- routes ----------
  SS.analyzeRoad = function (pts) {
    const r = { ok: false, len: 0, maxSlope: 0 };
    if (pts.length < 2) { r.err = 'Placez au moins deux points.'; return r; }
    if (!SS.inVillage(pts[0].x, pts[0].z) && !SS.nearRoad(pts[0].x, pts[0].z, 90)) { r.err = 'Une route doit partir du village ou d\'une route existante.'; return r; }
    for (let i = 0; i < pts.length - 1; i++) {
      const a = pts[i], b = pts[i + 1], d = Math.hypot(b.x - a.x, b.z - a.z), n = Math.max(1, Math.ceil(d / 25));
      for (let k = 0; k < n; k++) {
        const h0 = T.getH(SS.lerp(a.x, b.x, k / n), SS.lerp(a.z, b.z, k / n)), h1 = T.getH(SS.lerp(a.x, b.x, (k + 1) / n), SS.lerp(a.z, b.z, (k + 1) / n));
        r.maxSlope = Math.max(r.maxSlope, Math.abs(h1 - h0) / (d / n));
      }
      r.len += d;
      if (!T.inside(b.x, b.z)) { r.err = 'Hors des limites.'; return r; }
    }
    r.trees = T.treesNearPath(pts, 8, false);
    r.cost = Math.round((r.len * 130 + r.trees * 120) / 100) * 100;
    if (r.maxSlope > 0.16) { r.err = 'Pente trop forte pour une route (' + Math.round(r.maxSlope * 100) + ' %, max 16 %). Tracez des lacets.'; return r; }
    if (!SS.canPay(r.cost)) r.warn = 'Fonds insuffisants.';
    r.ok = true; return r;
  };
  SS.commitRoad = function (pts, an) {
    if (!SS.spend(an.cost, 'Construction')) return null;
    T.treesNearPath(pts, 8, true);
    const r = { id: G.nextId++, pts: pts.map(p => ({ x: Math.round(p.x), z: Math.round(p.z) })) };
    G.roads.push(r); SS.emit('world'); return r;
  };

  // ---------- démolition (jamais avec des visiteurs engagés dessus) ----------
  SS.demolish = function (kind, obj) {
    if (kind === 'lift') {
      if (obj.queue.length || obj.veh.some(v => v.length)) return 'Des visiteurs utilisent encore cette remontée : fermez-la et attendez qu\'elle se vide.';
      G.lifts.splice(G.lifts.indexOf(obj), 1); SS.earn(Math.round(obj.cost * 0.25), 'Revente');
    } else if (kind === 'piste') {
      if (obj.on > 0) return 'Des skieurs sont encore sur cette piste : fermez-la d\'abord.';
      G.pistes.splice(G.pistes.indexOf(obj), 1);
    } else if (kind === 'building') {
      if (obj.occ > 0 || obj.queue.length) return 'Bâtiment occupé : fermez-le et attendez la sortie des clients.';
      if (obj.type === 'ticket' && G.buildings.filter(b => b.type === 'ticket').length < 2) return 'Il faut conserver au moins une billetterie.';
      G.buildings.splice(G.buildings.indexOf(obj), 1); SS.earn(Math.round(D.building(obj.type).cost * 0.25), 'Revente');
    } else if (kind === 'road') {
      if (obj.fixed) return 'La route d\'accès ne peut pas être supprimée.';
      G.roads.splice(G.roads.indexOf(obj), 1);
    }
    SS.Net.rebuild(); SS.emit('removed', kind, obj); SS.emit('world');
    return null;
  };

  // ---------- réseau du domaine (GPS) ----------
  const Net = SS.Net = { nodes: [], edges: [], version: 0, epoch: 0, cache: new Map() };
  Net.rebuild = function () {
    const old = Net.nodes, nodes = Net.nodes = [], edges = Net.edges = [];
    function node(x, z, snap) {
      for (const n of nodes) if (Math.hypot(n.x - x, n.z - z) < (n.hub ? SS.HUB_R : snap || 75)) return n;
      const n = { id: nodes.length, x, z, y: T.getH(x, z), out: [], services: [] }; nodes.push(n); return n;
    }
    const hub = node(SS.HUB.x, SS.HUB.z); hub.hub = true; Net.hub = hub;
    for (const l of G.lifts) { l.na = node(l.a.x, l.a.z); l.nb = node(l.b.x, l.b.z); }
    for (const p of G.pistes) { const a = p.pts[0], b = p.pts[p.pts.length - 1]; p.na = node(a.x, a.z); p.nb = node(b.x, b.z); }
    const E = e => { e.id = edges.length; edges.push(e); e.from.out.push(e); return e; };
    for (const l of G.lifts) if (l.na !== l.nb) l.edge = E({ kind: 'lift', lift: l, from: l.na, to: l.nb, len: l.L });
    for (const p of G.pistes) {
      const stops = [{ s: 0, n: p.na }];
      for (const n of nodes) { if (n === p.na || n === p.nb) continue; const c = closestS(p, n.x, n.z); if (c.d < 55 && c.s > 50 && c.s < p.len - 50) stops.push({ s: c.s, n }); }
      stops.sort((a, b) => a.s - b.s); stops.push({ s: p.len, n: p.nb });
      p.edges = [];
      for (let i = 0; i < stops.length - 1; i++) if (stops[i].n !== stops[i + 1].n) p.edges.push(E({ kind: 'piste', piste: p, from: stops[i].n, to: stops[i + 1].n, s0: stops[i].s, s1: stops[i + 1].s, len: stops[i + 1].s - stops[i].s }));
    }
    for (const b of G.buildings) {
      let best = null, bd = 260;
      for (const n of nodes) { const d = Math.hypot(n.x - b.x, n.z - b.z); if (d < bd) { bd = d; best = n; } }
      if (SS.inVillage(b.x, b.z)) best = hub;
      b.node = best ? best.id : -1; if (best) best.services.push(b);
    }
    Net.version++; Net.cache.clear();
    SS.emit('network', old);
  };
  Net.touch = function () { Net.epoch++; Net.cache.clear(); };
  Net.nearestNode = function (x, z) {
    let best = null, bd = 1e9;
    for (const n of Net.nodes) { const d = Math.hypot(n.x - x, n.z - z); if (d < bd) { bd = d; best = n; } }
    return best ? { n: best, d: bd } : null;
  };
  Net.edgeUsable = function (e, maxDiff) {
    return e.kind === 'lift' ? SS.liftBoarding(e.lift) : SS.pisteOpen(e.piste) && e.piste.diff <= maxDiff;
  };
  Net.edgeTime = function (e) {     // secondes de jeu, attente estimée comprise
    if (e.kind === 'lift') { const l = e.lift; return l.L / (D.lift(l.type).speed * SS.MOTION) + l.queue.length / Math.max(1, l.capacity) * 3600 + 60; }
    return e.len / (9 * SS.MOTION);
  };
  // Dijkstra depuis src : {dist[], prev[]}. down=true : uniquement des pistes (retour après fermeture)
  Net.from = function (src, maxDiff, down) {
    const key = src + '|' + maxDiff + '|' + (down || 0);
    let r = Net.cache.get(key); if (r) return r;
    const n = Net.nodes.length, dist = new Float64Array(n).fill(Infinity), prev = new Array(n).fill(null), done = new Uint8Array(n);
    dist[src] = 0;
    for (; ;) {
      let u = -1, bd = Infinity;
      for (let i = 0; i < n; i++) if (!done[i] && dist[i] < bd) { bd = dist[i]; u = i; }
      if (u < 0) break; done[u] = 1;
      for (const e of Net.nodes[u].out) {
        if (down === 'plan') { if (!(e.kind === 'lift' ? e.lift.open && SS.liftSeason(e.lift) : e.piste.open && !e.piste.snowClosed && e.piste.diff <= maxDiff)) continue; }
        else if (down) { if (e.kind === 'lift' || !(e.piste.diff <= maxDiff && e.piste.open)) continue; }
        else if (!Net.edgeUsable(e, maxDiff)) continue;
        const d = bd + Net.edgeTime(e);
        if (d < dist[e.to.id]) { dist[e.to.id] = d; prev[e.to.id] = e; }
      }
    }
    r = { dist, prev }; Net.cache.set(key, r); return r;
  };
  Net.path = function (src, dst, maxDiff, down) {
    const r = Net.from(src, maxDiff, down); if (r.dist[dst] === Infinity) return null;
    const path = []; let n = dst;
    while (n !== src) { const e = r.prev[n]; if (!e) return null; path.unshift(e); n = e.from.id; }
    return path;
  };
  Net.time = (src, dst, maxDiff, down) => Net.from(src, maxDiff, down).dist[dst];

  // ---------- statistiques de station / prestige ----------
  SS.stats = function () {
    const s = { lifts: G.lifts.length, km: 0, easy: 0, red: 0, black: 0, connected: 0, topAlt: 0, beds: 0, bestDay: G.bestDay, sat: G.sat, prestige: G.prestige, access: 0, vert: 0 };
    const hub = Net.hub.id;
    for (const p of G.pistes) {
      s.km += p.len / 1000; if (p.open) { if (p.diff <= 1) s.easy++; if (p.diff === 2) s.red++; if (p.diff === 3) s.black++; }
      if (Net.from(p.nb.id, 3, 'plan').dist[hub] < Infinity && Net.from(hub, 3, 'plan').dist[p.na.id] < Infinity) s.connected++;
    }
    let lo = 1e9;
    for (const l of G.lifts) { s.topAlt = Math.max(s.topAlt, l.b.y); lo = Math.min(lo, l.a.y); }
    s.vert = G.lifts.length ? s.topAlt - lo : 0;
    for (const def of D.BUILDINGS) s[def.type] = s[def.type] || 0;
    for (const b of G.buildings) {
      const def = D.building(b.type); s[def.type]++; if (def.id !== def.type) s[def.id] = (s[def.id] || 0) + 1;
      if (def.type === 'lodging') s.beds += SS.bcap(b); if (def.type === 'parking') s.access += SS.bcap(b);
    }
    s.stage = s.stage || 0; s.eventsHosted = Object.values(G.eventsHosted).reduce((a, b) => a + b, 0);
    s.worldCup = G.eventsHosted.worldcup || 0; s.olympics = G.eventsHosted.olympics || 0;
    return s;
  };
  SS.updatePrestige = function () {
    const s = SS.stats(); let p = 0;
    p += Math.min(18, s.km * 0.9); p += Math.min(10, s.vert / 200); p += Math.min(6, (s.red > 0) * 2 + (s.black > 0) * 4);
    let tech = 0; for (const l of G.lifts) tech = Math.max(tech, D.lift(l.type).tier); p += tech * 2;
    const seen = {}; for (const b of G.buildings) if (!seen[b.type]) { seen[b.type] = 1; p += (D.building(b.type).prestige || 0); }
    p += Math.min(10, s.beds / 90); p += Math.min(12, Object.keys(G.eventsHosted).reduce((a, k) => a + D.EVENTS.find(e => e.id === k).prestige * Math.min(2, G.eventsHosted[k]) * 0.5, 0));
    p += (G.reputation - 50) * 0.24;
    G.prestige = SS.clamp(Math.round(p), 0, 100); return s;
  };
  // attractivité du domaine : sert à la demande et à la « juste valeur » du forfait
  SS.attract = function () {
    const s = SS.stats(); let km = 0; const kinds = [0, 0, 0, 0];
    for (const p of G.pistes) if (p.open && !p.snowClosed && p.edges.length && Net.from(p.nb.id, 3, 'plan').dist[Net.hub.id] < Infinity && Net.from(Net.hub.id, 3, 'plan').dist[p.na.id] < Infinity) { km += p.len / 1000; kinds[p.diff]++; }
    let a = Math.min(1, km / 22) * 0.55 + kinds.filter(k => k > 0).length * 0.05 + Math.min(0.1, s.vert / 12000);
    for (const b of G.buildings) a += (D.building(b.type).attract || 0) + (G.summer && D.building(b.type).summer ? 0.04 : 0);
    return { a: Math.min(1.25, a), km, kinds, s };
  };

  // ---------- sauvegarde ----------
  SS.serialize = function () {
    try {
      const strip = (o, keys) => { const c = {}; for (const k in o) if (keys.indexOf(k) < 0) c[k] = o[k]; return c; };
      const data = Object.assign({}, G, {
        pistes: G.pistes.map(p => strip(p, ['pts', 'edges', 'na', 'nb', 'on'])),
        lifts: G.lifts.map(l => strip(l, ['path', 'veh', 'queue', 'na', 'nb', 'edge'])),
        buildings: G.buildings.map(b => strip(b, ['queue', 'occ'])),
        removedTrees: Array.from(T.removedTrees), satToday: []
      });
      return JSON.stringify(data);
    } catch (e) { return null; }
  };
  SS.save = function () { try { const s = SS.serialize(); if (!s) return false; localStorage.setItem(SAVE_KEY, s); return true; } catch (e) { return false; } };
  // code de sauvegarde à copier : survit à un changement d'appareil ou de navigateur
  SS.exportSave = function () { const s = SS.serialize(); return s ? 'SKI1:' + btoa(unescape(encodeURIComponent(s))) : ''; };
  SS.importSave = function (txt) { try { txt = String(txt).trim(); if (txt.indexOf('SKI1:') !== 0) return null; const d = JSON.parse(decodeURIComponent(escape(atob(txt.slice(5))))); return d && d.pistes && d.lifts && d.buildings ? d : null; } catch (e) { return null; } };
  SS.hasSave = function () { try { return !!localStorage.getItem(SAVE_KEY); } catch (e) { return false; } };
  SS.loadData = function () { try { return JSON.parse(localStorage.getItem(SAVE_KEY)); } catch (e) { return null; } };
  // à appeler après T.init(seed) et avant la construction des maillages
  SS.restore = function (data) {
    G = SS.G = data; G.paused = true; SS.Mgmt.migrate(G); if (!G.nightPrice) G.nightPrice = 14; if (!G.ach) G.ach = {}; if (!G.summerPass) G.summerPass = 12; if (G.level === undefined) G.level = 1;
    for (const f of G.flats) T.flatten(f[0], f[1], f[2], f[3], f[4]);
    T.removedTrees = new Set(data.removedTrees || []); delete G.removedTrees;
    const pistes = G.pistes, lifts = G.lifts; G.pistes = []; G.lifts = [];
    for (const l of lifts) { const an = SS.analyzeLift(l.type, l.a, l.b, true); if (an.path) { SS.commitLift(an, l); G.lifts.push(l); } }
    for (const p of pistes) { const an = SS.analyzePiste(p.ctrl, p.width); if (an.pts) { SS.commitPiste(p.ctrl, an, p); G.pistes.push(p); } }
    for (const b of G.buildings) { b.queue = []; b.occ = 0; }
    if (G.time > 9 * 3600) G.time = 8 * 3600;
    Net.rebuild();
    SS.Sim.startDay();
    return G;
  };
})();
