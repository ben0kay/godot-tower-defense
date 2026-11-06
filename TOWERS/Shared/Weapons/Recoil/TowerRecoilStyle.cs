// Compatibility resource: existing tower presets keep their script, class, properties and snapshot contract.
using Godot;

public readonly record struct TowerRecoilSnapshot(bool Enabled, float Distance, double KickSeconds, double ReturnSeconds, float Maximum);

[GlobalClass]
public partial class TowerRecoilStyle : WeaponRecoilStyle
{
    public new TowerRecoilSnapshot Capture()
    {
        WeaponRecoilSnapshot motion = base.Capture();
        return new(motion.Enabled, motion.Distance, motion.KickSeconds, motion.ReturnSeconds, motion.Maximum);
    }
}
