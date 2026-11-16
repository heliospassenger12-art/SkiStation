// SkiStation Web — événements aléatoires du matin : une situation, deux choix, des conséquences visibles
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data;
  const E = SS.Events = {};
  const pistes = f => SS.G.pistes.filter(f);

  // chaque entrée : test d'éligibilité, texte, choix [libellé, effet -> message]
  const LIST = [
    { id: 'influencer', ok: G => G.lifts.length > 0, title: '📱 Une influenceuse ski', text: 'Une créatrice suivie par 800 000 personnes propose de filmer sa journée chez vous contre des forfaits gratuits pour son équipe.',
      choices: [['Accepter (forfaits offerts)', G => { G.reputation = Math.min(100, G.reputation + 3); G.marketing.social = Math.max(G.marketing.social, 3); return 'Vidéo publiée : réputation +3 et effet « réseaux sociaux » pendant 3 jours.'; }],
                ['Refuser poliment', G => 'Elle ira tourner chez la concurrence.']] },
    { id: 'strike', ok: G => G.lifts.length >= 2, title: '✊ Préavis de grève', text: 'Les conducteurs de remontées réclament une prime de froid. Sans accord, ils menacent d\'arrêter les installations jusqu\'à 11 h.',
      choices: [['Verser la prime (12 000 €)', G => { SS.charge(12000, 'Salaires'); return 'Accord trouvé, les remontées tournent normalement.'; }],
                ['Refuser', G => { for (const l of G.lifts) l.brokenUntil = Math.max(l.brokenUntil, 11 * 3600); G.reputation = Math.max(0, G.reputation - 2); SS.Net.touch(); return 'Grève : toutes les remontées arrêtées jusqu\'à 11 h. Réputation −2.'; }]] },
    { id: 'avalanche', ok: G => pistes(p => p.topAlt > 2400).length > 0 && G.weather.snow > 5, title: '⚠️ Risque d\'avalanche', text: 'Le chef pisteur signale une plaque instable au-dessus des pistes hautes après la nuit de neige.',
      choices: [['Fermer les pistes hautes jusqu\'à 12 h', G => { for (const p of pistes(p => p.topAlt > 2400)) p.avaUntil = 12 * 3600; SS.Net.touch(); return 'Pistes hautes fermées ce matin, aucun risque pris.'; }],
                ['Garder ouvert', G => { if (Math.random() < 0.35) { G.reputation = Math.max(0, G.reputation - 8); const p = pistes(p => p.topAlt > 2400)[0]; p.avaUntil = 15 * 3600; SS.Net.touch(); return '❄️ Une coulée a touché ' + p.name + ' : pas de victime, mais la presse s\'en empare. Réputation −8.'; } return 'La plaque a tenu. Journée normale.'; }]] },
    { id: 'subsidy', ok: G => G.mode === 'career' && G.passPrice > 18, title: '🏛️ Aide de la région', text: 'La région offre une subvention si vous baissez le forfait journée de 3 € pour soutenir l\'accès au ski.',
      choices: [['Accepter (+90 000 €, forfait −3 €)', G => { G.money += 90000; G.passPrice -= 3; return 'Subvention versée. Forfait ramené à ' + G.passPrice + ' €.'; }],
                ['Décliner', G => 'Vous gardez la main sur vos tarifs.']] },
    { id: 'team', ok: G => pistes(p => p.diff >= 2).length > 0, title: '🎿 Équipe nationale', text: 'L\'équipe de France de slalom voudrait privatiser une de vos pistes rouges ou noires pour s\'entraîner aujourd\'hui.',
      choices: [['Privatiser la piste (+35 000 €)', G => { const p = pistes(p => p.diff >= 2).sort((a, b) => b.diff - a.diff)[0]; p.avaUntil = 16.75 * 3600; SS.Net.touch(); G.money += 35000; G.reputation = Math.min(100, G.reputation + 1); return p.name + ' réservée aux athlètes aujourd\'hui. +35 000 €, réputation +1.'; }],
                ['Refuser', G => 'Les athlètes iront ailleurs.']] },
    { id: 'power', ok: G => G.lifts.length > 0, title: '⚡ Panne électrique', text: 'Une ligne haute tension est tombée cette nuit. EDF annonce un retour du courant vers 11 h 30.',
      choices: [['Louer des groupes électrogènes (18 000 €)', G => { SS.charge(18000, 'Remontées'); return 'Groupes installés : ouverture à l\'heure.'; }],
                ['Attendre le courant', G => { for (const l of G.lifts) l.brokenUntil = Math.max(l.brokenUntil, 11.5 * 3600); SS.Net.touch(); return 'Remontées arrêtées jusqu\'à 11 h 30.'; }]] },
    { id: 'film', ok: G => G.pistes.length >= 2, title: '🎬 Tournage de film', text: 'Une production cherche un décor alpin pour une scène de poursuite et propose de louer le domaine pour la matinée.',
      choices: [['Accepter (+45 000 €)', G => { G.money += 45000; G.reputation = Math.min(100, G.reputation + 1); for (const l of G.lifts) l.brokenUntil = Math.max(l.brokenUntil, 10 * 3600); SS.Net.touch(); return 'Tournage jusqu\'à 10 h : ouverture retardée, +45 000 €.'; }],
                ['Refuser', G => 'Vos skieurs profitent de la première trace.']] },
    { id: 'inspection', ok: G => G.lifts.some(l => l.cond < 0.8), title: '🔎 Contrôle technique', text: 'Le service des remontées mécaniques annonce une inspection surprise. Certaines installations sont fatiguées.',
      choices: [['Remise à niveau immédiate', G => { let c = 0; for (const l of G.lifts) { c += Math.round((1 - l.cond) * l.cost * 0.08); l.cond = 1; } SS.charge(c, 'Remontées'); return 'Toutes les remontées remises à neuf pour ' + SS.eur(c) + '.'; }],
                ['Passer le contrôle tel quel', G => { const bad = G.lifts.filter(l => l.cond < 0.75); for (const l of bad) l.brokenUntil = 13 * 3600; SS.Net.touch(); return bad.length ? bad.length + ' remontée(s) arrêtée(s) jusqu\'à 13 h par l\'inspecteur.' : 'Contrôle réussi de justesse.'; }]] },
    { id: 'chef', ok: G => G.buildings.some(b => D.building(b.type).type === 'food'), title: '👨‍🍳 Chef étoilé', text: 'Un chef réputé propose une résidence d\'une semaine dans votre restaurant.',
      choices: [['L\'accueillir (25 000 €)', G => { if (!SS.spend(25000, 'Marketing')) return 'Fonds insuffisants : le chef décline.'; G.reputation = Math.min(100, G.reputation + 4); return 'La presse gastronomique en parle : réputation +4.'; }],
                ['Décliner', G => 'Le restaurant garde sa carte habituelle.']] }
  ];

  // appelé chaque matin : environ une chance sur quatre, jamais deux fois de suite le même
  E.morning = function () {
    const G = SS.G; if (G.pending || G.day < 2 || Math.random() > 0.27) return;
    const pool = LIST.filter(e => e.id !== G.lastEvent && e.ok(G)); if (!pool.length) return;
    const e = SS.pick(pool); G.pending = e.id; G.lastEvent = e.id;
  };
  E.current = () => SS.G && SS.G.pending ? LIST.find(e => e.id === SS.G.pending) : null;
  E.choose = function (i) {
    const G = SS.G, e = E.current(); if (!e) return;
    const msg = e.choices[i][1](G); G.pending = null;
    SS.log(e.title + ' — ' + msg, 'info');
  };
})();
