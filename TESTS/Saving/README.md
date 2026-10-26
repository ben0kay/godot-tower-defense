# Saving checks — S1

Run `CampaignStoreChecks.tscn` with the Godot .NET editor/headless runner after building.
One disposable-directory pass covers metadata round-trip/reopen, profile isolation, invalid paths,
occupied-slot refusal, stale writes, valid-backup recovery, corrupt-data preservation, missing/
unknown/duplicate fields, unsupported versions, size bounds and temporary-file cleanup.

These are storage checks, not a battlefield restore test or permission for a broad runtime suite.
Authorship environment had no dotnet/Godot executable, so build/runtime execution remains local.
S2 must add a focused real battlefield round-trip for money, work, cargo and mission-ticket parity.
