# Ammunition Depot — FUTURE / NOT IMPLEMENTED

**Role:** large ammunition storage building and home station for resupply drones. Receives batches from factories, serves nearby/assigned offensive towers, and makes depot placement and distance strategically important.

## Proposed gameplay
- Configurable maximum storage, ammunition type handling, initial stock, **drone slots**, maximum concurrent jobs and potential electricity draw.
- A [supply drone](../../../LOGISTICS/Ammunition/README.md) departs with reserved ammo, flies to a requesting tower, transfers only what fits, then returns. A depot cannot launch more drones than available slots. Towers may keep firing while a drone is en route.
- Track **stored**, **reserved/in-flight**, and **delivered** ammo separately. No duplicate stock on dispatch/return or when a tower is destroyed during delivery. Decide cancellation/loss/recovery cases explicitly in the implementation pass.
- Future research/level improvements: depot capacity, drone count, payload/dispatch efficiency. Multiple depots must support useful distributed logistics without per-frame all-building scans.

[Tower-local ammunition](../../Shared/Ammunition/README.md) is separately owned after delivery.

**Plans only. No storage or drones have been implemented.**
