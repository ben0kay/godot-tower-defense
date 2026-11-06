# Drone Station
Building scene/config/artwork and station UI live here; fighter flight, targeting, service and ownership stay in [DRONES/Combat](../../../DRONES/Combat/README.md).

FD4.1/FD5 are **source implemented / awaiting owner validation**. Complete construction, buy a fighter, choose its slot and LAUNCH. Defaults remain `drone_station`, 2×3, two fighter slots and 250 Credits per fighter.

Station stock starts empty (capacity 2000), supplied by real Depot couriers on the existing 10% demand policy. One FIFO bay rearms at 200 rounds/6s, transferring real stock; repairs 20 HP/s at 1 Credit/10 HP with retained fractional prepaid credit. Capacity, bay count, full-refill duration, HP rate and pricing are Inspector-editable. A tiny bar beside the actual docked fighter shows real rearm progress; stock shortages wait without ticking. Roster shows stock, courier and service status.

Auto-service returns can relaunch; manual recall and station disable stay docked. Docking/launch/loading never grants stock or HP. Save/load retains inventory, shipments, service reason/progress and repair credit. Sale/destruction withdraws courier destinations and accounts remaining station stock as loss. FD3 patrol and anti-air remain separate.
