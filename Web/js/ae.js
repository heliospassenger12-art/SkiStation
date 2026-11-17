// SkiStation Web — ALPINE EMPIRES (mode compétition, prototype solo) : 4 compagnies sur le même massif.
// Concessions (secteur de départ, extensions achetables, sommet protégé), 3 stations rivales gérées par l'ordinateur qui
// construisent pour de vrai (remontées, pistes, bâtiments visibles), clientèle disputée, échelle des événements,
// candidature olympique concurrente. Victoire : la première compagnie (ou équipe) au jour 1 des Jeux Olympiques.
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data, T = SS.Terrain;
  const AE = SS.AE = {};
  const DEG = Math.PI / 180;
  let G;
  const rt = {};                                   // géométrie d'exécution des rivaux (jamais sauvegardée)

  // ---------- concessions : secteurs angulaires autour du sommet (0° = le village, au sud) ----------
  const ZONES = [
    { id: 'start', name: 'Votre concession', a0: -50, a1: 50, price: 0 },
    { id: 'ext-e', name: 'Extension Est', a0: 50, a1: 80, price: 50000 },
    { id: 'ext-w', name: 'Extension Ouest', a0: -80, a1: -50, price: 100000 },
    { id: 'r1', name: 'Secteur Est', a0: 80, a1: 150 },
    { id: 'r2', name: 'Secteur Nord', a0: 150, a1: 210 },
    { id: 'r3', name: 'Secteur Ouest', a0: -150, a1: -80 }
  ];
  const PROTECT_R = 650;                           // sommet protégé : aucune construction
  AE.ZONES = ZONES; AE.PROTECT_R = PROTECT_R;
  const angOf = (x, z) => { let a = Math.atan2(x, z) / DEG; if (a < -150) a += 360; return a; };   // -150..210
  AE.zoneAt = function (x, z) {
    if (Math.hypot(x, z) < PROTECT_R) return 'protected';
    const a = angOf(x, z); for (const zn of ZONES) if (a >= zn.a0 && a < zn.a1) return zn.id; return 'r2';
  };
  AE.on = () => !!(SS.G && SS.G.ae);
  AE.canBuild = function (x, z) { if (!AE.on()) return true; const id = AE.zoneAt(x, z); return SS.G.ae.owned.indexOf(id) >= 0; };
  // message d'erreur si un tracé sort de la concession du joueur (null sinon)
  AE.checkPts = function (pts) {
    if (!AE.on()) return null; G = SS.G;
    for (const p of pts) {
      if (AE.canBuild(p.x, p.z)) continue;
      const id = AE.zoneAt(p.x, p.z);
      if (id === 'protected') return 'Sommet protégé : aucune construction au-dessus de la ligne de crête centrale.';
      const zn = ZONES.find(z => z.id === id), r = G.ae.rivals.find(r => r.zone === id);
      if (r) return 'Hors de votre concession : ce secteur appartient à ' + r.name + '.';
      return 'Hors de votre concession : achetez l\'' + zn.name.toLowerCase() + ' (' + SS.eur(zn.price) + ') dans le panneau Alpine Empires.';
    }
    return null;
  };
  // les devis du joueur refusent tout tracé hors de sa concession (les rivaux construisent avec AE.busy)
  AE.busy = false;
  const ptsOf = {
    analyzeLift: (r, a) => { const o = []; for (let i = 0; i <= 10; i++) o.push({ x: SS.lerp(a[1].x, a[2].x, i / 10), z: SS.lerp(a[1].z, a[2].z, i / 10) }); return o; },
    analyzePiste: (r, a) => r.pts || a[0],
    analyzeBuilding: (r, a) => [{ x: a[1], z: a[2] }],
    analyzeRoad: (r, a) => a[0]
  };
  for (const k in ptsOf) {
    const fn = SS[k];
    SS[k] = function () {
      const r = fn.apply(this, arguments);
      if (r && !AE.busy && AE.on() && !(k === 'analyzeLift' && arguments[3])) { const e = AE.checkPts(ptsOf[k](r, arguments)); if (e) { r.ok = false; r.err = e; } }
      return r;
    };
  }
  AE.buyZone = function (id) {
    G = SS.G; const zn = ZONES.find(z => z.id === id); if (!zn || !zn.price || G.ae.owned.indexOf(id) >= 0) return 'Indisponible.';
    if (!SS.spend(zn.price, 'Construction')) return 'Fonds insuffisants.';
    G.ae.owned.push(id); SS.emit('world'); return null;
  };

  // ---------- compagnies rivales ----------
  const RIVALS = [
    { id: 'company-2', zone: 'r1', name: 'Valfroide', color: '#e0663a', persona: 'bat', motto: 'Bâtisseur : le plus grand domaine' },
    { id: 'company-3', zone: 'r2', name: 'Pic-Royal', color: '#8a5cd6', persona: 'lux', motto: 'Luxe : palaces, gastronomie, remontées d\'exception' },
    { id: 'company-4', zone: 'r3', name: 'Les Rochettes', color: '#1f9e8f', persona: 'pop', motto: 'Populaire : petits prix et clients ravis' }
  ];
  const PERSONA = {
    bat: { price: 1.0, domain: 1.4, lodge: ['gite', 'chalets', 'hotel', 'residence', 'club'], food: ['restaurant', 'alt_restaurant', 'savoyard'], acts: ['snowpark', 'luge', 'nordic', 'stage', 'rink'], quality: 1, xgames: true },
    lux: { price: 1.3, domain: 0.95, lodge: ['chalets', 'hotel4', 'luxchalet', 'palace', 'hotel4'], food: ['alt_restaurant', 'gastro', 'savoyard'], acts: ['spa', 'stage', 'rink', 'dogs', 'snowpark'], quality: 5, xgames: false },
    pop: { price: 0.82, domain: 1.1, lodge: ['gite', 'residence', 'hotel2', 'gite', 'club'], food: ['snack', 'restaurant', 'pizzeria'], acts: ['snowpark', 'luge', 'stage', 'rink', 'nordic'], quality: 3, xgames: true }
  };
  AE.TEAMS = { solo: 'Individuel : chacun pour soi', duo: 'Deux équipes : vous et Valfroide contre les deux autres' };

  function findBase(zn) {
    let best = null;
    for (let r = 2300; r <= 3700; r += 100) for (let a = zn.a0 + 8; a <= zn.a1 - 8; a += 3) {
      const x = Math.sin(a * DEG) * r, z = Math.cos(a * DEG) * r;
      if (!T.inside(x, z, 150) || T.slope(x, z) > 0.35) continue;
      const h = T.getH(x, z); if (!best || h < best.h) best = { x, z, h };
    }
    return best || { x: Math.sin((zn.a0 + zn.a1) / 2 * DEG) * 3000, z: Math.cos((zn.a0 + zn.a1) / 2 * DEG) * 3000, h: 2400 };
  }
  AE.setup = function (g, opts) {
    G = g;
    G.ae = { team: opts.team === 'duo' ? 'duo' : 'solo', owned: ['start'], rivals: [], news: [], winner: null, edition: null, nextId: 1 };
    for (const def of RIVALS) {
      const zn = ZONES.find(z => z.id === def.zone), base = findBase(zn);
      G.ae.rivals.push({
        id: def.id, name: def.name, color: def.color, persona: def.persona, zone: def.zone, base: { x: Math.round(base.x), z: Math.round(base.z) },
        ally: G.ae.team === 'duo' && def.id === 'company-2',
        money: G.money, loan: 0, fame: 6, prestige: 0, sat: 62, rep: 50, satHist: [], visitors: 0, bestDay: 0, total: 0, price: 26, tier: 0,
        lifts: [], pistes: [], blds: [], hosted: {}, event: null, bid: null, lastBuild: -9, inc: 0, exp: 0
      });
    }
    for (const r of G.ae.rivals) { rt[r.id] = { lifts: [], pistes: [], blds: [] }; addBld(r, 'ticket', true); addBld(r, 'parking', true); }
  };
  const runtime = r => rt[r.id] || (rt[r.id] = { lifts: [], pistes: [], blds: [] });

  // ---------- construction réelle (géométrie du noyau, sans toucher aux listes du joueur) ----------
  function inWedge(r, x, z) { return AE.zoneAt(x, z) === r.zone; }
  function liftObj(r, data, an) {
    const l = SS.commitLift(an, { id: data.id, type: data.type, name: data.name, open: true, cond: 1, cost: an.cost, owner: r.id });
    l.owner = r.id; l.night = false; return l;
  }
  function pisteObj(r, data, an) {
    const p = SS.commitPiste(data.ctrl, an, { id: data.id, name: data.name, ctrl: data.ctrl, width: data.width, open: true, cannons: false, depth: 80, groomed: 0.9, ice: 0, slush: 0, powder: 0, passes: 0, prio: 1 });
    p.owner = r.id; return p;
  }
  function liftTypeFor(r) {
    const prefs = r.persona === 'lux' ? ['cablecar', 'gondola_10', 'gondola_8', 'chair_det_6', 'chair_det_4', 'chair_fixed_4', 'chair_fixed_2'] : ['gondola_8', 'chair_det_6', 'chair_det_4', 'chair_fixed_4', 'chair_fixed_2'];
    return prefs.filter(id => D.lift(id).tier <= r.tier);
  }
  function tryBuildLift(r) { AE.busy = true; try { return tryBuildLift0(r); } finally { AE.busy = false; } }
  function tryBuildLift0(r) {
    const R = runtime(r), types = liftTypeFor(r); if (!types.length) return null;
    const starts = [{ x: r.base.x, z: r.base.z }].concat(R.lifts.map(l => ({ x: l.b.x, z: l.b.z })), R.lifts.map(l => ({ x: l.a.x, z: l.a.z })));
    for (let tries = 0; tries < 26; tries++) {
      const type = types[Math.min(types.length - 1, tries % 3)], def = D.lift(type);
      const s = starts[tries < 4 ? 0 : Math.floor(Math.random() * starts.length)];
      const ax = s.x + (Math.random() - 0.5) * 60, az = s.z + (Math.random() - 0.5) * 60, d = Math.hypot(ax, az) || 1;
      const ang = (Math.random() - 0.5) * 70 * DEG, len = def.maxLen * (0.5 + Math.random() * 0.4);
      const ux = -ax / d, uz = -az / d, bx = ax + (ux * Math.cos(ang) - uz * Math.sin(ang)) * len, bz = az + (ux * Math.sin(ang) + uz * Math.cos(ang)) * len;
      if (!inWedge(r, ax, az) || !inWedge(r, bx, bz)) continue;
      if (R.lifts.some(l => Math.hypot(l.b.x - bx, l.b.z - bz) < 220 || SS.segDist(bx, bz, l.a.x, l.a.z, l.b.x, l.b.z).d < 70)) continue;
      const an = SS.analyzeLift(type, { x: ax, z: az }, { x: bx, z: bz }); if (!an.ok) continue;
      // une piste doit redescendre de l'arrivée vers la base (ou la gare de départ)
      let pr = null;
      for (const goal of [{ x: r.base.x, z: r.base.z }, { x: ax, z: az }]) {
        const res = SS.autoPiste({ x: bx, z: bz }, goal, r.persona === 'lux' ? 35 : 30, [1, 2, 0, 3, 1][R.pistes.length % 5]);
        if (res.err || res.bad || !res.an.ok) continue;
        if (res.an.pts.every(p => inWedge(r, p.x, p.z))) { pr = res; break; }
      }
      if (!pr) continue;
      return { type, an, pr };
    }
    return null;
  }
  function buildLiftAndPiste(r, plan, silent) { AE.busy = true; try { return buildLiftAndPiste0(r, plan, silent); } finally { AE.busy = false; } }
  function buildLiftAndPiste0(r, plan, silent) {
    const R = runtime(r), n = r.lifts.length + 1, short = { tow: 'TK', chair: 'TS', gondola: 'TC', cablecar: 'TPH', funi: 'Funi' }[plan.an.def.kind] || 'TS';
    const ld = { id: r.id + '-l' + G.ae.nextId++, type: plan.type, name: short + ' ' + r.name + ' ' + n, a: { x: Math.round(plan.an.a.x), z: Math.round(plan.an.a.z) }, b: { x: Math.round(plan.an.b.x), z: Math.round(plan.an.b.z) } };
    const l = liftObj(r, ld, plan.an); for (const st of [l.a, l.b]) G.flats.push([st.x, st.z, 12, st.y, 14]);
    r.lifts.push(ld); R.lifts.push(l);
    const names = D.PISTE_NAMES, pd = { id: r.id + '-p' + G.ae.nextId++, name: names[(r.pistes.length * 7 + RIVALS.findIndex(x => x.id === r.id) * 13) % names.length] + ' (' + r.name + ')', ctrl: plan.pr.ctrl.map(c => ({ x: Math.round(c.x), z: Math.round(c.z) })), width: plan.pr.an.width };
    const an2 = SS.analyzePiste(pd.ctrl, pd.width); const p = pisteObj(r, pd, an2.pts ? an2 : plan.pr.an);
    r.pistes.push(pd); R.pistes.push(p);
    r.money -= plan.an.cost + plan.pr.an.cost; r.exp += plan.an.cost + plan.pr.an.cost;
    if (!silent) { SS.emit('lift:add', l); SS.emit('piste:add', p); SS.emit('world'); news(r, 'ouvre le ' + l.name + ' et la piste ' + p.name.replace(' (' + r.name + ')', '') + '.'); }
    return true;
  }
  function addBld(r, type, silent) {
    const def = D.building(type), R = runtime(r);
    for (let k = 0; k < 80; k++) {
      const ang = Math.random() * Math.PI * 2, rad = 50 + Math.random() * (90 + R.blds.length * 14);
      const x = r.base.x + Math.cos(ang) * rad, z = r.base.z + Math.sin(ang) * rad;
      if (!inWedge(r, x, z) || !T.inside(x, z, 120) || T.slope(x, z) > (def.r > 25 ? 0.4 : 0.55)) continue;
      if (R.blds.some(b => Math.hypot(b.x - x, b.z - z) < D.building(b.type).r + def.r + 6)) continue;
      if (R.lifts.some(l => SS.segDist(x, z, l.a.x, l.a.z, l.b.x, l.b.z).d < def.r + 10)) continue;
      if (R.pistes.some(p => p.pts.some(q => Math.hypot(q.x - x, q.z - z) < def.r + p.width / 2))) continue;
      const y = T.getH(x, z), bd = { id: r.id + '-b' + G.ae.nextId++, type, x: Math.round(x), z: Math.round(z), rot: Math.round(ang * 100) / 100 };
      T.flatten(bd.x, bd.z, def.r, y); G.flats.push([bd.x, bd.z, def.r, y]);
      T.treesIn((tx, tz) => Math.hypot(tx - bd.x, tz - bd.z) < def.r + 4, { x0: bd.x - def.r - 4, z0: bd.z - def.r - 4, x1: bd.x + def.r + 4, z1: bd.z + def.r + 4 }, true);
      const b = { id: bd.id, type, x: bd.x, z: bd.z, y, rot: bd.rot, owner: r.id };
      r.blds.push(bd); R.blds.push(b);
      if (!silent) { SS.emit('building:add', b); if (def.cost > 0) { r.money -= def.cost; r.exp += def.cost; } }
      return b;
    }
    return null;
  }
  // relance la géométrie après un chargement (les terrassements sont déjà dans G.flats)
  AE.rebuild = function () {
    G = SS.G; if (!G.ae) return; AE.busy = true;
    for (const r of G.ae.rivals) {
      const R = rt[r.id] = { lifts: [], pistes: [], blds: [] };
      for (const ld of r.lifts) { const an = SS.analyzeLift(ld.type, ld.a, ld.b, true); if (an.path) R.lifts.push(liftObj(r, ld, an)); }
      for (const pd of r.pistes) { const an = SS.analyzePiste(pd.ctrl, pd.width); if (an.pts) R.pistes.push(pisteObj(r, pd, an)); }
      for (const bd of r.blds) R.blds.push({ id: bd.id, type: bd.type, x: bd.x, z: bd.z, y: T.getH(bd.x, bd.z), rot: bd.rot, owner: r.id });
    }
    AE.busy = false;
  };
  // après la construction de la scène 3D : afficher les installations rivales
  AE.afterWorld = function () {
    if (!AE.on()) return; G = SS.G;
    for (const r of G.ae.rivals) { const R = runtime(r); R.lifts.forEach(l => SS.emit('lift:add', l)); R.pistes.forEach(p => SS.emit('piste:add', p)); R.blds.forEach(b => SS.emit('building:add', b)); }
    SS.emit('world');
  };
  AE.lifts = () => { if (!AE.on()) return []; let o = []; for (const r of SS.G.ae.rivals) o = o.concat(runtime(r).lifts); return o; };
  AE.pistesOf = r => runtime(r).pistes;
  AE.rivalOf = id => SS.G.ae.rivals.find(r => r.id === id);

  // ---------- statistiques d'une compagnie rivale (mêmes clés que SS.stats, pour réutiliser les critères) ----------
  AE.statsOf = function (r) {
    const R = runtime(r), s = { lifts: R.lifts.length, km: 0, easy: 0, red: 0, black: 0, connected: R.pistes.length, topAlt: 0, vert: 0, beds: 0, luxBeds: 0, capacity: 0, restaurants: 0, activities: 0, access: 200 };
    for (const p of R.pistes) { s.km += p.len / 1000; if (p.diff <= 1) s.easy++; if (p.diff === 2) s.red++; if (p.diff === 3) s.black++; }
    let lo = 1e9; for (const l of R.lifts) { s.topAlt = Math.max(s.topAlt, l.b.y); lo = Math.min(lo, l.a.y); s.capacity += l.capacity; }
    s.vert = R.lifts.length ? s.topAlt - lo : 0;
    const acts = new Set();
    for (const b of R.blds) {
      const def = D.building(b.type); s[def.type] = (s[def.type] || 0) + 1; if (def.id !== def.type) s[def.id] = (s[def.id] || 0) + 1;
      if (def.type === 'lodging') { s.beds += def.cap; if (def.seg === 2) s.luxBeds += def.cap; }
      if (def.type === 'parking') s.access += def.cap;
      if (def.type === 'food') s.restaurants++;
      if (def.type === 'activity') acts.add(def.id);
    }
    s.activities = acts.size;
    s.patrol = (s.rescue || 0) * 5; s.machines = (s.garage || 0) * 3; s.heli = s.heli || 0;
    s.premiumLift = R.lifts.some(l => ['funicular', 'cablecar', 'three_s'].indexOf(l.type) >= 0);
    s.fame = r.fame; s.prestige = r.prestige; s.sat = Math.round(r.sat); s.satAvg = r.satHist.length ? r.satHist.reduce((a, b) => a + b, 0) / r.satHist.length : r.sat;
    s.bestDay = r.bestDay; s.debt = r.loan; s.money = r.money; s.snowmaking = r.tier >= 2;
    s.eventsHosted = 0; for (const e of D.EVENTS) { s['h_' + e.id] = r.hosted[e.id] || 0; s.eventsHosted += r.hosted[e.id] || 0; }
    s.worldCup = s.h_worldcup; s.olympics = s.h_olympics;
    s.stars = SS.prestigeStars(r.prestige);
    return s;
  };
  function attractOf(r, s) {
    const R = runtime(r), kinds = new Set(R.pistes.map(p => p.diff)).size;
    let a = Math.min(1, s.km / 22) * 0.55 + kinds * 0.05 + Math.min(0.1, s.vert / 12000);
    for (const b of R.blds) a += D.building(b.type).attract || 0;
    return Math.min(1.25, a);
  }
  function prestigeOf(r, s) {
    const R = runtime(r); let tech = 0, prem = 0; const kinds = new Set();
    for (const l of R.lifts) { const d = D.lift(l.type); tech = Math.max(tech, d.tier); kinds.add(d.id); }
    prem = (kinds.has('funicular') ? 4 : 0) + (kinds.has('cablecar') ? 4 : 0) + (kinds.has('three_s') ? 3 : 0) + (kinds.has('gondola_8') || kinds.has('gondola_10') ? 2 : 0) + (kinds.has('bubble_8') ? 1.5 : 0) + (kinds.has('chair_det_6') ? 1 : 0);
    const colors = new Set(R.pistes.map(p => p.diff)).size;
    let pt = Math.min(28, Math.min(12, s.km * 0.4) + Math.min(6, s.vert / 300) + colors + Math.min(6, Math.max(0, (s.topAlt - 2000) / 250)));
    pt += Math.min(14, tech * 1.1 + Math.min(7, prem));
    let nights = 0, priceSum = 0; for (const b of R.blds) { const d = D.building(b.type); if (d.type === 'lodging') { nights += d.cap; priceSum += d.price * d.cap; } }
    const avgNight = nights ? priceSum / nights : 0;
    pt += Math.min(24, Math.min(14, s.luxBeds / 15) + Math.min(6, Math.max(0, (avgNight - 80) / 25)) + (s.luxBeds && r.sat > 70 ? 4 : 0));
    pt += Math.min(20, s.activities * 2 + (s.gastro ? 4 : 0) + (s.spa ? 3 : 0) + Math.min(3, (s.alt_restaurant || 0) + (s.gastro || 0)));
    let ev = 0; for (const k in r.hosted) { const e = D.EVENTS.find(x => x.id === k); if (e) ev += e.prestige * Math.min(2, r.hosted[k]) * 0.35; }
    pt += Math.min(8, ev) + SS.clamp((r.sat - 55) / 8, 0, 6);
    if (!s.luxBeds) pt = Math.min(pt, 55); if (s.activities < 3) pt = Math.min(pt, 60); if (prem < 2) pt = Math.min(pt, 72); if (!s.gastro && !s.spa) pt = Math.min(pt, 80);
    return SS.clamp(Math.round(pt), 0, 100);
  }
  AE.dossierOf = function (r) {
    const s = AE.statsOf(r), share = critShare(s, 'olympics');
    return SS.clamp(55 * share + 0.15 * s.fame + 0.12 * s.prestige + 0.1 * s.satAvg + 2 * Math.min(4, s.h_worldcup + s.h_xgames + s.h_europe), 0, 100);
  };
  function critShare(s, id) { const c = SS.Scn.CRIT[id]; return c.filter(x => x[1](s)).length / c.length; }
  AE.critShare = (r, id) => critShare(AE.statsOf(r), id);

  // ---------- marché : les compagnies se disputent la même clientèle ----------
  function scoreOf(a, fame, prestige, price, fair) { return Math.max(0.01, a) * (0.6 + fame / 125) * (0.75 + prestige / 200) * SS.clamp(1.25 - 0.4 * price / Math.max(1, fair), 0.6, 1.2); }
  function playerScore() { const at = SS.attract(), fair = SS.Sim.fairPrice(at); return scoreOf(at.a, G.fame, G.prestige, SS.passNow(), fair); }
  AE.scores = function () {
    G = SS.G; const o = { player: playerScore() };
    for (const r of G.ae.rivals) { const s = AE.statsOf(r), a = attractOf(r, s); o[r.id] = scoreOf(a, r.fame, r.prestige, r.price, fairOf(r, s, a)); }
    return o;
  };
  function mulFor(id, sc) {
    const ids = Object.keys(sc), mine = sc[id], others = ids.filter(k => k !== id).map(k => sc[k]), avg = others.reduce((a, b) => a + b, 0) / others.length;
    if (mine + avg <= 0.03) return 1;
    return SS.clamp(0.55 + 0.9 * mine / (mine + avg), 0.55, 1.35);
  }
  AE.marketMul = function () { if (!AE.on()) return 1; G = SS.G; return mulFor('player', AE.scores()); };
  function fairOf(r, s, a) { return G.summer ? 9 + s.km * 0.45 + s.vert * 0.002 + a * 8 : 22 + s.km * 1.4 + s.vert * 0.004 + a * 12; }

  // ---------- journée d'une compagnie rivale ----------
  const EFF = [0.85, 1, 1.12];
  function rivalDay(r, sc, report) {
    const R = runtime(r), P = PERSONA[r.persona], s = AE.statsOf(r), a = attractOf(r, s), fair = fairOf(r, s, a), eff = EFF[G.level === undefined ? 1 : G.level];
    r.price = Math.max(8, Math.round(fair * P.price));
    r.inc = 0; r.exp = 0;
    // fréquentation
    let vis = 0;
    if (s.km > 0 && R.lifts.length) {
      const base = (G.summer ? 30 + 650 * Math.pow(a, 1.1) : 40 + 1500 * Math.pow(a, 1.1)) * eff;
      const rep = 0.55 + r.rep / 100 * 0.9, cal = (SS.isWeekend(G.day) ? 1.35 : 1) * (G.summer ? 1.15 : SS.isHoliday(G.day) ? 1.3 : 1);
      const priceF = SS.clamp(1.6 - 0.6 * r.price / fair, 0.2, 1.35), ev = r.event && r.event.day <= G.elapsed ? D.EVENTS.find(e => e.id === r.event.id).boost : 1;
      const demand = base * rep * cal * SS.Weather.def().demand * priceF * ev * (s.rental ? 1 : 0.74) * mulFor(r.id, sc);
      vis = Math.round(Math.min(1500, Math.min(demand, s.access) + Math.min(s.beds * 0.75, demand * 0.6)) * (0.92 + Math.random() * 0.16));
    }
    r.visitors = vis; r.total += vis; r.bestDay = Math.max(r.bestDay, vis);
    // recettes et charges
    const foodCap = (s.restaurants || 0) * 55, acts = s.activities;
    const spend = 6 + 5 * Math.min(1, foodCap / Math.max(1, vis / 5)) + 2.5 * Math.min(4, acts) + (s.rental ? 4 : 0) + (r.persona === 'lux' ? 6 : 0);
    let nightRev = 0; const occ = s.beds ? Math.min(0.85, vis * 0.45 / s.beds) : 0;                // résidents limités par la clientèle réelle
    for (const b of R.blds) { const d = D.building(b.type); if (d.type === 'lodging') nightRev += d.cap * d.price * occ; }
    r.inc = vis * r.price * 0.93 + vis * spend + nightRev;
    let cost = 0; for (const l of R.lifts) cost += l.maint; for (const b of R.blds) cost += D.building(b.type).upkeep * 1.05;
    cost += r.loan * 0.0011;
    r.exp += cost; r.money += r.inc - cost;
    if (r.loan > 0) { const pay = Math.min(r.loan, Math.max(2000, r.loan / 120)); if (r.money > pay + 50000) { r.loan -= pay; r.money -= pay; } }
    if (r.money < -40000) { r.loan += 300000; r.money += 300000; r.rep = Math.max(0, r.rep - 1); }
    // satisfaction, réputation, notoriété, prestige
    const crowd = s.capacity ? vis / (s.capacity * 2.4) : 0;
    let sat = 60 + P.quality + 10 * (fair - r.price) / fair - 16 * (1 - Math.min(1, foodCap / Math.max(1, vis / 4))) - 22 * Math.max(0, crowd - 1) + 2 * Math.min(4, new Set(R.pistes.map(p => p.diff)).size) + Math.min(6, acts * 1.5) - (s.km > 3 && !s.rescue ? 6 : 0) + (Math.random() - 0.5) * 6;
    if (!vis) sat = r.sat;
    r.sat = SS.clamp(r.sat + (sat - r.sat) * 0.5, 15, 96);
    r.satHist.push(Math.round(r.sat)); if (r.satHist.length > 7) r.satHist.shift();
    if (vis) r.rep = SS.clamp(r.rep + (r.sat - r.rep) * Math.min(0.22, 0.04 + vis / 2500), 0, 100);
    r.prestige = prestigeOf(r, s);
    const satAvg = r.satHist.reduce((x, y) => x + y, 0) / r.satHist.length, vfm = SS.clamp(55 + 70 * (1 - r.price / fair), 0, 100);
    let evp = 0; for (const k in r.hosted) { const e = D.EVENTS.find(x => x.id === k); if (e) evp += Math.min(2, r.hosted[k]) * e.fameGain * 0.5; }
    const target = SS.clamp((0.5 * satAvg + 0.2 * vfm + 0.15 * Math.min(100, 100 * Math.log10(1 + r.bestDay) / Math.log10(1501)) + 0.1 * r.prestige + Math.min(15, evp)) * SS.clamp(0.25 + r.total / 2500, 0.25, 1), 0, 100);
    r.fame = SS.clamp(r.fame + (target - r.fame) * (target > r.fame ? 0.05 : 0.04), 0, 100);
    // décisions : une construction par jour au plus
    decide(r, s, vis);
    // événements en cours
    if (r.event && r.event.day <= G.elapsed) {
      r.event.left--;
      if (r.event.left <= 0) {
        const ev = D.EVENTS.find(e => e.id === r.event.id); r.hosted[ev.id] = (r.hosted[ev.id] || 0) + 1;
        if (r.sat >= 50) { r.fame = SS.clamp(r.fame + ev.fameGain, 0, 100); r.rep = SS.clamp(r.rep + ev.prestige * 0.6, 0, 100); }
        if (ev.id !== 'olympics') news(r, (r.sat >= 50 ? 'réussit son ' : 'rate son ') + ev.name.charAt(0).toLowerCase() + ev.name.slice(1) + '.', report, ev.prestige >= 6);
        r.event = null;
      }
    }
  }
  const LADDER = ['fete', 'concert', 'regional', 'europe', 'xgames', 'worldcup'];
  function decide(r, s, vis) {
    const P = PERSONA[r.persona], t = G.elapsed, reserve = 30000 + t * 250;
    // recherche (palier technologique) : grimpe avec le temps et l'argent
    const wantTier = Math.min(6, Math.floor(1 + t / (r.persona === 'lux' ? 26 : 32)));
    if (r.tier < wantTier && r.money > reserve + 120000 * (r.tier + 1)) { const c = 120000 * (r.tier + 1); r.money -= c; r.exp += c; r.tier++; return; }
    if (t - r.lastBuild < 1) return;
    const room = r.money - reserve, can = c => room >= c;
    const kmTarget = Math.min(70, (3 + t * 0.16) * P.domain);
    const needs = [];
    if (!r.lifts.length || s.km < kmTarget || (s.capacity && vis > s.capacity * 2.2)) needs.push('domain');
    if (!s.rental) needs.push('rental');
    if (s.restaurants * 55 < vis / 4) needs.push('food');
    if (s.beds < vis * 0.45) needs.push('lodge');
    if ((s.rescue || 0) < 1 + Math.floor(s.km / 10)) needs.push('rescue');
    if ((s.garage || 0) < Math.ceil(s.km / 12)) needs.push('garage');
    if (s.activities < 1 + Math.floor(t / 22)) needs.push('act');
    if (t > 50 && !s.stadium) needs.push('stadium');
    if (s.access < vis * 1.1) needs.push('parking');
    if (r.persona === 'lux' && t > 40 && !s.luxBeds) needs.unshift('lodge');
    if (t > 140 && critShare(s, 'xgames') > 0.7 && !s.heliport) needs.push('heliport');
    if (r.bid && (r.bid.phase === 'prep' || r.bid.phase === 'eval') && !s.olympic) needs.unshift('olympic');
    for (const n of needs) {
      if (n === 'domain') {
        if (!can(140000)) { if (needs.indexOf('domain') === 0 && r.lifts.length) return; continue; }      // on épargne pour agrandir le domaine
        if (r.failAt === t) continue;
        const plan = tryBuildLift(r); if (plan && can(plan.an.cost + plan.pr.an.cost)) { buildLiftAndPiste(r, plan); r.lastBuild = t; return; }
        r.failAt = t; continue;
      }
      let type = n;
      if (n === 'food') type = P.food[Math.min(P.food.length - 1, Math.floor(s.restaurants / 2))];
      if (n === 'lodge') type = P.lodge[Math.min(P.lodge.length - 1, (s.lodging || 0))];
      if (n === 'act') { type = P.acts.find(a => !s[a]) || null; if (!type) continue; }
      if (n === 'heliport') type = 'heliport';
      const def = D.building(type); if (!def || !can(def.cost)) continue;
      if (addBld(r, type)) { r.lastBuild = t; if (def.cost >= 250000) news(r, 'inaugure : ' + def.name.toLowerCase() + '.'); return; }
    }
    // événements : on grimpe l'échelle quand les conditions sont réunies
    if (!r.event && r.tier >= 2) {
      for (let i = LADDER.length - 1; i >= 0; i--) {
        const ev = D.EVENTS.find(e => e.id === LADDER[i]); if (ev.id === 'xgames' && !P.xgames) continue;
        if ((r.hosted[ev.id] || 0) >= 3) continue;
        if (ev.need(s) && can(ev.cost) && (ev.prestige >= 3 || (r.hosted[ev.id] || 0) < 1)) { r.money -= ev.cost; r.exp += ev.cost; r.event = { id: ev.id, day: G.elapsed + 3, left: ev.days }; if (ev.prestige >= 6) news(r, 'annonce : ' + ev.name + ' dans 3 jours.', null, true); break; }
      }
    }
    // candidature olympique
    if (!r.bid || (r.bid.phase === 'refused' && G.elapsed >= r.bid.retry)) {
      if (r.tier >= 5 && critShare(s, 'xgames') === 1 && s.h_worldcup >= 1 && r.fame >= 75 && r.prestige >= 60 && r.money > 900000 && !AE.editionHolder()) {
        r.money -= SS.Scn.BID_COST; r.exp += SS.Scn.BID_COST; r.bid = { phase: 'eval', due: G.elapsed + SS.Scn.EVAL_DAYS, tries: ((r.bid && r.bid.tries) || 0) + 1 };
        news(r, 'dépose sa candidature aux Jeux Olympiques !', null, true);
      }
    }
  }
  // qui détient l'édition des Jeux (attribuée) : 'player', un id de compagnie, ou null
  AE.editionHolder = function () {
    G = SS.G; if (G.bid && ['prep', 'games', 'done'].indexOf(G.bid.phase) >= 0) return 'player';
    for (const r of G.ae.rivals) if (r.bid && ['prep', 'games', 'done'].indexOf(r.bid.phase) >= 0) return r.id;
    return null;
  };
  function rivalBid(r, report) {
    const b = r.bid; if (!b) return;
    if (b.phase === 'eval' && G.elapsed >= b.due) {
      const me = AE.dossierOf(r), foes = [74 + Math.random() * 14, 74 + Math.random() * 14];
      for (const o of G.ae.rivals) if (o !== r && o.bid && o.bid.phase === 'eval') foes.push(AE.dossierOf(o));
      if (G.bid && G.bid.phase === 'eval') foes.push(SS.Scn.dossierScore());
      const holder = AE.editionHolder();
      if (!holder && me > Math.max.apply(null, foes)) { b.phase = 'prep'; b.due = G.elapsed + SS.Scn.PREP_DAYS; news(r, 'obtient les Jeux Olympiques (dossier ' + Math.round(me) + ' / 100) ! Inspection finale dans ' + SS.Scn.PREP_DAYS + ' jours.', report, true); }
      else { b.phase = 'refused'; b.retry = G.elapsed + 60; news(r, 'échoue au vote du CIO (' + Math.round(me) + ' / 100).', report, true); }
    } else if (b.phase === 'prep' && G.elapsed >= b.due && !G.summer) {
      const share = AE.critShare(r, 'olympics');
      if (share >= 0.92) { b.phase = 'games'; r.event = { id: 'olympics', day: G.elapsed + 1, left: 6 }; news(r, 'passe l\'inspection finale : les Jeux s\'ouvrent demain chez ' + r.name + ' !', report, true); }
      else { b.phase = 'refused'; b.retry = G.elapsed + 80; r.fame = Math.max(0, r.fame - 18); news(r, 'échoue à l\'inspection finale : le CIO lui retire les Jeux.', report, true); }
    } else if (b.phase === 'games' && r.event && r.event.id === 'olympics' && r.event.day === G.elapsed && !G.ae.winner) {
      G.ae.winner = r.id; b.phase = 'done';
      const ally = r.ally;
      if (report) {
        if (ally) { report.victory = true; report.victoryText = 'Les Jeux Olympiques s\'ouvrent chez votre allié ' + r.name + ' : votre équipe remporte la partie ! Vous pouvez continuer à jouer.'; }
        else { report.failed = true; report.failTitle = 'Les Jeux vous échappent'; report.failText = 'La cérémonie d\'ouverture des Jeux Olympiques se tient à ' + r.name + '. La compagnie rivale remporte la partie. Vous pouvez continuer à jouer librement.'; }
      }
      news(r, 'ouvre les Jeux Olympiques d\'hiver : ' + (ally ? 'victoire de votre équipe.' : 'la partie est perdue.'), report, true);
    }
  }
  function news(r, txt, report, big) {
    const msg = '🏔 ' + r.name + ' ' + txt;
    G.ae.news.unshift({ d: G.elapsed, msg }); if (G.ae.news.length > 40) G.ae.news.pop();
    if (big) { if (report) report.notes.push(msg); else SS.log(msg, 'info'); }
  }
  // appelé en fin de journée, après le bilan du joueur
  AE.daily = function (report) {
    G = SS.G; if (!G.ae) return;
    if (G.OlympicsOpened || G.olympicsDay1) G.ae.winner = G.ae.winner || 'player';
    const sc = AE.scores();
    for (const r of G.ae.rivals) { rivalDay(r, sc, report); rivalBid(r, report); }
  };

  // ---------- animation : véhicules et skieurs d'ambiance sur les domaines rivaux ----------
  AE.tick = function (dt) {
    if (!AE.on()) return; const t = SS.G.time, run = t >= SS.LIFT_OPEN - 900 && t < SS.LIFT_CLOSE + 1200 && !SS.G.summer;
    if (!run) return;
    for (const l of AE.lifts()) { const def = D.lift(l.type); l.phase = (l.phase + def.speed * SS.MOTION * dt) % (2 * l.L); }
  };
  const tmp = {};
  AE.drawAmbient = function (n, max, put) {
    if (!AE.on()) return n; const t = SS.G.time, h = t / 3600;
    const k = h < 9 || h > 17 ? 0 : SS.clamp(Math.min(h - 9, 17 - h) / 2.5, 0, 1); if (!k || SS.G.summer) return n;
    for (const r of SS.G.ae.rivals) {
      const R = runtime(r), km = R.pistes.reduce((a, p) => a + p.len, 0) || 1, present = r.visitors * 0.35 * k;
      R.pistes.forEach((p, pi) => {
        const cnt = Math.min(60, Math.round(present * p.len / km));
        for (let i = 0; i < cnt && n < max; i++) {
          const sp = 6 + (i * 37 % 9), s = (t * sp * SS.MOTION + i * 997 + pi * 131) % p.len;
          SS.pisteAt(p, s, tmp); const d = Math.hypot(tmp.dx, tmp.dz) || 1, lat = Math.sin(t * 0.01 + i) * (p.width / 2 - 3);
          const x = tmp.x - tmp.dz / d * lat, z = tmp.z + tmp.dx / d * lat;
          put(n++, x, T.getH(x, z), z, Math.atan2(tmp.dx, tmp.dz), r.color);
        }
      });
    }
    return n;
  };
  AE.labels = function (add) {
    if (!AE.on()) return;
    for (const r of SS.G.ae.rivals) add('ae-' + r.id, '🏔 ' + r.name + (r.ally ? ' (allié)' : ''), r.color, r.base.x, T.getH(r.base.x, r.base.z) + 40, r.base.z);
  };
  // carte : concessions et pistes rivales peintes sur le relief
  AE.paint = function (c, X, Z, k, path) {
    if (!AE.on()) return; const G = SS.G;
    for (const r of G.ae.rivals) for (const p of runtime(r).pistes) {
      c.strokeStyle = r.color; c.lineWidth = (p.width + 7) * k; path(p.pts);
      c.strokeStyle = '#e6edf6'; c.lineWidth = Math.max(1, (p.width - 3) * k); path(p.pts);
    }
    c.setLineDash([30 * k, 22 * k]); c.lineWidth = Math.max(1.5, 6 * k);
    for (const zn of ZONES) for (const a of [zn.a0]) {
      const own = G.ae.owned.indexOf(zn.id) >= 0; c.strokeStyle = own ? 'rgba(255,178,56,0.85)' : 'rgba(90,100,120,0.55)';
      c.beginPath(); c.moveTo(X(Math.sin(a * DEG) * PROTECT_R), Z(Math.cos(a * DEG) * PROTECT_R)); c.lineTo(X(Math.sin(a * DEG) * 6000), Z(Math.cos(a * DEG) * 6000)); c.stroke();
    }
    c.strokeStyle = 'rgba(200,60,60,0.6)'; c.beginPath(); c.arc(X(0), Z(0), PROTECT_R * k, 0, 7); c.stroke(); c.setLineDash([]);
  };

  // ---------- classement ----------
  AE.standings = function () {
    G = SS.G; const s = SS.stats(), rows = [];
    rows.push({ id: 'player', name: G.name, color: '#ffb238', you: true, money: G.money, fame: G.fame, prestige: G.prestige, visitors: G.history.length ? G.history[G.history.length - 1].v : 0, km: s.km, lifts: G.lifts.length, hosted: Object.values(G.eventsHosted).reduce((a, b) => a + b, 0), xg: SS.Scn.critShare('xgames'), oly: SS.Scn.critShare('olympics'), dossier: SS.Scn.dossierScore(), bid: G.bid ? G.bid.phase : null });
    for (const r of G.ae.rivals) { const rs = AE.statsOf(r); rows.push({ id: r.id, name: r.name, color: r.color, ally: r.ally, motto: PERSONA[r.persona] && RIVALS.find(x => x.id === r.id).motto, money: r.money, fame: r.fame, prestige: r.prestige, visitors: r.visitors, km: rs.km, lifts: rs.lifts, hosted: rs.eventsHosted, xg: critShare(rs, 'xgames'), oly: critShare(rs, 'olympics'), dossier: AE.dossierOf(r), bid: r.bid ? r.bid.phase : null, price: r.price, sat: r.sat }); }
    return rows;
  };
})();
