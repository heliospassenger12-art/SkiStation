// SkiStation Web — modèles procéduraux : bâtiments, gares, véhicules, personnages
(function () {
  'use strict';
  const SS = window.SS, Mo = SS.Models = {};
  const WOOD = 0x8a5a3a, WOOD2 = 0x6f452b, STONE = 0x9a9a98, SNOW = 0xf4f8fc, DARK = 0x2c3038, CONC = 0xb9bcc0, RED = 0xd8342c, GLASS = 0x7fb6d9;

  const M4 = () => new THREE.Matrix4();
  function box(w, h, d, x, y, z, color, ry) {
    const m = M4().makeRotationY(ry || 0); m.setPosition(x, y + h / 2, z);
    return { geo: new THREE.BoxGeometry(w, h, d), matrix: m, color };
  }
  function cyl(r, h, x, y, z, color, seg, r2) {
    const m = M4().makeTranslation(x, y + h / 2, z);
    return { geo: new THREE.CylinderGeometry(r2 === undefined ? r : r2, r, h, seg || 10), matrix: m, color };
  }
  function gable(w, h, d, x, y, z, color, ry) {      // toit à deux pans, faîtage le long de z
    const hw = w / 2, hd = d / 2;
    const v = [-hw, 0, -hd, hw, 0, -hd, 0, h, -hd, -hw, 0, hd, hw, 0, hd, 0, h, hd];
    const idx = [0, 2, 1, 3, 4, 5, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 4, 0, 1, 4, 0, 4, 3];
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(v, 3)); g.setIndex(idx);
    const ng = g.toNonIndexed(); ng.computeVertexNormals();
    const m = M4().makeRotationY(ry || 0); m.setPosition(x, y, z);
    return { geo: ng, matrix: m, color };
  }
  // chalet générique : socle pierre, étage bois, toit enneigé débordant, fenêtres (retournées à part)
  function chalet(P, Wn, w, d, floors, x, z, wall, ry, base) {
    const bh = 2.6, fh = 3 * floors, c = Math.cos(ry || 0), s = Math.sin(ry || 0);
    const at = (lx, lz) => [x + lx * c + lz * s, z - lx * s + lz * c];
    P.push(box(w, bh, d, x, 0, z, base || STONE, ry), box(w + 0.6, fh, d + 0.6, x, bh, z, wall, ry));
    P.push(gable(w + 3, w * 0.36, d + 3, x, bh + fh, z, SNOW, ry), box(w + 2.2, 0.5, d + 2.2, x, bh + fh - 0.4, z, WOOD2, ry));
    for (let f = 0; f < floors; f++) for (const side of [-1, 1]) {
      const n = Math.max(1, Math.floor(w / 4));
      for (let i = 0; i < n; i++) { const p = at((i - (n - 1) / 2) * (w / n), side * (d / 2 + 0.35)); Wn.push(box(1.5, 1.4, 0.2, p[0], bh + 0.8 + f * 3, p[1], 0xffffff, ry)); }
    }
    const dp = at(0, d / 2 + 0.3); P.push(box(1.6, 2.3, 0.3, dp[0], 0, dp[1], WOOD2, ry));
  }
  function sign(P, x, z, color, h) { P.push(cyl(0.25, h || 7, x, 0, z, DARK, 6), box(5, 2, 0.4, x, (h || 7) - 1, z, color)); }
  function fence(P, r, n, color) { for (let i = 0; i < n; i++) { const a = i / n * 6.283; P.push(box(0.4, 1.6, 0.4, Math.cos(a) * r, 0, Math.sin(a) * r, color)); } }

  const CARS = [0xd8342c, 0x2b78e4, 0xe8e8e8, 0x333842, 0xf2c230, 0x6d8f4e, 0xaab1ba];
  Mo.building = function (look, r) {
    const P = [], Wn = [];
    switch (look) {
      case 'kiosk': chalet(P, Wn, 9, 7, 1, 0, 0, WOOD, 0); sign(P, 7, 3, 0xf2c230, 6); break;
      case 'shop': chalet(P, Wn, 16, 11, 1, 0, 0, 0xa8663d, 0); P.push(box(16, 1.2, 0.5, 0, 4.2, 6.2, 0x2b78e4)); break;
      case 'chalet': chalet(P, Wn, 22, 14, 2, 0, 0, WOOD, 0); P.push(box(22, 0.5, 8, 0, 0, 11, 0xb08a5c)); for (let i = -2; i <= 2; i++) P.push(cyl(1.4, 1.1, i * 4.2, 0.5, 11, 0xd8342c, 8)); break;
      case 'chalet_stone': chalet(P, Wn, 20, 13, 2, 0, 0, STONE, 0, 0x777a7d); P.push(box(20, 0.5, 9, 0, 0, 11, 0xb08a5c), cyl(0.3, 9, 9, 0, 14, DARK, 6), box(3, 1.6, 0.2, 10.4, 7, 14, RED)); break;
      case 'chalet_lux': chalet(P, Wn, 20, 14, 2, 0, 0, 0x5b3a26, 0, 0x55585c); P.push(box(8, 5, 3, 0, 0, 8.5, GLASS)); break;
      case 'chalets': for (const p of [[-13, -9, 0.2], [12, -10, -0.3], [-12, 10, 2.9], [13, 11, 3.4], [0, 0, 1.4]]) chalet(P, Wn, 10, 8, 1, p[0], p[1], WOOD, p[2]); break;
      case 'hotel': chalet(P, Wn, 30, 16, 4, 0, 0, 0xc9a37a, 0); P.push(box(10, 4, 5, 0, 0, 10, WOOD2), box(10.6, 0.6, 5.6, 0, 4, 10, SNOW)); break;
      case 'palace': chalet(P, Wn, 40, 18, 5, 0, 0, 0xe6dccb, 0, 0x8d8f92); chalet(P, Wn, 14, 14, 6, -20, 4, 0xe6dccb, 0, 0x8d8f92); chalet(P, Wn, 14, 14, 6, 20, 4, 0xe6dccb, 0, 0x8d8f92); P.push(box(14, 1, 10, 0, 0, 14, 0x9a2f2f)); break;
      case 'school': chalet(P, Wn, 16, 11, 1, 0, 0, RED, 0); sign(P, 10, 5, RED, 8); for (let i = 0; i < 5; i++) P.push(box(0.3, 2.2, 0.3, -9 + i * 1.2, 0, 7, [0xf2c230, 0x2b78e4, RED][i % 3])); break;
      case 'garden': {
        fence(P, r - 2, 28, 0xf2c230);
        for (let i = 0; i < 4; i++) { const x = -14 + i * 9, c = [RED, 0x2b78e4, 0x2fae4e, 0xf2c230][i]; P.push(box(0.6, 4, 0.6, x - 2.5, 0, 6, c), box(0.6, 4, 0.6, x + 2.5, 0, 6, c), box(5.6, 0.6, 0.6, x, 4, 6, c)); }
        P.push(box(3, 0.4, 30, -20, 0, -4, 0x4a4f57), cyl(4.5, 3.2, 14, 0, -12, SNOW, 10, 0.8), box(6, 3, 5, 18, 0, 14, RED), gable(8, 2.4, 7, 18, 3, 14, SNOW));
        for (let i = 0; i < 6; i++) P.push(cyl(0.5, 0.9, -8 + i * 4, 0, -8 + (i % 2) * 5, [0xf2c230, RED, 0x2b78e4][i % 3], 6, 0.05));
        break;
      }
      case 'rescue': chalet(P, Wn, 11, 8, 1, 0, 0, 0xe9e2d0, 0); P.push(box(3.4, 1, 0.3, 0, 4.4, 4.6, RED), box(1, 3.4, 0.3, 0, 3.2, 4.65, RED), box(2, 1.2, 4.5, 8, 0, 2, 0xf28a1c), cyl(0.2, 8, -6.5, 0, 0, DARK, 5)); break;
      case 'heli': P.push(cyl(15, 0.6, 0, 0, 0, 0x4a4f57, 20), cyl(11.5, 0.7, 0, 0, 0, 0x5d636c, 20), box(2, 0.75, 12, -4, 0, 0, 0xf2c230), box(2, 0.75, 12, 4, 0, 0, 0xf2c230), box(8, 0.75, 2, 0, 0, 0, 0xf2c230)); chalet(P, Wn, 9, 7, 1, -17, -12, CONC, 0.5); P.push(cyl(0.2, 9, 14, 0, -14, DARK, 5), box(3, 1, 1, 15.5, 8, -14, 0xf28a1c)); break;
      case 'garage': P.push(box(26, 7, 15, 0, 0, 0, CONC), gable(29, 4, 18, 0, 7, 0, SNOW), box(7, 5.4, 0.4, -7, 0, 7.6, DARK), box(7, 5.4, 0.4, 7, 0, 7.6, DARK)); Mo.groomerParts(P, 16, 0, 14, 0.4); break;
      case 'parking': {
        P.push(box(r * 1.9, 0.5, r * 1.3, 0, 0, 0, 0x555b64));
        for (let row = -1; row <= 1; row++) for (let i = 0; i < 12; i++) if ((i * 7 + row * 3) % 5 !== 0) P.push(box(2.2, 1.5, 4.6, -r * 0.85 + i * (r * 1.7 / 11), 0.5, row * r * 0.42, CARS[(i * 3 + row + 7) % CARS.length]));
        sign(P, -r * 0.9, r * 0.7, 0x2b78e4, 7); break;
      }
      case 'bus': P.push(box(40, 0.5, 26, 0, 0, 0, 0x555b64)); for (let i = 0; i < 3; i++) { P.push(box(3.2, 3.6, 13, -12 + i * 7, 0.5, -3, [0xe8e8e8, 0x2b78e4, 0xf2c230][i])); Wn.push(box(3.3, 1.1, 11, -12 + i * 7, 2.6, -3, 0xffffff)); } P.push(box(14, 0.5, 5, 10, 4, 8, CONC), cyl(0.3, 4, 4, 0.5, 8, DARK, 6), cyl(0.3, 4, 16, 0.5, 8, DARK, 6)); break;
      case 'snowpark': {
        for (let i = 0; i < 3; i++) { const m = M4().makeRotationX(-0.38); m.setPosition(-12 + i * 12, 1.2 + i * 0.6, -14 + i * 10); P.push({ geo: new THREE.BoxGeometry(8, 3 + i, 12), matrix: m, color: SNOW }); }
        P.push(box(0.5, 1.8, 16, 16, 0, 6, 0xf28a1c), box(6, 1.2, 0.5, -16, 0.6, 12, 0x2b78e4), box(0.6, 7, 0.6, -8, 0, -26, DARK), box(0.6, 7, 0.6, 8, 0, -26, DARK), box(17, 2, 0.5, 0, 6, -26, 0x8e24aa)); break;
      }
      case 'luge': P.push(box(0.6, 5, 0.6, -5, 0, -16, DARK), box(0.6, 5, 0.6, 5, 0, -16, DARK), box(11, 1.6, 0.5, 0, 4.4, -16, 0xf28a1c)); for (let i = 0; i < 9; i++) { P.push(box(0.4, 1.2, 0.4, -6 + Math.sin(i) * 3, 0, -14 + i * 4, RED), box(0.4, 1.2, 0.4, 6 + Math.sin(i) * 3, 0, -14 + i * 4, RED)); } for (let i = 0; i < 4; i++) P.push(box(1, 0.5, 1.8, -9 + i * 1.6, 0, 20, [RED, 0x2b78e4, 0xf2c230, 0x2fae4e][i])); break;
      case 'rink': P.push(box(40, 0.5, 24, 0, 0, 0, 0xbfe3f2), box(41, 1.3, 0.6, 0, 0, 12.3, SNOW), box(41, 1.3, 0.6, 0, 0, -12.3, SNOW), box(0.6, 1.3, 25, 20.5, 0, 0, SNOW), box(0.6, 1.3, 25, -20.5, 0, 0, SNOW)); chalet(P, Wn, 9, 6, 1, 0, -18, WOOD, 0); break;
      case 'dogs': for (let i = 0; i < 6; i++) { P.push(box(2.2, 1.6, 2.6, -12 + i * 4.6, 0, -8, WOOD), gable(2.8, 1.2, 3.2, -12 + i * 4.6, 1.6, -8, SNOW)); } chalet(P, Wn, 9, 7, 1, 8, 8, WOOD2, 0); P.push(box(1.4, 0.8, 4, -8, 0, 8, RED), box(0.5, 0.7, 1.2, -8, 0, 2.5, 0xe8e8e8), box(0.5, 0.7, 1.2, -9.2, 0, 1, 0x555555), box(0.5, 0.7, 1.2, -6.8, 0, 1, 0xe8e8e8)); break;
      case 'paraglide': chalet(P, Wn, 8, 6, 1, 0, 0, WOOD, 0); P.push(box(12, 0.4, 3.4, 9, 9, 6, 0xf28a1c), box(4, 0.4, 3.2, 3.5, 8.5, 6, 0xf2c230), box(4, 0.4, 3.2, 14.5, 8.5, 6, 0xf2c230), cyl(0.12, 9, 5, 0, 6, DARK, 4), cyl(0.12, 9, 13, 0, 6, DARK, 4)); break;
      case 'spa': P.push(box(34, 7, 20, 0, 0, 0, 0xe9edf0), box(30, 6, 0.6, 0, 0.5, 10.2, GLASS), cyl(9, 5, 0, 7, 0, GLASS, 14, 2), box(36, 0.8, 22, 0, 7, 0, SNOW), box(14, 0.5, 9, 22, 0, 6, 0x5fc3e6)); Wn.push(box(26, 3, 0.3, 0, 2, 10.6, 0xffffff)); break;
      case 'culture': P.push(box(26, 9, 16, 0, 0, 0, 0xcfc8b8), gable(29, 5, 19, 0, 9, 0, SNOW)); for (let i = 0; i < 5; i++) P.push(cyl(0.9, 8, -10 + i * 5, 0, 9.6, 0xe9e2d0, 8)); P.push(box(28, 0.8, 5, 0, 0, 10, CONC)); break;
      case 'stage': P.push(box(26, 2, 14, 0, 0, 0, DARK), box(1, 12, 1, -12.5, 2, -6.5, 0x777c85), box(1, 12, 1, 12.5, 2, -6.5, 0x777c85), box(1, 10, 1, -12.5, 2, 6.5, 0x777c85), box(1, 10, 1, 12.5, 2, 6.5, 0x777c85), box(28, 0.8, 16, 0, 13, 0, 0x8e24aa), box(26, 9, 0.5, 0, 3, -7, 0x1c1c22)); Wn.push(box(3, 3, 0.3, -8, 6, -6.6, 0xffffff), box(3, 3, 0.3, 8, 6, -6.6, 0xffffff)); break;
      case 'stadium': P.push(box(1, 9, 1, -12, 0, 0, RED), box(1, 9, 1, 12, 0, 0, RED), box(25, 2.4, 0.8, 0, 7, 0, RED)); for (let i = 0; i < 5; i++) P.push(box(30, 1.5 * (i + 1), 3, 0, 0, 14 + i * 3, i % 2 ? 0x2b78e4 : 0x3d8bf0)); chalet(P, Wn, 8, 6, 1, -22, 8, WOOD, 0); for (let i = 0; i < 6; i++) P.push(box(0.3, 2.4, 0.3, (i % 2 ? -4 : 4), 0, -8 - i * 5, i % 2 ? RED : 0x2b78e4)); break;
      case 'olympic': {
        for (let i = 0; i < 20; i++) { const a = i / 20 * 6.283; if (i > 7 && i < 13) continue; const m = M4().makeRotationY(-a); m.setPosition(Math.cos(a) * 42, 6, Math.sin(a) * 42); P.push({ geo: new THREE.BoxGeometry(8, 12, 14), matrix: m, color: i % 2 ? 0xe9edf0 : 0xcfd6de }); }
        P.push(cyl(34, 0.6, 0, 0, 0, SNOW, 24), box(4, 46, 4, -44, 0, -30, CONC));
        { const m = M4().makeRotationX(0.62); m.setPosition(-44, 26, -8); P.push({ geo: new THREE.BoxGeometry(5, 1.2, 52), matrix: m, color: SNOW }); }
        [0x2b78e4, 0x1c1c22, RED, 0xf2c230, 0x2fae4e].forEach((c, i) => { const m = M4().makeTranslation(-12 + i * 6, 20 + (i % 2 ? -3 : 0), 46); P.push({ geo: new THREE.TorusGeometry(3.2, 0.5, 6, 16), matrix: m, color: c }); });
        P.push(cyl(1, 22, 0, 0, 46, CONC, 8), cyl(2.4, 2, 0, 22, 46, 0xf28a1c, 8, 3.4)); break;
      }
      case 'church': P.push(box(10, 8, 18, 0, 0, 0, 0xe9e2d0), gable(12, 5, 20, 0, 8, 0, SNOW), box(5, 18, 5, 0, 0, 11, 0xe9e2d0)); { const g = new THREE.ConeGeometry(4.2, 9, 4); g.rotateY(0.785); P.push({ geo: g, matrix: M4().makeTranslation(0, 22.5, 11), color: 0x5b6470 }); } Wn.push(box(1.2, 2.4, 0.3, 0, 12, 13.6, 0xffffff)); break;
      case 'house': chalet(P, Wn, 10, 9, 2, 0, 0, [WOOD, 0xa8663d, 0x7a4d30][Math.floor(r) % 3], 0); break;
      default: chalet(P, Wn, 14, 10, 1, 0, 0, WOOD, 0);
    }
    return { body: SS.mergeParts(P), windows: Wn.length ? SS.mergeParts(Wn) : null };
  };

  Mo.groomerParts = function (P, x, y, z, ry) {
    const c = Math.cos(ry || 0), s = Math.sin(ry || 0), at = (lx, lz) => [x + lx * c + lz * s, z - lx * s + lz * c];
    let p = at(0, 0); P.push(box(4.2, 1.2, 7, p[0], y, p[1], DARK, ry));
    p = at(0, 0.5); P.push(box(3.6, 2.2, 3.4, p[0], y + 1.2, p[1], RED, ry));
    p = at(0, 4.4); P.push(box(5.6, 1.4, 0.6, p[0], y + 0.2, p[1], 0xb9bcc0, ry));
    p = at(0, -4.2); P.push(box(5.2, 0.5, 1.6, p[0], y + 0.1, p[1], 0x777c85, ry));
  };
  Mo.groomer = function () { const P = []; Mo.groomerParts(P, 0, 0, 0, 0); P.push(box(0.5, 0.4, 0.2, -1.2, 2.2, 2.3, 0xfff2a8), box(0.5, 0.4, 0.2, 1.2, 2.2, 2.3, 0xfff2a8)); return SS.mergeParts(P); };
  Mo.patrol = function () { return SS.mergeParts([box(0.8, 1.7, 0.6, 0, 0, 0, RED), box(0.5, 0.5, 0.5, 0, 1.7, 0, 0xf1c9a5), box(0.9, 0.5, 2.6, 0, 0, -2.2, 0xf28a1c), box(0.3, 0.9, 0.2, 0, 0.6, 0.35, 0xffffff)]); };
  Mo.heli = function () {
    const body = SS.mergeParts([box(2.4, 2.4, 5, 0, 0, 0.5, RED), box(2.2, 1.2, 1.6, 0, 0.9, 3.2, GLASS), box(0.7, 0.8, 5.5, 0, 1.2, -4.5, RED), box(0.2, 2, 1, 0, 1.4, -7, 0xf2c230), box(0.3, 0.6, 0.3, 0, 2.4, 0.5, DARK), box(3, 0.2, 0.3, 0, -0.5, 1.2, DARK), box(0.2, 0.2, 4, -1.4, -0.6, 0.5, DARK), box(0.2, 0.2, 4, 1.4, -0.6, 0.5, DARK)]);
    const rotor = SS.mergeParts([box(12, 0.12, 0.5, 0, 3, 0.5, DARK), box(0.5, 0.12, 12, 0, 3, 0.5, DARK)]);
    return { body, rotor };
  };

  // gares et pylônes d'une remontée, fusionnés dans une seule géométrie (repère monde)
  Mo.liftStatic = function (l, def) {
    const P = [], Wn = [], dx = l.b.x - l.a.x, dz = l.b.z - l.a.z, ry = Math.atan2(dx, dz), k = def.kind, w = Mo.cableOffset(def);
    const col = k === 'tow' ? 0xf28a1c : k === 'chair' ? 0x2b78e4 : k === 'gondola' ? 0xd8342c : 0x8e24aa;
    for (const [st, top] of [[l.a, false], [l.b, true]]) {
      const P2 = [], sgn = top ? 1 : -1;
      if (k === 'carpet') { P2.push(box(3, 1.2, 3, 0, 0, sgn * 2, 0x2fae4e)); }
      else if (k === 'tow') { P2.push(cyl(0.5, 5.5, 0, 0, sgn * 3, CONC, 6), cyl(2.2, 0.4, 0, 5.2, sgn * 3, col, 14), box(4, 3, 3.4, 5.5, 0, 0, WOOD), gable(5, 1.4, 4.4, 5.5, 3, 0, SNOW)); }
      else if (k === 'chair') { P2.push(cyl(0.8, 6.5, -4, 0, 0, CONC, 6), cyl(0.8, 6.5, 4, 0, 0, CONC, 6), box(12, 1.6, 15, 0, 6.5, sgn * 1, col), box(12.6, 0.5, 15.6, 0, 8.1, sgn * 1, SNOW), cyl(3.2, 0.4, 0, 5.6, sgn * 4, DARK, 16), box(4.4, 3, 4, 9, 0, 0, WOOD), gable(5.4, 1.5, 5, 9, 3, 0, SNOW)); }
      else { const big = k === 'cablecar' || def.big; const W = big ? 20 : 15, Dp = big ? 26 : 20, Hh = big ? 13 : 9; P2.push(box(W, Hh, Dp, 0, 0, sgn * 3, big ? CONC : 0xe9edf0), box(W + 1, 1, Dp + 1, 0, Hh, sgn * 3, SNOW), box(W - 4, 4, 0.5, 0, 1, sgn * 3 - sgn * (Dp / 2 + 0.2), DARK), box(W + 0.4, 2, Dp + 0.4, 0, Hh - 3.5, sgn * 3, col)); P2.push(box(W + 0.5, 2.2, Dp - 6, 0, 3, sgn * 3, 0xffffff)); }
      const m = M4().makeRotationY(ry); m.setPosition(st.x, st.y, st.z);
      for (const p of P2) { p.matrix.premultiply(m); (p.color === 0xffffff ? Wn : P).push(p); }
    }
    for (const p of l.path) {
      if (!p.pylon) continue;
      const h = p.y - p.g, big = h > 22, P2 = [];
      if (big) P2.push(cyl(2.4, h, 0, 0, 0, 0x8d9299, 4, 0.9)); else P2.push(cyl(0.55, h, 0, 0, 0, 0x8d9299, 6, 0.4));
      P2.push(box(w * 2 + 1.5, 0.5, 0.7, 0, h - 0.2, 0, 0x6d737b));
      const m = M4().makeRotationY(ry); m.setPosition(p.x, p.g - 0.5, p.z);
      for (const q of P2) { q.matrix.premultiply(m); P.push(q); }
    }
    if (k === 'funi' && false) for (let i = 0; i < l.path.length - 1; i++) {      // funiculaire souterrain : aucune voie visible
      const a = l.path[i], b = l.path[i + 1], len = Math.hypot(b.x - a.x, b.z - a.z), m = M4().makeRotationY(ry); m.multiply(M4().makeRotationX(-Math.atan2(b.y - a.y, len))); m.setPosition((a.x + b.x) / 2, (a.y + b.y) / 2 - 0.9, (a.z + b.z) / 2);
      P.push({ geo: new THREE.BoxGeometry(7.5, 0.7, Math.hypot(len, b.y - a.y) + 0.5), matrix: m, color: 0x6a6f78 });
    }
    if (k === 'carpet') {
      const n = Math.max(1, Math.round(Math.hypot(dx, dz) / 12));
      for (let i = 0; i < n; i++) {
        const x = SS.lerp(l.a.x, l.b.x, (i + 0.5) / n), z = SS.lerp(l.a.z, l.b.z, (i + 0.5) / n), y0 = SS.Terrain.getH(SS.lerp(l.a.x, l.b.x, i / n), SS.lerp(l.a.z, l.b.z, i / n)), y1 = SS.Terrain.getH(SS.lerp(l.a.x, l.b.x, (i + 1) / n), SS.lerp(l.a.z, l.b.z, (i + 1) / n));
        const len = Math.hypot(dx, dz) / n, m = M4().makeRotationY(ry); m.multiply(M4().makeRotationX(-Math.atan2(y1 - y0, len))); m.setPosition(x, (y0 + y1) / 2 + 0.25, z);
        P.push({ geo: new THREE.BoxGeometry(2.4, 0.3, Math.hypot(len, y1 - y0) + 0.3), matrix: m, color: 0x3a3f47 });
      }
    }
    return { body: SS.mergeParts(P), windows: Wn.length ? SS.mergeParts(Wn) : null };
  };
  Mo.cableOffset = def => def.kind === 'funi' ? 1.7 : def.kind === 'tow' ? 1.6 : def.kind === 'chair' ? 2.6 : def.kind === 'carpet' ? 0 : def.kind === 'cablecar' || def.big ? 5 : 3.2;
  Mo.hang = def => def.kind === 'tow' || def.kind === 'funi' ? 0 : def.kind === 'chair' ? 4.6 : def.kind === 'cablecar' ? 7 : def.big ? 6 : 4.4;
  // véhicule : origine sur le câble, corps suspendu en dessous
  Mo.vehicle = function (def) {
    const P = [], k = def.kind;
    if (k === 'tow') { P.push(box(0.12, 3.6, 0.12, 0, -3.6, 0, DARK), box(def.seats > 1 ? 1.6 : 0.5, 0.12, 0.3, 0, -3.7, 0, 0xf28a1c)); }
    else if (k === 'chair') {
      const w = def.seats * 0.75 + 0.4;
      P.push(box(0.14, 3.4, 0.14, 0, -3.4, 0, DARK), box(w, 0.18, 0.9, 0, -4.6, 0.2, 0x3a3f47), box(w, 1.0, 0.16, 0, -4.5, -0.3, 0x3a3f47), box(w, 0.1, 0.1, 0, -3.9, 0.7, DARK));
      if (def.bubble) P.push(box(w + 0.1, 1.5, 1.5, 0, -3.6, 0.1, 0x58a8e0));
    } else if (k === 'gondola') { const s = def.big ? 2 : 1; P.push(box(0.2, 2.2 * s, 0.2, 0, -2.2 * s, 0, DARK), box(2.4 * s, 2.4 * s, 2.8 * s, 0, -4.6 * s, 0, def.big ? 0xe9edf0 : RED), box(2.5 * s, 1 * s, 2.9 * s, 0, -3.6 * s, 0, GLASS)); }
    else if (k === 'cablecar') { P.push(box(0.5, 3, 0.5, 0, -3, 0, DARK), box(4.6, 4.2, 8.4, 0, -7.2, 0, 0xf2c230), box(4.7, 1.6, 8.5, 0, -5.4, 0, GLASS), box(3, 0.5, 2.4, 0, 0, 0, DARK)); }
    else if (k === 'funi') { P.push(box(2.6, 2.6, 13, 0, 0.2, 0, 0xd8342c), box(2.7, 1.0, 12, 0, 1.5, 0, GLASS), box(2.7, 0.3, 13.2, 0, 2.8, 0, SNOW)); }
    else P.push(box(0.01, 0.01, 0.01, 0, 0, 0, DARK));
    return SS.mergeParts(P);
  };

  // visiteurs : veste (teintée par instance) + reste du corps
  Mo.person = function () {
    const jacket = SS.mergeParts([box(0.62, 0.72, 0.4, 0, 0.82, 0, 0xffffff), box(0.2, 0.62, 0.22, -0.42, 0.88, 0.05, 0xffffff), box(0.2, 0.62, 0.22, 0.42, 0.88, 0.05, 0xffffff)]);
    const rest = SS.mergeParts([box(0.26, 0.82, 0.3, -0.16, 0, 0, 0x2a2e36), box(0.26, 0.82, 0.3, 0.16, 0, 0, 0x2a2e36), box(0.36, 0.36, 0.36, 0, 1.56, 0, 0xf1c9a5), box(0.4, 0.16, 0.4, 0, 1.86, 0, 0x30343c), box(0.13, 0.05, 1.9, -0.17, 0, 0.1, 0x16181c), box(0.13, 0.05, 1.9, 0.17, 0, 0.1, 0x16181c)]);
    return { jacket, rest };
  };
})();
