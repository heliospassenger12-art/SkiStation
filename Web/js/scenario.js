// SkiStation Web — scénarios : objectifs par étapes, délais, échec et victoire, notoriété, candidature olympique,
// stations préconstruites (ex. une station en difficulté à redresser)
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data, T = SS.Terrain, Mg = SS.Mgmt;
  const Sc = SS.Scn = {};
  const KEY = 'skistation_scn';
  let G;

  Sc.get = id => D.SCENARIOS.find(s => s.id === id);
  Sc.cur = () => SS.G && SS.G.scn ? Sc.get(SS.G.scn.id) : null;
  Sc.records = function () { try { return JSON.parse(localStorage.getItem(KEY) || '{}'); } catch (e) { return {}; } };
  function saveRecord(id, stars, days) { try { const r = Sc.records(); if (!r[id] || r[id].stars < stars || (r[id].stars === stars && days < r[id].days)) r[id] = { stars, days }; localStorage.setItem(KEY, JSON.stringify(r)); } catch (e) { } }

  // ---------- statistiques utilisées par les objectifs ----------
  Sc.stats = function () {
    G = SS.G; const s = SS.stats();
    s.money = G.money; s.total = G.totalVisitors; s.year = G.year; s.loan = G.loan;
    return s;
  };

  // ---------- NOTORIÉTÉ : la renommée, portée par l'humeur des visiteurs et le rapport qualité/prix ----------
  // Une station modeste mais où tout le monde est heureux et paie le juste prix peut avoir une notoriété excellente.
  Sc.valueForMoney = function () {
    G = SS.G; const fair = SS.Sim.today && SS.Sim.today.fair || 26;
    let v = 55 + 70 * (1 - SS.passNow() / fair);                                        // forfait vs juste prix
    let w = 0, r = 0, q = 0;
    for (const b of G.buildings) {
      const d = D.building(b.type); if (!(SS.Mgmt.isService(b) || d.type === 'lodging') || !b.price) continue;
      const k = Math.max(1, b.visits + (b.guests || 0)); w += k; r += k * b.price / (SS.Mgmt.refPrice(b) || b.price); q += k * (b.rating - 3);
    }
    if (w) { v += 30 * (1 - r / w) + 8 * (q / w); }
    return SS.clamp(v, 0, 100);
  };
  Sc.fameParts = function () {
    G = SS.G; const s = SS.stats(), parts = {};
    parts.humeur = 0.5 * s.satAvg;
    parts.qualitePrix = 0.2 * Sc.valueForMoney();
    parts.frequentation = 0.15 * Math.min(100, 100 * Math.log10(1 + G.bestDay) / Math.log10(1501));
    parts.prestige = 0.1 * G.prestige;
    parts.evenements = Math.min(15, Object.keys(G.eventsHosted).reduce((a, k) => { const e = D.EVENTS.find(x => x.id === k); return a + (e ? Math.min(2, G.eventsHosted[k]) * e.fameGain * 0.5 : 0); }, 0));
    parts.marketing = (G.marketing.tv > 0 ? 5 : 0) + (G.marketing.social > 0 ? 3 : 0);
    return parts;
  };
  Sc.fameTarget = function () {
    G = SS.G; const p = Sc.fameParts(), base = Object.values(p).reduce((a, b) => a + b, 0);
    return SS.clamp(base * SS.clamp(0.25 + G.totalVisitors / 2500, 0.25, 1), 0, 100);          // sans clientèle, pas de renommée
  };
  Sc.dailyFame = function () {
    G = SS.G; const t = Sc.fameTarget();
    G.fame = SS.clamp(G.fame + (t - G.fame) * (t > G.fame ? 0.05 : 0.04), 0, 100);
  };

  // ---------- création d'une partie de scénario ----------
  Sc.setup = function (g, opts) {
    G = g; const sc = Sc.get(opts.scn); if (!sc) return;
    const st = sc.start;
    G.scn = { id: sc.id, stage: 0, done: false, failed: false, stars: 0 };
    G.mode = 'career'; G.money = st.money; G.fame = st.fame || 5;
    if (st.rep !== undefined) G.reputation = st.rep;
    if (st.sat !== undefined) G.sat = st.sat;
    G.research.done = (st.research || []).slice(); G.tier = 0;
    for (const id of G.research.done) { const r = D.RESEARCH.find(x => x.id === id); if (r && r.tier) G.tier = Math.max(G.tier, r.tier); }
    if (st.prebuilt === 'decay') { const m = G.money; G.money = 1e9; buildDecay(); G.money = m; G.ledger = { inc: {}, exp: {} }; G.season = { inc: {}, exp: {}, days: 0, visitors: 0 }; G.history = []; }
  };

  function rngFor(seed) { return SS.rng(seed * 3 + 11); }
  function placeLift(type, rng, o) {
    const m = SS.reachMask();
    for (let i = 0; i < 900; i++) {
      const a = o.from || { x: SS.HUB.x + (rng() - 0.5) * 240, z: SS.HUB.z + (rng() - 0.3) * 110 };
      const ang = (rng() - 0.5) * (o.spread || 1.8), len = o.min + rng() * (o.max - o.min), b = { x: a.x + Math.sin(ang) * len, z: a.z - Math.cos(ang) * len };
      const gi = Math.round((b.z + SS.HALF) / SS.CELL) * SS.N + Math.round((b.x + SS.HALF) / SS.CELL); if (!m[gi]) continue;
      if (G.lifts.some(l => Math.hypot(l.b.x - b.x, l.b.z - b.z) < 260)) continue;
      const an = SS.analyzeLift(type, a, b); if (an.ok) return SS.commitLift(an);
    }
    return null;
  }
  function placePiste(l, ease) { const r = SS.autoPiste({ x: l.b.x, z: l.b.z }, null, 30, ease); return r.err || r.bad ? null : SS.commitPiste(r.ctrl, r.an); }
  function placeBuilding(type, rng, z) {
    for (let i = 0; i < 200; i++) { const x = -520 + rng() * 1000, an = SS.analyzeBuilding(type, x, z + (rng() - 0.5) * 120); if (an.ok) return SS.commitBuilding(an, 0); }
    return null;
  }

  // Station vieillissante : remontées usées, pistes mal entretenues, peu de services, dettes, forfait trop cher
  function buildDecay() {
    const rng = rngFor(G.seed || 4242);
    const l1 = placeLift('tow_classic', rng, { min: 380, max: 700 }), l2 = placeLift('chair_fixed_2', rng, { min: 900, max: 1300 }), l3 = placeLift('tow_classic', rng, { min: 650, max: 900 });
    [[l1, 0, 0.32], [l2, 2, 0.48], [l3, 1, 0.4]].forEach(([l, ease, cond]) => { if (!l) return; l.cond = cond; const p = placePiste(l, ease); if (p) { p.groomed = 0.25; p.depth = 40; if (ease === 2) p.ice = 0.45; } });
    const sn = placeBuilding('snack', rng, 2600), rt = placeBuilding('rental', rng, 2640), gt = placeBuilding('gite', rng, 2900), gg = placeBuilding('garage', rng, 2760);
    if (sn) { sn.range = 0; sn.staffLvl = 0; sn.rating = 1.7; } if (rt) { rt.range = 0; rt.rating = 2; } if (gt) { gt.rating = 2.1; }
    if (gg) gg.machines = 1;
    G.loans = [{ id: G.nextId++, name: 'Prêt d\'équipement (ancien)', left: 380000, pay: 3200, rate: 0.0009, daysLeft: 118 }, { id: G.nextId++, name: 'Crédit de trésorerie', left: 90000, pay: 2500, rate: 0.0013, daysLeft: 36 }];
    Mg.syncLoan(); G.passPrice = 38; G.maint = 0; G.wage = 0; G.bestDay = 60; G.snowBase = 40;
    for (const p of G.pistes) { p.cannons = false; }
    SS.Net.rebuild(); SS.updatePrestige();
  }

  // ---------- vérification des étapes, échec, victoire ----------
  Sc.goalState = function (stage) { const s = Sc.stats(); return stage.goals.map(g => ({ text: g[0], ok: !!g[1](s), val: g[2] ? g[2](s) : '' })); };
  Sc.hint = function () {
    G = SS.G; const sc = Sc.cur(); if (!sc || G.scn.done || G.scn.failed) return null;
    const st = sc.stages[G.scn.stage], gs = Sc.goalState(st), todo = gs.find(g => !g.ok);
    return todo ? '<b>Objectif de l\'étape « ' + st.title + ' »</b> : ' + todo.text + (todo.val ? ' (' + todo.val + ')' : '') + '.' : null;
  };
  Sc.daysLeft = function () { G = SS.G; const sc = Sc.cur(); return sc && sc.deadline ? Math.max(0, sc.deadline - G.elapsed) : null; };

  SS.checkCareer = function (report) {
    G = SS.G; const sc = Sc.cur(); if (!sc || G.scn.done || G.scn.failed) return;
    const note = m => { if (report) report.notes.push(m); else SS.log(m, 'good'); };
    // échec : faillite ou délai dépassé
    if (G.money < -(sc.bankrupt || 200000) && !G.unlimited) { G.scn.failed = true; if (report) { report.failed = true; report.failTitle = 'Faillite'; report.failText = 'Vos dettes dépassent ce que la banque tolère : la station est mise sous tutelle. Vous pouvez continuer en mode libre ou recommencer.'; } SS.emit('career'); return; }
    if (sc.deadline && G.elapsed >= sc.deadline) { G.scn.failed = true; if (report) { report.failed = true; report.failTitle = 'Délai dépassé'; report.failText = 'Le délai de ' + sc.deadline + ' jours est écoulé sans que tous les objectifs soient atteints. Vous pouvez continuer en mode libre.'; } SS.emit('career'); return; }
    const st = sc.stages[G.scn.stage], s = Sc.stats();
    if (st.goals.every(g => g[1](s))) {
      G.money += st.reward; G.scn.stage++;
      note('🎖️ Étape « ' + st.title + ' » accomplie ! Prime : ' + SS.eur(st.reward) + '.');
      if (G.scn.stage >= sc.stages.length) {
        G.scn.done = true; G.scn.stage = sc.stages.length - 1;
        const ratio = sc.deadline ? G.elapsed / sc.deadline : 0.5, stars = ratio <= 0.5 ? 3 : ratio <= 0.8 ? 2 : 1;
        G.scn.stars = sc.deadline ? stars : 3; saveRecord(sc.id, G.scn.stars, G.elapsed);
        if (report) { report.victory = true; report.victoryText = sc.victory; report.stars = G.scn.stars; report.elapsed = G.elapsed; }
      }
      SS.emit('career');
    }
  };

  // ---------- CRITÈRES DES GRANDS ÉVÉNEMENTS INTERNATIONAUX ----------
  // Les critères olympiques reprennent tous ceux des X Games, avec des seuils plus élevés : on peut organiser
  // les X Games sans viser les Jeux, mais on ne peut pas prétendre aux Jeux sans le niveau X Games.
  const C = (text, test, val) => [text, test, val];
  const pct = k => s => Math.round(s[k]) + ' %', n = k => s => SS.num(s[k] || 0);
  Sc.CRIT = {
    xgames: [
      C('Notoriété d\'au moins 60 %', s => s.fame >= 60, pct('fame')),
      C('Prestige ★★½ (45)', s => s.prestige >= 45, s => s.prestige + ' / 45'),
      C('Satisfaction moyenne (7 jours) d\'au moins 72 %', s => s.satAvg >= 72, pct('satAvg')),
      C('Snowpark et scène de concerts', s => s.snowpark >= 1 && s.stage >= 1, s => (s.snowpark ? '✔' : '✘') + ' / ' + (s.stage ? '✔' : '✘')),
      C('800 lits touristiques', s => s.beds >= 800, s => SS.num(s.beds) + ' / 800'),
      C('30 km de pistes dont 2 noires', s => s.km >= 30 && s.black >= 2, s => s.km.toFixed(0) + ' km · ' + s.black + ' noire(s)'),
      C('Débit des remontées de 9 000 pers./h', s => s.capacity >= 9000, s => SS.num(s.capacity) + ' / 9 000'),
      C('10 pisteurs répartis sur 2 postes de secours', s => s.patrol >= 10 && s.rescue >= 2, s => s.patrol + ' pisteurs · ' + s.rescue + ' poste(s)'),
      C('5 restaurants', s => s.restaurants >= 5, s => s.restaurants + ' / 5'),
      C('Une Coupe d\'Europe déjà organisée', s => s.h_europe >= 1, n('h_europe'))
    ],
    olympics: [
      C('Notoriété d\'au moins 82 %', s => s.fame >= 82, pct('fame')),
      C('Prestige ★★★½ (72)', s => s.prestige >= 72, s => s.prestige + ' / 72'),
      C('Satisfaction moyenne (7 jours) d\'au moins 78 %', s => s.satAvg >= 78, pct('satAvg')),
      C('Snowpark, scène, stade de slalom et stade olympique', s => s.snowpark >= 1 && s.stage >= 1 && s.stadium >= 1 && s.olympic >= 1, s => [s.snowpark, s.stage, s.stadium, s.olympic].map(x => x ? '✔' : '✘').join(' ')),
      C('1 400 lits dont 250 de luxe', s => s.beds >= 1400 && s.luxBeds >= 250, s => SS.num(s.beds) + ' lits · ' + SS.num(s.luxBeds) + ' luxe'),
      C('55 km de pistes dont 4 noires, sommet au-dessus de 3 000 m', s => s.km >= 55 && s.black >= 4 && s.topAlt >= 3000, s => s.km.toFixed(0) + ' km · ' + s.black + ' noires · ' + SS.num(s.topAlt) + ' m'),
      C('Débit de 16 000 pers./h et une remontée d\'exception', s => s.capacity >= 16000 && s.premiumLift, s => SS.num(s.capacity) + ' / 16 000 · ' + (s.premiumLift ? 'exception ✔' : 'exception ✘')),
      C('18 pisteurs, 3 postes de secours et un héliport', s => s.patrol >= 18 && s.rescue >= 3 && s.heli >= 1, s => s.patrol + ' · ' + s.rescue + ' postes · héliport ' + (s.heli ? '✔' : '✘')),
      C('8 restaurants', s => s.restaurants >= 8, s => s.restaurants + ' / 8'),
      C('6 dameuses et la neige de culture', s => s.machines >= 6 && SS.researched('snowmaking'), s => s.machines + ' dameuses · ' + (SS.researched('snowmaking') ? 'neige ✔' : 'neige ✘')),
      C('Une Coupe d\'Europe et une Coupe du monde organisées', s => s.h_europe >= 1 && s.h_worldcup >= 1, s => s.h_europe + ' · ' + s.h_worldcup),
      C('Dette inférieure à 1 M€', s => s.debt < 1000000, s => SS.num(s.debt) + ' €')
    ]
  };
  Sc.critState = function (id) { const s = Sc.stats(); return Sc.CRIT[id].map(c => ({ text: c[0], ok: !!c[1](s), val: c[2](s) })); };
  Sc.critOk = id => Sc.critState(id).every(c => c.ok);
  Sc.critShare = id => { const st = Sc.critState(id); return st.filter(c => c.ok).length / st.length; };

  // ---------- CANDIDATURE OLYMPIQUE : longue et sélective ----------
  // 1. Dépôt (prérequis : niveau X Games atteint, Coupe du monde, recherche, notoriété 75 %, prestige 60).
  // 2. Évaluation par la commission du CIO pendant 25 jours, face à 3 villes rivales.
  // 3. Vote : le meilleur dossier l'emporte. 4. Préparation de 100 jours, inspection à mi-parcours.
  // 5. Inspection finale : tous les critères olympiques. 6. Jour 1 des Jeux = victoire de la partie.
  const RIVALS = ['Val d\'Argent', 'Hochfeld', 'Nordvik', 'Montsorel', 'Sankt Albrun', 'Lago Bianco', 'Kirovsk-Ouest', 'Shiramine'];
  Sc.BID_COST = 600000; Sc.EVAL_DAYS = 25; Sc.PREP_DAYS = 100;
  Sc.VICTORY = 'olympics_day1';                       // condition de victoire réutilisable (mode compétition Alpine Empires)
  Sc.bidCan = function () {
    G = SS.G; const s = Sc.stats(), why = [];
    if (G.bid && ['eval', 'prep', 'games', 'done'].indexOf(G.bid.phase) >= 0) return { ok: false, why: ['Une candidature est déjà en cours.'] };
    if (G.bid && G.bid.phase === 'refused' && G.elapsed < G.bid.retry) why.push('Nouvelle candidature possible dans ' + (G.bid.retry - G.elapsed) + ' jours.');
    if (!SS.researched('olympic')) why.push('Recherche « Dossier olympique » requise.');
    if (!Sc.critOk('xgames')) why.push('Le niveau X Games doit être atteint (tous ses critères remplis).');
    if (!(s.h_worldcup >= 1)) why.push('Il faut avoir organisé une Coupe du monde.');
    if (s.fame < 75) why.push('Notoriété de 75 % requise (' + Math.round(s.fame) + ' %).');
    if (s.prestige < 60) why.push('Prestige de 60 requis (' + s.prestige + ').');
    if (!SS.canPay(Sc.BID_COST)) why.push('Il faut ' + SS.eur(Sc.BID_COST) + ' pour monter le dossier.');
    return { ok: !why.length, why };
  };
  Sc.dossierScore = function () {                     // 0-100 : ce que voit la commission d'évaluation
    const s = Sc.stats();
    return SS.clamp(55 * Sc.critShare('olympics') + 0.15 * s.fame + 0.12 * s.prestige + 0.1 * s.satAvg + 2 * Math.min(4, s.h_worldcup + s.h_xgames + s.h_europe), 0, 100);
  };
  Sc.bidStart = function () {
    G = SS.G; const c = Sc.bidCan(); if (!c.ok) return c.why[0];
    if (!SS.spend(Sc.BID_COST, 'Événements')) return 'Fonds insuffisants.';
    const pool = RIVALS.slice().sort(() => Math.random() - 0.5).slice(0, 3);
    G.bid = { phase: 'eval', due: G.elapsed + Sc.EVAL_DAYS, rivals: pool.map(n => ({ name: n, score: Math.round(74 + Math.random() * 16) })), tries: ((G.bid && G.bid.tries) || 0) + 1 };
    SS.log('📁 Dossier de candidature olympique déposé. La commission du CIO évalue les 4 candidatures pendant ' + Sc.EVAL_DAYS + ' jours.', 'info'); return null;
  };
  Sc.bidReqState = () => Sc.critState('olympics');

  Sc.dailyBid = function (report) {
    G = SS.G; const b = G.bid; if (!b) return;
    if (b.phase === 'eval') {
      for (const r of b.rivals) r.score = SS.clamp(r.score + (Math.random() - 0.45) * 0.6, 60, 97);   // les rivaux progressent aussi
      if (G.elapsed < b.due) return;
      const me = Math.round(Sc.dossierScore()), best = b.rivals.reduce((a, r) => r.score > a.score ? r : a, b.rivals[0]);
      b.score = me;
      if (me > best.score) { b.phase = 'prep'; b.due = G.elapsed + Sc.PREP_DAYS; b.mid = G.elapsed + Math.round(Sc.PREP_DAYS / 2); report.notes.push('🏛️ Vote du CIO : votre dossier (' + me + ' / 100) devance ' + b.rivals.map(r => r.name + ' (' + Math.round(r.score) + ')').join(', ') + '. Les Jeux Olympiques vous sont attribués ! ' + Sc.PREP_DAYS + ' jours pour remplir tout le cahier des charges.'); }
      else { b.phase = 'refused'; b.retry = G.elapsed + 60; report.notes.push('🏛️ Vote du CIO : ' + best.name + ' l\'emporte (' + Math.round(best.score) + ' contre ' + me + ' pour vous). Renforcez la station et recandidatez dans 60 jours.'); }
    } else if (b.phase === 'prep') {
      if (b.mid && G.elapsed >= b.mid) { b.mid = 0; const miss = Sc.critState('olympics').filter(c => !c.ok); report.notes.push(miss.length ? '🔍 Inspection de mi-parcours : ' + miss.length + ' critère(s) encore non rempli(s) — ' + miss.slice(0, 3).map(c => c.text).join(', ') + (miss.length > 3 ? '…' : '') + '.' : '🔍 Inspection de mi-parcours : tout est conforme, continuez ainsi.'); }
      if (G.elapsed < b.due) return;
      if (G.summer) { b.late = true; return; }                       // les Jeux se tiennent en hiver
      const miss = Sc.critState('olympics').filter(c => !c.ok);
      if (!miss.length) { b.phase = 'games'; G.event = { id: 'olympics', day: G.day + 1, left: 6 }; report.notes.push('🔥 Inspection finale réussie : la flamme olympique arrive demain. Les Jeux Olympiques d\'hiver commencent chez vous !'); }
      else { b.phase = 'refused'; b.retry = G.elapsed + 80; G.reputation = Math.max(0, G.reputation - 12); G.fame = Math.max(0, G.fame - 18); report.notes.push('😱 Inspection finale : ' + miss.length + ' critère(s) non rempli(s) (' + miss.slice(0, 3).map(c => c.text).join(', ') + '…). Le CIO retire les Jeux. Réputation −12, notoriété −18.'); }
    } else if (b.phase === 'games' && !G.event) b.phase = 'done';
  };
  // appelé au début de chaque journée : le jour 1 des Jeux Olympiques marque la victoire
  Sc.dayStart = function (report) {
    G = SS.G;
    if (G.event && G.event.id === 'olympics' && G.event.day === G.day && !G.olympicsDay1) {
      G.olympicsDay1 = G.elapsed;
      report.notes.push('🏅 Jour 1 des Jeux Olympiques d\'hiver : la cérémonie d\'ouverture se tient à ' + G.name + '. Partie gagnée !');
      const sc = Sc.cur();
      if (sc && sc.bid && !G.scn.done) {                      // la campagne est gagnée dès l'ouverture des Jeux
        G.scn.stage = sc.stages.length - 1; G.scn.done = true; G.scn.stars = 3; saveRecord(sc.id, 3, G.elapsed);
        report.victory = true; report.victoryText = sc.victory; report.stars = 3; report.elapsed = G.elapsed; SS.emit('career');
      } else if (sc) SS.checkCareer(report);
      if (!G.scn) { report.victory = true; report.victoryText = 'Les Jeux Olympiques d\'hiver s\'ouvrent chez vous : vous avez gagné la partie. Vous pouvez continuer à jouer.'; }
    }
  };

  Sc.free = function () { G = SS.G; G.scn = null; G.mode = 'sandbox'; };
})();
