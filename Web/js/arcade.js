// SkiStation Web — mode arcade : descente libre à la troisième personne sur le domaine construit
(function () {
  'use strict';
  const SS = window.SS, T = SS.Terrain, R = SS.R;
  const A = SS.Arcade = { active: false, touch: {} };
  let mesh, st, g = {};

  A.start = function (piste) {
    const G = SS.G, p0 = piste.pts[0], p1 = piste.pts[Math.min(4, piste.pts.length - 1)];
    if (!mesh) {
      const per = SS.Models.person(); mesh = new THREE.Group();
      mesh.add(new THREE.Mesh(per.jacket, new THREE.MeshLambertMaterial({ color: 0xff5a1f })), new THREE.Mesh(per.rest, new THREE.MeshLambertMaterial({ vertexColors: true })));
    }
    R.scene.add(mesh);
    st = A.st = { piste, x: p0.x, z: p0.z, y: p0.y, h: Math.atan2(p1.x - p0.x, p1.z - p0.z), v: 3, vy: 0, air: false, spin: 0, flip: 0, score: 0, t: 0, crash: 0, done: false, best: 0, msg: 'Prêt ? Penchez-vous dans la pente !', msgT: 3, airT: 0, top: 0 };
    A.prev = { paused: G.paused, speed: G.speed }; G.paused = false; G.speed = 1 / (SS.CLOCK_RATE * SS.MOTION);
    A.active = true; SS.Build.cancel(true); SS.emit('arcade');
  };
  A.stop = function () {
    if (!A.active) return; A.active = false; R.scene.remove(mesh);
    SS.G.paused = A.prev.paused; SS.G.speed = A.prev.speed; SS.emit('arcade'); SS.emit('speed');
  };
  function say(m) { st.msg = m; st.msgT = 2.2; }

  A.frame = function (dt) {
    if (!A.active) return;
    const k = SS.Build.keys || {}, tc = A.touch;
    const left = k.KeyA || k.ArrowLeft || tc.left, right = k.KeyD || k.ArrowRight || tc.right, tuck = k.KeyW || k.ArrowUp || tc.tuck, brake = k.KeyS || k.ArrowDown || tc.brake, jump = k.Space || tc.jump;
    dt = Math.min(dt, 0.05);
    if (!st.done) st.t += dt;
    st.msgT -= dt;
    const steer = (left ? 1 : 0) - (right ? 1 : 0);
    if (st.crash > 0) { st.crash -= dt; st.v *= 0.92; }
    else if (st.air) {
      st.spin += steer * 6.5 * dt; st.flip += ((tuck ? 1 : 0) - (brake ? 1 : 0)) * 6 * dt;
      st.vy -= 16 * dt; st.y += st.vy * dt; st.airT += dt;
    } else {
      st.h += steer * (2.1 - Math.min(1.1, st.v / 40)) * dt;
      T.grad(st.x, st.z, g);
      const hx = Math.sin(st.h), hz = Math.cos(st.h), sl = -(g.x * hx + g.z * hz);
      const on = SS.closestOnPiste(st.piste, st.x, st.z).d < st.piste.width / 2 + 4 || SS.G.pistes.some(p => SS.closestOnPiste(p, st.x, st.z).d < p.width / 2 + 4);
      let a = 9.8 * sl * 1.15 - (tuck ? 0.0016 : 0.0034) * st.v * st.v - (on ? 0.25 : 1.6) - Math.abs(steer) * st.v * 0.16 - (brake ? 9 : 0);
      st.v = SS.clamp(st.v + a * dt, 0, 46);
      if (st.v < 1.2 && sl > 0.03 && !brake) st.v = 1.2;
      if (jump && st.v > 3) { st.air = true; st.vy = 6.5 + st.v * 0.16; st.spin = 0; st.flip = 0; st.airT = 0; }
      st.offPiste = !on;
    }
    const hx = Math.sin(st.h), hz = Math.cos(st.h);
    const nx = st.x + hx * st.v * dt, nz = st.z + hz * st.v * dt;
    if (T.inside(nx, nz, 40)) { st.x = nx; st.z = nz; } else st.v = 0;
    const gh = T.getH(st.x, st.z);
    if (st.air) {
      if (st.y <= gh) {
        st.air = false; st.y = gh;
        const rot = Math.abs(st.spin) / 6.283, fl = Math.abs(st.flip) / 6.283, es = Math.abs(rot - Math.round(rot)), ef = Math.abs(fl - Math.round(fl));
        if (es > 0.2 || ef > 0.2) { st.crash = 1.0; say('Aïe ! Réception ratée'); }
        else { const pts = Math.round((Math.round(rot) * 360 + Math.round(fl) * 500 + st.airT * 60) / 10) * 10; if (pts > 0) { st.score += pts; say((Math.round(fl) ? 'Flip ' : '') + (Math.round(rot) ? Math.round(rot) * 360 + '° ' : 'Saut ') + '+' + pts); } }
        st.spin = 0; st.flip = 0;
      }
    } else {
      if (st.y - gh > 1.2 && st.v > 9) { st.air = true; st.vy = 0; st.airT = 0; }       // décollage naturel sur une rupture de pente
      else st.y = gh;
    }
    st.top = Math.max(st.top, st.v);
    const end = st.piste.pts[st.piste.pts.length - 1];
    if (!st.done && Math.hypot(st.x - end.x, st.z - end.z) < 28) { st.done = true; st.score += Math.max(0, Math.round(600 - st.t * 3)); say('Arrivée ! ' + st.t.toFixed(1) + ' s — ' + st.score + ' pts'); st.msgT = 99; }
    mesh.position.set(st.x, st.y, st.z); mesh.scale.setScalar(1.25);
    mesh.rotation.set(st.crash > 0 ? -1.3 : st.flip + (tuck && !st.air ? 0.5 : 0), st.h + st.spin, st.air ? 0 : -steer * 0.35, 'YXZ');
    const cam = R.camera, cx = st.x - hx * 15, cz = st.z - hz * 15, cy = Math.max(T.getH(SS.clamp(cx, -SS.HALF, SS.HALF), SS.clamp(cz, -SS.HALF, SS.HALF)) + 3, st.y + 6.5);
    cam.position.lerp(new THREE.Vector3(cx, cy, cz), 1 - Math.exp(-dt * 6)); cam.lookAt(st.x + hx * 9, st.y + 1.5, st.z + hz * 9);
  };
})();
