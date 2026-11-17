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
    { id: 'three_s', name: 'Télécabine 3S 30 places', kind: 'gondola', tier: 6, seats: 30, maxLen: 6000, speed: 8, spacing: 75, cost: 5100000, pylonH: 30, comfort: 1, wind: 105, big: true },
    { id: 'funicular', name: 'Funiculaire 120 places', kind: 'funi', tier: 6, seats: 120, maxLen: 4500, speed: 10, spacing: 0, cost: 5600000, pylonH: 0, comfort: 1, wind: 999 }
  ];
  D.lift = id => D.LIFTS.find(l => l.id === id);

  // type : rôle de simulation. cap = clients simultanés (ou lits / places). dur = minutes de jeu sur place.
  D.BUILDINGS = [
    { id: 'ticket', name: 'Billetterie', cat: 'Accueil', type: 'ticket', cost: 30000, upkeep: 260, cap: 16, r: 14, look: 'kiosk', desc: 'Vente des forfaits. Plus de guichets = moins d\'attente à l\'arrivée.' },
    { id: 'parking', name: 'Parking 200 places', cat: 'Accueil', type: 'parking', cost: 45000, upkeep: 120, cap: 200, r: 38, look: 'parking', road: true, desc: 'Accueille 200 visiteurs à la journée. Doit être desservi par une route.' },
    { id: 'bus', name: 'Gare routière', cat: 'Accueil', type: 'parking', cost: 90000, upkeep: 380, cap: 320, r: 26, look: 'bus', road: true, desc: 'Navettes et autocars : 320 visiteurs à la journée.' },
    { id: 'snack', name: 'Snack / crêperie', cat: 'Restauration', type: 'food', cost: 28000, upkeep: 330, cap: 14, price: 9, dur: 18, quality: 0.45, r: 10, look: 'kiosk', alt: true, desc: 'Petite faim, service rapide. Peut être posé en altitude.' },
    { id: 'restaurant', name: 'Restaurant self-service', cat: 'Restauration', type: 'food', cost: 85000, upkeep: 900, cap: 60, price: 17, dur: 40, quality: 0.6, r: 20, look: 'chalet', desc: 'Grande capacité au pied des pistes.' },
    { id: 'alt_restaurant', name: 'Restaurant d\'altitude', cat: 'Restauration', type: 'food', cost: 140000, upkeep: 1150, cap: 50, price: 24, dur: 45, quality: 0.8, r: 20, look: 'chalet_stone', alt: true, desc: 'Terrasse panoramique : très apprécié, à relier au domaine par piste ou remontée.' },
    { id: 'gastro', name: 'Table gastronomique', cat: 'Restauration', type: 'food', cost: 260000, upkeep: 1900, cap: 36, price: 58, dur: 70, quality: 1, r: 18, look: 'chalet_lux', eve: true, research: 'luxury', prestige: 4, desc: 'Clientèle premium, fort prestige.' },
    { id: 'pizzeria', name: 'Pizzeria familiale', cat: 'Restauration', type: 'food', cost: 70000, upkeep: 620, cap: 45, price: 14, dur: 35, quality: 0.55, r: 16, look: 'chalet', eve: true, desc: 'Abordable et rapide, ouverte midi et soir : parfaite pour les familles.' },
    { id: 'savoyard', name: 'Restaurant savoyard', cat: 'Restauration', type: 'food', cost: 125000, upkeep: 980, cap: 40, price: 27, dur: 55, quality: 0.78, r: 17, look: 'chalet_stone', eve: true, desc: 'Fondues et raclettes : le dîner préféré des résidents.' },
    { id: 'bar', name: 'Bar d\'après-ski', cat: 'Restauration', type: 'drink', cost: 60000, upkeep: 520, cap: 40, price: 8, dur: 30, quality: 0.6, r: 14, look: 'chalet', alt: true, eve: true, desc: 'Boissons chaudes et ambiance. Redonne de l\'énergie et du moral.' },
    { id: 'rental', name: 'Location de matériel', cat: 'Commerces', type: 'rental', cost: 55000, upkeep: 420, cap: 16, price: 24, dur: 10, quality: 0.6, r: 15, look: 'shop', desc: 'Attire les visiteurs sans équipement (+35 % de demande).' },
    { id: 'shop', name: 'Magasin de sport', cat: 'Commerces', type: 'shop', cost: 50000, upkeep: 380, cap: 14, price: 32, dur: 14, quality: 0.6, r: 14, look: 'shop', desc: 'Vêtements, souvenirs et accessoires.' },
    { id: 'gite', name: 'Gîte d\'étape (50 lits)', cat: 'Hébergement', type: 'lodging', seg: 0, stars: 1, cost: 90000, upkeep: 380, cap: 50, price: 30, quality: 0.4, r: 18, look: 'chalet', road: true, desc: 'Dortoirs et petits prix : attire les jeunes et les budgets serrés.' },
    { id: 'residence', name: 'Résidence de tourisme (120 lits)', cat: 'Hébergement', type: 'lodging', seg: 0, stars: 2, cost: 360000, upkeep: 1100, cap: 120, price: 52, quality: 0.52, r: 26, look: 'hotel', road: true, desc: 'Appartements en location : beaucoup de lits, peu de services.' },
    { id: 'hotel2', name: 'Hôtel ★★ (70 lits)', cat: 'Hébergement', type: 'lodging', seg: 0, stars: 2, cost: 210000, upkeep: 950, cap: 70, price: 64, quality: 0.5, r: 22, look: 'hotel', road: true, desc: 'Hôtel simple et fonctionnel.' },
    { id: 'chalets', name: 'Hameau de chalets (40 lits)', cat: 'Hébergement', type: 'lodging', seg: 1, stars: 3, cost: 120000, upkeep: 500, cap: 40, price: 68, quality: 0.6, r: 26, look: 'chalets', road: true, desc: 'Chalets indépendants, très demandés par les familles.' },
    { id: 'hotel', name: 'Hôtel ★★★ (90 lits)', cat: 'Hébergement', type: 'lodging', seg: 1, stars: 3, cost: 300000, upkeep: 1400, cap: 90, price: 95, quality: 0.7, r: 24, look: 'hotel', road: true, desc: 'Hébergement confortable au cœur de la station.' },
    { id: 'club', name: 'Club de vacances (220 lits)', cat: 'Hébergement', type: 'lodging', seg: 1, stars: 3, cost: 880000, upkeep: 3600, cap: 220, price: 105, quality: 0.68, r: 34, look: 'palace', road: true, hb: true, desc: 'Formule tout compris pour les familles : demi-pension incluse d\'office.' },
    { id: 'hotel4', name: 'Hôtel ★★★★ (110 lits)', cat: 'Hébergement', type: 'lodging', seg: 2, stars: 4, cost: 560000, upkeep: 2400, cap: 110, price: 155, quality: 0.85, r: 26, look: 'hotel', road: true, prestige: 3, desc: 'Clientèle aisée : exige un domaine prestigieux pour se remplir.' },
    { id: 'palace', name: 'Palace ★★★★★ (140 lits)', cat: 'Hébergement', type: 'lodging', seg: 2, stars: 5, cost: 950000, upkeep: 4200, cap: 140, price: 260, quality: 1, r: 30, look: 'palace', road: true, research: 'luxury', prestige: 8, desc: 'Le sommet du luxe. Ne se remplit que si le prestige de la station suit.' },
    { id: 'luxchalet', name: 'Chalet de prestige (10 lits)', cat: 'Hébergement', type: 'lodging', seg: 2, stars: 5, cost: 240000, upkeep: 650, cap: 10, price: 390, quality: 1, r: 16, look: 'chalet_lux', road: true, research: 'luxury', prestige: 2, desc: 'Chalet privatif avec majordome : peu de lits, tarif très élevé.' },
    { id: 'refuge', name: 'Refuge d\'altitude (20 lits)', cat: 'Hébergement', type: 'lodging', seg: 0, stars: 1, cost: 90000, upkeep: 350, cap: 20, price: 58, quality: 0.6, r: 13, look: 'chalet_stone', alt: true, research: 'nordic', prestige: 2, desc: 'Nuit en montagne pour randonneurs et skieurs de randonnée.' },
    { id: 'school', name: 'École de ski', cat: 'École & enfants', type: 'school', cost: 70000, upkeep: 300, cap: 6, price: 42, r: 16, look: 'school', desc: 'Cours collectifs et individuels. Chaque moniteur encadre 8 élèves.' },
    { id: 'pioupiou', name: 'Jardin des Piou-Piou', cat: 'École & enfants', type: 'kids', cost: 55000, upkeep: 260, cap: 30, price: 36, r: 34, look: 'garden', desc: 'Espace enfants clôturé avec tapis et arches. Indispensable aux familles.' },
    { id: 'rescue', name: 'Poste de secours', cat: 'Sécurité', type: 'rescue', cost: 60000, upkeep: 640, cap: 4, r: 12, look: 'rescue', alt: true, desc: 'Base des pisteurs-secouristes (effectif réglable). Ils sécurisent les pistes le matin et interviennent sur les accidents.' },
    { id: 'heliport', name: 'Héliport de secours', cat: 'Sécurité', type: 'heli', cost: 280000, upkeep: 1500, cap: 1, r: 22, look: 'heli', research: 'heli', prestige: 3, desc: 'Évacuation héliportée des accidents graves.' },
    { id: 'garage', name: 'Garage à dameuses', cat: 'Exploitation', type: 'garage', cost: 110000, upkeep: 700, cap: 2, r: 20, look: 'garage', desc: 'Abrite jusqu\'à 6 dameuses (2 fournies). Chaque machine prépare environ 6 km de pistes par soirée.' },
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
    { cat: 'Remontées', id: 'tier1', name: 'Remontées — palier 1', cost: 25000, days: 2, desc: 'Tapis roulant, téléski à archets, télésiège fixe 4 places.', tier: 1 },
    { cat: 'Remontées', id: 'tier2', name: 'Remontées — palier 2', cost: 70000, days: 3, desc: 'Télésiège débrayable 4 places.', tier: 2, req: 'tier1' },
    { cat: 'Remontées', id: 'tier3', name: 'Remontées — palier 3', cost: 150000, days: 4, desc: 'Débrayable 6 places et télécabine 8 places.', tier: 3, req: 'tier2' },
    { cat: 'Remontées', id: 'tier4', name: 'Remontées — palier 4', cost: 300000, days: 5, desc: 'Télésiège à bulles 8 places, télécabine 10 places.', tier: 4, req: 'tier3' },
    { cat: 'Remontées', id: 'tier5', name: 'Remontées — palier 5', cost: 600000, days: 6, desc: 'Téléphérique 80 places : franchit les grands vides.', tier: 5, req: 'tier4' },
    { cat: 'Remontées', id: 'tier6', name: 'Remontées — palier 6', cost: 1200000, days: 8, desc: 'Télécabine 3S et funiculaire, insensibles au vent.', tier: 6, req: 'tier5' },
    { cat: 'Neige et pistes', id: 'snowmaking', name: 'Neige de culture', cost: 60000, days: 3, desc: 'Permet d\'équiper les pistes d\'enneigeurs.' },
    { cat: 'Neige et pistes', id: 'winch', name: 'Dameuses à treuil', cost: 90000, days: 3, desc: 'Damage des pistes noires et +40 % de rendement.', req: 'snowmaking' },
    { cat: 'Remontées', id: 'handsfree', name: 'Forfaits mains-libres', cost: 50000, days: 2, desc: 'Embarquement plus fluide : +12 % de débit sur toutes les remontées.' },
    { cat: 'Sécurité', id: 'padding', name: 'Protections de pylônes', cost: 35000, days: 2, desc: 'Matelas et filets : -30 % d\'accidents.' },
    { cat: 'Sécurité', id: 'gazex', name: 'Déclencheurs d\'avalanches', cost: 110000, days: 4, desc: 'Sécurisation rapide : plus d\'ouverture retardée après les grosses chutes.' },
    { cat: 'Sécurité', id: 'heli', name: 'Secours héliporté', cost: 80000, days: 3, desc: 'Débloque l\'héliport de secours.', req: 'padding' },
    { cat: 'Loisirs et événements', id: 'snowpark', name: 'Freestyle', cost: 40000, days: 2, desc: 'Débloque le snowpark.' },
    { cat: 'Loisirs et événements', id: 'nordic', name: 'Activités nordiques', cost: 45000, days: 2, desc: 'Centre nordique, chiens de traîneau, refuge.' },
    { cat: 'Accueil et hébergement', id: 'luxury', name: 'Hôtellerie de prestige', cost: 180000, days: 5, desc: 'Palace, table gastronomique, spa.' },
    { cat: 'Loisirs et événements', id: 'events', name: 'Événementiel sportif', cost: 120000, days: 4, desc: 'Stade de slalom, scène de concerts et calendrier d\'événements.' },
    { cat: 'Neige et pistes', id: 'night', name: 'Ski nocturne', cost: 140000, days: 3, desc: 'Éclairage des pistes et ouverture de 18 h 30 à 21 h des remontées désignées, avec un forfait du soir.', req: 'snowmaking' },
    { cat: 'Loisirs et événements', id: 'summer', name: 'Saison d\'été', cost: 100000, days: 4, desc: 'Saison d\'été jouable de 30 jours : VTT et randonnée sur vos remontées (hors téléskis et tapis). Débloque aussi parapente et musée.' },
    { cat: 'Remontées', id: 'energy', name: 'Motorisations économes', cost: 90000, days: 4, desc: 'Entretien et énergie des remontées : −15 %.', req: 'tier2' },
    { cat: 'Neige et pistes', id: 'groomer2', name: 'Dameuses nouvelle génération', cost: 110000, days: 4, desc: 'Vitesse de damage +30 %.', req: 'winch' },
    { cat: 'Neige et pistes', id: 'snowmaking2', name: 'Enneigeurs basse énergie', cost: 130000, days: 4, desc: 'Coût de la neige de culture −40 %, production dès −1 °C.', req: 'snowmaking' },
    { cat: 'Sécurité', id: 'patrol2', name: 'Motoneiges de secours', cost: 55000, days: 2, desc: 'Les pisteurs interviennent 50 % plus vite.' },
    { cat: 'Sécurité', id: 'signage', name: 'Balisage renforcé', cost: 40000, days: 2, desc: 'Filets, jalons et panneaux : −15 % d\'accidents, chaque pisteur sécurise 1 km de plus.' },
    { cat: 'Accueil et hébergement', id: 'training', name: 'Formation du personnel', cost: 60000, days: 3, desc: 'Qualité de tous les services en hausse.' },
    { cat: 'Accueil et hébergement', id: 'kitchen', name: 'Cuisines professionnelles', cost: 85000, days: 3, desc: 'Restaurants : +15 % de couverts et service 12 % plus rapide.', req: 'training' },
    { cat: 'Accueil et hébergement', id: 'booking', name: 'Centrale de réservation', cost: 70000, days: 3, desc: 'Demande d\'hébergement +15 %.' },
    { cat: 'Gestion', id: 'yield', name: 'Tarification dynamique', cost: 80000, days: 3, desc: 'Forfaits vendus 12 % plus cher les week-ends et vacances, sans perte de clientèle.' },
    { cat: 'Gestion', id: 'loyalty', name: 'Programme de fidélité', cost: 65000, days: 3, desc: 'La réputation progresse 30 % plus vite quand les visiteurs sont contents.' },
    { cat: 'Gestion', id: 'purchasing', name: 'Centrale d\'achats', cost: 75000, days: 3, desc: 'Coût des marchandises (restauration, boutiques) −15 %.' },
    { cat: 'Loisirs et événements', id: 'olympic', name: 'Dossier olympique', cost: 900000, days: 10, desc: 'Débloque le stade olympique et la candidature aux Jeux.', req: 'events' }
  ];

  // étapes de carrière : check(G, stats) -> [fait, texte] par objectif
  D.RCATS = ['Remontées', 'Neige et pistes', 'Sécurité', 'Accueil et hébergement', 'Loisirs et événements', 'Gestion'];
  D.SEG = ['Économique', 'Confort', 'Luxe'];
  D.RANGE = ['Économique', 'Standard', 'Premium'];
  D.STAFF = ['Réduit', 'Normal', 'Renforcé'];
  D.LOANS = [
    { id: 'short', name: 'Crédit de trésorerie', amount: 150000, days: 40, rate: 0.0011 },
    { id: 'invest', name: 'Prêt d\'équipement', amount: 600000, days: 120, rate: 0.0007 },
    { id: 'long', name: 'Prêt d\'infrastructure', amount: 2000000, days: 260, rate: 0.0005 }
  ];

  // ---- Scénarios : chaque étape est une liste d'objectifs [texte, test(stats), valeur affichée(stats)] ----
  const v = (k, unit) => s => SS.num(s[k] || 0) + (unit || '');
  D.SCENARIOS = [
    { id: 'first', name: 'Premiers pas', tag: 'Facile', diff: 1,
      brief: 'Une montagne vierge, 420 000 € en poche et 70 jours pour prouver que votre station peut attirer du monde. Objectif : faire venir 250 visiteurs en une seule journée.',
      start: { money: 420000, seed: 1337, fame: 6, research: [] }, deadline: 70, victory: 'Plus de 250 visiteurs en une journée : votre station est sur la carte des Alpes !',
      stages: [
        { title: 'Ouvrir la station', reward: 40000, goals: [
          ['Construire une remontée', s => s.lifts >= 1, v('lifts')], ['Relier une piste au front de neige', s => s.connected >= 1, v('connected')], ['Accueillir 60 visiteurs en une journée', s => s.bestDay >= 60, s => s.bestDay + ' / 60']] },
        { title: '250 visiteurs', reward: 120000, goals: [
          ['250 visiteurs en une seule journée', s => s.bestDay >= 250, s => s.bestDay + ' / 250'], ['Satisfaction d\'au moins 55 %', s => s.sat >= 55, s => s.sat + ' %']] }] },
    { id: 'rescue', name: 'Station en perdition', tag: 'Difficile', diff: 3, seedLock: true,
      brief: 'Val-Mort vieillit mal : remontées usées, forfait trop cher, clients mécontents et 470 000 € de dettes. Remettez les comptes à flot, modernisez, puis faites de cette station une destination incontournable avant 320 jours.',
      start: { money: 45000, seed: 4242, fame: 12, rep: 24, sat: 38, prebuilt: 'decay', research: ['tier1'] }, deadline: 320, victory: 'Val-Mort est redevenue incontournable : la presse parle d\'un miracle alpin !',
      stages: [
        { title: 'Éviter la faillite', reward: 60000, goals: [
          ['Satisfaction d\'au moins 55 %', s => s.sat >= 55, s => s.sat + ' %'], ['Résultat positif en moyenne sur 7 jours', s => s.profit7 > 0, s => (s.profit7 >= 0 ? '+' : '−') + SS.num(Math.abs(s.profit7)) + ' € / jour'], ['Un poste de secours', s => s.rescue >= 1, v('rescue')]] },
        { title: 'Remise à niveau', reward: 150000, goals: [
          ['Toutes les remontées en bon état (≥ 80 %)', s => s.minCond >= 0.8, s => Math.round(s.minCond * 100) + ' %'], ['Au moins 2 dameuses', s => s.machines >= 2, v('machines')], ['Dette ramenée à 250 000 € ou moins', s => s.debt <= 250000, s => SS.num(s.debt) + ' €'], ['Satisfaction d\'au moins 68 %', s => s.sat >= 68, s => s.sat + ' %'], ['10 km de pistes', s => s.km >= 10, s => s.km.toFixed(1) + ' km']] },
        { title: 'Incontournable', reward: 400000, goals: [
          ['Notoriété d\'au moins 55 %', s => s.fame >= 55, s => Math.round(s.fame) + ' %'], ['600 visiteurs en une journée', s => s.bestDay >= 600, s => s.bestDay + ' / 600'], ['Satisfaction d\'au moins 78 %', s => s.sat >= 78, s => s.sat + ' %'], ['Prestige d\'au moins 45', s => s.prestige >= 45, v('prestige')], ['Organiser une course régionale', s => s.h_regional >= 1, v('h_regional')], ['Dette inférieure à 100 000 €', s => s.debt < 100000, s => SS.num(s.debt) + ' €']] }] },
    { id: 'family', name: 'Capitale des familles', tag: 'Moyen', diff: 2,
      brief: 'Faites de votre station la référence des vacances en famille : Piou-Piou, école de ski, pistes faciles, hébergements et activités pour toutes les générations. Vous avez deux saisons.',
      start: { money: 520000, seed: 9001, fame: 8, research: ['tier1'] }, deadline: 240, victory: 'Les familles du monde entier réservent chez vous : Capitale des familles !',
      stages: [
        { title: 'Accueillir les petits', reward: 80000, goals: [
          ['Jardin des Piou-Piou', s => s.kids >= 1, v('kids')], ['École de ski', s => s.school >= 1, v('school')], ['2 pistes vertes ou bleues ouvertes', s => s.easy >= 2, v('easy')], ['Un restaurant', s => s.food >= 1, v('food')]] },
        { title: 'Station familiale', reward: 250000, goals: [
          ['350 visiteurs en une journée', s => s.bestDay >= 350, s => s.bestDay + ' / 350'], ['150 lits touristiques', s => s.beds >= 150, s => SS.num(s.beds) + ' / 150'], ['Satisfaction d\'au moins 72 %', s => s.sat >= 72, s => s.sat + ' %'], ['200 médailles délivrées par l\'école', s => s.medals >= 200, s => s.medals + ' / 200'], ['Une luge ou une patinoire', s => (s.luge || 0) + (s.rink || 0) >= 1, s => ((s.luge || 0) + (s.rink || 0)) + '']] }] },
    { id: 'luxe', name: 'Station de prestige', tag: 'Difficile', diff: 3,
      brief: 'Une clientèle fortunée, des palaces et des tables étoilées. Avec 1,4 M€ et la recherche hôtelière acquise, bâtissez l\'adresse la plus prisée des Alpes en moins de 300 jours.',
      start: { money: 1400000, seed: 1337, fame: 10, research: ['tier1', 'tier2', 'luxury'] }, deadline: 300, victory: 'Votre palace affiche complet : vous êtes l\'adresse des Alpes !',
      stages: [
        { title: 'Les premières étoiles', reward: 200000, goals: [
          ['Un hôtel ★★★★ ou un palace', s => (s.hotel4 || 0) + (s.palace || 0) >= 1, s => ((s.hotel4 || 0) + (s.palace || 0)) + ''], ['Une table gastronomique', s => s.gastro >= 1, v('gastro')], ['Prestige d\'au moins 40', s => s.prestige >= 40, v('prestige')], ['Satisfaction d\'au moins 70 %', s => s.sat >= 70, s => s.sat + ' %']] },
        { title: 'L\'adresse des Alpes', reward: 500000, goals: [
          ['Un palace ★★★★★', s => s.palace >= 1, v('palace')], ['2 chalets de prestige', s => s.luxchalet >= 2, v('luxchalet')], ['Notoriété d\'au moins 55 %', s => s.fame >= 55, s => Math.round(s.fame) + ' %'], ['Prestige d\'au moins 65', s => s.prestige >= 65, v('prestige')], ['Organiser 2 événements', s => s.eventsHosted >= 2, v('eventsHosted')], ['Satisfaction d\'au moins 80 %', s => s.sat >= 80, s => s.sat + ' %']] }] },
    { id: 'olympic', name: 'La route des Jeux', tag: 'Campagne', diff: 2, bid: true,
      brief: 'La grande campagne, longue et exigeante : de la première remontée aux Jeux Olympiques d\'hiver. Il faudra exceller partout (domaine, prestige, notoriété, sécurité, hébergement), atteindre le niveau X Games, organiser une Coupe du monde, puis battre les villes rivales devant le CIO. L\'ouverture des Jeux marque la victoire. Pas de limite de temps.',
      start: { money: 650000, seed: 1337, fame: 5, research: [] }, deadline: 0, victory: 'La flamme olympique est allumée chez vous : les Jeux Olympiques d\'hiver s\'ouvrent et votre station entre dans la légende. Partie gagnée !',
      stages: [
        { title: 'Premiers flocons', reward: 60000, goals: [
          ['Construire une remontée', s => s.lifts >= 1, v('lifts')], ['Relier une piste au front de neige', s => s.connected >= 1, v('connected')], ['40 visiteurs en une journée', s => s.bestDay >= 40, s => s.bestDay + ' / 40']] },
        { title: 'Station familiale', reward: 120000, goals: [
          ['2 pistes vertes ou bleues', s => s.easy >= 2, v('easy')], ['Un restaurant et une location de matériel', s => s.food >= 1 && s.rental >= 1, s => (s.food >= 1 ? '✔' : '✘') + ' / ' + (s.rental >= 1 ? '✔' : '✘')], ['150 visiteurs en une journée', s => s.bestDay >= 150, s => s.bestDay + ' / 150'], ['Satisfaction d\'au moins 60 %', s => s.sat >= 60, s => s.sat + ' %']] },
        { title: 'Station reconnue', reward: 250000, fame: 20, goals: [
          ['École de ski et Jardin des Piou-Piou', s => s.school >= 1 && s.kids >= 1, s => (s.school >= 1 ? '✔' : '✘') + ' / ' + (s.kids >= 1 ? '✔' : '✘')], ['6 km de pistes', s => s.km >= 6, s => s.km.toFixed(1) + ' km'], ['Garage à dameuses et poste de secours', s => s.garage >= 1 && s.rescue >= 1, s => (s.garage >= 1 ? '✔' : '✘') + ' / ' + (s.rescue >= 1 ? '✔' : '✘')], ['100 lits touristiques', s => s.beds >= 100, s => SS.num(s.beds) + ' / 100'], ['400 visiteurs en une journée', s => s.bestDay >= 400, s => s.bestDay + ' / 400'], ['Notoriété de 20 %', s => s.fame >= 20, s => Math.round(s.fame) + ' %']] },
        { title: 'Terre de compétition', reward: 400000, fame: 35, goals: [
          ['Une piste rouge et une piste noire', s => s.red >= 1 && s.black >= 1, s => s.red + ' rouge · ' + s.black + ' noire'], ['Un stade de slalom', s => s.stadium >= 1, v('stadium')], ['Organiser une course régionale', s => s.h_regional >= 1, v('h_regional')], ['600 visiteurs en une journée', s => s.bestDay >= 600, s => s.bestDay + ' / 600'], ['Notoriété de 35 %', s => s.fame >= 35, s => Math.round(s.fame) + ' %']] },
        { title: 'Niveau international', reward: 600000, fame: 45, goals: [
          ['Organiser une Coupe d\'Europe', s => s.h_europe >= 1, v('h_europe')], ['Prestige ★★ (40)', s => s.prestige >= 40, s => s.prestige + ' / 40'], ['Notoriété de 50 %', s => s.fame >= 50, s => Math.round(s.fame) + ' %'], ['20 km de pistes', s => s.km >= 20, s => s.km.toFixed(1) + ' km'], ['Satisfaction moyenne de 70 %', s => s.satAvg >= 70, s => Math.round(s.satAvg) + ' %']] },
        { title: 'Le niveau X Games', reward: 900000, fame: 60, goals: [
          ['Remplir tous les critères X Games (les organiser reste facultatif)', s => SS.Scn.critOk('xgames'), s => Math.round(SS.Scn.critShare('xgames') * 100) + ' %']] },
        { title: 'Coupe du monde', reward: 1200000, fame: 68, goals: [
          ['Organiser une Coupe du monde', s => s.h_worldcup >= 1, v('h_worldcup')], ['Prestige ★★★ (60)', s => s.prestige >= 60, s => s.prestige + ' / 60'], ['Notoriété de 75 %', s => s.fame >= 75, s => Math.round(s.fame) + ' %'], ['Dossier « Jeux Olympiques » étudié', s => s.research_olympic, s => s.research_olympic ? '✔' : '✘']] },
        { title: 'Candidature olympique', reward: 2000000, fame: 75, goals: [
          ['Candidature retenue par le CIO face aux villes rivales', s => s.bidOk, s => s.bidOk ? '✔' : '✘']] },
        { title: 'Les Jeux Olympiques', reward: 0, fame: 82, goals: [
          ['Réussir l\'inspection finale et ouvrir les Jeux (jour 1 = victoire)', s => s.olyDay1, s => s.olyDay1 ? '✔' : '✘']] }] }
  ];

  // fame : notoriété minimale ; fameGain : notoriété gagnée si l'événement est réussi (satisfaction ≥ 50 %)
  D.EVENTS = [
    { id: 'fete', name: 'Fête du village', cost: 15000, boost: 1.25, prestige: 1, fame: 0, fameGain: 0.8, days: 1, need: s => true, needTxt: 'Aucun prérequis' },
    { id: 'concert', name: 'Festival de concerts', cost: 60000, boost: 1.4, prestige: 2, fame: 10, fameGain: 1.8, days: 2, need: s => s.stage >= 1 && s.fame >= 10, needTxt: 'Scène de concerts · notoriété 10 %' },
    { id: 'regional', name: 'Course régionale', cost: 40000, boost: 1.3, prestige: 3, fame: 18, fameGain: 3, days: 1, comp: true, need: s => s.stadium >= 1 && s.red >= 1 && s.fame >= 18, needTxt: 'Stade de slalom, piste rouge · notoriété 18 %' },
    { id: 'europe', name: 'Coupe d\'Europe', cost: 150000, boost: 1.5, prestige: 6, fame: 35, fameGain: 5, days: 2, comp: true, need: s => s.stadium >= 1 && s.black >= 1 && s.beds >= 250 && s.fame >= 35 && s.prestige >= 30, needTxt: 'Stade, piste noire, 250 lits · notoriété 35 %, prestige 30' },
    { id: 'xgames', name: 'X Games d\'hiver', cost: 450000, boost: 1.9, prestige: 9, fame: 60, fameGain: 9, days: 3, comp: true, need: s => SS.Scn.critOk('xgames'), needTxt: 'Tous les critères X Games (voir Carrière) · notoriété 60 %, prestige 45' },
    { id: 'worldcup', name: 'Coupe du monde', cost: 600000, boost: 1.8, prestige: 10, fame: 68, fameGain: 10, days: 3, comp: true, need: s => s.stadium >= 1 && s.black >= 2 && s.beds >= 600 && s.h_europe >= 1 && s.fame >= 68 && s.prestige >= 50 && s.satAvg >= 72, needTxt: 'Stade, 2 pistes noires, 600 lits, une Coupe d\'Europe, satisfaction 72 % · notoriété 68 %, prestige 50' },
    { id: 'olympics', name: 'Jeux Olympiques d\'hiver', cost: 3000000, boost: 2.2, prestige: 25, fame: 80, fameGain: 20, days: 6, comp: true, bid: true, need: s => false, needTxt: 'Uniquement via la candidature olympique' }
  ];

  D.ACH = [
    { id: 'first', name: 'Premier client', desc: 'Accueillir un premier visiteur.', test: s => s.total >= 1 },
    { id: 'v10k', name: 'Dix mille sourires', desc: '10 000 visiteurs cumulés.', test: s => s.total >= 10000 },
    { id: 'v100k', name: 'Destination incontournable', desc: '100 000 visiteurs cumulés.', test: s => s.total >= 100000 },
    { id: 'full', name: 'Complet !', desc: '1 000 visiteurs en une journée.', test: s => s.bestDay >= 1000 },
    { id: 'happy', name: 'Station du bonheur', desc: 'Satisfaction de 80 % avec au moins 300 visiteurs.', test: (s, G) => s.sat >= 80 && G.visitorsToday >= 300 },
    { id: 'rainbow', name: 'Les quatre couleurs', desc: 'Une piste ouverte de chaque difficulté.', test: (s, G) => [0, 1, 2, 3].every(d => G.pistes.some(p => p.diff === d && p.open)) },
    { id: 'km20', name: 'Grand domaine', desc: '20 km de pistes.', test: s => s.km >= 20 },
    { id: 'alt3500', name: 'Toit du domaine', desc: 'Une remontée arrivant au-dessus de 3 500 m.', test: s => s.topAlt >= 3500 },
    { id: 'vert2000', name: 'Deux mille de dénivelé', desc: '2 000 m entre le point bas et le point haut desservis.', test: s => s.vert >= 2000 },
    { id: 'million', name: 'Premier million', desc: '1 000 000 € en caisse.', test: s => s.money >= 1000000 },
    { id: 'debtfree', name: 'Sans dette', desc: 'N\'avoir aucun emprunt en cours après la première saison.', test: s => s.year >= 2 && s.loan <= 0 },
    { id: 'medals', name: 'Pépinière de champions', desc: '500 médailles délivrées par l\'école de ski.', test: s => s.medals >= 500 },
    { id: 'nightowl', name: 'Oiseaux de nuit', desc: 'Vendre des forfaits nocturnes.', test: s => s.night },
    { id: 'science', name: 'Bureau d\'études', desc: 'Terminer 10 recherches.', test: s => s.research >= 10 },
    { id: 'host', name: 'Terre d\'événements', desc: 'Organiser 5 événements.', test: s => s.eventsHosted >= 5 },
    { id: 'stars5', name: 'Cinq étoiles', desc: 'Prestige ★★★★★ (95 ou plus).', test: s => s.prestige >= 95 },
    { id: 'season', name: 'Une saison au compteur', desc: 'Terminer une saison d\'hiver.', test: s => s.year >= 2 },
    { id: 'olympic', name: 'Flamme olympique', desc: 'Organiser les Jeux Olympiques d\'hiver.', test: s => s.olympics >= 1 }
  ];

  D.WEATHER = {
    sun: { name: 'Grand soleil', icon: '☀️', demand: 1.15, sky: 0x7db9ee, snow: 0, light: 1 },
    cloud: { name: 'Nuageux', icon: '⛅', demand: 1.0, sky: 0xa9bfd3, snow: 0, light: 0.8 },
    snow: { name: 'Chutes de neige', icon: '🌨️', demand: 0.85, sky: 0xb9c3cf, snow: 12, light: 0.65 },
    heavy: { name: 'Fortes chutes', icon: '❄️', demand: 0.6, sky: 0xaab2bc, snow: 35, light: 0.5 },
    storm: { name: 'Tempête', icon: '🌬️', demand: 0.4, sky: 0x8d959f, snow: 20, light: 0.45, wind: 60 },
    rain: { name: 'Pluie', icon: '🌧️', demand: 0.45, sky: 0x8f9aa6, snow: 0, light: 0.6, rain: true },
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
