# Resource Storage — SALV1 planned building

**Status: PLAN ONLY — not implemented.** Authoritative scope: [SALV1 implementation plan](../ScrapRefinery/SALV1_IMPLEMENTATION_PLAN.md).

The **Resource Storage** building is **separate from Ammunition Depot**. It is intended for physical **Refined Metal** rather than ammunition rounds; it has its own placeable scene/config, inventory, capacity, incoming and construction reservations, selected-building Inspector, SaveId and checkpoint data.

Suggested initial values: Economy build category, **200 Credits**, **2×2**, **4-second build**, **1,000 HP**, and **100 Metal storage capacity**, starting empty. A refinery transport drone moves processed metal from the Scrap Refinery output buffer into Storage. The inventory stays local to that specific building and never automatically becomes a global wallet balance.

For a future tower whose construction recipe requires Metal, a **Citadel-launched material/construction drone** retrieves reserved Metal from an operational Resource Storage, delivers it physically to the construction site, then joins a **second Citadel-launched engineering drone** in building that tower. Ordinary Credits-only buildings keep their existing construction path.

Existing `AmmunitionDepot/AmmoStorage` and `AmmunitionLogistics` demonstrate finite stock, reservations and drone transfer **but must not own Metal**. Do not share inventory instances or cargo jobs between the two building types. This feature does not implement other material categories, campaign-wide inventory, research, or new tower prices yet.
