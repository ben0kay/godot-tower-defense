# Shared detection eligibility — SE1

TowerDetection.CanDetect(enemy, detectsStealth) is the common living/nonqueued/stealth gate. TowerConfig exposes Detection / CanDetectStealth=false by default, copied privately through the existing config-copy flow. Enabling it allows that specific tower to acquire a hidden target without revealing it globally.

TowerTargeting.Find checks detection before preferences, status eligibility and First/Last/Closest/Farthest/Strongest/Weakest scoring. Valid checks cached locks too. Battle, Blaster, Rocket, Laser and the shared Sniper/MachineGun/Cryo/Stasis turret path pass their config capability to both acquisition and cached checks. Laser stops continuous damage/ammo on its next attack tick when revelation is lost. Shield preference never bypasses hidden-target eligibility. No new targeting-priority button or automatically detecting offensive tower is added.

Stealth prevents acquiring/tracking targets; it is not damage immunity. Already-launched projectiles finish their existing delivery, splash can incidentally hit hidden enemies, and Shockwave remains an unguided omnidirectional pulse. This intentionally keeps damage resolution separate from perception. A future change to stealth immunity/delivery would need its own explicit policy and pass.
