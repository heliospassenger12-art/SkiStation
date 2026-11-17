// SkiStation Web — interface de gestion : finances, banque, recherche, exploitation (pisteurs, dameuses, restauration,
// hébergements, loisirs) et fiche de gestion d'un bâtiment. S'appuie sur manage.js pour toutes les règles.
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data, UI = SS.UI, Mg = SS.Mgmt, Sim = SS.Sim, Net = SS.Net;
  const { row, btn, dot, eur, esc, pct, stars } = UI.H;
  const P = UI.PANELS;
  let G;
  const tabs = (list, cur, act) => `<div class="tabs">${list.map(t => btn(t[1], act, t[0], 'tabb' + (cur === t[0] ? ' on' : ''))).join('')}</div>`;
  const bar = (v, c) => `<div class="bar"><i style="width:${Math.round(SS.clamp(v, 0, 1) * 100)}%;background:${c}"></i></div>`;
  const star5 = r => { const n = Math.round(r); let s = ''; for (let i = 1; i <= 5; i++) s += `<i class="st${n >= i ? ' on' : ''}">★</i>`; return s; };
  const sign = v => (v >= 0 ? '+' : '−') + eur(Math.abs(v));
  const cls = v => v >= 0 ? 'pos' : 'neg';
  const chips = (list, cur, act, extra) => `<div class="chips">${list.map((n, i) => btn(n, act, i, 'chip' + (cur === i ? ' on' : ''))).join('')}${extra || ''}</div>`;

  // ---------- estimation des charges fixes du jour ----------
  function fixedCosts() {
    G = SS.G; let build = 0, wages = 0, lifts = 0, interest = 0;
    for (const b of G.buildings) { const c = Mg.dailyCost(b); build += c[0]; wages += c[1]; }
    for (const l of G.lifts) lifts += l.maint * [0.55, 1, 1.6][G.maint] * (l.open ? 1 : 0.4) * (SS.researched('energy') ? 0.85 : 1);
    for (const l of G.loans) interest += l.left * l.rate;
    return { build, wages, lifts, interest, total: build + wages + lifts + interest };
  }
  Mg.fixedCosts = fixedCosts;

  // =====================================================================================================
  //  FINANCES
  // =====================================================================================================
  P.finance = function () {
    G = SS.G; UI.ftab = UI.ftab || 'resume';
    let h = tabs([['resume', 'Résultats'], ['tarifs', 'Tarifs & politique'], ['banque', 'Banque']], UI.ftab, 'ftab');
    const inc = G.ledger.inc, exp = G.ledger.exp, ti = Mg.sum(inc), te = Mg.opex(exp), fc = fixedCosts();
    if (UI.ftab === 'resume') {
      h += `<div class="kpis"><div><b>${G.unlimited ? '∞' : eur(G.money)}</b>trésorerie</div><div><b class="${G.loan > 0 ? 'neg' : ''}">${eur(G.loan)}</b>dette</div><div><b class="${cls(ti - te)}">${sign(ti - te)}</b>résultat du jour</div></div>`;
      h += '<canvas id="chart" width="380" height="130"></canvas>';
      const cats = Object.keys(inc).sort((a, b) => inc[b] - inc[a]), ecats = Object.keys(exp).filter(k => k !== 'Construction' && k !== 'Recherche').sort((a, b) => exp[b] - exp[a]);
      h += `<h4>Compte de résultat du jour</h4><div class="two"><div><small class="fine">Recettes</small>${cats.map(k => row(k, eur(inc[k]))).join('') || '<p class="fine">Aucune pour l\'instant.</p>'}${row('<b>Total</b>', '<b>' + eur(ti) + '</b>')}</div><div><small class="fine">Charges</small>${ecats.map(k => row(k, '−' + eur(exp[k]))).join('') || '<p class="fine">Aucune pour l\'instant.</p>'}${row('<b>Total</b>', '<b>−' + eur(te) + '</b>')}</div></div>`;
      const inv = (exp.Construction || 0) + (exp.Recherche || 0);
      if (inv) h += `<p class="fine">Investissements du jour (hors résultat) : ${eur(inv)}.</p>`;
      const s = G.season, sr = Mg.sum(s.inc), sc = Mg.opex(s.exp);
      h += `<h4>${G.summer ? 'Été' : 'Saison'} en cours · ${s.days} jour(s)</h4><div class="grid">${row('Recettes', eur(sr))}${row('Charges', '−' + eur(sc))}${row('Résultat d\'exploitation', `<span class="${cls(sr - sc)}">${sign(sr - sc)}</span>`)}${row('Par jour', s.days ? eur((sr - sc) / s.days) : '—')}${row('Visiteurs', SS.num(s.visitors))}${row('Dépense moyenne', s.visitors ? eur(sr / s.visitors) : '—')}</div>`;
      if (G.lastSeason && G.lastSeason.days) { const l = G.lastSeason, lr = Mg.sum(l.inc), lc = Mg.opex(l.exp); h += `<p class="fine">Saison précédente : recettes ${eur(lr)}, charges ${eur(lc)}, résultat <span class="${cls(lr - lc)}">${sign(lr - lc)}</span> sur ${l.days} jours.</p>`; }
      h += `<h4>Charges fixes estimées par jour</h4><div class="grid">${row('Entretien des bâtiments', eur(fc.build))}${row('Salaires', eur(fc.wages))}${row('Entretien des remontées', eur(fc.lifts))}${row('Intérêts', eur(fc.interest))}${row('<b>Total</b>', '<b>' + eur(fc.total) + '</b>')}</div>`;
      const avg = s.visitors ? sr / s.visitors : (Sim.today && Sim.today.total ? ti / Math.max(1, G.visitorsToday) : 0);
      if (avg > 0) h += `<p class="fine">Seuil de rentabilité : environ <b>${SS.num(fc.total / (avg * 0.78))}</b> visiteurs par jour (dépense moyenne ${eur(avg)}, marge sur achats déduite).</p>`;
      // rentabilité par bâtiment (saison)
      const rows = G.buildings.filter(b => b.sRev + b.revenue > 0 || D.building(b.type).type === 'lodging').map(b => { const rev = b.sRev + b.revenue, cost = b.sCost + (b.lastCost && !b.sCost ? 0 : 0); return { b, rev, cost, m: rev - cost }; }).sort((a, b) => a.m - b.m);
      if (rows.length) {
        h += '<h4>Rentabilité par bâtiment (saison)</h4><div class="jtab">' + rows.slice(0, 12).map(r => `<div class="jrow li2" data-act="focus" data-arg="building:${r.b.id}"><span>${esc(D.building(r.b.type).name)}</span><span>${eur(r.rev)}</span><span>${eur(r.cost)}</span><span class="${cls(r.m)}">${sign(r.m)}</span></div>`).join('') + '</div><p class="fine">Recettes · charges (salaires, entretien, achats) · marge. Trié du moins rentable au plus rentable.</p>';
      }
    } else if (UI.ftab === 'tarifs') {
      const d = Sim.today || { fair: 26 }; const at = d.at;
      if (G.summer) h += `<h4>Forfait été (VTT et piétons)</h4><div class="slider"><input id="summerPass" type="range" min="5" max="45" step="1" value="${G.summerPass}" data-in="spass"><b id="spassOut">${G.summerPass} €</b></div><p class="fine">Juste prix estimé : <b>${Math.round(d.fair)} €</b>.</p>`;
      h += `<h4>Forfait journée${G.summer ? ' d\'hiver' : ''}</h4><div class="slider"><input id="passPrice" type="range" min="10" max="90" step="1" value="${G.passPrice}" data-in="pass"><b id="passOut">${G.passPrice} €</b></div><p class="fine">Juste prix estimé pour votre domaine : <b>${Math.round(d.fair)} €</b>${at ? ` (${at.km.toFixed(1)} km de pistes reliées)` : ''}. Au-dessus, la fréquentation et la satisfaction baissent. Enfants −40 %, résidents −12 %${SS.researched('yield') ? ', +12 % week-ends et vacances (tarification dynamique)' : ''}.</p>`;
      { const sp = SS.Loyal ? SS.Loyal.passPrice() : 0; h += `<h4>Forfait saison</h4><div class="slider"><input id="seasonPass" type="range" min="100" max="1500" step="10" value="${sp}" data-in="spass2"><b id="spass2Out">${sp} €</b></div><p class="fine">Vendu aux habitués les plus fidèles au début de chaque hiver : recette encaissée d'avance, ils viennent alors bien plus souvent. Repère : environ 11 forfaits journée (${Math.round(G.passPrice * 11)} €).</p>`; }
      if (SS.researched('night')) h += `<h4>Forfait nocturne</h4><div class="slider"><input id="nightPrice" type="range" min="5" max="40" step="1" value="${G.nightPrice}" data-in="night"><b id="nightOut">${G.nightPrice} €</b></div>`;
      h += `<h4>Politique salariale</h4>${chips(['Économe', 'Standard', 'Attractif'], G.wage, 'fwage')}<p class="fine">${['Salaires −12 % : qualité de service en baisse et réputation qui s\'érode.', 'Salaires de référence.', 'Salaires +15 % : meilleure qualité de service et réputation en hausse lente.'][G.wage]}</p>`;
      h += `<h4>Entretien des remontées</h4>${chips(['Minimal', 'Normal', 'Renforcé'], G.maint, 'maint')}<p class="fine">Un entretien minimal coûte moins cher mais multiplie les pannes.</p>`;
      h += `<h4>Marketing</h4><div class="actions">${btn(G.marketing.social > 0 ? `Réseaux sociaux : ${G.marketing.social} j restants` : 'Réseaux sociaux — 8 000 € (+15 %, 5 j)', 'marketing', 'social', '', G.marketing.social > 0)}${btn(G.marketing.tv > 0 ? `Télévision : ${G.marketing.tv} j restants` : 'Télévision — 45 000 € (+35 %, 7 j)', 'marketing', 'tv', '', G.marketing.tv > 0)}</div>`;
    } else {
      const lim = Mg.creditLimit(), fcI = fixedCosts().interest;
      h += `<div class="kpis"><div><b>${eur(G.loan)}</b>dette</div><div><b>${eur(Math.max(0, lim - G.loan))}</b>capacité restante</div><div><b>${eur(fcI)}</b>intérêts / jour</div></div>`;
      h += `<p class="fine">Plafond d'endettement : <b>${eur(lim)}</b>. Il augmente avec le prestige et la réputation. Les remboursements sont prélevés chaque soir.</p>`;
      if (G.negDays > 0) h += `<div class="msg warn">⚠️ Découvert depuis ${G.negDays} jour(s). Au bout de 3 jours, la banque impose un crédit d'urgence à taux fort.</div>`;
      h += '<h4>Prêts en cours</h4>';
      h += G.loans.length ? G.loans.map(l => `<div class="card"><div class="ctitle"><b>${esc(l.name)}</b><span>${eur(l.left)}</span></div><div class="cmeta">Remboursement ${eur(l.pay)} / jour · encore ${l.daysLeft} jour(s) · intérêts ${(l.rate * 100).toFixed(2)} % / jour</div>${btn('Solder — ' + eur(l.left), 'loanPay', l.id, 'small', !G.unlimited && G.money < l.left)}</div>`).join('') : '<p class="fine">Aucun prêt.</p>';
      h += '<h4>Emprunter</h4>' + D.LOANS.map(p => `<div class="card"><div class="ctitle"><b>${p.name}</b><span>${eur(p.amount)}</span></div><div class="cmeta">Sur ${p.days} jours · ${eur(p.amount / p.days)} / jour · intérêts ${(p.rate * 100).toFixed(2)} % / jour (≈ ${eur(p.amount * p.rate * p.days / 2)} au total)</div>${btn('Emprunter', 'loanTake', p.id, 'small', G.loan + p.amount > lim)}</div>`).join('');
    }
    return h;
  };

  // =====================================================================================================
  //  RECHERCHE
  // =====================================================================================================
  P.research = function () {
    G = SS.G; const R = G.research;
    let h = G.allUnlocked ? '<div class="msg">Bac à sable : tout le catalogue est débloqué.</div>' : '';
    h += `<div class="card cur"><div class="ctitle"><b>🔬 Laboratoire — niveau ${G.lab} / 3</b><span>${G.lab} projet(s) en parallèle</span></div><div class="cmeta">Niveau 2 : deux projets simultanés et −15 % de durée. Niveau 3 : trois projets et −30 %.</div>${G.lab < 3 ? btn('Améliorer — ' + eur(Mg.labCost()), 'labup', undefined, 'small', !SS.canPay(Mg.labCost())) : ''}</div>`;
    h += '<h4>En cours</h4>';
    h += R.active.length ? R.active.map(a => { const r = D.RESEARCH.find(x => x.id === a.id); return `<div class="card cur"><div class="ctitle"><b>${r.name}</b><span>${a.left} j restants</span></div>${bar(1 - a.left / a.total, 'var(--blue)')}</div>`; }).join('') : '<p class="fine">Aucun projet en cours.</p>';
    if (R.queue.length) h += '<h4>File d\'attente</h4>' + R.queue.map((id, i) => { const r = D.RESEARCH.find(x => x.id === id); return `<div class="li">${i + 1}. <b>${r.name}</b> <small>démarre quand un poste se libère (${eur(r.cost)} prélevés à ce moment)</small>${btn('Retirer', 'runq', id, 'small')}</div>`; }).join('');
    for (const cat of D.RCATS) {
      h += `<h4>${cat}</h4>`;
      for (const r of D.RESEARCH.filter(x => x.cat === cat)) {
        const st = Mg.rState(r), cur = st === 'active', done = st === 'done', locked = st === 'locked', days = Mg.rDays(r);
        const req = r.req ? D.RESEARCH.find(x => x.id === r.req) : null;
        const free = R.active.length < G.lab && (!r.req || R.done.indexOf(r.req) >= 0);
        h += `<div class="card${done ? ' done' : locked ? ' locked' : cur ? ' cur' : ''}"><div class="ctitle"><b>${r.name}</b><span>${done ? '✔ acquis' : cur ? 'en cours' : st === 'queued' ? 'en file' : eur(r.cost) + ' · ' + days + ' j'}</span></div><div class="cmeta">${r.desc}${locked || (req && !done && R.done.indexOf(r.req) < 0 && st !== 'active') ? ` <i>Nécessite : ${req.name}${st === 'open' ? ' (peut être mise en file)' : ''}.</i>` : ''}</div>${st === 'open' && !G.allUnlocked ? btn(free ? 'Lancer' : 'Mettre en file', 'rstart', r.id, 'small') : ''}</div>`;
      }
    }
    return h;
  };

  // =====================================================================================================
  //  EXPLOITATION : pisteurs, dameuses, restauration, hébergement, loisirs
  // =====================================================================================================
  const ETABS = [['pisteurs', '🚑 Pisteurs'], ['dameuses', '🚜 Dameuses'], ['resto', '🍽️ Restauration'], ['heberg', '🛏️ Hébergement'], ['loisirs', '🎡 Loisirs']];
  const link = b => `data-act="focus" data-arg="building:${b.id}"`;

  function pisteurs() {
    const info = Mg.patrolInfo || Mg.securePistes(), S = G.safety, posts = G.buildings.filter(b => b.type === 'rescue');
    const open = G.pistes.filter(p => p.open), km = open.reduce((a, p) => a + p.len, 0) / 1000;
    let h = `<div class="kpis"><div><b>${info.patrol}</b>pisteurs</div><div><b>${info.posts}</b>postes</div><div><b class="${info.capKm >= info.needKm ? 'pos' : 'neg'}">${info.capKm.toFixed(0)} / ${info.needKm.toFixed(0)} km</b>couverts</div></div>`;
    h += `<p class="fine">Chaque matin, les pisteurs sécurisent les pistes ouvertes les plus proches de leur poste, dans la limite de <b>${Mg.kmPerPatrol()} km par pisteur</b>. Une piste sécurisée a ~36 % d'accidents en moins qu'une piste non sécurisée.</p>`;
    if (!posts.length) h += '<div class="msg bad">Aucun poste de secours : toutes les pistes sont à risque et les évacuations sont très lentes. Construisez-en un (Construire › Sécurité).</div>';
    else if (info.unsecured) h += `<div class="msg warn">${info.unsecured} piste(s) ouverte(s) non sécurisée(s) : ajoutez des pisteurs ou un poste plus proche.</div>`;
    h += '<h4>Postes de secours</h4>' + (posts.map(b => `<div class="li" ${link(b)}><b>Poste à ${SS.num(b.y)} m</b><span class="tag ${b.open ? 'ok' : 'off'}">${b.open ? b.patrol + ' pisteurs' : 'Fermé'}</span><small>${b.busy || 0} intervention(s) en cours · ${eur(Mg.dailyCost(b)[0] + Mg.dailyCost(b)[1])} / jour</small></div>`).join('') || '<p class="fine">Aucun.</p>');
    h += '<h4>Couverture des pistes</h4>' + (open.map(p => { const eta = Mg.responseEta(p); return `<div class="li" data-act="focus" data-arg="piste:${p.id}">${dot(p.diff)}<b>${esc(p.name)}</b><span class="tag ${p.secured ? 'ok' : 'bad'}">${p.secured ? 'sécurisée' : 'non sécurisée'}</span><small>${SS.num(p.len)} m · ${p.postDist > 0 ? 'poste à ' + SS.num(p.postDist) + ' m' : 'aucun poste'}${eta ? ' · intervention ≈ ' + eta.toFixed(0) + ' min' : ''}</small></div>`; }).join('') || '<p class="fine">Aucune piste ouverte.</p>');
    h += `<h4>Bilan de la saison</h4><div class="grid">${row('Accidents', S.incidents)}${row('dont graves', S.severe)}${row('Délai moyen d\'arrivée', S.respN ? (S.respSum / S.respN / 60).toFixed(0) + ' min' : '—')}${row('Héliport', G.buildings.some(b => b.type === 'heliport') ? 'oui' : 'non')}${row('Motoneiges de secours', SS.researched('patrol2') ? 'oui (+50 % de vitesse)' : 'non')}${row('Balisage renforcé', SS.researched('signage') ? 'oui' : 'non')}</div>`;
    h += '<p class="fine">Ajustez l\'effectif dans la fiche de chaque poste (clic sur le poste). Chaque pisteur au-delà de l\'équipe de base coûte environ 150 € par jour.</p>';
    return h;
  }

  function dameuses() {
    const cnt = Mg.groomerCount(), plan = Mg.groomPlan(), needKm = plan.reduce((a, p) => a + p.len, 0) / 1000, cap = Mg.groomCapacityKm(), garages = G.buildings.filter(b => b.type === 'garage');
    let h = `<div class="kpis"><div><b>${cnt}</b>dameuses</div><div><b class="${cap >= needKm ? 'pos' : 'neg'}">${cap.toFixed(0)} / ${needKm.toFixed(0)} km</b>capacité / besoin</div><div><b>${(Mg.groomSpeed() * 3.6).toFixed(1)} km/h</b>vitesse de damage</div></div>`;
    if (!garages.length) h += '<div class="msg bad">Aucun garage : sans dameuse, les pistes se dégradent chaque jour (Construire › Exploitation).</div>';
    else if (cap < needKm) h += `<div class="msg warn">Capacité insuffisante : il manque environ ${(needKm - cap).toFixed(0)} km de damage par nuit. Achetez des dameuses, passez en équipe de nuit, ou classez moins de pistes en « Normal ».</div>`;
    h += `<h4>Équipe de nuit</h4><div class="actions">${btn(G.groomNight ? '☑ Équipe de nuit renforcée (+50 % de vitesse)' : '☐ Équipe de nuit renforcée (+50 % de vitesse)', 'gnight')}</div><p class="fine">Coûte environ 170 € par machine et par nuit en plus.</p>`;
    h += '<h4>Parc de machines</h4>' + (garages.map(b => `<div class="li" ${link(b)}><b>Garage à ${SS.num(b.y)} m</b><span class="tag ok">${b.machines} dameuses</span><small>${eur(Mg.dailyCost(b)[0] + Mg.dailyCost(b)[1])} / jour</small>${btn('+ Acheter ' + eur(Mg.machineCost), 'machines', b.id + ':1', 'small', b.machines >= 6)}${btn('− Vendre', 'machines', b.id + ':-1', 'small', b.machines <= 1)}</div>`).join('') || '<p class="fine">Aucun garage.</p>');
    h += '<h4>Plan de damage de cette nuit</h4><p class="fine">Les pistes « Prioritaire » passent d\'abord ; « Normal » ensuite si la capacité le permet ; « Jamais » restent bosselées (freeride). Les pistes noires nécessitent les dameuses à treuil.</p>';
    h += G.pistes.map(p => `<div class="li">${dot(p.diff)}<b>${esc(p.name)}</b> <small>${SS.num(p.len)} m · damage ${pct(p.groomed)} · ${SS.Weather.state(p)}${p.diff === 3 && !SS.researched('winch') ? ' · treuil requis' : ''}</small>${chips(['Jamais', 'Normal', 'Prioritaire'], p.prio, 'gprio_' + p.id)}</div>`).join('') || '<p class="fine">Aucune piste.</p>';
    if (G.groomReport) h += `<h4>Dernière nuit</h4><div class="grid">${row('Pistes damées', G.groomReport.done)}${row('Kilomètres', G.groomReport.km.toFixed(1) + ' km')}${row('Non faites', G.groomReport.missed.length ? G.groomReport.missed.slice(0, 4).map(esc).join(', ') : 'aucune')}</div>`;
    return h;
  }

  function foodList(types) {
    return G.buildings.filter(b => types.indexOf(D.building(b.type).type) >= 0);
  }
  function resto() {
    const list = foodList(['food', 'drink']), exp = Sim.expected || 0;
    const cap = list.filter(b => Mg.isOpen(b) || b.open).reduce((a, b) => a + SS.bcap(b) * (D.building(b.type).type === 'food' ? 1 : 0.4), 0);
    const eve = list.filter(b => b.open && b.hours === 'eve' && D.building(b.type).type === 'food');
    const need = exp * 0.55, lod = G.buildings.filter(b => D.building(b.type).type === 'lodging' && b.open).reduce((a, b) => a + (b.opts.hb ? 0 : b.guests), 0);
    let h = `<div class="kpis"><div><b>${list.length}</b>établissements</div><div><b class="${cap * 3 >= need ? 'pos' : 'neg'}">${SS.num(cap)}</b>places</div><div><b>${eve.length}</b>ouverts le soir</div></div>`;
    h += `<p class="fine">Au déjeuner, environ <b>${SS.num(need)}</b> visiteurs cherchent à manger (sur ${SS.num(exp)} attendus) ; vos ${SS.num(cap)} places font ${(cap * 3 >= need ? 'largement' : 'à peine')} face avec trois services. ${lod > 0 ? `Le soir, <b>${SS.num(lod)}</b> résidents sans demi-pension dînent dehors : il faut au moins un restaurant ouvert le soir.` : ''}</p>`;
    if (lod > 0 && !eve.length) h += '<div class="msg warn">Aucun restaurant ouvert le soir : vos résidents sans demi-pension repartent mécontents. Réglez « Horaires » sur « Midi et soir » dans la fiche d\'un restaurant, ou construisez une pizzeria ou un restaurant savoyard.</div>';
    h += '<h4>Établissements</h4>' + (list.map(b => { const def = D.building(b.type), q = Mg.quality(b), m = b.sRev + b.revenue - b.sCost - (b.cogs || 0); return `<div class="li" ${link(b)}><b>${def.name}</b><span class="tag ${b.open ? 'ok' : 'off'}">${b.open ? b.occ + '/' + SS.bcap(b) : 'Fermé'}</span><small>${star5(b.rating)} ${D.RANGE[b.range]} · qualité ${pct(q)} · ${b.price} € (réf. ${Mg.refPrice(b)} €) · ${b.hours === 'eve' ? 'midi et soir' : 'midi'}${b.queue.length ? ' · file ' + b.queue.length : ''} · saison <span class="${cls(m)}">${sign(m)}</span></small></div>`; }).join('') || '<p class="fine">Aucun restaurant ni bar. Construire › Restauration.</p>');
    h += '<p class="fine">Un restaurant premium attire les budgets confortables et rapporte plus par couvert, mais les petits budgets l\'évitent. Les achats représentent 27 à 40 % du chiffre d\'affaires selon la gamme.</p>';
    return h;
  }

  function heberg() {
    const info = Mg.lodgingInfo || { pools: [0, 0, 0], served: [0, 0, 0], beds: [0, 0, 0] }, lod = G.buildings.filter(b => D.building(b.type).type === 'lodging');
    const beds = lod.reduce((a, b) => a + SS.bcap(b), 0), guests = lod.reduce((a, b) => a + b.guests, 0);
    let rev = 0; for (const b of lod) rev += b.guests * b.price;
    let h = `<div class="kpis"><div><b>${SS.num(beds)}</b>lits</div><div><b>${beds ? pct(guests / beds) : '—'}</b>occupation</div><div><b>${eur(beds ? rev / beds : 0)}</b>revenu / lit</div></div>`;
    h += '<h4>Clientèle par gamme</h4><div class="jtab"><div class="jrow head"><span>Gamme</span><span>Demande</span><span>Lits</span><span>Occupés</span></div>' + D.SEG.map((n, i) => `<div class="jrow"><span>${n}</span><span>${SS.num(info.pools[i])}</span><span>${SS.num(info.beds[i])}</span><span class="${info.beds[i] && info.served[i] / info.beds[i] > 0.85 ? 'pos' : ''}">${SS.num(info.served[i])}</span></div>`).join('') + '</div>';
    const tips = [];
    if (info.beds[0] === 0 && info.pools[0] > 20) tips.push('Aucun hébergement économique : gîte, hôtel ★★ ou résidence de tourisme capteraient ' + SS.num(info.pools[0]) + ' clients.');
    if (info.beds[1] === 0 && info.pools[1] > 20) tips.push('Aucun hébergement confort : un hameau de chalets ou un hôtel ★★★ plairait aux familles.');
    if (info.beds[2] === 0 && info.pools[2] > 10) tips.push('Demande de luxe non servie (' + SS.num(info.pools[2]) + ' clients) : hôtel ★★★★, palace ou chalet de prestige.');
    for (let i = 0; i < 3; i++) if (info.beds[i] && info.served[i] / info.beds[i] > 0.92 && info.pools[i] > info.beds[i] * 1.2) tips.push('Gamme ' + D.SEG[i].toLowerCase() + ' saturée : la demande dépasse les lits. Construisez ou agrandissez.');
    if (G.prestige < 45 && info.beds[2] > 0) tips.push('Le luxe ne se remplit que si le prestige suit (actuellement ' + G.prestige + ' / 100) : événements, domaine étendu, réputation.');
    h += tips.length ? tips.map(t => `<div class="msg warn">${t}</div>`).join('') : '<p class="fine">Offre équilibrée avec la demande.</p>';
    h += '<h4>Établissements</h4>' + (lod.map(b => { const def = D.building(b.type), occ = SS.bcap(b) ? b.guests / SS.bcap(b) : 0, m = b.sRev + b.revenue - b.sCost; return `<div class="li" ${link(b)}><b>${def.name}</b><span class="tag ${b.open ? 'ok' : 'off'}">${b.open ? pct(occ) : 'Fermé'}</span><small>${D.SEG[def.seg || 0]} · ${star5(b.rating)} · ${b.price} € / nuit (réf. ${def.price} €)${b.opts.hb ? ' · demi-pension' : ''}${b.opts.spa ? ' · spa' : ''}${b.opts.shuttle ? ' · navette' : ''} · saison <span class="${cls(m)}">${sign(m)}</span></small></div>`; }).join('') || '<p class="fine">Aucun hébergement. Construire › Hébergement.</p>');
    return h;
  }

  function loisirs() {
    const list = G.buildings.filter(b => D.building(b.type).type === 'activity'), pass = G.buildings.filter(b => ['school', 'kids', 'stadium', 'olympic', 'heli'].indexOf(D.building(b.type).type) >= 0);
    let h = `<div class="kpis"><div><b>${list.length}</b>activités</div><div><b>${list.reduce((a, b) => a + b.visits, 0)}</b>entrées aujourd'hui</div><div><b>${eur(list.reduce((a, b) => a + b.revenue, 0))}</b>recettes</div></div>`;
    h += `<p class="fine">Les activités attirent des clients (attractivité du domaine), occupent l'après-ski et fidélisent. ${G.summer ? 'En été, elles portent une grande part de l\'activité.' : 'Certaines (spa, musée, parapente) rapportent aussi l\'été.'}</p>`;
    h += '<h4>Activités</h4>' + (list.map(b => { const def = D.building(b.type), m = b.sRev + b.revenue - b.sCost; return `<div class="li" ${link(b)}><b>${def.name}</b><span class="tag ${b.open ? 'ok' : 'off'}">${b.open ? b.occ + '/' + SS.bcap(b) : 'Fermé'}</span><small>${star5(b.rating)} ${def.tags || ''} · ${b.price ? b.price + ' €' : 'gratuit'} · ${b.visits} entrées · saison <span class="${cls(m)}">${sign(m)}</span></small></div>`; }).join('') || '<p class="fine">Aucune activité. Construire › Loisirs (snowpark, luge, patinoire, spa, nordique…).</p>');
    if (pass.length) h += '<h4>Autres équipements</h4>' + pass.map(b => `<div class="li" ${link(b)}><b>${D.building(b.type).name}</b><span class="tag ${b.open ? 'ok' : 'off'}">${b.open ? 'ouvert' : 'fermé'}</span><small>${b.visits} clients · ${eur(b.revenue)} aujourd'hui</small></div>`).join('');
    return h;
  }

  P.exploit = function () {
    G = SS.G; UI.etab = UI.etab || 'pisteurs';
    return tabs(ETABS, UI.etab, 'etab') + ({ pisteurs, dameuses, resto, heberg, loisirs }[UI.etab])();
  };

  // =====================================================================================================
  //  FICHE DE GESTION D'UN BÂTIMENT
  // =====================================================================================================
  UI.mgmtBuilding = function (o, def) {
    G = SS.G; const t = def.type; let h = '';
    const isSvc = Mg.isService(o), isLodg = t === 'lodging';
    if (isSvc || isLodg) {
      const q = Mg.quality(o), ref = Mg.refPrice(o), lo = Math.max(1, Math.round(ref * (isLodg ? 0.4 : 0.5))), hi = Math.round(ref * (isLodg ? 2.2 : 2.2));
      h += `<h4>Qualité & réputation</h4><div class="grid">${row('Note des clients', star5(o.rating) + ' ' + o.rating.toFixed(1))}${row('Qualité du service', pct(q))}</div>`;
      if (isSvc && t !== 'activity' || t === 'activity') {
        if (t === 'food' || t === 'drink' || t === 'shop' || t === 'rental') h += `<h4>Gamme</h4>${chips(D.RANGE, o.range, 'range')}<p class="fine">${['Prix et achats bas, petits budgets, qualité plus faible.', 'Équilibre standard.', 'Meilleure qualité et prix plus élevés ; les petits budgets s\'en détournent.'][o.range]}</p>`;
        h += `<h4>Personnel</h4>${chips(D.STAFF, o.staffLvl, 'staffl')}<p class="fine">${['Salaires −20 %, service plus lent, capacité −12 %.', 'Effectif normal.', 'Salaires +28 %, service plus rapide, capacité +10 %, qualité en hausse.'][o.staffLvl]}</p>`;
      }
      if (t === 'food' || t === 'drink') h += `<h4>Horaires</h4><div class="chips">${btn('Midi (jusqu\'à 17 h 30)', 'hours', 'day', 'chip' + (o.hours === 'day' ? ' on' : ''))}${btn('Midi et soir', 'hours', 'eve', 'chip' + (o.hours === 'eve' ? ' on' : ''))}</div><p class="fine">Ouvert le soir, il sert les résidents et l'après-ski, mais le personnel de soirée coûte plus cher.</p>`;
      h += `<h4>Tarif${isLodg ? ' de la nuitée' : ''}</h4><div class="slider"><input id="bprice_${o.id}" type="range" min="${lo}" max="${hi}" step="1" value="${SS.clamp(o.price, lo, hi)}" data-in="bprice"><b id="bpriceOut">${o.price} €</b></div><p class="fine">Tarif de référence : ${ref} €. ${isLodg ? 'Un prix trop haut vide les chambres ; trop bas, vous perdez de la marge.' : 'Trop cher, les clients renoncent et la note baisse.'}</p>`;
    }
    if (isLodg) {
      const seg = def.seg || 0, occ = SS.bcap(o) ? o.guests / SS.bcap(o) : 0, ho = o.opts;
      h += `<h4>Hébergement ${'★'.repeat(def.stars || 1)} · clientèle ${D.SEG[seg].toLowerCase()}</h4><div class="grid">${row('Occupation', `${o.guests} / ${SS.bcap(o)} (${pct(occ)})`)}${row('Recette par nuit', eur(o.guests * o.price))}</div>${bar(occ, occ > 0.85 ? 'var(--ok)' : occ > 0.5 ? 'var(--blue)' : 'var(--warn)')}`;
      h += `<h4>Options</h4><div class="actions col">${btn((ho.hb ? '☑' : '☐') + ' Demi-pension : dîner inclus (+' + eur(Mg.dinnerPrice(o)) + ' par résident, marge 62 %)', 'opt', 'hb')}${btn((ho.spa ? '☑ Spa installé (+10 % de qualité, 320 € / jour)' : '☐ Installer un spa — ' + eur(Mg.spaCost(o)) + ' (+10 % de qualité)'), 'opt', 'spa')}${btn((ho.shuttle ? '☑' : '☐') + ' Navette jusqu\'au front de neige (190 € / jour, satisfaction en hausse)', 'opt', 'shuttle')}</div>`;
    }
    if (t === 'garage') h += `<h4>Dameuses</h4><div class="actions">${btn('−', 'machines', o.id + ':-1', '', o.machines <= 1)}<b class="count">${o.machines}</b>${btn('+ ' + eur(Mg.machineCost), 'machines', o.id + ':1', '', o.machines >= 6)}</div><p class="fine">6 maximum par garage. Achat ${eur(Mg.machineCost)}, revente ${eur(30000)}. Chaque machine ajoutée coûte environ 160 € de salaire et 90 € d'entretien par jour au-delà des 2 de base.</p>`;
    if (t === 'rescue') h += `<h4>Pisteurs</h4><div class="actions">${btn('−', 'patrol', o.id + ':-1', '', o.patrol <= 2)}<b class="count">${o.patrol}</b>${btn('+', 'patrol', o.id + ':1', '', o.patrol >= 12)}</div><p class="fine">Chaque pisteur sécurise ${Mg.kmPerPatrol()} km de pistes. 150 € par jour au-delà de l'équipe de base.</p>`;
    if (isSvc || isLodg || t === 'school' || t === 'kids') {
      const c = Mg.dailyCost(o), rev = o.revenue, cg = o.cogs || 0;
      h += `<h4>Résultat</h4><div class="two"><div><small class="fine">Aujourd'hui</small>${row('Recettes', eur(rev))}${row('Achats', '−' + eur(cg))}${row('Charges fixes', '−' + eur(c[0] + c[1]))}${row('<b>Marge</b>', `<b class="${cls(rev - cg - c[0] - c[1])}">${sign(rev - cg - c[0] - c[1])}</b>`)}</div><div><small class="fine">Saison</small>${row('Recettes', eur(o.sRev + rev))}${row('Charges', '−' + eur(o.sCost + c[0] + c[1] + cg))}${row('<b>Marge</b>', `<b class="${cls(o.sRev + rev - o.sCost - c[0] - c[1] - cg)}">${sign(o.sRev + rev - o.sCost - c[0] - c[1] - cg)}</b>`)}</div></div><p class="fine">Les charges du jour sont comptées au prorata dès maintenant ; elles sont prélevées en fin de journée.</p>`;
    }
    return h;
  };

  // =====================================================================================================
  //  ACTIONS
  // =====================================================================================================
  const A = UI.acts, T = (m, k) => UI.toast(m, k || 'info');
  const refreshAll = () => { UI.refresh(true); };
  A.ftab = a => { UI.ftab = a; UI.refresh(true); };
  A.etab = a => { UI.etab = a; UI.refresh(true); };
  A.fwage = a => { SS.G.wage = +a; UI.refresh(true); };
  A.loanTake = a => { const e = Mg.takeLoan(a); if (e) T(e, 'warn'); else T('🏦 Prêt accordé.', 'good'); UI.refresh(true); };
  A.loanPay = a => { const e = Mg.repayLoan(+a); if (e) T(e, 'warn'); else T('Prêt soldé.', 'good'); UI.refresh(true); };
  A.rstart = a => { const e = Mg.startResearch(a); if (e === 'queued') T('Ajouté à la file : le projet démarrera quand un poste se libère.', 'info'); else if (e) T(e, 'warn'); else T('🔬 Recherche lancée.', 'good'); UI.refresh(true); };
  A.runq = a => { Mg.cancelQueued(a); UI.refresh(true); };
  A.labup = () => { G = SS.G; if (G.lab >= 3) return; if (SS.spend(Mg.labCost(), 'Recherche')) { G.lab++; T('🔬 Laboratoire amélioré : niveau ' + G.lab + '.', 'good'); } else T('Fonds insuffisants.', 'bad'); UI.refresh(true); };
  A.gnight = () => { SS.G.groomNight = !SS.G.groomNight; UI.refresh(true); };
  const sel = () => UI.sel && UI.sel.obj;
  A.range = a => { Mg.setRange(sel(), +a); UI.refresh(true); };
  A.staffl = a => { sel().staffLvl = +a; UI.refresh(true); };
  A.hours = a => { sel().hours = a; UI.refresh(true); };
  A.opt = a => {
    const b = sel(), o = b.opts;
    if (a === 'spa' && !o.spa) { if (!SS.spend(Mg.spaCost(b), 'Construction')) { T('Fonds insuffisants.', 'bad'); return; } o.spa = true; T('Spa installé.', 'good'); }
    else o[a] = !o[a];
    UI.refresh(true);
  };
  A.machines = a => {
    const [id, d] = a.split(':'), b = SS.G.buildings.find(x => x.id === +id), n = +d; if (!b) return;
    if (n > 0) { if (b.machines >= 6) return; if (!SS.spend(Mg.machineCost, 'Construction')) { T('Fonds insuffisants.', 'bad'); return; } b.machines++; T('🚜 Dameuse livrée.', 'good'); }
    else { if (b.machines <= 1) return; b.machines--; SS.earn(30000, 'Revente'); }
    UI.refresh(true);
  };
  A.patrol = a => { const [id, d] = a.split(':'), b = SS.G.buildings.find(x => x.id === +id); if (!b) return; b.patrol = SS.clamp(b.patrol + (+d), 2, 12); Mg.securePistes(); UI.refresh(true); };
  // priorités de damage : une action par piste, 'gprio_<id>' (arg = 0, 1 ou 2)
  UI.act = function (a, arg, el) {
    if (a.indexOf('gprio_') === 0) { const p = SS.G.pistes.find(x => x.id === +a.slice(6)); if (p) { p.prio = +arg; p.noGroom = p.prio === 0; } UI.refresh(true); return true; }
    return false;
  };
})();
