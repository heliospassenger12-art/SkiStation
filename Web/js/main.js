// SkiStation Web — démarrage et boucle principale
(function () {
  'use strict';
  const SS = window.SS;
  SS.startGame = function (opts, saved) {
    const T = SS.Terrain, seed = saved ? saved.seed : (opts.seed || 1337);
    T.init(seed); T.genTrees();
    if (saved) SS.restore(saved); else { opts.seed = seed; SS.newGame(opts); }
    SS.R.buildWorld();
    SS.Cam.focus(0, 2250, 1900); SS.Cam.yaw = 0; SS.Cam.pitch = 0.62;
    SS.updatePrestige();
    SS.emit('game:start');
  };
  window.addEventListener('DOMContentLoaded', function () {
    const canvas = document.getElementById('view');
    try { SS.R.init(canvas); } catch (e) { document.getElementById('fatal').style.display = 'flex'; return; }
    SS.Build.initInput(canvas);
    SS.UI.init();
    let last = performance.now();
    function loop(now) {
      const dt = Math.min(0.1, (now - last) / 1000); last = now;
      if (SS.G) { SS.tick(dt); SS.Build.frame(dt); SS.Arcade.frame(dt); SS.R.frame(); SS.UI.frame(dt); }
      requestAnimationFrame(loop);
    }
    requestAnimationFrame(loop);
  });
})();
