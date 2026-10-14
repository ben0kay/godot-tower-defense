# Optional enemy abilities — E2B

Abilities are opt-in scene children, grouped under a plain Abilities node when useful. Ordinary Enemy001/Enemy002 have none: no mandatory manager, empty controller, discovery/reflection or ability tick is added. The only implemented component is ShieldEmitter/. Host Enemy.Initialized runs after registry/movement setup; existing Removed handles kill/arrival and component ExitTree handles unload. Enemy exposes a read-only Registry accessor and safe RestoreShield API. Ability cooldowns/cursors are instance-local; source configs and recipient definitions are never mutated.

No universal EnemyAbility hierarchy was needed for one component. E3A death-spawning, E3B weapons/electrical pulses/tower durability, linked shields, timed effects and armor remain later work.
