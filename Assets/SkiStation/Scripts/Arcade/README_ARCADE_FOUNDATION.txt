SkiStation 2.24 - Open World Arcade Foundation (opt-in / untested in Unity)

- ArcadeSkiController: free movement anywhere with colliders, not locked to pistes.
- Trees/rocks deflect; only unfinished aerial tricks cause a crash.
- Huge landing never crashes a properly aligned skier; optional snow burst scales with impact.
- R retries from the safe pre-jump checkpoint immediately or after watching the fall.
- ArcadeSkiSession: EnterWorld/ExitWorld API for a later management UI integration.
- No automatic scene objects, no visitor spawns, no AI changes, no new demo domain.
- This is an architectural prototype, NOT a completed SSX-style controller.
- Input prototype: WASD / arrows, Space jump, F/G spin, Left Ctrl brake, R retry, Esc exit.
- Needs a separate test scene, dedicated avatar and camera, real collider validation,
  animation/ragdoll, snowboard variant, effects and tuning before player release.
- IMPORTANT: 2.14 ghost-domain fix and 2.17-2.23 remain unverified in Unity.
