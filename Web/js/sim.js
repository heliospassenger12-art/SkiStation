// SkiStation Web — simulation : horloge unique, visiteurs (besoins + décisions), remontées physiques, services, secours, damage, journée
(function () {
  'use strict';
  const SS = window.SS, T = SS.Terrain, D = SS.Data, Net = SS.Net, W = SS.Weather, Mg = SS.Mgmt;
  const Sim = SS.Sim = { visitors: [], arrivals: [], rescues: [], groomers: [], present: 0, nextV: 1, lastTouch: 0, fb: {} };
  const M = SS.MOTION, WALK = 1.0, SKI_V = [5.5, 9, 12.5, 16];
  const COLORS = [0xe53935, 0x1e88e5, 0xfdd835, 0x43a047, 0xfb8c00, 0x8e24aa, 0x00acc1, 0xd81b60, 0x3949ab, 0x7cb342, 0xf4511e, 0x212121, 0xffffff, 0x6d4c41];
  let G;
  const tmp = {};

  // ---------------- journée : demande et arrivées ----------------
  Sim.demand = function () {
    G = SS.G;
    const at = SS.attract(), s = at.s;
    const fair = Sim.fairPrice(at);
    if (at.km <= 0) return { total: 0, day: 0, lodgers: 0, fair, at, why: G.summer ? 'Aucun sentier desservi : l\'été, seuls télésièges, télécabines, téléphériques et funiculaires fonctionnent.' : 'Aucune piste reliée au front de neige par une remontée.' };
    const base = (G.summer ? 30 + 650 * Math.pow(at.a, 1.1) : 40 + 1500 * Math.pow(at.a, 1.1)) * (G.mode === 'career' ? [1.15, 1, 0.88][G.level] : 1);
    const rep = 0.55 + G.reputation / 100 * 0.9;
    const cal = (SS.isWeekend(G.day) ? 1.35 : 1) * (G.summer ? 1.15 : SS.isHoliday(G.day) ? 1.3 : 1);
    const priceF = SS.clamp(1.6 - 0.6 * SS.passNow() / fair, 0.2, 1.35);
    const mk = 1 + (G.marketing.social > 0 ? 0.15 : 0) + (G.marketing.tv > 0 ? 0.35 : 0);
    const ev = G.event && G.event.day <= G.day ? D.EVENTS.find(e => e.id === G.event.id).boost : 1;
    const rental = s.rental > 0 || G.summer ? 1 : 0.74;
    const demand = base * rep * cal * W.def().demand * priceF * mk * ev * rental * (SS.AE && SS.AE.on() ? SS.AE.marketMul() : 1);
    const lodgers = Mg.allocateLodging(demand, cal, ev);
    const day = Math.round(Math.min(demand, s.access));
    const cap = 1500, total = Math.min(cap, day + lodgers);
    return { total, day: Math.max(0, total - lodgers), lodgers: Math.min(lodgers, total), fair, at, demand: Math.round(demand), access: s.access, lost: Math.max(0, Math.round(demand - s.access)) };
  };
  Sim.yieldMul = () => SS.researched('yield') && (SS.isWeekend(SS.G.day) || SS.G.summer || SS.isHoliday(SS.G.day)) ? 1.12 : 1;
  Sim.fairPrice = at => SS.G.summer ? 9 + at.km * 0.45 + at.s.vert * 0.002 + at.a * 8 : 22 + at.km * 1.4 + at.s.vert * 0.004 + at.a * 12;

  function pickProfile(comp) {
    let r = Math.random(), tot = 0; const ws = D.PROFILES.map(p => p.w * (comp && p.id === 'sportif' ? 2 : 1));
    for (const w of ws) tot += w; r *= tot;
    for (let i = 0; i < ws.length; i++) { r -= ws[i]; if (r <= 0) return D.PROFILES[i]; }
    return D.PROFILES[0];
  }
  function tri(a, c, b) { const u = Math.random(), f = (c - a) / (b - a); return u < f ? a + Math.sqrt(u * (b - a) * (c - a)) : b - Math.sqrt((1 - u) * (b - a) * (b - c)); }

  Sim.startDay = function () {
    G = SS.G;
    const d = Sim.today = Sim.demand(), s = d.at.s;
    Sim.arrivals = []; Sim.visitors = []; Sim.rescues = []; Sim.groomers = []; Sim.present = 0; Sim.fb = {}; Sim.grooming = false;
    Sim.lessons = {}; Sim.flags = {};
    G.satToday = []; G.visitorsToday = 0; G.incidentsToday = 0;
    for (const b of G.buildings) { b.visits = 0; b.revenue = 0; b.cogs = 0; b.occ = 0; b.queue = []; b.session = {}; b.busy = 0; }
    Mg.securePistes();
    for (const l of G.lifts) { l.queue = []; l.rides = 0; l.waitSum = 0; l.veh.forEach(v => v.length = 0); }
    for (const p of G.pistes) p.on = 0;
    const comp = G.event && G.event.day <= G.day && D.EVENTS.find(e => e.id === G.event.id).comp;
    const parkings = G.buildings.filter(b => D.building(b.type).type === 'parking' && b.open);
    const hasKids = s.kids > 0, hasSchool = s.school > 0;
    const mk = (home, lodger, pr, grp) => {
      pr = pr || pickProfile(comp); let r = Math.random(), skill = 0;
      for (let i = 0; i < 4; i++) { r -= pr.skill[i]; if (r <= 0) { skill = i; break; } }
      const arrive = grp && grp.arrive ? grp.arrive + Math.random() * 240 : lodger ? tri(8.4, 9.0, 10.2) * 3600 : tri(8.0, 9.2, 11.6) * 3600;
      const v = {
        id: Sim.nextV++, name: SS.pick(D.FIRST) + ' ' + (grp && grp.last || SS.pick(D.LAST)), prof: pr, skill, child: !!pr.child, color: SS.pick(COLORS),
        home, lodger, arriveT: arrive, leaveT: (lodger ? SS.range(15.6, 16.9) : Math.min(17.0, Math.max(arrive / 3600 + 3.5, SS.range(14.6, 17.0)))) * 3600,
        hunger: SS.range(0.08, 0.35), energy: SS.range(0.85, 1), sat: 0.62, spent: 0, runs: 0, km: 0,
        patience: SS.range(0.7, 1.4), spend: lodger ? [0.72, 1.05, 1.85][D.building(home.type).seg || 0] * SS.range(0.85, 1.2) : pr.spend * SS.range(0.7, 1.4),
        needRental: s.rental > 0 && Math.random() < 0.32, hasGear: false, hasPass: false,
        lesson: G.summer ? false : pr.child ? (hasKids && Math.random() < 0.8) : (hasSchool && (skill === 0 ? Math.random() < 0.5 : skill === 1 && Math.random() < 0.1)),
        state: 'pending', node: Net.hub.id, x: home.x, z: home.z, y: 0, hx: 0, hz: 1, lat: Math.random() * 6.28, decision: 'Arrive', reason: '', thoughts: [], did: {}
      };
      v.seg = v.spend < 0.85 ? 0 : v.spend < 1.4 ? 1 : 2;
      if (lodger && Math.random() < [0.5, 0.3, 0.06][D.building(home.type).seg || 0] && !(home.opts && home.opts.hb)) v.dined = true;      // séjours en autonomie (cuisine du chalet, de la résidence)
      if (lodger && home.opts && home.opts.shuttle) { v.x = SS.HUB.x + (Math.random() - 0.5) * 30; v.z = SS.HUB.z + 40 + Math.random() * 20; v.sat += 0.02; }
      if (v.lesson) v.lessonAt = (arrive < 9.4 * 3600 ? 9.75 : 14) * 3600;
      if (G.summer) {                                   // l'été : vététistes (rapides) et randonneurs (lents, tous sentiers)
        const bike = !v.child && Math.random() < (pr.id === 'sportif' ? 0.85 : pr.id === 'fetard' ? 0.6 : 0.3) && !(grp && grp.fam);
        v.mode = grp && grp.mode ? grp.mode : bike ? 'vtt' : 'rando'; if (grp && !grp.mode) grp.mode = v.mode;
        if (v.mode === 'rando') { v.sf = 0.2; v.skill = 3; v.needRental = false; } else { v.sf = 0.85; v.needRental = s.rental > 0 && Math.random() < 0.45; }
      }
      if (grp) { if (!grp.arrive) { grp.arrive = arrive; grp.leave = v.leaveT; } else v.leaveT = grp.leave + Math.random() * 300; v.grp = grp; grp.members.push(v); grp.maxDiff = Math.min(grp.maxDiff, Math.min(3, skill + 1)); }
      Sim.arrivals.push(v);
    };
    // familles et groupes d'amis : arrivent, skient et déjeunent ensemble. Renvoie le nombre de personnes créées.
    const PF = id => D.PROFILES.find(p => p.id === id);
    const party = (home, lodger, max) => {
      const pr = pickProfile(comp), fam = pr.id === 'famille' || pr.id === 'enfant';
      let n = Math.random() < (fam ? 0.15 : 0.45) ? 1 : 2 + Math.floor(Math.random() * (fam ? 3 : 4)); n = Math.min(n, max);
      if (n < 2) { mk(home, lodger, pr.id === 'enfant' ? PF('famille') : pr); return 1; }
      const last = SS.pick(D.LAST), grp = { id: Sim.nextV, fam, last: fam ? last : null, name: fam ? 'Famille ' + last : 'Groupe d\'amis', members: [], target: null, targetT: -1e9, eatT: -1e9, eatNode: -1, maxDiff: 3 };
      const adults = fam ? Math.min(n, Math.random() < 0.3 ? 1 : 2) : n;
      for (let i = 0; i < n; i++) mk(home, lodger, fam ? PF(i < adults ? 'famille' : 'enfant') : pr, grp);
      return n;
    };
    let k = 0;
    for (let i = 0; i < d.day;) {
      let home = SS.ARRIVAL;
      if (parkings.length) { home = parkings[k % parkings.length]; k++; }
      i += party(home, false, d.day - i);
    }
    let left = d.lodgers;
    for (const b of G.buildings) if (D.building(b.type).type === 'lodging' && b.open) for (let i = 0; i < (b.guests || 0) && left > 0;) { const n = party(b, true, Math.min((b.guests || 0) - i, left)); i += n; left -= n; }
    Sim.arrivals.sort((a, b) => a.arriveT - b.arriveT);
    Sim.expected = Sim.arrivals.length;
  };

  // ---------------- pensées / satisfaction ----------------
  function think(v, text, cat, delta) {
    if (delta) v.sat = SS.clamp(v.sat + delta, 0, 1);
    v.thoughts.unshift(text); if (v.thoughts.length > 5) v.thoughts.pop();
    if (cat) Sim.fb[cat] = (Sim.fb[cat] || 0) + 1;
  }
  Sim.think = think;

  // ---------------- déplacements à pied ----------------
  function walkTo(v, x, z, then, speed) { v.state = 'walk'; v.tx = x; v.tz = z; v.then = then; v.wspeed = speed || WALK; }
  function jitter(r) { return (Math.random() - 0.5) * 2 * r; }
  function nodeOf(v) { return Net.nodes[v.node] || Net.hub; }

  // ---------------- services ----------------
  function servicesAt(node, type, v) {
    let best = null, bs = -1e9;
    for (const b of node.services) {
      const def = D.building(b.type);
      if (def.type !== type || !Mg.isOpen(b)) continue;
      const ref = Mg.refPrice(b);
      if (b.price > ref * (1.2 + 0.5 * v.spend)) continue;
      if (type === 'food' && b.range === 2 && v.seg === 0 && Math.random() < 0.8) continue;      // les petits budgets évitent le premium
      if (def.tags && type === 'activity' && v.did[b.id]) continue;
      const sc = Mg.quality(b) * (0.6 + 0.4 * v.spend) + b.rating * 0.06 - b.queue.length / Math.max(4, SS.bcap(b)) - (b.price / (ref || 1)) * 0.2 - Math.abs(b.range - v.seg) * 0.12 + Math.random() * 0.3;
      if (sc > bs) { bs = sc; best = b; }
    }
    return best;
  }
  function goService(v, b, label, why) {
    v.svc = b; v.decision = label; v.reason = why || '';
    const def = D.building(b.type);
    walkTo(v, b.x + jitter(def.r * 0.5), b.z + def.r * 0.7 + jitter(3), 'service');
  }
  function tryEnter(v) {
    const b = v.svc, def = b && D.building(b.type);
    if (!b || G.buildings.indexOf(b) < 0 || !Mg.isOpen(b)) { v.svc = null; return backToNode(v); }
    if (b.occ < SS.bcap(b)) return enter(v, b);
    if (b.queue.length > SS.bcap(b) * 0.8 * v.patience + 4 && def.type !== 'ticket' && def.type !== 'rental') {
      if (!v.did[b.id]) think(v, 'Trop de monde : ' + def.name, 'Attente aux services', -0.04); v.did[b.id] = 1; v.svc = null; v.skipFood = G.time + 1800; return backToNode(v);
    }
    b.queue.push(v); v.state = 'queueS'; v.state0 = 'q'; v.waitStart = G.time;
  }
  function enter(v, b) {
    const def = D.building(b.type);
    b.occ++; v.state = 'inS'; v.svcWait = v.waitStart && v.state0 === 'q' ? G.time - v.waitStart : 0; v.state0 = '';
    v.until = G.time + (def.type === 'ticket' ? 2 : Mg.dur(b)) * 60 * SS.range(0.8, 1.2);
  }
  function finishService(v) {
    const b = v.svc, def = D.building(b.type); b.occ--; b.visits++;
    let price = b.price, cat = def.cat; const q = Mg.quality(b), ref = Mg.refPrice(b);
    if (def.type === 'ticket') {
      price = SS.passNow() * (v.child ? 0.6 : v.lodger ? 0.88 : 1) * Sim.yieldMul(); cat = 'Forfaits'; v.hasPass = true;
      const fair = Sim.today.fair; v.sat += SS.clamp((fair - SS.passNow()) / fair * 0.18, -0.2, 0.08);
      if (SS.passNow() > fair * 1.25) think(v, 'Le forfait est cher pour ce domaine', 'Prix du forfait');
    } else if (def.type === 'rental') { v.hasGear = true; cat = 'Location'; }
    else if (def.type === 'food') {
      cat = 'Restauration'; v.hunger = def.id === 'snack' ? Math.max(0, v.hunger - 0.65) : 0; v.energy = Math.min(1, v.energy + 0.25);
      think(v, q > 0.8 ? 'Excellent repas !' : q < 0.42 ? 'Repas décevant' : 'Repas correct', q < 0.42 ? 'Qualité des restaurants' : null, (q - 0.5) * 0.16 + (b.node !== Net.hub.id ? 0.03 : 0));
      if (v.decision === 'Dîner') v.dined = true;
    } else if (def.type === 'drink') { cat = 'Restauration'; v.energy = Math.min(1, v.energy + 0.3); v.hunger = Math.max(0, v.hunger - 0.15); v.sat += 0.04; }
    else if (def.type === 'shop') { cat = 'Boutiques'; v.sat += 0.02; }
    else if (def.type === 'activity') { cat = 'Activités'; think(v, 'Super moment : ' + def.name, null, (def.fun || 0.05) * (0.6 + q * 0.5)); v.energy -= 0.06; }
    v.did[b.id] = 1;
    if (price > 0) {
      SS.earn(price, cat); b.revenue += price; v.spent += price; v.sat -= Math.max(0, price / (ref || price) - 1) * 0.08;
      const cg = price * Mg.cogsRate(b); if (cg > 0) { SS.charge(cg, 'Achats'); b.cogs = (b.cogs || 0) + cg; }
    }
    if (Mg.isService(b)) Mg.rate(b, SS.clamp(q * 5.2 - (v.svcWait || 0) / 600 - Math.max(0, b.price / (ref || 1) - 1) * 3 + Math.random() * 0.6 - 0.3, 0, 5));
    while (b.queue.length && b.occ < SS.bcap(b)) { const q = b.queue.shift(); if (q.state === 'queueS') { const w = G.time - q.waitStart; if (w > 900) think(q, 'Longue attente : ' + def.name, 'Attente aux services', -Math.min(0.1, w / 36000)); enter(q, b); } }
    v.svc = null; backToNode(v);
  }
  function backToNode(v) { const n = nodeOf(v); walkTo(v, n.x + jitter(12), n.z + jitter(12), 'node'); }

  // ---------------- décision de journée : SKIER / MANGER / REPOS / RENTRER ----------------
  function goHome(v, why) {
    v.decision = 'Rentrer'; v.reason = why; v.leaving = true;
    const h = v.home; walkTo(v, h.x + jitter(8), h.z + jitter(8), 'exit');
  }
  function headToHub(v, why) {
    const hub = Net.hub.id, md = Math.min(3, v.skill + 1);
    v.decision = 'Rentrer'; v.reason = why; v.target = null;
    let path = G.time < SS.LIFT_CLOSE - 300 ? (Net.path(v.node, hub, md) || Net.path(v.node, hub, 3)) : null;
    if (!path) path = Net.path(v.node, hub, 3, true);
    if (!path) {
      think(v, 'Bloqué en altitude : retour à pied !', 'Bloqués en altitude', -0.3);
      v.node = hub; return walkTo(v, SS.HUB.x + jitter(15), SS.HUB.z + jitter(15), 'node', WALK * 1.6);
    }
    v.path = path; takeEdge(v, v.path.shift());
  }
  function findFoodNode(v, remain) {
    const md = Math.min(3, v.skill + 1), hub = Net.hub.id, from = Net.from(v.node, md);
    let best = null, bt = Infinity;
    for (const n of Net.nodes) {
      if (n.id === v.node || from.dist[n.id] === Infinity) continue;
      if (!n.services.some(b => D.building(b.type).type === 'food' && b.open)) continue;
      const back = n.hub ? 0 : Net.time(n.id, hub, md);
      const t = from.dist[n.id] + back;
      if (t + 2400 < remain && from.dist[n.id] < bt) { bt = from.dist[n.id]; best = n; }
    }
    return best;
  }

  function decide(v) {
    G = SS.G;
    if (++v.dn > 5) { v.state = 'rest'; v.until = G.time + 300; return; }
    const n = nodeOf(v), hub = Net.hub.id, now = G.time, atHub = n.id === hub, md = Math.min(3, v.skill + 1);
    v.path = null; v.target = null;
    if (v.leaving) return atHub ? goHome(v, v.reason) : headToHub(v, v.reason);
    // forfait puis matériel
    if (!v.hasPass) { const b = servicesAt(Net.hub, 'ticket', v) || G.buildings.find(x => x.type === 'ticket'); if (b) return goService(v, b, 'Achète son forfait'); return goHome(v, 'Pas de billetterie'); }
    if (v.needRental && !v.hasGear) {
      const b = servicesAt(Net.hub, 'rental', v);
      if (b) return goService(v, b, 'Loue son matériel');
      think(v, 'Location trop chère ou fermée', 'Location', -0.25); return goHome(v, 'Pas de matériel');
    }
    // cours de ski
    if (v.lesson && !v.lessonDone && atHub) {
      if (now > v.lessonAt + 1500) { if (v.lessonAt < 12 * 3600 && now < 14.2 * 3600) v.lessonAt = 14 * 3600; else { v.lesson = false; } }
      if (v.lesson && now > v.lessonAt - 3 * 3600) {
        const b = G.buildings.find(x => x.open && D.building(x.type).type === (v.child ? 'kids' : 'school'));
        if (b && now >= v.lessonAt - 1500) return joinLesson(v, b);
        if (b && v.lessonAt - now < 4500) { v.state = 'rest'; v.until = v.lessonAt - 1200; v.decision = 'Attend son cours'; v.reason = 'Cours à ' + SS.hhmm(v.lessonAt); return; }
        if (!b) v.lesson = false;
      }
    }
    const inNight = v.night && SS.nightActive() && now < SS.NIGHT_CLOSE - 600;
    if (v.night && !inNight && now < SS.NIGHT_OPEN && now >= SS.LIFT_CLOSE - 600) {
      if (!atHub) return headToHub(v, 'Pause avant la nocturne');
      const b = v.hunger > 0.45 ? servicesAt(n, 'food', v) : !v.did.bar ? servicesAt(n, 'drink', v) : null;
      if (b) { if (D.building(b.type).type === 'drink') v.did.bar = 1; return goService(v, b, 'Attend la nocturne', D.building(b.type).name); }
      v.state = 'rest'; v.until = SS.NIGHT_OPEN + Math.random() * 300; v.decision = 'Attend la nocturne'; v.reason = 'Ouverture à 18:30'; return;
    }
    const liftsOpen = (now >= SS.LIFT_OPEN && now < SS.LIFT_CLOSE - 600) || inNight;
    const tHome = atHub ? 0 : Math.min(Net.time(v.node, hub, md), 5400);
    const remain = v.leaveT - now - tHome - 600;
    v.remain = remain;
    if ((now >= SS.LIFT_CLOSE - 600 && !inNight) || remain <= 0) {
      if (v.night && now >= SS.NIGHT_OPEN) v.night = false;
      if (!atHub) return headToHub(v, remain <= 0 ? 'Heure de départ' : 'Fermeture des remontées');
      return apresSki(v);
    }
    // MANGER
    const lunch = now > 11.5 * 3600 && now < 14 * 3600;
    const gEat = v.grp && now - v.grp.eatT < 1500 && v.grp.eatNode === n.id && v.hunger > 0.28;
    if ((v.hunger > 0.78 || (lunch && v.hunger > 0.5) || gEat) && !(v.skipFood > now)) {
      const b = servicesAt(n, 'food', v);
      if (b) { if (v.grp && !gEat) { v.grp.eatT = now; v.grp.eatNode = n.id; } return goService(v, b, 'Manger', gEat ? 'Déjeune avec ' + v.grp.name.toLowerCase() : 'Faim ' + Math.round(v.hunger * 100) + ' %'); }
      const fn = findFoodNode(v, remain);
      if (fn) { v.path = Net.path(v.node, fn.id, md); if (v.path && v.path.length) { v.decision = 'Manger'; v.reason = 'Restaurant le plus proche via le domaine'; return takeEdge(v, v.path.shift()); } }
      if (!v.noFood) { v.noFood = true; think(v, 'Nulle part où manger !', 'Manque de restauration', -0.12); }
      if (v.hunger > 0.97 && atHub) return goHome(v, 'Trop faim, rien à manger');
      v.skipFood = now + 2400;
    }
    // REPOS
    if (v.energy < 0.24) {
      const b = servicesAt(n, 'drink', v) || (v.hunger > 0.3 ? servicesAt(n, 'food', v) : null);
      if (b) return goService(v, b, 'Repos', 'Énergie ' + Math.round(v.energy * 100) + ' %');
      if (v.energy < 0.1 && remain < 5400) return atHub ? apresSki(v) : headToHub(v, 'Épuisé');
      v.state = 'rest'; v.until = now + 600; v.restGain = 0.2; v.decision = 'Repos'; v.reason = 'Pause de 10 min, énergie ' + Math.round(v.energy * 100) + ' %'; return;
    }
    if (!liftsOpen) { v.state = 'rest'; v.until = Math.max(now + 300, SS.LIFT_OPEN + Math.random() * 120); v.decision = 'Attend l\'ouverture'; v.reason = 'Remontées ouvertes à 09:00'; return; }
    // activité sur place (snowpark, luge…)
    if (Math.random() < 0.12) { const b = activityAt(n, v); if (b) return goService(v, b, 'Activité', D.building(b.type).name); }
    // SKIER : choisir une piste cible, vérifier le budget temps sur le trajet GPS complet + retour
    const from = Net.from(v.node, md), grp = v.grp;
    let best = null, bs = -1e9, together = false;
    if (grp && grp.target && now - grp.targetT < 1800 && G.pistes.indexOf(grp.target) >= 0) {
      const p = grp.target, t1 = from.dist[p.na.id], t2 = p.nb.id === hub ? 0 : Net.time(p.nb.id, hub, md);
      if (SS.pisteOpen(p) && p.diff <= md && p.edges.length && t1 < 2400 && t2 < Infinity && t1 + p.len / (SKI_V[v.skill] * M * (v.sf || 1)) + t2 <= remain) { best = p; together = true; }
    }
    if (!best) for (const p of G.pistes) {
      if (!SS.pisteOpen(p) || p.diff > md || !p.edges.length) continue;
      const t1 = from.dist[p.na.id]; if (t1 === Infinity) continue;
      const t2 = p.nb.id === hub ? 0 : Net.time(p.nb.id, hub, md); if (t2 === Infinity) continue;
      const total = t1 + p.len / (SKI_V[v.skill] * M * (v.sf || 1)) + t2;
      if (total > remain) continue;
      const dd = p.diff - v.skill;
      let sc = (dd === 0 ? 1 : dd === -1 ? 0.7 : dd === 1 ? 0.35 : 0.3) + W.quality(p, v.skill) * 0.6 - Math.min(1, p.on / (p.len * p.width / 500)) * 0.5 - t1 / 3600 * 0.7 + Math.random() * 0.55;
      if (p === v.lastPiste) sc -= 0.25;
      if (grp && p.diff > grp.maxDiff) sc -= 0.9;                // on choisit pour le moins à l'aise du groupe
      if (v.prof.id === 'contemplatif' || v.prof.id === 'sportif') sc += (p.topAlt - 1650) / 2500 * 0.3;
      if (sc > bs) { bs = sc; best = p; }
    }
    if (!best) {
      if (!atHub) return headToHub(v, 'Plus assez de temps pour une descente');
      // remontées momentanément à l'arrêt (panne, grève, vent) : on patiente au lieu de repartir
      const stopped = G.lifts.find(l => l.open && SS.liftSeason(l) && l.na === Net.hub && (now < l.brokenUntil || l.windStop));
      if (stopped && now < SS.LIFT_CLOSE - 5400 && (v.waits = (v.waits || 0) + 1) <= 10) {
        if (v.waits === 1) think(v, stopped.name + ' à l\'arrêt, j\'attends', 'Pannes', -0.05);
        v.state = 'rest'; v.until = now + 900; v.decision = 'Attend la remise en service'; v.reason = stopped.name + ' à l\'arrêt'; return;
      }
      if (now < SS.LIFT_CLOSE - 3600 && !v.runs) { think(v, 'Aucune piste accessible pour mon niveau', 'Pistes inadaptées au niveau', -0.3); return goHome(v, 'Rien à skier'); }
      return apresSki(v);
    }
    if (grp && !together) { grp.target = best; grp.targetT = now; }
    if (together) v.sat = Math.min(1, v.sat + 0.004);
    v.target = best; v.decision = 'Skier'; v.reason = (together ? 'Avec ' + grp.name.toLowerCase() + ' : ' : 'Objectif : ') + best.name + ' (' + D.DIFF[best.diff].name.toLowerCase() + ')';
    v.path = Net.path(v.node, best.na.id, md) || [];
    advance(v);
  }
  Sim.decide = decide;
  function activityAt(n, v) {
    let pick = null;
    for (const b of n.services) { const def = D.building(b.type); if (def.type === 'activity' && b.open && !v.did[b.id] && b.price <= def.price * (1.2 + 0.5 * v.spend + (def.price === 0 ? 1 : 0)) && (def.tags === v.prof.tags || Math.random() < 0.3)) pick = b; }
    return pick;
  }
  // une piste éclairée dont le départ est desservi depuis le front de neige par une remontée nocturne
  function nightReachable() {
    if (!SS.nightOn()) return false;
    return G.pistes.some(p => p.lit && p.open && !p.snowClosed && G.lifts.some(l => l.night && l.open && l.na === Net.hub && l.nb === p.na));
  }
  Sim.nightReachable = nightReachable;
  function apresSki(v) {
    if (!v.nightDone && G.time < SS.NIGHT_CLOSE - 3600 && nightReachable() && v.energy > 0.3 && v.skill > 0) {
      v.nightDone = true;
      const want = (v.lodger ? 0.35 : 0.12) + (v.prof.id === 'fetard' || v.prof.id === 'sportif' ? 0.25 : 0) - Math.max(0, G.nightPrice - 14) * 0.02;
      if (Math.random() < want) {
        v.night = true; v.leaveT = SS.NIGHT_CLOSE - 300 + Math.random() * 300;
        const price = G.nightPrice * (v.lodger ? 0.85 : 1); SS.earn(price, 'Forfaits nocturnes'); v.spent += price;
        think(v, 'Je prolonge avec le ski nocturne !', null, 0.03); v.dn = 0; return decide(v);
      }
    }
    if (v.lodger && !v.dined && !(v.home.opts && v.home.opts.hb) && !v.noDinner) {          // dîner au restaurant pour les résidents sans demi-pension
      const evening = Net.hub.services.filter(b => D.building(b.type).type === 'food' && b.open && b.hours === 'eve');
      if (!evening.length) v.noDinner = true;
      else if (G.time < 18.4 * 3600) { if (v.apres >= 1) { v.state = 'rest'; v.until = 18.4 * 3600 + Math.random() * 1500; v.decision = 'Attend le dîner'; v.reason = 'Les restaurants servent le soir à partir de 18 h 30'; return; } }
      else if (G.time < 21.5 * 3600) { const b = servicesAt(Net.hub, 'food', v); if (b) return goService(v, b, 'Dîner', D.building(b.type).name); v.noDinner = true; }
    }
    const hubN = Net.hub, n = v.apres = (v.apres || 0) + 1;
    if (G.time < (v.nightDone && G.time > SS.NIGHT_OPEN ? 21.5 : 19.5) * 3600 && n <= (v.lodger ? 3 : 2) && Math.random() < (v.prof.id === 'fetard' ? 0.85 : 0.55)) {
      const b = activityAt(hubN, v) || (v.did.bar ? null : servicesAt(hubN, 'drink', v)) || (Math.random() < 0.4 && !v.did.shop ? servicesAt(hubN, 'shop', v) : null) || (v.hunger > 0.6 ? servicesAt(hubN, 'food', v) : null);
      if (b) { const t = D.building(b.type).type; if (t === 'drink') v.did.bar = 1; if (t === 'shop') v.did.shop = 1; return goService(v, b, 'Après-ski', D.building(b.type).name); }
    }
    goHome(v, 'Journée terminée');
  }

  // suit le plan GPS : étape suivante, ou la piste cible une fois à son départ
  function advance(v) {
    const n = nodeOf(v);
    if (v.path && v.path.length) return takeEdge(v, v.path.shift());
    if (v.target) {
      for (const e of n.out) if (e.kind === 'piste' && e.piste === v.target) return takeEdge(v, e);
      v.lastPiste = v.target; v.target = null;
    }
    decide(v);
  }
  function takeEdge(v, e) {
    if (!e || Net.edges[e.id] !== e) { v.path = null; v.target = null; return decide(v); }
    const forced = v.decision === 'Rentrer';
    if (e.kind === 'lift') {
      const l = e.lift;
      if (!SS.liftBoarding(l)) { Net.touch(); v.path = null; v.target = null; return decide(v); }
      const wait = l.queue.length / Math.max(1, l.capacity) * 3600;
      if (wait > 1500 * v.patience && !forced && !v.balked) { v.balked = G.time; think(v, 'File interminable au ' + l.name, 'Files aux remontées', -0.06); }
      l.queue.push(v); v.state = 'queueL'; v.lift = l; v.waitStart = G.time;
    } else {
      const p = e.piste;
      if (!SS.pisteOpen(p) && !forced) { Net.touch(); v.path = null; v.target = null; return decide(v); }
      v.state = 'ski'; v.edge = e; v.s = e.s0; p.on++;
      const dd = p.diff - v.skill;
      const risk = Mg.riskFactor(p) * (v.mode === 'rando' ? 0.1 : 1) * 0.0024 * (1 + 2.2 * Math.max(0, dd)) * (1 + 2.5 * p.ice) * (1 + Math.min(1.5, p.on / (p.len * p.width / 400))) * (SS.researched('padding') ? 0.7 : 1) * (p.groomed < 0.3 ? 1.3 : 1) * (G.casual ? 0.5 : 1);
      v.fallAt = Math.random() < risk * e.len / 1000 ? SS.range(e.s0, e.s1) : -1;
    }
  }
  function arriveNode(v, node) { v.node = node.id; v.x = node.x + jitter(8); v.z = node.z + jitter(8); advance(v); }

  // ---------------- cours de ski ----------------
  function joinLesson(v, b) {
    const def = D.building(b.type), key = v.lessonAt;
    b.session = b.session || {}; const n = b.session[key] || 0;
    const cap = def.type === 'kids' ? SS.bcap(b) : b.staff * 8;
    if (n >= cap) { think(v, 'Cours complet', 'Cours de ski complets', -0.12); v.lesson = false; return decide(v); }
    let price = b.price, seats = 1; v.lessonKind = 'collectif';
    if (def.type === 'school' && v.spend > 1.25 && n + 8 <= cap) { price = Math.round(b.price * 2.4); seats = 8; v.lessonKind = 'individuel'; }
    b.session[key] = n + seats;
    v.svc = b; v.state = 'walk'; v.decision = 'Cours de ski'; v.reason = 'Cours ' + v.lessonKind + ' à ' + SS.hhmm(v.lessonAt);
    v.lessonPrice = price; walkTo(v, b.x + jitter(10), b.z + jitter(10), 'lesson');
  }
  function endLesson(v) {
    const b = v.svc; v.lessonDone = true; v.svc = null;
    SS.earn(v.lessonPrice, 'Cours de ski'); if (b) { b.revenue += v.lessonPrice; b.visits++; } v.spent += v.lessonPrice;
    let medal;
    if (v.child) medal = D.MEDALS[Math.min(5, Math.floor(Math.random() * 3.2 + Math.random() * 3))];
    else if (v.skill === 0 && Math.random() < 0.45) { v.skill = 1; medal = '1re Étoile'; } else medal = v.skill === 0 ? 'Flocon' : v.skill === 1 ? 'Étoile de Bronze' : Math.random() < 0.5 ? 'Flèche' : 'Chamois';
    G.medals[medal] = (G.medals[medal] || 0) + 1;
    think(v, 'Médaille obtenue : ' + medal + ' !', null, 0.13); v.energy -= 0.2; v.hunger += 0.1;
    backToNode(v);
  }

  // ---------------- accidents et secours ----------------
  function fall(v) {
    const r = Math.random(), p = v.edge.piste;
    v.fallAt = -1;
    if (r < 0.72) { v.state = 'down'; v.until = G.time + 150; think(v, 'Chute sans gravité sur ' + p.name, null, -0.05); return; }
    const severe = r > 0.95;
    v.state = 'hurt'; G.incidentsToday++; G.safety.incidents++; if (severe) G.safety.severe++; v.decision = 'Blessé'; v.reason = 'Attend les secours';
    let post = null, bd = 1e9;
    const heli = severe ? G.buildings.find(b => b.type === 'heliport' && b.open) : null;
    for (const b of G.buildings) if (b.type === 'rescue' && b.open && (b.busy || 0) < SS.bcap(b)) { const d = Math.hypot(b.x - v.x, b.z - v.z); if (d < bd) { bd = d; post = b; } }
    const src = heli || post;
    if (!src) {
      think(v, 'Blessé, et aucun secours sur le domaine !', 'Sécurité', -0.5); G.reputation = Math.max(0, G.reputation - (severe ? 2 : 0.8));
      SS.log('🚑 Accident sur ' + p.name + ' sans poste de secours : évacuation très lente.', 'bad');
      Sim.rescues.push({ kind: 'patrol', x: SS.HUB.x, z: SS.HUB.z, y: 0, v, phase: 'go', speed: 2.2, slow: true });
    } else {
      if (post && !heli) post.busy = (post.busy || 0) + 1;
      if (severe && !heli) { G.reputation = Math.max(0, G.reputation - 0.8); SS.log('🚑 Accident grave sur ' + p.name + ' : sans héliport, l\'évacuation en traîneau est longue.', 'bad'); }
      else if (severe) SS.log('🚁 Accident grave sur ' + p.name + ' : l\'hélicoptère décolle.', 'warn');
      Sim.rescues.push({ kind: heli ? 'heli' : 'patrol', x: src.x, z: src.z, y: src.y, v, phase: 'go', speed: heli ? 14 : (severe ? 2.6 : 4.5) * Mg.patrolSpeed(), post: heli ? null : post, src, t0: G.time });
    }
  }
  function updateRescues(dt) {
    for (let i = Sim.rescues.length - 1; i >= 0; i--) {
      const r = Sim.rescues[i], v = r.v;
      const tx = r.phase === 'go' ? v.x : r.kind === 'heli' ? r.src.x : SS.HUB.x, tz = r.phase === 'go' ? v.z : r.kind === 'heli' ? r.src.z : SS.HUB.z;
      const d = Math.hypot(tx - r.x, tz - r.z), stp = r.speed * dt;
      if (d <= stp + 4) {
        if (r.phase === 'go') {
          r.phase = 'back'; const w = G.time - (r.t0 || G.time - 3000); G.safety.respSum += w; G.safety.respN++;
          think(v, w < 900 ? 'Secours arrivés très vite, bravo aux pisteurs' : 'Secours trop lents…', w < 900 ? null : 'Sécurité', w < 900 ? -0.1 : -0.3);
        } else {
          if (r.post) r.post.busy = Math.max(0, r.post.busy - 1);
          v.p_on_done || (v.edge.piste.on--, v.p_on_done = true);
          finalize(v); Sim.rescues.splice(i, 1);
        }
        continue;
      }
      r.x += (tx - r.x) / d * stp; r.z += (tz - r.z) / d * stp;
      r.y = T.getH(r.x, r.z) + (r.kind === 'heli' ? 90 * Math.min(1, d / 200) + 6 : 0);
      if (r.phase === 'back') { v.x = r.x + 2; v.z = r.z; v.y = r.y - (r.kind === 'heli' ? 4 : 0); v.carried = true; }
    }
  }

  // ---------------- remontées : véhicules physiques, embarquement réel ----------------
  function updateLifts(dt) {
    const hf = SS.researched('handsfree') ? 1.12 : 1;
    for (const l of G.lifts) {
      const def = D.lift(l.type), boarding = SS.liftBoarding(l);
      const broken = G.time < l.brokenUntil;
      let riders = 0; for (const vh of l.veh) riders += vh.length;
      l.riders = riders;
      // panne aléatoire selon l'état et le niveau d'entretien
      if (boarding && Math.random() < dt / 3600 * 0.012 * (1.7 - l.cond) * [2.2, 1, 0.45][G.maint]) {
        l.brokenUntil = G.time + SS.range(900, 3000); Net.touch();
        SS.log('🔧 Panne sur ' + l.name + ' : arrêt d\'environ ' + Math.round((l.brokenUntil - G.time) / 60) + ' min.', 'warn');
        for (const vh of l.veh) for (const v of vh) think(v, 'Coincé sur ' + l.name + ' en panne', 'Pannes', -0.08);
      }
      l.moving = !broken && (boarding || riders > 0 || (SS.liftRunning(l) && G.time < SS.LIFT_CLOSE));
      if (!boarding && l.queue.length && G.time >= SS.LIFT_OPEN && (!broken || G.time - l.queue[0].waitStart > 2400)) {
        const q = l.queue; l.queue = [];
        for (const v of q) { if (l.windStop || broken) think(v, l.name + ' arrêté', 'Pannes', -0.04); v.dn = 0; decide(v); }
      }
      if (!l.moving) continue;
      const adv = def.speed * M * hf * dt, L2 = 2 * l.L, old = l.phase;
      l.phase = (old + adv) % L2;
      for (let i = 0; i < l.n; i++) {
        const s0 = (old + i * l.D) % L2, s1 = s0 + adv, vh = l.veh[i];
        if (s0 < l.L && s1 >= l.L && vh.length) {                       // gare d'arrivée
          const list = vh.slice(); vh.length = 0;
          for (const v of list) { v.lift = null; v.dn = 0; l.rides++; arriveNode(v, l.nb); }
        }
        if (s1 >= L2 && boarding && l.queue.length) {                   // gare de départ
          for (let k = 0; k < def.seats && l.queue.length; k++) {
            const v = l.queue.shift(), w = G.time - v.waitStart;
            l.waitSum += w;
            if (w > 720) { const pen = Math.min(0.1, (w - 720) / 3600 * 0.25, 0.3 - (v.qPen || 0)); v.qPen = (v.qPen || 0) + Math.max(0, pen); think(v, Math.round(w / 60) + ' min de queue au ' + l.name, 'Files aux remontées', -Math.max(0, pen)); }
            v.sat += (def.comfort - 0.5) * 0.012;
            v.state = 'ride'; v.veh = i; vh.push(v);
          }
        }
      }
    }
  }
  Sim.liftWait = l => l.queue.length / Math.max(1, l.capacity) * 60;       // minutes estimées

  // ---------------- damage en soirée ----------------
  function startGrooming() {
    Sim.grooming = true; Sim.groomers = [];
    if (G.summer) return;
    const winch = SS.researched('winch');
    Sim.groomList = Mg.groomPlan(); Sim.groomPlanned = Sim.groomList.slice();
    for (const b of G.buildings) if (b.type === 'garage' && b.open) for (let i = 0; i < SS.bcap(b); i++) Sim.groomers.push({ x: b.x + i * 6, z: b.z, y: b.y, garage: b, piste: null, s: 0, state: 'idle', hx: 0, hz: 1 });
    if (!Sim.groomers.length && G.pistes.length) SS.log('Aucune dameuse : les pistes ne seront pas préparées cette nuit.', 'warn');
  }
  function updateGroomers(dt) {
    const sp = Mg.groomSpeed();
    for (const g of Sim.groomers) {
      if (g.state === 'idle') { g.piste = null;
        const wait = p => p.lit && SS.nightOn() && G.time < SS.NIGHT_CLOSE + 600;
        const i = Sim.groomList.findIndex(p => !wait(p));
        if (i < 0) { if (!Sim.groomList.length) g.state = 'home'; continue; }
        g.piste = Sim.groomList.splice(i, 1)[0]; g.state = 'travel'; g.s = g.piste.len;
      }
      if (g.state === 'travel' || g.state === 'home') {
        const tgt = g.state === 'home' ? g.garage : g.piste.pts[g.piste.pts.length - 1];
        const d = Math.hypot(tgt.x - g.x, tgt.z - g.z), stp = 5 * dt;
        if (d < stp + 2) { if (g.state === 'travel') g.state = 'groom'; else g.state = 'parked'; continue; }
        g.hx = (tgt.x - g.x) / d; g.hz = (tgt.z - g.z) / d; g.x += g.hx * stp; g.z += g.hz * stp; g.y = T.getH(g.x, g.z);
      } else if (g.state === 'groom') {
        g.s -= sp * dt; const p = g.piste;
        if (g.s <= 0) { p.groomed = 1; p.ice *= 0.15; p.powder *= 0.5; p.groomedTonight = true; SS.charge(Math.round(p.len * 0.4), 'Damage'); g.state = 'idle'; continue; }
        SS.pisteAt(p, g.s, tmp); g.x = tmp.x; g.z = tmp.z; g.y = T.getH(g.x, g.z); const d = Math.hypot(tmp.dx, tmp.dz) || 1; g.hx = -tmp.dx / d; g.hz = -tmp.dz / d;
      }
    }
  }

  // ---------------- un visiteur ----------------
  function finalize(v) {
    v.state = 'gone'; Sim.present--;
    if (v.lodger && v.home && v.home.price) {
      const b = v.home, def = D.building(b.type), q = Mg.quality(b);
      SS.earn(b.price, 'Hébergement'); b.revenue += b.price; b.visits++;
      if (b.opts.hb && !v.dined) { const dp = Mg.dinnerPrice(b); SS.earn(dp, 'Restauration'); b.revenue += dp; SS.charge(dp * 0.38, 'Achats'); b.cogs = (b.cogs || 0) + dp * 0.38; v.dined = true; v.sat += 0.02; }
      if (!v.dined && !G.summer) { v.sat -= 0.03; Sim.fb['Rien pour dîner'] = (Sim.fb['Rien pour dîner'] || 0) + 1; }
      v.sat += (q - 0.5) * 0.14 - Math.max(0, b.price / def.price - 1) * 0.1;
      Mg.rate(b, SS.clamp(v.sat * 4 + q * 1.6 - Math.max(0, b.price / def.price - 1) * 2, 0, 5));
    }
    if (v.runs === 0 && !v.lessonDone && !v.carried) v.sat -= 0.15;
    v.sat = SS.clamp(v.sat, 0, 1); G.satToday.push(v.sat);
  }
  function updateVisitor(v, dt) {
    v.dn = 0;
    v.hunger += dt / 16200; v.energy -= dt / 90000;
    if (v.hunger > 0.9) v.sat -= dt * 0.000012;
    switch (v.state) {
      case 'walk': {
        const dx = v.tx - v.x, dz = v.tz - v.z, d = Math.hypot(dx, dz), stp = v.wspeed * dt;
        if (d <= stp) {
          v.x = v.tx; v.z = v.tz;
          if (v.then === 'service') tryEnter(v);
          else if (v.then === 'exit') finalize(v);
          else if (v.then === 'lesson') { v.state = 'lesson'; v.until = Math.max(G.time + 3600, v.lessonAt + 7200); v.bx = v.x; v.bz = v.z; }
          else decide(v);
        } else {
          v.hx = dx / d; v.hz = dz / d; let nx = v.x + v.hx * stp, nz = v.z + v.hz * stp;
          if (d > 6) {                                   // glisse autour des emprises au lieu de les traverser
            const obs = Sim.obstacles;
            for (let i = 0; i < obs.length; i++) {
              const o = obs[i], ox = nx - o.x, oz = nz - o.z, od = Math.hypot(ox, oz);
              if (od >= o.r || od < 0.01 || Math.hypot(v.tx - o.x, v.tz - o.z) < o.r + 3) continue;
              if (Math.hypot(v.x - o.x, v.z - o.z) < o.r - 0.5 || (v.x - o.x) * v.hx + (v.z - o.z) * v.hz > 0) continue;   // on en sort ou on s'en éloigne
              // tourne autour du centre dans le sens qui rapproche de la cible (stable d'un pas à l'autre)
              const px = v.x - o.x, pz = v.z - o.z, pd = Math.hypot(px, pz) || 1, tx = v.tx - o.x, tz = v.tz - o.z;
              const ccw = px * tz - pz * tx > 0 ? 1 : -1, R = Math.max(o.r, pd);
              nx = o.x + px / pd * R + (-pz / pd) * ccw * stp; nz = o.z + pz / pd * R + (px / pd) * ccw * stp;
            }
          }
          v.x = nx; v.z = nz;
        }
        v.y = T.getH(v.x, v.z); break;
      }
      case 'ski': {
        const e = v.edge, p = e.piste; SS.pisteAt(p, v.s, tmp);
        const sp = (v.sf || 1) * SKI_V[v.skill] * SS.clamp(0.7 + tmp.slope * 1.3, 0.6, 1.35) * (1 - 0.25 * p.slush) * (1 - 0.12 * p.powder) * M;
        v.s += sp * dt; v.lat += sp * dt * 0.035;
        const d = Math.hypot(tmp.dx, tmp.dz) || 1, amp = (p.width / 2 - 3) * (0.5 + 0.4 * Math.sin(v.id));
        const off = Math.sin(v.lat) * amp, c = Math.cos(v.lat) * 0.6;
        v.hx = tmp.dx / d - (-tmp.dz / d) * -c; v.hz = tmp.dz / d - (tmp.dx / d) * -c;
        v.x = tmp.x + (-tmp.dz / d) * off; v.z = tmp.z + (tmp.dx / d) * off; v.y = T.getH(v.x, v.z);
        if (v.fallAt > 0 && v.s >= v.fallAt) { fall(v); break; }
        if (v.s >= e.s1) {
          p.on--; p.passes++; p.groomed = Math.max(0, p.groomed - 0.25 / (p.len / 1000 * 320)); p.powder = Math.max(0, p.powder - 0.0008);
          const dd = p.diff - v.skill, q = W.quality(p, v.skill), crowd = Math.min(1, p.on / (p.len * p.width / 450));
          const fun = (dd === 0 ? 1 : dd === -1 ? 0.6 : dd === 1 ? 0.2 : dd < -1 ? 0.3 : -0.6) * (0.4 + 0.6 * q) - crowd * 0.5;
          v.sat += fun * e.len / 1000 * 0.032;
          if (q < 0.3 && Math.random() < 0.3) think(v, W.state(p) + ' sur ' + p.name, 'Qualité de neige', -0.03);
          if (crowd > 0.8 && Math.random() < 0.2) think(v, 'Trop de monde sur ' + p.name, 'Pistes surchargées', -0.02);
          if (dd > 1) think(v, p.name + ' est trop difficile pour moi', 'Pistes inadaptées au niveau', -0.06);
          v.energy -= e.len / 1000 * (0.03 + 0.016 * p.diff + (dd > 0 ? 0.03 : 0)); v.km += e.len / 1000;
          if (e.to === p.nb) v.runs++;
          arriveNode(v, e.to);
        }
        break;
      }
      case 'queueS': if (G.time - v.waitStart > 1500 * v.patience && D.building(v.svc.type).type !== 'ticket') {
        const b = v.svc, i = b.queue.indexOf(v); if (i >= 0) b.queue.splice(i, 1);
        if (!v.did[b.id]) think(v, 'J\'abandonne la file : ' + D.building(b.type).name, 'Attente aux services', -0.05); v.did[b.id] = 1; v.svc = null; v.skipFood = G.time + 1800; backToNode(v);
      } break;
      case 'inS': if (G.time >= v.until) finishService(v); break;
      case 'rest': if (G.time >= v.until) { if (v.restGain) { v.energy = Math.min(1, v.energy + v.restGain); v.restGain = 0; } decide(v); } break;
      case 'lesson': if (G.time >= v.until) endLesson(v); break;
      case 'down': if (G.time >= v.until) v.state = 'ski'; break;
    }
  }

  function rebuildObstacles() {
    const o = Sim.obstacles = [];
    for (const b of SS.G.buildings) { const def = D.building(b.type); if (def.look === 'parking' || def.look === 'bus' || def.look === 'garden' || def.look === 'snowpark' || def.look === 'luge' || def.look === 'rink') continue; o.push({ x: b.x, z: b.z, r: Math.min(def.r * 0.62, 16) }); }
    for (const d of SS.DECOR) o.push({ x: d.x, z: d.z, r: d.r * 0.75 });
  }
  Sim.obstacles = [];
  SS.on('network', rebuildObstacles);

  // ---------------- boucle principale ----------------
  function step(dt) {
    G.time += dt;
    while (Sim.arrivals.length && Sim.arrivals[0].arriveT <= G.time) {
      const v = Sim.arrivals.shift(); v.state = 'walk'; v.y = T.getH(v.x, v.z); v.dn = 0;
      if (v.lodger && !v.hasPass) { v.hasPass = true; const pr = SS.passNow() * (v.child ? 0.6 : 0.88) * Sim.yieldMul(); SS.earn(pr, 'Forfaits'); v.spent += pr; }
      Sim.visitors.push(v); Sim.present++; G.visitorsToday++; G.totalVisitors++; decide(v);
    }
    if (G.time - Sim.lastTouch > 600 || G.time < Sim.lastTouch) {
      Sim.lastTouch = G.time; Net.touch(); W.updateWind(); W.dayTick();
      if (Sim.present > 0 && Math.random() < 0.5) SS.emit('pulse');
    }
    if (!Sim.flags.open && G.time >= SS.LIFT_OPEN) { Sim.flags.open = true; Net.touch(); if (G.lifts.length) SS.emit('toast', '🚡 09:00 — ouverture des remontées.', 'info'); }
    if (!Sim.flags.night && G.time >= SS.NIGHT_OPEN && SS.nightOn()) { Sim.flags.night = true; Net.touch(); SS.emit('toast', '🌙 18:30 — ouverture du ski nocturne sur les pistes éclairées.', 'info'); }
    if (!Sim.flags.nightEnd && G.time >= SS.NIGHT_CLOSE && SS.nightOn()) { Sim.flags.nightEnd = true; Net.touch(); }
    if (!Sim.flags.close && G.time >= SS.LIFT_CLOSE) { Sim.flags.close = true; Net.touch(); if (G.lifts.length) SS.emit('toast', '16:45 — fermeture des remontées, dernier retour station.', 'info'); }
    updateLifts(dt); if (SS.AE) SS.AE.tick(dt);
    const vs = Sim.visitors;
    for (let i = 0; i < vs.length; i++) if (vs[i].state !== 'gone') updateVisitor(vs[i], dt);
    if (Sim.rescues.length) updateRescues(dt);
    if (!Sim.grooming && G.time >= 17.25 * 3600) startGrooming();
    if (Sim.grooming) updateGroomers(dt);
    if (G.time >= SS.DAY_END || (G.time > 18 * 3600 && Sim.present <= 0 && !Sim.arrivals.length && Sim.groomers.every(g => g.state === 'parked'))) SS.endDay();
  }
  SS.tick = function (realDt) {
    G = SS.G; if (!G || G.paused) return;
    let dt = Math.min(realDt, 0.1) * G.speed * SS.CLOCK_RATE;
    const n = Math.min(10, Math.ceil(dt / 3)); dt /= n;
    for (let i = 0; i < n; i++) { step(dt); if (G.paused) break; }
  };
  // passer au lendemain quand la station est vide : le damage du soir est joué en accéléré
  SS.skipToEnd = function () {
    G = SS.G;
    if (Sim.present > 0 || (Sim.arrivals.length && G.time < 17 * 3600)) return false;
    Sim.arrivals = [];
    if (!Sim.grooming) startGrooming();
    const from = Math.max(G.time, 17.25 * 3600);
    for (let t = from; t < SS.DAY_END; t += 30) { G.time = t; updateGroomers(30); }
    G.time = SS.DAY_END; SS.endDay(); return true;
  };

  // ---------------- fin de journée : bilan, entretien, recherche, carrière, nuit ----------------
  SS.endDay = function () {
    G = SS.G;
    // les derniers visiteurs encore présents terminent leur journée (comptés comme retour tardif)
    for (const v of Sim.visitors) if (v.state !== 'gone') { think(v, 'Retour très tardif', 'Bloqués en altitude', -0.15); if (v.state === 'inS' && v.svc) v.svc.occ = 0; finalize(v); }
    // les pistes non terminées par les dameuses à 22 h restent en l'état
    let up = 0, sal = 0;
    for (const b of G.buildings) { const c = Mg.dailyCost(b); up += c[0]; sal += c[1]; b.sRev += b.revenue; b.sCost += c[0] + c[1] + (b.cogs || 0); b.sVisits += b.visits; b.lastRev = b.revenue; b.lastCost = c[0] + c[1] + (b.cogs || 0); b.lastVisits = b.visits; }
    let lm = 0; for (const l of G.lifts) { lm += l.maint * [0.55, 1, 1.6][G.maint] * (l.open ? 1 : 0.4) * (SS.researched('energy') ? 0.85 : 1); l.cond = SS.clamp(l.cond - 0.012 + [0.004, 0.012, 0.022][G.maint], 0.2, 1); l.brokenUntil = 0; }
    SS.charge(Math.round(up), 'Entretien bâtiments'); SS.charge(Math.round(sal), 'Salaires'); SS.charge(Math.round(lm), 'Remontées');
    for (const k of ['social', 'tv']) if (G.marketing[k] > 0) G.marketing[k]--;
    const n = G.satToday.length, avg = n ? G.satToday.reduce((a, b) => a + b, 0) / n * 100 : G.sat;
    if (n) { G.sat = Math.round(avg); G.reputation = SS.clamp(G.reputation + (avg - G.reputation) * Math.min(0.22, 0.04 + n / 2500) * (avg > G.reputation && SS.researched('loyalty') ? 1.3 : 1) + (G.wage - 1) * 0.12, 0, 100); }
    G.bestDay = Math.max(G.bestDay, G.visitorsToday);
    const inc = Object.values(G.ledger.inc).reduce((a, b) => a + b, 0), exp = Object.values(G.ledger.exp).reduce((a, b) => a + b, 0);
    const opex = Object.keys(G.ledger.exp).filter(k => k !== 'Construction' && k !== 'Recherche').reduce((a, k) => a + G.ledger.exp[k], 0);
    const report = { opex, day: G.day, date: SS.dateStr(G.day), visitors: G.visitorsToday, sat: G.sat, inc, exp, ledger: G.ledger, fb: Sim.fb, incidents: G.incidentsToday, lost: Sim.today ? Sim.today.lost : 0, why: Sim.today && Sim.today.why, weather: G.weather.type, notes: [] };
    G.history.push({ su: !!G.summer, day: G.day, year: G.year, v: G.visitorsToday, sat: G.sat, inc: Math.round(inc), exp: Math.round(opex), money: Math.round(G.money), rep: Math.round(G.reputation) });
    if (G.history.length > 150) G.history.shift();
    // événement en cours
    if (G.event && G.event.day <= G.day) {
      const ev = D.EVENTS.find(e => e.id === G.event.id); G.event.left--;
      if (G.event.left <= 0) {
        const ok = G.sat >= 50; G.eventsHosted[ev.id] = (G.eventsHosted[ev.id] || 0) + 1;
        G.reputation = SS.clamp(G.reputation + (ok ? ev.prestige * 0.6 : -3), 0, 100); G.fame = SS.clamp(G.fame + (ok ? ev.fameGain : -ev.fameGain * 0.6), 0, 100);
        report.notes.push((ok ? '🏆 ' + ev.name + ' : un succès ! Réputation en hausse.' : '😕 ' + ev.name + ' : organisation critiquée (satisfaction trop basse).'));
        G.event = null;
      }
    }
    Mg.dailyResearch(report);
    Mg.dailyLoans(report);
    // bilan du damage : ce qui était prévu et n'a pas pu être fait
    if (!G.summer && Sim.groomPlanned) { const missed = Sim.groomPlanned.filter(p => !p.groomedTonight && G.pistes.indexOf(p) >= 0); G.groomReport = { done: Sim.groomPlanned.length - missed.length, missed: missed.map(p => p.name), km: Sim.groomPlanned.filter(p => p.groomedTonight).reduce((a, p) => a + p.len, 0) / 1000 }; if (missed.length && Mg.groomerCount()) report.notes.push('🚜 Damage incomplet : ' + missed.length + ' piste(s) non préparée(s) faute de capacité (' + missed.slice(0, 3).map(p => p.name).join(', ') + (missed.length > 3 ? '…' : '') + ').'); }
    Mg.closeDay(G.ledger, G.visitorsToday);
    G.ledger0 = G.ledger.inc; G.ledger = { inc: {}, exp: {} };
    W.night();
    SS.updatePrestige(); G.elapsed++; SS.Scn.dailyFame(); SS.Scn.dailyBid(report);
    if (SS.AE && SS.AE.on()) SS.AE.daily(report);
    SS.checkCareer(report);
    SS.checkAch(report);
    G.day++; W.next();
    if (G.summer) { if (G.day >= SS.SUMMER_DAYS) endSummer(report); }
    else if (G.day >= SS.SEASON_DAYS) { if (SS.researched('summer')) startSummer(report); else seasonEnd(report); }
    G.time = SS.DAY_START; Sim.lastTouch = 0;
    for (const n of report.notes) { G.log.unshift({ t: SS.DAY_START, d: G.day, msg: n, kind: 'good' }); if (G.log.length > 60) G.log.pop(); }
    Sim.startDay();
    SS.Scn.dayStart(report);
    SS.Events.morning();
    SS.save();
    SS.emit('day:end', report);
  };
  function winterReset() {
    G.day = 0; G.year++; G.snowBase = 45; Mg.newSeason();
    for (const p of G.pistes) { p.depth = 45 + (p.topAlt - 1650) * 0.03; p.ice = 0; p.slush = 0; p.groomed = 0.8; p.snowClosed = false; p.avaUntil = 0; }
  }
  function startSummer(report) {
    G.summer = true; G.day = 0; G.event = G.event && G.event.day > 0 ? null : G.event;
    for (const p of G.pistes) { p.snowClosed = false; p.avaUntil = 0; p.ice = 0; p.slush = 0; p.powder = 0; }
    report.notes.push('🌞 Fin de la saison d\'hiver. La saison d\'été commence : 30 jours de VTT et de randonnée. Téléskis et tapis sont à l\'arrêt ; réglez le forfait été dans Finances.');
    W.init(); Net.touch(); SS.emit('season');
  }
  function endSummer(report) {
    G.summer = false; winterReset();
    report.notes.push('❄️ Fin de l\'été : les premières neiges tombent. Saison d\'hiver ' + G.year + ' !');
    W.init(); Net.touch(); SS.emit('season');
  }
  // passer l'été sans le jouer : recettes forfaitaires réduites pour les jours restants
  SS.skipSummer = function () {
    if (!G.summer || Sim.present > 0) return false;
    const hs = G.history.filter(h => h.su && h.year === G.year), avg = hs.length ? hs.reduce((a, h) => a + h.inc - h.exp, 0) / hs.length : 0;
    const gain = Math.round(Math.max(0, avg) * 0.6 * (SS.SUMMER_DAYS - G.day)); G.money += gain;
    const report = { opex: 0, day: G.day, date: SS.dateStr(G.day), visitors: 0, sat: G.sat, inc: 0, exp: 0, ledger: G.ledger, fb: {}, incidents: 0, notes: ['Été abrégé : ' + SS.eur(gain) + ' de recettes estimées pour les jours restants.'] };
    endSummer(report); G.time = SS.DAY_START; Sim.lastTouch = 0; Sim.startDay(); SS.save(); SS.emit('day:end', report); return true;
  };
  function seasonEnd(report) {
    let summer = 0;
    if (SS.researched('summer')) { for (const b of G.buildings) { const def = D.building(b.type); summer += (def.summer || 0) * 90 * 9 + (def.type === 'lodging' ? SS.bcap(b) * b.price * 0.25 * 70 : 0); } for (const l of G.lifts) if (D.lift(l.type).kind !== 'tow') summer += 2500; }
    G.money += summer; G.day = 0; G.year++; G.snowBase = 45; Mg.newSeason();
    for (const p of G.pistes) { p.depth = 45 + (p.topAlt - 1650) * 0.03; p.ice = 0; p.slush = 0; p.groomed = 0.8; p.snowClosed = false; }
    report.notes.push('🌞 Fin de saison ' + (G.year - 1) + '. ' + (summer > 0 ? 'Recettes d\'été (VTT, randonnée, hébergement) : ' + SS.eur(summer) + '.' : 'Recherchez « Saison d\'été » pour générer des recettes hors saison.') + ' Nouvelle saison d\'hiver !');
    W.init();
  }
  SS.checkAch = function (report) {
    const s = SS.stats(); s.money = G.money; s.total = G.totalVisitors; s.medals = Object.values(G.medals).reduce((a, b) => a + b, 0); s.year = G.year; s.loan = G.loan; s.night = G.ledger0 && G.ledger0['Forfaits nocturnes'] > 0; s.research = G.research.done.length;
    for (const a of D.ACH) if (!G.ach[a.id] && a.test(s, G)) { G.ach[a.id] = G.day + 1 + (G.year - 1) * SS.SEASON_DAYS; const msg = '🏅 Succès débloqué : ' + a.name + '.'; if (report) report.notes.push(msg); else SS.log(msg, 'good'); }
  };
  // remise en cohérence des visiteurs quand le réseau change en cours de journée
  SS.on('network', function (old) {
    if (!old || !old.length) return;
    for (const v of Sim.visitors) {
      if (v.state === 'gone') continue;
      const o = old[v.node]; if (o) { const nn = Net.nearestNode(o.x, o.z); v.node = nn ? nn.n.id : Net.hub.id; } else v.node = Net.hub.id;
      v.path = null; v.target = null;
      if (v.state === 'ski' || v.state === 'down') { const p = v.edge.piste, e = p.edges.find(x => v.s >= x.s0 - 1 && v.s < x.s1); if (e) v.edge = e; else v.edge = { piste: p, s0: v.s, s1: p.len, to: p.nb, from: p.na, len: p.len - v.s, kind: 'piste' }; }
    }
  });
})();
