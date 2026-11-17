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

  // ---------- notoriété : lente à gagner, lente à perdre ----------
  Sc.fameTarget = function () {
    G = SS.G; let t = (0.45 * G.prestige + 0.45 * G.reputation) * SS.clamp(0.25 + G.totalVisitors / 2500, 0.25, 1);       // sans clientèle, pas de notoriété
    for (const k in G.eventsHosted) { const e = D.EVENTS.find(x => x.id === k); if (e) t += Math.min(2, G.eventsHosted[k]) * e.fameGain * 0.35; }
    if (G.marketing.tv > 0) t += 4; if (G.marketing.social > 0) t += 2;
    if (G.sat < 50) t -= 6;
    return SS.clamp(t, 0, 100);
  };
  Sc.dailyFame = function () {
    G = SS.G; const t = Sc.fameTarget();
    G.fame = SS.clamp(G.fame + (t - G.fame) * (t > G.fame ? 0.07 : 0.04), 0, 100);
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

  // ---------- candidature olympique ----------
  const REQ = [
    ['Stade olympique construit', s => s.olympic >= 1, s => s.olympic + ' / 1'],
    ['Héliport de secours', s => s.heli >= 1, s => s.heli + ' / 1'],
    ['900 lits touristiques', s => s.beds >= 900, s => SS.num(s.beds) + ' / 900'],
    ['Un snowpark', s => s.snowpark >= 1, s => s.snowpark + ' / 1'],
    ['12 pisteurs au moins', s => s.patrol >= 12, s => s.patrol + ' / 12'],
    ['40 km de pistes dont 2 noires', s => s.km >= 40 && s.black >= 2, s => s.km.toFixed(0) + ' km · ' + s.black + ' noire(s)'],
    ['Débit des remontées de 12 000 pers./h', s => s.capacity >= 12000, s => SS.num(s.capacity) + ' / 12 000'],
    ['Satisfaction d\'au moins 70 %', s => s.sat >= 70, s => s.sat + ' %'],
    ['Notoriété d\'au moins 80 %', s => s.fame >= 80, s => Math.round(s.fame) + ' %']
  ];
  Sc.BID_REQ = REQ; Sc.BID_COST = 400000;
  Sc.bidCan = function () {
    G = SS.G; const s = Sc.stats(), why = [];
    if (G.bid && (G.bid.phase === 'dossier' || G.bid.phase === 'prep' || G.bid.phase === 'games' || G.bid.phase === 'done')) return { ok: false, why: ['Une candidature est déjà en cours.'] };
    if (G.bid && G.bid.phase === 'refused' && G.elapsed < G.bid.retry) why.push('Nouvelle candidature possible dans ' + (G.bid.retry - G.elapsed) + ' jours.');
    if (!SS.researched('olympic')) why.push('Recherche « Dossier olympique » requise.');
    if (s.fame < 70) why.push('Notoriété de 70 % requise (' + Math.round(s.fame) + ' %).');
    if (!(s.h_worldcup >= 1)) why.push('Il faut avoir organisé une Coupe du monde.');
    if (!SS.canPay(Sc.BID_COST)) why.push('Il faut ' + SS.eur(Sc.BID_COST) + ' pour monter le dossier.');
    return { ok: !why.length, why };
  };
  Sc.bidStart = function () {
    G = SS.G; const c = Sc.bidCan(); if (!c.ok) return c.why[0];
    if (!SS.spend(Sc.BID_COST, 'Événements')) return 'Fonds insuffisants.';
    G.bid = { phase: 'dossier', due: G.elapsed + 12 }; SS.log('📁 Dossier de candidature olympique déposé. Réponse du CIO dans 12 jours.', 'info'); return null;
  };
  Sc.dossierScore = function () {                 // 0-100 : ce que voit le jury du CIO
    const s = Sc.stats();
    return SS.clamp(0.34 * s.fame + 0.2 * s.sat + 0.14 * Math.min(100, s.beds / 9) + 0.1 * Math.min(100, s.capacity / 120) + 0.1 * Math.min(100, (s.h_worldcup + s.h_xgames + s.h_europe) * 33) + 0.07 * Math.min(100, s.patrol * 8) + 0.05 * (s.heli >= 1 ? 100 : 0), 0, 100);
  };
  Sc.bidChance = function () { return SS.clamp((Sc.dossierScore() - 58) / 28, 0, 1); };
  Sc.bidReqState = function () { const s = Sc.stats(); return REQ.map(r => ({ text: r[0], ok: !!r[1](s), val: r[2](s) })); };

  Sc.dailyBid = function (report) {
    G = SS.G; const b = G.bid; if (!b) return;
    if (b.phase === 'dossier' && G.elapsed >= b.due) {
      const score = Sc.dossierScore(), ch = Sc.bidChance();
      if (Math.random() < ch) { b.phase = 'prep'; b.due = G.elapsed + 70; b.score = Math.round(score); report.notes.push('🏛️ Le CIO vous attribue les Jeux Olympiques d\'hiver ! Vous avez 70 jours pour remplir le cahier des charges (stade olympique, 900 lits, héliport…).'); }
      else { b.phase = 'refused'; b.retry = G.elapsed + 40; b.score = Math.round(score); report.notes.push('🏛️ Le CIO a retenu une autre candidature (dossier noté ' + Math.round(score) + ' / 100). Renforcez votre station et recandidatez dans 40 jours.'); }
    } else if (b.phase === 'prep' && G.elapsed >= b.due) {
      if (G.summer) { b.late = true; return; }                       // les Jeux se tiennent en hiver : on attend la saison
      const st = Sc.bidReqState(), ok = st.every(r => r.ok);
      if (ok) { b.phase = 'games'; G.event = { id: 'olympics', day: G.day + 1, left: 6 }; report.notes.push('🔥 La flamme olympique arrive demain : les Jeux Olympiques d\'hiver commencent chez vous !'); }
      else { b.phase = 'refused'; b.retry = G.elapsed + 60; G.reputation = Math.max(0, G.reputation - 12); G.fame = Math.max(0, G.fame - 18); report.notes.push('😱 Cahier des charges non respecté (' + st.filter(r => !r.ok).map(r => r.text).slice(0, 3).join(', ') + '…) : le CIO retire les Jeux. Réputation −12, notoriété −18.'); }
    } else if (b.phase === 'games' && !G.event) { b.phase = 'done'; }
  };

  Sc.free = function () { G = SS.G; G.scn = null; G.mode = 'sandbox'; };
})();
