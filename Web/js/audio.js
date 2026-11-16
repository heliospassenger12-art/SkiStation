// SkiStation Web — ambiance sonore procédurale (Web Audio, aucun fichier) : vent, foule, câbles, interface
(function () {
  'use strict';
  const SS = window.SS;
  const A = SS.Audio = { on: true, ready: false };
  let ac, master, wind, windF, crowd, crowdG, hum, humG, noiseBuf;
  try { const v = localStorage.getItem('skistation_sound'); if (v === '0') A.on = false; } catch (e) { }

  function noise() {
    const n = ac.sampleRate * 2, b = ac.createBuffer(1, n, ac.sampleRate), d = b.getChannelData(0); let last = 0;
    for (let i = 0; i < n; i++) { last = (last + 0.02 * (Math.random() * 2 - 1)) / 1.02; d[i] = last * 3.5; }   // bruit brun
    return b;
  }
  function loop(buf) { const s = ac.createBufferSource(); s.buffer = buf; s.loop = true; s.start(); return s; }
  function start() {
    if (A.ready || !A.on) return;
    const AC = window.AudioContext || window.webkitAudioContext; if (!AC) return;
    try { ac = new AC(); } catch (e) { return; }
    master = ac.createGain(); master.gain.value = 0.5; master.connect(ac.destination);
    noiseBuf = noise();
    windF = ac.createBiquadFilter(); windF.type = 'bandpass'; windF.frequency.value = 400; windF.Q.value = 0.7;
    wind = ac.createGain(); wind.gain.value = 0; loop(noiseBuf).connect(windF); windF.connect(wind); wind.connect(master);
    const cf = ac.createBiquadFilter(); cf.type = 'bandpass'; cf.frequency.value = 900; cf.Q.value = 1.4;
    crowdG = ac.createGain(); crowdG.gain.value = 0; const cs = loop(noiseBuf); cs.playbackRate.value = 1.7; cs.connect(cf); cf.connect(crowdG); crowdG.connect(master);
    hum = ac.createOscillator(); hum.type = 'sawtooth'; hum.frequency.value = 52; const hf = ac.createBiquadFilter(); hf.type = 'lowpass'; hf.frequency.value = 160;
    humG = ac.createGain(); humG.gain.value = 0; hum.connect(hf); hf.connect(humG); humG.connect(master); hum.start();
    A.ready = true;
  }
  function blip(freqs, dur, type, vol) {
    if (!A.ready || !A.on) return; const t = ac.currentTime;
    freqs.forEach((f, i) => {
      const o = ac.createOscillator(), g = ac.createGain(); o.type = type || 'sine'; o.frequency.value = f;
      g.gain.setValueAtTime(0, t + i * dur * 0.6); g.gain.linearRampToValueAtTime(vol || 0.12, t + i * dur * 0.6 + 0.01); g.gain.exponentialRampToValueAtTime(0.0001, t + i * dur * 0.6 + dur);
      o.connect(g); g.connect(master); o.start(t + i * dur * 0.6); o.stop(t + i * dur * 0.6 + dur + 0.05);
    });
  }
  A.click = () => blip([660], 0.06, 'triangle', 0.05);
  A.good = () => blip([523, 659, 784], 0.16, 'sine', 0.08);
  A.bad = () => blip([330, 247], 0.2, 'square', 0.04);
  A.cash = () => blip([1046, 1318], 0.1, 'triangle', 0.07);
  A.setOn = function (v) {
    A.on = v; try { localStorage.setItem('skistation_sound', v ? '1' : '0'); } catch (e) { }
    if (v) { start(); if (ac && ac.state === 'suspended') ac.resume(); } else if (ac) ac.suspend();
  };

  // l'audio ne démarre qu'après une interaction de l'utilisateur
  const kick = () => { start(); if (ac && ac.state === 'suspended' && A.on) ac.resume(); };
  window.addEventListener('pointerdown', kick); window.addEventListener('keydown', kick);
  document.addEventListener('click', e => { if (e.target.closest && e.target.closest('button')) A.click(); });
  SS.on('toast', (m, k) => { if (k === 'good') { if (/construit/.test(m)) A.cash(); else A.good(); } else if (k === 'bad') A.bad(); });

  A.frame = function () {
    if (!A.ready || !A.on || !SS.G) return;
    const G = SS.G, C = SS.Cam, t = ac.currentTime, alt = SS.Terrain.getH(C.tx, C.tz);
    const running = !G.paused;
    const w = SS.clamp((SS.Weather.windNow || 0) / 60, 0.05, 1) * (0.5 + SS.clamp((alt - 1650) / 2500, 0, 1) * 0.7) * (SS.Arcade.active ? 1.4 : 1);
    wind.gain.setTargetAtTime(0.10 * w + (SS.Arcade.active ? SS.clamp(SS.Arcade.st.v / 40, 0, 1) * 0.25 : 0), t, 0.4);
    windF.frequency.setTargetAtTime(280 + w * 500 + (SS.Arcade.active ? SS.Arcade.st.v * 18 : 0), t, 0.4);
    const near = SS.clamp(1 - (C.dist - 200) / 2500, 0, 1), crowdV = SS.clamp(SS.Sim.present / 500, 0, 1);
    crowdG.gain.setTargetAtTime(running && !SS.Arcade.active ? 0.05 * near * crowdV : 0, t, 0.5);
    let moving = 0; for (const l of G.lifts) if (l.moving) moving++;
    humG.gain.setTargetAtTime(running ? SS.clamp(moving / 4, 0, 1) * 0.025 * near : 0, t, 0.5);
  };
})();
