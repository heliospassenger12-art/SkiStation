// SkiStation Web — catalogues : remontées, bâtiments, recherche, carrière, événements
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data = {};

  D.DIFF = [
    { id: 0, name: 'Verte', color: '#2fae4e', hex: 0x2fae4e },
    { id: 1, name: 'Bleue', color: '#2b78e4', hex: 0x2b78e4 },
    { id: 2, name: 'Rouge', color: '#e03a32', hex: 0xe03a32 },
    { id: 3, name: 'Noire', color: '#1c1c22', hex: 0x1c1c22 }
  ];
  D.SKILL = ['Débutant', 'Intermédiaire', 'Confirmé', 'Expert'];

  // vitesse réelle en m/s ; spacing = distance entre véhicules ; kind pilote le visuel et les règles
  D.LIFTS = [
    { id: 'tow_classic', name: 'Téléski classique', kind: 'tow', tier: 0, seats: 1, maxLen: 1000, speed: 3.5, spacing: 16, cost: 42000, pylonH: 8, comfort: 0.3, wind: 95 },
    { id: 'chair_fixed_2', name: 'Télésiège fixe 2 places', kind: 'chair', tier: 0, seats: 2, maxLen: 1500, speed: 4.0, spacing: 26, cost: 80000, pylonH: 12, comfort: 0.5, wind: 70 },
    { id: 'carpet', name: 'Tapis roulant débutants', kind: 'carpet', tier: 1, seats: 1, maxLen: 200, speed: 1.2, spacing: 5, cost: 38000, pylonH: 0, comfort: 0.7, wind: 120 },
    { id: 'tow_tbar', name: 'Téléski à archets 2 places', kind: 'tow', tier: 1, seats: 2, maxLen: 1100, speed: 3.5, spacing: 17, cost: 72000, pylonH: 8, comfort: 0.3, wind: 95 },
    { id: 'chair_fixed_4', name: 'Télésiège fixe 4 places', kind: 'chair', tier: 1, seats: 4, maxLen: 1700, speed: 4.2, spacing: 30, cost: 150000, pylonH: 12, comfort: 0.55, wind: 70 },
    { id: 'chair_det_4', name: 'Télésiège débrayable 4 places', kind: 'chair', tier: 2, seats: 4, maxLen: 2200, speed: 5.5, spacing: 27, cost: 320000, pylonH: 13, comfort: 0.75, wind: 75 },
    { id: 'chair_det_6', name: 'Télésiège débrayable 6 places', kind: 'chair', tier: 3, seats: 6, maxLen: 2600, speed: 6.0, spacing: 28, cost: 520000, pylonH: 13, comfort: 0.8, wind: 75 },
    { id: 'gondola_8', name: 'Télécabine 8 places', kind: 'gondola', tier: 3, seats: 8, maxLen: 3400, speed: 6.0, spacing: 32, cost: 880000, pylonH: 16, comfort: 0.9, wind: 65 },
    { id: 'bubble_8', name: 'Télésiège à bulles 8 places', kind: 'chair', tier: 4, seats: 8, maxLen: 3000, speed: 6.0, spacing: 30, cost: 800000, pylonH: 13, comfort: 0.95, wind: 85, bubble: true },
    { id: 'gondola_10', name: 'Télécabine 10 places', kind: 'gondola', tier: 4, seats: 10, maxLen: 4000, speed: 6.5, spacing: 30, cost: 1200000, pylonH: 16, comfort: 0.95, wind: 70 },
    { id: 'cablecar', name: 'Téléphérique 80 places', kind: 'cablecar', tier: 5, seats: 80, maxLen: 5200, speed: 10, spacing: 0, cost: 2900000, pylonH: 34, comfort: 0.9, wind: 60 },
    { id: 'three_s', name: 'Télécabine 3S 30 places', kind: 'gondola', tier: 6, seats: 30, maxLen: 6000, speed: 8, spacing: 75, cost: 5100000, pylonH: 30, comfort: 1, wind: 105, big: true }
  ];
  D.lift = id => D.LIFTS.find(l => l.id === id);

  // type : rôle de simulation. cap = clients simultanés (ou lits / places). dur = minutes de jeu sur place.
  D.BUILDINGS = [
    { id: 'ticket', name: 'Billetterie', cat: 'Accueil', type: 'ticket', cost: 30000, upkeep: 260, cap: 12, r: 14, look: 'kiosk', desc: 'Vente des forfaits. Plus de guichets = moins d\'attente à l\'arrivée.' },
    { id: 'parking', name: 'Parking 200 places', cat: 'Accueil', type: 'parking', cost: 45000, upkeep: 120, cap: 200, r: 38, look: 'parking', road: true, desc: 'Accueille 200 visiteurs à la journée. Doit être desservi par une route.' },
    { id: 'bus', name: 'Gare routière', cat: 'Accueil', type: 'parking', cost: 90000, upkeep: 380, cap: 320, r: 26, look: 'bus', road: true, desc: 'Navettes et autocars : 320 visiteurs à la journée.' },
    { id: 'snack', name: 'Snack / crêperie', cat: 'Restauration', type: 'food', cost: 28000, upkeep: 330, cap: 14, price: 9, dur: 18, quality: 0.45, r: 10, look: 'kiosk', alt: true, desc: 'Petite faim, service rapide. Peut être posé en altitude.' },
    { id: 'restaurant', name: 'Restaurant self-service', cat: 'Restauration', type: 'food', cost: 85000, upkeep: 900, cap: 60, price: 17, dur: 40, quality: 0.6, r: 20, look: 'chalet', desc: 'Grande capacité au pied des pistes.' },
    { id: 'alt_restaurant', name: 'Restaurant d\'altitude', cat: 'Restauration', type: 'food', cost: 140000, upkeep: 1150, cap: 50, price: 24, dur: 45, quality: 0.8, r: 20, look: 'chalet_stone', alt: true, desc: 'Terrasse panoramique : très apprécié, à relier au domaine par piste ou remontée.' },
    { id: 'gastro', name: 'Table gastronomique', cat: 'Restauration', type: 'food', cost: 260000, upkeep: 1900, cap: 36, price: 58, dur: 70, quality: 1, r: 18, look: 'chalet_lux', research: 'luxury', prestige: 4, desc: 'Clientèle premium, fort prestige.' },
    { id: 'bar', name: 'Bar d\'après-ski', cat: 'Restauration', type: 'drink', cost: 60000, upkeep: 520, cap: 40, price: 8, dur: 30, quality: 0.6, r: 14, look: 'chalet', alt: true, desc: 'Boissons chaudes et ambiance. Redonne de l\'énergie et du moral.' },
    { id: 'rental', name: 'Location de matériel', cat: 'Commerces', type: 'rental', cost: 55000, upkeep: 420, cap: 16, price: 24, dur: 10, quality: 0.6, r: 15, look: 'shop', desc: 'Attire les visiteurs sans équipement (+35 % de demande).' },
    { id: 'shop', name: 'Magasin de sport', cat: 'Commerces', type: 'shop', cost: 50000, upkeep: 380, cap: 14, price: 32, dur: 14, quality: 0.6, r: 14, look: 'shop', desc: 'Vêtements, souvenirs et accessoires.' },
    { id: 'chalets', name: 'Résidence de chalets (40 lits)', cat: 'Hébergement', type: 'lodging', cost: 120000, upkeep: 500, cap: 40, price: 62, quality: 0.55, r: 26, look: 'chalets', road: true, desc: 'Les résidents skient plusieurs jours et consomment sur place.' },
    { id: 'hotel', name: 'Hôtel ★★★ (90 lits)', cat: 'Hébergement', type: 'lodging', cost: 300000, upkeep: 1400, cap: 90, price: 95, quality: 0.7, r: 24, look: 'hotel', road: true, desc: 'Hébergement confortable au cœur de la station.' },
    { id: 'palace', name: 'Hôtel ★★★★★ (140 lits)', cat: 'Hébergement', type: 'lodging', cost: 950000, upkeep: 4200, cap: 140, price: 240, quality: 1, r: 30, look: 'palace', road: true, research: 'luxury', prestige: 8, desc: 'Palace avec spa. Clientèle fortunée et prestige élevé.' },
    { id: 'refuge', name: 'Refuge d\'altitude (20 lits)', cat: 'Hébergement', type: 'lodging', cost: 90000, upkeep: 350, cap: 20, price: 70, quality: 0.6, r: 13, look: 'chalet_stone', alt: true, research: 'nordic', prestige: 2, desc: 'Nuit en montagne pour randonneurs et skieurs de randonnée.' },
    { id: 'school', name: 'École de ski', cat: 'École & enfants', type: 'school', cost: 70000, upkeep: 300, cap: 6, price: 42, r: 16, look: 'school', desc: 'Cours collectifs et individuels. Chaque moniteur encadre 8 élèves.' },
    { id: 'pioupiou', name: 'Jardin des Piou-Piou', cat: 'École & enfants', type: 'kids', cost: 55000, upkeep: 260, cap: 30, price: 36, r: 34, look: 'garden', desc: 'Espace enfants clôturé avec tapis et arches. Indispensable aux familles.' },
    { id: 'rescue', name: 'Poste de secours', cat: 'Sécurité', type: 'rescue', cost: 60000, upkeep: 640, cap: 4, r: 12, look: 'rescue', alt: true, desc: '4 pisteurs-secouristes. Plus ils sont proches, plus l\'intervention est rapide.' },
    { id: 'heliport', name: 'Héliport de secours', cat: 'Sécurité', type: 'heli', cost: 280000, upkeep: 1500, cap: 1, r: 22, look: 'heli', research: 'heli', prestige: 3, desc: 'Évacuation héliportée des accidents graves.' },
    { id: 'garage', name: 'Garage à dameuses', cat: 'Exploitation', type: 'garage', cost: 110000, upkeep: 700, cap: 2, r: 20, look: 'garage', desc: '2 dameuses : environ 9 km de pistes préparées par nuit.' },
    { id: 'snowpark', name: 'Snowpark', cat: 'Loisirs', type: 'activity', cost: 90000, upkeep: 450, cap: 30, price: 0, dur: 35, quality: 0.8, r: 40, look: 'snowpark', alt: true, research: 'snowpark', attract: 0.05, fun: 0.10, tags: 'sportif', desc: 'Modules et big air. Attire les jeunes et les sportifs.' },
    { id: 'luge', name: 'Piste de luge', cat: 'Loisirs', type: 'activity', cost: 48000, upkeep: 220, cap: 24, price: 9, dur: 30, quality: 0.7, r: 26, look: 'luge', attract: 0.03, fun: 0.08, tags: 'famille', desc: 'Idéal pour les familles et l\'après-ski.' },
    { id: 'rink', name: 'Patinoire', cat: 'Loisirs', type: 'activity', cost: 120000, upkeep: 520, cap: 40, price: 11, dur: 40, quality: 0.7, r: 28, look: 'rink', attract: 0.03, fun: 0.07, tags: 'famille', desc: 'Activité de fin de journée au village.' },
    { id: 'nordic', name: 'Centre nordique', cat: 'Loisirs', type: 'activity', cost: 80000, upkeep: 380, cap: 40, price: 14, dur: 80, quality: 0.7, r: 22, look: 'chalet', research: 'nordic', attract: 0.04, fun: 0.07, tags: 'contemplatif', summer: 60, desc: 'Ski de fond, raquettes et randonnée.' },
    { id: 'dogs', name: 'Chiens de traîneau', cat: 'Loisirs', type: 'activity', cost: 65000, upkeep: 420, cap: 12, price: 38, dur: 45, quality: 0.85, r: 22, look: 'dogs', research: 'nordic', attract: 0.03, fun: 0.10, tags: 'famille', desc: 'Balades en traîneau très demandées.' },
    { id: 'paraglide', name: 'Base de parapente', cat: 'Loisirs', type: 'activity', cost: 70000, upkeep: 350, cap: 8, price: 75, dur: 40, quality: 0.9, r: 16, look: 'paraglide', alt: true, research: 'summer', attract: 0.03, fun: 0.12, tags: 'sportif', summer: 90, prestige: 1, desc: 'Vols biplace depuis les hauteurs.' },
    { id: 'spa', name: 'Centre aqualudique & spa', cat: 'Loisirs', type: 'activity', cost: 420000, upkeep: 1700, cap: 80, price: 26, dur: 70, quality: 0.9, r: 30, look: 'spa', research: 'luxury', attract: 0.06, fun: 0.10, tags: 'contemplatif', summer: 150, prestige: 3, desc: 'Détente après le ski, ouvert toute l\'année.' },
    { id: 'culture', name: 'Musée de la montagne', cat: 'Loisirs', type: 'activity', cost: 150000, upkeep: 480, cap: 40, price: 9, dur: 45, quality: 0.7, r: 20, look: 'culture', research: 'summer', attract: 0.02, fun: 0.05, tags: 'contemplatif', summer: 70, prestige: 2, desc: 'Activité culturelle et familiale.' },
    { id: 'stage', name: 'Scène de concerts', cat: 'Loisirs', type: 'activity', cost: 180000, upkeep: 600, cap: 150, price: 12, dur: 60, quality: 0.8, r: 26, look: 'stage', research: 'events', attract: 0.04, fun: 0.10, tags: 'fetard', prestige: 2, desc: 'Concerts d\'après-ski et festivals.' },
    { id: 'stadium', name: 'Stade de slalom', cat: 'Compétition', type: 'stadium', cost: 350000, upkeep: 900, cap: 0, r: 30, look: 'stadium', alt: true, research: 'events', prestige: 6, desc: 'Aire d\'arrivée, tribune et chronométrage. Requis pour les compétitions.' },
    { id: 'olympic', name: 'Stade olympique', cat: 'Compétition', type: 'olympic', cost: 4500000, upkeep: 6000, cap: 0, r: 60, look: 'olympic', research: 'olympic', prestige: 15, road: true, desc: 'Cérémonies, tremplins et village des athlètes. Requis pour les Jeux.' }
  ];
  D.building = id => D.BUILDINGS.find(b => b.id === id);

  D.RESEARCH = [
    { id: 'tier1', name: 'Remontées — palier 1', cost: 25000, days: 2, desc: 'Tapis roulant, téléski à archets, télésiège fixe 4 places.', tier: 1 },
    { id: 'tier2', name: 'Remontées — palier 2', cost: 70000, days: 3, desc: 'Télésiège débrayable 4 places.', tier: 2, req: 'tier1' },
    { id: 'tier3', name: 'Remontées — palier 3', cost: 150000, days: 4, desc: 'Débrayable 6 places et télécabine 8 places.', tier: 3, req: 'tier2' },
    { id: 'tier4', name: 'Remontées — palier 4', cost: 300000, days: 5, desc: 'Télésiège à bulles 8 places, télécabine 10 places.', tier: 4, req: 'tier3' },
    { id: 'tier5', name: 'Remontées — palier 5', cost: 600000, days: 6, desc: 'Téléphérique 80 places : franchit les grands vides.', tier: 5, req: 'tier4' },
    { id: 'tier6', name: 'Remontées — palier 6', cost: 1200000, days: 8, desc: 'Télécabine 3S, insensible au vent.', tier: 6, req: 'tier5' },
    { id: 'snowmaking', name: 'Neige de culture', cost: 60000, days: 3, desc: 'Permet d\'équiper les pistes d\'enneigeurs.' },
    { id: 'winch', name: 'Dameuses à treuil', cost: 90000, days: 3, desc: 'Damage des pistes noires et +40 % de rendement.', req: 'snowmaking' },
    { id: 'handsfree', name: 'Forfaits mains-libres', cost: 50000, days: 2, desc: 'Embarquement plus fluide : +12 % de débit sur toutes les remontées.' },
    { id: 'padding', name: 'Protections de pylônes', cost: 35000, days: 2, desc: 'Matelas et filets : -30 % d\'accidents.' },
    { id: 'gazex', name: 'Déclencheurs d\'avalanches', cost: 110000, days: 4, desc: 'Sécurisation rapide : plus d\'ouverture retardée après les grosses chutes.' },
    { id: 'heli', name: 'Secours héliporté', cost: 80000, days: 3, desc: 'Débloque l\'héliport de secours.', req: 'padding' },
    { id: 'snowpark', name: 'Freestyle', cost: 40000, days: 2, desc: 'Débloque le snowpark.' },
    { id: 'nordic', name: 'Activités nordiques', cost: 45000, days: 2, desc: 'Centre nordique, chiens de traîneau, refuge.' },
    { id: 'luxury', name: 'Hôtellerie de prestige', cost: 180000, days: 5, desc: 'Palace, table gastronomique, spa.' },
    { id: 'events', name: 'Événementiel sportif', cost: 120000, days: 4, desc: 'Stade de slalom, scène de concerts et calendrier d\'événements.' },
    { id: 'summer', name: 'Saison d\'été', cost: 100000, days: 4, desc: 'Parapente, musée, et recettes estivales pendant l\'intersaison.' },
    { id: 'olympic', name: 'Dossier olympique', cost: 900000, days: 10, desc: 'Débloque le stade olympique et la candidature aux Jeux.', req: 'events' }
  ];

  // étapes de carrière : check(G, stats) -> [fait, texte] par objectif
  D.CAREER = [
    { title: 'Premiers flocons', reward: 60000, goals: [
      ['Construire une remontée', s => s.lifts >= 1], ['Tracer une piste reliée au front de neige', s => s.connected >= 1], ['Accueillir 40 visiteurs en une journée', s => s.bestDay >= 40]] },
    { title: 'Station familiale', reward: 120000, goals: [
      ['2 pistes vertes ou bleues ouvertes', s => s.easy >= 2], ['Un point de restauration', s => s.food >= 1], ['Une location de matériel', s => s.rental >= 1], ['150 visiteurs en une journée', s => s.bestDay >= 150], ['Satisfaction ≥ 60 %', s => s.sat >= 60]] },
    { title: 'Station reconnue', reward: 250000, goals: [
      ['École de ski et Jardin des Piou-Piou', s => s.school >= 1 && s.kids >= 1], ['6 km de pistes', s => s.km >= 6], ['Un garage à dameuses et un poste de secours', s => s.garage >= 1 && s.rescue >= 1], ['100 lits touristiques', s => s.beds >= 100], ['400 visiteurs en une journée', s => s.bestDay >= 400]] },
    { title: 'Station sportive', reward: 500000, goals: [
      ['Une piste rouge et une piste noire', s => s.red >= 1 && s.black >= 1], ['Un stade de slalom', s => s.stadium >= 1], ['Organiser une compétition', s => s.eventsHosted >= 1], ['Prestige ≥ 45', s => s.prestige >= 45], ['700 visiteurs en une journée', s => s.bestDay >= 700]] },
    { title: 'Grande station', reward: 1000000, goals: [
      ['Une remontée arrivant au-dessus de 3 000 m', s => s.topAlt >= 3000], ['15 km de pistes', s => s.km >= 15], ['500 lits touristiques', s => s.beds >= 500], ['Accueillir une Coupe du monde', s => s.worldCup >= 1], ['Prestige ≥ 70', s => s.prestige >= 70]] },
    { title: 'Station olympique', reward: 3000000, goals: [
      ['Stade olympique et héliport', s => s.olympic >= 1 && s.heli >= 1], ['900 lits touristiques', s => s.beds >= 900], ['Prestige ≥ 88', s => s.prestige >= 88], ['Organiser les Jeux Olympiques d\'hiver', s => s.olympics >= 1]] }
  ];

  D.EVENTS = [
    { id: 'fete', name: 'Fête du village', cost: 15000, boost: 1.25, prestige: 1, days: 1, need: s => true, needTxt: 'Aucun prérequis' },
    { id: 'concert', name: 'Festival de concerts', cost: 60000, boost: 1.4, prestige: 2, days: 2, need: s => s.stage >= 1, needTxt: 'Scène de concerts' },
    { id: 'regional', name: 'Course régionale', cost: 40000, boost: 1.3, prestige: 3, days: 1, comp: true, need: s => s.stadium >= 1 && s.red >= 1, needTxt: 'Stade de slalom + piste rouge' },
    { id: 'europe', name: 'Coupe d\'Europe', cost: 150000, boost: 1.5, prestige: 6, days: 2, comp: true, need: s => s.stadium >= 1 && s.black >= 1 && s.prestige >= 40, needTxt: 'Stade, piste noire, prestige 40' },
    { id: 'worldcup', name: 'Coupe du monde', cost: 500000, boost: 1.8, prestige: 10, days: 3, comp: true, need: s => s.stadium >= 1 && s.black >= 1 && s.prestige >= 60 && s.beds >= 300, needTxt: 'Stade, piste noire, prestige 60, 300 lits' },
    { id: 'olympics', name: 'Jeux Olympiques d\'hiver', cost: 3000000, boost: 2.2, prestige: 25, days: 6, comp: true, need: s => s.olympic >= 1 && s.heli >= 1 && s.prestige >= 85 && s.beds >= 900, needTxt: 'Stade olympique, héliport, prestige 85, 900 lits' }
  ];

  D.WEATHER = {
    sun: { name: 'Grand soleil', icon: '☀️', demand: 1.15, sky: 0x7db9ee, snow: 0, light: 1 },
    cloud: { name: 'Nuageux', icon: '⛅', demand: 1.0, sky: 0xa9bfd3, snow: 0, light: 0.8 },
    snow: { name: 'Chutes de neige', icon: '🌨️', demand: 0.85, sky: 0xb9c3cf, snow: 12, light: 0.65 },
    heavy: { name: 'Fortes chutes', icon: '❄️', demand: 0.6, sky: 0xaab2bc, snow: 35, light: 0.5 },
    storm: { name: 'Tempête', icon: '🌬️', demand: 0.4, sky: 0x8d959f, snow: 20, light: 0.45, wind: 60 },
    thaw: { name: 'Redoux', icon: '🌦️', demand: 0.8, sky: 0x9fb4c4, snow: 0, light: 0.8, warm: 7 },
    frost: { name: 'Grand froid', icon: '🥶', demand: 0.9, sky: 0x86bdf0, snow: 0, light: 1, warm: -9 }
  };

  D.PROFILES = [
    { id: 'famille', name: 'Famille', w: 0.30, skill: [0.45, 0.4, 0.13, 0.02], spend: 0.9, tags: 'famille' },
    { id: 'sportif', name: 'Sportif', w: 0.25, skill: [0.02, 0.2, 0.48, 0.3], spend: 0.9, tags: 'sportif' },
    { id: 'contemplatif', name: 'Contemplatif', w: 0.20, skill: [0.2, 0.55, 0.22, 0.03], spend: 1.3, tags: 'contemplatif' },
    { id: 'fetard', name: 'Fêtard', w: 0.15, skill: [0.15, 0.45, 0.32, 0.08], spend: 1.2, tags: 'fetard' },
    { id: 'enfant', name: 'Enfant', w: 0.10, skill: [0.8, 0.2, 0, 0], spend: 0.4, tags: 'famille', child: true }
  ];
  D.MEDALS = ['Piou-Piou', 'Ourson', 'Flocon', '1re Étoile', '2e Étoile', '3e Étoile', 'Étoile de Bronze', 'Étoile d\'Or', 'Flèche', 'Chamois'];

  D.FIRST = ['Léa', 'Hugo', 'Emma', 'Lucas', 'Chloé', 'Nathan', 'Manon', 'Théo', 'Camille', 'Enzo', 'Sarah', 'Louis', 'Inès', 'Jules', 'Zoé', 'Tom', 'Jade', 'Paul', 'Lina', 'Noah', 'Anna', 'Maël', 'Clara', 'Rayan', 'Eva', 'Arthur', 'Lou', 'Sacha', 'Nina', 'Victor', 'Alice', 'Gabin', 'Romane', 'Axel', 'Margot', 'Yanis', 'Élise', 'Bastien', 'Hélène', 'Marc', 'Sophie', 'Pierre', 'Julie', 'Karim', 'Mei', 'Olga', 'Sven', 'Marta', 'Luca', 'Ingrid'];
  D.LAST = ['Martin', 'Bernard', 'Favre', 'Blanc', 'Roche', 'Perrin', 'Morel', 'Girard', 'Roux', 'Fournier', 'Mercier', 'Dupont', 'Lambert', 'Bonnet', 'Chevalier', 'Rey', 'Collomb', 'Gaillard', 'Besson', 'Charvet', 'Mollard', 'Ducroz', 'Simond', 'Payot', 'Müller', 'Rossi', 'Nilsson', 'Tanaka', 'Smith', 'Garcia'];
  D.PISTE_NAMES = ['Les Marmottes', 'Chamois', 'Combe Blanche', 'Le Lièvre', 'Edelweiss', 'Les Mélèzes', 'Grand Tétras', 'L\'Hermine', 'Le Couloir', 'Les Rhodos', 'Plan du Lac', 'Les Bouquetins', 'La Face', 'Gentiane', 'Le Mur', 'Belvédère', 'Les Arolles', 'Solaire', 'La Forêt', 'Glacier Bleu', 'Les Myrtilles', 'Le Gypaète', 'La Cascade', 'Les Crêtes', 'Les Lutins', 'Aigle Royal', 'La Combe', 'Les Écureuils', 'Perce-Neige', 'Le Renard', 'Lagopède', 'Les Génépis', 'La Moraine', 'L\'Envers', 'Le Stade', 'Boulevard des Alpages', 'Les Sources', 'Le Névé', 'Casse-Noisettes', 'Les Séracs'];
  D.LIFT_NAMES = ['Front de Neige', 'Les Alpages', 'Bellecôte', 'Les Aiguilles', 'Roc Noir', 'Plan Soleil', 'Grande Combe', 'Les Chalets', 'Col du Vent', 'Tête Blanche', 'Le Signal', 'Les Glaciers', 'Pierre Grise', 'Mont Joly', 'La Vizelle', 'Les Écrins', 'Dôme', 'La Loze', 'L\'Arpette', 'Pointe Percée'];
})();
