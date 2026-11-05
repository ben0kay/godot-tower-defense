# Repair Tower — Restoration Tower (PLANNED)

The planned **Restoration Tower** is a 1×1 rotating laser support tower with sleek white/silver artwork and pale aqua detailing. It seeks damaged **friendly towers**, not enemies, and continuously repairs structural hull with a muzzle-origin beam when aligned and funded. The old Vector TD 2026 **Restoration Array** inspired its lowest-integrity targeting and energy-based repair, but the modern tower uses the existing shared Godot targeting/aiming, durability, heat, construction and later capacitor-based power infrastructure.

**Authoritative staged design:** [RT0–RT4 implementation plan](IMPLEMENTATION_PLAN.md). Codex should read this plan plus `NOTES/CodePreferences.md` and implement **only the requested pass** on `dev`. No gameplay implementation exists in this folder yet.

Phase overview: RT0 friendly spatial targeting → RT1 playable repair turret/beam → RT2 shared heat + real PW4 power integration when available → RT3 purchased tiers/Inspector and friendly priority options → RT4 optional Citadel/shield/mobile repair after separate approval. Current `TOWERS/Shared/Power/README.md` marks PW4 consumer buffers as planned, so do **not** invent a second energy or resource accounting system.
