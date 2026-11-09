# Scrap Refinery — planned SALV1

**Status: PLAN ONLY, no gameplay implemented.**

Read the [SALV1 implementation plan](SALV1_IMPLEMENTATION_PLAN.md) for the complete physical-material economy:

1. 1% kill drop → physical Scrap → Citadel salvage drone → Scrap Refinery.
2. Refinery processes 1 Scrap into 5 Refined Metal after 8s; **output remains stored at the refinery**.
3. A distinct refinery cargo drone carries Metal to a separately placeable [Resource Storage](../ResourceStorage/README.md) building.
4. Future metal-cost tower builds reserve Metal from Resource Storage; **two drones leave a Citadel**: an engineering drone flies to the site, and a material/construction drone visits Storage then joins construction.
5. The first drone may advance real work to 25% while awaiting Metal; actual delivery unlocks the rest of the normal work clock.

This design **replaces** the previous proposal to grant Refined Metal directly to the session wallet on refinery completion. Metal is physically held by refinery buffers, material-flight cargo and storage. Future tower costs use existing `ResourceCost` identities with a physical-material affordability adapter; ammo depots and their jobs remain separate.

All balance values are provisional. No existing tower gets a metal price in this plan. This document does not authorize gameplay implementation by itself.
