// SkiStation Web — panneau Carrière : scénario, prestige (étoiles) et notoriété, critères X Games et olympiques,
// candidature face aux villes rivales, événements, succès
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data, UI = SS.UI, Sc = SS.Scn;
  const { row, btn, eur, esc } = UI.H;
  let G;
  const goalHtml = g => `<div class="goal ${g.ok ? 'ok' : ''}">${g.ok ? '✔' : '○'} ${g.text}${g.val ? `<small>${g.val}</small>` : ''}</div>`;
  const bar = (v, c) => `<div class="bar"><i style="width:${Math.round(SS.clamp(v, 0, 1) * 100)}%;background:${c}"></i></div>`;
  const LABP = { domaine: 'Domaine skiable (max 28)', remontees: 'Remontées et technologie (max 14)', hebergement: 'Hébergement haut de gamme (max 24)', loisirs: 'Loisirs, gastronomie, spa (max 20)', evenements: 'Compétitions accueillies (max 8)', service: 'Qualité de service (max 6)' };
  const LABF = { humeur: 'Humeur des visiteurs', qualitePrix: 'Rapport qualité / prix', frequentation: 'Fréquentation', prestige: 'Part du prestige', evenements: 'Événements réussis', marketing: 'Marketing en cours' };

  function twoGauges() {
    const parts = G.prestigeParts || {}, fp = Sc.fameParts(), t = Sc.fameTarget();
    let h = `<h4>Prestige — ${UI.starHtml(SS.prestigeStars(G.prestige))} <small>${G.prestige} / 100</small></h4>${bar(G.prestige / 100, 'var(--acc)')}`;
    h += '<p class="fine">Le <b>standing</b> de la station. De belles pistes et de bonnes remontées ne suffisent pas : il faut aussi du luxe, des loisirs variés et des remontées d\'exception.</p>';
    h += '<div class="parts">' + Object.keys(LABP).map(k => row(LABP[k], (parts[k] || 0).toFixed(1))).join('') + '</div>';
    if (G.prestigeCaps && G.prestigeCaps.length) h += G.prestigeCaps.map(c => `<div class="capmsg">⛔ Plafonné : ${c}</div>`).join('');
    h += `<h4>Notoriété — ${Math.round(G.fame)} %</h4>${bar(G.fame / 100, 'var(--blue)')}`;
    h += `<p class="fine">La <b>renommée</b> : surtout l'humeur des visiteurs et le rapport qualité/prix. Une station simple où tout le monde est content peut être très réputée. Elle évolue lentement vers <b>${Math.round(t)} %</b>.</p>`;
    h += '<div class="parts">' + Object.keys(LABF).map(k => row(LABF[k], '+' + (fp[k] || 0).toFixed(1))).join('') + row('Rapport qualité / prix (indice)', Math.round(Sc.valueForMoney()) + ' / 100') + '</div>';
    if (G.totalVisitors < 2250) h += '<p class="fine">Tant que la station a peu de clients cumulés, la notoriété reste freinée.</p>';
    return h;
  }

  function critBlock(id, title, intro) {
    const st = Sc.critState(id), n = st.filter(c => c.ok).length;
    return `<h4>${title} — ${n} / ${st.length}</h4><p class="fine">${intro}</p>${bar(n / st.length, n === st.length ? 'var(--ok)' : 'var(--blue)')}${st.map(goalHtml).join('')}`;
  }

  function bidPart() {
    const b = G.bid;
    let h = '<h4>🏛️ Candidature aux Jeux Olympiques</h4>';
    const steps = [['eval', 'Évaluation'], ['vote', 'Vote du CIO'], ['prep', 'Préparation'], ['games', 'Jour 1 des Jeux']], idx = !b ? -1 : { eval: 0, prep: 2, games: 3, done: 3, refused: -1 }[b.phase];
    h += `<div class="phase">${steps.map((p, i) => `<span class="${idx === i ? 'on' : idx > i ? 'past' : ''}">${p[1]}</span>`).join('')}</div>`;
    if (!b || b.phase === 'refused') {
      const c = Sc.bidCan();
      h += `<p class="fine">Un long processus : dépôt du dossier (${eur(Sc.BID_COST)}), ${Sc.EVAL_DAYS} jours d'évaluation face à 3 villes rivales, vote du CIO (le meilleur dossier l'emporte), puis ${Sc.PREP_DAYS} jours pour remplir <b>tout</b> le cahier des charges olympique, avec une inspection à mi-parcours. L'ouverture des Jeux (jour 1) marque la victoire.</p>`;
      if (b && b.phase === 'refused') h += `<div class="msg warn">Dernière candidature refusée${b.score !== undefined ? ' (votre dossier : ' + b.score + ' / 100)' : ''}.</div>`;
      h += `<p class="fine">Note actuelle de votre dossier : <b>${Math.round(Sc.dossierScore())} / 100</b>. Les rivaux obtiennent en général entre 74 et 90.</p>`;
      if (!c.ok) h += c.why.map(w => `<div class="msg warn">${w}</div>`).join('');
      return h + btn('📁 Déposer le dossier — ' + eur(Sc.BID_COST), 'bidStart', undefined, 'primary', !c.ok);
    }
    if (b.phase === 'eval') {
      h += `<p>La commission évalue les dossiers : vote dans <b>${Math.max(0, b.due - G.elapsed)} jour(s)</b>. Continuez à améliorer la station : la note est calculée le jour du vote.</p>`;
      h += `<div class="rivals"><span class="me">${esc(G.name)} (vous)</span><b class="me">${Math.round(Sc.dossierScore())}</b>${b.rivals.map(r => `<span>${r.name}</span><b>${Math.round(r.score)}</b>`).join('')}</div>`;
    }
    if (b.phase === 'prep') {
      const left = Math.max(0, b.due - G.elapsed);
      h += `<p>🎉 Jeux attribués. Inspection finale dans <b>${left} jour(s)</b>${b.late ? ' (reportée au début de la prochaine saison d\'hiver)' : ''}. Un seul critère manquant et le CIO retire les Jeux.</p>`;
    }
    if (b.phase === 'games' || b.phase === 'done') h += `<div class="msg">🔥 ${G.olympicsDay1 ? 'Les Jeux Olympiques ont été ouverts chez vous : partie gagnée.' : 'Inspection réussie : les Jeux commencent demain.'}</div>`;
    return h;
  }

  UI.PANELS.career = function () {
    G = SS.G; const sc = Sc.cur(), s = Sc.stats(); let h = '';
    h += `<div class="kpis"><div><b>${UI.starHtml(SS.prestigeStars(G.prestige))}</b>prestige ${G.prestige}</div><div><b>${Math.round(G.fame)} %</b>notoriété</div><div><b>${Math.round(G.reputation)}</b>réputation</div><div><b>${G.bestDay}</b>record visiteurs</div></div>`;
    if (sc) {
      h += `<div class="card cur"><div class="ctitle"><b>${sc.name}</b><span>${sc.tag}</span></div><div class="cmeta">${sc.brief}</div>`;
      const dl = Sc.daysLeft();
      if (dl !== null) h += `<div class="dl"><span>Temps restant</span><b class="${dl < 25 ? 'low' : ''}">${dl} jour(s) sur ${sc.deadline}</b></div>${bar(1 - dl / sc.deadline, dl < 25 ? 'var(--bad)' : 'var(--blue)')}`;
      h += '</div>';
      if (G.scn.failed) h += `<div class="msg bad">❌ Scénario échoué. Vous pouvez continuer à jouer sans objectif.</div>${btn('Continuer en mode libre', 'scnFree', undefined, 'primary')}`;
      if (G.scn.done) h += `<div class="msg">🏅 ${sc.victory}</div><div class="bigstars">${'★'.repeat(G.scn.stars)}<span>${'★'.repeat(3 - G.scn.stars)}</span></div>`;
      h += '<h4>Étapes</h4>';
      sc.stages.forEach((st, i) => {
        if (G.scn.done || i < G.scn.stage) { h += `<div class="card done"><div class="ctitle"><b>${i + 1}. ${st.title}</b><span>✔</span></div></div>`; return; }
        if (i > G.scn.stage || G.scn.failed) { h += `<div class="card locked"><div class="ctitle"><b>${i + 1}. ${st.title}</b><span>${st.reward ? 'prime ' + eur(st.reward) : 'victoire'}</span></div>${st.fame ? `<div class="cmeta">Notoriété visée : ${st.fame} %</div>` : ''}</div>`; return; }
        const gs = Sc.goalState(st), n = gs.filter(g => g.ok).length;
        h += `<div class="card cur"><div class="ctitle"><b>${i + 1}. ${st.title}</b><span>${st.reward ? 'prime ' + eur(st.reward) : 'victoire'}</span></div>${gs.map(goalHtml).join('')}${bar(n / gs.length, 'var(--ok)')}</div>`;
      });
    } else h += '<p class="fine">Bac à sable : pas d\'objectifs imposés. Prestige, notoriété, événements et candidature olympique restent disponibles ; l\'ouverture des Jeux vaut victoire.</p>';
    h += twoGauges();
    if (!sc || sc.bid) {
      h += critBlock('xgames', '🏂 Niveau X Games', 'Les X Games sont l\'événement le plus réputé après les Jeux. Les organiser est facultatif, mais leur niveau est le socle de toute candidature olympique.');
      h += critBlock('olympics', '🔥 Cahier des charges olympique', 'Reprend tous les critères X Games avec des seuils plus élevés, plus le stade olympique, l\'héliport et une Coupe du monde. Il doit être entièrement rempli à l\'inspection finale.');
      h += bidPart();
    }
    h += '<h4>Événements</h4>';
    if (!SS.researched('events')) h += '<p class="fine">Recherchez « Événementiel sportif » pour débloquer les compétitions, les X Games et les concerts. La fête du village est toujours possible.</p>';
    if (G.event) { const ev = D.EVENTS.find(e => e.id === G.event.id); h += `<div class="msg">📅 ${ev.name}${G.event.day > G.day ? ' dans ' + (G.event.day - G.day) + ' jour(s)' : ' en cours — ' + G.event.left + ' jour(s) restants'}.</div>`; }
    for (const ev of D.EVENTS) {
      if (ev.bid || (ev.id !== 'fete' && !SS.researched('events'))) continue;
      const ok = ev.need(s);
      h += `<div class="card${ok ? '' : ' locked'}"><div class="ctitle"><b>${ev.name}</b><span>${eur(ev.cost)}</span></div><div class="cmeta">${ev.days} jour(s) · fréquentation ×${ev.boost} · notoriété +${ev.fameGain} · ${ev.needTxt}${G.eventsHosted[ev.id] ? ' · organisé ' + G.eventsHosted[ev.id] + ' fois' : ''}</div>${ok && !G.event ? btn('Programmer', 'event', ev.id, 'small') : ''}</div>`;
    }
    const na = D.ACH.filter(a => G.ach[a.id]).length;
    h += `<h4>Succès — ${na} / ${D.ACH.length}</h4><div class="achs">${D.ACH.map(a => `<div class="ach${G.ach[a.id] ? ' on' : ''}"><b>${G.ach[a.id] ? '🏅 ' : '🔒 '}${a.name}</b><small>${a.desc}</small></div>`).join('')}</div>`;
    return h;
  };
})();

// ---------- panneau Alpine Empires : classement des compagnies, concessions, course aux Jeux ----------
(function () {
  'use strict';
  const SS = window.SS, UI = SS.UI;
  const { row, btn, eur, esc } = UI.H;
  const bar = (v, c) => `<div class="bar"><i style="width:${Math.round(SS.clamp(v, 0, 1) * 100)}%;background:${c}"></i></div>`;
  const PH = { eval: 'évaluation du CIO', prep: 'préparation des Jeux', games: 'Jeux en cours', done: 'Jeux organisés', refused: 'candidature refusée' };
  UI.PANELS.ae = function () {
    const G = SS.G; if (!G.ae) return '<p class="fine">Ce panneau concerne le mode Alpine Empires.</p>';
    const AE = SS.AE, rows = AE.standings(), holder = AE.editionHolder();
    let h = '';
    if (G.ae.winner) {
      const w = G.ae.winner === 'player' ? null : AE.rivalOf(G.ae.winner);
      h += w ? `<div class="msg ${w.ally ? '' : 'bad'}">${w.ally ? '🏅 Victoire de votre équipe' : '❌ Partie perdue'} : les Jeux Olympiques se sont ouverts à ${esc(w.name)}.</div>` : '<div class="msg">🏅 Victoire : les Jeux Olympiques se sont ouverts chez vous !</div>';
    } else h += `<p class="fine"><b>But :</b> être la première compagnie à ouvrir les Jeux Olympiques (jour 1). ${G.ae.team === 'duo' ? 'Vous faites équipe avec Valfroide : sa victoire est aussi la vôtre.' : 'Chacun pour soi.'} Vos rivaux construisent, fixent leurs prix et se disputent les mêmes clients que vous.</p>`;
    if (holder) h += `<div class="msg warn">🔥 Édition des Jeux attribuée à ${holder === 'player' ? 'votre station' : esc(AE.rivalOf(holder).name)}.</div>`;
    h += '<h4>Classement</h4>';
    const sorted = rows.slice().sort((a, b) => b.dossier - a.dossier);
    for (const r of sorted) {
      h += `<div class="card${r.you ? ' cur' : ''}" style="border-left:4px solid ${r.color}"><div class="ctitle"><b>${esc(r.name)}${r.you ? ' (vous)' : r.ally ? ' (allié)' : ''}</b><span>dossier ${Math.round(r.dossier)} / 100</span></div>`;
      if (r.motto) h += `<div class="cmeta">${esc(r.motto)}</div>`;
      h += row('Visiteurs (dernier jour)', SS.num(r.visitors)) + row('Domaine', r.km.toFixed(1) + ' km · ' + r.lifts + ' remontée(s)') + row('Trésorerie', eur(r.money));
      h += row('Notoriété · prestige', Math.round(r.fame) + ' % · ' + UI.starHtml(SS.prestigeStars(r.prestige))) + row('Événements organisés', r.hosted);
      if (!r.you) h += row('Forfait · satisfaction', eur(r.price) + ' · ' + Math.round(r.sat) + ' %');
      h += `<div class="cmeta">Niveau X Games ${Math.round(r.xg * 100)} % · cahier olympique ${Math.round(r.oly * 100)} %${r.bid ? ' · ' + (PH[r.bid] || r.bid) : ''}</div>${bar(r.oly, r.color)}</div>`;
    }
    h += '<h4>Concessions</h4><p class="fine">Vous ne pouvez construire que sur vos terrains (traits orange sur la carte). Le sommet central est protégé. Les autres secteurs appartiennent aux rivaux.</p>';
    for (const zn of AE.ZONES) {
      if (!zn.price && zn.id !== 'start') continue;
      const own = G.ae.owned.indexOf(zn.id) >= 0;
      h += `<div class="li"><b>${zn.name}</b><small>${own ? 'à vous' : eur(zn.price)}</small>${own ? '' : btn('Acheter — ' + eur(zn.price), 'aeBuy', zn.id, 'small')}</div>`;
    }
    h += '<h4>Nouvelles des rivaux</h4>' + (G.ae.news.length ? G.ae.news.slice(0, 15).map(n => `<div class="fine">J${n.d} · ${esc(n.msg)}</div>`).join('') : '<p class="fine">Rien pour l\'instant.</p>');
    return h;
  };
})();
