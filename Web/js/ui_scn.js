// SkiStation Web — panneau Carrière : scénario, objectifs, notoriété, candidature olympique, événements, succès
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data, UI = SS.UI, Sc = SS.Scn;
  const { row, btn, eur, esc, pct } = UI.H;
  let G;

  // paliers de notoriété affichés sur la jauge
  const MARKS = [[10, 'Concerts'], [18, 'Régional'], [35, 'Europe'], [50, 'X Games'], [62, 'Monde'], [70, 'Candidature'], [80, 'JO']];
  const goalHtml = g => `<div class="goal ${g.ok ? 'ok' : ''}">${g.ok ? '✔' : '○'} ${g.text}${g.val ? `<small>${g.val}</small>` : ''}</div>`;

  function famePart() {
    const f = G.fame, t = Sc.fameTarget();
    return `<h4>Notoriété — ${Math.round(f)} %</h4><div class="fameg"><i style="width:${f}%"></i>${MARKS.map(m => `<u style="left:${m[0]}%"><small>${m[1]} ${m[0]}</small></u>`).join('')}</div>` +
      `<p class="fine">La notoriété monte lentement avec le prestige, la réputation, les événements réussis et le marketing, et retombe si les clients sont mécontents. Elle tend actuellement vers <b>${Math.round(t)} %</b>. Chaque grand événement exige un palier.</p>`;
  }

  function bidPart() {
    const b = G.bid, s = Sc.stats();
    let h = '<h4>🏛️ Candidature aux Jeux Olympiques</h4>';
    const phases = [['dossier', 'Dossier'], ['prep', 'Préparation'], ['games', 'Les Jeux']], order = { dossier: 0, prep: 1, games: 2, done: 3 };
    if (!b || b.phase === 'refused') {
      const c = Sc.bidCan();
      h += `<p class="fine">Le CIO étudie les candidatures. Vous déposez un dossier (${eur(Sc.BID_COST)}, réponse sous 12 jours). S'il est accepté, vous avez 70 jours pour remplir le cahier des charges ; sinon les Jeux vous sont retirés.</p>`;
      if (b && b.phase === 'refused') h += `<div class="msg warn">Dernier dossier refusé${b.score !== undefined ? ' (noté ' + b.score + ' / 100)' : ''}.</div>`;
      h += `<div class="grid">${row('Notoriété ≥ 70 %', Math.round(G.fame) + ' %')}${row('Dossier olympique', SS.researched('olympic') ? '✔' : '✘ à rechercher')}${row('Coupe du monde organisée', s.h_worldcup >= 1 ? '✔' : '✘')}</div>`;
      if (!c.ok) h += c.why.map(w => `<div class="msg warn">${w}</div>`).join('');
      h += `<p class="fine">Chances actuelles d'acceptation : <b>${Math.round(Sc.bidChance() * 100)} %</b> (note du dossier ${Math.round(Sc.dossierScore())} / 100 ; à partir de 58 le jury s'intéresse, 86 assure l'attribution).</p>`;
      h += btn('📁 Déposer le dossier — ' + eur(Sc.BID_COST), 'bidStart', undefined, 'primary', !c.ok);
      return h;
    }
    h += `<div class="phase">${phases.map((p, i) => `<span class="${order[b.phase] === i ? 'on' : order[b.phase] > i ? 'past' : ''}">${p[1]}</span>`).join('')}</div>`;
    if (b.phase === 'dossier') h += `<p>Dossier déposé : réponse du CIO dans <b>${Math.max(0, b.due - G.elapsed)} jour(s)</b>. Chances actuelles : ${Math.round(Sc.bidChance() * 100)} % (note ${Math.round(Sc.dossierScore())} / 100). Améliorez la station d'ici là.</p>`;
    if (b.phase === 'prep') {
      const left = Math.max(0, b.due - G.elapsed), st = Sc.bidReqState();
      h += `<p>🎉 Jeux attribués ! Cahier des charges à remplir dans <b>${left} jour(s)</b>${G.summer ? ' (les Jeux se tiennent en hiver : ils démarreront au début de la prochaine saison)' : ''}.</p>` + st.map(goalHtml).join('') + `<p class="fine">${st.filter(r => r.ok).length} / ${st.length} conditions remplies. Si une seule manque à l'échéance, les Jeux sont retirés (réputation −12, notoriété −18).</p>`;
    }
    if (b.phase === 'games') h += '<div class="msg">🔥 Les Jeux Olympiques sont en cours ou imminents : maintenez une bonne satisfaction !</div>';
    if (b.phase === 'done') h += '<div class="msg">🏅 Les Jeux se sont tenus chez vous.</div>';
    return h;
  }

  UI.PANELS.career = function () {
    G = SS.G; const sc = Sc.cur(), s = Sc.stats(); let h = '';
    h += `<div class="kpis"><div><b>${Math.round(G.fame)} %</b>notoriété</div><div><b>${G.prestige}</b>prestige</div><div><b>${Math.round(G.reputation)}</b>réputation</div><div><b>${G.bestDay}</b>record visiteurs</div></div>`;
    if (sc) {
      h += `<div class="card cur"><div class="ctitle"><b>${sc.name}</b><span>${sc.tag}</span></div><div class="cmeta">${sc.brief}</div>`;
      const dl = Sc.daysLeft();
      if (dl !== null) h += `<div class="dl"><span>Temps restant</span><b class="${dl < 25 ? 'low' : ''}">${dl} jour(s) sur ${sc.deadline}</b></div><div class="bar"><i style="width:${Math.round((1 - dl / sc.deadline) * 100)}%;background:${dl < 25 ? 'var(--bad)' : 'var(--blue)'}"></i></div>`;
      h += '</div>';
      if (G.scn.failed) h += `<div class="msg bad">❌ Scénario échoué. Vous pouvez continuer à jouer sans objectif.</div>${btn('Continuer en mode libre', 'scnFree', undefined, 'primary')}`;
      if (G.scn.done) h += `<div class="msg">🏅 ${sc.victory}</div><div class="bigstars">${'★'.repeat(G.scn.stars)}<span>${'★'.repeat(3 - G.scn.stars)}</span></div>`;
      h += '<h4>Étapes</h4>';
      sc.stages.forEach((st, i) => {
        if (G.scn.done || i < G.scn.stage) { h += `<div class="card done"><div class="ctitle"><b>${i + 1}. ${st.title}</b><span>✔</span></div></div>`; return; }
        if (i > G.scn.stage || G.scn.failed) { h += `<div class="card locked"><div class="ctitle"><b>${i + 1}. ${st.title}</b><span>prime ${eur(st.reward)}</span></div>${st.fame ? `<div class="cmeta">Notoriété conseillée : ${st.fame} %</div>` : ''}</div>`; return; }
        const gs = Sc.goalState(st), n = gs.filter(g => g.ok).length;
        h += `<div class="card cur"><div class="ctitle"><b>${i + 1}. ${st.title}</b><span>prime ${eur(st.reward)}</span></div>${gs.map(goalHtml).join('')}<div class="bar" style="margin-top:6px"><i style="width:${Math.round(n / gs.length * 100)}%;background:var(--ok)"></i></div></div>`;
      });
    } else h += '<p class="fine">Bac à sable : pas d\'objectifs imposés. Notoriété, événements et candidature olympique restent disponibles.</p>';
    h += famePart();
    if (!sc || sc.bid) h += bidPart();
    // événements
    h += '<h4>Événements</h4>';
    if (!SS.researched('events')) h += '<p class="fine">Recherchez « Événementiel sportif » pour débloquer les compétitions, les X Games et les concerts. La fête du village est toujours possible.</p>';
    if (G.event) { const ev = D.EVENTS.find(e => e.id === G.event.id); h += `<div class="msg">📅 ${ev.name}${G.event.day > G.day ? ' dans ' + (G.event.day - G.day) + ' jour(s)' : ' en cours — ' + G.event.left + ' jour(s) restants'}.</div>`; }
    for (const ev of D.EVENTS) {
      if (ev.bid) continue;
      if (ev.id !== 'fete' && !SS.researched('events')) continue;
      const ok = ev.need(s), famOk = G.fame >= ev.fame;
      h += `<div class="card${ok ? '' : ' locked'}"><div class="ctitle"><b>${ev.name}</b><span>${eur(ev.cost)}</span></div><div class="cmeta">${ev.days} jour(s) · fréquentation ×${ev.boost} · notoriété +${ev.fameGain} · ${ev.needTxt}${G.eventsHosted[ev.id] ? ' · organisé ' + G.eventsHosted[ev.id] + ' fois' : ''}</div>${!famOk ? `<div class="cmeta"><i>Notoriété insuffisante (${Math.round(G.fame)} / ${ev.fame} %).</i></div>` : ''}${ok && !G.event ? btn('Programmer', 'event', ev.id, 'small') : ''}</div>`;
    }
    const na = D.ACH.filter(a => G.ach[a.id]).length;
    h += `<h4>Succès — ${na} / ${D.ACH.length}</h4><div class="achs">${D.ACH.map(a => `<div class="ach${G.ach[a.id] ? ' on' : ''}"><b>${G.ach[a.id] ? '🏅 ' : '🔒 '}${a.name}</b><small>${a.desc}</small></div>`).join('')}</div>`;
    return h;
  };
})();
