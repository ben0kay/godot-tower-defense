# Resource Storage

Place this 2×2 Economy vault for 200 Credits. It takes four seconds to build and holds 100 Refined Metal by default. The inspector shows Stored, Reserved, Incoming and Available stock. The HUD totals physical Metal in storage, including reserved units.

To test material construction, add a positive `RefinedMetal.tres` cost to a tower's construction recipe alongside Credits. One storage must supply the full amount, within the material drone's capacity. The ordinary Citadel engineer starts construction. A second drone leaves a Citadel, loads the reserved stock, delivers it, and assists with a second beam. Work waits at 25% by default until delivery and completes at the normal rate. Delivered Metal is consumed and has no sale refund.

The two Economy buildings have Credits-only recipes. Ammo stock and ammo dispatch are separate. Between-wave checkpoints capture stock, cargo, reservations and flight progress. Older checkpoints still follow the existing authored content-hash compatibility rules.
