2.27 - Speed feedback prototype (NOT Unity-tested)

- Dynamic camera FOV and camera pullback increase with actual skier speed.
- Peripheral speed streaks left/right/bottom, leaving avatar/center unobstructed.
- Built-in render pipeline: optional radial peripheral screen blur shader via OnRenderImage.
- URP: OnRenderImage is NOT called; fallback streaks + FOV only. True URP blur
  requires a dedicated URP Full Screen Pass / Renderer Feature in a later version.
- During airborne tricks the peripheral effect fades to 35%; crash fades to zero.
- Effects do not change actual world speed or visitor/management systems.
- Create test scene with SkiStation > Arcade > Creer et ouvrir la scene de glisse 2.27.
- The scene is generated on demand and does not modify the main management scene.
