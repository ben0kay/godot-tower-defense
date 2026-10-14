# Enemy movement

`EnemyMovement.cs` follows centrally requested A* waypoints, turns the ship smoothly and caches remaining route distance for tower targeting. Suffix distances are calculated when a route is assigned, so First targeting never runs A*.

A WorldGrid navigation revision stops ships and invalidates their targeting distance until repathed. No-route cases wait and retry at EnemyMasterConfig.FailedPathRetrySeconds. Each continuous movement segment checks ship-radius clearance before moving; unsafe segments stop, never fall back to direct travel. Routing currently uses cell centers with corner-safe diagonals; ship separation/path offsets and large-ship routing are future work. Ships may overlap each other in this test.
