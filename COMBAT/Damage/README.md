# Typed damage — Phase 4C-1

DamageInfo validates finite nonnegative base amounts and Kinetic/Energy/Explosive types. DefaultDamageProfile.tres owns separate shield/hull coefficients: Kinetic .5/1, Energy 2/.85, Explosive .25/1.25. DamageResolver references this resource once. Invalid coefficients/types/state fail before damage state commits. Zero amounts are safe no-ops.

Shields consume min(base amount, shield / shield coefficient) base units. Remaining base units damage hull using its own coefficient. Energy 20 against shield 30 consumes 15 base, then deals 4.25 hull. With a zero shield coefficient, existing shields block without being damaged or bypassed. Double intermediates avoid overflow and outputs remain finite/nonnegative.

Enemy.ApplyDamage uses this resolver; TakeDamage(float) retains explicit 1:1 shield-first legacy behavior. Only hull loss refreshes the hull bar; hull death alone publishes the existing kill/reward lifecycle. No shield recharge/UI/armor or typed Citadel attacks were added.
