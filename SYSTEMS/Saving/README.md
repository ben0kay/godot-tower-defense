# Saving — S0 audit / S1 storage foundation

Implemented 2026-10-26: `CampaignData` (typed v1 metadata) and `CampaignStore` (strict
per-profile storage, flushed atomic writes and read-only backup recovery).
See [the S0 audit and S0–S3 plan](IMPLEMENTATION_PLAN.md) for source ownership,
important findings, full storage contract and remaining integration.

**Battlefield capture/restore and Save/Load/Continue remain unimplemented.** No game caller
writes campaign files yet. Metadata alone cannot resume a level. S2 supplies the complete
typed battlefield schema and capture/restore; S3 enables the real interface. Save will be
disabled during combat with the exact explanation **Unavailable during combat**.
Between-wave construction and ammunition deliveries must be preserved, not used as extra gates.

Profile identity, XP, RP, purchased research and mission reward receipts already persist through
Profiles/Progression. Those remain separate from campaign metadata and battlefield saves.
Keep gameplay rules with their existing owners, never serialize cached TowerFinalStats,
never write restored values into authored maps or shared default resources.

Validation is deliberately brief: source/diff checks here; .NET/Godot build and the focused
[storage fixture](../../TESTS/Saving/README.md) require local execution because neither runtime
was available. No extensive runtime suite or performance profiling was run.
