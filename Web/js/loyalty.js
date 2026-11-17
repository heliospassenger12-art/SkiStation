// SkiStation Web — FIDÉLITÉ : la station garde la mémoire de sa clientèle. Les visiteurs contents deviennent des habitués
// (familles, groupes, skieurs seuls) qui reviennent plusieurs fois dans la saison — surtout le week-end et pendant les
// vacances — et d'une saison à l'autre. Les plus fidèles achètent le forfait saison. Les déçus espacent leurs venues
// puis disparaissent. Une clientèle fidèle et heureuse nourrit le bouche-à-oreille (demande, notoriété).
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data;
  const L = SS.Loyal = {};
  const CAP = 1500;
  let G;
  const list = () => { G = SS.G; if (!G.clients) G.clients = []; if (!G.loyalSeq) G.loyalSeq = 1; return G.clients; };
  L.passPrice = () => { G = SS.G; return G.seasonPassPrice || Math.round(G.passPrice * 11); };

  // ---------- qui revient aujourd'hui ----------
  function chance(c) {
    const day = G.day, sinceDays = G.elapsed - c.lastDay;
    let p = 0.04 * c.loy + 0.17 * Math.pow(c.loy, 1.4);             // sans fidélité, plus de retour
    p *= SS.isWeekend(day) ? 1.8 : SS.isHoliday(day) ? 1.6 : 0.75;
    if (sinceDays < 3) p *= 0.3; else if (sinceDays < 7) p *= 0.7;
    if (c.pass === G.year) p *= 2.4;
    p *= SS.Weather.def().demand;
    if (G.summer) p *= 0.3;
    return SS.clamp(p, 0, 0.75);
  }
  // renvoie les habitués qui viennent aujourd'hui, sans dépasser la part de places de la journée
  L.pickToday = function (slots) {
    const out = []; let n = 0; const max = Math.floor(slots * 0.85);
    for (const c of list()) {
      if (n + c.members.length > max) continue;
      if (Math.random() < chance(c)) { out.push(c); n += c.members.length; }
    }
    return out;
  };
  L.expectedToday = function () { let e = 0; for (const c of list()) e += chance(c) * c.members.length; return e; };
  // bouche-à-oreille : des habitués nombreux et heureux font venir du monde
  L.demandMul = function () {
    let n = 0, s = 0; for (const c of list()) if (c.loy >= 0.5) { n++; s += c.sat; }
    if (!n) return 1;
    return SS.clamp(1 + Math.min(0.18, n / 1500 * 0.18) * (s / n - 0.55) / 0.25, 0.9, 1.2);
  };
  L.famePart = function () { let n = 0; for (const c of list()) if (c.loy >= 0.5) n++; return Math.min(6, n / 60); };

  // ---------- fin de journée : mise à jour des souvenirs, nouveaux habitués ----------
  L.endDay = function (visitors, report) {
    const cl = list(), byClient = new Map(), parties = new Map();
    for (const v of visitors) {
      if (v.state !== 'gone') continue;
      if (v.clientId) { (byClient.get(v.clientId) || byClient.set(v.clientId, []).get(v.clientId)).push(v); continue; }
      const key = v.grp ? 'g' + v.grp.id : 'v' + v.id; (parties.get(key) || parties.set(key, []).get(key)).push(v);
    }
    // habitués venus aujourd'hui
    let unhappy = 0;
    for (const [id, vs] of byClient) {
      const c = cl.find(x => x.id === id); if (!c) continue;
      const s = vs.reduce((a, v) => a + v.sat, 0) / vs.length;
      c.visits++; c.vs++; c.lastDay = G.elapsed; c.sat = c.sat * 0.7 + s * 0.3;
      c.loy = SS.clamp(c.loy + (s - 0.62) * 0.5 - (s < 0.45 ? 0.12 : 0), 0, 1); if (s < 0.45) unhappy++;
      const runs = {}; for (const v of vs) for (const k in (v.pc || {})) runs[k] = (runs[k] || 0) + v.pc[k];
      const fav = Object.keys(runs).sort((a, b) => runs[b] - runs[a])[0]; if (fav) c.fav = fav;
      for (let i = 0; i < vs.length && i < c.members.length; i++) c.members[i].s = Math.max(c.members[i].s, vs[i].skill);    // les enfants progressent
    }
    // nouveaux venus contents : ils deviennent habitués
    let added = 0;
    for (const vs of parties.values()) {
      const s = vs.reduce((a, v) => a + v.sat, 0) / vs.length; if (s < 0.62 || vs.every(v => v.runs === 0 && !v.lessonDone)) continue;
      if (Math.random() > 0.25 + (s - 0.62)) continue;
      const g = vs[0].grp, runs = {}; for (const v of vs) for (const k in (v.pc || {})) runs[k] = (runs[k] || 0) + v.pc[k];
      cl.push({
        id: G.loyalSeq++, last: g && g.fam ? g.last : vs[0].name.split(' ').slice(1).join(' '), fam: !!(g && g.fam), group: !!g,
        members: vs.map(v => ({ f: v.name.split(' ')[0], p: v.prof.id, s: v.skill, c: v.child ? 1 : 0, col: v.color })),
        loy: SS.clamp(0.25 + (s - 0.62) * 0.8, 0.1, 0.6), visits: 1, vs: 1, first: G.elapsed, lastDay: G.elapsed, sat: s, since: G.year, pass: 0, seg: vs[0].seg || 0,
        fav: Object.keys(runs).sort((a, b) => runs[b] - runs[a])[0] || null
      });
      added++;
    }
    // l'oubli : sans venue, la fidélité s'érode ; les clients perdus disparaissent
    let lost = 0;
    for (let i = cl.length - 1; i >= 0; i--) {
      const c = cl[i]; if (c.lastDay === G.elapsed) continue;
      c.loy = Math.max(0, c.loy - 0.0006);
      if (c.loy < 0.08 && G.elapsed - c.lastDay > 30) { cl.splice(i, 1); lost++; }
    }
    if (cl.length > CAP) { cl.sort((a, b) => b.loy - a.loy); cl.length = CAP; }
    const back = byClient.size;
    if (report) { report.loyal = { back, added, lost, unhappy }; if (back >= 10 && G.elapsed % 7 === 0) report.notes.push('🔁 ' + back + ' groupe(s) d\'habitués sont revenus aujourd\'hui.'); }
  };

  // ---------- nouvelle saison : forfaits saison pour les plus fidèles ----------
  L.newSeason = function (report) {
    const cl = list(), price = L.passPrice(); let sold = 0, people = 0;
    const fair = SS.Sim.today && SS.Sim.today.fair || 26;
    const accept = SS.clamp(1.6 - 0.6 * price / (fair * 11), 0.1, 1.2);
    for (const c of cl) {
      const want = c.loy >= 0.65 && c.vs >= 4 ? 0.55 * accept : c.loy >= 0.8 ? 0.35 * accept : 0;
      if (want && Math.random() < want) { c.pass = G.year; sold++; people += c.members.length; }
      c.vs = 0;
    }
    if (sold) {
      const rev = people * price * 0.8;             // les enfants paient moins
      SS.earn(Math.round(rev), 'Forfaits saison');
      if (report) report.notes.push('🎫 ' + sold + ' famille(s) et groupe(s) d\'habitués achètent le forfait saison (' + people + ' personnes) : ' + SS.eur(Math.round(rev)) + ' encaissés d\'avance.');
    }
  };

  // ---------- statistiques ----------
  L.stats = function () {
    const cl = list(); let loyal = 0, pass = 0, people = 0, visits = 0;
    for (const c of cl) { people += c.members.length; visits += c.visits; if (c.loy >= 0.5) loyal++; if (c.pass === G.year) pass++; }
    const today = SS.Sim.visitors.filter(v => v.clientId).length;
    return { clients: cl.length, people, loyal, pass, today, avgVisits: cl.length ? visits / cl.length : 0, top: cl.slice().sort((a, b) => b.visits - a.visits).slice(0, 8) };
  };
  L.label = c => (c.fam ? 'Famille ' + c.last : c.group ? 'Groupe de ' + c.members[0].f + ' ' + c.last : c.members[0].f + ' ' + c.last);
})();
