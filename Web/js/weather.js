// SkiStation Web — météo, enneigement, damage et neige de culture
(function () {
  'use strict';
  const SS = window.SS, D = SS.Data;
  const W = SS.Weather = {};

  function roll(day) {
    const late = SS.smooth(85, 140, day), r = Math.random();
    const table = SS.G.summer ? [['sun', 0.46], ['cloud', 0.30], ['rain', 0.17], ['storm', 0.07]] : [['sun', 0.30], ['cloud', 0.24], ['snow', 0.23 - late * 0.09], ['heavy', 0.07 - late * 0.03], ['storm', 0.05], ['thaw', 0.04 + late * 0.14], ['frost', 0.11 - late * 0.09]];
    let tot = 0; for (const t of table) tot += t[1];
    let x = r * tot;
    for (const t of table) { x -= t[1]; if (x <= 0) return make(t[0], day); }
    return make('sun', day);
  }
  function make(type, day) {
    const def = D.WEATHER[type];
    if (SS.G.summer) return { type, temp: Math.round(19 + SS.range(-3, 5) - (type === 'rain' || type === 'storm' ? 6 : 0)), wind: Math.round((def.wind || 0) * 0.7 + SS.range(3, 22)), snow: 0 };
    const base = -4 + 9 * SS.smooth(60, 140, day) + (day < 15 ? 1.5 : 0);
    return { type, temp: Math.round(base + (def.warm || 0) + SS.range(-2, 2) + (type === 'sun' ? 1.5 : 0)), wind: Math.round((def.wind || 0) + SS.range(5, type === 'storm' ? 45 : 28)), snow: Math.round(def.snow * SS.range(0.6, 1.4)) };
  }
  W.init = function () {
    const G = SS.G;
    G.weather = make('sun', 0); G.weather.wind = 10;
    G.forecast = [make('cloud', 1), roll(2), roll(3), roll(4)];
    W.windNow = G.weather.wind;
  };
  W.next = function () {
    const G = SS.G;
    G.weather = G.forecast.shift(); G.forecast.push(roll(G.day + 4));
    W.windNow = G.weather.wind;
  };
  W.def = () => D.WEATHER[SS.G.weather.type];
  W.tempAt = (alt, night) => SS.G.weather.temp - (alt - 1650) * 0.0065 - (night ? 5 : 0) + (SS.G.time > 12 * 3600 && SS.G.time < 16 * 3600 && !night ? 2 : 0);

  // qualité de neige perçue 0..1 (skill module l'effet de la poudreuse)
  W.quality = function (p, skill) {
    if (SS.G.summer) return SS.G.weather.type === 'rain' ? 0.45 : 0.78;
    let q = 0.5 + 0.3 * p.groomed + p.powder * (skill >= 2 ? 0.25 : -0.1) - 0.5 * p.ice - 0.35 * p.slush;
    if (p.depth < 30) q -= 0.2;
    return SS.clamp(q, 0, 1);
  };
  W.state = function (p) {
    if (SS.G.summer) return SS.G.weather.type === 'rain' ? 'Sentier boueux' : 'Sentier sec';
    if (p.snowClosed) return 'Neige insuffisante';
    if (p.ice > 0.45) return 'Verglas';
    if (p.slush > 0.45) return 'Neige soupe';
    if (p.powder > 0.5) return 'Poudreuse';
    if (p.groomed > 0.7) return 'Damée';
    if (p.groomed > 0.35) return 'Tracée';
    return 'Bosselée';
  };

  // vent : varie dans la journée, ferme les remontées exposées (les passagers engagés terminent)
  W.updateWind = function () {
    const G = SS.G;
    W.windNow = Math.max(0, G.weather.wind * (0.75 + 0.5 * Math.sin(G.time / 2700 + G.day)) + SS.range(-4, 4));
    let changed = false;
    for (const l of G.lifts) {
      const def = D.lift(l.type), w = W.windNow * (0.65 + (l.b.y - 1650) / 2500 * 0.75);
      const stop = w > def.wind || (l.windStop && w > def.wind - 8);
      if (stop !== l.windStop) { l.windStop = stop; changed = true; SS.log(stop ? '🌬️ ' + l.name + ' arrêté : vent ' + Math.round(w) + ' km/h en crête.' : l.name + ' : le vent faiblit, réouverture.', stop ? 'warn' : 'good'); }
    }
    if (changed) SS.Net.touch();
  };

  // effets en cours de journée (appelé toutes les 10 min de jeu)
  W.dayTick = function () {
    const G = SS.G; if (G.casual || G.summer) return;
    for (const p of G.pistes) {
      const mid = p.topAlt - p.drop / 2, t = W.tempAt(mid, false);
      if (G.time > 12.5 * 3600 && t > 1.5) p.slush = Math.min(1, p.slush + (t - 1.5) * 0.012);
      if (t > 3) p.ice = Math.max(0, p.ice - 0.03);
    }
  };

  // passage de la nuit : chutes, fonte, regel, canons. Le damage est fait avant par les dameuses.
  W.night = function () {
    const G = SS.G, w = G.weather; let cannonCost = 0, closed = [];
    if (G.summer) { for (const p of G.pistes) { p.passes = 0; p.snowClosed = false; p.avaUntil = 0; } return; }
    for (const p of G.pistes) {
      const mid = p.topAlt - p.drop / 2, tn = W.tempAt(mid, true), td = W.tempAt(mid, false);
      const fall = w.snow * (0.6 + (mid - 1650) / 1000 * 0.5);
      if (td > 2) p.depth -= (td - 2) * 1.3;
      p.depth -= 0.25 + p.passes * 0.001;
      if (p.slush > 0.25 && tn < -1) p.ice = Math.max(p.ice, p.slush * (p.groomedTonight ? 0.2 : 0.9));
      p.slush = 0;
      if (fall > 2) { p.depth += fall * 0.6; p.powder = SS.clamp(fall / 22, 0, 1); p.ice *= 0.2; if (!p.groomedTonight) p.groomed = Math.min(p.groomed, 0.45); }
      else p.powder *= 0.4;
      if (p.cannons && tn < (SS.researched('snowmaking2') ? -1 : -2) && p.depth < 110) { p.depth += 9; cannonCost += p.len * 0.9 * (SS.researched('snowmaking2') ? 0.6 : 1); }
      p.depth = SS.clamp(p.depth, 0, 400);
      if (G.casual) { p.depth = Math.max(p.depth, 60); p.ice = 0; p.slush = 0; }
      const was = p.snowClosed; p.snowClosed = p.depth < 15;
      if (p.snowClosed && !was) closed.push(p.name);
      p.avaUntil = 0;
      if (!SS.researched('gazex') && w.snow >= 25 && (p.topAlt > 2700 || p.diff === 3)) p.avaUntil = 10.5 * 3600;
      p.passes = 0; p.groomedTonight = false;
    }
    G.snowBase = SS.clamp(G.snowBase + w.snow * 0.5 - Math.max(0, w.temp - 1) * 1.2 - 0.3, 5, 300);
    if (cannonCost > 0) SS.charge(Math.round(cannonCost), 'Neige de culture');
    if (closed.length) SS.log('⚠️ Neige insuffisante : ' + closed.join(', ') + ' fermée(s). Installez des enneigeurs.', 'warn');
  };
})();
