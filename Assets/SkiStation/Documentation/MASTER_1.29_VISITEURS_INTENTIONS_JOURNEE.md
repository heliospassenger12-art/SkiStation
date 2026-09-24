# SkiStation 1.29 — Visitors, intentions & daily plans

1.29 deepens the real management visitor layer. Each existing SkierRuntime receives a lightweight daily plan derived from VisitorIntent and current state.

## Principles
- No artificial visitors.
- No teleportation.
- No player simulation coupling.
- Existing movement/routing remains authoritative.
- Plans are advisory and can be reevaluated as the visitor's real state changes.

## Main runtime
- VisitorDayPlanRuntime
- VisitorDayPlanBootstrap
- VisitorDayPlanDebugUI (F14)

The plan tracks current activity, next activity, remaining simulated minutes, completed steps and confidence.
