# SkiStation Master 1.13 — Chantier de piste

## Philosophy
Construction is optional in time, never mandatory. The default global policy is **Instant Construction** so a player can design a piste and immediately chain the next build. Players who enjoy a management timeline can press **I** to activate timed works.

## Workflow
1. Draw/design a piste as before.
2. Press **K**, then click the piste.
3. The project calculates a cost from length, width and relief.
4. The construction expense is recorded in FinanceCategory.Construction.
5. The piste is closed during timed construction.
6. Proxy construction machines move along the actual piste route.
7. On completion, the piste reopens automatically.

## Controls
- `K`: activate/deactivate construction launch mode.
- `Left Click`: launch project on selected piste.
- `I`: toggle instant/timed construction globally.

## Important
- Instant mode completes immediately: no artificial waiting.
- Timed mode advances with the game clock, so pause/acceleration remain meaningful.
- No fake visitors or vehicles are created.
- The construction layer is intentionally separate from future detailed machine simulation.
