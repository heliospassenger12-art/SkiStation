// SkiStation Web — interface : barre d'état, outils, panneaux de gestion, fiches, menu de départ
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data, B = SS.Build, R = SS.R, Sim = SS.Sim, W = SS.Weather, Net = SS.Net;
  const UI = SS.UI = { panel: null, arg: null, sel: null, acts: {} };
  const $ = id => document.getElementById(id), esc = SS.esc, eur = SS.eur;
  let G, acc = 0;

  const dot = d => '<i class="dot" style="background:' + D.DIFF[d].color + '"></i>';
  const row = (k, v, cls) => '<div class="row ' + (cls || '') + '"><span>' + k + '</span><b>' + v + '</b></div>';
  const btn = (label, act, arg, cls, dis) => '<button class="btn ' + (cls || '') + '" data-act="' + act + '"' + (arg !== undefined ? ' data-arg="' + esc(arg) + '"' : '') + (dis ? ' disabled' : '') + '>' + label + '</button>';
  const pct = v => Math.round(v * 100) + ' %';
  const stars = p => { const n = Math.round(p / 20); let s = ''; for (let i = 1; i <= 5; i++) s += '<i class="st' + (n >= i ? ' on' : '') + '">★</i>'; return s; };
  UI.H = { row, btn, dot, eur, esc, pct, stars };
  const OPEX = o => Object.keys(o).filter(k => k !== 'Construction' && k !== 'Recherche').reduce((a, k) => a + o[k], 0);

  // ---------------- initialisation ----------------
  UI.init = function () {
    document.addEventListener('click', e => { const el = e.target.closest('[data-act]'); if (el && !el.disabled) { act(el.dataset.act, el.dataset.arg, el); } });
    document.addEventListener('input', e => { const el = e.target; if (el.dataset && el.dataset.in) input(el.dataset.in, el.value, el); });
    SS.on('toast', toast);
    SS.on('build', renderRecap); SS.on('build:live', renderRecap);
    SS.on('select', s => { UI.sel = s; if (s) UI.open('inspect'); else if (UI.panel === 'inspect') UI.close(); });
    SS.on('demolish:ask', s => { UI.sel = s; B.highlight(s); UI.open('inspect'); });
    SS.on('day:end', dayReport);
    SS.on('speed', renderTop); SS.on('career', () => UI.refresh());
    SS.on('arcade', arcadeHud);
    SS.on('game:start', () => { G = SS.G; $('hud').hidden = false; $('modal').hidden = true; renderTop(); renderRecap(); UI.close(); if (!G.hints.welcome) { G.hints.welcome = 1; UI.open('help'); } });
    for (const k of ['left', 'right', 'tuck', 'brake', 'jump']) {
      const el = $('ak_' + k); if (!el) continue;
      el.addEventListener('pointerdown', e => { SS.Arcade.touch[k] = true; e.preventDefault(); }); const off = () => { SS.Arcade.touch[k] = false; };
      el.addEventListener('pointerup', off); el.addEventListener('pointerleave', off); el.addEventListener('pointercancel', off);
    }
    startMenu();
  };

  // ---------------- menu de départ ----------------
  function startMenu() {
    const has = SS.hasSave();
    $('modal').hidden = false; $('modal').className = 'start';
    $('modalBox').innerHTML =
      '<div class="logo">Ski<span>Station</span></div><p class="tag">Construisez, gérez et faites vivre votre station de ski — du premier téléski aux Jeux Olympiques.</p>' +
      '<label class="field">Nom de la station<input id="sm_name" maxlength="28" value="Mont-Hélios"></label>' +
      '<div class="modes"><button class="mode on" data-act="sm_mode" data-arg="career"><b>Carrière</b><span>Budget de 450 000 à 900 000 € selon la difficulté, recherche à débloquer, six étapes jusqu\'aux Jeux.</span></button>' +
      '<button class="mode" data-act="sm_mode" data-arg="sandbox"><b>Bac à sable</b><span>Liberté totale pour bâtir le domaine de vos rêves.</span></button></div>' +
      '<div class="smrow"><span>Difficulté</span>' + ['Détente', 'Normale', 'Exigeante'].map((n, i) => btn(n, 'sm_level', i, 'chip lvl' + (i === 1 ? ' on' : ''))).join('') + '</div>' +
      '<div class="smrow"><span>Montagne</span>' + [['Classique', 1337], ['Dents Blanches', 4242], ['Val Perdu', 9001], ['Aléatoire', 0]].map((m, i) => btn(m[0], 'sm_seed', m[1], 'chip seed' + (i === 0 ? ' on' : ''))).join('') + '</div>' +
      '<div id="sm_opts" class="opts" hidden><label><input type="checkbox" id="sm_unl" checked> Argent illimité</label><label><input type="checkbox" id="sm_all" checked> Tout le catalogue débloqué</label></div>' +
      '<div class="opts"><label><input type="checkbox" id="sm_casual"> Neige simplifiée (pas de verglas, de soupe ni de manque de neige)</label></div>' +
      '<div class="actions">' + btn('Nouvelle partie', 'sm_new', undefined, 'primary big') + (has ? btn('Continuer la sauvegarde', 'sm_load', undefined, 'big') : '') + btn('Importer un code', 'sm_import', undefined, 'big') + '</div><div id="sm_imp" hidden><textarea id="sm_code" placeholder="Collez ici un code de sauvegarde (SKI1:…)"></textarea>' + btn('Charger ce code', 'sm_importGo', undefined, 'primary') + '</div>' +
      '<p class="fine">Souris : glisser pour déplacer, clic droit pour tourner, molette pour zoomer. Tactile : un doigt déplace, deux doigts zooment et tournent.</p>';
    UI.mode = 'career'; UI.level = 1; UI.seed = 1337;
  }

  // ---------------- actions ----------------
  function act(a, arg, el) {
    G = SS.G;
    switch (a) {
      case 'sm_mode': UI.mode = arg; document.querySelectorAll('.mode').forEach(m => m.classList.toggle('on', m.dataset.arg === arg)); $('sm_opts').hidden = arg !== 'sandbox'; break;
      case 'sm_level': UI.level = +arg; document.querySelectorAll('.lvl').forEach(m => m.classList.toggle('on', m.dataset.arg === arg)); break;
      case 'sm_seed': UI.seed = +arg; document.querySelectorAll('.seed').forEach(m => m.classList.toggle('on', m.dataset.arg === arg)); break;
      case 'sm_import': $('sm_imp').hidden = !$('sm_imp').hidden; break;
      case 'sm_importGo': { const d = SS.importSave($('sm_code').value); if (d) SS.startGame(null, d); else { el.textContent = 'Code invalide — réessayer'; } break; }
      case 'export': { const code = SS.exportSave(); UI.code = code; UI.refresh(true); const ta = $('exportCode'); if (ta) { ta.value = code; ta.select(); } if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(code).then(() => toast('Code copié dans le presse-papiers.', 'good')).catch(() => toast('Sélectionnez le code et copiez-le.', 'info')); break; }
      case 'sm_new': SS.startGame({ level: UI.level, seed: UI.seed || (1 + Math.floor(Math.random() * 99999)), mode: UI.mode, name: ($('sm_name').value || 'Mont-Hélios').trim(), unlimited: UI.mode === 'sandbox' && $('sm_unl').checked, allUnlocked: UI.mode === 'sandbox' && $('sm_all').checked, casual: $('sm_casual').checked }); break;
      case 'sm_load': { const d = SS.loadData(); if (d) SS.startGame(null, d); break; }
      case 'menu': if (sure(el)) { SS.save(); SS.G.paused = true; $('hud').hidden = true; startMenu(); } break;
      case 'speed': if (arg === '0') G.paused = !G.paused; else { G.speed = +arg; G.paused = false; } renderTop(); break;
      case 'skip': if (!SS.skipToEnd()) toast('Des visiteurs sont encore attendus ou présents sur la station.', 'warn'); break;
      case 'panel': if (UI.panel === arg) UI.close(); else { B.highlight(null); UI.open(arg); } break;
      case 'close': UI.close(); B.highlight(null); break;
      case 'modalClose': $('modal').hidden = true; break;
      case 'tab': UI.tab = arg; UI.refresh(); break;
      case 'tool': { const [k, t] = arg.split(':'); B.start(k, t); if (R.coarse || innerWidth < 800) UI.close(); break; }
      case 'ease': B.setEase(+arg); break;
      case 'reach': R.showReach = !R.showReach; R.drawOverlay(); UI.refresh(); break;
      case 'width': B.setWidth(+arg); break;
      case 'b_confirm': B.confirm(); break;
      case 'b_undo': B.undo(); break;
      case 'b_rot': B.rotate(); break;
      case 'b_cancel': B.cancel(); break;
      case 'focus': { const [k, id] = arg.split(':'); const o = (k === 'lift' ? G.lifts : k === 'piste' ? G.pistes : G.buildings).find(x => x.id === +id); if (o) { UI.sel = { kind: k, obj: o }; B.highlight(UI.sel); const p = k === 'lift' ? o.a : k === 'piste' ? o.pts[Math.floor(o.pts.length / 2)] : o; SS.Cam.focus(p.x, p.z, Math.min(SS.Cam.dist, 1400)); UI.open('inspect'); } break; }
      case 'toggleOpen': { const o = UI.sel.obj; o.open = !o.open; Net.touch(); R.drawOverlay(); UI.refresh(); break; }
      case 'cannons': { const p = UI.sel.obj, c = Math.round(p.len * 55); if (SS.spend(c, 'Construction')) { p.cannons = true; R.refreshPiste(p); toast('Enneigeurs installés sur ' + p.name + '.', 'good'); } else toast('Fonds insuffisants.', 'bad'); UI.refresh(); break; }
      case 'light': { const p = UI.sel.obj, c = Math.round(p.len * 45); if (SS.spend(c, 'Construction')) { p.lit = true; R.refreshPiste(p); toast('Éclairage installé sur ' + p.name + '.', 'good'); } else toast('Fonds insuffisants.', 'bad'); UI.refresh(); break; }
      case 'nightLift': { const l = UI.sel.obj; l.night = !l.night; Net.touch(); UI.refresh(); break; }
      case 'noGroom': UI.sel.obj.noGroom = !UI.sel.obj.noGroom; UI.refresh(); break;
      case 'upgrade': { const b = UI.sel.obj, c = SS.upgradeCost(b); if (b.level < 3 && SS.spend(c, 'Construction')) { b.level++; toast('Agrandissement terminé : niveau ' + b.level + '.', 'good'); } else toast('Fonds insuffisants.', 'bad'); UI.refresh(); break; }
      case 'staff': { const b = UI.sel.obj; b.staff = SS.clamp(b.staff + (+arg), 1, 14); UI.refresh(); break; }
      case 'demolish': { const s = UI.sel; if (!sure(el)) break; const err = SS.demolish(s.kind, s.obj); if (err) toast(err, 'warn'); else { toast('Démolition effectuée.', 'info'); UI.sel = null; UI.close(); } break; }
      case 'arcade': UI.close(); B.highlight(null); SS.Arcade.start(UI.sel.obj); break;
      case 'arcadeStop': SS.Arcade.stop(); break;
      case 'loan': { const n = +arg; if (n > 0) { if (G.loan + n > 3000000 + G.prestige * 60000) { toast('La banque refuse : plafond d\'emprunt atteint.', 'warn'); break; } G.loan += n; G.money += n; } else { const r = Math.min(-n, G.loan, Math.max(0, G.money)); G.loan -= r; G.money -= r; } UI.refresh(); break; }
      case 'maint': G.maint = +arg; UI.refresh(); break;
      case 'marketing': { const c = arg === 'tv' ? 45000 : 8000; if (G.marketing[arg] > 0) break; if (SS.spend(c, 'Marketing')) { G.marketing[arg] = arg === 'tv' ? 7 : 5; toast('Campagne lancée : effet dès demain.', 'good'); } else toast('Fonds insuffisants.', 'bad'); UI.refresh(); break; }
      case 'research': { const r = D.RESEARCH.find(x => x.id === arg); if (G.research.cur) { toast('Une recherche est déjà en cours.', 'warn'); break; } if (SS.spend(r.cost, 'Recherche')) { G.research.cur = { id: r.id, left: r.days }; toast('Recherche lancée : ' + r.name + ' (' + r.days + ' jours).', 'good'); } else toast('Fonds insuffisants.', 'bad'); UI.refresh(); break; }
      case 'event': { const ev = D.EVENTS.find(x => x.id === arg); if (G.event) { toast('Un événement est déjà programmé.', 'warn'); break; } if (!ev.need(SS.stats())) { toast('Prérequis manquants : ' + ev.needTxt + '.', 'warn'); break; } if (SS.spend(ev.cost, 'Événements')) { G.event = { id: ev.id, day: G.day + 3, left: ev.days }; toast(ev.name + ' programmé(e) dans 3 jours. Préparez la station !', 'good'); } else toast('Fonds insuffisants.', 'bad'); UI.refresh(); break; }
      case 'view': if (arg === 'slope') { SS.Terrain.setViewMode(SS.Terrain.viewMode === 'slope' ? 'normal' : 'slope'); } else if (arg === 'map' || arg === 'crowd' || arg === 'snow') { R.mapMode = R.mapMode === arg ? false : arg; R.drawOverlay(); } else if (arg === 'labels') R.labelsOn = !R.labelsOn; UI.refresh(); break;
      default: if (UI.acts[a]) UI.acts[a](arg, el); else if (UI.act) UI.act(a, arg, el); break;
      case 'sound': SS.Audio.setOn(!SS.Audio.on); UI.refresh(); break;
      case 'minimap': SS.Minimap.on = !SS.Minimap.on; UI.refresh(); break;
      case 'skipSummer': if (!sure(el)) break; if (!SS.skipSummer()) toast('Attendez que la station soit vide (le soir) pour abréger l\'été.', 'warn'); UI.refresh(true); break;
      case 'save': toast(SS.save() ? '💾 Partie sauvegardée.' : 'Sauvegarde impossible dans ce navigateur.', 'info'); break;
      case 'vfollow': { const v = UI.sel.obj; SS.Cam.focus(v.x, v.z, 500); SS.Cam.follow = v; toast('Caméra attachée à ' + esc(v.name) + '. Déplacez la vue pour la libérer.', 'info'); break; }
      case 'modern': { const r = SS.modernize(UI.sel.obj, arg); if (r.err) toast(r.err, 'warn'); else toast('✅ Remontée modernisée pour ' + eur(r.cost) + '.', 'good'); UI.refresh(); break; }
      case 'choice': { SS.Events.choose(+arg); $('modal').hidden = true; G.paused = UI.wasPaused; renderTop(); break; }
    }
  }
  // confirmation intégrée à la page : un second clic sur le même bouton valide
  function sure(el) { if (el.dataset.sure) return true; el.dataset.sure = '1'; el.textContent = 'Confirmer ?'; el.classList.add('danger'); return false; }
  function input(k, val, el) {
    G = SS.G;
    if (k === 'rename') { const v = String(val).slice(0, 28); if (UI.sel && UI.sel.obj && v.trim()) { UI.sel.obj.name = v; UI.title = v; $('panelTitle').textContent = v; } return; }
    if (k === 'station') { if (String(val).trim()) G.name = String(val).slice(0, 28); return; }
    if (k === 'spass') { G.summerPass = +val; const o = $('spassOut'); if (o) o.textContent = val + ' €'; return; }
    if (k === 'night') { G.nightPrice = +val; const o = $('nightOut'); if (o) o.textContent = val + ' €'; }
    else if (k === 'pass') { G.passPrice = +val; const o = $('passOut'); if (o) o.textContent = val + ' €'; }
    else if (k === 'bprice') { UI.sel.obj.price = +val; const o = $('bpriceOut'); if (o) o.textContent = val + ' €'; }
  }

  // ---------------- barre d'état ----------------
  function renderTop() {
    G = SS.G; if (!G) return;
    const w = D.WEATHER[G.weather.type];
    $('t_name').textContent = G.name; $('t_mode').textContent = (G.mode === 'career' ? 'Carrière · ' + D.CAREER[G.career.stage].title : 'Bac à sable') + (G.summer ? ' · Été' : '');
    $('t_date').textContent = SS.dateStr(G.day) + ' · An ' + G.year; $('t_time').textContent = SS.hhmm(G.time);
    $('t_weather').textContent = w.icon + ' ' + G.weather.temp + '°'; $('t_weather').title = w.name;
    $('t_money').textContent = G.unlimited ? '∞' : eur(G.money); $('t_money').classList.toggle('neg', G.money < 0);
    $('t_vis').textContent = Sim.present + ' / ' + (Sim.expected || 0);
    $('t_sat').textContent = G.sat + ' %'; $('t_stars').innerHTML = stars(G.prestige); $('t_stars').title = 'Prestige ' + G.prestige + ' / 100';
    document.querySelectorAll('#speed .sp').forEach(b => b.classList.toggle('on', b.dataset.arg === '0' ? G.paused : !G.paused && +b.dataset.arg === G.speed));
    $('skipBtn').disabled = Sim.present > 0 || (Sim.arrivals.length > 0 && G.time < 17 * 3600);
  }

  // ---------------- conseiller ----------------
  function advice() {
    const s = SS.stats(), d = Sim.today || {}, fb = Sim.fb;
    if (B.tool) return null;
    if (!G.lifts.length) return ['🚡', 'Commencez par une remontée : <b>Construire › Remontées</b>, cliquez la gare de départ dans le <b>front de neige</b> (cercle orange) et la gare d\'arrivée plus haut, de préférence dans une zone verte.'];
    if (!s.connected) return ['⛷️', 'Reliez la remontée au <b>front de neige</b> (cercle orange) : <b>Construire › Pistes › Tracé assisté</b>, puis cliquez l\'arrivée de la remontée. Les zones vertes montrent où c\'est possible.'];
    if (G.paused) return ['▶', 'Votre domaine est prêt : lancez le temps (x1, x3 ou x8). Les premiers visiteurs arrivent vers 8 h, les remontées ouvrent à 9 h.'];
    if (G.money < 0) return ['💶', 'Trésorerie négative : empruntez dans <b>Finances</b> ou réduisez vos charges avant d\'investir.'];
    if (d.lost > 30) return ['🅿️', 'Aujourd\'hui, environ ' + d.lost + ' visiteurs n\'ont pas pu venir : parkings saturés. Construisez un parking, une gare routière ou des hébergements.'];
    const top = Object.keys(fb).sort((a, b) => fb[b] - fb[a])[0];
    if (top && fb[top] > 12) {
      const tips = { 'Files aux remontées': 'Ajoutez une remontée ou un modèle à plus fort débit.', 'Attente aux services': 'Agrandissez ou multipliez restaurants, billetterie et location.', 'Manque de restauration': 'Construisez un snack ou un restaurant, y compris en altitude.', 'Prix du forfait': 'Baissez le forfait dans Finances ou étoffez le domaine.', 'Pistes surchargées': 'Tracez de nouvelles pistes pour répartir les skieurs.', 'Qualité de neige': 'Garage à dameuses et enneigeurs améliorent la neige.', 'Pistes inadaptées au niveau': 'Il manque des pistes faciles (vertes/bleues) reliées au front de neige.', 'Sécurité': 'Construisez un poste de secours près des pistes.', 'Pannes': 'Augmentez le niveau d\'entretien dans Finances.', 'Cours de ski complets': 'Recrutez des moniteurs ou agrandissez le Jardin des Piou-Piou.', 'Bloqués en altitude': 'Chaque sommet doit avoir une piste (ou une chaîne de pistes) qui redescend au front de neige.', 'Location': 'Vérifiez le prix et l\'ouverture de la location.' };
      return ['💬', '<b>' + top + '</b> : ' + fb[top] + ' remarques aujourd\'hui. ' + (tips[top] || '')];
    }
    if (!s.food && G.visitorsToday > 30) return ['🍽️', 'Vos visiteurs n\'ont rien à manger : un snack ou un restaurant augmentera leur satisfaction et vos recettes.'];
    if (!G.summer && !s.rental) return ['🎿', 'Sans location de matériel, un quart des clients potentiels ne vient pas.'];
    if (!G.summer && !s.garage && G.pistes.length > 1) return ['🚜', 'Sans garage à dameuses, vos pistes se dégradent chaque jour.'];
    if (!s.rescue && G.visitorsToday > 80) return ['🚑', 'Aucun poste de secours : un accident ruinerait votre réputation.'];
    if (G.mode === 'career' && !G.research.active.length && G.tier < 6 && G.money > 150000) return ['🔬', 'Pensez à lancer une recherche pour débloquer de nouvelles remontées et équipements.'];
    return null;
  }

  // ---------------- récapitulatif de construction ----------------
  function renderRecap() {
    const el = $('recap'), t = B.tool; G = SS.G;
    if (!t || !G) { el.hidden = true; return; }
    el.hidden = false;
    const an = B.an || B.live; let title = '', help = '', body = '', canOk = !!(B.an && B.an.ok);
    if (t.kind === 'piste') {
      title = t.auto ? '🧭 Tracé assisté' : '⛷️ Tracé de piste'; help = t.auto && !t.ctrl.length ? 'Cliquez le point de départ (en haut, idéalement l\'arrivée d\'une remontée) : la descente jusqu\'au front de neige est calculée pour vous.' : t.ctrl.length ? (t.auto ? 'Tracé proposé : validez, ou « Annuler le point » pour recommencer.' : 'Ajoutez des points en descendant, puis validez.') : 'Cliquez le départ (en haut), puis descendez point par point. Les zones vertes mènent au front de neige.';
      if (t.auto) body += '<div class="chips">Difficulté visée ' + ['Verte', 'Bleue', 'Rouge', 'Libre'].map((n, i) => btn(n, 'ease', i, 'chip' + (t.ease === i ? ' on' : ''))).join('') + '</div>';
      if (B.autoMsg) body += '<div class="msg warn">🧭 ' + B.autoMsg + '</div>';
      body += '<div class="chips">Largeur ' + [20, 30, 40].map(w => btn(w + ' m', 'width', w, 'chip' + (t.width === w ? ' on' : ''))).join('') + '</div>';
      if (an && an.pts) {
        const a = an.pts[0], z = an.pts[an.pts.length - 1];
        body += '<div class="grid">' + row('Longueur', SS.num(an.len) + ' m') + row('Dénivelé', SS.num(an.drop) + ' m') + row('Pente moy. / forte', Math.round(an.avg * 100) + ' % / ' + Math.round(an.p90 * 100) + ' %') + row('Difficulté', dot(an.diff) + D.DIFF[an.diff].name) + row('Arbres à couper', an.trees) + row('Départ', B.describe(a.x, a.z) || '<em class="warn">non raccordé</em>') + row('Arrivée', B.describe(z.x, z.z) || '<em class="warn">non raccordée</em>') + row('Coût', eur(an.cost), 'cost') + '</div>';
      }
    } else if (t.kind === 'lift') {
      const def = D.lift(t.type); title = '🚡 ' + def.name; help = !t.a ? 'Cliquez l\'emplacement exact de la gare de départ (en bas).' : !t.b ? 'Cliquez la gare d\'arrivée (en haut). Portée max. ' + SS.maxLen(def) + ' m.' : 'Vérifiez le devis puis validez. Un nouveau clic déplace la gare d\'arrivée.';
      if (an && t.a) body += '<div class="grid">' + row('Longueur', SS.num(an.len) + ' / ' + an.maxLen + ' m', an.len > an.maxLen ? 'bad' : '') + row('Dénivelé', SS.num(an.vert) + ' m') + row('Débit', SS.num(an.capacity) + ' pers./h') + row('Durée du trajet', an.rideMin.toFixed(0) + ' min') + row('Entretien', eur(an.maint) + ' / jour') + row('Départ', B.describe(t.a.x, t.a.z) || 'isolé') + (t.b ? row('Arrivée', B.describe(t.b.x, t.b.z) || 'nouveau sommet') : '') + row('Coût', eur(an.cost), 'cost') + '</div>';
    } else if (t.kind === 'building') {
      const def = D.building(t.type); title = '🏠 ' + def.name; help = t.pos ? 'Vérifiez le devis puis validez. Un nouveau clic déplace le bâtiment.' : 'Cliquez l\'emplacement souhaité.';
      if (an) body += '<div class="grid">' + row('Altitude', SS.num(an.y) + ' m') + row('Pente du terrain', Math.round(an.slope * 100) + ' %') + row('Terrassement', eur(an.earth)) + row('Arbres à couper', an.trees) + row('Charges', eur(def.upkeep) + ' / jour') + row('Coût total', eur(an.cost), 'cost') + '</div>';
    } else if (t.kind === 'road') {
      title = '🛣️ Tracé de route'; help = 'Partez du village ou d\'une route existante. Pente max. 16 % : tracez des lacets.';
      if (an && an.len) body += '<div class="grid">' + row('Longueur', SS.num(an.len) + ' m') + row('Pente max.', Math.round(an.maxSlope * 100) + ' %', an.maxSlope > 0.16 ? 'bad' : '') + row('Arbres à couper', an.trees || 0) + row('Coût', eur(an.cost || 0), 'cost') + '</div>';
    } else if (t.kind === 'demolish') { title = '🧨 Démolition'; help = 'Cliquez une piste, une remontée, une route ou un bâtiment.'; }
    const msg = an && an.err ? '<div class="msg bad">⛔ ' + an.err + '</div>' : an && an.warn ? '<div class="msg warn">⚠️ ' + an.warn + '</div>' : '';
    const key = title + help + body + msg + canOk;
    if (el._k === key) return; el._k = key;
    el.innerHTML = '<div class="rhead"><b>' + title + '</b><span>' + help + '</span></div>' + body + msg + '<div class="actions">' +
      (t.kind !== 'demolish' ? btn('✔ Valider' + (B.an && B.an.cost ? ' — ' + eur(B.an.cost) : ''), 'b_confirm', undefined, 'primary', !canOk) + btn('↩ Annuler le point', 'b_undo') : '') +
      (t.kind === 'building' ? btn('⟳ Tourner', 'b_rot') : '') + btn('✕ Quitter', 'b_cancel') + '</div>';
  }

  // ---------------- panneaux ----------------
  const TITLES = { exploit: 'Exploitation', journal: 'Journal de la station', build: 'Construire', domain: 'Domaine skiable', finance: 'Finances', visitors: 'Visiteurs', research: 'Recherche', career: 'Carrière & événements', weather: 'Météo & neige', options: 'Options', inspect: 'Fiche', help: 'Bienvenue' };
  UI.open = function (id) { UI.panel = id; $('panel').hidden = false; UI.refresh(true); document.querySelectorAll('#toolbar .tb').forEach(b => b.classList.toggle('on', b.dataset.arg === id)); };
  UI.close = function () { UI.panel = null; $('panel').hidden = true; document.querySelectorAll('#toolbar .tb').forEach(b => b.classList.remove('on')); };
  UI.refresh = function (force) {
    if (!UI.panel || !SS.G) return; G = SS.G;
    const body = $('panelBody');
    if (!force && body.contains(document.activeElement) && document.activeElement.tagName === 'INPUT') return;
    const html = (PANELS[UI.panel] || (() => ''))();
    if (html === null) { UI.close(); return; }
    $('panelTitle').textContent = UI.panel === 'inspect' ? UI.title || 'Fiche' : TITLES[UI.panel];
    if (body._h !== html) { const st = body.scrollTop; body.innerHTML = html; body._h = html; body.scrollTop = force ? 0 : st; if (UI.panel === 'finance') drawChart(); }
  };

  const PANELS = {
    journal() {
      const hs = G.history.slice(-10).reverse();
      let h = '<h4>Derniers jours</h4>' + (hs.length ? '<div class="jtab"><div class="jrow head"><span>Jour</span><span>Visiteurs</span><span>Satisf.</span><span>Résultat</span></div>' + hs.map(x => '<div class="jrow"><span>' + SS.dateStr(x.day, !!x.su) + '</span><span>' + x.v + '</span><span>' + x.sat + ' %</span><span class="' + (x.inc - x.exp >= 0 ? 'pos' : 'neg') + '">' + eur(x.inc - x.exp) + '</span></div>').join('') + '</div>' : '<p class="fine">Aucune journée terminée pour l\'instant.</p>');
      h += '<h4>Faits marquants</h4>' + (G.log.length ? G.log.slice(0, 30).map(l => '<div class="jlog ' + l.kind + '"><small>' + SS.dateStr(l.d) + ' · ' + SS.hhmm(l.t) + '</small>' + l.msg + '</div>').join('') : '<p class="fine">Rien à signaler.</p>');
      return h;
    },
    help() {
      return '<p>Bienvenue à <b>' + esc(G.name) + '</b> ! La montagne est vierge : à vous de créer le domaine.</p><ol class="steps"><li><b>Remontée</b> — partez du <b>front de neige</b> (zone claire au-dessus du village).</li><li><b>Piste</b> — tracez de l\'arrivée de la remontée jusqu\'au front de neige. La couleur dépend du relief.</li><li><b>Lancez le temps</b> — les visiteurs arrivent, achètent leur forfait, skient, mangent, se reposent et rentrent selon leurs besoins.</li><li><b>Développez</b> — restauration, location, école de ski, dameuses, secours, hébergements, recherche…</li></ol>' +
        '<p class="fine">Astuce : <b>Options › Vue des pentes</b> colore le relief (vert, bleu, rouge, noir) pour repérer les meilleurs tracés. Le conseiller en haut de l\'écran signale la priorité du moment.</p>' + btn('C\'est parti', 'close', undefined, 'primary');
    },
    build() {
      const tab = UI.tab && ['pistes', 'lifts', 'buildings', 'roads'].indexOf(UI.tab) >= 0 ? UI.tab : 'lifts';
      let h = '<div class="tabs">' + [['lifts', 'Remontées'], ['pistes', 'Pistes'], ['buildings', 'Bâtiments'], ['roads', 'Routes & démolition']].map(t => btn(t[1], 'tab', t[0], 'tabb' + (tab === t[0] ? ' on' : ''))).join('') + '</div>';
      if (tab === 'lifts') {
        h += '<p class="fine">Palier de recherche ' + G.tier + ' / 6. Le joueur choisit l\'emplacement exact des gares : une portée dépassée est refusée, jamais corrigée.</p>';
        for (const def of D.LIFTS) {
          const ok = SS.liftUnlocked(def), cap = Math.round(def.seats * 3600 / ((def.spacing || def.maxLen) / (def.speed * SS.MOTION)));
          h += '<div class="card' + (ok ? '' : ' locked') + '" ' + (ok ? 'data-act="tool" data-arg="lift:' + def.id + '"' : '') + '><div class="ctitle"><b>' + def.name + '</b><span>' + (ok ? eur(def.cost * 0.45) + ' – ' + eur(def.cost) : '🔒 palier ' + def.tier) + '</span></div><div class="cmeta">' + def.seats + ' pl. · portée ' + SS.maxLen(def) + ' m · ~' + SS.num(cap) + ' pers./h · ' + (def.speed * 3.6).toFixed(0) + ' km/h · vent max ' + def.wind + ' km/h</div></div>';
        }
      } else if (tab === 'pistes') {
        h += '<p>Tracez librement de haut en bas. La difficulté découle du relief réellement traversé ; une piste ne peut ni remonter ni couper un téléski.</p>' + btn('🧭 Tracé assisté : cliquez le départ, le jeu trouve la descente jusqu\'au front de neige', 'tool', 'autopiste:', 'primary') + btn('⛷️ Tracer à la main', 'tool', 'piste:') + btn((R.showReach ? '☑' : '☐') + ' Carte des zones desservables', 'reach') +
          '<p class="fine">Les zones <b style="color:#46d878">vertes</b> sont celles d\'où une piste peut rejoindre le front de neige. Placez l\'arrivée de vos remontées dedans.</p>' +
          '<div class="legend">' + D.DIFF.map(d => '<span>' + dot(d.id) + d.name + '</span>').join('') + '</div><p class="fine">Enneigeurs, damage et ouverture se règlent dans la fiche de chaque piste (cliquez-la).</p>';
      } else if (tab === 'buildings') {
        let cat = '';
        for (const def of D.BUILDINGS) {
          if (def.cat !== cat) { cat = def.cat; h += '<h4>' + cat + '</h4>'; }
          const ok = SS.buildingUnlocked(def), rs = def.research && D.RESEARCH.find(r => r.id === def.research);
          h += '<div class="card' + (ok ? '' : ' locked') + '" ' + (ok ? 'data-act="tool" data-arg="building:' + def.id + '"' : '') + '><div class="ctitle"><b>' + def.name + '</b><span>' + (ok ? eur(def.cost) : '🔒 ' + (rs ? rs.name : '')) + '</span></div><div class="cmeta">' + def.desc + (def.seg !== undefined ? ' <b>Clientèle ' + D.SEG[def.seg].toLowerCase() + '.</b>' : '') + ' <i>Charges ' + eur(def.upkeep) + '/j.</i></div></div>';
        }
      } else {
        h += '<p>Les routes desservent parkings et hébergements hors du village.</p>' + btn('🛣️ Tracer une route', 'tool', 'road:', 'primary') + '<h4>Démolition</h4><p class="fine">Une infrastructure ne peut être démolie que vide : fermez-la d\'abord.</p>' + btn('🧨 Outil démolition', 'tool', 'demolish:');
      }
      return h;
    },
    domain() {
      const s = SS.stats();
      let h = '<div class="kpis"><div><b>' + s.km.toFixed(1) + ' km</b>pistes</div><div><b>' + G.lifts.length + '</b>remontées</div><div><b>' + SS.num(s.vert) + ' m</b>dénivelé</div><div><b>' + SS.num(s.topAlt || 1650) + ' m</b>point haut</div></div><h4>Remontées</h4>';
      if (!G.lifts.length) h += '<p class="fine">Aucune remontée.</p>';
      for (const l of G.lifts) {
        const stt = !l.open ? ['Fermée', 'off'] : l.windStop ? ['Vent', 'bad'] : G.time < l.brokenUntil ? ['Panne', 'bad'] : SS.liftBoarding(l) ? ['Ouverte', 'ok'] : ['Hors horaires', 'off'];
        h += '<div class="li" data-act="focus" data-arg="lift:' + l.id + '"><b>' + esc(l.name) + '</b><span class="tag ' + stt[1] + '">' + stt[0] + '</span><small>' + D.lift(l.type).name + ' · file ' + l.queue.length + ' (~' + Math.round(Sim.liftWait(l)) + ' min) · ' + l.rides + ' passages</small></div>';
      }
      h += '<h4>Pistes</h4>'; if (!G.pistes.length) h += '<p class="fine">Aucune piste.</p>';
      for (const p of G.pistes) h += '<div class="li" data-act="focus" data-arg="piste:' + p.id + '">' + dot(p.diff) + '<b>' + esc(p.name) + '</b><span class="tag ' + (SS.pisteOpen(p) ? 'ok' : 'off') + '">' + (SS.pisteOpen(p) ? 'Ouverte' : p.avaUntil > G.time ? 'Sécurisation' : 'Fermée') + '</span><small>' + SS.num(p.len) + ' m · ' + W.state(p) + ' · ' + Math.round(p.depth) + ' cm · ' + p.on + ' skieurs</small></div>';
      const bs = G.buildings.filter(b => D.building(b.type).price);
      if (bs.length) { h += '<h4>Services</h4>'; for (const b of bs) { const def = D.building(b.type); h += '<div class="li" data-act="focus" data-arg="building:' + b.id + '"><b>' + def.name + '</b><span class="tag ' + (b.open ? 'ok' : 'off') + '">' + (b.open ? b.occ + '/' + SS.bcap(b) : 'Fermé') + '</span><small>' + b.visits + ' clients · ' + eur(b.revenue) + ' aujourd\'hui' + (b.queue.length ? ' · file ' + b.queue.length : '') + '</small></div>'; } }
      return h;
    },
    finance() {
      const inc = G.ledger.inc, exp = G.ledger.exp, ti = Object.values(inc).reduce((a, b) => a + b, 0), te = OPEX(exp), d = Sim.today || { fair: 26 }, last = G.history[G.history.length - 1];
      let h = '<div class="kpis"><div><b>' + (G.unlimited ? '∞' : eur(G.money)) + '</b>trésorerie</div><div><b>' + eur(G.loan) + '</b>emprunt</div><div><b class="' + (ti - te >= 0 ? 'pos' : 'neg') + '">' + eur(ti - te) + '</b>résultat d\'exploitation</div></div>';
      h += '<canvas id="chart" width="380" height="130"></canvas>';
      if (G.summer) h += '<h4>Forfait été (VTT et piétons)</h4><div class="slider"><input id="summerPass" type="range" min="5" max="45" step="1" value="' + G.summerPass + '" data-in="spass"><b id="spassOut">' + G.summerPass + ' €</b></div><p class="fine">Juste prix estimé cet été : <b>' + Math.round(d.fair) + ' €</b>.</p>';
      h += '<h4>Forfait journée' + (G.summer ? ' d\'hiver' : '') + '</h4><div class="slider"><input type="range" min="10" max="90" step="1" value="' + G.passPrice + '" data-in="pass"><b id="passOut">' + G.passPrice + ' €</b></div><p class="fine">Juste prix estimé pour votre domaine : <b>' + Math.round(d.fair) + ' €</b>. Au-dessus, la fréquentation et la satisfaction baissent. Enfants −40 %, résidents −12 %.</p>';
      if (SS.researched('night')) h += '<h4>Forfait nocturne</h4><div class="slider"><input id="nightPrice" type="range" min="5" max="40" step="1" value="' + G.nightPrice + '" data-in="night"><b id="nightOut">' + G.nightPrice + ' €</b></div><p class="fine">Vendu en fin de journée aux skieurs qui prolongent (18 h 30 – 21 h). ' + (SS.Sim.nightReachable() ? 'Domaine nocturne prêt.' : '<span class="warn">Il faut une piste éclairée au départ d\'une remontée nocturne partant du front de neige.</span>') + '</p>';
      h += '<h4>Aujourd\'hui</h4><div class="two"><div>' + (Object.keys(inc).map(k => row(k, eur(inc[k]))).join('') || '<p class="fine">Pas encore de recettes.</p>') + '</div><div>' + (Object.keys(exp).map(k => row(k, '−' + eur(exp[k]))).join('') || '<p class="fine">Pas encore de dépenses.</p>') + '</div></div>';
      if (last) h += '<p class="fine">Hier : ' + last.v + ' visiteurs, recettes ' + eur(last.inc) + ', dépenses ' + eur(last.exp) + '.</p>';
      h += '<h4>Entretien des remontées</h4><div class="chips">' + ['Minimal', 'Normal', 'Renforcé'].map((n, i) => btn(n, 'maint', i, 'chip' + (G.maint === i ? ' on' : ''))).join('') + '</div><p class="fine">Un entretien minimal coûte moins cher mais multiplie les pannes.</p>';
      h += '<h4>Marketing</h4><div class="actions">' + btn(G.marketing.social > 0 ? 'Réseaux sociaux : ' + G.marketing.social + ' j restants' : 'Réseaux sociaux — 8 000 € (+15 %, 5 j)', 'marketing', 'social', '', G.marketing.social > 0) + btn(G.marketing.tv > 0 ? 'Télévision : ' + G.marketing.tv + ' j restants' : 'Télévision — 45 000 € (+35 %, 7 j)', 'marketing', 'tv', '', G.marketing.tv > 0) + '</div>';
      h += '<h4>Banque</h4><p class="fine">Intérêts : 0,12 % par jour sur le capital restant dû.</p><div class="actions">' + btn('+100 000 €', 'loan', 100000) + btn('+500 000 €', 'loan', 500000) + btn('Rembourser 100 000 €', 'loan', -100000, '', G.loan <= 0) + '</div>';
      return h;
    },
    visitors() {
      const d = Sim.today || {}, c = {}; for (const v of Sim.visitors) c[v.state] = (c[v.state] || 0) + 1;
      const lab = { ski: 'Sur les pistes', ride: 'Sur les remontées', queueL: 'En file aux remontées', walk: 'À pied', inS: 'Dans un service', queueS: 'En file aux services', rest: 'En pause', lesson: 'En cours de ski', hurt: 'Blessés', down: 'Au sol', gone: 'Repartis' };
      let h = '<div class="kpis"><div><b>' + Sim.present + '</b>présents</div><div><b>' + G.visitorsToday + ' / ' + (Sim.expected || 0) + '</b>arrivés</div><div><b>' + G.sat + ' %</b>satisfaction</div><div><b>' + Math.round(G.reputation) + '</b>réputation</div></div>';
      if (d.why) h += '<div class="msg warn">' + d.why + '</div>';
      else if (d.demand !== undefined) h += '<p class="fine">Demande du jour : <b>' + d.demand + '</b> visiteurs à la journée pour <b>' + d.access + '</b> places d\'accueil (parkings, cars) + <b>' + d.lodgers + '</b> résidents.' + (d.lost > 0 ? ' <span class="warn">' + d.lost + ' refoulés faute de place.</span>' : '') + '</p>';
      h += '<h4>Que font-ils ?</h4>' + (Object.keys(lab).filter(k => c[k]).map(k => row(lab[k], c[k])).join('') || '<p class="fine">Personne sur la station pour l\'instant.</p>');
      const fb = Sim.fb, ks = Object.keys(fb).sort((a, b) => fb[b] - fb[a]);
      h += '<h4>Remarques du jour</h4>' + (ks.length ? ks.map(k => row(k, fb[k], 'fbk')).join('') : '<p class="fine">Aucune plainte : bravo !</p>');
      const md = Object.keys(G.medals); if (md.length) h += '<h4>Médailles délivrées par l\'école</h4><div class="medals">' + D.MEDALS.filter(m => G.medals[m]).map(m => '<span>🏅 ' + m + ' × ' + G.medals[m] + '</span>').join('') + '</div>';
      h += '<p class="fine">Cliquez un visiteur dans la vue 3D pour ouvrir sa fiche : besoins, décision en cours et raison.</p>';
      return h;
    },
    research() {
      let h = G.allUnlocked ? '<div class="msg">Bac à sable : tout le catalogue est débloqué.</div>' : '';
      if (G.research.cur) { const r = D.RESEARCH.find(x => x.id === G.research.cur.id); h += '<div class="msg">🔬 En cours : <b>' + r.name + '</b> — ' + G.research.cur.left + ' jour(s) restants.</div>'; }
      for (const r of D.RESEARCH) {
        const done = G.research.done.indexOf(r.id) >= 0, cur = G.research.cur && G.research.cur.id === r.id, locked = r.req && G.research.done.indexOf(r.req) < 0;
        h += '<div class="card' + (done ? ' done' : locked ? ' locked' : '') + '"><div class="ctitle"><b>' + r.name + '</b><span>' + (done ? '✔ acquis' : cur ? 'en cours' : eur(r.cost) + ' · ' + r.days + ' j') + '</span></div><div class="cmeta">' + r.desc + (locked ? ' <i>Nécessite : ' + D.RESEARCH.find(x => x.id === r.req).name + '.</i>' : '') + '</div>' + (!done && !cur && !locked && !G.allUnlocked ? btn('Lancer', 'research', r.id, 'small') : '') + '</div>';
      }
      return h;
    },
    career() {
      const s = SS.stats(); let h = '<div class="kpis"><div><b>' + stars(G.prestige) + '</b>prestige ' + G.prestige + '/100</div><div><b>' + Math.round(G.reputation) + '</b>réputation</div><div><b>' + G.bestDay + '</b>record visiteurs</div></div>';
      if (G.mode === 'career') {
        if (G.career.done) h += '<div class="msg">🏅 Carrière accomplie : ' + esc(G.name) + ' est une station olympique !</div>';
        D.CAREER.forEach((st, i) => {
          if (i < G.career.stage && !(G.career.done && i === G.career.stage)) { h += '<div class="card done"><div class="ctitle"><b>' + (i + 1) + '. ' + st.title + '</b><span>✔</span></div></div>'; return; }
          if (i > G.career.stage) { h += '<div class="card locked"><div class="ctitle"><b>' + (i + 1) + '. ' + st.title + '</b><span>prime ' + eur(st.reward) + '</span></div></div>'; return; }
          h += '<div class="card cur"><div class="ctitle"><b>' + (i + 1) + '. ' + st.title + '</b><span>prime ' + eur(st.reward) + '</span></div>' + st.goals.map(g => '<div class="goal ' + (g[1](s) ? 'ok' : '') + '">' + (g[1](s) ? '✔' : '○') + ' ' + g[0] + '</div>').join('') + '</div>';
        });
      } else h += '<p class="fine">Bac à sable : pas d\'objectifs imposés. Les événements restent disponibles.</p>';
      const na = D.ACH.filter(a => G.ach[a.id]).length;
      h += '<h4>Succès — ' + na + ' / ' + D.ACH.length + '</h4><div class="achs">' + D.ACH.map(a => '<div class="ach' + (G.ach[a.id] ? ' on' : '') + '"><b>' + (G.ach[a.id] ? '🏅 ' : '🔒 ') + a.name + '</b><small>' + a.desc + '</small></div>').join('') + '</div>';
      h += '<h4>Événements</h4>';
      if (!SS.researched('events')) h += '<p class="fine">Recherchez « Événementiel sportif » pour débloquer les compétitions et les concerts. La fête du village est toujours possible.</p>';
      if (G.event) { const ev = D.EVENTS.find(e => e.id === G.event.id); h += '<div class="msg">📅 ' + ev.name + (G.event.day > G.day ? ' dans ' + (G.event.day - G.day) + ' jour(s)' : ' en cours — ' + G.event.left + ' jour(s) restants') + '.</div>'; }
      for (const ev of D.EVENTS) { if (ev.id !== 'fete' && !SS.researched('events')) continue; const ok = ev.need(s); h += '<div class="card' + (ok ? '' : ' locked') + '"><div class="ctitle"><b>' + ev.name + '</b><span>' + eur(ev.cost) + '</span></div><div class="cmeta">' + ev.days + ' jour(s) · fréquentation ×' + ev.boost + ' · prestige +' + ev.prestige + ' · ' + ev.needTxt + (G.eventsHosted[ev.id] ? ' · organisé ' + G.eventsHosted[ev.id] + ' fois' : '') + '</div>' + (ok && !G.event ? btn('Programmer', 'event', ev.id, 'small') : '') + '</div>'; }
      return h;
    },
    weather() {
      const w = G.weather, def = D.WEATHER[w.type];
      let h = '<div class="wnow"><span class="big">' + def.icon + '</span><div><b>' + def.name + '</b><br>' + w.temp + ' °C au village · vent ' + Math.round(W.windNow) + ' km/h' + (w.snow ? ' · ' + w.snow + ' cm attendus' : '') + '</div></div>';
      h += '<div class="fc">' + G.forecast.map((f, i) => '<div><small>' + SS.dateStr(G.day + 1 + i).split(' ')[0] + '</small><span>' + D.WEATHER[f.type].icon + '</span><small>' + f.temp + '°' + (f.snow ? ' · ' + f.snow + ' cm' : '') + '</small></div>').join('') + '</div>';
      h += '<div class="grid">' + row('Température à 2 500 m', Math.round(W.tempAt(2500)) + ' °C') + row('Température à 3 500 m', Math.round(W.tempAt(3500)) + ' °C') + (G.summer ? '' : row('Neige au village', Math.round(G.snowBase) + ' cm')) + row('Fréquentation', '×' + def.demand) + '</div>';
      if (G.summer) h += '<div class="msg">🌞 Saison d\'été, jour ' + (G.day + 1) + ' / ' + SS.SUMMER_DAYS + '. Pas de neige à gérer : les pistes servent de sentiers.</div><div class="actions">' + btn('⏭ Passer directement à l\'hiver', 'skipSummer') + '</div>';
      if (G.casual) h += '<div class="msg">Neige simplifiée activée : ni verglas, ni soupe, ni manque de neige.</div>';
      h += '<h4>' + (G.summer ? 'État des sentiers' : 'État des pistes') + '</h4>'; if (!G.pistes.length) h += '<p class="fine">Aucune piste.</p>';
      for (const p of G.pistes) h += '<div class="li" data-act="focus" data-arg="piste:' + p.id + '">' + dot(p.diff) + '<b>' + esc(p.name) + '</b><span class="tag ' + (p.ice > 0.45 || p.slush > 0.45 || p.snowClosed ? 'bad' : 'ok') + '">' + W.state(p) + '</span><small>' + (G.summer ? SS.num(p.len) + ' m · ' + p.passes + ' passages' : Math.round(p.depth) + ' cm · damage ' + pct(p.groomed) + (p.cannons ? ' · enneigeurs' : '')) + '</small></div>';
      if (!G.summer) h += '<p class="fine">Redoux l\'après-midi → neige soupe ; regel nocturne → verglas, sauf si la piste est damée. Après de fortes chutes, les pistes hautes ouvrent à 10 h 30 (sécurisation avalanches).</p>';
      return h;
    },
    options() {
      const T = SS.Terrain;
      return '<h4>Affichage</h4><div class="actions col">' + btn((T.viewMode === 'slope' ? '☑' : '☐') + ' Vue des pentes (vert / bleu / rouge / noir)', 'view', 'slope') + btn((R.mapMode === 'map' ? '☑' : '☐') + ' Plan des pistes coloré', 'view', 'map') + btn((R.mapMode === 'crowd' ? '☑' : '☐') + ' Carte d\'affluence des pistes', 'view', 'crowd') + btn((R.mapMode === 'snow' ? '☑' : '☐') + ' Carte de qualité de neige', 'view', 'snow') + btn((R.labelsOn ? '☑' : '☐') + ' Noms des pistes et remontées', 'view', 'labels') + btn((SS.Minimap.on ? '☑' : '☐') + ' Mini-carte', 'minimap') + btn((SS.Audio.on ? '☑' : '☐') + ' Ambiance sonore', 'sound') + '</div>' +
        '<h4>Partie</h4><label class="field">Nom de la station<input id="stationName" maxlength="28" value="' + esc(G.name) + '" data-in="station"></label><div class="actions col">' + btn('💾 Sauvegarder maintenant', 'save') + btn('📋 Exporter un code de sauvegarde', 'export') + (UI.code ? '<textarea id="exportCode" readonly></textarea><p class="fine">Copiez ce code et gardez-le : « Importer un code » au menu principal le recharge sur n\'importe quel appareil.</p>' : '') + btn('⏏ Menu principal', 'menu') + btn('❔ Rappel des bases', 'panel', 'help') + '</div>' +
        '<h4>Commandes</h4><p class="fine">Glisser : déplacer · clic droit ou Maj + glisser : tourner / incliner · molette : zoom · ZQSD/WASD et flèches : déplacer · A/E ou Q/E : tourner · Espace : pause · 1-2-3 : vitesses · R : tourner un bâtiment · Entrée : valider · Retour arrière : annuler un point · Échap : quitter l\'outil.</p>' +
        '<p class="fine">Mode arcade : ouvrez la fiche d\'une piste puis « Descendre en arcade ».</p><p class="fine">SkiStation ' + SS.VERSION + '</p>';
    },
    inspect() {
      const s = UI.sel; if (!s) return null;
      const o = s.obj;
      if (s.kind === 'building') {
        if (G.buildings.indexOf(o) < 0) return null;
        const def = D.building(o.type); UI.title = def.name;
        let h = '<p>' + def.desc + '</p><div class="grid">' + row('Niveau', o.level + ' / 3') + (def.cap ? row(def.type === 'lodging' ? 'Lits' : def.type === 'parking' ? 'Places' : def.type === 'garage' ? 'Dameuses' : def.type === 'rescue' ? 'Pisteurs' : 'Capacité', SS.bcap(o)) : '') + row('Altitude', SS.num(o.y) + ' m') + row('Charges', eur(def.upkeep * (1 + 0.4 * (o.level - 1))) + ' / j');
        if (def.price !== undefined) h += row('Clients du jour', o.visits) + row('Recettes du jour', eur(o.revenue)) + (def.type !== 'lodging' ? row('Sur place / file', o.occ + ' / ' + o.queue.length) : row('Résidents', o.guests || 0));
        if (def.type === 'ticket') h += row('Forfaits vendus', o.visits) + row('File', o.queue.length);
        if (def.type === 'rescue') h += row('En intervention', o.busy || 0);
        h += row('Raccordé à', o.node >= 0 ? (o.node === Net.hub.id ? 'Front de neige' : 'Domaine d\'altitude') : '<em class="warn">rien : inaccessible</em>') + '</div>';
        h += UI.mgmtBuilding(o, def);
        if (def.type === 'school') h += '<h4>Moniteurs</h4><div class="actions">' + btn('−', 'staff', -1) + '<b class="count">' + o.staff + '</b>' + btn('+', 'staff', 1) + '</div><p class="fine">8 élèves par moniteur en cours collectif (9 h 45 et 14 h). 190 € par moniteur et par jour. Les clients aisés demandent des cours individuels.</p>';
        h += '<div class="actions">' + btn(o.open ? 'Fermer' : 'Ouvrir', 'toggleOpen') + (def.cap && o.level < 3 ? btn('Agrandir — ' + eur(SS.upgradeCost(o)), 'upgrade') : '') + (o.starter && def.type === 'ticket' ? '' : btn('Démolir', 'demolish', undefined, 'danger')) + '</div>';
        return h;
      }
      if (s.kind === 'lift') {
        if (G.lifts.indexOf(o) < 0) return null;
        const def = D.lift(o.type); UI.title = o.name;
        const stt = !o.open ? 'Fermée par l\'exploitant' : o.windStop ? 'Arrêtée : vent trop fort' : G.time < o.brokenUntil ? 'En panne jusqu\'à ' + SS.hhmm(o.brokenUntil) : SS.liftBoarding(o) ? 'En service' : 'Hors horaires (9 h – 16 h 45)';
        return '<p>' + def.name + ' — ' + stt + '</p><div class="grid">' + row('Longueur', SS.num(o.len) + ' m') + row('Dénivelé', SS.num(o.vert) + ' m') + row('Altitudes', SS.num(o.a.y) + ' → ' + SS.num(o.b.y) + ' m') + row('Débit', SS.num(o.capacity) + ' pers./h') + row('File d\'attente', o.queue.length + ' (~' + Math.round(Sim.liftWait(o)) + ' min)') + row('À bord', o.riders || 0) + row('Passages du jour', o.rides) + row('État mécanique', pct(o.cond)) + row('Entretien', eur(o.maint) + ' / j') + row('Vent max.', def.wind + ' km/h') + '</div><div class="actions">' + btn(o.open ? 'Fermer' : 'Ouvrir', 'toggleOpen') + (SS.researched('night') ? btn(o.night ? '🌙 Nocturne : ouverte' : '🌙 Nocturne : fermée', 'nightLift') : '') + btn('Démolir', 'demolish', undefined, 'danger') + '</div>' + renameField(o) + modernList(o);
      }
      if (s.kind === 'piste') {
        if (G.pistes.indexOf(o) < 0) return null;
        UI.title = o.name;
        const con = o.edges.length && Net.from(o.nb.id, 3, 'plan').dist[Net.hub.id] < Infinity, acc2 = Net.from(Net.hub.id, 3, 'plan').dist[o.na.id] < Infinity;
        return '<p>' + dot(o.diff) + 'Piste ' + D.DIFF[o.diff].name.toLowerCase() + ' — ' + (SS.pisteOpen(o) ? 'ouverte' : o.snowClosed ? 'fermée : neige insuffisante' : o.avaUntil > G.time ? 'sécurisation avalanche jusqu\'à ' + SS.hhmm(o.avaUntil) : 'fermée') + '</p>' +
          (!acc2 ? '<div class="msg warn">Aucune remontée ne mène à son départ.</div>' : '') + (!con ? '<div class="msg warn">Son arrivée ne permet pas de redescendre au front de neige.</div>' : '') +
          '<div class="grid">' + row('Longueur', SS.num(o.len) + ' m') + row('Dénivelé', SS.num(o.drop) + ' m') + row('Pente moy. / forte', Math.round(o.avg * 100) + ' % / ' + Math.round(o.p90 * 100) + ' %') + row('Départ', SS.num(o.topAlt) + ' m') + row('Neige', W.state(o) + ' · ' + Math.round(o.depth) + ' cm') + row('Damage', pct(o.groomed)) + row('Éclairage', o.lit ? 'oui (nocturne)' : 'non') + row('Skieurs', o.on) + row('Passages du jour', o.passes) + '</div>' +
          '<div class="actions">' + btn(o.open ? 'Fermer' : 'Ouvrir', 'toggleOpen') + btn(o.noGroom ? 'Damage : désactivé (freeride)' : 'Damage : chaque soir', 'noGroom') + (o.lit ? '' : SS.researched('night') ? btn('💡 Éclairer — ' + eur(o.len * 45), 'light') : '') + (o.cannons ? '' : SS.researched('snowmaking') ? btn('Installer des enneigeurs — ' + eur(o.len * 55), 'cannons') : btn('Enneigeurs : recherche requise', 'x', undefined, '', true)) + '</div><div class="actions">' + btn('🎿 Descendre en arcade', 'arcade', undefined, 'primary') + btn('Démolir', 'demolish', undefined, 'danger') + '</div>' + renameField(o);
      }
      if (s.kind === 'road') { UI.title = 'Route'; return '<p>' + (o.fixed ? 'Route d\'accès à la station.' : 'Route de desserte.') + '</p>' + (o.fixed ? '' : '<div class="actions">' + btn('Démolir', 'demolish', undefined, 'danger') + '</div>'); }
      if (s.kind === 'visitor') {
        UI.title = o.name; const bar = (v, c) => '<div class="bar"><i style="width:' + Math.round(SS.clamp(v, 0, 1) * 100) + '%;background:' + c + '"></i></div>';
        const stl = { ski: 'Skie', ride: 'Sur une remontée', queueL: 'Dans la file' + (o.lift ? ' du ' + o.lift.name : ''), walk: 'Marche', inS: 'Dans un service', queueS: 'Fait la queue', rest: 'En pause', lesson: 'En cours de ski', hurt: 'Blessé', down: 'Se relève', gone: 'Reparti', pending: 'Pas encore arrivé' };
        return '<p>' + o.prof.name + ' · ' + D.SKILL[o.skill] + (o.lodger ? ' · résident' : ' · à la journée') + '</p><div class="grid">' + row('État', stl[o.state] || o.state) + (o.grp ? row('Groupe', esc(o.grp.name) + ' (' + o.grp.members.length + ')') : '') + row('Décision', o.decision) + row('Raison', o.reason || '—') + row('Départ prévu', SS.hhmm(o.leaveT)) + row('Temps disponible', o.remain > 0 ? Math.round(o.remain / 60) + ' min' : '—') + (o.mode ? row('Activité', o.mode === 'vtt' ? 'VTT de descente' : 'Randonnée') : '') + row('Descentes', o.runs + ' (' + o.km.toFixed(1) + ' km)') + row('Dépensé', eur(o.spent)) + '</div>' +
          '<div class="needs"><span>Faim</span>' + bar(o.hunger, '#f28a1c') + '<span>Énergie</span>' + bar(o.energy, '#35d07f') + '<span>Satisfaction</span>' + bar(o.sat, '#4fc3f7') + '</div>' +
          (o.thoughts.length ? '<h4>Pensées</h4>' + o.thoughts.map(t => '<div class="thought">« ' + esc(t) + ' »</div>').join('') : '') + '<div class="actions">' + btn('🎥 Suivre ce visiteur', 'vfollow') + '</div><p class="fine">Fiche en lecture seule : le visiteur décide seul selon l\'heure, ses besoins et le domaine.</p>';
      }
      return null;
    }
  };

  function renameField(o) { return '<h4>Nom</h4><label class="field"><input id="rename_' + o.id + '" maxlength="28" value="' + esc(o.name) + '" data-in="rename"></label>'; }
  function modernList(l) {
    let h = '';
    for (const def of D.LIFTS) {
      if (def.id === l.type || !SS.liftUnlocked(def) || def.tier < D.lift(l.type).tier) continue;
      const r = SS.modernize(l, def.id, true);
      h += '<div class="card"><div class="ctitle"><b>' + def.name + '</b><span>' + (r.err ? 'impossible' : eur(r.cost)) + '</span></div><div class="cmeta">' + (r.err ? esc(r.err) : 'Débit ' + SS.num(r.an.capacity) + ' pers./h (actuel ' + SS.num(l.capacity) + ') · trajet ' + r.an.rideMin.toFixed(0) + ' min. Reprise de 30 % de l\'ancien modèle.') + '</div>' + (r.err ? '' : btn('Moderniser', 'modern', def.id, 'small')) + '</div>';
    }
    return h ? '<h4>Moderniser sur la même ligne</h4>' + h : '';
  }
  UI.PANELS = PANELS;
  function drawChart() {
    const c = $('chart'); if (!c) return; const x = c.getContext('2d'), hs = G.history.slice(-45), Wd = c.width, H = c.height;
    x.clearRect(0, 0, Wd, H); x.font = '12px system-ui'; x.fillStyle = '#9fb0c3';
    if (hs.length < 2) { x.fillText('Le graphique apparaîtra après deux jours d\'exploitation.', 12, H / 2); return; }
    const res = hs.map(h => h.inc - h.exp), mx = Math.max(1, ...res.map(Math.abs)), bw = (Wd - 20) / hs.length, mid = H * 0.55;
    hs.forEach((h, i) => { const v = res[i] / mx * (H * 0.4); x.fillStyle = v >= 0 ? '#35d07f' : '#ff5d5d'; x.fillRect(10 + i * bw + 1, v >= 0 ? mid - v : mid, Math.max(1, bw - 2), Math.abs(v)); });
    const vm = Math.max(1, ...hs.map(h => h.v)); x.strokeStyle = '#ffd23f'; x.lineWidth = 2; x.beginPath(); hs.forEach((h, i) => { const px = 10 + i * bw + bw / 2, py = H - 8 - h.v / vm * (H - 24); i ? x.lineTo(px, py) : x.moveTo(px, py); }); x.stroke();
    x.fillStyle = '#9fb0c3'; x.fillText('Résultat/jour (barres, max ' + eur(mx) + ') · visiteurs (ligne, max ' + vm + ')', 8, 13);
  }

  // ---------------- notifications, bilans ----------------
  function toast(msg, kind) {
    const t = document.createElement('div'); t.className = 'toast ' + (kind || 'info'); t.innerHTML = msg; const root = $('toasts'); root.appendChild(t);
    while (root.children.length > 4) root.firstChild.remove();
    setTimeout(() => { t.classList.add('out'); setTimeout(() => t.remove(), 400); }, 5200);
  }
  UI.toast = toast;
  function dayReport(r) {
    const res = r.inc - r.opex;
    toast('📋 Bilan ' + r.date + ' : <b>' + r.visitors + '</b> visiteurs · satisfaction <b>' + r.sat + ' %</b> · résultat <b class="' + (res >= 0 ? 'pos' : 'neg') + '">' + (res >= 0 ? '+' : '') + eur(res) + '</b>', 'info');
    for (const n of r.notes) toast(n, 'good');
    if (r.victory || r.notes.length) {
      const fbk = Object.keys(r.fb).sort((a, b) => r.fb[b] - r.fb[a]).slice(0, 3);
      $('modal').hidden = false; $('modal').className = 'report';
      $('modalBox').innerHTML = '<h2>' + (r.victory ? '🏅 Triomphe olympique !' : 'Bilan du ' + r.date) + '</h2>' + (r.victory ? '<p>' + esc(G.name) + ' entre dans la légende des stations de ski. Vous pouvez continuer à développer votre domaine.</p>' : '') +
        r.notes.map(n => '<div class="msg">' + n + '</div>').join('') + '<div class="kpis"><div><b>' + r.visitors + '</b>visiteurs</div><div><b>' + r.sat + ' %</b>satisfaction</div><div><b class="' + (res >= 0 ? 'pos' : 'neg') + '">' + eur(res) + '</b>résultat</div></div>' +
        (fbk.length ? '<p class="fine">Principales remarques : ' + fbk.map(k => k + ' (' + r.fb[k] + ')').join(', ') + '.</p>' : '') + btn('Continuer', 'modalClose', undefined, 'primary');
      if (!r.victory) G.paused = true;
    }
    renderTop(); UI.refresh();
  }
  function arcadeHud() { const on = SS.Arcade.active; $('arcadeHud').hidden = !on; $('toolbar').hidden = on; $('topbar').hidden = on; $('speed').hidden = on; $('advice').hidden = on || !$('advice').innerHTML; if (on) { $('panel').hidden = true; $('recap').hidden = true; } }

  UI.frame = function (dt) {
    G = SS.G; if (!G) return;
    if (SS.Arcade.active) { const st = SS.Arcade.st; $('a_speed').textContent = Math.round(st.v * 3.6) + ' km/h'; $('a_score').textContent = st.score + ' pts'; $('a_time').textContent = st.t.toFixed(1) + ' s'; $('a_msg').textContent = st.msgT > 0 ? st.msg : st.offPiste ? 'Hors-piste : neige profonde' : ''; return; }
    acc += dt; if (acc < 0.33) return; acc = 0;
    const ev = SS.Events.current();
    if (ev && $('modal').hidden) {
      UI.wasPaused = G.paused; G.paused = true; $('modal').hidden = false; $('modal').className = 'report';
      $('modalBox').innerHTML = '<h2>' + ev.title + '</h2><p>' + ev.text + '</p><div class="actions col">' + ev.choices.map((c, i) => btn(c[0], 'choice', i, i ? '' : 'primary big')).join('') + '</div>';
      renderTop();
    }
    if ((R.mapMode === 'crowd' || R.mapMode === 'snow') && (UI.heatT = (UI.heatT || 0) + 1) % 6 === 0) R.drawOverlay();
    renderTop();
    const a = advice(), el = $('advice'), html = a ? '<span>' + a[0] + '</span><p>' + a[1] + '</p>' : '';
    if (el._h !== html) { el.innerHTML = html; el._h = html; el.hidden = !a; }
    if (UI.panel && UI.panel !== 'build' && UI.panel !== 'help' && UI.panel !== 'options') UI.refresh();
  };
})();
