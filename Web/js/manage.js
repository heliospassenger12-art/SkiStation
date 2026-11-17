// SkiStation Web — règles de gestion : services (gamme, personnel, horaires, note), hébergements (segments, options),
// banque (prêts échéancés), recherche (laboratoire, file d'attente), pisteurs, dameuses, comptes de saison
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data;
  const M = SS.Mgmt = {};
  const SVC = { food: 1, drink: 1, shop: 1, rental: 1, activity: 1 };
  let G;

  // ---------- valeurs par défaut et reprise des anciennes sauvegardes ----------
  M.initBuilding = function (b) {
    const def = D.building(b.type);
    if (b.range === undefined) b.range = 1;
    if (b.staffLvl === undefined) b.staffLvl = 1;
    if (b.hours === undefined) b.hours = def.eve ? 'eve' : 'day';
    if (b.rating === undefined) b.rating = 3.5;
    if (!b.opts) b.opts = { hb: !!def.hb, spa: false, shuttle: false };
    if (def.type === 'garage' && !b.machines) b.machines = def.cap;
    if (def.type === 'rescue' && !b.patrol) b.patrol = def.cap;
    if (b.sRev === undefined) { b.sRev = 0; b.sCost = 0; b.sVisits = 0; }
    if (b.guests === undefined) b.guests = 0;
    return b;
  };
  M.migrate = function (g) {
    G = g;
    if (!G.loans) { G.loans = []; if (G.loan > 0) G.loans.push({ id: G.nextId++, name: 'Emprunt antérieur', left: G.loan, pay: Math.round(G.loan / 120), rate: 0.0009, daysLeft: 120 }); }
    if (G.wage === undefined) G.wage = 1;
    if (!G.season) G.season = { inc: {}, exp: {}, days: 0, visitors: 0 };
    if (!G.lab) G.lab = 1;
    const r = G.research; if (!r.active) { r.active = []; if (r.cur) { const d = D.RESEARCH.find(x => x.id === r.cur.id); r.active.push({ id: r.cur.id, left: r.cur.left, total: d ? d.days : r.cur.left }); } delete r.cur; }
    if (!r.queue) r.queue = [];
    if (!G.safety) G.safety = { incidents: 0, severe: 0, respSum: 0, respN: 0 };
    if (G.groomNight === undefined) G.groomNight = false;
    if (!G.groomReport) G.groomReport = null;
    if (G.negDays === undefined) G.negDays = 0;
    if (G.fame === undefined) G.fame = Math.round(0.5 * (G.prestige || 0) + 0.2 * (G.reputation || 0));
    if (G.elapsed === undefined) G.elapsed = ((G.year || 1) - 1) * SS.SEASON_DAYS + (G.day || 0);
    if (G.bid === undefined) G.bid = null;
    if (G.scn === undefined) G.scn = G.mode === 'career' && G.career ? { id: 'olympic', stage: Math.min(G.career.stage || 0, 7), done: !!G.career.done, failed: false, stars: 0 } : null;
    delete G.career;
    for (const b of G.buildings) M.initBuilding(b);
    for (const p of G.pistes) if (p.prio === undefined) p.prio = p.noGroom ? 0 : 1;
    M.syncLoan();
  };

  // ---------- services : gamme, personnel, horaires ----------
  M.isService = b => !!SVC[D.building(b.type).type];
  M.refPrice = function (b) { const def = D.building(b.type); return def.type === 'lodging' ? def.price : Math.round((def.price || 0) * [0.75, 1, 1.4][b.range === undefined ? 1 : b.range]); };
  M.quality = function (b) {
    const def = D.building(b.type); G = SS.G;
    let q = (def.quality || 0.5) + ((b.range === undefined ? 1 : b.range) - 1) * 0.13 + ((b.staffLvl === undefined ? 1 : b.staffLvl) - 1) * 0.07 + (G.wage - 1) * 0.04;
    if (SS.researched('training')) q += 0.06;
    if (def.type === 'lodging' && b.opts && b.opts.spa) q += 0.1;
    return SS.clamp(q, 0.1, 1.25);
  };
  M.dur = function (b) { const def = D.building(b.type); return (def.dur || 8) * [1.25, 1, 0.82][b.staffLvl === undefined ? 1 : b.staffLvl] * (def.type === 'food' && SS.researched('kitchen') ? 0.88 : 1); };
  M.capMul = function (b) { const def = D.building(b.type); return (def.type === 'food' && SS.researched('kitchen') ? 1.15 : 1) * (SVC[def.type] ? [0.88, 1, 1.1][b.staffLvl === undefined ? 1 : b.staffLvl] : 1); };
  M.isOpen = function (b) {                     // ouvert maintenant ? (les restaurants « midi » ferment à 17 h 30)
    if (!b.open) return false;
    const t = D.building(b.type).type;
    if ((t === 'food' || t === 'drink') && b.hours !== 'eve' && SS.G.time > 17.5 * 3600) return false;
    return true;
  };
  M.cogsRate = function (b) {                   // coût des marchandises en part du chiffre d'affaires
    const t = D.building(b.type).type; if (!(t === 'food' || t === 'drink' || t === 'shop' || t === 'rental')) return 0;
    const base = t === 'rental' ? 0.12 : t === 'shop' ? 0.5 : [0.27, 0.33, 0.4][b.range === undefined ? 1 : b.range];
    return base * (SS.researched('purchasing') ? 0.85 : 1);
  };
  // charges fixes du jour : [entretien, salaires]
  M.dailyCost = function (b) {
    const def = D.building(b.type); G = SS.G;
    const lvl = 1 + 0.4 * (b.level - 1), wage = [0.88, 1, 1.15][G.wage];
    let up = def.upkeep * lvl * 0.45, sal = def.upkeep * lvl * 0.55 * [0.8, 1, 1.28][b.staffLvl === undefined ? 1 : b.staffLvl] * wage;
    if (!b.open) return [def.upkeep * 0.3, 0];
    if ((def.type === 'food' || def.type === 'drink') && b.hours === 'eve' && !def.eve) sal *= 1.25;
    if (def.type === 'school') sal += b.staff * 190 * wage;
    if (def.type === 'rescue') sal += Math.max(0, b.patrol - def.cap) * 150 * wage;
    if (def.type === 'garage') { sal += Math.max(0, b.machines - def.cap) * 160 * wage; up += Math.max(0, b.machines - def.cap) * 90; if (G.groomNight) sal += b.machines * 170 * wage; }
    if (def.type === 'lodging' && b.opts) { if (b.opts.spa) up += 320; if (b.opts.shuttle) up += 190; }
    return [up, sal];
  };
  M.rate = function (b, score) { b.rating = SS.clamp(b.rating + (score - b.rating) * 0.04, 0, 5); };
  M.setRange = function (b, r) { const k = b.price / (M.refPrice(b) || 1); b.range = r; b.price = Math.max(1, Math.round(M.refPrice(b) * SS.clamp(k, 0.6, 1.6))); };

  // ---------- hébergements : trois segments de clientèle ----------
  M.dinnerPrice = b => [16, 24, 46][D.building(b.type).seg || 0];
  M.spaCost = b => Math.round(D.building(b.type).cost * 0.12 / 1000) * 1000 + 20000;
  // répartit la demande de nuitées entre les hébergements ; renvoie le nombre total de résidents
  M.allocateLodging = function (demand, cal, ev) {
    G = SS.G;
    const N = demand * 0.8 * (SS.researched('booking') ? 1.15 : 1) * (cal > 1 ? 1.1 : 1) * (ev > 1 ? 1.25 : 1) * (G.summer ? 0.7 : 1);
    const pools = [N * 0.44, N * 0.38, N * 0.18 * SS.clamp(0.2 + G.prestige / 60, 0.2, 1.6)];
    const info = { pools: pools.map(Math.round), served: [0, 0, 0], beds: [0, 0, 0] };
    let spill = 0, total = 0;
    for (let seg = 2; seg >= 0; seg--) {
      const list = G.buildings.filter(b => { const d = D.building(b.type); return d.type === 'lodging' && (d.seg || 0) === seg; });
      let pool = pools[seg] + spill, wsum = 0;
      for (const b of list) {
        const def = D.building(b.type), beds = SS.bcap(b); info.beds[seg] += beds;
        b._pf = b.open ? SS.clamp(1.7 - 0.7 * b.price / def.price, 0.05, 1.3) : 0;                      // attrait du tarif
        b._w = beds * b._pf * (0.5 + M.quality(b) * 0.5 + b.rating / 5 * 0.4) * (b.opts.hb ? 1.08 : 1);
        wsum += b._w;
      }
      let servedSeg = 0;
      for (const b of list) {
        const beds = SS.bcap(b), want = wsum > 0 ? pool * b._w / wsum : 0;
        const target = Math.min(beds * Math.min(1, b._pf + 0.1), want);
        b.guests = Math.round(b.guests > 0 ? b.guests * 0.6 + target * 0.4 : target * 0.7);                // les séjours durent : inertie
        b.guests = Math.min(b.guests, beds); if (!b.open) b.guests = 0;
        servedSeg += b.guests;
      }
      info.served[seg] = servedSeg; total += servedSeg;
      spill = Math.max(0, pool - servedSeg) * 0.55;                                                          // les clients non logés se rabattent en partie sur la gamme inférieure
    }
    M.lodgingInfo = info;
    return total;
  };

  // ---------- banque ----------
  M.syncLoan = function () { G = SS.G; G.loan = Math.round(G.loans.reduce((a, l) => a + l.left, 0)); };
  M.creditLimit = () => Math.round(400000 + SS.G.prestige * 45000 + SS.G.reputation * 6000);
  M.takeLoan = function (id) {
    G = SS.G; const p = D.LOANS.find(l => l.id === id);
    if (G.loan + p.amount > M.creditLimit()) return 'La banque refuse : votre plafond d\'endettement est de ' + SS.eur(M.creditLimit()) + '. Il augmente avec le prestige et la réputation.';
    G.loans.push({ id: G.nextId++, name: p.name, left: p.amount, pay: Math.round(p.amount / p.days), rate: p.rate, daysLeft: p.days });
    G.money += p.amount; M.syncLoan(); return null;
  };
  M.repayLoan = function (lid) {
    G = SS.G; const l = G.loans.find(x => x.id === lid); if (!l) return 'Prêt introuvable.';
    if (G.money < l.left && !G.unlimited) return 'Trésorerie insuffisante pour solder ce prêt (' + SS.eur(l.left) + ').';
    if (!G.unlimited) G.money -= l.left; G.loans.splice(G.loans.indexOf(l), 1); M.syncLoan(); return null;
  };
  // échéances du jour : intérêts (charge) + remboursement du capital (sortie de trésorerie)
  M.dailyLoans = function (report) {
    G = SS.G; let interest = 0, capital = 0;
    for (const l of G.loans.slice()) {
      interest += l.left * l.rate; const pay = Math.min(l.pay, l.left); l.left -= pay; capital += pay; l.daysLeft--;
      if (l.left <= 1 || l.daysLeft <= 0) { capital += Math.max(0, l.left); G.loans.splice(G.loans.indexOf(l), 1); report.notes.push('🏦 ' + l.name + ' entièrement remboursé.'); }
    }
    if (interest > 0) SS.charge(Math.round(interest), 'Intérêts');
    if (capital > 0 && !G.unlimited) G.money -= Math.round(capital);
    report.capital = Math.round(capital);
    // découvert prolongé : la banque impose un crédit d'urgence coûteux
    if (!G.unlimited && G.money < -30000) {
      G.negDays++;
      if (G.negDays >= 3) {
        const amt = Math.ceil((-G.money + 40000) / 10000) * 10000;
        G.loans.push({ id: G.nextId++, name: 'Crédit d\'urgence', left: amt, pay: Math.round(amt / 45), rate: 0.0026, daysLeft: 45 });
        G.money += amt; G.negDays = 0; G.reputation = Math.max(0, G.reputation - 1.5);
        report.notes.push('🏦 Découvert prolongé : la banque impose un crédit d\'urgence de ' + SS.eur(amt) + ' au taux fort. Réputation −1,5.');
      }
    } else G.negDays = 0;
    M.syncLoan();
  };

  // ---------- recherche : laboratoire, projets en parallèle, file d'attente ----------
  M.labCost = () => [0, 120000, 320000][SS.G.lab] || 0;
  M.rDays = r => Math.max(1, Math.round(r.days * [1, 1, 0.85, 0.7][SS.G.lab]));
  M.rState = function (r) {
    G = SS.G; const R = G.research;
    if (R.done.indexOf(r.id) >= 0) return 'done';
    if (R.active.some(a => a.id === r.id)) return 'active';
    if (R.queue.indexOf(r.id) >= 0) return 'queued';
    if (r.req && R.done.indexOf(r.req) < 0 && !R.active.some(a => a.id === r.req) && R.queue.indexOf(r.req) < 0) return 'locked';
    return 'open';
  };
  function begin(r) { G.research.active.push({ id: r.id, left: M.rDays(r), total: M.rDays(r) }); }
  M.startResearch = function (id) {
    G = SS.G; const r = D.RESEARCH.find(x => x.id === id), st = M.rState(r);
    if (st !== 'open') return 'Indisponible.';
    const reqDone = !r.req || G.research.done.indexOf(r.req) >= 0;
    if (G.research.active.length < G.lab && reqDone) { if (!SS.spend(r.cost, 'Recherche')) return 'Fonds insuffisants.'; begin(r); return null; }
    G.research.queue.push(id); return 'queued';
  };
  M.cancelQueued = function (id) { const q = SS.G.research.queue, i = q.indexOf(id); if (i >= 0) q.splice(i, 1); };
  M.dailyResearch = function (report) {
    G = SS.G; const R = G.research;
    for (const a of R.active.slice()) {
      a.left--;
      if (a.left <= 0) { const r = D.RESEARCH.find(x => x.id === a.id); R.done.push(r.id); if (r.tier) G.tier = Math.max(G.tier, r.tier); R.active.splice(R.active.indexOf(a), 1); report.notes.push('🔬 Recherche terminée : ' + r.name + '.'); }
    }
    // les projets en attente démarrent dès qu'un poste se libère, si le prérequis est acquis et la trésorerie suffisante
    for (let i = 0; i < R.queue.length && R.active.length < G.lab;) {
      const r = D.RESEARCH.find(x => x.id === R.queue[i]);
      if (r.req && R.done.indexOf(r.req) < 0) { i++; continue; }
      if (!SS.spend(r.cost, 'Recherche')) { i++; continue; }
      R.queue.splice(i, 1); begin(r); report.notes.push('🔬 Recherche lancée depuis la file : ' + r.name + '.');
    }
  };

  // ---------- pisteurs-secouristes ----------
  M.patrolCount = () => SS.G.buildings.reduce((a, b) => a + (b.type === 'rescue' && b.open ? b.patrol : 0), 0);
  M.kmPerPatrol = () => 3 + (SS.researched('signage') ? 1 : 0);
  // chaque matin : les pisteurs sécurisent les pistes les plus proches de leur poste, dans la limite de leur effectif
  M.securePistes = function () {
    G = SS.G; const posts = G.buildings.filter(b => b.type === 'rescue' && b.open);
    let budget = M.patrolCount() * M.kmPerPatrol() * 1000, need = 0;
    const list = G.pistes.filter(p => p.open).map(p => { const mid = p.pts[p.pts.length >> 1]; let d = 1e9; for (const b of posts) d = Math.min(d, Math.hypot(b.x - mid.x, b.z - mid.z)); return { p, d }; }).sort((a, b) => a.d - b.d);
    for (const p of G.pistes) { p.secured = false; p.postDist = 0; }
    for (const it of list) { need += it.p.len; it.p.postDist = it.d < 1e8 ? it.d : -1; if (budget >= it.p.len * 0.5 && posts.length) { it.p.secured = true; budget -= it.p.len; } }
    M.patrolInfo = { patrol: M.patrolCount(), posts: posts.length, needKm: need / 1000, capKm: M.patrolCount() * M.kmPerPatrol(), unsecured: G.pistes.filter(p => p.open && !p.secured).length };
    return M.patrolInfo;
  };
  M.riskFactor = p => (p.secured ? 0.8 : 1.25) * (SS.researched('signage') ? 0.85 : 1);
  M.patrolSpeed = () => SS.researched('patrol2') ? 1.5 : 1;
  M.responseEta = function (p) {                // minutes estimées pour atteindre le milieu de la piste
    if (!(p.postDist > 0)) return null;
    return (240 + p.postDist / (4.5 * M.patrolSpeed())) / 60;
  };

  // ---------- dameuses ----------
  M.groomerCount = () => SS.G.buildings.reduce((a, b) => a + (b.type === 'garage' && b.open ? b.machines : 0), 0);
  M.groomSpeed = () => 0.25 * (SS.researched('winch') ? 1.4 : 1) * (SS.researched('groomer2') ? 1.3 : 1) * (SS.G.groomNight ? 1.5 : 1);
  M.groomCapacityKm = () => M.groomerCount() * (SS.DAY_END - 17.25 * 3600) * M.groomSpeed() / 1000 * 0.9;
  M.groomPlan = function () {                   // ordre de passage prévu ce soir
    G = SS.G; const winch = SS.researched('winch');
    return G.pistes.filter(p => p.prio > 0 && (p.diff < 3 || winch)).sort((a, b) => (b.prio - a.prio) || ((b.passes + b.ice * 500 + (1 - b.groomed) * 300) - (a.passes + a.ice * 500 + (1 - a.groomed) * 300)));
  };
  M.machineCost = 85000;

  // ---------- comptes de saison ----------
  M.closeDay = function (ledger, visitors) {
    G = SS.G; const s = G.season;
    for (const k in ledger.inc) s.inc[k] = (s.inc[k] || 0) + ledger.inc[k];
    for (const k in ledger.exp) s.exp[k] = (s.exp[k] || 0) + ledger.exp[k];
    s.days++; s.visitors += visitors;
  };
  M.newSeason = function () { G = SS.G; G.lastSeason = G.season; G.season = { inc: {}, exp: {}, days: 0, visitors: 0 }; for (const b of G.buildings) { b.sRev = 0; b.sCost = 0; b.sVisits = 0; } G.safety = { incidents: 0, severe: 0, respSum: 0, respN: 0 }; };
  M.sum = o => Object.values(o).reduce((a, b) => a + b, 0);
  M.opex = o => Object.keys(o).filter(k => k !== 'Construction' && k !== 'Recherche').reduce((a, k) => a + o[k], 0);
})();
