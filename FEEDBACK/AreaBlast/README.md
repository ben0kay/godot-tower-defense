# Instant impact flash — Phase 4D implemented

BlastFlash.Spawn optionally adds one small self-cleaning Node2D under World/Effects after an instant impact has already applied damage. It draws an orange disc/ring, fades over 0.22 seconds and queues deletion. Radius is passed from the projectile's launch snapshot; the drawn minimum size, ring expansion and duration are purely cosmetic.

This component holds no enemy/registry/hit/tower references and performs no combat queries or damage. It does not reuse enemy-death explosions and is not a shockwave emitter. A missing Effects parent suppresses only the visual. Live-window art review remains necessary; headless tests check visual animation/cleanup cannot repeat damage.
