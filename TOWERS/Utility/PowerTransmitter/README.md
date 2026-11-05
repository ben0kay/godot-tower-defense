## PW5-B handoff — 2026-11-05

Implemented in Utility with provisional 200 Credits / 4s / 900 HP / 1×1 and separate staged/core art. `DefaultPowerTransmitterConfig.tres` exposes construction values and `HopCostCells=8`. Select a finished operational unit, choose LINK, then another island's completed unpaired Transmitter. Right-click/Esc cancels; UNLINK clears both peers. Temporary EMP/disable suspends the retained pairing; destruction/removal clears its peer.

One static thin cyan line per powered pair is included in B; subdued amber means retained inactive/unpowered link. A positive max-of-both-ends hop cost adds to conducting distance; existing falloff applies. Networks pool each real producer once and share one finite budget; no boost, arbitrary range/throughput limit or generated energy. Durable optional `PowerPeerId` is restored reciprocally before delivery/unpause; broken references fail safely. Source/resource/SVG review only; .NET unavailable. [Full implementation/owner checks](../../Economy/Power/IMPLEMENTATION_PLAN.md). PW5-C animation/coverage/F4 polish remains planned.

---

# Power Transmitter — PW5-B CROSS-ISLAND connector

**Source Implemented / Awaiting Owner Validation — 2026-11-05.** This is a **separate Utility building** from the [Power Relay](../PowerRelay/README.md). Its only function is to connect two distinct conducting islands across empty space through an explicit transmitter-to-transmitter link, **not** to boost local recharge or generate electricity. Follow the [authoritative PW5 plan](../../Economy/Power/IMPLEMENTATION_PLAN.md).

**Authoring/controls:** one peer per Transmitter initially. Select a finished operational unit; choose **Link**, then another eligible transmitter on a different conducting island; support **Unlink**. Reject self/same-island, missing, disabled/dead/incomplete or occupied partner and display useful feedback. No automatic proximity pairing.

**Power rules:** a live reciprocal pair creates one graph edge between island power networks. One island may have **no generator** and still receive energy from the other; if neither side produces, both remain out of supply. Multiple pairs/chains may form connected components without double-counting generation. Only one `PowerGridManager` owns derived network union, cached distance/hop policy, finite combined source budget, existing capacitor recharge and NoPower behavior. Stored buffers persist when links drop; no unlimited transfer, free generator or link-energy bank.

**Lifecycle/saving:** persist the other building's durable Tower SaveId, not Godot instance ID or transient island number. Temporarily disabled ends suspend a saved link; removal/unlink destroys effective link, with safe partner cleanup. Revalidate both ends after restoration, rebuild before unpausing, handle alternate paths, pause and save/retry. No fuel, arbitrary range/capacity caps or tiers in first pass unless owner approves.

**Visual contract:** a thin cyan/blue **visible beam between active paired transmitters is required in PW5-B itself**, not a later optional polish. Only one renderer per pair, no useless duplication and clear inactive/unpowered status. PW5-C subsequently adds subtle moving pulses/glow, selection/Inspector identity, and optional F4 network diagnostics. Do not render a false straight beam between local Relay and generator.

PW5-A and B were implemented together at the owner’s explicit request; minimal source checks follow CodePreferences. [Complete acceptance checklist](../../Economy/Power/IMPLEMENTATION_PLAN.md).
